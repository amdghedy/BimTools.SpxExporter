using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Engines
{
    public static class EngineFactory
    {
        public static IExportEngine Create(ExportEngineKind kind)
        {
            switch (kind)
            {
                case ExportEngineKind.Ifc:
                    return new IfcEngine();
                case ExportEngineKind.ModelInspection:
                    return new ModelInspectionEngine();
                case ExportEngineKind.ElementData:
                    return new ElementDataEngine();
                default:
                    return new SynchroAddinEngine();
            }
        }
    }
}
