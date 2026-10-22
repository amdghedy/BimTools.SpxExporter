using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Infrastructure;

namespace BimTools.SpxExporter.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public sealed class AboutCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                TaskDialog dialog = new TaskDialog(AppPaths.DialogTitle);
                dialog.MainInstruction = "BIM TOOLS SPX Exporter 1.3.0";
                dialog.MainContent =
                    "Batch export of Revit 2024 models for SYNCHRO 4D." + Environment.NewLine + Environment.NewLine +
                    "Direct SPX export through the SYNCHRO 4D Revit exporter, plus element data CSV, model inspection and IFC." + Environment.NewLine +
                    Environment.NewLine +
                    "Settings and logs: %AppData%\\BimTools\\SpxExporter";
                dialog.CommonButtons = TaskDialogCommonButtons.Close;
                dialog.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
