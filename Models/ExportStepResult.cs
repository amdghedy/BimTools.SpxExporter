using System.Collections.Generic;

namespace BimTools.SpxExporter.Models
{
    public sealed class ExportStepResult
    {
        private ExportStepResult(StepState state, string message, IEnumerable<string> files)
        {
            State = state;
            Message = message ?? string.Empty;
            OutputFiles = files == null ? new List<string>() : new List<string>(files);
        }

        public StepState State { get; }

        public string Message { get; }

        public IList<string> OutputFiles { get; }

        public static ExportStepResult Completed(string message, IEnumerable<string> files)
        {
            return new ExportStepResult(StepState.Completed, message, files);
        }

        public static ExportStepResult Pending(string message)
        {
            return new ExportStepResult(StepState.Pending, message, null);
        }

        public static ExportStepResult Failed(string message)
        {
            return new ExportStepResult(StepState.Failed, message, null);
        }

        public static ExportStepResult Skipped(string message)
        {
            return new ExportStepResult(StepState.Skipped, message, null);
        }
    }
}
