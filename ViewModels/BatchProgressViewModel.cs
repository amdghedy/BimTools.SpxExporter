using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;

namespace BimTools.SpxExporter.ViewModels
{
    public sealed class BatchProgressViewModel : ObservableObject
    {
        private const int MaxLogLines = 500;

        private double _progress;
        private string _currentText = "Preparing...";
        private string _statusText = string.Empty;
        private bool _isRunning = true;
        private bool _showContinue;
        private string _continueText = "Continue to next model";
        private bool _cancelRequested;
        private bool _continueRequested;
        private string _reportPath = string.Empty;
        private string _outputFolder = string.Empty;

        public BatchProgressViewModel(IEnumerable<ExportJobItem> items, string title)
        {
            Items = new ObservableCollection<ExportJobItem>(items);
            Title = title;
            LogLines = new ObservableCollection<string>();
            CancelCommand = new RelayCommand(
                delegate { CancelRequested = true; StatusText = "Cancelling after the current step..."; },
                delegate { return IsRunning && !CancelRequested; });
            ContinueCommand = new RelayCommand(
                delegate { _continueRequested = true; },
                delegate { return IsRunning && ShowContinue; });
            OpenReportCommand = new RelayCommand(
                delegate { OpenPath(ReportPath, true); },
                delegate { return !string.IsNullOrEmpty(ReportPath) && File.Exists(ReportPath); });
            OpenOutputCommand = new RelayCommand(
                delegate { OpenPath(OutputFolder, false); },
                delegate { return !string.IsNullOrEmpty(OutputFolder) && Directory.Exists(OutputFolder); });
        }

        public event Action CloseRequested;

        public string Title { get; }

        public ObservableCollection<ExportJobItem> Items { get; }

        public ObservableCollection<string> LogLines { get; }

        public ICommand CancelCommand { get; }

        public ICommand ContinueCommand { get; }

        public ICommand OpenReportCommand { get; }

        public ICommand OpenOutputCommand { get; }

        public double Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
        }

        public string CurrentText
        {
            get { return _currentText; }
            set { SetProperty(ref _currentText, value); }
        }

        public string StatusText
        {
            get { return _statusText; }
            set { SetProperty(ref _statusText, value); }
        }

        public bool IsRunning
        {
            get { return _isRunning; }
            set { SetProperty(ref _isRunning, value); }
        }

        public bool ShowContinue
        {
            get { return _showContinue; }
            set { SetProperty(ref _showContinue, value); }
        }

        public string ContinueText
        {
            get { return _continueText; }
            set { SetProperty(ref _continueText, value); }
        }

        public bool CancelRequested
        {
            get { return _cancelRequested; }
            set { SetProperty(ref _cancelRequested, value); }
        }

        public string ReportPath
        {
            get { return _reportPath; }
            set { SetProperty(ref _reportPath, value); }
        }

        public string OutputFolder
        {
            get { return _outputFolder; }
            set { SetProperty(ref _outputFolder, value); }
        }

        public bool ConsumeContinueRequest()
        {
            bool requested = _continueRequested;
            _continueRequested = false;
            return requested;
        }

        public void AppendLog(string line)
        {
            LogLines.Add(line);
            while (LogLines.Count > MaxLogLines)
            {
                LogLines.RemoveAt(0);
            }
        }

        public void RequestClose()
        {
            Action handler = CloseRequested;
            if (handler != null)
            {
                handler();
            }
        }

        private static void OpenPath(string path, bool selectFile)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string arguments = selectFile ? "/select,\"" + path + "\"" : "\"" + path + "\"";
            Process.Start("explorer.exe", arguments);
        }
    }
}
