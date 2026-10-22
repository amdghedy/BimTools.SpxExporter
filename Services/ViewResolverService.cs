using System;
using Autodesk.Revit.DB;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Services
{
    public sealed class ViewResolverService
    {
        public const string TemporaryViewName = "BIM_TOOLS_SPX_EXPORT";
        public const string TemporaryPerspectiveName = "BIM_TOOLS_SPX_EXPORT_PERSPECTIVE";

        public ViewResolveResult Resolve(Document doc, ExportSettings settings)
        {
            ViewResolveResult result = new ViewResolveResult();

            if (settings.ViewMode == ViewSelectionMode.TemporaryIsometric)
            {
                return CreateTemporary(doc, settings, result);
            }

            View3D found = FindExisting(doc, settings);
            if (found != null)
            {
                result.View = found;
                result.Message = "Using view '" + found.Name + "'.";
                return result;
            }

            if (settings.FallbackToTemporaryView)
            {
                return CreateTemporary(doc, settings, result);
            }

            result.Message = "No suitable 3D view was found in the model.";
            return result;
        }

        private static bool IsTemporaryName(string name)
        {
            return name == TemporaryViewName || name == TemporaryPerspectiveName;
        }

        private static View3D FindExisting(Document doc, ExportSettings settings)
        {
            bool wantPerspective = settings.ViewProjection == ViewProjectionOption.Perspective;
            bool byName = settings.ViewMode == ViewSelectionMode.ByName;
            View3D firstUsable = null;
            View3D defaultView = null;

            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                collector.OfClass(typeof(View3D));
                foreach (Element element in collector)
                {
                    View3D view = element as View3D;
                    if (view == null || view.IsTemplate || IsTemporaryName(view.Name))
                    {
                        continue;
                    }

                    if (byName)
                    {
                        if (string.Equals(view.Name, settings.ViewName, StringComparison.OrdinalIgnoreCase))
                        {
                            return view;
                        }

                        continue;
                    }

                    if (view.IsPerspective != wantPerspective)
                    {
                        continue;
                    }

                    if (defaultView == null && string.Equals(view.Name, "{3D}", StringComparison.OrdinalIgnoreCase))
                    {
                        defaultView = view;
                    }

                    if (firstUsable == null)
                    {
                        firstUsable = view;
                    }
                }
            }

            return defaultView ?? firstUsable;
        }

        private static ViewResolveResult CreateTemporary(Document doc, ExportSettings settings, ViewResolveResult result)
        {
            bool perspective = settings.ViewProjection == ViewProjectionOption.Perspective;
            string temporaryName = perspective ? TemporaryPerspectiveName : TemporaryViewName;

            using (FilteredElementCollector existing = new FilteredElementCollector(doc))
            {
                existing.OfClass(typeof(View3D));
                foreach (Element element in existing)
                {
                    View3D candidate = element as View3D;
                    if (candidate != null && !candidate.IsTemplate && candidate.Name == temporaryName)
                    {
                        result.View = candidate;
                        result.CreatedTemporary = true;
                        result.Message = "Reusing temporary export view.";
                        return result;
                    }
                }
            }

            ElementId typeId = ElementId.InvalidElementId;
            using (FilteredElementCollector types = new FilteredElementCollector(doc))
            {
                types.OfClass(typeof(ViewFamilyType));
                foreach (Element element in types)
                {
                    ViewFamilyType type = element as ViewFamilyType;
                    if (type != null && type.ViewFamily == ViewFamily.ThreeDimensional)
                    {
                        typeId = type.Id;
                        break;
                    }
                }
            }

            if (ElementId.InvalidElementId.Equals(typeId))
            {
                result.Message = "The model has no 3D view family type, so a temporary view cannot be created.";
                return result;
            }

            using (Transaction transaction = new Transaction(doc, "BIM TOOLS - Temporary SPX export view"))
            {
                transaction.Start();
                try
                {
                    View3D view = perspective ? View3D.CreatePerspective(doc, typeId) : View3D.CreateIsometric(doc, typeId);
                    view.Name = temporaryName;
                    view.DetailLevel = ViewDetailLevel.Fine;
                    if (view.IsSectionBoxActive)
                    {
                        view.IsSectionBoxActive = false;
                    }

                    if (perspective)
                    {
                        DisableCrop(view);
                    }

                    transaction.Commit();
                    result.View = view;
                    result.CreatedTemporary = true;
                    result.Message = perspective
                        ? "Created temporary perspective 3D view (discarded when the model closes)."
                        : "Created temporary orthographic 3D view (discarded when the model closes).";
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    result.Message = "Could not create a temporary 3D view: " + ex.Message;
                }
            }

            return result;
        }

        private static void DisableCrop(View3D view)
        {
            try
            {
                view.CropBoxActive = false;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                return;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                return;
            }
        }
    }
}
