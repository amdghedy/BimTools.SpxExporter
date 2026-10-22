using System.Runtime.Serialization;

namespace BimTools.SpxExporter.Models
{
    [DataContract]
    public sealed class ExportSettings : ObservableObject
    {
        private ExportEngineKind _engine;
        private string _outputFolder;
        private bool _outputToSourceFolder;
        private bool _preserveFolderStructure;
        private bool _createDateSubfolder;
        private string _fileNameTemplate;
        private OverwritePolicy _overwritePolicy;
        private DetachMode _detachMode;
        private WorksetMode _worksetMode;
        private string _worksetIncludePatterns;
        private string _worksetExcludePatterns;
        private bool _auditOnOpen;
        private bool _suppressDialogs;
        private ViewSelectionMode _viewMode;
        private string _viewName;
        private bool _fallbackToTemporaryView;
        private IfcVersionOption _ifcVersion;
        private bool _ifcExportBaseQuantities;
        private bool _ifcSplitWalls;
        private string _synchroCommandId;
        private string _synchroWatchFolder;
        private int _synchroTimeoutMinutes;
        private bool _postProcessEnabled;
        private string _postProcessExecutable;
        private string _postProcessArguments;
        private string _postProcessOutputExtension;
        private int _postProcessTimeoutMinutes;
        private bool _deleteIntermediateFiles;
        private int _retryCount;
        private bool _continueOnError;
        private bool _writeCsvReport;
        private bool _openOutputFolderWhenDone;
        private bool _showSummaryDialog;
        private bool _alsoExportElementData;
        private string _dataParameterNames;
        private string _dataCategoryFilter;
        private bool _dataIncludeTypeParameters;
        private bool _inspectionDeepScan;
        private DetailLevelOption _viewDetailLevel;
        private bool _prepareExistingViews;
        private bool _viewHideRooms;
        private bool _viewHideGridsAndLevels;
        private bool _viewHideLinks;
        private bool _viewHideViewMarkers;
        private string _viewHideCategoryNames;
        private string _viewHideWorksetPatterns;
        private string _viewPhaseName;
        private string _viewPhaseFilterName;
        private string _secondaryCopyFolder;
        private bool _createZipArchive;
        private bool _scheduleEnabled;
        private string _scheduledStart;
        private bool _playSoundWhenDone;
        private bool _writeHtmlReport;
        private string _runAfterBatchExecutable;
        private string _runAfterBatchArguments;
        private ViewProjectionOption _viewProjection;
        private bool _autoAnswerDialogs;
        private string _autoClickButtons;
        private string _dialogFieldOverrides;
        private CoordinateSystemOption _coordinateSystem;
        private string _excludeFilePatterns;

        public ExportSettings()
        {
            ApplyDefaults();
        }

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            ApplyDefaults();
        }

        private void ApplyDefaults()
        {
            _engine = ExportEngineKind.SynchroAddin;
            _outputFolder = "";
            _outputToSourceFolder = true;
            _preserveFolderStructure = true;
            _createDateSubfolder = false;
            _fileNameTemplate = "{ModelName}";
            _overwritePolicy = OverwritePolicy.Overwrite;
            _detachMode = DetachMode.DetachAndPreserveWorksets;
            _worksetMode = WorksetMode.OpenAll;
            _worksetIncludePatterns = "";
            _worksetExcludePatterns = "";
            _auditOnOpen = false;
            _suppressDialogs = true;
            _viewMode = ViewSelectionMode.Default3D;
            _viewName = "{3D}";
            _fallbackToTemporaryView = true;
            _ifcVersion = IfcVersionOption.Ifc2x3CoordinationView2;
            _ifcExportBaseQuantities = true;
            _ifcSplitWalls = false;
            _synchroCommandId = "CustomCtrl_%CustomCtrl_%Add-Ins%Synchro%ExportToSynchroButton";
            _synchroWatchFolder = "";
            _synchroTimeoutMinutes = 30;
            _postProcessEnabled = false;
            _postProcessExecutable = "";
            _postProcessArguments = "\"{input}\" \"{output}\"";
            _postProcessOutputExtension = ".spx";
            _postProcessTimeoutMinutes = 30;
            _deleteIntermediateFiles = false;
            _retryCount = 1;
            _continueOnError = true;
            _writeCsvReport = true;
            _openOutputFolderWhenDone = true;
            _showSummaryDialog = true;
            _alsoExportElementData = false;
            _dataParameterNames = "Mark;Comments";
            _dataCategoryFilter = "";
            _dataIncludeTypeParameters = true;
            _inspectionDeepScan = true;
            _viewDetailLevel = DetailLevelOption.Fine;
            _prepareExistingViews = false;
            _viewHideRooms = false;
            _viewHideGridsAndLevels = false;
            _viewHideLinks = false;
            _viewHideViewMarkers = false;
            _viewHideCategoryNames = "";
            _viewHideWorksetPatterns = "";
            _viewPhaseName = "";
            _viewPhaseFilterName = "";
            _secondaryCopyFolder = "";
            _createZipArchive = false;
            _scheduleEnabled = false;
            _scheduledStart = "22:00";
            _playSoundWhenDone = true;
            _writeHtmlReport = false;
            _runAfterBatchExecutable = "";
            _runAfterBatchArguments = "\"{reportPath}\" \"{outputDir}\"";
            _viewProjection = ViewProjectionOption.Orthographic;
            _autoAnswerDialogs = true;
            _autoClickButtons = "Save;Yes;OK;Continue;Export;Select Folder;Select;Open;Next;Finish;Close";
            _dialogFieldOverrides = "";
            _coordinateSystem = CoordinateSystemOption.Shared;
            _excludeFilePatterns = "*_detached*";
        }

        [DataMember]
        public ExportEngineKind Engine
        {
            get { return _engine; }
            set { SetProperty(ref _engine, value); }
        }

        [DataMember]
        public string OutputFolder
        {
            get { return _outputFolder; }
            set { SetProperty(ref _outputFolder, value); }
        }

        [DataMember]
        public bool OutputToSourceFolder
        {
            get { return _outputToSourceFolder; }
            set { SetProperty(ref _outputToSourceFolder, value); }
        }

        [DataMember]
        public bool PreserveFolderStructure
        {
            get { return _preserveFolderStructure; }
            set { SetProperty(ref _preserveFolderStructure, value); }
        }

        [DataMember]
        public bool CreateDateSubfolder
        {
            get { return _createDateSubfolder; }
            set { SetProperty(ref _createDateSubfolder, value); }
        }

        [DataMember]
        public string FileNameTemplate
        {
            get { return _fileNameTemplate; }
            set { SetProperty(ref _fileNameTemplate, value); }
        }

        [DataMember]
        public OverwritePolicy OverwritePolicy
        {
            get { return _overwritePolicy; }
            set { SetProperty(ref _overwritePolicy, value); }
        }

        [DataMember]
        public DetachMode DetachMode
        {
            get { return _detachMode; }
            set { SetProperty(ref _detachMode, value); }
        }

        [DataMember]
        public WorksetMode WorksetMode
        {
            get { return _worksetMode; }
            set { SetProperty(ref _worksetMode, value); }
        }

        [DataMember]
        public string WorksetIncludePatterns
        {
            get { return _worksetIncludePatterns; }
            set { SetProperty(ref _worksetIncludePatterns, value); }
        }

        [DataMember]
        public string WorksetExcludePatterns
        {
            get { return _worksetExcludePatterns; }
            set { SetProperty(ref _worksetExcludePatterns, value); }
        }

        [DataMember]
        public bool AuditOnOpen
        {
            get { return _auditOnOpen; }
            set { SetProperty(ref _auditOnOpen, value); }
        }

        [DataMember]
        public bool SuppressDialogs
        {
            get { return _suppressDialogs; }
            set { SetProperty(ref _suppressDialogs, value); }
        }

        [DataMember]
        public ViewSelectionMode ViewMode
        {
            get { return _viewMode; }
            set { SetProperty(ref _viewMode, value); }
        }

        [DataMember]
        public string ViewName
        {
            get { return _viewName; }
            set { SetProperty(ref _viewName, value); }
        }

        [DataMember]
        public bool FallbackToTemporaryView
        {
            get { return _fallbackToTemporaryView; }
            set { SetProperty(ref _fallbackToTemporaryView, value); }
        }

        [DataMember]
        public IfcVersionOption IfcVersion
        {
            get { return _ifcVersion; }
            set { SetProperty(ref _ifcVersion, value); }
        }

        [DataMember]
        public bool IfcExportBaseQuantities
        {
            get { return _ifcExportBaseQuantities; }
            set { SetProperty(ref _ifcExportBaseQuantities, value); }
        }

        [DataMember]
        public bool IfcSplitWalls
        {
            get { return _ifcSplitWalls; }
            set { SetProperty(ref _ifcSplitWalls, value); }
        }

        [DataMember]
        public string SynchroCommandId
        {
            get { return _synchroCommandId; }
            set { SetProperty(ref _synchroCommandId, value); }
        }

        [DataMember]
        public string SynchroWatchFolder
        {
            get { return _synchroWatchFolder; }
            set { SetProperty(ref _synchroWatchFolder, value); }
        }

        [DataMember]
        public int SynchroTimeoutMinutes
        {
            get { return _synchroTimeoutMinutes; }
            set { SetProperty(ref _synchroTimeoutMinutes, value); }
        }

        [DataMember]
        public bool PostProcessEnabled
        {
            get { return _postProcessEnabled; }
            set { SetProperty(ref _postProcessEnabled, value); }
        }

        [DataMember]
        public string PostProcessExecutable
        {
            get { return _postProcessExecutable; }
            set { SetProperty(ref _postProcessExecutable, value); }
        }

        [DataMember]
        public string PostProcessArguments
        {
            get { return _postProcessArguments; }
            set { SetProperty(ref _postProcessArguments, value); }
        }

        [DataMember]
        public string PostProcessOutputExtension
        {
            get { return _postProcessOutputExtension; }
            set { SetProperty(ref _postProcessOutputExtension, value); }
        }

        [DataMember]
        public int PostProcessTimeoutMinutes
        {
            get { return _postProcessTimeoutMinutes; }
            set { SetProperty(ref _postProcessTimeoutMinutes, value); }
        }

        [DataMember]
        public bool DeleteIntermediateFiles
        {
            get { return _deleteIntermediateFiles; }
            set { SetProperty(ref _deleteIntermediateFiles, value); }
        }

        [DataMember]
        public int RetryCount
        {
            get { return _retryCount; }
            set { SetProperty(ref _retryCount, value); }
        }

        [DataMember]
        public bool ContinueOnError
        {
            get { return _continueOnError; }
            set { SetProperty(ref _continueOnError, value); }
        }

        [DataMember]
        public bool WriteCsvReport
        {
            get { return _writeCsvReport; }
            set { SetProperty(ref _writeCsvReport, value); }
        }

        [DataMember]
        public bool OpenOutputFolderWhenDone
        {
            get { return _openOutputFolderWhenDone; }
            set { SetProperty(ref _openOutputFolderWhenDone, value); }
        }

        [DataMember]
        public bool ShowSummaryDialog
        {
            get { return _showSummaryDialog; }
            set { SetProperty(ref _showSummaryDialog, value); }
        }

        [DataMember]
        public bool AlsoExportElementData
        {
            get { return _alsoExportElementData; }
            set { SetProperty(ref _alsoExportElementData, value); }
        }

        [DataMember]
        public string DataParameterNames
        {
            get { return _dataParameterNames; }
            set { SetProperty(ref _dataParameterNames, value); }
        }

        [DataMember]
        public string DataCategoryFilter
        {
            get { return _dataCategoryFilter; }
            set { SetProperty(ref _dataCategoryFilter, value); }
        }

        [DataMember]
        public bool DataIncludeTypeParameters
        {
            get { return _dataIncludeTypeParameters; }
            set { SetProperty(ref _dataIncludeTypeParameters, value); }
        }

        [DataMember]
        public bool InspectionDeepScan
        {
            get { return _inspectionDeepScan; }
            set { SetProperty(ref _inspectionDeepScan, value); }
        }

        [DataMember]
        public DetailLevelOption ViewDetailLevel
        {
            get { return _viewDetailLevel; }
            set { SetProperty(ref _viewDetailLevel, value); }
        }

        [DataMember]
        public bool PrepareExistingViews
        {
            get { return _prepareExistingViews; }
            set { SetProperty(ref _prepareExistingViews, value); }
        }

        [DataMember]
        public bool ViewHideRooms
        {
            get { return _viewHideRooms; }
            set { SetProperty(ref _viewHideRooms, value); }
        }

        [DataMember]
        public bool ViewHideGridsAndLevels
        {
            get { return _viewHideGridsAndLevels; }
            set { SetProperty(ref _viewHideGridsAndLevels, value); }
        }

        [DataMember]
        public bool ViewHideLinks
        {
            get { return _viewHideLinks; }
            set { SetProperty(ref _viewHideLinks, value); }
        }

        [DataMember]
        public bool ViewHideViewMarkers
        {
            get { return _viewHideViewMarkers; }
            set { SetProperty(ref _viewHideViewMarkers, value); }
        }

        [DataMember]
        public string ViewHideCategoryNames
        {
            get { return _viewHideCategoryNames; }
            set { SetProperty(ref _viewHideCategoryNames, value); }
        }

        [DataMember]
        public string ViewHideWorksetPatterns
        {
            get { return _viewHideWorksetPatterns; }
            set { SetProperty(ref _viewHideWorksetPatterns, value); }
        }

        [DataMember]
        public string ViewPhaseName
        {
            get { return _viewPhaseName; }
            set { SetProperty(ref _viewPhaseName, value); }
        }

        [DataMember]
        public string ViewPhaseFilterName
        {
            get { return _viewPhaseFilterName; }
            set { SetProperty(ref _viewPhaseFilterName, value); }
        }

        [DataMember]
        public string SecondaryCopyFolder
        {
            get { return _secondaryCopyFolder; }
            set { SetProperty(ref _secondaryCopyFolder, value); }
        }

        [DataMember]
        public bool CreateZipArchive
        {
            get { return _createZipArchive; }
            set { SetProperty(ref _createZipArchive, value); }
        }

        [DataMember]
        public bool ScheduleEnabled
        {
            get { return _scheduleEnabled; }
            set { SetProperty(ref _scheduleEnabled, value); }
        }

        [DataMember]
        public string ScheduledStart
        {
            get { return _scheduledStart; }
            set { SetProperty(ref _scheduledStart, value); }
        }

        [DataMember]
        public bool PlaySoundWhenDone
        {
            get { return _playSoundWhenDone; }
            set { SetProperty(ref _playSoundWhenDone, value); }
        }

        [DataMember]
        public bool WriteHtmlReport
        {
            get { return _writeHtmlReport; }
            set { SetProperty(ref _writeHtmlReport, value); }
        }

        [DataMember]
        public string RunAfterBatchExecutable
        {
            get { return _runAfterBatchExecutable; }
            set { SetProperty(ref _runAfterBatchExecutable, value); }
        }

        [DataMember]
        public string RunAfterBatchArguments
        {
            get { return _runAfterBatchArguments; }
            set { SetProperty(ref _runAfterBatchArguments, value); }
        }

        [DataMember]
        public ViewProjectionOption ViewProjection
        {
            get { return _viewProjection; }
            set { SetProperty(ref _viewProjection, value); }
        }

        [DataMember]
        public bool AutoAnswerDialogs
        {
            get { return _autoAnswerDialogs; }
            set { SetProperty(ref _autoAnswerDialogs, value); }
        }

        [DataMember]
        public string AutoClickButtons
        {
            get { return _autoClickButtons; }
            set { SetProperty(ref _autoClickButtons, value); }
        }

        [DataMember]
        public string DialogFieldOverrides
        {
            get { return _dialogFieldOverrides; }
            set { SetProperty(ref _dialogFieldOverrides, value); }
        }

        [DataMember]
        public CoordinateSystemOption CoordinateSystem
        {
            get { return _coordinateSystem; }
            set { SetProperty(ref _coordinateSystem, value); }
        }

        [DataMember]
        public string ExcludeFilePatterns
        {
            get { return _excludeFilePatterns; }
            set { SetProperty(ref _excludeFilePatterns, value); }
        }

    }
}
