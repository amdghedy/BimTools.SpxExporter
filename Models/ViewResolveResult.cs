using Autodesk.Revit.DB;

namespace BimTools.SpxExporter.Models
{
    public sealed class ViewResolveResult
    {
        public View3D View { get; set; }

        public bool CreatedTemporary { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
