using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;
using BimTools.SpxExporter.Services;

namespace BimTools.SpxExporter.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public sealed class ElementDataCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                return BatchWindowLauncher.Run(commandData.Application, ExportEngineKind.ElementData);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show(AppPaths.DialogTitle, "The element data export could not start:" + Environment.NewLine + ex.Message);
                return Result.Failed;
            }
        }
    }
}
