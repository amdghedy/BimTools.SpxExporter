using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class ViewPreparationService
    {
        public string Apply(Document doc, View3D view, ExportSettings settings, bool isTemporary)
        {
            if (!isTemporary && !settings.PrepareExistingViews)
            {
                return string.Empty;
            }

            List<string> notes = new List<string>();
            using (Transaction transaction = new Transaction(doc, "BIM TOOLS - Prepare export view"))
            {
                transaction.Start();
                try
                {
                    ApplyDetailLevel(view, settings);
                    HideCategories(doc, view, settings, notes);
                    ApplyPhase(doc, view, settings, notes);
                    HideWorksets(doc, view, settings, notes);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    return "View preparation failed: " + ex.Message;
                }
            }

            return notes.Count == 0 ? string.Empty : string.Join(" ", notes);
        }

        private static void ApplyDetailLevel(View3D view, ExportSettings settings)
        {
            switch (settings.ViewDetailLevel)
            {
                case DetailLevelOption.Coarse:
                    view.DetailLevel = ViewDetailLevel.Coarse;
                    break;
                case DetailLevelOption.Medium:
                    view.DetailLevel = ViewDetailLevel.Medium;
                    break;
                case DetailLevelOption.Fine:
                    view.DetailLevel = ViewDetailLevel.Fine;
                    break;
            }
        }

        private static void HideCategories(Document doc, View3D view, ExportSettings settings, List<string> notes)
        {
            List<ElementId> ids = new List<ElementId>();
            if (settings.ViewHideRooms)
            {
                ids.Add(new ElementId(BuiltInCategory.OST_Rooms));
            }

            if (settings.ViewHideGridsAndLevels)
            {
                ids.Add(new ElementId(BuiltInCategory.OST_Grids));
                ids.Add(new ElementId(BuiltInCategory.OST_Levels));
            }

            if (settings.ViewHideLinks)
            {
                ids.Add(new ElementId(BuiltInCategory.OST_RvtLinks));
            }

            if (settings.ViewHideViewMarkers)
            {
                ids.Add(new ElementId(BuiltInCategory.OST_Cameras));
                ids.Add(new ElementId(BuiltInCategory.OST_Viewers));
            }

            string[] names = PatternMatcher.Split(settings.ViewHideCategoryNames);
            if (names.Length > 0)
            {
                foreach (Category category in doc.Settings.Categories)
                {
                    if (PatternMatcher.MatchesAny(category.Name, names))
                    {
                        ids.Add(category.Id);
                    }
                }
            }

            int hidden = 0;
            foreach (ElementId id in ids)
            {
                try
                {
                    if (view.CanCategoryBeHidden(id))
                    {
                        view.SetCategoryHidden(id, true);
                        hidden++;
                    }
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    continue;
                }
            }

            if (hidden > 0)
            {
                notes.Add("Hid " + hidden + " categor" + (hidden == 1 ? "y." : "ies."));
            }
        }

        private static void ApplyPhase(Document doc, View3D view, ExportSettings settings, List<string> notes)
        {
            if (!string.IsNullOrWhiteSpace(settings.ViewPhaseName))
            {
                Parameter phaseParameter = view.get_Parameter(BuiltInParameter.VIEW_PHASE);
                if (phaseParameter != null && !phaseParameter.IsReadOnly)
                {
                    foreach (Phase phase in doc.Phases)
                    {
                        if (string.Equals(phase.Name, settings.ViewPhaseName.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            phaseParameter.Set(phase.Id);
                            notes.Add("Phase '" + phase.Name + "'.");
                            break;
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.ViewPhaseFilterName))
            {
                Parameter filterParameter = view.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER);
                if (filterParameter != null && !filterParameter.IsReadOnly)
                {
                    using (FilteredElementCollector collector = new FilteredElementCollector(doc))
                    {
                        collector.OfClass(typeof(PhaseFilter));
                        foreach (Element element in collector)
                        {
                            if (string.Equals(element.Name, settings.ViewPhaseFilterName.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                filterParameter.Set(element.Id);
                                notes.Add("Phase filter '" + element.Name + "'.");
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void HideWorksets(Document doc, View3D view, ExportSettings settings, List<string> notes)
        {
            string[] patterns = PatternMatcher.Split(settings.ViewHideWorksetPatterns);
            if (patterns.Length == 0 || !doc.IsWorkshared)
            {
                return;
            }

            int hidden = 0;
            FilteredWorksetCollector collector = new FilteredWorksetCollector(doc);
            try
            {
                collector.OfKind(WorksetKind.UserWorkset);
                foreach (Workset workset in collector)
                {
                    if (PatternMatcher.MatchesAny(workset.Name, patterns))
                    {
                        view.SetWorksetVisibility(workset.Id, WorksetVisibility.Hidden);
                        hidden++;
                    }
                }
            }
            finally
            {
                Disposer.Release(collector);
            }

            if (hidden > 0)
            {
                notes.Add("Hid " + hidden + " workset(s) in the export view.");
            }
        }
    }
}
