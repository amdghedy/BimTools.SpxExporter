using System;
using Autodesk.Revit.UI;

namespace BimTools.SpxExporter.Services
{
    /// <summary>
    /// Runs one queued piece of work in a valid Revit API context (an ExternalEvent), where switching the
    /// active document is permitted. Revit refuses that inside Idling event handlers.
    /// </summary>
    public sealed class WorkExternalEventHandler : IExternalEventHandler
    {
        private Action<UIApplication> _work;

        public bool Completed { get; private set; } = true;

        public Exception Error { get; private set; }

        public void Prepare(Action<UIApplication> work)
        {
            _work = work;
            Error = null;
            Completed = false;
        }

        public void Execute(UIApplication app)
        {
            try
            {
                if (_work != null)
                {
                    _work(app);
                }
            }
            catch (Exception ex)
            {
                // The exception is handed back to the batch runner, which reports it for the current model.
                Error = ex;
            }
            finally
            {
                _work = null;
                Completed = true;
            }
        }

        public string GetName()
        {
            return "BIM TOOLS SPX Exporter batch step";
        }
    }
}
