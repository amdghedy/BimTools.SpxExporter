namespace BimTools.SpxExporter.Models
{
    public sealed class ExportJobItem : ObservableObject
    {
        private bool _isSelected = true;
        private ItemStatus _status = ItemStatus.Pending;
        private string _message = string.Empty;
        private string _duration = string.Empty;
        private string _outputFiles = string.Empty;

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public string ModelName { get; set; }

        public string RelativeDirectory { get; set; } = string.Empty;

        public string SizeText { get; set; } = string.Empty;

        public string RevitVersion { get; set; } = string.Empty;

        public bool IsWorkshared { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        public ItemStatus Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }

        public string Message
        {
            get { return _message; }
            set { SetProperty(ref _message, value); }
        }

        public string Duration
        {
            get { return _duration; }
            set { SetProperty(ref _duration, value); }
        }

        public string OutputFiles
        {
            get { return _outputFiles; }
            set { SetProperty(ref _outputFiles, value); }
        }
    }
}
