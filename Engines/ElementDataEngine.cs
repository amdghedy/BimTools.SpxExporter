using System;
using System.Globalization;
using System.IO;
using BimTools.SpxExporter.Models;
using BimTools.SpxExporter.Services;

namespace BimTools.SpxExporter.Engines
{
    public sealed class ElementDataEngine : IExportEngine
    {
        private readonly ElementDataExporter _exporter = new ElementDataExporter();

        public string DisplayName
        {
            get { return "Element data CSV (IDs, categories, levels, phases, parameters)"; }
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
            get { return true; }
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
                string file = Path.Combine(context.OutputDirectory, context.BaseName + OutputExtension);
                int rows = _exporter.Write(context.Document, context.Settings, file);
                return ExportStepResult.Completed(
                    rows.ToString(CultureInfo.InvariantCulture) + " element rows written.",
                    new[] { file });
            }
            catch (Exception ex)
            {
                return ExportStepResult.Failed("Element data export failed: " + ex.Message);
            }
        }

        public ExportStepResult Poll(ExportContext context)
        {
            return ExportStepResult.Completed("Element data exported.", context.OutputFiles);
        }
    }
}
