using System;
using System.Reflection;
using System.Windows.Media;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Commands;
using BimTools.SpxExporter.Infrastructure;

namespace BimTools.SpxExporter.Startup
{
    public sealed class SpxExporterApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                CreateRibbonTabIfMissing(application);
                RibbonPanel panel = GetOrCreatePanel(application);
                string assemblyPath = Assembly.GetExecutingAssembly().Location;

                PushButtonData batch = new PushButtonData(
                    "BimTools.SpxExporter.Batch",
                    "Batch SPX\nExport",
                    assemblyPath,
                    typeof(BatchExportCommand).FullName);
                batch.ToolTip = "Batch-export Revit models to SYNCHRO SPX (through the SYNCHRO exporter).";
                batch.LongDescription = "Select many Revit models or whole folders, choose the export method, worksets, view and naming rules, and export them unattended with logging and a CSV report.";
                batch.LargeImage = IconFactory.Create("SPX", Color.FromRgb(23, 92, 211), 32);
                batch.Image = IconFactory.Create("SPX", Color.FromRgb(23, 92, 211), 16);

                PushButtonData active = new PushButtonData(
                    "BimTools.SpxExporter.Active",
                    "Export Active",
                    assemblyPath,
                    typeof(ExportActiveModelCommand).FullName);
                active.ToolTip = "Export the active model with the last-used batch settings.";
                active.Image = IconFactory.Create("1", Color.FromRgb(6, 118, 71), 16);
                active.LargeImage = IconFactory.Create("1", Color.FromRgb(6, 118, 71), 32);

                PushButtonData inspector = new PushButtonData(
                    "BimTools.SpxExporter.Inspector",
                    "Model Inspector",
                    assemblyPath,
                    typeof(ModelInspectorCommand).FullName);
                inspector.ToolTip = "Open many models and write a QA report: version, worksets, links, warnings, element counts.";
                inspector.Image = IconFactory.Create("QA", Color.FromRgb(181, 71, 8), 16);
                inspector.LargeImage = IconFactory.Create("QA", Color.FromRgb(181, 71, 8), 32);

                PushButtonData data = new PushButtonData(
                    "BimTools.SpxExporter.ElementData",
                    "Element Data",
                    assemblyPath,
                    typeof(ElementDataCommand).FullName);
                data.ToolTip = "Export element ids, categories, levels, phases and parameters to CSV for 4D linking.";
                data.Image = IconFactory.Create("CSV", Color.FromRgb(14, 116, 144), 16);
                data.LargeImage = IconFactory.Create("CSV", Color.FromRgb(14, 116, 144), 32);

                PushButtonData logs = new PushButtonData(
                    "BimTools.SpxExporter.Logs",
                    "Open Logs",
                    assemblyPath,
                    typeof(OpenLogFolderCommand).FullName);
                logs.ToolTip = "Open the folder containing export logs.";
                logs.Image = IconFactory.Create("LOG", Color.FromRgb(71, 84, 103), 16);
                logs.LargeImage = IconFactory.Create("LOG", Color.FromRgb(71, 84, 103), 32);

                PushButtonData about = new PushButtonData(
                    "BimTools.SpxExporter.About",
                    "About",
                    assemblyPath,
                    typeof(AboutCommand).FullName);
                about.ToolTip = "About the BIM TOOLS SPX Exporter.";
                about.Image = IconFactory.Create("i", Color.FromRgb(105, 65, 198), 16);
                about.LargeImage = IconFactory.Create("i", Color.FromRgb(105, 65, 198), 32);

                panel.AddItem(batch);
                panel.AddStackedItems(active, inspector, data);
                panel.AddSeparator();
                panel.AddStackedItems(logs, about);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show(AppPaths.DialogTitle, "The ribbon could not be created: " + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static void CreateRibbonTabIfMissing(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(AppPaths.TabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                return;
            }
        }

        private static RibbonPanel GetOrCreatePanel(UIControlledApplication application)
        {
            foreach (RibbonPanel existing in application.GetRibbonPanels(AppPaths.TabName))
            {
                if (existing.Name == AppPaths.PanelName)
                {
                    return existing;
                }
            }

            return application.CreateRibbonPanel(AppPaths.TabName, AppPaths.PanelName);
        }
    }
}
