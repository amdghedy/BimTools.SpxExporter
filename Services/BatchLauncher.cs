using System;
using System.Collections.Generic;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;
using BimTools.SpxExporter.ViewModels;
using BimTools.SpxExporter.Views;

namespace BimTools.SpxExporter.Services
{
    public static class BatchLauncher
    {
        public static void Launch(UIApplication uiApp, ExportSettings settings, IList<ExportJobItem> items)
        {
            if (BatchRunner.IsRunning)
            {
                TaskDialog.Show(AppPaths.DialogTitle, "A batch export is already running.");
                return;
            }

            LogService log = LogService.CreateNew();
            string title = "SPX Export - " + items.Count + " model(s)";
            BatchProgressViewModel viewModel = new BatchProgressViewModel(items, title);

            ProgressWindow window = new ProgressWindow(viewModel);
            log.LineWritten += delegate (string line)
            {
                window.Dispatcher.BeginInvoke(new Action<string>(viewModel.AppendLog), line);
            };

            WindowInteropHelper helper = new WindowInteropHelper(window);
            helper.Owner = uiApp.MainWindowHandle;
            window.Show();

            BatchRunner runner = new BatchRunner(uiApp, settings, items, viewModel, log);
            runner.RegisterOwnWindow(helper.Handle);
            runner.Start();
        }
    }
}
