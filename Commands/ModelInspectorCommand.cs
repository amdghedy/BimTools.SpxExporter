using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;
using BimTools.SpxExporter.Services;

namespace BimTools.SpxExporter.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public sealed class ModelInspectorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                return BatchWindowLauncher.Run(commandData.Application, ExportEngineKind.ModelInspection);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show(AppPaths.DialogTitle, "The model inspector could not start:" + Environment.NewLine + ex.Message);
                return Result.Failed;
            }
        }
    }
}
