using System.Windows.Interop;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;
using BimTools.SpxExporter.ViewModels;
using BimTools.SpxExporter.Views;

namespace BimTools.SpxExporter.Services
{
    public static class BatchWindowLauncher
    {
        public static Result Run(UIApplication uiApp, ExportEngineKind? forcedEngine)
        {
            if (BatchRunner.IsRunning)
            {
                TaskDialog.Show(AppPaths.DialogTitle, "A batch export is already running.");
                return Result.Cancelled;
            }

            SettingsService settingsService = new SettingsService();
            ExportSettings settings = settingsService.Load();
            if (forcedEngine.HasValue)
            {
                settings.Engine = forcedEngine.Value;
            }

            MainViewModel viewModel = new MainViewModel(uiApp, settings, settingsService);
            MainWindow window = new MainWindow(viewModel);
            WindowInteropHelper helper = new WindowInteropHelper(window);
            helper.Owner = uiApp.MainWindowHandle;

            bool? accepted = window.ShowDialog();
            if (!forcedEngine.HasValue)
            {
                settingsService.Save(viewModel.Settings);
            }

            if (accepted != true)
            {
                return Result.Cancelled;
            }

            BatchLauncher.Launch(uiApp, viewModel.Settings, viewModel.GetSelectedItems());
            return Result.Succeeded;
        }
    }
}
