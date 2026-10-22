using System.ComponentModel;
using System.Windows;
using BimTools.SpxExporter.ViewModels;

namespace BimTools.SpxExporter.Views
{
    public partial class ProgressWindow : Window
    {
        private readonly BatchProgressViewModel _viewModel;

        public ProgressWindow(BatchProgressViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = viewModel;
            viewModel.CloseRequested += Close;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_viewModel.IsRunning)
            {
                e.Cancel = true;
                _viewModel.CancelRequested = true;
                _viewModel.StatusText = "Cancelling after the current step...";
            }

            base.OnClosing(e);
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
