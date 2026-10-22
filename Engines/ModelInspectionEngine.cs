using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Autodesk.Revit.DB;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Engines
{
    public sealed class ModelInspectionEngine : IExportEngine
    {
        private string _reportPath;

        public string DisplayName
        {
            get { return "Model inspection report (QA before export)"; }
        }

        public string OutputExtension
        {
            get { return ".csv"; }
        }

        public bool RequiresActiveDocument
        {
            get { return false; }
        }

        public bool WritesPerModelFile
        {
            get { return false; }
        }

        public bool ProducesIntermediateFile
        {
            get { return false; }
        }

        public bool NeedsView(ExportSettings settings)
        {
            return false;
        }

        public string Validate(ExportSettings settings)
        {
            return null;
        }

        public ExportStepResult Begin(ExportContext context)
        {
            try
            {
                if (_reportPath == null)
                {
                    _reportPath = Path.Combine(
                        context.OutputDirectory,
                        "Model_Inspection_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv");
                    File.WriteAllText(_reportPath, CsvUtil.Row(Columns()) + Environment.NewLine, new UTF8Encoding(true));
                }

                string row = CsvUtil.Row(Inspect(context).ToArray());
                File.AppendAllText(_reportPath, row + Environment.NewLine, new UTF8Encoding(true));
                return ExportStepResult.Completed("Inspected.", new[] { _reportPath });
            }
            catch (Exception ex)
            {
                return ExportStepResult.Failed("Inspection failed: " + ex.Message);
            }
        }

        public ExportStepResult Poll(ExportContext context)
        {
            return ExportStepResult.Completed("Inspected.", context.OutputFiles);
        }

        private static string[] Columns()
        {
            return new[]
            {
                "Model", "Path", "Saved In Revit", "File Size (MB)", "Project Name", "Project Number",
                "Workshared", "Central Path", "Worksets", "Levels", "Grids", "3D Views", "Sheets", "Phases",
                "Revit Links", "Link Status", "CAD Instances", "Rooms", "Model Elements", "Warnings"
            };
        }

        private static List<string> Inspect(ExportContext context)
        {
            Document doc = context.Document;
            ExportJobItem item = context.Item;
            bool deep = context.Settings.InspectionDeepScan;

            string centralPath = string.Empty;
            string worksetCount = string.Empty;
            if (doc.IsWorkshared)
            {
                ModelPath central = doc.GetWorksharingCentralModelPath();
                centralPath = central == null ? string.Empty : ModelPathUtils.ConvertModelPathToUserVisiblePath(central);
                worksetCount = WorksetSummary(doc);
            }

            string linkStatus;
            int links = LinkSummary(doc, out linkStatus);

            ProjectInfo info = doc.ProjectInformation;
            List<string> cells = new List<string>
            {
                item.ModelName,
                item.FilePath,
                item.RevitVersion,
                item.SizeText.Replace(" MB", string.Empty),
                info == null ? string.Empty : info.Name,
                info == null ? string.Empty : info.Number,
                doc.IsWorkshared ? "Yes" : "No",
                centralPath,
                worksetCount,
                Count(doc, typeof(Level)),
                Count(doc, typeof(Grid)),
                CountViews3D(doc),
                Count(doc, typeof(ViewSheet)),
                doc.Phases.Size.ToString(CultureInfo.InvariantCulture),
                links.ToString(CultureInfo.InvariantCulture),
                linkStatus,
                deep ? Count(doc, typeof(ImportInstance)) : string.Empty,
                deep ? RoomCount(doc) : string.Empty,
                deep ? ModelElementCount(doc) : string.Empty,
                deep ? doc.GetWarnings().Count.ToString(CultureInfo.InvariantCulture) : string.Empty
            };
            return cells;
        }

        private static string Count(Document doc, Type type)
        {
            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                return collector.OfClass(type).GetElementCount().ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string CountViews3D(Document doc)
        {
            int count = 0;
            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                collector.OfClass(typeof(View3D));
                foreach (Element element in collector)
                {
                    View3D view = element as View3D;
                    if (view != null && !view.IsTemplate)
                    {
                        count++;
                    }
                }
            }

            return count.ToString(CultureInfo.InvariantCulture);
        }

        private static string RoomCount(Document doc)
        {
            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                return collector.OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().GetElementCount().ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string ModelElementCount(Document doc)
        {
            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                return collector.WhereElementIsNotElementType().GetElementCount().ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string WorksetSummary(Document doc)
        {
            int count = 0;
            FilteredWorksetCollector collector = new FilteredWorksetCollector(doc);
            try
            {
                collector.OfKind(WorksetKind.UserWorkset);
                foreach (Workset workset in collector)
                {
                    if (workset != null)
                    {
                        count++;
                    }
                }
            }
            finally
            {
                Disposer.Release(collector);
            }

            return count.ToString(CultureInfo.InvariantCulture);
        }

        private static int LinkSummary(Document doc, out string status)
        {
            List<string> parts = new List<string>();
            using (FilteredElementCollector collector = new FilteredElementCollector(doc))
            {
                collector.OfClass(typeof(RevitLinkType));
                foreach (Element element in collector)
                {
                    RevitLinkType link = element as RevitLinkType;
                    if (link != null)
                    {
                        parts.Add(link.Name + ": " + link.GetLinkedFileStatus());
                    }
                }
            }

            status = string.Join("; ", parts);
            return parts.Count;
        }
    }
}
