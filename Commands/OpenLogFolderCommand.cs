using System;
using System.Diagnostics;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Infrastructure;

namespace BimTools.SpxExporter.Commands
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public sealed class OpenLogFolderCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Process.Start("explorer.exe", "\"" + AppPaths.LogFolder + "\"");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show(AppPaths.DialogTitle, "The log folder could not be opened:" + Environment.NewLine + ex.Message);
                return Result.Failed;
            }
        }
    }
}
