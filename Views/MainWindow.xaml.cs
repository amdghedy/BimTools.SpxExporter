using System.Windows;
using BimTools.SpxExporter.ViewModels;

namespace BimTools.SpxExporter.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseRequested += OnCloseRequested;
        }

        private void OnCloseRequested(bool accepted)
        {
            DialogResult = accepted;
        }
    }
}
