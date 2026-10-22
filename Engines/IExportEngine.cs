using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.Engines
{
    public interface IExportEngine
    {
        string DisplayName { get; }

        string OutputExtension { get; }

        bool RequiresActiveDocument { get; }

        bool WritesPerModelFile { get; }

        bool ProducesIntermediateFile { get; }

        bool NeedsView(ExportSettings settings);

        string Validate(ExportSettings settings);

        ExportStepResult Begin(ExportContext context);

        ExportStepResult Poll(ExportContext context);
    }
}
