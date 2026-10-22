using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;
using BimTools.SpxExporter.Services;

namespace BimTools.SpxExporter.ViewModels
{
    public sealed class MainViewModel : ObservableObject
    {
        private readonly UIApplication _uiApp;
        private readonly SettingsService _settingsService;
        private readonly DialogService _dialogs = new DialogService();
        private readonly JobItemFactory _itemFactory = new JobItemFactory();
        private readonly ModelFileScanner _scanner = new ModelFileScanner();
        private readonly JournalCommandScanner _journalScanner = new JournalCommandScanner();
        private readonly SynchroCommandLocator _locator = new SynchroCommandLocator();

        private ExportSettings _settings;
        private bool _includeSubfolders = true;
        private string _statusText = "Add Revit models to begin.";
        private JournalCommand _selectedJournalCommand;
        private string _filterText = string.Empty;
        private string _selectedPreset;
        private bool _suppressPresetLoad;

        public MainViewModel(UIApplication uiApp, ExportSettings settings, SettingsService settingsService)
        {
            _uiApp = uiApp;
            _settingsService = settingsService;
            _settings = settings;

            Items = new ObservableCollection<ExportJobItem>();
            JournalCommands = new ObservableCollection<JournalCommand>();
            PresetNames = new ObservableCollection<string>();
            Items.CollectionChanged += delegate { OnPropertyChanged("SelectedCountText"); };
            ItemsView = CollectionViewSource.GetDefaultView(Items);
            ItemsView.Filter = FilterItem;

            EngineChoices = new[]
            {
                new ChoiceItem<ExportEngineKind>(ExportEngineKind.SynchroAddin, "SPX - direct export with the SYNCHRO 4D Revit exporter"),
                new ChoiceItem<ExportEngineKind>(ExportEngineKind.ElementData, "Element data CSV (IDs, levels, phases, parameters)"),
                new ChoiceItem<ExportEngineKind>(ExportEngineKind.ModelInspection, "Model inspection report (QA only, no export)"),
                new ChoiceItem<ExportEngineKind>(ExportEngineKind.Ifc, "IFC (open format, optional converter)")
            };
            DetachChoices = new[]
            {
                new ChoiceItem<DetachMode>(DetachMode.DetachAndPreserveWorksets, "Detach from central, preserve worksets (recommended)"),
                new ChoiceItem<DetachMode>(DetachMode.DetachAndDiscardWorksets, "Detach from central, discard worksets"),
                new ChoiceItem<DetachMode>(DetachMode.None, "Do not detach (open as-is)")
            };
            WorksetChoices = new[]
            {
                new ChoiceItem<WorksetMode>(WorksetMode.OpenAll, "Open all worksets"),
                new ChoiceItem<WorksetMode>(WorksetMode.OpenLastViewed, "Open last viewed worksets"),
                new ChoiceItem<WorksetMode>(WorksetMode.Filtered, "Open worksets by name filter")
            };
            ViewChoices = new[]
            {
                new ChoiceItem<ViewSelectionMode>(ViewSelectionMode.Default3D, "Default {3D} view (first 3D view as fallback)"),
                new ChoiceItem<ViewSelectionMode>(ViewSelectionMode.ByName, "3D view by name"),
                new ChoiceItem<ViewSelectionMode>(ViewSelectionMode.TemporaryIsometric, "Create a temporary 3D view (projection from the Output tab)")
            };
            OverwriteChoices = new[]
            {
                new ChoiceItem<OverwritePolicy>(OverwritePolicy.Overwrite, "Overwrite existing files"),
                new ChoiceItem<OverwritePolicy>(OverwritePolicy.Skip, "Skip models whose output exists"),
                new ChoiceItem<OverwritePolicy>(OverwritePolicy.AppendTimestamp, "Keep both (append timestamp)")
            };
            CoordinateChoices = new[]
            {
                new ChoiceItem<CoordinateSystemOption>(CoordinateSystemOption.Shared, "Shared coordinates (survey / site coordinates)"),
                new ChoiceItem<CoordinateSystemOption>(CoordinateSystemOption.Project, "Project coordinates (internal origin / project base point)")
            };
            IfcChoices = new[]
            {
                new ChoiceItem<IfcVersionOption>(IfcVersionOption.Ifc2x3CoordinationView2, "IFC 2x3 Coordination View 2.0"),
                new ChoiceItem<IfcVersionOption>(IfcVersionOption.Ifc2x3, "IFC 2x3"),
                new ChoiceItem<IfcVersionOption>(IfcVersionOption.Ifc4, "IFC 4"),
                new ChoiceItem<IfcVersionOption>(IfcVersionOption.Ifc4ReferenceView, "IFC 4 Reference View")
            };

            AddFilesCommand = new RelayCommand(delegate { AddFiles(); });
            AddFolderCommand = new RelayCommand(delegate { AddFolder(); });
            AddActiveCommand = new RelayCommand(delegate { AddActiveModel(); });
            AutoFolderCommand = new RelayCommand(delegate { AutoFolder(); });
            RemoveSelectedCommand = new RelayCommand(RemoveSelected);
            ClearCommand = new RelayCommand(delegate { Items.Clear(); StatusText = "List cleared."; }, delegate { return Items.Count > 0; });
            SelectAllCommand = new RelayCommand(delegate { SetAllSelected(true); });
            SelectNoneCommand = new RelayCommand(delegate { SetAllSelected(false); });
            BrowseOutputCommand = new RelayCommand(delegate { BrowseOutput(); });
            BrowseWatchCommand = new RelayCommand(delegate { BrowseWatchFolder(); });
            BrowseExecutableCommand = new RelayCommand(delegate { BrowseExecutable(); });
            ScanJournalsCommand = new RelayCommand(delegate { ScanJournals(); });
            AutoDetectCommand = new RelayCommand(delegate { AutoDetectSynchro(); });
            TestCommandIdCommand = new RelayCommand(delegate { TestCommandId(); });
            ImportSettingsCommand = new RelayCommand(delegate { ImportSettings(); });
            ExportSettingsCommand = new RelayCommand(delegate { ExportSettingsToFile(); });
            ResetSettingsCommand = new RelayCommand(delegate { ResetSettings(); });
            MoveUpCommand = new RelayCommand(delegate (object p) { MoveSelected(p, -1); });
            MoveDownCommand = new RelayCommand(delegate (object p) { MoveSelected(p, 1); });
            SortCommand = new RelayCommand(delegate { SortItems(); }, delegate { return Items.Count > 1; });
            ImportListCommand = new RelayCommand(delegate { ImportList(); });
            ExportListCommand = new RelayCommand(delegate { ExportList(); }, delegate { return Items.Count > 0; });
            ShowInFolderCommand = new RelayCommand(ShowInFolder);
            PreflightCommand = new RelayCommand(delegate { Preflight(); });
            SavePresetCommand = new RelayCommand(delegate { SavePreset(); });
            BrowseCopyFolderCommand = new RelayCommand(delegate { BrowseCopyFolder(); });
            BrowsePostBatchCommand = new RelayCommand(delegate { BrowsePostBatch(); });
            StartCommand = new RelayCommand(delegate { Start(); });
            CancelCommand = new RelayCommand(delegate { RaiseClose(false); });

            HookSettings(_settings);
            RefreshPresets();
        }

        public event Action<bool> CloseRequested;

        public ObservableCollection<ExportJobItem> Items { get; }

        public ObservableCollection<JournalCommand> JournalCommands { get; }

        public IList<ChoiceItem<ExportEngineKind>> EngineChoices { get; }

        public IList<ChoiceItem<DetachMode>> DetachChoices { get; }

        public IList<ChoiceItem<WorksetMode>> WorksetChoices { get; }

        public IList<ChoiceItem<ViewSelectionMode>> ViewChoices { get; }

        public IList<ChoiceItem<OverwritePolicy>> OverwriteChoices { get; }

        public IList<ChoiceItem<CoordinateSystemOption>> CoordinateChoices { get; }

        public IList<ChoiceItem<IfcVersionOption>> IfcChoices { get; }

        public IList<ChoiceItem<DetailLevelOption>> DetailChoices { get; }

        public IList<ChoiceItem<ViewProjectionOption>> ProjectionChoices { get; }

        public ICommand AutoFolderCommand { get; }

        public ObservableCollection<string> PresetNames { get; }

        public ICollectionView ItemsView { get; }

        public ICommand MoveUpCommand { get; }

        public ICommand MoveDownCommand { get; }

        public ICommand SortCommand { get; }

        public ICommand ImportListCommand { get; }

        public ICommand ExportListCommand { get; }

        public ICommand ShowInFolderCommand { get; }

        public ICommand PreflightCommand { get; }

        public ICommand SavePresetCommand { get; }

        public ICommand BrowseCopyFolderCommand { get; }

        public ICommand BrowsePostBatchCommand { get; }

        public ICommand AddFilesCommand { get; }

        public ICommand AddFolderCommand { get; }

        public ICommand AddActiveCommand { get; }

        public ICommand RemoveSelectedCommand { get; }

        public ICommand ClearCommand { get; }

        public ICommand SelectAllCommand { get; }

        public ICommand SelectNoneCommand { get; }

        public ICommand BrowseOutputCommand { get; }

        public ICommand BrowseWatchCommand { get; }

        public ICommand BrowseExecutableCommand { get; }

        public ICommand ScanJournalsCommand { get; }

        public ICommand AutoDetectCommand { get; }

        public ICommand TestCommandIdCommand { get; }

        public ICommand ImportSettingsCommand { get; }

        public ICommand ExportSettingsCommand { get; }

        public ICommand ResetSettingsCommand { get; }

        public ICommand StartCommand { get; }

        public ICommand CancelCommand { get; }

        public ExportSettings Settings
        {
            get { return _settings; }
            private set
            {
                if (SetProperty(ref _settings, value))
                {
                    HookSettings(value);
                    OnPropertyChanged("IsIfcEngine");
                    OnPropertyChanged("IsSynchroEngine");
                    OnPropertyChanged("IsConverterAvailable");
                    OnPropertyChanged("IsWorksetFilterVisible");
                    OnPropertyChanged("IsViewNameVisible");
                }
            }
        }

        public bool IncludeSubfolders
        {
            get { return _includeSubfolders; }
            set { SetProperty(ref _includeSubfolders, value); }
        }

        public string StatusText
        {
            get { return _statusText; }
            set { SetProperty(ref _statusText, value); }
        }

        public string FilterText
        {
            get { return _filterText; }
            set
            {
                if (SetProperty(ref _filterText, value ?? string.Empty))
                {
                    ItemsView.Refresh();
                }
            }
        }

        public string SelectedPreset
        {
            get { return _selectedPreset; }
            set
            {
                if (SetProperty(ref _selectedPreset, value) && !_suppressPresetLoad && !string.IsNullOrEmpty(value))
                {
                    LoadPreset(value);
                }
            }
        }

        public JournalCommand SelectedJournalCommand
        {
            get { return _selectedJournalCommand; }
            set
            {
                if (SetProperty(ref _selectedJournalCommand, value) && value != null)
                {
                    Settings.SynchroCommandId = value.CommandId;
                }
            }
        }

        public bool IsIfcEngine
        {
            get { return Settings.Engine == ExportEngineKind.Ifc; }
        }

        public bool IsSynchroEngine
        {
            get { return Settings.Engine == ExportEngineKind.SynchroAddin; }
        }

        public bool IsConverterAvailable
        {
            get { return Settings.Engine == ExportEngineKind.Ifc; }
        }

        public bool IsWorksetFilterVisible
        {
            get { return Settings.WorksetMode == WorksetMode.Filtered; }
        }

        public bool IsViewNameVisible
        {
            get { return Settings.ViewMode == ViewSelectionMode.ByName; }
        }

        public string SelectedCountText
        {
            get
            {
                int selected = Items.Count(i => i.IsSelected);
                return selected.ToString() + " of " + Items.Count.ToString() + " model(s) selected";
            }
        }

        public IList<ExportJobItem> GetSelectedItems()
        {
            return Items.Where(i => i.IsSelected).ToList();
        }

        public void AddExternalModel(string path)
        {
            AddPaths(new[] { path }, null);
        }

        private void HookSettings(ExportSettings settings)
        {
            settings.PropertyChanged += OnSettingsPropertyChanged;
        }

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Engine")
            {
                OnPropertyChanged("IsIfcEngine");
                OnPropertyChanged("IsSynchroEngine");
                OnPropertyChanged("IsConverterAvailable");
            }
            else if (e.PropertyName == "WorksetMode")
            {
                OnPropertyChanged("IsWorksetFilterVisible");
            }
            else if (e.PropertyName == "ViewMode")
            {
                OnPropertyChanged("IsViewNameVisible");
            }
        }

        private void AddFiles()
        {
            AddPaths(_dialogs.PickModels(), null);
        }

        private void AddFolder()
        {
            string folder = _dialogs.PickFolder("Select a folder containing Revit models", Settings.OutputFolder);
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            IList<string> files = _scanner.FindModels(folder, IncludeSubfolders, Settings.ExcludeFilePatterns);
            AddPaths(files, folder);
        }

        private void AutoFolder()
        {
            string folder = _dialogs.PickFolder("Select the root folder. Every Revit model in it and its sub-folders will be exported beside its original file.", null);
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            IList<string> files = _scanner.FindModels(folder, true, Settings.ExcludeFilePatterns);
            AddPaths(files, folder);
            Settings.OutputToSourceFolder = true;
            Settings.OverwritePolicy = OverwritePolicy.Overwrite;
            Settings.AutoAnswerDialogs = true;
            Settings.SuppressDialogs = true;
            Settings.ContinueOnError = true;
            StatusText = files.Count.ToString() + " model(s) found in the folder tree. Output goes beside each original. Review the settings, then press Start export.";
        }

        private void AddActiveModel()
        {
            UIDocument active = _uiApp.ActiveUIDocument;
            if (active == null || string.IsNullOrEmpty(active.Document.PathName))
            {
                StatusText = "There is no saved active model to add.";
                return;
            }

            AddPaths(new[] { active.Document.PathName }, null);
        }

        private void AddPaths(IEnumerable<string> paths, string rootFolder)
        {
            HashSet<string> known = new HashSet<string>(Items.Select(i => i.FilePath), StringComparer.OrdinalIgnoreCase);
            int added = 0;
            foreach (string path in paths)
            {
                if (known.Contains(path) || ModelFileScanner.IsBackup(path))
                {
                    continue;
                }

                Items.Add(_itemFactory.Create(path, rootFolder));
                known.Add(path);
                added++;
            }

            StatusText = added.ToString() + " model(s) added.";
            OnPropertyChanged("SelectedCountText");
        }

        private void RemoveSelected(object parameter)
        {
            IList selection = parameter as IList;
            if (selection == null)
            {
                return;
            }

            List<ExportJobItem> toRemove = selection.Cast<ExportJobItem>().ToList();
            foreach (ExportJobItem item in toRemove)
            {
                Items.Remove(item);
            }

            StatusText = toRemove.Count.ToString() + " model(s) removed.";
        }

        private void SetAllSelected(bool selected)
        {
            foreach (ExportJobItem item in Items)
            {
                item.IsSelected = selected;
            }

            OnPropertyChanged("SelectedCountText");
        }

        private void BrowseOutput()
        {
            string folder = _dialogs.PickFolder("Select the output folder", Settings.OutputFolder);
            if (!string.IsNullOrEmpty(folder))
            {
                Settings.OutputFolder = folder;
            }
        }

        private void BrowseWatchFolder()
        {
            string folder = _dialogs.PickFolder("Select the folder where the SYNCHRO exporter writes SPX files", Settings.SynchroWatchFolder);
            if (!string.IsNullOrEmpty(folder))
            {
                Settings.SynchroWatchFolder = folder;
            }
        }

        private void BrowseExecutable()
        {
            string path = _dialogs.PickExecutable();
            if (!string.IsNullOrEmpty(path))
            {
                Settings.PostProcessExecutable = path;
            }
        }

        private void ScanJournals()
        {
            string journal = _uiApp.Application.RecordingJournalFilename;
            string folder = string.IsNullOrEmpty(journal) ? null : Path.GetDirectoryName(journal);
            IList<JournalCommand> commands = _journalScanner.Scan(folder, 20);

            JournalCommands.Clear();
            foreach (JournalCommand command in commands)
            {
                JournalCommands.Add(command);
            }

            StatusText = commands.Count == 0
                ? "No external commands found in the journals. Click the SYNCHRO export button once in Revit, then scan again."
                : commands.Count.ToString() + " external command(s) found. Pick the SYNCHRO exporter from the list.";
        }

        private void AutoDetectSynchro()
        {
            string journal = _uiApp.Application.RecordingJournalFilename;
            string folder = string.IsNullOrEmpty(journal) ? null : Path.GetDirectoryName(journal);
            string explanation;
            string id = _locator.FindCommandId(folder, out explanation);
            if (id != null)
            {
                Settings.SynchroCommandId = id;
            }

            StatusText = explanation;
        }

        private void TestCommandId()
        {
            if (string.IsNullOrWhiteSpace(Settings.SynchroCommandId))
            {
                TaskDialog.Show(AppPaths.DialogTitle, "Enter or pick a command id first.");
                return;
            }

            RevitCommandId id = RevitCommandId.LookupCommandId(Settings.SynchroCommandId);
            TaskDialog.Show(
                AppPaths.DialogTitle,
                id == null
                    ? "Revit does not recognise this command id."
                    : "The command id is valid and can be launched by the batch.");
        }

        private void ImportSettings()
        {
            string path = _dialogs.PickSettingsToOpen();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                Settings = _settingsService.Import(path);
                StatusText = "Preset imported.";
            }
            catch (Exception ex)
            {
                TaskDialog.Show(AppPaths.DialogTitle, "The preset could not be imported: " + ex.Message);
            }
        }

        private void ExportSettingsToFile()
        {
            string path = _dialogs.PickSettingsToSave(AppPaths.PresetsFolder);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                _settingsService.Export(Settings, path);
                RefreshPresets();
                StatusText = "Preset saved.";
            }
            catch (Exception ex)
            {
                TaskDialog.Show(AppPaths.DialogTitle, "The preset could not be saved: " + ex.Message);
            }
        }

        private void ResetSettings()
        {
            Settings = new ExportSettings();
            StatusText = "Settings reset to defaults.";
        }

        private bool FilterItem(object candidate)
        {
            ExportJobItem item = candidate as ExportJobItem;
            if (item == null || string.IsNullOrWhiteSpace(FilterText))
            {
                return true;
            }

            return item.FilePath.IndexOf(FilterText.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void MoveSelected(object parameter, int direction)
        {
            IList selection = parameter as IList;
            if (selection == null || selection.Count == 0)
            {
                return;
            }

            List<int> indexes = selection.Cast<ExportJobItem>().Select(i => Items.IndexOf(i)).Where(i => i >= 0).ToList();
            indexes.Sort();
            if (direction > 0)
            {
                indexes.Reverse();
            }

            foreach (int index in indexes)
            {
                int target = index + direction;
                if (target < 0 || target >= Items.Count)
                {
                    break;
                }

                Items.Move(index, target);
            }
        }

        private void SortItems()
        {
            List<ExportJobItem> sorted = Items.OrderBy(i => i.FilePath, StringComparer.OrdinalIgnoreCase).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                Items.Move(Items.IndexOf(sorted[i]), i);
            }

            StatusText = "Sorted by path.";
        }

        private void ImportList()
        {
            string path = _dialogs.PickListToOpen();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            List<string> paths = new List<string>();
            foreach (string line in File.ReadAllLines(path))
            {
                string candidate = line.Trim().Trim('"');
                if (candidate.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                {
                    paths.Add(candidate);
                }
            }

            AddPaths(paths, null);
        }

        private void ExportList()
        {
            string path = _dialogs.PickListToSave();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            File.WriteAllLines(path, Items.Select(i => i.FilePath).ToArray(), new UTF8Encoding(false));
            StatusText = "List saved.";
        }

        private void ShowInFolder(object parameter)
        {
            ExportJobItem item = parameter as ExportJobItem;
            if (item != null && File.Exists(item.FilePath))
            {
                Process.Start("explorer.exe", "/select,\"" + item.FilePath + "\"");
            }
        }

        private void Preflight()
        {
            List<string> issues = new List<string>();
            List<ExportJobItem> selected = Items.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0)
            {
                TaskDialog.Show(AppPaths.DialogTitle, "No models are selected.");
                return;
            }

            foreach (ExportJobItem item in selected)
            {
                if (!File.Exists(item.FilePath))
                {
                    issues.Add(item.FileName + ": file not found.");
                    continue;
                }

                int year;
                if (int.TryParse(item.RevitVersion, out year) && year > AppPaths.RevitYear)
                {
                    issues.Add(item.FileName + ": saved in Revit " + year + ", newer than Revit " + AppPaths.RevitYear + ".");
                }
                else if (item.RevitVersion == "?")
                {
                    issues.Add(item.FileName + ": file information could not be read.");
                }

                if (item.IsWorkshared && Settings.DetachMode == DetachMode.None)
                {
                    issues.Add(item.FileName + ": workshared model will be opened without detaching.");
                }
            }

            string outputFolder = Settings.OutputToSourceFolder ? null : Settings.OutputFolder;
            if (!string.IsNullOrWhiteSpace(outputFolder))
            {
                try
                {
                    Directory.CreateDirectory(outputFolder);
                    string probe = Path.Combine(outputFolder, ".bimtools_write_test");
                    File.WriteAllText(probe, "x");
                    File.Delete(probe);

                    DriveInfo drive = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(outputFolder)));
                    long freeMb = drive.AvailableFreeSpace / 1048576;
                    if (freeMb < 2048)
                    {
                        issues.Add("Only " + freeMb + " MB free on the output drive.");
                    }
                }
                catch (Exception ex)
                {
                    if (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
                    {
                        issues.Add("The output folder is not writable: " + ex.Message);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            string verdict = Engines.EngineFactory.Create(Settings.Engine).Validate(Settings);
            if (verdict != null)
            {
                issues.Add(verdict);
            }

            string text = issues.Count == 0
                ? selected.Count + " model(s) passed the pre-flight checks."
                : string.Join(Environment.NewLine, issues.Take(20)) + (issues.Count > 20 ? Environment.NewLine + "...and " + (issues.Count - 20) + " more." : string.Empty);
            TaskDialog.Show(AppPaths.DialogTitle, text);
        }

        private void RefreshPresets()
        {
            _suppressPresetLoad = true;
            try
            {
                PresetNames.Clear();
                foreach (string name in _settingsService.ListPresets())
                {
                    PresetNames.Add(name);
                }
            }
            finally
            {
                _suppressPresetLoad = false;
            }
        }

        private void LoadPreset(string name)
        {
            try
            {
                Settings = _settingsService.Import(_settingsService.PresetPath(name));
                StatusText = "Preset '" + name + "' loaded.";
            }
            catch (Exception ex)
            {
                TaskDialog.Show(AppPaths.DialogTitle, "The preset could not be loaded: " + ex.Message);
            }
        }

        private void SavePreset()
        {
            ExportSettingsToFile();
        }

        private void BrowseCopyFolder()
        {
            string folder = _dialogs.PickFolder("Select the secondary copy folder", Settings.SecondaryCopyFolder);
            if (!string.IsNullOrEmpty(folder))
            {
                Settings.SecondaryCopyFolder = folder;
            }
        }

        private void BrowsePostBatch()
        {
            string path = _dialogs.PickExecutable();
            if (!string.IsNullOrEmpty(path))
            {
                Settings.RunAfterBatchExecutable = path;
            }
        }

        private void Start()
        {
            string problem = Validate();
            if (problem != null)
            {
                TaskDialog.Show(AppPaths.DialogTitle, problem);
                return;
            }

            RaiseClose(true);
        }

        private string Validate()
        {
            if (!Items.Any(i => i.IsSelected))
            {
                return "Select at least one model to export.";
            }

            if (!Settings.OutputToSourceFolder && string.IsNullOrWhiteSpace(Settings.OutputFolder))
            {
                return "Choose an output folder or enable 'save next to each source model'.";
            }

            if (Settings.RetryCount < 0 || Settings.RetryCount > 5)
            {
                return "Retry count must be between 0 and 5.";
            }

            if (Settings.PostProcessEnabled && IsConverterAvailable)
            {
                if (string.IsNullOrWhiteSpace(Settings.PostProcessExecutable) || !File.Exists(Settings.PostProcessExecutable))
                {
                    return "The post-processing converter executable was not found.";
                }
            }

            if (Settings.Engine == ExportEngineKind.SynchroAddin && _uiApp.ActiveUIDocument != null && string.IsNullOrEmpty(_uiApp.ActiveUIDocument.Document.PathName))
            {
                return "Save the active model first so it can be restored after the SYNCHRO batch.";
            }

            if (Settings.ScheduleEnabled)
            {
                TimeSpan scheduled;
                if (!TimeSpan.TryParse(Settings.ScheduledStart, System.Globalization.CultureInfo.InvariantCulture, out scheduled) || scheduled < TimeSpan.Zero || scheduled >= TimeSpan.FromHours(24))
                {
                    return "Enter the scheduled start time as HH:mm, for example 22:30.";
                }
            }

            if (!string.IsNullOrEmpty(Settings.RunAfterBatchExecutable) && !File.Exists(Settings.RunAfterBatchExecutable))
            {
                return "The post-batch program was not found.";
            }

            if (Settings.Engine == ExportEngineKind.SynchroAddin && string.IsNullOrWhiteSpace(Settings.SynchroCommandId))
            {
                AutoDetectSynchro();
            }

            return Engines.EngineFactory.Create(Settings.Engine).Validate(Settings);
        }

        private void RaiseClose(bool accepted)
        {
            Action<bool> handler = CloseRequested;
            if (handler != null)
            {
                handler(accepted);
            }
        }
    }
}
