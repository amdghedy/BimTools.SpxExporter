using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace BimTools.SpxExporter.Services
{
    public sealed class DialogSuppressor : IDisposable
    {
        private readonly UIApplication _uiApplication;
        private readonly LogService _log;
        private bool _attached;
        private bool _dialogsHooked;

        public DialogSuppressor(UIApplication uiApplication, LogService log)
        {
            _uiApplication = uiApplication;
            _log = log;
        }

        public void Attach(bool suppressDialogs)
        {
            if (_attached)
            {
                return;
            }

            if (suppressDialogs)
            {
                _uiApplication.DialogBoxShowing += OnDialogBoxShowing;
                _dialogsHooked = true;
            }

            _uiApplication.Application.FailuresProcessing += OnFailuresProcessing;
            _attached = true;
        }

        public void Dispose()
        {
            if (!_attached)
            {
                return;
            }

            if (_dialogsHooked)
            {
                _uiApplication.DialogBoxShowing -= OnDialogBoxShowing;
                _dialogsHooked = false;
            }

            _uiApplication.Application.FailuresProcessing -= OnFailuresProcessing;
            _attached = false;
        }

        private void OnDialogBoxShowing(object sender, DialogBoxShowingEventArgs e)
        {
            TaskDialogShowingEventArgs taskDialog = e as TaskDialogShowingEventArgs;
            if (taskDialog != null)
            {
                _log.Warn("Dismissed task dialog '" + taskDialog.DialogId + "'.");
                e.OverrideResult((int)TaskDialogResult.Close);
                return;
            }

            _log.Warn("Dismissed a Revit dialog box.");
            e.OverrideResult(1);
        }

        private void OnFailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            FailuresAccessor accessor = e.GetFailuresAccessor();
            accessor.DeleteAllWarnings();
            e.SetProcessingResult(FailureProcessingResult.Continue);
        }
    }
}
