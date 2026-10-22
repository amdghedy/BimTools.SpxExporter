using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Engines;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;
using BimTools.SpxExporter.Services;

namespace BimTools.SpxExporter.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public sealed class ExportActiveModelCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument activeDocument = uiApp.ActiveUIDocument;
                if (activeDocument == null || string.IsNullOrEmpty(activeDocument.Document.PathName))
                {
                    TaskDialog.Show(AppPaths.DialogTitle, "Open and save a Revit model first.");
                    return Result.Cancelled;
                }

                SettingsService settingsService = new SettingsService();
                ExportSettings settings = settingsService.Load();
                if (!settings.OutputToSourceFolder && string.IsNullOrWhiteSpace(settings.OutputFolder))
                {
                    settings.OutputToSourceFolder = true;
                }

                string problem = EngineFactory.Create(settings.Engine).Validate(settings);
                if (problem != null)
                {
                    TaskDialog.Show(AppPaths.DialogTitle, problem + Environment.NewLine + Environment.NewLine + "Open Batch SPX Export to adjust the settings.");
                    return Result.Cancelled;
                }

                ExportJobItem item = new JobItemFactory().Create(activeDocument.Document.PathName, null);
                BatchLauncher.Launch(uiApp, settings, new List<ExportJobItem> { item });
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show(AppPaths.DialogTitle, "The export could not start:" + Environment.NewLine + ex.Message);
                return Result.Failed;
            }
        }
    }
}
