using System;
using System.IO;
using Autodesk.Revit.DB;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Engines
{
    public sealed class IfcEngine : IExportEngine
    {
        public string DisplayName
        {
            get { return "IFC (open interchange format)"; }
        }

        public string OutputExtension
        {
            get { return ".ifc"; }
        }

        public bool RequiresActiveDocument
        {
            get { return false; }
        }

        public bool WritesPerModelFile
        {
            get { return true; }
        }

        public bool ProducesIntermediateFile
        {
            get { return true; }
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
            IFCExportOptions options = new IFCExportOptions();
            try
            {
                options.FileVersion = MapVersion(context.Settings.IfcVersion);
                options.ExportBaseQuantities = context.Settings.IfcExportBaseQuantities;
                options.WallAndColumnSplitting = context.Settings.IfcSplitWalls;

                bool exported;
                using (Transaction transaction = new Transaction(context.Document, "BIM TOOLS - IFC export"))
                {
                    transaction.Start();
                    exported = context.Document.Export(context.OutputDirectory, context.BaseName, options);
                    transaction.RollBack();
                }

                string file = Path.Combine(context.OutputDirectory, context.BaseName + OutputExtension);
                if (!exported || !File.Exists(file))
                {
                    return ExportStepResult.Failed("The IFC exporter did not produce a file.");
                }

                return ExportStepResult.Completed("IFC exported.", new[] { file });
            }
            catch (Exception ex)
            {
                return ExportStepResult.Failed("IFC export failed: " + ex.Message);
            }
            finally
            {
                Disposer.Release(options);
            }
        }

        public ExportStepResult Poll(ExportContext context)
        {
            return ExportStepResult.Completed("IFC exported.", context.OutputFiles);
        }

        private static IFCVersion MapVersion(IfcVersionOption option)
        {
            switch (option)
            {
                case IfcVersionOption.Ifc2x3:
                    return IFCVersion.IFC2x3;
                case IfcVersionOption.Ifc4:
                    return IFCVersion.IFC4;
                case IfcVersionOption.Ifc4ReferenceView:
                    return IFCVersion.IFC4RV;
                default:
                    return IFCVersion.IFC2x3CV2;
            }
        }
    }
}
