using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using BimTools.SpxExporter.Engines;
using BimTools.SpxExporter.Infrastructure;
using BimTools.SpxExporter.Models;
using BimTools.SpxExporter.ViewModels;

namespace BimTools.SpxExporter.Services
{
    public sealed class BatchRunner
    {
        private const int MaxActivationTicks = 200;

        private readonly UIApplication _uiApp;
        private readonly ExportSettings _settings;
        private readonly IList<ExportJobItem> _items;
        private readonly BatchProgressViewModel _viewModel;
        private readonly LogService _log;
        private readonly IExportEngine _engine;
        private readonly ModelOpenService _opener = new ModelOpenService();
        private readonly ViewResolverService _viewResolver = new ViewResolverService();
        private readonly FileNamingService _naming = new FileNamingService();
        private readonly PostProcessService _postProcess = new PostProcessService();
        private readonly ReportService _report = new ReportService();
        private readonly Stopwatch _batchClock = Stopwatch.StartNew();
        private readonly Queue<string> _pendingConversions = new Queue<string>();
        private readonly List<string> _finalOutputs = new List<string>();
        private readonly List<string> _intermediateFiles = new List<string>();
        private readonly StringBuilder _processOutput = new StringBuilder();
        private readonly List<string> _extraOutputs = new List<string>();
        private readonly List<string> _allOutputs = new List<string>();
        private readonly ViewPreparationService _viewPreparation = new ViewPreparationService();
        private readonly ElementDataExporter _elementData = new ElementDataExporter();
        private readonly ArchiveService _archive = new ArchiveService();

        private DialogSuppressor _suppressor;
        private DialogAutomationService _dialogAutomation;
        private readonly List<int> _ownWindows = new List<int>();
        private RunnerPhase _phase = RunnerPhase.NextItem;
        private int _index;
        private int _attempt;
        private int _waitTicks;
        private ExportJobItem _current;
        private ExportContext _context;
        private Document _document;
        private Document _deferredClose;
        private bool _openedByUs;
        private string _originalPath = string.Empty;
        private Stopwatch _itemClock;
        private Process _process;
        private string _processTarget = string.Empty;
        private DateTime _processStartedUtc;
        private bool _finished;
        private bool _batchEnding;
        private WorkExternalEventHandler _workHandler;
        private ExternalEvent _externalEvent;
        private Action _afterEvent;
        private Stopwatch _eventClock;
        private ExportStepResult _stepFromEvent;
        private View _previousView;
        private DateTime _scheduledAt;

        public BatchRunner(
            UIApplication uiApp,
            ExportSettings settings,
            IList<ExportJobItem> items,
            BatchProgressViewModel viewModel,
            LogService log)
        {
            _uiApp = uiApp;
            _settings = settings;
            _items = items;
            _viewModel = viewModel;
            _log = log;
            _engine = EngineFactory.Create(settings.Engine);
        }

        public static bool IsRunning { get; private set; }

        public void RegisterOwnWindow(IntPtr handle)
        {
            _ownWindows.Add((int)handle.ToInt64());
        }

        public void Start()
        {
            IsRunning = true;
            _workHandler = new WorkExternalEventHandler();
            _externalEvent = ExternalEvent.Create(_workHandler);

            UIDocument active = _uiApp.ActiveUIDocument;
            if (active != null)
            {
                _originalPath = active.Document.PathName ?? string.Empty;
            }

            _log.Info("Batch started: " + _items.Count.ToString(CultureInfo.InvariantCulture) + " model(s), engine = " + _engine.DisplayName + ".");

            if (_settings.SuppressDialogs)
            {
                _suppressor = new DialogSuppressor(_uiApp, _log);
                _suppressor.Attach(!_engine.RequiresActiveDocument);
            }

            if (_engine.RequiresActiveDocument && _settings.AutoAnswerDialogs)
            {
                _dialogAutomation = new DialogAutomationService(
                    Process.GetCurrentProcess().Id,
                    (int)_uiApp.MainWindowHandle.ToInt64(),
                    _settings,
                    _log);
                foreach (int handle in _ownWindows)
                {
                    _dialogAutomation.ExcludeWindow(handle);
                }

                _dialogAutomation.Start();
                _log.Info("Dialog automation is active: save and select prompts of the exporter are answered automatically.");
            }

            if (_settings.ScheduleEnabled)
            {
                _scheduledAt = NextOccurrence(_settings.ScheduledStart);
                _phase = RunnerPhase.WaitingForStart;
                _viewModel.ContinueText = "Start now";
                _viewModel.ShowContinue = true;
                _log.Info("Batch scheduled for " + _scheduledAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + ".");
            }

            _uiApp.Idling += OnIdling;
        }

        public static DateTime NextOccurrence(string timeText)
        {
            TimeSpan time;
            if (!TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out time) || time < TimeSpan.Zero || time >= TimeSpan.FromHours(24))
            {
                return DateTime.Now;
            }

            DateTime candidate = DateTime.Today + time;
            return candidate <= DateTime.Now ? candidate.AddDays(1) : candidate;
        }

        private void OnIdling(object sender, IdlingEventArgs e)
        {
            if (_finished)
            {
                return;
            }

            try
            {
                Tick();
            }
            catch (Exception ex)
            {
                _log.Error("Unexpected error: " + ex);
                if (_current != null && _current.Status == ItemStatus.Running)
                {
                    CompleteItem(ItemStatus.Failed, ex.Message);
                }
                else
                {
                    FinishBatch();
                }
            }

            if (!_finished && _phase != RunnerPhase.Polling && _phase != RunnerPhase.PostProcess && _phase != RunnerPhase.WaitingForStart && _phase != RunnerPhase.WaitingForEvent)
            {
                e.SetRaiseWithoutDelay();
            }
        }

        private void Tick()
        {
            if (_viewModel.CancelRequested && _current != null && _current.Status == ItemStatus.Running && _phase != RunnerPhase.NextItem && _phase != RunnerPhase.Restoring && _phase != RunnerPhase.WaitingForEvent)
            {
                StopProcess();
                CompleteItem(ItemStatus.Cancelled, "Cancelled by user.");
                return;
            }

            switch (_phase)
            {
                case RunnerPhase.WaitingForStart:
                    WaitForStart();
                    break;
                case RunnerPhase.WaitingForEvent:
                    WaitForEvent();
                    break;
                case RunnerPhase.NextItem:
                    NextItem();
                    break;
                case RunnerPhase.AwaitActivation:
                    AwaitActivation();
                    break;
                case RunnerPhase.Export:
                    RunExport();
                    break;
                case RunnerPhase.Polling:
                    PollEngine();
                    break;
                case RunnerPhase.PostProcess:
                    PollConversion();
                    break;
                case RunnerPhase.Restoring:
                    RestoreOriginal();
                    break;
            }
        }

        private void WaitForStart()
        {
            if (_viewModel.CancelRequested)
            {
                _viewModel.ShowContinue = false;
                MarkRemainingCancelled();
                FinishBatch();
                return;
            }

            bool startNow = _viewModel.ConsumeContinueRequest();
            TimeSpan remaining = _scheduledAt - DateTime.Now;
            if (startNow || remaining <= TimeSpan.Zero)
            {
                _viewModel.ShowContinue = false;
                _viewModel.ContinueText = "Continue to next model";
                _log.Info("Scheduled start reached.");
                _phase = RunnerPhase.NextItem;
                return;
            }

            _viewModel.CurrentText = "Waiting for scheduled start at " + _scheduledAt.ToString("HH:mm", CultureInfo.InvariantCulture);
            _viewModel.StatusText = "Starting in " + remaining.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture) + ". Keep Revit open and the computer awake.";
        }

        private void NextItem()
        {
            if (_viewModel.CancelRequested)
            {
                MarkRemainingCancelled();
                FinishBatch();
                return;
            }

            while (_index < _items.Count && !_items[_index].IsSelected)
            {
                _index++;
            }

            if (_index >= _items.Count)
            {
                FinishBatch();
                return;
            }

            BeginItem();
        }

        private void BeginItem()
        {
            _current = _items[_index];
            _attempt = 0;
            _itemClock = Stopwatch.StartNew();
            _finalOutputs.Clear();
            _intermediateFiles.Clear();
            _extraOutputs.Clear();
            _pendingConversions.Clear();
            _previousView = null;

            int position = _index + 1;
            _viewModel.CurrentText = string.Format(CultureInfo.InvariantCulture, "[{0}/{1}] {2}", position, _items.Count, _current.FileName);
            SetStatus(ItemStatus.Running, "Opening model...");
            _log.Info("Processing " + _current.FilePath);

            if (!File.Exists(_current.FilePath))
            {
                CompleteItem(ItemStatus.Failed, "File not found.");
                return;
            }

            int sourceYear;
            if (int.TryParse(_current.RevitVersion, NumberStyles.Integer, CultureInfo.InvariantCulture, out sourceYear) && sourceYear > AppPaths.RevitYear)
            {
                CompleteItem(ItemStatus.Failed, "Model was saved in Revit " + sourceYear.ToString(CultureInfo.InvariantCulture) + " and cannot be opened in Revit " + AppPaths.RevitYear.ToString(CultureInfo.InvariantCulture) + ".");
                return;
            }

            string directory = _naming.ResolveOutputDirectory(_settings, _current);
            string baseName = _naming.BuildName(_settings.FileNameTemplate, _current, _settings, position);
            bool skip = false;
            if (_engine.WritesPerModelFile)
            {
                baseName = _naming.ApplyOverwritePolicy(directory, baseName, FinalExtension(), _settings.OverwritePolicy, out skip);
            }

            if (skip)
            {
                CompleteItem(ItemStatus.Skipped, "Output already exists (overwrite policy: skip).");
                return;
            }

            Directory.CreateDirectory(directory);
            if (_dialogAutomation != null)
            {
                _dialogAutomation.Arm(Path.Combine(directory, baseName + _engine.OutputExtension));
            }

            _context = new ExportContext
            {
                UiApp = _uiApp,
                Item = _current,
                Settings = _settings,
                OutputDirectory = directory,
                BaseName = baseName
            };

            Document active = _uiApp.ActiveUIDocument == null ? null : _uiApp.ActiveUIDocument.Document;
            if (active != null && string.Equals(active.PathName, _current.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                _document = active;
                _openedByUs = false;
                _phase = RunnerPhase.Export;
                return;
            }

            _openedByUs = true;
            if (_engine.RequiresActiveDocument)
            {
                ExportJobItem openItem = _current;
                RunInEvent(
                    delegate (UIApplication app)
                    {
                        if (_deferredClose != null)
                        {
                            ModelLinkGuard.UnloadLinks(_deferredClose);
                        }

                        if (ModelLinkGuard.IsOpenAsLink(app.Application, openItem.FilePath))
                        {
                            throw new InvalidOperationException(ModelLinkGuard.LinkedMessage(openItem.FileName));
                        }

                        UIDocument opened = _opener.OpenAndActivate(app, openItem, _settings);
                        _document = opened.Document;
                    },
                    delegate
                    {
                        _waitTicks = 0;
                        _phase = RunnerPhase.AwaitActivation;
                    });
                return;
            }

            if (ModelLinkGuard.IsOpenAsLink(_uiApp.Application, _current.FilePath))
            {
                CompleteItem(ItemStatus.Failed, ModelLinkGuard.LinkedMessage(_current.FileName));
                return;
            }

            _document = _opener.OpenBackground(_uiApp.Application, _current, _settings);
            _phase = RunnerPhase.Export;
        }

        private void AwaitActivation()
        {
            UIDocument active = _uiApp.ActiveUIDocument;
            if (active != null && string.Equals(active.Document.Title, _document.Title, StringComparison.OrdinalIgnoreCase))
            {
                _phase = RunnerPhase.Export;
                return;
            }

            _waitTicks++;
            if (_waitTicks > MaxActivationTicks)
            {
                CompleteItem(ItemStatus.Failed, "The model could not be activated in the Revit window.");
            }
        }

        private void RunExport()
        {
            CloseDeferredDocument();
            _context.Document = _document;
            SetStatus(ItemStatus.Running, "Exporting...");

            if (_engine.NeedsView(_settings) && _context.ExportView == null)
            {
                ViewResolveResult resolved = _viewResolver.Resolve(_document, _settings);
                if (resolved.View == null)
                {
                    CompleteItem(ItemStatus.Failed, resolved.Message);
                    return;
                }

                _context.ExportView = resolved.View;
                _context.TemporaryViewCreated = resolved.CreatedTemporary;
                _log.Info(resolved.Message);

                string prepared = _viewPreparation.Apply(_document, resolved.View, _settings, resolved.CreatedTemporary);
                if (!string.IsNullOrEmpty(prepared))
                {
                    _log.Info("View preparation: " + prepared);
                }
            }

            if (_engine.RequiresActiveDocument)
            {
                ExportContext context = _context;
                View exportView = _context.ExportView;
                RunInEvent(
                    delegate (UIApplication app)
                    {
                        UIDocument uiDocument = app.ActiveUIDocument;
                        if (exportView != null && uiDocument != null)
                        {
                            if (_previousView == null)
                            {
                                _previousView = uiDocument.ActiveView;
                            }

                            uiDocument.ActiveView = exportView;
                        }

                        context.StartedUtc = DateTime.UtcNow;
                        _stepFromEvent = _engine.Begin(context);
                    },
                    delegate { HandleStep(_stepFromEvent); });
                return;
            }

            _context.StartedUtc = DateTime.UtcNow;
            HandleStep(_engine.Begin(_context));
        }

        private void RunInEvent(Action<UIApplication> work, Action after)
        {
            _workHandler.Prepare(work);
            _afterEvent = after;
            _eventClock = Stopwatch.StartNew();
            _phase = RunnerPhase.WaitingForEvent;
            _externalEvent.Raise();
        }

        private void WaitForEvent()
        {
            if (!_workHandler.Completed)
            {
                if (_eventClock.Elapsed > TimeSpan.FromSeconds(120))
                {
                    _afterEvent = null;
                    FailEventStep("Revit did not run the requested step within two minutes.");
                }

                return;
            }

            Exception error = _workHandler.Error;
            Action after = _afterEvent;
            _afterEvent = null;
            if (error != null)
            {
                _log.Error("Revit step failed: " + error);
                FailEventStep(error.Message);
                return;
            }

            if (after != null)
            {
                after();
            }
        }

        private void FailEventStep(string message)
        {
            if (_batchEnding)
            {
                _log.Warn(message);
                CompleteBatch();
                return;
            }

            CompleteItem(ItemStatus.Failed, message);
        }

        private void PollEngine()
        {
            _context.ContinueRequested = _viewModel.ConsumeContinueRequest();
            HandleStep(_engine.Poll(_context));
        }

        private void HandleStep(ExportStepResult step)
        {
            switch (step.State)
            {
                case StepState.Pending:
                    _phase = RunnerPhase.Polling;
                    _viewModel.ShowContinue = true;
                    _viewModel.StatusText = step.Message;
                    break;

                case StepState.Completed:
                    _viewModel.ShowContinue = false;
                    _context.OutputFiles.Clear();
                    _context.OutputFiles.AddRange(step.OutputFiles);
                    AfterExport(step.Message);
                    break;

                case StepState.Skipped:
                    _viewModel.ShowContinue = false;
                    CompleteItem(ItemStatus.Skipped, step.Message);
                    break;

                default:
                    _viewModel.ShowContinue = false;
                    if (_attempt < _settings.RetryCount)
                    {
                        _attempt++;
                        _log.Warn("Attempt failed (" + step.Message + "). Retrying " + _attempt.ToString(CultureInfo.InvariantCulture) + "/" + _settings.RetryCount.ToString(CultureInfo.InvariantCulture) + ".");
                        _phase = RunnerPhase.Export;
                    }
                    else
                    {
                        CompleteItem(ItemStatus.Failed, step.Message);
                    }

                    break;
            }
        }

        private void AfterExport(string message)
        {
            if (_settings.AlsoExportElementData && _settings.Engine != ExportEngineKind.ElementData && _settings.Engine != ExportEngineKind.ModelInspection)
            {
                try
                {
                    string dataFile = Path.Combine(_context.OutputDirectory, _context.BaseName + "_data.csv");
                    int rows = _elementData.Write(_document, _settings, dataFile);
                    _extraOutputs.Add(dataFile);
                    _log.Info("Element data written (" + rows.ToString(CultureInfo.InvariantCulture) + " rows): " + dataFile);
                }
                catch (Exception ex)
                {
                    _log.Warn("Element data export failed: " + ex.Message);
                }
            }

            if (_settings.PostProcessEnabled && _engine.ProducesIntermediateFile)
            {
                foreach (string file in _context.OutputFiles)
                {
                    _intermediateFiles.Add(file);
                    _pendingConversions.Enqueue(file);
                }

                StartNextConversion();
                return;
            }

            _finalOutputs.AddRange(_context.OutputFiles);
            CompleteItem(ItemStatus.Success, message);
        }

        private void StartNextConversion()
        {
            if (_pendingConversions.Count == 0)
            {
                if (_settings.DeleteIntermediateFiles)
                {
                    DeleteIntermediates();
                }

                CompleteItem(ItemStatus.Success, "Exported and converted.");
                return;
            }

            string input = _pendingConversions.Dequeue();
            _processTarget = _postProcess.BuildOutputPath(_settings, input);
            _processOutput.Clear();
            SetStatus(ItemStatus.Running, "Converting " + Path.GetFileName(input) + "...");
            _log.Info("Running converter: " + _settings.PostProcessExecutable);

            try
            {
                _process = _postProcess.Start(_settings, input, _processTarget, _processOutput);
            }
            catch (Exception ex)
            {
                CompleteItem(ItemStatus.Failed, "Could not start the converter: " + ex.Message);
                return;
            }

            _processStartedUtc = DateTime.UtcNow;
            _phase = RunnerPhase.PostProcess;
        }

        private void PollConversion()
        {
            if (_process == null)
            {
                CompleteItem(ItemStatus.Failed, "The converter process is not available.");
                return;
            }

            if (!_process.HasExited)
            {
                TimeSpan limit = TimeSpan.FromMinutes(Math.Max(1, _settings.PostProcessTimeoutMinutes));
                if (DateTime.UtcNow - _processStartedUtc > limit)
                {
                    StopProcess();
                    CompleteItem(ItemStatus.Failed, "The converter timed out.");
                }

                return;
            }

            _process.WaitForExit();
            int exitCode = _process.ExitCode;
            _process.Dispose();
            _process = null;

            if (exitCode != 0 || !File.Exists(_processTarget))
            {
                CompleteItem(
                    ItemStatus.Failed,
                    "Converter exit code " + exitCode.ToString(CultureInfo.InvariantCulture) + ". " + PostProcessService.Tail(_processOutput, 400));
                return;
            }

            _finalOutputs.Add(_processTarget);
            StartNextConversion();
        }

        private void DeleteIntermediates()
        {
            foreach (string file in _intermediateFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch (IOException ex)
                {
                    _log.Warn("Could not delete " + file + ": " + ex.Message);
                }
            }
        }

        private void StopProcess()
        {
            if (_process == null)
            {
                return;
            }

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                _log.Warn("The converter had already exited.");
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }

        private void CompleteItem(ItemStatus status, string message)
        {
            if (_current == null)
            {
                return;
            }

            _itemClock.Stop();
            if (status == ItemStatus.Success)
            {
                _finalOutputs.AddRange(_extraOutputs);
                CopyToSecondaryLocation();
                foreach (string file in _finalOutputs)
                {
                    if (!_allOutputs.Contains(file))
                    {
                        _allOutputs.Add(file);
                    }
                }
            }

            _current.Status = status;
            _current.Message = message;
            _current.Duration = _itemClock.Elapsed.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
            _current.OutputFiles = string.Join("; ", _finalOutputs.Count > 0 ? _finalOutputs : _context == null ? new List<string>() : _context.OutputFiles);

            string line = status + " - " + _current.FileName + " - " + message;
            if (status == ItemStatus.Failed)
            {
                _log.Error(line);
            }
            else
            {
                _log.Info(line);
            }

            if (_dialogAutomation != null)
            {
                _dialogAutomation.Disarm();
            }

            RemoveTemporaryView();
            ReleaseDocument();
            _context = null;
            _viewModel.ShowContinue = false;
            _index++;
            _viewModel.Progress = 100.0 * _index / Math.Max(1, _items.Count);
            _phase = RunnerPhase.NextItem;

            if (status == ItemStatus.Failed && !_settings.ContinueOnError)
            {
                _log.Warn("Stopping the batch because 'continue on error' is off.");
                MarkRemainingCancelled();
                _viewModel.CancelRequested = true;
            }
        }

        private void CopyToSecondaryLocation()
        {
            if (string.IsNullOrWhiteSpace(_settings.SecondaryCopyFolder))
            {
                return;
            }

            try
            {
                string target = _settings.SecondaryCopyFolder;
                if (_settings.PreserveFolderStructure && !string.IsNullOrEmpty(_current.RelativeDirectory))
                {
                    target = Path.Combine(target, _current.RelativeDirectory);
                }

                Directory.CreateDirectory(target);
                foreach (string file in _finalOutputs)
                {
                    if (File.Exists(file))
                    {
                        File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
                    }
                }

                _log.Info("Copied outputs to " + target);
            }
            catch (Exception ex)
            {
                _log.Warn("Could not copy outputs to the secondary folder: " + ex.Message);
            }
        }

        private void RemoveTemporaryView()
        {
            if (_context == null || !_context.TemporaryViewCreated || _context.ExportView == null || _document == null || _openedByUs)
            {
                return;
            }

            try
            {
                ElementId viewId = _context.ExportView.Id;
                UIDocument uiDocument = _uiApp.ActiveUIDocument;
                if (uiDocument != null && _previousView != null && uiDocument.ActiveView.Id.Equals(viewId))
                {
                    uiDocument.ActiveView = _previousView;
                }

                using (Transaction transaction = new Transaction(_document, "BIM TOOLS - Remove temporary export view"))
                {
                    transaction.Start();
                    _document.Delete(viewId);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                _log.Warn("The temporary export view could not be removed from the open model: " + ex.Message);
            }
        }

        private void ReleaseDocument()
        {
            Document document = _document;
            _document = null;
            if (document == null || !_openedByUs)
            {
                return;
            }

            if (_engine.RequiresActiveDocument)
            {
                CloseDeferredDocument();
                _deferredClose = document;
                return;
            }

            CloseDocument(document);
        }

        private void CloseDeferredDocument()
        {
            if (_deferredClose == null)
            {
                return;
            }

            Document pending = _deferredClose;
            if (_document != null && string.Equals(pending.Title, _document.Title, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _deferredClose = null;
            CloseDocument(pending);
        }

        private void CloseDocument(Document document)
        {
            try
            {
                if (document.IsValidObject)
                {
                    document.Close(false);
                }
            }
            catch (Exception ex)
            {
                _log.Warn("Could not close a model: " + ex.Message);
            }
        }

        private void MarkRemainingCancelled()
        {
            for (int i = _index; i < _items.Count; i++)
            {
                if (_items[i].IsSelected && _items[i].Status == ItemStatus.Pending)
                {
                    _items[i].Status = ItemStatus.Cancelled;
                    _items[i].Message = "Not processed.";
                }
            }
        }

        private void SetStatus(ItemStatus status, string message)
        {
            _current.Status = status;
            _current.Message = message;
            _viewModel.StatusText = message;
        }

        private string FinalExtension()
        {
            if (_settings.PostProcessEnabled && _engine.ProducesIntermediateFile)
            {
                return FileNamingService.NormalizeExtension(_settings.PostProcessOutputExtension, ".spx");
            }

            return _engine.OutputExtension;
        }

        private void FinishBatch()
        {
            if (_engine.RequiresActiveDocument && _deferredClose != null && !string.IsNullOrEmpty(_originalPath) && File.Exists(_originalPath))
            {
                _viewModel.StatusText = "Restoring your original model...";
                _batchEnding = true;
                string original = _originalPath;
                RunInEvent(
                    delegate (UIApplication app) { app.OpenAndActivateDocument(original); },
                    delegate
                    {
                        _waitTicks = 0;
                        _phase = RunnerPhase.Restoring;
                    });
                return;
            }

            CompleteBatch();
        }

        private void RestoreOriginal()
        {
            UIDocument active = _uiApp.ActiveUIDocument;
            bool restored = active != null && string.Equals(active.Document.PathName, _originalPath, StringComparison.OrdinalIgnoreCase);
            _waitTicks++;
            if (!restored && _waitTicks < MaxActivationTicks)
            {
                return;
            }

            if (restored)
            {
                CloseDeferredDocument();
            }

            CompleteBatch();
        }

        private void CompleteBatch()
        {
            _finished = true;
            _uiApp.Idling -= OnIdling;
            if (_externalEvent != null)
            {
                _externalEvent.Dispose();
                _externalEvent = null;
            }

            if (_dialogAutomation != null)
            {
                _dialogAutomation.Dispose();
                _dialogAutomation = null;
            }

            if (_suppressor != null)
            {
                _suppressor.Dispose();
                _suppressor = null;
            }

            _batchClock.Stop();
            int success = 0;
            int failed = 0;
            int skipped = 0;
            int cancelled = 0;
            foreach (ExportJobItem item in _items)
            {
                if (!item.IsSelected)
                {
                    continue;
                }

                switch (item.Status)
                {
                    case ItemStatus.Success:
                        success++;
                        break;
                    case ItemStatus.Failed:
                        failed++;
                        break;
                    case ItemStatus.Skipped:
                        skipped++;
                        break;
                    default:
                        cancelled++;
                        break;
                }
            }

            string summary = string.Format(
                CultureInfo.InvariantCulture,
                "Finished in {0:hh\\:mm\\:ss}. Succeeded: {1}, failed: {2}, skipped: {3}, not completed: {4}.",
                _batchClock.Elapsed,
                success,
                failed,
                skipped,
                cancelled);
            _log.Info(summary);

            string reportFolder = ReportFolder();
            if (_settings.WriteCsvReport)
            {
                try
                {
                    string reportPath = Path.Combine(reportFolder, "SPX_Export_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv");
                    _report.WriteCsv(reportPath, _items);
                    _viewModel.ReportPath = reportPath;
                    _log.Info("Report written: " + reportPath);
                }
                catch (IOException ex)
                {
                    _log.Warn("Could not write the CSV report: " + ex.Message);
                }
            }

            if (_settings.WriteHtmlReport)
            {
                try
                {
                    string htmlPath = Path.Combine(reportFolder, "SPX_Export_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".html");
                    _report.WriteHtml(htmlPath, _items, summary, _engine.DisplayName);
                    _log.Info("HTML report written: " + htmlPath);
                    if (string.IsNullOrEmpty(_viewModel.ReportPath))
                    {
                        _viewModel.ReportPath = htmlPath;
                    }
                }
                catch (IOException ex)
                {
                    _log.Warn("Could not write the HTML report: " + ex.Message);
                }
            }

            if (_settings.CreateZipArchive && _allOutputs.Count > 0)
            {
                try
                {
                    string zipPath = Path.Combine(reportFolder, "SPX_Export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".zip");
                    int files = _archive.CreateZip(zipPath, _allOutputs);
                    _log.Info("Archive created with " + files.ToString(CultureInfo.InvariantCulture) + " file(s): " + zipPath);
                }
                catch (Exception ex)
                {
                    _log.Warn("Could not create the zip archive: " + ex.Message);
                }
            }

            RunAfterBatchCommand(reportFolder);

            _viewModel.OutputFolder = reportFolder;
            _viewModel.Progress = 100;
            _viewModel.CurrentText = "Finished";
            _viewModel.StatusText = summary;
            _viewModel.IsRunning = false;
            IsRunning = false;

            if (_settings.OpenOutputFolderWhenDone && Directory.Exists(reportFolder) && success > 0)
            {
                Process.Start("explorer.exe", "\"" + reportFolder + "\"");
            }

            if (_settings.PlaySoundWhenDone)
            {
                System.Media.SystemSounds.Asterisk.Play();
            }

            if (_settings.ShowSummaryDialog)
            {
                TaskDialog dialog = new TaskDialog(AppPaths.DialogTitle);
                dialog.MainInstruction = failed == 0 ? "Batch export complete" : "Batch export finished with errors";
                dialog.MainContent = summary + Environment.NewLine + Environment.NewLine + "Log: " + _log.FilePath;
                dialog.CommonButtons = TaskDialogCommonButtons.Close;
                dialog.Show();
            }
        }

        private void RunAfterBatchCommand(string reportFolder)
        {
            if (string.IsNullOrWhiteSpace(_settings.RunAfterBatchExecutable))
            {
                return;
            }

            try
            {
                string arguments = (_settings.RunAfterBatchArguments ?? string.Empty)
                    .Replace("{reportPath}", _viewModel.ReportPath ?? string.Empty)
                    .Replace("{outputDir}", reportFolder)
                    .Replace("{logPath}", _log.FilePath);
                Process.Start(_settings.RunAfterBatchExecutable, arguments);
                _log.Info("Started post-batch command: " + _settings.RunAfterBatchExecutable);
            }
            catch (Exception ex)
            {
                _log.Warn("The post-batch command could not be started: " + ex.Message);
            }
        }

        private string ReportFolder()
        {
            if (!_settings.OutputToSourceFolder && !string.IsNullOrWhiteSpace(_settings.OutputFolder) && Directory.Exists(_settings.OutputFolder))
            {
                return _settings.OutputFolder;
            }

            foreach (ExportJobItem item in _items)
            {
                if (item.IsSelected)
                {
                    string directory = Path.GetDirectoryName(item.FilePath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        return directory;
                    }
                }
            }

            return AppPaths.LogFolder;
        }
    }
}
