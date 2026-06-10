Option Strict Off

'Imports SevenZip
Imports System.Diagnostics
Imports System.IO
Imports System.Speech.Synthesis
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Windows.Forms.DataVisualization.Charting
Imports System.Xml
Imports de.etas.cebra.toolAPI.Inca
Imports NAudio.Wave

'Imports CLEVIR_INCA_7_4.DataDictionarySingleton

'Comment out for PATAC


Public Class SignalEnums
    Public Property VariableName As String
    Public Property DeviceName As String

    ' Using a List(Of String) instead of a fixed array.
    ' This allows for simpler resizing and adding of enum values.
    Public Property Enums As List(Of String)

    Public Sub New()
        Enums = New List(Of String)()
    End Sub

    Public Sub New(variableName As String, deviceName As String)
        Me.VariableName = variableName
        Me.DeviceName = deviceName
        Enums = New List(Of String)()
    End Sub
End Class

''' <summary>
''' Camera configuration entry mapping position to IP address.
''' Used to build the active camera list for the current vehicle.
''' </summary>
Public Class CameraConfig
    Public Property Position As String      ' FRONT, REAR, LEFT, RIGHT, etc.
    Public Property IpAddress As String     ' 192.168.40.101
    Public Property Enabled As Boolean      ' true/false

    Public Sub New()
        Enabled = True
    End Sub

    Public Sub New(pos As String, ip As String, Optional en As Boolean = True)
        Position = pos
        IpAddress = ip
        Enabled = en
    End Sub
End Class

Public Module Module1

    'This is the code module which contains global variable definitions, routines and functions that are shared by multiple forms or are not specific to any particular
    'form.

    Public Structure SavedCustomAnnotations
        Dim Filename As String
        Dim CustomAnnotations() As String
    End Structure

    Public mySavedCustomAnnotations() As SavedCustomAnnotations

    Public mySignalEnums() As SignalEnums
    Public GlobalSignalEnums As List(Of SignalEnums)

    Enum OperatingModes
        UNDEFINED
        ResOnVpc
        ResOnLaptop
        ResOnLaptopVpc
    End Enum

    Public Structure MeasInfo
        Dim MeasName As String
        Dim DeviceName As String
    End Structure

    Public myMeasInfo() As MeasInfo 'used by Form1 (CLEVIR Calibration Interface Form) to display available measure variables for selection

    Public Structure CalInfo
        Dim CalName As String
        Dim DeviceName As String
        Dim CalType As String
        Dim IsMatrix As Boolean
    End Structure

    Public myCalInfo() As CalInfo 'used by Form1 (CLEVIR Calibration Interface Form) to display available calibration variables for selection

    Public Structure FlashInfo
        Dim FlashType As String
        Dim DeviceName As String
        Dim DeviceType As String
    End Structure

    Public MyIncaInterface As INCA_InterfaceClass 'Set in HandleWirelessConnection. This is the instance of INCA_InterfaceClass which interfaces to GM_INCA_CommClass

    'INCA_InterfaceClass and GM_INCA_CommClass --- This implementation is based on the original client / Server architecture which is no longer used in this application.
    'Originally, GM_INCA_CommClass contained all of the INCA Tool API calls. It was a server that could interface with multiple clients.
    'The INCA_InterfaceClass was the Interface between the CLEVIR client app and the GM_INCA_CommClass server, so whenever any interaction with INCA was required by
    'any routine, that routine would make a call to the INCA_InterfaceClass which would in turn make a call to the GM_INCA_Comm server.  Midway through the design, 
    'the client / server architecture was dumped so the GM_INCA_Comm is no longer a server.  However, because we had a functional application, when the switch was made, 
    'much of the original organization of the code in GM_INCA_Comm was retained.  Since then, we no longer use GM_INCA_Comm as the exclusive interface making all the INCA API Calls, 
    'so you will see some "muddying of the waters" in terms of implementation when it comes to making INCA API calls and in terms of the relationship between the rest of the application and
    'the INCA_InterfaceClass and GM_INCA_CommClass...

    Private ReadOnly compuMethodMap As New Dictionary(Of String, String)()
    Private ReadOnly compuVtabMap As New Dictionary(Of String, List(Of String))()
    Private ReadOnly measurementMethodMap As New Dictionary(Of String, String)()
    Public hostname As String

    Public ExitPressed As Boolean
    Public SaveDataFrozenDeviceName As String = ""
    Public SaveVideoFrozenDeviceName As String = ""
    Public SaveLostDeviceName As String = ""

    Public ReadOnly FlashParameters(0 To 5) As FlashInfo 'Set in HandleWorkspace, used in FlashingStatus form when flashing
    Public AlternateRecordingMode As String 'FlexrayAndFO, FlexrayOnly, NoCanalyzer or VehicleSpy - Set during initialization, used during recording
    Public ReadOnly DriveLetters As String() = {"A", "B", "D", "E", "F", "G"} 'Used in InitForm_Load when determining if external flash drive is being used
    Public UsingFlashDrive As Boolean       'Set in InitForm_Load if we determine that external flash drive is being used - affects various aspects of CLEVIR behavior
    Public SelectedTestName As String       'Set in SetupDataLogging based on vehiclenumber, date, time, etc.  Used for naming the various recorded files
    Public SaveSelectedTestName As String   'Set in SetupDataLogging.  Used in conjunction with SelectedTestName
    Public SaveLoginID As String = ""            'Set as part of login process, either from LoginForm or in SetupDataLogging
    Public VehicleNumber As String          'Holds the vehiclenumber read from the vehicleconfig.txt file.  Used when setting up file names for record files etc.
    Public ANNOFileName As String           'Set in SetupDataLogging, holds the annotation file name for the current recording session.  Used when saving data to this file, etc.
    Public CanTemplateExperimentName As String     'Set in ReadVehicleConfigsFile and is based on ProjectName derived from information in vehicleconfugurations.csv - Used when creating a new experiment 
    Private MessageLogLevel As Integer        'Set in ReadDebugFile - used as a debug message filter, the lower the MessageLogLevel,the more messages are written to the GM_ResidentClient.log file.
    Public CLEVIRFilesPath As String = "Current" 'Set in ReadVehicleConfigsFile and SaveVehicleConfigChanges.  Determines directory where recorded files are saved, based on ProjectName
    Public ZipTheMF4Files As Boolean = True  'Set in ReadVehicleConfigsFile.  Determines if mf4 files will be zipped during recording sessions.  Validation vehicles do not zip, all others do.

    Public FCMConfigName As String

    Private ReadOnly OriginalVehicleConfiguration As String = "UNDEFINED"
    Public CurrentVehicleUsage As String = "UNDEFINED"

    Public CounterValue As Integer           'Set in ReadDebugFile, used in GM_INCA_CommClass - DataCollectionProcess
    Public DebugMessages As Integer          'Set in ReadDebugFile, used as a debug message fileter for DataCollectionProcess messages
    Public INCARunning As Boolean            'Set in ConnectToINCA and Connect (Called from InitINCA),  True if INCA is running.  Impacts startup behavior based on status of INCA when CLEVIR is launched.
    Public LoginIDNameAndFreqAL As ArrayList = Nothing  'Set in GM_INCA_CommClass - ReadUserIDList, this is a list of login IDs and frequency of use
    Public myUserName As String              'Set in GM_INCA_CommClass - SwitchToUserNamed - used when switching to a new user (This is not commonly done anymore, this is more or less legacy)
    Public SetExperimentDispatch As Boolean      'Status of the rci2.IncaSetExperimentDispatch(myExpEnvView) call.  Used in HandleWorkspace and RegisterSignals
    Public ReferenceDataSetDataBasePaths() As String 'Set in Initialize, contains INCA reference Dataset Folder paths for each device
    Public WorkingDataSetDataBasePaths() As String   'Set in initialize, contains INCA Working Dataset folder paths for each device
    Public ProjectDatabasePaths() As String          'Set in initialize using the call SetProjectDatabaseInfo
    Public ProjectDatabaseNames() As String          'Set in initialize using the call SetProjectDatabaseInfo
    Public FinalPathToSaveData As String             'Set in SetupDataLogging, contains the path to save data to for the current recording session
    Public SaveFinalPathToSaveData As String         'Set on Measurement/Record Start/Stop events used when saving data from previous recording session at start of new recording.
    Public RecordingFileFormat As String             'Set in GM_INCA_CommClass - SetupDataLogging and in INCA_InterfaceClass - StopRecording
    Public TerminateInitThread As Boolean            'Used and set in various places in InitForm.vb and GmResidentClient.vb
    Public Initialized As Boolean                    'Set after INCA is initialized.  Used in connect to determine if INCA has already been initialized. (This variable currently has no real use)
    Private CurrentWAVSequence As Integer             ' Near other recording state (e.g., under CurrentWAVFilename)
    Public myinca As Inca                            'This is the main INCA object, used throughout...
    Public myActualDatabase As IncaDataBase          'This is the INCA database, used throughout...
    Public MyHWC As HardwareConfiguration            'This is the INCA Workspace, used throughout...

    Public INCAWorkspace As String                   'This is the INCA Workspace name, used throughout...
    Public INCADatabase As String                    'This is the INCA Database name
    Public INCAExperiment As String                  'This is the INCA Experiment name
    Public InitialINCAExperiment As String           'Stores the name of the INCA Experiment that was first referenced when CLEVIR started, in case we need to revert back to it
    Public INCAVariableFile As String                'This is the INCA Signal List file (.xlsx) file. 
    Public INCAWorkspaceTemplateName As String

    Public MeasurementStarted As Boolean
    Public myDeviceRasterSignals() As IGM_INCA_Comm.DeviceRasterSignalStatus
    Public myDeviceRasterSignal As IGM_INCA_Comm.DeviceRasterSignalStatus
    Public StatusString() As String
    Public PlaybackMode As Boolean

    Public CurrentMileage As Double = 0.0
    Public LaneClassCurrent As Integer = 0
    Public CurrentLatitude As Double = 0.0
    Public CurrentLongitude As Double = 0.0

    Public AvailableWorkspaces() As String

    Public SessionComments As String
    Public SessionLocation As String
    Public SessionRing As String

    '    Public AnnotationText As String
    Public SaveAnnoButtonText As String

    Public CalSnapShotFilesAreSet() As Boolean

    Public OperatingMode As OperatingModes
    Public NetworkAdapterDescription As String
    Public EnableDataUpload As Boolean

    Public ReadOnly myDGs As New List(Of GridDataClass)
    Public ReadNewDataFile As Boolean

    Public EnableAltRecReStartAfterRecordStop As Boolean
    Public SaveCalSnapshotEnabled As Boolean
    Public AlternateRecordEnabled As Boolean
    Public AlternateRecordConfig As String

    Public CheckForNewExecutables As Boolean
    Public CheckForNewINCAProjects As Boolean

    Public NetworkDriveLetter As String
    Public SaveNetworkDriveLetter As String

    Public NetworkDriveMapping As String = ""

    Public UnzipPath As String
    Public UnzipSubDir As String
    Public UnzipFileName As String

    Public DataUploadPath As String = ""
    Public SaveDataUploadPath As String = ""

    Public AggregateAnnoFileName As String = ""

    Public exceldata(,) As Object
    Public ValidExcelData(,) As Object
    Public exceldataforsave(,) As Object

    Public UploadDataOnExit As Boolean
    Public AudioToTextConversion As Boolean
    Public SaveCalSnapshotEnabledChanged As Boolean
    Public CLEVIRFlavor As String 'DEVELOPMENT, DATALOGGING, or DATALOGGINGWITHUPLOAD
    Public InSession As Boolean
    Public DebugMode As Boolean
    Public DebugKey As Boolean
    Public OnLoginScreen As Boolean
    Public SaveRecordingFileName As String
    Public HaveRecorded As Boolean
    Public ShutdownWindows As Boolean
    Public RestartWindows As Boolean
    Public GridToModify As String
    Public RecordFileDurationMinutes As Long
    Public FormDisplayed As Boolean

    Public ETAS_USER_PATH As String
    Public EtasDefaulUserName As String

    Public AvailableExperimentNames As String()
    Public NumControllers As Integer = 0

    Public TriggerCommFailureInDebugMode As Boolean
    Public OverrideCommFailureInDebugMode As Boolean

    Public myVoiceCommands() As String
    Public VoiceRecognitionInstance As VoiceRecognitionClass = Nothing

    Public UserStatusInfoText As String

    Public AnnotationDataDictionaryFile As String

    Public IgnoreLostDeviceUntilNextRecordingSession As Boolean
    Public IgnoreLostDeviceForThisDrive As Boolean

    Public BackgroundLoopCounterNotUpdating As Boolean
    Public VideoCameraNotUpdating As Boolean

    Public WhatToDo As String
    Public ReadOnly BaseLocalDataPath As String = "C:\HB\"

    Public SaveSignalRegistrationMode As String
    Public SignalRegistrationMode As String

    Public ClevirAdministrator As Boolean
    Public ReadOnly AdminPassword As String = "poc"

    Public ReadOnly BlueBoxInfo(3) As String
    Public MasterTemplateName As String

    Public RecorderStopWatch As Stopwatch
    Public RecorderElapsedTime As TimeSpan

    Public INCACommCheckStopWatch As Stopwatch = Nothing
    Public INCACommCheckElapsedTime As TimeSpan
    Public INCACommCheckWarningTime As Integer
    Public INCACommIgnoreEvents As Boolean

    Public commCheckTimer As Timers.Timer = Nothing

    Public ActiveIncaApiCall As String
    Public RecordWAVTime As String
    Public BaseDataCollectionPath As String
    Public ProcessingInvalidSignalsLog As Boolean '5.6.2
    Public APICommErrorMsgDelayTime As String = "90"
    Public ReadOnly CLEVIRBaseDir As String = ""
    Public LiveSupportAvailability As Boolean
    Public CLEVIRAvailability As Boolean
    Public NetworkDrivePermission As Boolean
    Public Const RepeatWarningTimeInSeconds = 25
    Public ProcessEscalations As Boolean
    Public VehiclePTPLookupInfo As List(Of String)
    Public PATAC As Boolean = False
    Public TriggerWAVRecording As Boolean
    Public MaxCameras As Integer
    Public recordingAllowed As Boolean = False
    Public CurrentWAVFilename As String = ""
    Public CameraWaitTime As Integer = 10 'seconds
    Public CameraPingTime As Integer = 20 'seconds

    ''' <summary>
    ''' All configured camera position → IP mappings from config.xml
    ''' Key = Position (e.g., "FRONT"), Value = CameraConfig object
    ''' </summary>
    Public ConfiguredCameras As New Dictionary(Of String, CameraConfig)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>
    ''' Active cameras for the current vehicle (subset of ConfiguredCameras)
    ''' Populated by ReadVehicleConfigsFile() based on VehicleConfigurationsNF.csv
    ''' </summary>
    Public ActiveCameras As New List(Of CameraConfig)()

    ' ═══════════════════════════════════════════════════════════════════
    ' APPLICATION LIFECYCLE MANAGEMENT
    ' ═══════════════════════════════════════════════════════════════════

    ''' <summary>
    ''' ✅ Global flag indicating the application is exiting.
    ''' Used to prevent re-entrant calls to ExitApp and suppress UI operations during shutdown.
    ''' </summary>
    Public exitInProgress As Boolean = False

    ''' <summary>
    ''' ✅ Global flag indicating Initialize() is currently running.
    ''' Prevents recursive initialization calls during form lifecycle events.
    ''' </summary>
    Public initializationInProgress As Boolean = False

    Public Property CompressMF4 As Boolean = True
    Public Property CompressPCAP As Boolean = True
    Public Property CompressASC As Boolean = False
    Public Property CompressVSB As Boolean = False
    Public Property DeleteAfterCompression As Boolean = False  ' ← Safe default
    Public Property CompressionLevel As Integer = 1  ' Fast compression
    Public Property CompressionMaxRetries As Integer = 5
    Public Property CompressionRetryDelay As Integer = 10

    Public ZipCompressionEnabled As Boolean = True
    Public ZipCompressionMaxRetries As Integer = 5
    Public ZipCompressionRetryDelay As Integer = 10
    Public ZipFileLockTimeout As Integer = 30

    Private verifyConfigsCached As Boolean = False

    ''' <summary>
    ''' Cached current recording filename to avoid repeated reconstruction
    ''' Updated only when recording starts or file rotates
    ''' </summary>
    Public CachedRecordingFilename As String = ""

    ''' <summary>
    ''' Last known recording time (milliseconds) for rotation detection
    ''' </summary>
    Public LastKnownRecordingTimeMs As Integer = 0

    ''' <summary>
    ''' Timestamp of last filename update (for throttling checks)
    ''' </summary>
    Public LastFilenameUpdateTime As DateTime = DateTime.MinValue

    ' ═══════════════════════════════════════════════════════════════════
    ' PERFORMANCE OPTIMIZATION: Cache sequence info to avoid disk I/O
    ' ═══════════════════════════════════════════════════════════════════

    ''' <summary>
    ''' Cached base name from most recent GetCurrentRecordingInfo() call
    ''' Invalidated when sequence changes or recording stops
    ''' </summary>
    Private cachedRecordingBaseName As String = ""

    ''' <summary>
    ''' Cached sequence number from most recent GetCurrentRecordingInfo() call
    ''' Invalidated when sequence changes or recording stops
    ''' </summary>
    Private cachedRecordingSequence As Integer = 0

    ''' <summary>
    ''' Timestamp of last cache update (for invalidation logic)
    ''' </summary>
    Private cacheLastUpdated As DateTime = DateTime.MinValue

    ''' <summary>
    ''' Lock object for thread-safe cache access
    ''' </summary>
    Private ReadOnly recordingCacheLock As New Object()

    ' Fields to hold the recording objects
    Private waveIn As WaveInEvent
    Private waveFile As WaveFileWriter
    Private ReadOnly waveFileLock As New Object()
    Public audioFilePath As String

    ' Add to module initialization
    Private eventProcessingCts As CancellationTokenSource
    Private eventProcessingTask As Task

    ' Add monitoring for approaching MF4 boundaries
    Private lastMf4Check As DateTime = DateTime.MinValue
    Private mf4TransitionImminent As Boolean = False
    Private isProcessingBuffer As Boolean = False ' Flag to prevent re-entrancy

    ' Declare sequenceNumber as a class-level variable
    Private ReadOnly sequenceNumber As Integer = 0

    ' =====================================================================
    ' LiDAR Capture - N-Device Scalable Architecture
    ' =====================================================================
    ' Global master switch for LiDAR capture functionality
    Public LidarCaptureEnabled As Boolean = False

    ' Collection of configured LiDAR devices (supports multiple sensors)
    Public LidarDevices As New List(Of LidarDevice)

    ' Global flag indicating if any LiDAR device is currently capturing
    Public LidarCaptureStarted As Boolean = False

    ' Active SharedNicCapture instances (one per unique adapter GUID that hosts
    ' more than one LiDAR device).  Keyed by upper-case adapter GUID.
    Public SharedNicCaptures As New Dictionary(Of String, SharedNicCapture)(StringComparer.OrdinalIgnoreCase)

    ' ════════════════════════════════════════════════════════════
    ' Background Compression Task Tracking
    ' ════════════════════════════════════════════════════════════
    Public ActiveCompressionTasks As New Concurrent.ConcurrentBag(Of Task)
    Public CompressionTasksLock As New Object()

    ' ═══════════════════════════════════════════════════════════════════
    ' SESSION METADATA (Optional fields for enhanced search/reporting)
    ' ═══════════════════════════════════════════════════════════════════
    Public SaveEmailAddress As String = ""
    Public SaveGroupName As String = ""
    Public SaveProcedureName As String = ""

    ' Predefined dropdown values — loaded from SessionMetadata.xml; hardcoded values are fallback defaults.
    Public PredefinedGroups As String() = {
                                              "Front Impact",
                                              "Path/Route Following",
                                              "Comms & Diagnostics",
                                              "Advanced Integration",
                                              "Robustness",
                                              "Rear/Lateral Impact",
                                              "Fusion",
                                              "Driver Monitoring",
                                              "Speed /Traffic Control",
                                              "Other"
                                          }

    Public PredefinedProcedures As String() = {
                                                  "Development",
                                                  "Validation",
                                                  "Ride Trip",
                                                  "Other"
                                              }

    ''' <summary>
    ''' Loads session metadata defaults and predefined dropdown values from SessionMetadata.xml.
    ''' Falls back silently to hardcoded defaults if the file is missing or malformed.
    ''' </summary>
    Public Function LoadSessionMetadataConfig(filePath As String) As Boolean
        Try
            If Not File.Exists(filePath) Then
                HandleUserMessageLogging("Module1", $"LoadSessionMetadataConfig: File not found at {filePath} - using defaults")
                Return False
            End If

            Dim doc As XDocument = XDocument.Load(filePath)

            ' Load default session field values (empty string = no pre-population)
            Dim defaults = doc.<SessionMetadata>.<Defaults>.FirstOrDefault()
            If defaults IsNot Nothing Then
                SaveEmailAddress = If(defaults.<EmailAddress>.FirstOrDefault()?.Value, "")
                SaveGroupName = If(defaults.<GroupName>.FirstOrDefault()?.Value, "")
                SaveProcedureName = If(defaults.<ProcedureName>.FirstOrDefault()?.Value, "")
            End If

            ' Load predefined groups — only replace if the file contains at least one entry
            Dim groups = doc...<Group>.Select(Function(x) x.Value).Where(Function(v) Not String.IsNullOrWhiteSpace(v)).ToArray()
            If groups.Length > 0 Then PredefinedGroups = groups

            ' Load predefined procedures — same guard
            Dim procs = doc...<Procedure>.Select(Function(x) x.Value).Where(Function(v) Not String.IsNullOrWhiteSpace(v)).ToArray()
            If procs.Length > 0 Then PredefinedProcedures = procs

            HandleUserMessageLogging("Module1", $"LoadSessionMetadataConfig: Loaded {groups.Length} groups, {procs.Length} procedures from {filePath}")
            Return True

        Catch ex As Exception
            HandleUserMessageLogging("Module1", $"LoadSessionMetadataConfig: {ex.Message} - using defaults")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Fully shuts down all LiDAR devices, including unregistering from Hesai SDK.
    ''' Call this when application is exiting or when you want to fully release all LiDAR resources.
    ''' </summary>
    Public Sub ShutdownAllLidarDevices()
        Try
            ' ✅ Early exit if LiDAR not enabled (minor optimization)
            If Not LidarCaptureEnabled Then
                HandleUserMessageLogging("GMRC", "ShutdownAllLidarDevices: LiDAR not enabled - skipping")
                Return
            End If

            HandleUserMessageLogging("GMRC", "ShutdownAllLidarDevices: Shutting down all LiDAR devices...")

            If LidarDevices Is Nothing OrElse LidarDevices.Count = 0 Then
                HandleUserMessageLogging("GMRC", "ShutdownAllLidarDevices: No devices to shutdown")
                Return
            End If

            For Each device In LidarDevices
                Try
                    device.ShutdownDevice()  ' ✅ This handles StopCapture + UnregisterDevice
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"ShutdownAllLidarDevices: Error shutting down {device.DeviceId}: {ex.Message}")
                End Try
            Next

            ' ✅ Clean up global SDK state after all devices are unregistered
            If HesaiInterop.IsAvailable() Then
                HesaiInterop.Shutdown()
                HandleUserMessageLogging("GMRC", "ShutdownAllLidarDevices: Hesai SDK shutdown complete")
            End If

            LidarCaptureStarted = False
            HandleUserMessageLogging("GMRC", "ShutdownAllLidarDevices: Complete")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ShutdownAllLidarDevices: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Builds dynamic LiDAR header columns for CSV files
    ''' Returns empty string if LiDAR is not enabled
    ''' </summary>
    Private Function BuildLidarHeaders() As String
        'If LidarCaptureEnabled AndAlso LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 Then
        If LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 Then
            Dim headers As New List(Of String)
            For i As Integer = 1 To LidarDevices.Count
                headers.Add($"LiDAR[{i}]Frame")
            Next
            Return "," & String.Join(",", headers)
        End If
        Return String.Empty
    End Function

    ' =====================================================================
    ' Legacy Single-Device Proxy Properties (Backward Compatibility)
    ' These redirect to LidarDevices(0) to support existing code
    ' =====================================================================

    ''' <summary>
    ''' Network adapter GUID for the primary LiDAR device
    ''' Legacy property that maps to first device in collection
    ''' </summary>
    Public Property LidarAdapterGuid As String
        Get
            Return If(LidarDevices.Count > 0, LidarDevices(0).LidarAdapterGuid, "")
        End Get
        Set(value As String)
            If LidarDevices.Count = 0 Then
                LidarDevices.Add(New LidarDevice With {.DeviceId = "LiDAR1"})
            End If
            LidarDevices(0).LidarAdapterGuid = value
        End Set
    End Property

    ''' <summary>
    ''' IP address of the primary LiDAR sensor
    ''' Legacy property that maps to first device in collection
    ''' </summary>
    Public Property LidarIpAddress As String
        Get
            Return If(LidarDevices.Count > 0, LidarDevices(0).LidarIpAddress, "192.168.1.201")
        End Get
        Set(value As String)
            If LidarDevices.Count = 0 Then
                LidarDevices.Add(New LidarDevice With {.DeviceId = "LiDAR1"})
            End If
            LidarDevices(0).LidarIpAddress = value
        End Set
    End Property

    ''' <summary>
    ''' UDP port for LiDAR data packets
    ''' Legacy property that maps to first device in collection
    ''' </summary>
    Public Property LidarDataPort As UShort
        Get
            Return If(LidarDevices.Count > 0, LidarDevices(0).LidarDataPort, CUShort(2368))
        End Get
        Set(value As UShort)
            If LidarDevices.Count = 0 Then
                LidarDevices.Add(New LidarDevice With {.DeviceId = "LiDAR1"})
            End If
            LidarDevices(0).LidarDataPort = value
        End Set
    End Property

    ''' <summary>
    ''' UDP port for LiDAR IMU packets
    ''' Legacy property that maps to first device in collection
    ''' </summary>
    Public Property LidarImuPort As UShort
        Get
            Return If(LidarDevices.Count > 0, LidarDevices(0).LidarImuPort, CUShort(8308))
        End Get
        Set(value As UShort)
            If LidarDevices.Count = 0 Then
                LidarDevices.Add(New LidarDevice With {.DeviceId = "LiDAR1"})
            End If
            LidarDevices(0).LidarImuPort = value
        End Set
    End Property

    Public Enum EXCEL_DATA
        VariableName = 1
        DisplayName
        DeviceName
        Raster
        DefaultBackColor
        DefaultForeColor
        HighThreshBackColor
        LowThreshBackColor
        HighThreshForeColor
        LowThreshForeColor
        HighThresh
        LowThresh
        EqualTo
        Row
        Col
        CheckForDataChange
        DisplayWindowName
        DisplayWindowSize
        AssociatedControlName
        LocationOnForm
        GridSize
        AlsoAssociatedWith
        DisplayFormat
    End Enum

    Private Enum LogEventType
        AudioStart
        AudioStop
    End Enum

    Public Enum ExitOption
        None
        ExitClevirAndCloseInca
        ExitClevirOnly
        ExitClevirCloseIncaShutdownWindows
        CancelExit
    End Enum

    ' Variables for event buffering
    Private eventBuffer As New List(Of PendingEvent)
    Private eventBufferLock As New Object()

    Private Structure PendingEvent
        Public eventComment As String
        Public TimeCriticalInfo As String
        Public buttonPressTime As DateTime
        Public RetryCount As Integer
        Public MaxRetries As Integer
    End Structure

    ' ✅ NEW: Track last warning time to prevent spam
    Private _lastDiskSpaceWarning As DateTime = DateTime.MinValue
    Private Const DiskSpaceWarningCooldownMinutes As Integer = 15

    ''' <summary>
    ''' Checks available disk space and warns user if insufficient.
    ''' Can be called from any module in CLEVIR.
    ''' </summary>
    ''' <param name="targetPath">Path to check (file or directory)</param>
    ''' <param name="minimumSpaceGB">Minimum required free space in GB (default: 5)</param>
    ''' <param name="showDialog">Show user dialog if low space (default: True)</param>
    ''' <param name="allowOverride">Allow user to continue despite warning (default: True)</param>
    ''' <returns>True if space is adequate OR user chose to continue, False if user cancelled</returns>
    Public Function CheckDiskSpace(
    targetPath As String,
    Optional minimumSpaceGB As Double = 5.0,
    Optional showDialog As Boolean = True,
    Optional allowOverride As Boolean = True
) As Boolean

        Try
            ' ✅ Validate input path
            If String.IsNullOrWhiteSpace(targetPath) Then
                HandleUserMessageLogging("GMRC", "CheckDiskSpace: Invalid path provided")
                Return True ' Don't block on invalid path
            End If

            ' ✅ Get drive from path
            Dim drive As New DriveInfo(Path.GetPathRoot(targetPath))

            ' ✅ Convert to bytes for comparison
            Dim minimumSpaceBytes As Long = CLng(minimumSpaceGB * 1024L * 1024L * 1024L)

            ' ✅ Check free space
            If drive.AvailableFreeSpace < minimumSpaceBytes Then
                Dim freeSpaceGB As Double = drive.AvailableFreeSpace / (1024.0 ^ 3)

                ' Log the warning
                HandleUserMessageLogging("GMRC",
                $"⚠️ LOW DISK SPACE: {freeSpaceGB:F2} GB free on {drive.Name} (minimum: {minimumSpaceGB:F0} GB)")

                ' ✅ CHECK: Should we show warning? (Throttle to once every 15 minutes)
                Dim shouldWarn As Boolean = False
                If _lastDiskSpaceWarning = DateTime.MinValue Then
                    shouldWarn = True ' First time
                ElseIf DateTime.Now.Subtract(_lastDiskSpaceWarning).TotalMinutes >= DiskSpaceWarningCooldownMinutes Then
                    shouldWarn = True ' Cooldown expired
                End If

                If shouldWarn Then
                    _lastDiskSpaceWarning = DateTime.Now

                    ' ════════════════════════════════════════════════════════════
                    ' ✅ VOICE ALERT (Using existing pattern from ProcessKiller)
                    ' ════════════════════════════════════════════════════════════
                    Try
                        Dim synth As New SpeechSynthesizer()
                        synth.SelectVoice("Microsoft Zira Desktop")
                        synth.Rate = 0
                        synth.SpeakAsync("Low Disk Space Alert")
                    Catch ex As Exception
                        ' Don't fail disk check if voice fails
                        HandleUserMessageLogging("GMRC", $"Voice alert failed: {ex.Message}")
                    End Try

                    ' ════════════════════════════════════════════════════════════
                    ' ✅ TOAST NOTIFICATION (Non-blocking, always show)
                    ' ════════════════════════════════════════════════════════════
                    StatusNotifier.ToastSticky(
                    $"⚠️ LOW DISK SPACE: {freeSpaceGB:F2} GB free on {drive.Name}",
                    "DISK SPACE",
                    StatusNotifier.ToastKind.Warning,
                    ensureMainOnTop:=False
                )

                    ' ════════════════════════════════════════════════════════════
                    ' ✅ DIALOG (Only if requested AND not recording)
                    ' ════════════════════════════════════════════════════════════
                    If showDialog AndAlso Not MyIncaInterface.Recording Then
                        If allowOverride Then
                            ' Ask user if they want to continue
                            Dim result = MsgBox(
                            $"WARNING: Only {freeSpaceGB:F2} GB free on {drive.Name}." & vbCrLf &
                            $"Recommended minimum: {minimumSpaceGB:F0} GB." & vbCrLf & vbCrLf &
                            "Continue anyway?",
                            vbYesNo + vbExclamation,
                            "CLEVIR - Low Disk Space Warning"
                        )

                            If result = vbNo Then
                                HandleUserMessageLogging("GMRC", "User cancelled operation due to low disk space")
                                Return False ' User chose to abort
                            Else
                                HandleUserMessageLogging("GMRC", "User chose to continue despite low disk space")
                                Return True ' User overrode warning
                            End If
                        Else
                            ' No override allowed - show error and block
                            MsgBox(
                            $"ERROR: Insufficient disk space!" & vbCrLf & vbCrLf &
                            $"Available: {freeSpaceGB:F2} GB on {drive.Name}" & vbCrLf &
                            $"Required: {minimumSpaceGB:F0} GB" & vbCrLf & vbCrLf &
                            "Operation cancelled.",
                            vbCritical,
                            "CLEVIR - Insufficient Disk Space"
                        )
                            Return False ' Block operation
                        End If
                    End If
                End If

                ' Low space but user already notified recently
                Return True

            Else
                ' ✅ Sufficient space - log success for diagnostics (only once per session)
                Static loggedOnce As Boolean = False
                If Not loggedOnce Then
                    Dim freeSpaceGB As Double = drive.AvailableFreeSpace / (1024.0 ^ 3)
                    HandleUserMessageLogging("GMRC", $"Disk space check OK: {freeSpaceGB:F2} GB free on {drive.Name}")
                    loggedOnce = True
                End If
                Return True
            End If

        Catch ex As Exception
            ' Don't fail operations if disk check fails (network drives, etc.)
            HandleUserMessageLogging("GMRC", $"CheckDiskSpace error: {ex.Message}")
            Return True ' Allow operation to continue
        End Try
    End Function

    Public Sub DeleteUnusedDirectories(ByVal DirectoryName As String)

        'Called prior to uploading data.  Deletes any directory under the vehicle name directory path
        'that does not have any data in it.

        Dim dir As DirectoryInfo '= New DirectoryInfo(DirectoryName)
        Dim files As FileInfo()
        Dim dirs As DirectoryInfo()

        Dim x As Integer
        Dim y As Integer

        Dim UselessFiles As Integer

        Try

            If Directory.Exists(DirectoryName) Then

                dir = New DirectoryInfo(DirectoryName)

                dirs = dir.GetDirectories

                For x = 0 To UBound(dirs)
                    UselessFiles = 0
                    files = dirs(x).GetFiles
                    If files.Count = 0 Then
                        dirs(x).Delete()
                    ElseIf files.Count = 1 Then
                        For y = 0 To UBound(files)
                            If InStr(files(y).Name, "_ANNO.csv") > 0 Then
                                dirs(x).Delete(True)
                                Exit For
                            End If
                        Next

                    ElseIf files.Count = 2 Then
                        For y = 0 To UBound(files)
                            If InStr(files(y).Name, "_ANNO.csv") > 0 Then
                                UselessFiles += 1
                            End If
                            If files(y).Name = "GM_INCA_Comm.log" Then
                                UselessFiles += 1
                            End If
                        Next
                        If UselessFiles = 2 Then
                            dirs(x).Delete(True)
                        End If
                    End If

                Next

            Else
                'MsgBox(DirectoryName & " not found.  No recorded data available for this vehicle.")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "DeleteUnusedDirectories: ERROR " & ex.Message)
        End Try

    End Sub

    Public Function CheckForValidParameters(ByVal WorkspaceName As String) As Boolean

        'Called when Select Existing Workspace in INCA to Flash button is pressed on FlashingStatus form as well as on SoftwareVersionSelect_Load 
        'and when Refresh button is pressed on SoftwareVersionSelect form.

        'Returns true if workspace name matches the appropriate construct for the workspace to be selected by the user.
        'This so template workspaces, etc. are not made available to the user to select...

        Dim l_HWC As HardwareConfiguration

        CheckForValidParameters = True

        l_HWC = Get_Workspace(WorkspaceName, "CLEVIR Setup\Workspaces")

        If l_HWC IsNot Nothing Then

            If l_HWC.HasAssignedExperimentEnvironment = False And
                ((IsNumeric(Mid(WorkspaceName, 1, 3)) And Mid(WorkspaceName, 4, 1) = "_") Or
                (Mid(WorkspaceName, 1, 5) = "ACP2_") Or (Mid(WorkspaceName, 1, 5) = "ACP3_") Or
                 (Mid(WorkspaceName, 1, 5) = "ACP4_") Or (Mid(WorkspaceName, 1, 4) = "FCM_") Or
                 (Mid(WorkspaceName, 1, 7) = "FCM100_") Or (Mid(WorkspaceName, 1, 3) = "HC_") Or
                 (Mid(WorkspaceName, 1, 3) = "LC_") Or (Mid(WorkspaceName, 1, 6) = "CSAV2_")) Then
                CheckForValidParameters = False
            End If

        Else
            HandleUserMessageLogging("GMRC", "CheckForValidParameters: Get_Workspace could not find " & WorkspaceName,, )
        End If

    End Function

    Sub CopyFileToDDrive(ByVal DirName As String, ByVal Filename As String, ByVal AllFiles As Boolean)

        'Called from TriggerEncryptAndCopy which is called from EncryptFilesInDirectory.  Copies encrypted files to the Flash drive.
        'Note:  Drive letter designation may be something other than D: Drive...
        'Note:  Only used when running CLEVIR using a flash drive onto which the files will be copied...

        Dim myProcess As Process
        Dim ExecutableFile As String = "C:\csvscripts\robocopy.exe"
        Dim p As New ProcessStartInfo

        Dim RoboParams As String

        'run robocopy routine

        HandleUserMessageLogging("GMRC", "CopyFileToDrive: " & DirName & " - " & Filename)

        p.WindowStyle = ProcessWindowStyle.Hidden
        p.FileName = ExecutableFile

        'RoboParams = " /R:1 /move /s"
        'RoboParams = " /R:1 /mov"
        RoboParams = " /R:1"

        If Len(DirName) > 0 Then
            p.Arguments = BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & "\" & DirName & " " & NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & "\" & DirName & " " & Filename & RoboParams
        Else
            p.Arguments = BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & " " & NetworkDriveLetter & "\Data\gmcsv" & VehicleNumber & " " & Filename & RoboParams
        End If

        myProcess = Process.Start(p)

        'If AllFiles = True, we are exiting, so we will wait until copying is complete before exiting. If we are recording, we zip, encrypt and copy "on the fly" so 
        'we continue even if the copy is not yet complete...
        If AllFiles = True Then
            myProcess.WaitForExit()
        End If

    End Sub

    ''' <summary>
    ''' Verifies INCA configuration files (workspace, experiment, signal list, enumerations).
    ''' This is the centralized validation function called from InitForm.Drive().
    ''' </summary>
    Public Function VerifyConfigFiles(Optional ByVal WhoCalledMe As String = "") As Boolean
        ' ═══════════════════════════════════════════════════════════════
        ' CRITICAL: Use cached results to avoid expensive re-validation
        ' ═══════════════════════════════════════════════════════════════
        If verifyConfigsCached Then
            HandleUserMessageLogging("GMRC", $"VerifyConfigFiles: Using cached results (caller: {WhoCalledMe})")
            Return True
        End If

        Try
            HandleUserMessageLogging("GMRC", $"VerifyConfigFiles: Starting validation (called from {WhoCalledMe})...")

            ' ═══════════════════════════════════════════════════════════════
            ' STEP 1: Verify Workspace Exists in INCA
            ' ═══════════════════════════════════════════════════════════════
            Dim workspaceFound As Boolean = False
            AvailableWorkspaces = MyIncaInterface.GetAvailableWorkspaces()

            For Each ws As String In AvailableWorkspaces
                If String.Equals(ws, INCAWorkspace, StringComparison.OrdinalIgnoreCase) Then
                    workspaceFound = True
                    Exit For
                End If
            Next

            If Not workspaceFound Then
                HandleUserMessageLogging("GMRC",
                $"VerifyConfigFiles: Workspace '{INCAWorkspace}' not found in INCA database",
                DisplayMsgBox)
                Return False
            End If

            HandleUserMessageLogging("GMRC", $"✓ Workspace verified: {INCAWorkspace}")

            ' ═══════════════════════════════════════════════════════════════
            ' STEP 2: Verify Experiment Exists in INCA
            ' ═══════════════════════════════════════════════════════════════
            Dim experimentFound As Boolean = False
            AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames()

            For Each exp As String In AvailableExperimentNames
                If String.Equals(exp, INCAExperiment, StringComparison.OrdinalIgnoreCase) Then
                    experimentFound = True
                    Exit For
                End If
            Next

            If Not experimentFound AndAlso Not ConfigureForNewSoftwareVersion Then
                HandleUserMessageLogging("GMRC",
                $"VerifyConfigFiles: Experiment '{INCAExperiment}' not found in INCA database",
                DisplayMsgBox)
                Return False
            End If

            HandleUserMessageLogging("GMRC", $"✓ Experiment verified: {INCAExperiment}")

            ' ═══════════════════════════════════════════════════════════════
            ' STEP 3: Verify Signal List File Exists on Disk
            ' ═══════════════════════════════════════════════════════════════
            If Not File.Exists(INCAVariableFile) Then
                HandleUserMessageLogging("GMRC",
                $"VerifyConfigFiles: Signal list file '{INCAVariableFile}' not found on disk",
                DisplayMsgBox)
                Return False
            End If

            HandleUserMessageLogging("GMRC", $"✓ Signal list verified: {Path.GetFileName(INCAVariableFile)}")

            ' ═══════════════════════════════════════════════════════════════
            ' STEP 4: Extract Version/Model Year from Workspace Name
            ' ═══════════════════════════════════════════════════════════════
            GModelYear = DetermineModelYear(INCAWorkspace)
            GSoftwareVersion = DetermineSoftwareVersion(INCAWorkspace)
            GSpecificArxml = DetermineIfUsingDifferentArxml(INCAWorkspace)
            GWorkspaceIsForRtk = InStr(INCAWorkspace, "_RTK") > 0

            HandleUserMessageLogging("GMRC",
            $"✓ Extracted metadata: SW={GSoftwareVersion}, MY={GModelYear}, ARXML={GSpecificArxml}")

            ' ═══════════════════════════════════════════════════════════════
            ' STEP 5: Setup Enumerations File
            ' ═══════════════════════════════════════════════════════════════
            Dim enumFileName As String
            If InStr(FCMConfigName, "FCM") = 0 Then
                enumFileName = Path.Combine(My.Application.Info.DirectoryPath,
                $"{GSoftwareVersion}_{GModelYear}{GSpecificArxml}_Enumerations_{GProjectAbbreviation}.txt")
            Else
                enumFileName = Path.Combine(My.Application.Info.DirectoryPath,
                $"{GSoftwareVersion}_{GModelYear}{GSpecificArxml}_Enumerations_{FCMConfigName}.txt")
            End If

            ' If versioned enum file doesn't exist, create it
            If Not File.Exists(enumFileName) Then
                Dim genericEnumFile As String = Path.Combine(My.Application.Info.DirectoryPath, "Enumerations.txt")

                If File.Exists(genericEnumFile) Then
                    ' Backup generic file before renaming
                    Dim backupFile As String = Path.Combine(My.Application.Info.DirectoryPath, "Enumerations.txt.save")
                    If File.Exists(backupFile) Then File.Delete(backupFile)
                    File.Move(genericEnumFile, backupFile)
                End If

                ' Create new versioned enumerations file from A2L files
                HandleUserMessageLogging("GMRC", $"Creating enumerations file: {Path.GetFileName(enumFileName)}")
                CreateNewEnumerationFile(enumFileName)
            End If

            ' Always copy versioned file to generic name for runtime use
            Dim runtimeEnumFile As String = Path.Combine(My.Application.Info.DirectoryPath, "Enumerations.txt")
            File.Copy(enumFileName, runtimeEnumFile, overwrite:=True)

            HandleUserMessageLogging("GMRC", $"✓ Enumerations file ready: {Path.GetFileName(enumFileName)}")

            ' ═══════════════════════════════════════════════════════════════
            ' STEP 6: Store config info for LoginForm to display
            ' (LoginForm will read these variables when it loads)
            ' ═══════════════════════════════════════════════════════════════
            ' No need to update UI here - LoginForm will update itself when loaded

            ' ═══════════════════════════════════════════════════════════════
            ' SUCCESS: Cache result to avoid re-validation
            ' ═══════════════════════════════════════════════════════════════
            verifyConfigsCached = True
            HandleUserMessageLogging("GMRC", "VerifyConfigFiles: ✓ All checks passed")
            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"VerifyConfigFiles: {ex.Message} Exiting...", DisplayMsgBox)
            GmResidentClient.ExitApp("Complete")
            Return False
        End Try
    End Function

    Public Function ReadConfigFile() As Boolean
        ' This function now acts as a simple wrapper around the centralized ReadConfiguration helper.
        Dim configFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "Config.xml")
        HandleUserMessageLogging("Module1", "ReadConfigFile: Reading Master Config File " & configFilePath & "...")

        ' Call the centralized helper located in the main form class
        Return GmResidentClient.ReadConfiguration(configFilePath)
    End Function

    ''' <summary>
    ''' Reads camera configuration from config.xml and populates ConfiguredCameras dictionary.
    ''' Called by GmResidentClient.ReadConfiguration() during startup.
    ''' </summary>
    Public Sub ReadCameraConfiguration(cameraConfigNode As XmlNode)
        Try
            If cameraConfigNode Is Nothing Then
                HandleUserMessageLogging("GMRC", "No CameraConfiguration found in config.xml - using defaults")
                LoadDefaultCameraConfig()
                Return
            End If

            ' Clear existing camera configs
            ConfiguredCameras.Clear()

            ' Read MaxCameras
            Dim maxCamerasNode = cameraConfigNode.SelectSingleNode("MaxCameras")
            If maxCamerasNode IsNot Nothing Then
                MaxCameras = Integer.Parse(maxCamerasNode.InnerText)
            Else
                MaxCameras = 9 ' Default
            End If

            ' Read camera mappings
            Dim camerasNode = cameraConfigNode.SelectSingleNode("Cameras")
            If camerasNode IsNot Nothing Then
                For Each cameraNode As XmlNode In camerasNode.SelectNodes("Camera")
                    Dim position As String = cameraNode.Attributes("position")?.Value
                    Dim ipAddress As String = cameraNode.Attributes("ipAddress")?.Value
                    Dim enabledStr As String = cameraNode.Attributes("enabled")?.Value
                    Dim enabled As Boolean = String.IsNullOrEmpty(enabledStr) OrElse Boolean.Parse(enabledStr)

                    If Not String.IsNullOrEmpty(position) AndAlso Not String.IsNullOrEmpty(ipAddress) Then
                        ConfiguredCameras(position) = New CameraConfig(position, ipAddress, enabled)
                        HandleUserMessageLogging("GMRC", $"Camera config loaded: {position} → {ipAddress}")
                    End If
                Next
            End If

            ' Read camera validation settings
            Dim waitTimeNode = cameraConfigNode.SelectSingleNode("InitialWaitTime")
            If waitTimeNode IsNot Nothing Then
                CameraWaitTime = Integer.Parse(waitTimeNode.InnerText)
            End If

            Dim pingTimeNode = cameraConfigNode.SelectSingleNode("PingTimeout")
            If pingTimeNode IsNot Nothing Then
                CameraPingTime = Integer.Parse(pingTimeNode.InnerText)
            End If

            HandleUserMessageLogging("GMRC", $"Loaded {ConfiguredCameras.Count} camera configurations from config.xml")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ReadCameraConfiguration error: {ex.Message} - using defaults")
            LoadDefaultCameraConfig()
        End Try
    End Sub

    ''' <summary>
    ''' Loads default camera IP mappings if config.xml is missing camera section.
    ''' Provides backward compatibility with hardcoded addresses.
    ''' </summary>
    Public Sub LoadDefaultCameraConfig()
        ConfiguredCameras.Clear()
        ConfiguredCameras("FRONT") = New CameraConfig("FRONT", "192.168.40.101")
        ConfiguredCameras("RIGHTREAR") = New CameraConfig("RIGHTREAR", "192.168.40.102")
        ConfiguredCameras("LEFTREAR") = New CameraConfig("LEFTREAR", "192.168.40.103")
        ConfiguredCameras("HMI") = New CameraConfig("HMI", "192.168.40.104")
        ConfiguredCameras("LEFTFRONT") = New CameraConfig("LEFTFRONT", "192.168.40.105")
        ConfiguredCameras("RIGHTFRONT") = New CameraConfig("RIGHTFRONT", "192.168.40.106")
        ConfiguredCameras("LABEL1") = New CameraConfig("LABEL1", "192.168.40.107")
        ConfiguredCameras("LABEL2") = New CameraConfig("LABEL2", "192.168.40.108")
        ConfiguredCameras("LABEL3") = New CameraConfig("LABEL3", "192.168.40.109")

        HandleUserMessageLogging("GMRC", "Loaded 9 default camera configurations")
    End Sub

    Public Sub WriteConfigFile()
        ' This function now acts as a simple wrapper around the centralized WriteConfiguration helper.
        Dim configFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "Config.xml")
        HandleUserMessageLogging("Module1", "WriteConfigFile: Writing Master Config File " & configFilePath & "...")

        ' Call the centralized helper located in the main form class
        GmResidentClient.WriteConfiguration(configFilePath)
    End Sub

    Function SetAvailability(ByVal myHostNameDirectory As String, ByVal myAvailability As Boolean, Optional ByVal myButton As Button = Nothing) As Boolean

        'The CLEVIR Administrator can display the CLEVIR VehicleStatDashboard and see which computers are currently running CLEVIR and connected to GM Network.
        'SetAvailability function is called to switch the available state of the computer between true and false.  It does this by writing to the availability.txt
        'file for the specific computer on the share drive...

        Dim fnum As Integer

        Try

            'bypass functionality in here - for PATAC
            If PATAC = True Then
                Exit Function
            End If

            'Do not set availability flag if the computer is running as ClevirAdministrator unless CLEVIR VehicleStatDashboard has called
            'this function, which is the case if myButton has been passed in as optional parameter...
            If ClevirAdministrator = True And myButton Is Nothing Then
                SetAvailability = False
                Exit Function
            End If

            HandleUserMessageLogging("GMRC", "SetAvailability: Setting availability to " & myAvailability)

            If myAvailability = True Then
                If myButton IsNot Nothing Then
                    myButton.Text = "Available"
                    myButton.BackColor = Color.LightGreen
                End If

            Else
                If myButton IsNot Nothing Then
                    myButton.BackColor = SystemColors.Control
                    myButton.Text = "Un-Available"
                End If

            End If

            If NetworkDrivePermission = False Then
                HandleUserMessageLogging("GMRC", "SetAvailability: Could not access " & NetworkDriveMapping & CLEVIRBaseDir & " SetAvailability set to False. Exiting...")
                SetAvailability = False
                Exit Function
            End If

            If Directory.Exists(NetworkDriveMapping & CLEVIRBaseDir) Then

                If Not Directory.Exists(myHostNameDirectory) Then
                    Directory.CreateDirectory(myHostNameDirectory)
                End If

                fnum = FreeFile()

                FileOpen(fnum, My.Application.Info.DirectoryPath & "\Availability.txt", OpenMode.Output)
                PrintLine(fnum, myAvailability.ToString)

                FileClose(fnum)

                File.Copy(My.Application.Info.DirectoryPath & "\availability.txt", myHostNameDirectory & "\availability.txt", True)

                HandleUserMessageLogging("GMRC", "SetAvailability: Availability set to " & myAvailability)

            Else
                HandleUserMessageLogging("GMRC", "SetAvailability: Network Path Not Found.  Cannot set Availability to " & myAvailability & "...")
            End If

            SetAvailability = myAvailability

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "SetAvailability: " & ex.Message & " SetAvailability set to False",, )
            SetAvailability = False
        End Try

    End Function

    Public Function CheckAvailability(ByVal myHostNameDirectory As String) As Boolean

        'Checks to see if the CLEVIR Administrator is currently running the VehicleStatusDashboard and is available to support.
        'If so, the availability.txt file on the share drive in the myHostNameDirectory will contain "True" and CheckAvailability
        'will return True.  This will cause a button (Request Assistance) to appear in the upper right of the user computer (see calling function).

        Dim fnum As Integer
        Dim textline As String

        'bypass functionality in here - for PATAC
        If PATAC = True Then
            Exit Function
        End If

        Try
            If Not Directory.Exists(myHostNameDirectory) Then
                CheckAvailability = False
                Exit Function
            End If

            If Not FileInUse(myHostNameDirectory & "\Availability.txt") Then

                fnum = FreeFile()

                FileOpen(fnum, myHostNameDirectory & "\Availability.txt", OpenMode.Input)
                textline = LineInput(fnum)

                CheckAvailability = UCase(textline) = "TRUE"

                FileClose(fnum)

            Else
                CheckAvailability = False
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckAvailability: " & ex.Message & " CheckAvailability set to False",, )
            CheckAvailability = False
        End Try

    End Function

    ' Sub WriteGMRCLogFileToHostNameDir()

    ''Writes the GM_ResidentClient.log file to a computer hostname specific folder on the share drive.  Called when CLEVIR is first
    ''launched and when it is exited.  This was done because the computers are no longer staying with the vehicles, so we need
    ''computer specific log files in addition to log files based on vehicle number.  We are retaining vehicle number based log files
    ''for now, but they do not really make sense anymore because log files from different computers will overwrite the vehicle specific
    ''log files on the share drive and we will lose log file data.  Searching based on hostname based log files has been added
    ''to the VehicleStatDashboard as well...

    'Dim myHostNameDirectory As String

    '    Try

    '        'bypass functionality in here - for PATAC
    '        If PATAC = True Or ClevirAdministrator = True Then
    '            Exit Sub
    '        End If

    '        HandleUserMessageLogging("GMRC", "WriteGMRCLogFileToHostNameDir: Writing Log File to " & CLEVIRBaseDir & "\Development\PC_HostNameLogFiles\" & hostname & " directory on share drive.")

    '        If NetworkDrivePermission = False Then
    '            HandleUserMessageLogging("GMRC", "WriteGMRCLogFileToHostNameDir: Could not access " & NetworkDriveMapping & CLEVIRBaseDir & ". Exiting...")
    '            Exit Sub
    '        End If

    '        myHostNameDirectory = NetworkDriveMapping & CLEVIRBaseDir & "\Development\PC_HostNameLogFiles\" & hostname

    '        If Directory.Exists(NetworkDriveMapping & CLEVIRBaseDir) Then

    '            If Not Directory.Exists(myHostNameDirectory) Then
    '                Directory.CreateDirectory(myHostNameDirectory)
    '            End If

    '            File.Copy(My.Application.Info.DirectoryPath & "\GM_ResidentClient.log", myHostNameDirectory & "\GM_ResidentClient.log", True)

    '            'If File.Exists(My.Application.Info.DirectoryPath & "\GM_INCA_Comm.log") Then
    '            'File.Copy(My.Application.Info.DirectoryPath & "\GM_INCA_Comm.log", myHostNameDirectory & "\GM_INCA_Comm.log", True)
    '            'End If

    '        End If


    '    Catch ex As Exception
    '        HandleUserMessageLogging("GMRC", "WriteGMRCLogFileToHostNameDir: " & ex.Message,, )
    '    End Try

    'End Sub

    Sub UpdateINCAWithLatestExperiment(ByVal ExperimentName As String, ByVal CSVFileInFolder As Boolean, Optional ByRef mismatch As Boolean = False)
        Try
            HandleUserMessageLogging("GMRC", "UpdateINCAWithLatestExperiment...")
            GmResidentClient.TopMost = True

            Dim FilenameToChange As String
            If SaveLoginID.ToUpperInvariant() = "DEMO" OrElse String.IsNullOrEmpty(SaveLoginID) Then
                FilenameToChange = "config.xml"
            Else
                FilenameToChange = SaveLoginID & ".xml"
            End If

            Dim ReturnStr As String = MyIncaInterface.ConnectToInca()

            If ReturnStr = "True" Then
                If AvailableExperimentNames Is Nothing Then
                    AvailableExperimentNames = MyIncaInterface.GetAvailableExperimentNames()
                End If

                Dim BypassImport As Boolean = False

                For Each exp In AvailableExperimentNames
                    If exp = Path.GetFileNameWithoutExtension(ExperimentName) Then
                        Dim reimport As Boolean = StatusNotifier.Confirm($"Experiment {Path.GetFileNameWithoutExtension(ExperimentName)} already in INCA. Do you want to re-import?", "INCA")
                        If Not reimport Then
                            BypassImport = True
                        End If
                        Exit For
                    End If
                Next

                Dim success As Boolean = False
                If Not BypassImport Then
                    HandleUserMessageLogging("GMRC", "UpdateINCAWithLatestExperiment: Importing Experiment " & Path.GetFileNameWithoutExtension(ExperimentName) & " into INCA.")
                    success = ImportFileIntoINCA(ExperimentName, True, False)
                    UserStatusInfo.Hide()
                    If success Then
                        StatusNotifier.Toast($"Imported {Path.GetFileNameWithoutExtension(ExperimentName)} into INCA.", "INCA", durationMs:=1000, ensureMainOnTop:=False)
                    End If
                End If

                If success OrElse BypassImport Then
                    OnVehicleScreen.TopMost = True
                    HandleUserMessageLogging("GMRC", "UpdateINCAWithLatestExperiment: Updated Experiment " & Path.GetFileNameWithoutExtension(ExperimentName) & " is available in INCA.  Do you want to update the " & FilenameToChange & " configuration file? Question Asked...")

                    If StatusNotifier.Confirm($"Updated Experiment {Path.GetFileNameWithoutExtension(ExperimentName)} is available in INCA. Do you want to update the {FilenameToChange} configuration file?", "INCA") Then
                        HandleUserMessageLogging("GMRC", "UpdateINCAWithLatestExperiment: User Responded Yes...")
                        mismatch = False

                        Dim configFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, FilenameToChange)
                        If File.Exists(configFilePath) Then
                            Dim lines As List(Of String) = File.ReadAllLines(configFilePath).ToList()

                            If lines.Count > 2 Then
                                lines(2) = "INCAExperiment" & vbTab & Path.GetFileNameWithoutExtension(ExperimentName)
                                INCAExperiment = Path.GetFileNameWithoutExtension(ExperimentName)
                                InitialINCAExperiment = INCAExperiment
                            End If

                            If lines.Count > 3 Then
                                Dim baseFileName As String = Path.GetFileName(ExperimentName)
                                Dim extIndex As Integer = baseFileName.IndexOf("."c)
                                Dim baseName As String = If(extIndex > 0, baseFileName.Substring(0, extIndex), baseFileName)

                                Dim variableFile As String
                                If Not CSVFileInFolder Then
                                    variableFile = Path.Combine(My.Application.Info.DirectoryPath, "SignalLists", baseName & ".xlsx")
                                Else
                                    variableFile = Path.Combine(My.Application.Info.DirectoryPath, "SignalLists", baseName & ".csv")
                                End If

                                lines(3) = "INCAVariableFile" & vbTab & variableFile
                                INCAVariableFile = variableFile
                            End If

                            File.WriteAllLines(configFilePath, lines)
                            HandleUserMessageLogging("GMRC", "UpdateINCAWithLatestExperiment: Config File " & FilenameToChange & " has been updated.")
                            StatusNotifier.Toast($"{FilenameToChange} updated.", "INCA", durationMs:=1000, ensureMainOnTop:=False)
                        Else
                            HandleUserMessageLogging("GMRC", "UpdateINCAWithLatestExperiment: Config File " & FilenameToChange & " not found.", DisplayMsgBox)
                        End If
                    Else
                        INCAExperiment = InitialINCAExperiment
                        HandleUserMessageLogging("GMRC", "UpdateINCAWithLatestExperiment: User Responded No: INCAExperiment = " & INCAExperiment)
                        StatusNotifier.Toast("Config update canceled.", "INCA", durationMs:=1000, ensureMainOnTop:=False)
                    End If

                    OnVehicleScreen.TopMost = False
                Else
                    HandleUserMessageLogging("GMRC", "UpdateINCAWithLatestExperiment: Import Experiment " & ExperimentName & " into INCA Failed...",)
                    StatusNotifier.Error($"Import failed: {Path.GetFileNameWithoutExtension(ExperimentName)}", "INCA")
                    GmResidentClient.TopMost = False
                    Exit Sub
                End If
            Else
                Dim tempstr As String
                If ReturnStr.Contains("Experiment") Then
                    tempstr = "Could Not initialize INCA - Please Check config.xml file for correct Experiment name. Exiting..."
                ElseIf ReturnStr.Contains("Workspace") Then
                    tempstr = "Could Not initialize INCA - Please Check config.xml file for correct Workspace name. Exiting..."
                ElseIf ReturnStr.Contains("Database") Then
                    tempstr = "Could Not initialize INCA - Please Check config.xml file for correct INCA Database name. Exiting..."
                Else
                    tempstr = "Could Not initialize INCA - Please Check config.xml file for correct INCA Database name, Workspace name and Experiment name. Exiting..."
                End If

                HandleUserMessageLogging("GMRC", tempstr, DisplayMsgBox)
                GmResidentClient.ExitApp("Complete")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "UpdateINCAWithLatestExperiment: " & ex.Message, DisplayMsgBox, )
        Finally
            GmResidentClient.TopMost = False
        End Try
    End Sub

    Public Sub CopySavedCustomAnnoFilesToCLEVIRFolder()

        'Not Currently used...

        'Called from ExitApp...
        'copies the custom annotation files for a particular vehicle to the q drive

        HandleUserMessageLogging("GMRC", "Copying Saved Custom Annotation Files to CLEVIR folder on Share Drive...",,, FlashMsgOn)

        Dim directoryPath As String = My.Application.Info.DirectoryPath

        If Directory.Exists(directoryPath) Then
            Dim files As String() = Directory.GetFiles(directoryPath, "*SavedCustomAnnotations.txt")

            For Each filePath In files
                Dim fileName As String = Path.GetFileName(filePath)

                If Directory.Exists(NetworkDriveLetter & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations\" & VehicleNumber) Then
                    File.Copy(filePath, NetworkDriveLetter & CLEVIRBaseDir & "\Development\CLEVIR Vehicle Configurations\" & VehicleNumber & "\" & fileName, True)
                End If
            Next
        End If

        UserStatusInfo.Hide()
    End Sub

    Public Function RemoveDoubleQuotes(ByVal textstring As String) As String

        'Called from ParseA2lFile:  If file contains double double quotes, it removes these and creates a string
        'with a space between two strings.  This is required due to formatting specific to our a2l files...
        Dim inputarray() As String

        inputarray = Split(textstring, """")

        RemoveDoubleQuotes = inputarray(0) & " " & inputarray(1)

    End Function

    'Public Function PreLoadVoiceCommandsArray() As Integer

    '    'loads the voice command array with the predetermined hard coded commands that are
    '    'currently supported.  This array is added to in the ParseDataDictionary routine
    '    'by adding all of the commands associated with the data dictionary annotation button info.

    '    ReDim  myVoiceCommands(16)

    '    myVoiceCommands(0) = "start record"
    '    myVoiceCommands(1) = "stop record"
    '    myVoiceCommands(2) = "start audio"
    '    myVoiceCommands(3) = "disable"
    '    myVoiceCommands(4) = "start measurement"
    '    myVoiceCommands(5) = "stop measurement"
    '    'myVoiceCommands(6) = "display operation history"
    '    'myVoiceCommands(7) = "hide operation history"
    '    'myVoiceCommands(8) = "display main"
    '    myVoiceCommands(6) = "Zoom In"
    '    myVoiceCommands(7) = "Zoom Out"
    '    myVoiceCommands(8) = "Zoom One Hundred"
    '    myVoiceCommands(9) = "Drive"
    '    myVoiceCommands(10) = "Upload Data"
    '    myVoiceCommands(11) = "Secret Squirrel"

    '    myVoiceCommands(12) = "Actions"
    '    myVoiceCommands(13) = "Login"
    '    myVoiceCommands(14) = "Displays"
    '    myVoiceCommands(15) = "Miscellaneous"
    '    'myVoiceCommands(19) = "Custom INCA Setup"

    '    myVoiceCommands(16) = "Annotate"

    '    PreLoadVoiceCommandsArray = UBound(myVoiceCommands) + 1

    'End Function

    Public Sub ParseDataDictionary()
        ' Ensure main tab control is ready
        'GmResidentClient.InitializeAndSetupMainTabControl()
        ' ================================================================
        ' CRITICAL: Ensure main tab control exists before parsing
        ' ================================================================

        If GmResidentClient Is Nothing Then
            HandleUserMessageLogging("GMRC", "ParseDataDictionary: GmResidentClient is Nothing - aborting")
            Return
        End If

        ' Uncomment this line to initialize the TabControl if it doesn't exist
        If GmResidentClient.MyMainTabControl Is Nothing Then
            HandleUserMessageLogging("GMRC", "ParseDataDictionary: Initializing MyMainTabControl...")
            GmResidentClient.InitializeAndSetupMainTabControl()

            ' Double-check it was created successfully
            If GmResidentClient.MyMainTabControl Is Nothing Then
                HandleUserMessageLogging("GMRC", "ParseDataDictionary: Failed to create MyMainTabControl - aborting", DisplayMsgBox)
                Return
            End If
        End If


        ' Access the singleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        ' Clear existing data in SubTabs and AnnotationValueRecords
        dataDictionary.SubTabs.Clear()
        dataDictionary.AnnotationValueRecords.Clear()

        Try
            HandleUserMessageLogging("GMRC", "ParseDataDictionary: Parsing DataDictionary...")
            'StatusNotifier.Toast("ParseDataDictionary: Parsing DataDictionary...", "CONFIG", durationMs:=1000, ensureMainOnTop:=False)
            'OnVehicleScreen.Refresh()

            ' File read logic
            Dim filename As String = GetDataDictionaryFilename()
            If String.IsNullOrEmpty(filename) Then Exit Sub

            ' Clear existing pages and create the main "ANNOTATION" tab
            GmResidentClient.MyMainTabControl.TabPages.Clear()
            Dim annotationTab As New TabPage("ANNOTATION") With {.Visible = True, .Enabled = True}
            GmResidentClient.MyMainTabControl.TabPages.Add(annotationTab)

            ' Create a nested TabControl for sub-tabs within the "ANNOTATION" tab
            Dim subTabControl As New TabControl With {
               .Dock = DockStyle.Fill
           }
            annotationTab.Controls.Add(subTabControl)

            ' Read and parse CSV
            Using reader As New StreamReader(filename)
                ' Skip the header line
                reader.ReadLine()

                ' Read each subsequent line
                While Not reader.EndOfStream
                    Dim line As String = reader.ReadLine()
                    Dim fields As List(Of String) = ParseCSVLine(line)

                    ' Extract data fields with safety checks
                    If fields.Count < 4 Then
                        'Console.WriteLine($"Skipping line due to insufficient fields: {line}")
                        Continue While
                    End If

                    ' Extract data fields
                    Dim subTabName As String = fields(1).Trim()
                    Dim subTabID As Integer = Integer.Parse(fields(2).Trim())
                    Dim buttonName As String = fields(3).Trim()
                    Dim buttonID As Integer = Integer.Parse(fields(4).Trim())
                    Dim subCategories As New List(Of String)

                    ' Add sub-categories if available
                    For i As Integer = 5 To Math.Min(fields.Count - 1, 10)
                        Dim subCategory = fields(i).Trim()
                        If Not String.IsNullOrEmpty(subCategory) Then
                            subCategories.Add(subCategory)
                        End If
                    Next

                    ' Retrieve or create the SubTab and TabPage
                    Dim subTab As TabPage
                    If Not dataDictionary.SubTabs.ContainsKey(subTabName) Then
                        ' Create new SubTab in DataDictionarySingleton
                        Dim newSubTab = New DataDictionarySingleton.SubTab(subTabName, subTabID)
                        dataDictionary.SubTabs(subTabName) = newSubTab

                        ' Create the corresponding TabPage for UI
                        subTab = New TabPage(subTabName)
                        subTabControl.TabPages.Add(subTab)
                    Else
                        ' Retrieve the existing TabPage from subTabControl by name
                        subTab = subTabControl.TabPages.Cast(Of TabPage)().FirstOrDefault(Function(t) t.Text = subTabName)
                    End If

                    ' Ensure subTab is not Nothing before adding buttons
                    If subTab IsNot Nothing Then
                        ' Create and add the EventButton to the SubTab in singleton
                        Dim eventButton As New DataDictionarySingleton.EventButton(buttonName, buttonID, subCategories)
                        dataDictionary.SubTabs(subTabName).EventButtons.Add(eventButton)

                        ' Create button control for the UI
                        Dim button As New Button With {
                           .Text = buttonName,
                           .Width = DataDictionarySingleton.DefaultButtonWidth,
                           .Height = DataDictionarySingleton.DefaultButtonHeight
                       }

                        ' Add the centralized event handler
                        AddHandler button.Click, Sub(sender As Object, e As EventArgs)
                                                     HandleAnnotationButtons(sender, subTabName, buttonID)
                                                 End Sub

                        ' Add button to the TabPage in the UI
                        subTab.Controls.Add(button)

                        ' Arrange buttons within the sub-tab
                        ArrangeButtonsInTab(subTab, DataDictionarySingleton.DefaultButtonWidth, DataDictionarySingleton.DefaultButtonHeight, DataDictionarySingleton.HorizButtonSpacing, DataDictionarySingleton.VertButtonSpacing, DataDictionarySingleton.NumButtonsAcross)

                        ' Create and populate a new AnnotationValueRecord based on the parsed data
                        Dim annotationValueRecord As New DataDictionarySingleton.AnnotationValueRecord With {
                           .RecordType = 2, ' Assuming a constant RecordType for annotation values
                           .TypeId = subTabID, ' Use Sub-tab ID as TypeID
                           .Id = buttonID, ' Use Event Button ID as the unique ID
                           .EnumerationType = subTabID, ' Set EnumerationType to Sub-tab ID
                           .Description = buttonName, ' Set Description to Event Button Name
                           .SubTabName = subTabName ' Set SubTabName to Sub-tab Name
                       }

                        ' Add the new AnnotationValueRecord to AnnotationValueRecords list in DataDictionarySingleton
                        dataDictionary.AnnotationValueRecords.Add(annotationValueRecord)

                    End If
                End While
            End Using

            ' Call the file preparation function after parsing
            PrepareAnnotationFiles()

            ' Final UI update with SizeAnnotationButtons to apply parsed data
            GmResidentClient.SizeAnnotationButtons(dataDictionary.SubTabs)

            ' After parsing, initialize sub-tabs and buttons
            'dataDictionary.InitializeSubTabsAndButtons()

            HandleUserMessageLogging("GMRC", "ParseDataDictionary: Parsing Data Dictionary Complete")
            'StatusNotifier.Toast("ParseDataDictionary: Parsing Data Dictionary Complete", "CONFIG", durationMs:=1000, ensureMainOnTop:=False)
            'OnVehicleScreen.Refresh()

        Catch ex As NullReferenceException
            HandleUserMessageLogging("GMRC", $"ParseDataDictionary NullReferenceException: {ex.Message} - StackTrace: {ex.StackTrace}", DisplayMsgBox)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ParseDataDictionary Error -{ex.Message}", DisplayMsgBox)
        End Try
    End Sub

    Private Sub ArrangeButtonsInTab(tab As TabPage, buttonWidth As Integer, buttonHeight As Integer, horizontalSpacing As Integer, verticalSpacing As Integer, buttonsPerRow As Integer)
        Dim topOffset As Integer = DataDictionarySingleton.DefaultButtonTop
        Dim leftOffset As Integer = DataDictionarySingleton.DefaultButtonLeft
        Dim buttonIndex As Integer = 0

        For Each ctrl As Control In tab.Controls
            Dim button As Button = TryCast(ctrl, Button)
            If (button IsNot Nothing) Then

                ' Position button within the tab page
                button.Top = topOffset
                button.Left = leftOffset

                ' Update leftOffset for next button in the row
                leftOffset += buttonWidth + horizontalSpacing
                buttonIndex += 1

                ' Move to next row if limit per row is reached
                If buttonIndex >= buttonsPerRow Then
                    leftOffset = DataDictionarySingleton.DefaultButtonLeft
                    topOffset += buttonHeight + verticalSpacing
                    buttonIndex = 0
                End If
            End If
        Next
    End Sub

    Private Function GetDataDictionaryFilename() As String
        If Len(AnnotationDataDictionaryFile) = 0 Then
            HandleValidationDataDictionary()
            Return My.Application.Info.DirectoryPath & "\DataDictionary.csv"
        Else
            FileCopy(AnnotationDataDictionaryFile, AnnotationDataDictionaryFile & ".SAVE")
            Return AnnotationDataDictionaryFile
        End If
    End Function

    Private Sub HandleValidationDataDictionary()
        If Not File.Exists(My.Application.Info.DirectoryPath & "\VALIDATION_DataDictionary.csv") Then
            If CurrentVehicleUsage <> "DEVELOPMENT" Then
                HandleMissingValidationDataDictionary()
            End If
            CopyProjectDataDictionary()
        Else
            CopyValidationDataDictionary()
        End If
    End Sub

    Private Sub HandleMissingValidationDataDictionary()
        If OriginalVehicleConfiguration = "VALIDATION" Or OriginalVehicleConfiguration = "UNDEFINED" Or OriginalVehicleConfiguration = "VISTOOL" Then
            Dim message As String = "If the intended vehicle usage Is For VALIDATION, there should be a file called VALIDATION_DataDictionary.csv In the " & My.Application.Info.DirectoryPath & " Directory. This file was Not found. Do you wish To Continue Using the " & ProjectName & " Data Dictionary?"
            If Not StatusNotifier.Confirm(message, "CLEVIR") Then
                HandleUserMessageLogging("GMRC", "ParseDataDictionary: User Answered No, Exiting")
                GmResidentClient.ExitApp("Complete")
            End If
        End If
    End Sub

    Private Sub CopyProjectDataDictionary()
        Dim projectDataDictionary As String = My.Application.Info.DirectoryPath & "\" & ProjectName & "_DataDictionary.csv"
        If File.Exists(projectDataDictionary) Then
            File.Copy(projectDataDictionary, My.Application.Info.DirectoryPath & "\DataDictionary.csv", True)
        End If
    End Sub

    Private Sub CopyValidationDataDictionary()
        Dim validationDataDictionary As String = My.Application.Info.DirectoryPath & "\VALIDATION_DataDictionary.csv"
        File.Copy(validationDataDictionary, My.Application.Info.DirectoryPath & "\DataDictionary.csv", True)
    End Sub

    Private Sub PrepareAnnotationFiles()
        Dim dataDictionary = DataDictionarySingleton.GetInstance() ' Get the Singleton instance

        If dataDictionary.SubTabs IsNot Nothing AndAlso dataDictionary.SubTabs.Count > 0 Then
            HandleUserMessageLogging("GMRC", "Preparing Annotation Files For Each EventButton...",,,, Nothing)

            ' Loop through each sub-tab in SubTabs
            For Each subTabEntry In dataDictionary.SubTabs
                Dim subTabName As String = subTabEntry.Key
                Dim subTab As DataDictionarySingleton.SubTab = subTabEntry.Value

                ' Build the file path using Path.Combine
                Dim saveCustomAnnoFileName As String = Path.Combine(My.Application.Info.DirectoryPath, subTabName & "_SavedCustomAnnotations.txt")

                ' Check if the file already exists
                If Not File.Exists(saveCustomAnnoFileName) Then
                    ' Create and write to the file using a StreamWriter
                    Using writer As New StreamWriter(saveCustomAnnoFileName)
                        ' Loop through each EventButton in the current SubTab to write names
                        For Each eventButton In subTab.EventButtons
                            ' Skip if the button name is "ANNOTATION"
                            If String.Equals(eventButton.ButtonName, "ANNOTATION", StringComparison.OrdinalIgnoreCase) Then
                                Continue For
                            End If

                            ' Write the base button name on its own line
                            writer.WriteLine(eventButton.ButtonName & " - ")

                            ' If there are subcategories, write each one on a separate line
                            If eventButton.SubCategories IsNot Nothing AndAlso eventButton.SubCategories.Count > 0 Then
                                For Each subCategory As String In eventButton.SubCategories
                                    writer.WriteLine(eventButton.ButtonName & ": " & subCategory & " - ")
                                Next
                            End If
                        Next

                        ' Write a separate section for custom annotations
                        writer.WriteLine("[- Custom Annotations -] -")
                    End Using

                    HandleUserMessageLogging("GMRC", $"File created {saveCustomAnnoFileName} with button names included",,,, Nothing)
                Else
                    HandleUserMessageLogging("GMRC", $"File already exists {saveCustomAnnoFileName}",,,, Nothing)
                End If
            Next
        End If
    End Sub

    Private Function ParseCSVLine(ByVal line As String) As List(Of String)
        Dim fields As New List(Of String)()
        Dim currentField As New Text.StringBuilder()
        Dim insideQuote As Boolean = False

        For Each ch As Char In line
            If ch = """"c Then
                ' Toggle the state of insideQuote when encountering a double-quote
                insideQuote = Not insideQuote
            ElseIf ch = ","c And Not insideQuote Then
                ' If a comma is found outside of quotes, it marks the end of the current field
                fields.Add(currentField.ToString().Trim())
                currentField.Clear()
            Else
                ' Append the character to the current field
                currentField.Append(ch)
            End If
        Next

        ' Add the last field if there's any remaining data
        fields.Add(currentField.ToString().Trim())
        Return fields
    End Function

    Private Sub AddValidVoiceCommand(ByVal command As String, ByVal action As Action)
        ' Access the singleton instance
        Dim dataDictionary = DataDictionarySingleton.GetInstance()

        ' Convert the command to lowercase for consistent key matching
        Dim commandKey = command.ToLower()

        ' Check if the command already exists in the Commands dictionary
        If dataDictionary.Commands.ContainsKey(commandKey) Then
            ' Command already exists, skip adding
            Exit Sub
        End If

        ' Add the command and its associated action to the Commands dictionary
        dataDictionary.Commands.Add(commandKey, New DataDictionarySingleton.Command With {
            .CommandText = command,
            .Action = action
        })
    End Sub

    Public Function HandleVehicleConfigurationsFile(ByVal Filename As String) As Boolean
        ' Called from routines that open this file for input...
        ' Checks for existence of VehicleConfigurations.csv file - if it does not exist for some reason
        ' we will look for a saved version, copy it to VehicleConfigurations.csv and continue. If neither
        ' file exists we post a message and return false...
        Dim SaveFileName As String = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) & "_SAVE.csv")
        Dim FilenameNF As String = Path.Combine(My.Application.Info.DirectoryPath, "VehicleConfigurationsNF.csv")
        Dim NewString As String = ""
        Dim FirstLine As Boolean = True
        HandleUserMessageLogging("GMRC", "HandleVehicleConfigurationsFile Called...")
        HandleVehicleConfigurationsFile = False
        ' Check if the file exists; if not, check for a saved version and copy it
        If Not File.Exists(Filename) Then
            If File.Exists(SaveFileName) Then
                HandleUserMessageLogging("GMRC", "HandleVehicleConfigurationsFile Copying " & SaveFileName & " to " & Filename)
                File.Copy(SaveFileName, Filename, True)
                HandleVehicleConfigurationsFile = True
            Else
                HandleUserMessageLogging("GMRC", "HandleVehicleConfigurationsFile " & Filename & " file does Not exist Or Is corrupted, Exiting...", DisplayMsgBox)
                Return False
            End If
        Else
            ' Check if the file is in use
            If Not FileInUse(Filename) Then
                HandleVehicleConfigurationsFile = True
            Else
                HandleUserMessageLogging("GMRC", "HandleVehicleConfigurationsFile: Vehicle Configurations file " & Filename & " In use. Please close the file before continuing...", DisplayMsgBox)
                If FileInUse(Filename) Then
                    HandleUserMessageLogging("GMRC", "HandleVehicleConfigurationsFile: Vehicle Configurations file " & Filename & " In use. Please close the file And restart CLEVIR. Exiting...", DisplayMsgBox)
                    Return False
                Else
                    HandleVehicleConfigurationsFile = True
                End If
            End If
        End If
        ' If the Filename passed in is VehicleConfigurations.csv, create the new format NF file if necessary
        If Filename = Path.Combine(My.Application.Info.DirectoryPath, "VehicleConfigurations.csv") AndAlso MaxCameras = 8 Then
            LocalVehicleConfigFileModifyDate = File.GetLastWriteTime(Filename)
            ' Use StreamReader and StreamWriter to process the file
            Using reader As New StreamReader(Filename)
                Using writer As New StreamWriter(FilenameNF, False) ' False to overwrite the file
                    While Not reader.EndOfStream
                        Dim textarray = reader.ReadLine().Split(","c)
                        For x = 0 To textarray.Length - 1
                            If x = 0 Then
                                NewString = textarray(x)
                            ElseIf x <= 13 Then
                                NewString &= "," & textarray(x)
                            ElseIf x = 14 Then
                                If FirstLine Then
                                    NewString &= ",Camera 7,Camera 8," & textarray(x)
                                    FirstLine = False
                                Else
                                    NewString &= ",NA,NA," & textarray(x)
                                End If
                            Else
                                NewString &= "," & textarray(x)
                            End If
                        Next
                        writer.WriteLine(NewString)
                    End While
                End Using
            End Using
        Else
            LocalVehicleConfigFileModifyDate = File.GetLastWriteTime(Filename)
        End If
        Return HandleVehicleConfigurationsFile
    End Function

    Public Function ReadVehicleConfigsFile() As Boolean

        '-------------------------------------------------------------------
        ' Called from:
        '   1. Init_Form.Load
        '   2. SaveVehicleConfigChanges
        '   3. InitForm.SaveVehicleNumber
        '
        ' Reads and parses the vehicleconfigurations.csv (or VehicleConfigurationsNF.csv)
        ' based on the VehicleNumber, populating global variables needed for subsequent
        ' workspace-creation logic in CLEVIR.
        '
        ' This function sets:
        '   - ProjectName, GProjectAbbreviation
        '   - NumberOfCamerasInVehicle, CameraNames()
        '   - BlueBoxInfo(), BlueBoxID
        '   - NumControllers, FCMConfigName
        '   - INCAWorkspaceTemplateName, MasterTemplateName, WorkspaceNameSuffix
        '   - DataUploadPath, CLEVIRFilesPath, ZipTheMF4Files
        '
        ' Returns:
        '   True if vehicle config was read and processed successfully;
        '   otherwise False.
        '-------------------------------------------------------------------

        Dim defaultReturnValue As Boolean = False
        Dim filename As String = DetermineVehicleConfigFileName()
        Dim lineItems() As String = Nothing
        ' Column indices for the 25-column VehicleConfigurationsNF.csv format
        Dim PROC_START As Integer = 2, PROC_END As Integer = 7
        Dim CAMERA_START As Integer = 8, CAMERA_END As Integer = 16 ' Adjusted for 9 cameras
        Dim CANMON_START As Integer = 17, BLUEBOX_DESIGNATION As Integer = 18, CANMON_END As Integer = 20 ' Adjusted for new layout
        Dim DATA_UPLOAD_PATH As Integer = 21, CLEVIR_FILES_PATH As Integer = 22
        Dim ZIP_MF4_FILES As Integer = 23, CONFIG_NAME As Integer = 24
        ' Flags/holders
        Dim vehicleFound As Boolean = False
        Dim vehicleID As String = ""
        Dim foundFront As Boolean = False
        Dim acpCanConfig As String = ""
        Dim blueBoxID As String = ""
        ReadVehicleConfigsFile = False
        Try
            ' Ensure we have the correct CSV format and shift column indices if using the older format
            If Not HandleVehicleConfigurationsFile(filename) Then
                Return False
            End If
            If IsOldFormatFile(filename) Then
                AdjustColumnIndicesForOldFormat(PROC_START, PROC_END, CAMERA_START, CAMERA_END, CANMON_START, BLUEBOX_DESIGNATION, CANMON_END,
                                            DATA_UPLOAD_PATH, CLEVIR_FILES_PATH, ZIP_MF4_FILES, CONFIG_NAME)
            End If
            ' Reset key globals before reading
            NumberOfCamerasInVehicle = 0
            NumControllers = 0
            FCMConfigName = ""
            ReDim CameraNames(8) ' Resize for 9 cameras (indices 0-8)
            ' Open the file using StreamReader
            Using reader As New StreamReader(filename)
                ' Skip header
                Dim headerLine As String = reader.ReadLine()
                lineItems = headerLine.Split(","c)
                ' Read file line by line
                While Not reader.EndOfStream
                    Dim textLine As String = reader.ReadLine()
                    lineItems = textLine.Split(","c)
                    ' If this row matches the current VehicleNumber, process and break out
                    If String.Equals(lineItems(0), VehicleNumber, StringComparison.OrdinalIgnoreCase) Then
                        vehicleFound = True
                        vehicleID = lineItems(1)
                        Dim success As Boolean = ProcessVehicleRow(
                        lineItems,
                        filename,
                        ReadVehicleConfigsFile,
                        PROC_START, PROC_END,
                        CAMERA_START, CAMERA_END,
                        CANMON_START, BLUEBOX_DESIGNATION, CANMON_END,
                        DATA_UPLOAD_PATH, CLEVIR_FILES_PATH,
                        ZIP_MF4_FILES, CONFIG_NAME,
                        foundFront,
                        acpCanConfig,
                        blueBoxID
                    )
                        If Not success Then
                            Return False
                        End If
                        ' If not ClevirAdministrator, we can exit after finding the matched vehicle
                        If Not ClevirAdministrator Then Exit While
                    Else
                        ' If admin, still do additional mismatch checks on lines that don’t match
                        If ClevirAdministrator Then
                            CheckClevirFilesPathMismatch(lineItems, CLEVIR_FILES_PATH, PROC_START)
                        End If
                    End If
                End While
            End Using
            If Not vehicleFound AndAlso Not String.Equals(VehicleNumber, "UNDEFINED", StringComparison.OrdinalIgnoreCase) Then
                HandleMissingVehicleNumber(vehicleNumber:=VehicleNumber, vehicleID, ReadVehicleConfigsFile)
                Return True
            End If
            ' We have enough info to figure out the workspace template and suffix
            If Not ConfigureWorkspaceAndTemplate(filename, acpCanConfig, blueBoxID) Then
                Return False
            End If
            WorkspaceNameSuffix = INCAWorkspaceTemplateName & NumberOfCamerasInVehicle & "C"
            ' Save these in case we revert from version-specific templates
            GSaveIncaWorkspaceTemplateName = INCAWorkspaceTemplateName
            GSaveWorkspaceNameSuffix = WorkspaceNameSuffix
            ' Make a backup of the config file
            File.Copy(filename,
                  Path.Combine(My.Application.Info.DirectoryPath, Path.GetFileNameWithoutExtension(filename) & "_SAVE.csv"),
                  overwrite:=True)
            defaultReturnValue = True
            HandleUserMessageLogging("GMRC",
                                 $"ReadVehicleConfigsFile Number of Cameras defined in {Path.GetFileName(filename)} = {NumberOfCamerasInVehicle}")
        Catch ex As Exception
            ' If anything goes wrong, log the error
            HandleUserMessageLogging("GMRC",
                                 $"ReadVehicleConfigsFile {Path.GetFileName(filename)} file may be corrupted, {ex.Message} Exiting...",
                                  DisplayMsgBox)
        Finally
            ReadVehicleConfigsFile = defaultReturnValue
        End Try
        Return ReadVehicleConfigsFile
    End Function

    '-----------------------------------------------------------
    '   DETERMINE WHICH CSV FILE TO USE
    '-----------------------------------------------------------
    Private Function DetermineVehicleConfigFileName() As String
        Dim nfFile As String = Path.Combine(My.Application.Info.DirectoryPath, "VehicleConfigurationsNF.csv")
        Dim legacyFile As String = Path.Combine(My.Application.Info.DirectoryPath, "VehicleConfigurations.csv")

        If File.Exists(nfFile) Then
            MaxCameras = 9 ' Updated to support 9 cameras
            Return nfFile
        Else
            MaxCameras = 8 ' Legacy file supports 8 cameras
            Return legacyFile
        End If
    End Function

    Private Function IsOldFormatFile(filename As String) As Boolean
        Return Path.GetFileName(filename).Equals("VehicleConfigurations.csv", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Sub AdjustColumnIndicesForOldFormat(ByRef PROC_START As Integer,
                                                ByRef PROC_END As Integer,
                                                ByRef CAMERA_START As Integer,
                                                ByRef CAMERA_END As Integer,
                                                ByRef CANMON_START As Integer,
                                                ByRef BLUEBOX_DESIGNATION As Integer,
                                                ByRef CANMON_END As Integer,
                                                ByRef DATA_UPLOAD_PATH As Integer,
                                                ByRef CLEVIR_FILES_PATH As Integer,
                                                ByRef ZIP_MF4_FILES As Integer,
                                                ByRef CONFIG_NAME As Integer)
        ' Adjust indices for the legacy format, which has 2 fewer camera columns
        CAMERA_END -= 2
        CANMON_START -= 2
        BLUEBOX_DESIGNATION -= 2
        CANMON_END -= 2
        DATA_UPLOAD_PATH -= 2
        CLEVIR_FILES_PATH -= 2
        ZIP_MF4_FILES -= 2
        CONFIG_NAME -= 2
    End Sub

    '-----------------------------------------------------------
    '   PROCESS A MATCHING VEHICLE ROW
    '-----------------------------------------------------------
    Private Function ProcessVehicleRow(
    ByVal lineItems() As String,
    ByVal filename As String,
    ByRef readVehicleConfigsFile As Boolean,   ' <-- byRef param here!
    ByVal PROC_START As Integer, ByVal PROC_END As Integer,
    ByVal CAMERA_START As Integer, ByVal CAMERA_END As Integer,
    ByVal CANMON_START As Integer, ByVal BLUEBOX_DESIGNATION As Integer, ByVal CANMON_END As Integer,
    ByVal DATA_UPLOAD_PATH As Integer, ByVal CLEVIR_FILES_PATH As Integer,
    ByVal ZIP_MF4_FILES As Integer, ByVal CONFIG_NAME As Integer,
    ByRef foundFront As Boolean,
    ByRef acpCanConfig As String,
    ByRef blueBoxID As String
) As Boolean

        NumberOfCamerasInVehicle = 0

        ' ✅ Clear ActiveCameras for this vehicle
        ActiveCameras.Clear()

        ' Ensure lineItems is large enough to prevent index errors
        If lineItems.Length <= CONFIG_NAME Then
            HandleUserMessageLogging("GMRC", $"ReadVehicleConfigsFile: Skipped row due to insufficient columns for vehicle {lineItems(0)}.")
            Return True ' Continue processing other rows
        End If

        For x As Integer = PROC_START To CONFIG_NAME

            ' 1) Handle the very first processor (assign ProjectName, etc.)
            If x = PROC_START Then
                If Not DetermineProjectName(lineItems(x)) Then
                    ' If invalid, log and return False to exit
                    HandleUserMessageLogging(
                    "GMRC",
                    $"ReadVehicleConfigsFile: Invalid Processor name ({lineItems(x)}) " &
                    $"In {Path.GetFileName(filename)} file For Vehicle {lineItems(0)}. Exiting...",
                    DisplayMsgBox,
)
                    Return False
                End If
            End If

            ' 2) Count controllers
            If x >= PROC_START AndAlso x <= PROC_END Then
                If Not lineItems(x).ToUpper().Contains("NA") Then
                    NumControllers += 1
                End If
            End If

            ' 3) Gather camera info and map to IP addresses
            If x >= CAMERA_START AndAlso x <= CAMERA_END Then
                Dim cameraIndex As Integer = x - CAMERA_START
                If cameraIndex < CameraNames.Length Then
                    Dim cameraPosition As String = lineItems(x).Trim()
                    CameraNames(cameraIndex) = cameraPosition

                    If Not cameraPosition.Equals("NA", StringComparison.OrdinalIgnoreCase) Then
                        If cameraPosition.ToUpper() = "FRONT" Then foundFront = True
                        NumberOfCamerasInVehicle += 1

                        ' ✅ NEW: Map camera position to IP address from config.xml
                        If ConfiguredCameras.ContainsKey(cameraPosition) Then
                            Dim cameraConfig = ConfiguredCameras(cameraPosition)
                            ActiveCameras.Add(cameraConfig)
                            HandleUserMessageLogging("GMRC",
                                $"Camera mapped: {cameraPosition} → {cameraConfig.IpAddress} (Index: {cameraIndex})")
                        Else
                            HandleUserMessageLogging("GMRC",
                                $"⚠️ WARNING: Camera position '{cameraPosition}' not found in config.xml camera mappings",
                                DisplayMsgBox)
                        End If
                    End If
                End If
            End If

            ' 4) Gather CAN monitoring info
            If x >= CANMON_START AndAlso x <= CANMON_END Then
                Dim canMonIndex As Integer = x - CANMON_START
                If canMonIndex < BlueBoxInfo.Length Then
                    BlueBoxInfo(canMonIndex) = lineItems(x)
                    If ProjectName.ToUpper().Contains("ACP4") AndAlso
                       BlueBoxInfo(canMonIndex).ToUpper().Contains("RTK") Then
                        acpCanConfig = "_RTK"
                    End If
                End If

                ' Check the special BlueBoxDesignation column
                If x = BLUEBOX_DESIGNATION Then
                    blueBoxID = DetermineBlueBoxID(lineItems(x), acpCanConfig)
                End If
            End If

            ' 5) Handle DataUploadPath, CLEVIRFilesPath, ZipTheMF4Files, and FCMConfigName
            If Not ProcessSpecialFields(
            lineItems, x,
            DATA_UPLOAD_PATH, CLEVIR_FILES_PATH, ZIP_MF4_FILES, CONFIG_NAME,
            foundFront, acpCanConfig, blueBoxID
        ) Then
                Return False
            End If

        Next

        ' ✅ Log final camera summary for this vehicle
        HandleUserMessageLogging("GMRC",
            $"Vehicle {lineItems(0)}: {ActiveCameras.Count} active camera(s) - " &
            String.Join(", ", ActiveCameras.Select(Function(c) $"{c.Position}:{c.IpAddress}")))

        Return True
    End Function

    '-----------------------------------------------------------
    '   DETERMINE PROJECT NAME FROM FIRST PROCESSOR
    '-----------------------------------------------------------
    Private Function DetermineProjectName(firstProcessor As String) As Boolean
        Dim proc As String = UCase(firstProcessor)

        Select Case True
            Case proc = "XETK1"
                ProjectName = "LowContent"
                GProjectAbbreviation = "LC"

            Case (proc.Contains("FCM") AndAlso Not proc.Contains("FCM100"))
                ProjectName = "FCM"
                GProjectAbbreviation = "FCM"

            Case proc.Contains("FCM100")
                ProjectName = "FCM100"
                GProjectAbbreviation = "FCM100"

            Case proc = "HCF"
                ProjectName = "HighContent"
                GProjectAbbreviation = "HC"

            Case proc.Contains("ACP2")
                ProjectName = "ACP2"
                GProjectAbbreviation = "ACP2"

            Case proc.Contains("ACP3")
                ProjectName = "ACP3"
                GProjectAbbreviation = "ACP3"

            Case proc.Contains("ACP4")
                ProjectName = "ACP4"
                GProjectAbbreviation = "ACP4"

            Case proc = "IP"
                ProjectName = "CSAV2"
                GProjectAbbreviation = "CSAV2"

            Case Else
                Return False
        End Select

        Return True
    End Function

    '-----------------------------------------------------------
    '   DETERMINE BLUEBOX ID
    '   (Adjust based on instrumentation defaults)
    '-----------------------------------------------------------
    Private Function DetermineBlueBoxID(currentBlueBoxID As String, ByVal acpCanConfig As String) As String
        Dim candidate As String = currentBlueBoxID

        ' If instrumentation setup matches a default, reset to ""
        If ProjectName.ToUpper.Contains("LOWCONTENT") AndAlso candidate = "523" Then
            candidate = ""

        ElseIf ProjectName.ToUpper.Contains("FCM") AndAlso candidate = "523" Then
            candidate = ""

        ElseIf ProjectName.ToUpper.Contains("HIGHCONTENT") AndAlso candidate = "523" Then
            candidate = ""

        ElseIf ProjectName.ToUpper.Contains("ACP2") Then
            If candidate = "523" Then
                candidate = ""
            ElseIf candidate = "NA" Then
                acpCanConfig = "_NOCAN"
                candidate = ""
            End If

        ElseIf ProjectName.ToUpper.Contains("ACP3") Then
            If candidate = "523" Then
                candidate = ""
            ElseIf candidate = "NA" Then
                acpCanConfig = "_NOCAN"
                candidate = ""
            End If

        ElseIf ProjectName.ToUpper.Contains("ACP4") Then
            If candidate.ToUpper.Contains("523") Then
                candidate = ""
            ElseIf candidate = "NA" AndAlso acpCanConfig <> "_RTK" Then
                acpCanConfig = "_NOCAN"
                candidate = ""
            End If

        ElseIf ProjectName.ToUpper.Contains("CSAV2") Then
            If candidate = "593" Then
                candidate = ""
            End If
        End If

        Return candidate
    End Function

    '-----------------------------------------------------------
    '   HANDLE DATA UPLOAD PATH, CLEVIR FILES PATH, ZIP MF4 FILES, CONFIG NAME
    '-----------------------------------------------------------
    Private Function ProcessSpecialFields(lineItems() As String,
                                         currentIndex As Integer,
                                         DATA_UPLOAD_PATH As Integer,
                                         CLEVIR_FILES_PATH As Integer,
                                         ZIP_MF4_FILES As Integer,
                                         CONFIG_NAME As Integer,
                                         foundFront As Boolean,
                                         ByRef acpCanConfig As String,
                                         ByRef blueBoxID As String) As Boolean

        Select Case currentIndex

            Case DATA_UPLOAD_PATH
                If lineItems(currentIndex).Length > 0 Then
                    DataUploadPath = "\" & lineItems(currentIndex)
                Else
                    DataUploadPath = ""
                End If

            Case CLEVIR_FILES_PATH
                CLEVIRFilesPath = lineItems(currentIndex)
                CheckClevirFilesPathMismatch(lineItems, CLEVIR_FILES_PATH, 2) ' 2 = PROC_START in default scenario

            Case ZIP_MF4_FILES
                ZipTheMF4Files = (UCase(lineItems(currentIndex)) = "TRUE")

                ' Determine radio buttons for Development vs Validation
                Select Case CurrentVehicleUsage.ToUpper
                    Case "DEVELOPMENT"
                        InitForm.RadioButton1.Checked = True
                    Case "VALIDATION"
                        InitForm.RadioButton2.Checked = True
                End Select

                HandleUserMessageLogging("GMRC",
                    $"ReadVehicleConfigsFile OriginalVehicleConfiguration = {OriginalVehicleConfiguration} CurrentVehicleUsage = {CurrentVehicleUsage} ZipTheMF4Files = {ZipTheMF4Files}")

                If UsingFlashDrive Then
                    InitForm.RadioButton1.Checked = True
                    HandleUserMessageLogging("GMRC",
                        $"ReadVehicleConfigsFile UsingFlashDrive = True. CurrentVehicleUsage = {CurrentVehicleUsage} ZipTheMF4Files set to True")
                End If

            Case CONFIG_NAME
                FCMConfigName = lineItems(currentIndex)
        End Select

        Return True
    End Function

    '-----------------------------------------------------------
    '   IF WE DON’T MATCH THE VEHICLE NUMBER, CHECK PATH MISMATCH
    '   (Admin only)
    '-----------------------------------------------------------
    Private Sub CheckClevirFilesPathMismatch(lineItems() As String,
                                             CLEVIR_FILES_PATH As Integer,
                                             PROC_START As Integer)

        If Not ClevirAdministrator Then Exit Sub

        Dim cPath As String = UCase(lineItems(CLEVIR_FILES_PATH))
        Dim proc As String = UCase(lineItems(PROC_START))
        Dim mismatch As Boolean = False

        If cPath.Contains("LOWCONTENT") AndAlso proc <> "XETK1" Then mismatch = True
        If cPath.Contains("FCM") AndAlso Not proc.Contains("FCM") Then mismatch = True
        If cPath.Contains("HIGHCONTENT") AndAlso proc <> "HCF" Then mismatch = True
        If cPath.Contains("ACP2") AndAlso proc <> "ACP2_MCU" Then mismatch = True
        If cPath.Contains("ACP3") AndAlso proc <> "ACP3_MCU" Then mismatch = True
        If cPath.Contains("ACP4") AndAlso proc <> "ACP4_MCU" Then mismatch = True
        If cPath = "CURRENT" AndAlso proc <> "IP" Then mismatch = True

        If mismatch Then
            If VehicleStatDashboard.Visible = False Then
                HandleUserMessageLogging("GMRC",
                    $"ReadVehicleConfigsFile: CLEVIR Files Path {lineItems(CLEVIR_FILES_PATH)} does Not match controller type {lineItems(PROC_START)} For Vehicle Number {lineItems(0)}",
                    DisplayMsgBox, )
            End If
        End If
    End Sub

    '-----------------------------------------------------------
    '   HANDLE CASE WHERE VEHICLE ID NOT FOUND
    '-----------------------------------------------------------
    Private Sub HandleMissingVehicleNumber(vehicleNumber As String,
                                       ByRef vehicleID As String,
                                       ByRef ReadVehicleConfigsFile As Boolean)
        ' Log or display a message if the vehicle number is not found
        If Not VehicleStatDashboard.Visible Then
            HandleUserMessageLogging("GMRC",
            $"Vehicle Number {vehicleNumber} Not found In Vehicle Configurations File...",
            DisplayMsgBox, )
        Else
            VehicleStatDashboard.ListBox9.Items.Add($"Vehicle Number {vehicleNumber} Not found In Vehicle Configurations File...")
        End If
        ' Update the vehicle ID and number to "UNDEFINED"
        vehicleID = "UNDEFINED"
        vehicleNumber = "UNDEFINED"
        ReadVehicleConfigsFile = True
    End Sub


    '-----------------------------------------------------------
    '   CONFIGURE WORKSPACE TEMPLATE / SUFFIX
    '-----------------------------------------------------------
    Private Function ConfigureWorkspaceAndTemplate(filename As String,
                                                   acpCanConfig As String,
                                                   blueBoxID As String) As Boolean
        Select Case GProjectAbbreviation
            Case "CSAV2"
                If NumControllers = 3 Then
                    INCAWorkspaceTemplateName = GProjectAbbreviation & "_3P" & blueBoxID
                ElseIf NumControllers = 6 Then
                    INCAWorkspaceTemplateName = GProjectAbbreviation & "_3P3R" & blueBoxID
                Else
                    Return FailConfig(filename)
                End If
                MasterTemplateName = "CSAV2_WorkspaceTemplate"

            Case "FCM"
                If NumControllers = 1 Then
                    INCAWorkspaceTemplateName = GProjectAbbreviation & "_1P" & blueBoxID
                Else
                    Return FailConfig(filename)
                End If
                MasterTemplateName = "FCM_WorkspaceTemplate"

            Case "FCM100"
                If NumControllers = 1 Then
                    INCAWorkspaceTemplateName = GProjectAbbreviation & "_1P" & blueBoxID
                Else
                    Return FailConfig(filename)
                End If
                MasterTemplateName = "FCM100_WorkspaceTemplate"

            Case "LC"
                Select Case NumControllers
                    Case 1
                        INCAWorkspaceTemplateName = GProjectAbbreviation & "_1P" & blueBoxID
                        MasterTemplateName = "LC_WorkspaceTemplate"
                    Case 2
                        INCAWorkspaceTemplateName = GProjectAbbreviation & "_" & Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1) & blueBoxID
                        If InStr(FCMConfigName, "FCM100") > 0 Then
                            MasterTemplateName = "LC_FCM100_WorkspaceTemplate"
                        Else
                            MasterTemplateName = "LC_FCM_WorkspaceTemplate"
                        End If
                    Case Else
                        Return FailConfig(filename)
                End Select

            Case "HC"
                Select Case NumControllers
                    Case 2
                        INCAWorkspaceTemplateName = GProjectAbbreviation & "_2P" & blueBoxID
                        MasterTemplateName = "HC_WorkspaceTemplate"
                    Case 3
                        INCAWorkspaceTemplateName = GProjectAbbreviation & "_" & Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1) & blueBoxID
                        If InStr(FCMConfigName, "FCM100") > 0 Then
                            MasterTemplateName = "HC_FCM100_WorkspaceTemplate"
                        Else
                            MasterTemplateName = "HC_FCM_WorkspaceTemplate"
                        End If
                    Case Else
                        Return FailConfig(filename)
                End Select

            Case "ACP2"
                If NumControllers = 1 Then
                    INCAWorkspaceTemplateName = GProjectAbbreviation & acpCanConfig & "_1P" & blueBoxID
                    MasterTemplateName = "ACP2_WorkspaceTemplate"
                Else
                    Return FailConfig(filename)
                End If

            Case "ACP3"
                Select Case NumControllers
                    Case 1
                        INCAWorkspaceTemplateName = GProjectAbbreviation & acpCanConfig & "_1P" & blueBoxID
                        MasterTemplateName = "ACP3_WorkspaceTemplate"
                    Case 2
                        INCAWorkspaceTemplateName = GProjectAbbreviation & acpCanConfig & "_" & Mid(FCMConfigName, 1, InStr(FCMConfigName, "_") - 1) & blueBoxID
                        If InStr(FCMConfigName, "FCM100") > 0 Then
                            MasterTemplateName = "ACP3_FCM100_WorkspaceTemplate"
                        Else
                            MasterTemplateName = "ACP3_FCM_WorkspaceTemplate"
                        End If
                    Case Else
                        Return FailConfig(filename)
                End Select

            Case "ACP4"
                If NumControllers = 1 Then
                    INCAWorkspaceTemplateName = GProjectAbbreviation & acpCanConfig & "_1P" & blueBoxID
                Else
                    Return FailConfig(filename)
                End If

                ' If ACP4 is using RTK, we use a different MasterTemplate
                If acpCanConfig <> "_RTK" Then
                    MasterTemplateName = "ACP4_WorkspaceTemplate"
                Else
                    MasterTemplateName = "ACP4_WorkspaceTemplate_RTK"
                End If
        End Select

        Return True
    End Function

    Private Function FailConfig(filename As String) As Boolean
        HandleUserMessageLogging("GMRC",
            $"Invalid contents In {Path.GetFileName(filename)} file For {VehicleNumber} Exiting...",
            DisplayMsgBox)
        Return False
    End Function


    Public Function HandleEscalationProcessingEXE(ByVal DirNAme As String, ByVal NumMF4Files As Integer) As Boolean

        'Called for each session folder found in Data\gmcsvVEHCILENUMBER folder from DeleteMF4Files which is called whenever the user
        'stops recording by pressing Stop Record or Stop Measurement.  Assumes that the Escalation Processing .exe will look for all
        '.mf4 files in session folder,  process each one and create a .csv file which will be placed in the same session folder...

        'Utilizes python generated .exe file to perform mf4 file processing to find escalations...

        Static ExecutableFile As String
        Static FileProcessingTime As Integer
        Static InitTime As Integer
        Static TerminationTime As Integer
        Static WindowStyle As String

        Dim textline As String
        Dim TotalWaitTimeInSeconds As Integer

        Try

            'We only do the following the first time this routine is called (after the first user initiated stop record event (Len(ExecutableFile) = 0)...
            '

            If File.Exists(My.Application.Info.DirectoryPath & "\" & "EscalationExeName.txt") And Len(ExecutableFile) = 0 Then

                'If we have .zip files in app directory and they have not yet been unzipped to their respective folders, we will do this here...
                '.zip files and EscalationExeName.txt file will be copied by CLEVIR from UpdatedFiles directory on share drive at initialization...

                If File.Exists(My.Application.Info.DirectoryPath & "\process_supercruise_events.zip") And Not Directory.Exists(My.Application.Info.DirectoryPath & "\process_supercruise_events") Then
                    HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Unzipping process_supercruise_events.zip...",,, FlashMsg1Sec)
                    UnzipFolder(My.Application.Info.DirectoryPath & "\process_supercruise_events.zip")

                    If Directory.Exists(My.Application.Info.DirectoryPath & "\process_supercruise_events") Then
                        HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Unzip Complete",,, FlashMsg1Sec)
                    Else
                        HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Unzip Did Not complete successfully, Exiting...",,, FlashMsg3Sec)
                        Exit Function
                    End If

                    If File.Exists(My.Application.Info.DirectoryPath & "\assets.zip") And Not Directory.Exists(My.Application.Info.DirectoryPath & "\assets") Then
                        HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Unzipping assets.zip...",,, FlashMsg1Sec)
                        UnzipFolder(My.Application.Info.DirectoryPath & "\assets.zip")
                        If Directory.Exists(My.Application.Info.DirectoryPath & "\assets") Then
                            HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Unzip Complete",,, FlashMsg1Sec)
                        Else
                            HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Unzip Did Not complete successfully, Exiting...",,, FlashMsg3Sec)
                            Exit Function
                        End If

                    End If

                End If

                ProcessEscalations = True

                'Here we are processing the EscalationExeName file which contains various execution parameters for the Escalation processing .exe
                'We place the info into static variables because we are only calling this one time...

                Dim fnum As Integer

                HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Processing EscalationExeName.txt file...",, )

                fnum = FreeFile()
                FileOpen(fnum, My.Application.Info.DirectoryPath & "\" & "EscalationExeName.txt", OpenMode.Input)
                textline = LineInput(fnum)
                ExecutableFile = Mid(textline, InStr(textline, "=") + 1, Len(textline))
                textline = LineInput(fnum)
                FileProcessingTime = CInt(Mid(textline, InStr(textline, "=") + 1, Len(textline)))
                textline = LineInput(fnum)
                InitTime = CInt(Mid(textline, InStr(textline, "=") + 1, Len(textline)))
                textline = LineInput(fnum)
                TerminationTime = CInt(Mid(textline, InStr(textline, "=") + 1, Len(textline)))
                textline = LineInput(fnum)
                WindowStyle = Mid(textline, InStr(textline, "=") + 1, Len(textline))

                FileClose(fnum)

                HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE EscalationExeName.txt file processing complete.",, )

            ElseIf Not File.Exists(My.Application.Info.DirectoryPath & "\" & "EscalationExeName.txt") Then
                HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE EscalationExeName.txt does Not exist in install directory...",, )
                Exit Function
            End If

            If ProcessEscalations = False Then
                HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE ProcessEscalations = False...")
                Exit Function
            End If

            If File.Exists(ExecutableFile) Then

                OnVehicleScreen.Button9.BackColor = Color.LightGreen
                HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Processing Escalations...",,, FlashMsg2Sec)

                Dim p As New ProcessStartInfo

                'WindowStyle is defined in EscalationExeName.txt file...
                Select Case UCase(WindowStyle)
                    Case "NORMAL"
                        p.WindowStyle = ProcessWindowStyle.Normal
                    Case "HIDDEN"
                        p.WindowStyle = ProcessWindowStyle.Hidden
                    Case Else
                        p.WindowStyle = ProcessWindowStyle.Hidden
                End Select
                'p.WindowStyle = ProcessWindowStyle.Hidden (Normal?)
                p.FileName = ExecutableFile

                'any arguments to pass to .exe will be done here - 
                'Presumably full path. Maybe some info indicating sw version, My And VehicleType? Other parameters required?
                p.Arguments = DirNAme

                'Assumptions for Escalation application as follows...
                '1 Writes .csv file to same recording session folder as mf4 files are located.
                '2 Creates one .csv file per session folder - .csv file will be automatically copied up to q drive with no code change required.
                '3 exe should return an exit code = 0 if success, or numeric value indicating failure of some kind (myProcess.ExitCode)

                'Dim myProcess As Process

                Using myProcess = New Process With {
                    .EnableRaisingEvents = True
                    }
                    HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Starting Process...")
                    myProcess.StartInfo = p
                    myProcess.Start()

                    TotalWaitTimeInSeconds = InitTime + TerminationTime + (FileProcessingTime * NumMF4Files)

                    HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE: Process Started - Waiting " & CStr(TotalWaitTimeInSeconds) & " seconds For Exit...")

                    'We will wait here for the spawned process to complete.  We give it InitTime + TerminationTime + ProcessingTime * Number of files to process, these values
                    'are all defined in the EscalationExeName.txt file....

                    myProcess.WaitForExit(TotalWaitTimeInSeconds)

                    'If we time out before the .exe indicates that it has exited, we will terminate it...
                    If myProcess.HasExited = True Then

                        'Handling of exit code, TBD...

                        Select Case myProcess.ExitCode
                            Case 0
                                HandleEscalationProcessingEXE = True
                                HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE " & ExecutableFile & " Process exited with Exit Code " & myProcess.ExitCode & " Success!!!",,, FlashMsg3Sec)
                            Case Else
                                HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE " & ExecutableFile & " Process exited with Exit Code " & myProcess.ExitCode & "...",,, FlashMsg3Sec)
                        End Select

                    Else
                        myProcess.Kill()
                        HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE " & ExecutableFile & " did Not exit within allotted time (" & CStr(TotalWaitTimeInSeconds) & " - Process terminated...",,, FlashMsg3Sec)
                    End If

                    myProcess.Dispose()
                End Using
            Else
                HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE " & ExecutableFile & " does Not exist...",, )
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleEscalationProcessingEXE " & ex.Message,,, FlashMsg3Sec)
        End Try

        Return True

    End Function

    Public Sub ParseA2lFile(ByVal enumFileName As String)
        Try
            ' Check if Enumerations.txt exists
            If Not File.Exists(Path.Combine(My.Application.Info.DirectoryPath, "Enumerations.txt")) Then
                HandleUserMessageLogging("GMRC", "Parsing A2L Files and creating a new software and model year-specific Enumerations file...", DisplayMsgBox)
                CreateNewEnumerationFile(enumFileName)
            End If
            ' If not configuring for a new software version, load Enumerations.txt into data structures
            If Not ConfigureForNewSoftwareVersion Then
                LoadEnumerationFile(enumFileName)
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ParseA2lFile: " & ex.Message, DisplayMsgBox)
        End Try
    End Sub

    Private Sub ParseA2lFileContent(ByVal filePath As String, ByVal writer As StreamWriter)
        Using reader As New StreamReader(filePath)
            Dim textLine As String
            While Not reader.EndOfStream
                textLine = reader.ReadLine()

                ' Handle COMPU_METHOD blocks
                If textLine.Contains("/begin COMPU_METHOD") AndAlso textLine.Contains("CM_") Then
                    ProcessCompuMethodBlock(reader, textLine)
                End If

                ' Handle COMPU_VTAB blocks
                If textLine.Contains("/begin COMPU_VTAB") AndAlso textLine.Contains("CT_") Then
                    ProcessCompuVTabBlock(reader, textLine)
                End If

                ' Handle MEASUREMENT blocks
                If textLine.Contains("/begin MEASUREMENT") AndAlso textLine.Contains("_e_") Then
                    ProcessMeasurementBlock(reader, textLine)
                End If
            End While
        End Using
    End Sub

    Private Sub ProcessCompuMethodBlock(ByVal reader As StreamReader, ByVal firstLine As String)
        ' Extract the COMPU_METHOD name from the line, e.g. "CM_TeDF3R_e_MfrsLrnStat"
        Dim tokens As String() = firstLine.Split(New Char() {" "}, StringSplitOptions.RemoveEmptyEntries)
        ' tokens might look like: {"/begin", "COMPU_METHOD", "CM_TeDF3R_e_MfrsLrnStat"}
        If tokens.Length >= 3 Then
            Dim compuMethodName As String = tokens(2) ' e.g. "CM_TeDF3R_e_MfrsLrnStat"

            ' Now read lines until /end COMPU_METHOD
            Dim line As String = Nothing
            Dim compuTabRef As String = Nothing
            Do
                If reader.EndOfStream Then Exit Do
                line = reader.ReadLine()

                If line.Contains("/end COMPU_METHOD") Then
                    Exit Do
                ElseIf line.Contains("COMPU_TAB_REF") Then
                    ' Example line: "COMPU_TAB_REF CT_TeDF3R_e_MfrsLrnStat"
                    Dim parts As String() = line.Split(New Char() {" "}, StringSplitOptions.RemoveEmptyEntries)
                    If parts.Length >= 2 Then
                        ' The part after "COMPU_TAB_REF" is "CT_TeDF3R_e_MfrsLrnStat"
                        compuTabRef = parts(parts.Length - 1)
                    End If
                End If
            Loop

            ' Store mapping: e.g. "CM_TeDF3R_e_MfrsLrnStat" -> "CT_TeDF3R_e_MfrsLrnStat"
            If Not String.IsNullOrEmpty(compuMethodName) AndAlso Not String.IsNullOrEmpty(compuTabRef) Then
                compuMethodMap(compuMethodName) = compuTabRef
            End If
        End If
    End Sub

    Private Sub ProcessMeasurementBlock(ByVal reader As StreamReader, ByVal firstLine As String)
        '
        ' 1. Check bracket logic before we do anything else.
        '
        ' E.g. your original condition:
        '   If ((InStr(textLine, "[") = 0) Or ((InStr(textLine, "[") > 0) And InStr(textLine, "_e_") < InStr(textLine, "["))) And
        '        InStr(textLine, ".") = 0 Then
        '
        ' Translates to:
        '
        If Not ShouldProcessMeasurement(firstLine) Then
            ' Skip lines until we find /end MEASUREMENT, then exit.
            While Not reader.EndOfStream
                Dim skipLine As String = reader.ReadLine()
                If skipLine.Contains("/end MEASUREMENT") Then Exit While
            End While
            Return
        End If

        '
        ' 2. If the measurement line passes bracket logic, extract the measurement name.
        '
        Dim measurementName As String = ExtractMeasurementName(firstLine)
        Dim compuMethodName As String = ""

        '
        ' 3. Now read lines until we see /end MEASUREMENT, looking for "CM_..."
        '
        While Not reader.EndOfStream
            Dim line As String = reader.ReadLine()
            If line.Contains("/end MEASUREMENT") Then
                Exit While
            Else
                line = line.Trim()
                ' If it's the line that references the COMPU_METHOD, it typically starts with "CM_"
                If line.StartsWith("CM_") Then
                    ' e.g. "CM_TeDF3R_e_MfrsLrnStat"
                    ' We take the first token as the compuMethodName
                    compuMethodName = line.Split(New Char() {" "}, StringSplitOptions.RemoveEmptyEntries)(0)
                End If
            End If
        End While

        '
        ' 4. Finally, store the measurement -> compuMethod mapping
        '
        If Not String.IsNullOrEmpty(measurementName) AndAlso
           Not String.IsNullOrEmpty(compuMethodName) Then
            measurementMethodMap(measurementName) = compuMethodName
        End If
    End Sub

    Private Sub ProcessCompuVTabBlock(ByVal reader As StreamReader, ByVal firstLine As String)
        ' E.g. "/begin COMPU_VTAB CT_TeDF3R_e_MfrsLrnStat"
        Dim tokens As String() = firstLine.Split(New Char() {" "}, StringSplitOptions.RemoveEmptyEntries)
        Dim compuVtabName As String = Nothing
        If tokens.Length >= 3 Then
            compuVtabName = tokens(2)  ' e.g. "CT_TeDF3R_e_MfrsLrnStat"
        End If

        Dim enumList As New List(Of String)

        ' --------------------------------------------------
        ' Skip the 3 "header" lines after the compu_vtab name:
        '   1) " "
        '   2) TAB_VERB
        '   3) number-of-enum-lines (like "4")
        ' --------------------------------------------------
        For i As Integer = 1 To 3
            If Not reader.EndOfStream Then
                reader.ReadLine()  ' discard
            End If
        Next

        ' Now read until /end COMPU_VTAB
        While Not reader.EndOfStream
            Dim line As String = reader.ReadLine()
            If line.Contains("/end COMPU_VTAB") Then
                Exit While
            End If

            ' Typically enumerations lines look like "0    "CeDF3R_e_MLS_NotLearned""
            ' We'll just parse out the text after the index
            line = line.Trim()
            ' Filter out empty or comment-like lines:
            If line <> "" AndAlso Not line.StartsWith("/") Then
                ' We only want the actual enumeration text, e.g. CeDF3R_e_MLS_NotLearned
                ' The line might be:  0   "CeDF3R_e_MLS_NotLearned"
                Dim quoteIndex As Integer = line.IndexOf(""""c)
                Dim enumValue As String = line
                If quoteIndex >= 0 Then
                    ' Attempt to extract what's inside the quotes
                    Dim lastQuoteIndex As Integer = line.LastIndexOf(""""c)
                    If lastQuoteIndex > quoteIndex Then
                        enumValue = line.Substring(quoteIndex + 1, lastQuoteIndex - (quoteIndex + 1))
                    End If
                End If
                enumList.Add(enumValue)
            End If
        End While

        ' Store in dictionary
        If Not String.IsNullOrEmpty(compuVtabName) Then
            compuVtabMap(compuVtabName) = enumList
        End If
    End Sub

    Private Sub CreateNewEnumerationFile(ByVal enumFileName As String)
        Dim targetFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "Enumerations.txt")
        ' Clear the dictionaries each time we create from scratch:
        compuMethodMap.Clear()
        compuVtabMap.Clear()
        measurementMethodMap.Clear()

        Using writer As New StreamWriter(targetFilePath, False)
            Dim a2lFolderPath As String = Path.Combine(My.Application.Info.DirectoryPath, "A2L")
            If Not Directory.Exists(a2lFolderPath) Then
                Throw New DirectoryNotFoundException($"A2L folder not found: {a2lFolderPath}")
            End If

            ' --- First parse the entire folder, storing data in memory
            For Each filePath In Directory.GetFiles(a2lFolderPath, "*.*", SearchOption.TopDirectoryOnly)
                If filePath.EndsWith(".a2l", StringComparison.OrdinalIgnoreCase) OrElse
                   filePath.EndsWith(".dbc", StringComparison.OrdinalIgnoreCase) Then
                    ParseA2lFileContent(filePath, writer)
                    ' (We're ignoring "writer" for now, just collecting data in dictionaries)
                End If
            Next

            ' --- Now write out the enumerations once
            WriteAllEnumerations(writer)
        End Using

        ' Copy the newly created Enumerations.txt to the specified enumFileName
        File.Copy(targetFilePath, enumFileName, True)
        'HandleUserMessageLogging("GMRC", "Parsing A2L File(s) complete.", DisplayMsgBox)
    End Sub

    Private Sub WriteAllEnumerations(ByVal writer As StreamWriter)
        ' measurementMethodMap:  measurementName -> "CM_xxx"
        For Each measurementName In measurementMethodMap.Keys
            writer.WriteLine(measurementName)

            Dim cmName As String = measurementMethodMap(measurementName)
            If compuMethodMap.ContainsKey(cmName) Then
                Dim ctName As String = compuMethodMap(cmName)
                ' e.g. "CT_TeDF3R_e_MfrsLrnStat"
                If compuVtabMap.ContainsKey(ctName) Then
                    For Each enumValue In compuVtabMap(ctName)
                        writer.WriteLine(enumValue)
                    Next
                End If
            End If

            writer.WriteLine("END")
        Next
    End Sub
    ' Helper function to determine if a line should be processed as a measurement
    Private Function ShouldProcessMeasurement(textLine As String) As Boolean
        ' Must contain _e_
        If Not textLine.Contains("_e_") Then Return False

        ' Must NOT contain '.' 
        '   (equivalent to InStr(textLine, ".") = 0)
        If textLine.Contains(".") Then Return False

        ' If no '[', we are okay
        '   OR
        ' If '[' exists, then the indexOf("_e_") must be before indexOf("[")
        If Not textLine.Contains("[") Then
            Return True
        ElseIf textLine.IndexOf("_e_") < textLine.IndexOf("[") Then
            Return True
        End If

        ' Otherwise, fail
        Return False
    End Function

    ' Helper function to extract the measurement name from the first line of a MEASUREMENT block
    Private Function ExtractMeasurementName(firstLine As String) As String
        Dim measurementName As String = ""

        If Not firstLine.Contains("[") Then
            ' The existing approach:
            Dim tokens As String() = firstLine.Split(New Char() {" "}, StringSplitOptions.RemoveEmptyEntries)
            If tokens.Length >= 3 Then
                ' e.g. "/begin MEASUREMENT MyMeasurementName"
                measurementName = tokens(2)
            End If
        Else
            ' We do have '[', so parse up to bracket or from quotes
            ' for example:
            Dim beginIndex As Integer = firstLine.IndexOf("/begin MEASUREMENT") + 19
            Dim bracketIndex As Integer = firstLine.IndexOf("[")
            If bracketIndex > beginIndex Then
                ' e.g. substring from beginIndex up to bracketIndex
                Dim rawName As String = firstLine.Substring(beginIndex, bracketIndex - beginIndex)
                measurementName = rawName.Trim()
            End If
        End If

        Return measurementName
    End Function

    Private Sub LoadEnumerationFile(ByVal fileName As String, Optional ByVal SaveDeviceName As String = Nothing)
        Dim mySignalEnums As New List(Of SignalEnums)()
        Using reader As New StreamReader(fileName)
            While Not reader.EndOfStream
                ' Read the first line (variable name)
                Dim currentLine As String = reader.ReadLine()
                If String.IsNullOrWhiteSpace(currentLine) Then
                    Continue While
                End If

                ' Create a new signal with the variable name and a default device name
                Dim signal As New SignalEnums() With {
                        .VariableName = currentLine.Trim(),
                        .Enums = New List(Of String)()
                        }

                ' Read subsequent lines until "END" or until file ends
                Dim enumLine As String = If(Not reader.EndOfStream, reader.ReadLine(), Nothing)
                While enumLine IsNot Nothing AndAlso Not enumLine.Equals("END", StringComparison.OrdinalIgnoreCase)
                    ' Optionally handle numeric prefixes or "Ce"/"Ci"/"Cc" checks here
                    If currentLine.StartsWith("Ce") OrElse
                       currentLine.StartsWith("Ci") OrElse
                       currentLine.StartsWith("Cc") Then

                        ' Directly add the enum line
                        signal.Enums.Add(currentLine)
                    Else
                        ' Use Integer.Parse or TryParse if the first 2 chars are numeric
                        signal.Enums.Add(enumLine.Trim())
                    End If
                    If reader.EndOfStream Then Exit While
                    enumLine = reader.ReadLine()
                End While

                ' Add this signal entry to the collection
                mySignalEnums.Add(signal)
            End While
        End Using

        ' At this point, mySignalEnums contains all read data
        ' Store or process it as needed:
        GlobalSignalEnums = mySignalEnums

    End Sub

    Public Sub ReadFinalDataSavePath()
        ' Called from StartStopRecord: Reads FinalDataSavePath.txt file and sets the SaveFinalPathToSaveData variable.
        ' This is called when recording is started. If there is a pathname contained in this file, it means that there
        ' are files from the previous recording session that still need to be processed.
        Dim dataSavePathFileName As String = Path.Combine(My.Application.Info.DirectoryPath, "FinalDataSavePath.txt")
        ' Check if the file exists
        If File.Exists(dataSavePathFileName) Then
            ' Use StreamReader to read the file
            Using reader As New StreamReader(dataSavePathFileName)
                SaveFinalPathToSaveData = reader.ReadLine()
            End Using
        End If
    End Sub

    Public Sub CopyToLog(ByVal inputstr As String, Optional ByVal MessageLogNumber As Integer = 0)
        ' Copies CLEVIR status messages to the GM_ResidentClient.log file with full concurrency protection.

        If MessageLogNumber > MessageLogLevel Then Exit Sub

        inputstr = $"{DateTime.Now:MM/dd/yyyy HH:mm:ss} - {inputstr}"
        Dim logFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "GM_ResidentClient.log")

        Const maxRetries As Integer = 5
        Const initialDelayMs As Integer = 50
        Dim currentRetry As Integer = 0

        While currentRetry < maxRetries
            Try
                ' ✅ CRITICAL FIX: Use FileShare.ReadWrite to allow concurrent writes
                Using fs As New FileStream(logFilePath,
                                      FileMode.Append,
                                      FileAccess.Write,
                                      FileShare.ReadWrite)  ' ← KEY CHANGE
                    Using writer As New StreamWriter(fs)
                        writer.WriteLine(inputstr)
                        writer.Flush()  ' Force write to disk immediately
                    End Using
                End Using

                ' ✅ Success - exit retry loop
                Exit Sub

            Catch ex As IOException When currentRetry < maxRetries - 1
                ' ✅ File is locked - retry with exponential backoff
                currentRetry += 1
                Dim delayMs As Integer = initialDelayMs * CInt(Math.Pow(2, currentRetry - 1))  ' 50ms, 100ms, 200ms, 400ms, 800ms
                Threading.Thread.Sleep(delayMs)

            Catch ex As Exception
                ' ✅ Non-retryable exception (permissions, disk full, etc.)
                ' DO NOT call CopyToCOMMLog here to avoid circular reference
                Debug.WriteLine($"CopyToLog FATAL: {ex.Message}")
                Exit Sub
            End Try
        End While

        ' ✅ All retries exhausted - log to Debug output only
        Debug.WriteLine($"CopyToLog: Failed to write after {maxRetries} attempts: {inputstr}")

        ' ✅ Manage file size (moved outside retry loop for efficiency)
        Try
            ManageLogFileSize(logFilePath)
        Catch ex As Exception
            Debug.WriteLine($"ManageLogFileSize failed: {ex.Message}")
        End Try
    End Sub

    Private Function WaitForFileAccess(ByVal logFilePath As String) As Boolean
        ' Waits and checks if a file is accessible
        If FileInUse(logFilePath) Then
            Threading.Thread.Sleep(100)
            Return Not FileInUse(logFilePath)
        End If
        Return True
    End Function

    Private Sub WriteToLogFile(ByVal logFilePath As String, ByVal inputStr As String)
        ' Writes the input string to the specified log file
        Using writer As New StreamWriter(logFilePath, True) ' True enables appending
            writer.WriteLine(inputStr)
        End Using
    End Sub

    Private Sub ManageLogFileSize(ByVal logFilePath As String)
        ' Manages log file size and copies to network drive if necessary
        Dim logFileInfo As New FileInfo(logFilePath)

        If logFileInfo.Length > 5000000 AndAlso Len(VehicleNumber) > 0 Then
            File.Delete(logFilePath)
        End If

    End Sub

    ' This is called from the ENABLE / DISABLE VOICE CMDS button on the OnVehicleScreen
    ' or, if voice commands are enabled, called on voice command "disable"

    Public Sub ActivateDeactivateVoiceCommands(ByVal sender As Object, ByVal action As String)
        ' ================================================================
        ' Toggles voice command recognition on/off using ACTUAL class methods
        ' ================================================================

        Try
            Dim button As Button = TryCast(sender, Button)
            If button Is Nothing Then
                HandleUserMessageLogging("GMRC", "ActivateDeactivateVoiceCommands: Invalid button reference")
                Return
            End If

            Select Case action.ToLower()
                Case "enabled"
                    ' ═══════════════════════════════════════════════════════
                    ' ENABLE VOICE COMMANDS
                    ' ═══════════════════════════════════════════════════════
                    If VoiceRecognitionInstance Is Nothing Then
                        ' Initialize voice recognition if not already created
                        Try
                            VoiceRecognitionInstance = New VoiceRecognitionClass()
                            VoiceRecognitionInstance.InitVoice()

                            ' Small delay to ensure initialization completes
                            Threading.Thread.Sleep(500)

                            HandleUserMessageLogging("GMRC", "Voice Recognition initialized")

                        Catch ex As Exception
                            HandleUserMessageLogging("GMRC", $"Failed to initialize voice recognition: {ex.Message}", DisplayMsgBox)
                            button.BackColor = SystemColors.Control
                            StatusNotifier.ToastError($"Failed to initialize voice recognition: {ex.Message}", "Voice Recognition", durationMs:=12000, ensureMainOnTop:=False)
                            Return
                        End Try
                    End If

                    ' ✅ Use new convenience method (StartListening = ActivateVoiceRecognition)
                    Try
                        VoiceRecognitionInstance.StartListening()

                        ' Verify activation succeeded
                        If VoiceRecognitionInstance.IsListening Then
                            ' Update button appearance
                            button.BackColor = Color.LightGreen
                            button.ForeColor = Color.Black
                            button.Text = "VOICE"

                            ' Add tooltip to indicate current state
                            Dim tooltip As New ToolTip()
                            tooltip.SetToolTip(button, "Voice Commands ENABLED - Click to disable")

                            HandleUserMessageLogging("GMRC", "Voice Commands ENABLED")

                            ' ✅ CORRECTED: Use Toast with ToastKind.Info
                            StatusNotifier.Toast("Voice Commands Activated", ToastKind.Info, "Voice Recognition", durationMs:=12000, ensureMainOnTop:=False)
                        Else
                            ' Activation failed
                            HandleUserMessageLogging("GMRC", "Voice activation failed - check microphone", DisplayMsgBox)
                            button.BackColor = SystemColors.Control

                            ' ✅ CORRECTED: Use ToastError for error notification
                            StatusNotifier.ToastError("Voice activation failed - check microphone", "Voice Recognition", durationMs:=12000, ensureMainOnTop:=False)
                        End If

                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", $"Failed to activate voice commands: {ex.Message}", DisplayMsgBox)
                        button.BackColor = SystemColors.Control

                        ' ✅ CORRECTED: Use ToastError for exceptions
                        StatusNotifier.ToastError($"Failed to activate voice commands: {ex.Message}", "Voice Recognition", durationMs:=12000, ensureMainOnTop:=False)
                    End Try

                Case "disabled"
                    ' ═══════════════════════════════════════════════════════
                    ' DISABLE VOICE COMMANDS
                    ' ═══════════════════════════════════════════════════════
                    If VoiceRecognitionInstance IsNot Nothing Then
                        Try
                            ' ✅ Use new convenience method (StopListening = DeactivateVoiceRecognition)
                            VoiceRecognitionInstance.StopListening()

                            ' Update button appearance
                            button.BackColor = SystemColors.Control
                            button.ForeColor = Color.Black
                            button.Text = "VOICE"

                            ' Update tooltip
                            Dim tooltip As New ToolTip()
                            tooltip.SetToolTip(button, "Voice Commands DISABLED - Click to enable")

                            HandleUserMessageLogging("GMRC", "Voice Commands DISABLED")

                            ' ✅ CORRECTED: Use Toast with Warning kind for disable notification
                            StatusNotifier.Toast("Voice Commands Deactivated", ToastKind.Warning, "Voice Recognition", durationMs:=12000, ensureMainOnTop:=False)

                        Catch ex As Exception
                            HandleUserMessageLogging("GMRC", $"Failed to disable voice commands: {ex.Message}")

                            ' ✅ CORRECTED: Use ToastError for exceptions
                            StatusNotifier.ToastError($"Failed to disable voice commands: {ex.Message}", "Voice Recognition", durationMs:=12000, ensureMainOnTop:=False)
                        End Try
                    Else
                        ' Already disabled/not initialized
                        button.BackColor = SystemColors.Control
                        button.ForeColor = Color.Black
                        button.Text = "VOICE"

                        Dim tooltip As New ToolTip()
                        tooltip.SetToolTip(button, "Voice Commands DISABLED - Click to enable")

                        HandleUserMessageLogging("GMRC", "Voice Commands not initialized")

                        ' ✅ CORRECTED: Use Toast for informational message
                        StatusNotifier.Toast("Voice Commands not initialized", ToastKind.Info, "Voice Recognition", durationMs:=12000, ensureMainOnTop:=False)
                    End If

                Case Else
                    HandleUserMessageLogging("GMRC", $"Invalid action parameter: {action}")

                    ' ✅ CORRECTED: Use ToastError for invalid parameter
                    StatusNotifier.ToastError($"Invalid action parameter: {action}", "Voice Recognition", durationMs:=12000, ensureMainOnTop:=False)
            End Select

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ActivateDeactivateVoiceCommands: {ex.Message}", DisplayMsgBox)

            ' ✅ CORRECTED: Use ToastError for top-level exceptions
            StatusNotifier.ToastError($"Voice command toggle error: {ex.Message}", "Voice Recognition", durationMs:=12000, ensureMainOnTop:=False)
        End Try
    End Sub

    Public Sub MicrophoneClick(ByVal sender As System.Object)
        'Called from the PictureBox1_Click event on the OnVehicleScreen or on the GmResidentClient screen.

        If MyIncaInterface.Recording = True Then
            If sender.BackColor = Color.Green Then
                ' Stop the current WAV recording using the values captured at StartWAVRecord
                Dim filenameToStop As String = CurrentWAVFilename
                If String.IsNullOrEmpty(filenameToStop) Then
                    filenameToStop = audioFilePath
                End If

                If IsWAVRecordingActive() Then
                    StopWAVRecord(filenameToStop)
                Else
                    HandleUserMessageLogging("GMRC", "MicrophoneClick: No active WAV recording to stop.")
                End If

                UpdateUIElementsForStop()
            End If
        Else
            If sender.parent.name = "OnVehicleScreen" Then
                sender.parent.Label5.Text = "Cannot start audio unless Recording Data in INCA...."
            Else
                sender.parent.parent.Label5.Text = "Cannot start audio unless Recording Data in INCA...."
            End If
        End If
    End Sub

    Public Sub RedisplayOnVehicleForm(ByVal mode As String)

        ' This sub is called from various places to ensure the main vehicle screen is visible and correctly configured.
        ' It should not interfere with other forms that are intentionally displayed on top.

        Try
            If OperatingMode = OperatingModes.ResOnVpc Then
                ' Ensure GmResidentClient is hidden and not topmost.
                If GmResidentClient IsNot Nothing AndAlso Not GmResidentClient.IsDisposed Then
                    GmResidentClient.TopMost = False
                    GmResidentClient.Hide()
                End If

                Dim frm = OnVehicleScreen
                If frm IsNot Nothing AndAlso Not frm.IsDisposed Then
                    ' Activate and BringToFront are generally sufficient without causing flicker.
                    ' The TopMost property is set to True to ensure it stays on top of other non-modal windows.
                    frm.TopMost = True
                    frm.BringToFront()
                    frm.Activate()
                    ' Configure the form's controls based on the mode.
                    If mode <> "GroupBox1" Then
                        frm.GroupBox1.Visible = False
                        frm.TextBox1.Visible = True
                        frm.TextBox1.BringToFront()
                    Else
                        frm.GroupBox1.Visible = True
                        frm.TextBox1.Visible = False
                    End If
                    frm.Refresh() ' Refresh to ensure UI updates are drawn.
                End If
            Else
                If GmResidentClient IsNot Nothing AndAlso Not GmResidentClient.IsDisposed Then
                    GmResidentClient.TopMost = False
                End If
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "RedisplayOnVehicleForm (UI): " & ex.Message)
        End Try
    End Sub

    Public Sub DetermineAlternateRecordMode()
        ' ═══════════════════════════════════════════════════════════════════
        ' REFACTORED: Read explicit config, then validate file existence
        ' This eliminates implicit detection and gives users full control
        ' ═══════════════════════════════════════════════════════════════════

        Dim baseDir As String = My.Application.Info.DirectoryPath

        ' Read desired mode from config.xml (defaults to "None" if not set)
        Dim desiredMode As String = If(String.IsNullOrWhiteSpace(AlternateRecordConfig), "None", AlternateRecordConfig.Trim().ToUpper())

        ' Reset UI label if it exists
        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
            OnVehicleScreen.Label3.Visible = False
        End If

        ' ═══════════════════════════════════════════════════════════════════
        ' EXPLICIT MODE SELECTION WITH VALIDATION
        ' ═══════════════════════════════════════════════════════════════════
        Select Case desiredMode
            Case "VEHICLESPY"
                Dim vspyConfigPath As String = IO.Path.Combine(baseDir, "VehicleSpy", VSpySelectedConfigFileName)
                If IO.File.Exists(vspyConfigPath) Then
                    AlternateRecordingMode = "VehicleSpy"
                    AlternateRecordEnabled = True
                    HandleUserMessageLogging("GMRC", $"✓ Alternate recording: VehicleSpy configured ({vspyConfigPath})")
                Else
                    ' ✅ EXPLICIT ERROR MESSAGE
                    HandleUserMessageLogging("GMRC",
                        $"WARNING: AlternateRecordConfig=VehicleSpy in config.xml but file not found: {vspyConfigPath}",
                        DisplayMsgBox)
                    AlternateRecordingMode = "None"
                    AlternateRecordEnabled = False
                End If

            Case "CANALYZER"
                Dim canConfigPath As String = IO.Path.Combine(baseDir, "CANalyzer", "CANalyzer.cfg")
                If IO.File.Exists(canConfigPath) Then
                    AlternateRecordingMode = "CANalyzer"
                    AlternateRecordEnabled = True
                    HandleUserMessageLogging("GMRC", $"✓ Alternate recording: CANalyzer configured ({canConfigPath})")
                Else
                    ' ✅ EXPLICIT ERROR MESSAGE
                    HandleUserMessageLogging("GMRC",
                        $"WARNING: AlternateRecordConfig=CANalyzer in config.xml but file not found: {canConfigPath}",
                        DisplayMsgBox)
                    AlternateRecordingMode = "None"
                    AlternateRecordEnabled = False
                End If

            Case "NONE"
                AlternateRecordingMode = "None"
                AlternateRecordEnabled = False
                HandleUserMessageLogging("GMRC", "Alternate recording: Explicitly disabled in config.xml")

            Case Else
                ' ✅ UNKNOWN VALUE - WARN USER
                HandleUserMessageLogging("GMRC",
                    $"ERROR: Invalid AlternateRecordConfig value: '{AlternateRecordConfig}'. Must be VehicleSpy, CANalyzer, or None. Defaulting to None.",
                    DisplayMsgBox)
                AlternateRecordingMode = "None"
                AlternateRecordEnabled = False
        End Select

        ' ✅ UI UPDATE REMOVED: LoginForm will read AlternateRecordingMode 
        ' and update its own CheckBox3 when the form loads
    End Sub

    Private Sub StartWAVRecord(ByVal preciseElapsedMs As Integer, ByVal audioFilename As String)
        ' This will deactivate voice recognition if it is activated,
        ' then open a new WAV file for voice recording.

        Dim myVoiceRecognition = DataDictionarySingleton.VoiceRecognitionManager.Instance

        Try
            If waveIn IsNot Nothing Then
                HandleUserMessageLogging("GMRC", "WARNING: Starting new recording while waveIn exists - cleaning up first")
                Try
                    RemoveHandler waveIn.RecordingStopped, AddressOf waveIn_RecordingStopped
                    waveIn.StopRecording()
                    DisposeWaveFile()
                    DisposeWaveIn()
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Error cleaning up existing waveIn: {ex.Message}")
                End Try
            End If

            ' Always derive from the active MF4 sequence to avoid duplicate "_01"
            Dim activeSeq As String = GetCurrentActiveSequence()
            Dim currentSequenceNumber As Integer = GetSequenceNumberFromFileName(activeSeq)

            Dim directory As String = Path.GetDirectoryName(audioFilename)
            Dim correctedAudioFilename As String = BuildWavFilenameFromActiveSequence(activeSeq, preciseElapsedMs, directory)

            audioFilePath = correctedAudioFilename
            CurrentWAVFilename = correctedAudioFilename
            CurrentWAVSequence = currentSequenceNumber

            HandleUserMessageLogging("GMRC", $"Starting WAV recording: {Path.GetFileName(correctedAudioFilename)} (Sequence: {currentSequenceNumber:D2})")


            CreateAndConfigureWaveInEvent(correctedAudioFilename)

            If waveIn IsNot Nothing Then
                AddHandler waveIn.RecordingStopped, AddressOf waveIn_RecordingStopped
                waveIn.StartRecording()
            Else
                HandleUserMessageLogging("GMRC", "waveIn is not initialized.")
                Return
            End If

            UpdateUIElementsForStart()
            LogEvent(LogEventType.AudioStart)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "StartWAVRecord Exception: " & ex.ToString(), DisplayMsgBox)
        End Try
    End Sub

    Private Sub waveIn_RecordingStopped(sender As Object, e As StoppedEventArgs)
        Try
            ' NOTE: This event handler should now rarely fire since we remove it before stopping
            ' But keep it as a safety net for automatic stops (buffer full, etc.)

            HandleUserMessageLogging("GMRC", "waveIn_RecordingStopped event fired (unexpected)")

            ' Remove the event handler to prevent memory leaks
            If sender IsNot Nothing Then
                RemoveHandler DirectCast(sender, WaveInEvent).RecordingStopped, AddressOf waveIn_RecordingStopped
            End If

            ' Dispose of waveFile if it still exists
            DisposeWaveFile()

            ' Access the singleton instance
            Dim myVoiceRecognition = DataDictionarySingleton.VoiceRecognitionManager.Instance

            ' Reactivate voice recognition if it was previously active
            If myVoiceRecognition.VoiceWasActivated Then
                Try
                    myVoiceRecognition.ActivateVoiceRecognition()
                    myVoiceRecognition.VoiceWasActivated = False
                    myVoiceRecognition.VoiceActivated = True
                    HandleUserMessageLogging("GMRC", "Voice recognition reactivated from event handler.")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", "Error reactivating voice recognition: " & ex.Message)
                End Try
            End If

            ' Dispose of waveIn
            DisposeWaveIn()

            ' Update UI elements safely
            UpdateUIElementsSafely()

            ' Handle any exceptions from the recording
            If e.Exception IsNot Nothing Then
                HandleUserMessageLogging("GMRC", "RecordingStopped Exception: " & e.Exception.Message)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "waveIn_RecordingStopped Exception: " & ex.Message)
        End Try
    End Sub

    ' Helper method to safely update UI elements on the UI thread
    Private Sub UpdateUIElementsSafely()
        ' Check if OnVehicleScreen is not Nothing
        If OnVehicleScreen IsNot Nothing Then
            If OnVehicleScreen.InvokeRequired Then
                ' Use BeginInvoke to avoid potential deadlocks
                OnVehicleScreen.BeginInvoke(New MethodInvoker(AddressOf UpdateUIElementsForStop))
            Else
                'UpdateUIElementsForStop()
                'Console.WriteLine("UpdateUIElementsForStop() is called from withn UpdateUIElementsSafely()")
            End If
        Else
            ' Handle the case where OnVehicleScreen is not initialized
            HandleUserMessageLogging("GMRC", "OnVehicleScreen is not available to update UI elements.")
        End If
    End Sub

    Private Function BuildWavFilenameFromActiveSequence(activeSequenceMf4 As String, elapsedMs As Integer, Optional directory As String = Nothing) As String
        ' activeSequenceMf4 is like: 20250805_155733_DRVR00_6SME5384_01.mf4
        Dim baseWithSeq As String = Path.GetFileNameWithoutExtension(activeSequenceMf4) ' -> ..._01
        Dim name As String = $"{baseWithSeq}_{elapsedMs}.wav"
        If String.IsNullOrEmpty(directory) Then
            Return name
        Else
            Return Path.Combine(directory, name)
        End If
    End Function

    Private Function SpellOutAcronyms(text As String) As String
        ' ✅ Define acronyms you want spelled out (add more as needed)
        Dim replacements As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                {"ACC", "ADAPTIVE CRUISE CONTROL"},
                {"LiDAR1", "Liedar-one"},
                {"LiDAR2", "Liedar-two"},
                {"LKA", "LANE KEEP ASSIST"},
                {"FIM", "FRONT IMPACT MITIGATION"},
                {"TSM", "TRAFFIC SIGN RECOGNITION"},
                {"WAV", "W.A.V."},
                {"LAT CTRL", "LATERAL CONTROL"},
                {"HMI", "HUMAN MACHINE INTERFACE"},
                {"SLSNA", "SPEED LIMIT SIGN NORTH AMERICA"},
                {"SLSEU", "SPEED LIMIT SIGN EUROPEAN UNION"},
                {"DMS", "DRIVER MONITORING SYSTEM"},
                {"KSS", "KAROLINSKA SLEEPINESS SCALE"},
                {"HUD", "H.U.D."},
                {"RVB", "REAR VIRTUAL BUMPER"},
                {"APA", "ADVANCED PARKING ASSIST"},
                {"AEB", "AUTOMATIC EMERGENCY BRAKING"},
                {"FCW", "FORWARD COLLISION WARNING"},
                {"RCTA", "REAR CROSS TRAFFIC ALERT"},
                {"BSM", "BLIND SPOT MONITORING"},
                {"LDW", "LANE DEPARTURE WARNING"},
                {"LCA", "LANE CHANGE ASSIST"},
                {"RCTB", "REAR CROSS TRAFFIC BRAKING"},
                {"VCU", "VEHICLE COCKPIT UNIT"},
                {"SC", "SUPER CRUISE"},
                {"IACC", "INTELLIGENT ADAPTIVE CRUISE CONTROL"},
                {"PED", "PEDESTRIAN DETECTION"},
                {"FCA", "FORWARD COLLISION ALERT"},
                {"ICM", "INTERSECTION COLLISION MITIGATION"}
                }

        For Each kvp In replacements
            ' ✅ Match whole words only using Regex
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                $"\b{System.Text.RegularExpressions.Regex.Escape(kvp.Key)}\b",
                kvp.Value,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                )
        Next

        Return text
    End Function
    Private Sub ProvideFeedbackToUser(Optional ByVal synth As SpeechSynthesizer = Nothing,
                                      Optional ByVal message As String = Nothing,
                                      Optional ByVal tabButtonFeedback As String = Nothing,
                                      Optional ByVal fullText As String = Nothing)
        If GmResidentClient.MuteVoiceRecordingMessages = False Then
            If synth Is Nothing Then
                synth = New SpeechSynthesizer()
                synth.SelectVoice("Microsoft Zira Desktop")
                synth.Rate = 0
            End If

            Dim messageToSpeak As String
            If Not String.IsNullOrEmpty(tabButtonFeedback) AndAlso Not "ANNOTATION".Equals(message, StringComparison.OrdinalIgnoreCase) Then
                messageToSpeak = If(String.IsNullOrEmpty(fullText), tabButtonFeedback, fullText)
            ElseIf Not String.IsNullOrEmpty(message) Then
                messageToSpeak = message
            Else
                messageToSpeak = "Event Recording Canceled"
            End If

            ' ✅ APPLY SPELLING CONVERSION
            messageToSpeak = SpellOutAcronyms(messageToSpeak)

            synth.SpeakAsync(messageToSpeak)
        End If
    End Sub

    ' Get the current active sequence
    Public Function GetCurrentActiveSequence() As String
        Try
            Dim info = GetCurrentRecordingInfo()
            Dim result As String = $"{info.BaseName}_{info.Sequence:D2}.mf4"
            HandleUserMessageLogging("GMRC", $"GetCurrentActiveSequence: Final result={result} (Base: {info.BaseName}, Sequence: {info.Sequence})")
            Return result
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"GetCurrentActiveSequence error: {ex.Message}")
            ' Fallback to original filename if available
            If Not String.IsNullOrEmpty(SaveRecordingFileName) Then
                Return SaveRecordingFileName
            Else
                Return $"{SelectedTestName}_{DateTime.Now:HHmmss}_01.mf4"
            End If
        End Try
    End Function

    Private Function ParseTmpFileName(tmpFileName As String) As (sequenceNumber As Integer, baseName As String)
        Try
            ' Example: 20250805_155733_DRVR00_6SME5384_01.mf4.2025-08-05-15-57-38.mf4.tmp

            ' Remove the .tmp extension first
            If tmpFileName.EndsWith(".tmp") Then
                tmpFileName = tmpFileName.Substring(0, tmpFileName.Length - 4)
            End If

            ' Find the first occurrence of ".mf4." to split the filename
            Dim mf4DotIndex As Integer = tmpFileName.IndexOf(".mf4.")
            If mf4DotIndex > 0 Then
                ' Get the base part before the timestamp
                Dim baseWithSequence As String = tmpFileName.Substring(0, mf4DotIndex)

                ' Find the last underscore to separate base name from sequence
                Dim lastUnderscoreIndex As Integer = baseWithSequence.LastIndexOf("_")
                If lastUnderscoreIndex >= 0 AndAlso lastUnderscoreIndex < baseWithSequence.Length - 1 Then
                    Dim sequencePart As String = baseWithSequence.Substring(lastUnderscoreIndex + 1)
                    Dim basePart As String = baseWithSequence.Substring(0, lastUnderscoreIndex)

                    ' Validate sequence number
                    If IsNumeric(sequencePart) AndAlso sequencePart.Length <= 3 Then ' Max 3 digits for sequence
                        Return (CInt(sequencePart), basePart)
                    End If
                End If
            End If

            ' If parsing failed, return defaults
            Return (1, "")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ParseTmpFileName error: {ex.Message}")
            Return (1, "")
        End Try
    End Function

    Private Function DefineAudioFilePath(preciseElapsedMs As Integer)
        ' Centralize WAV path creation to avoid duplicate "_01"
        Dim activeSeq As String = GetCurrentActiveSequence()
        Dim baseWithSeq As String = Path.GetFileNameWithoutExtension(activeSeq)
        audioFilePath = Path.Combine(FinalPathToSaveData, $"{baseWithSeq}_{preciseElapsedMs}.wav")
        Return audioFilePath
    End Function

    Private Sub CreateAndConfigureWaveInEvent(ByVal audioFilename As String)
        waveIn = New WaveInEvent() With {
        .WaveFormat = New WaveFormat(16000, 16, 1) ' Set recording format: 16kHz, 16-bit, mono
    }
        waveFile = New WaveFileWriter(audioFilename, waveIn.WaveFormat)
        AddHandler waveIn.DataAvailable, AddressOf OnDataAvailable
    End Sub

    Private Sub UpdateUIElementsForStart()
        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
            OnVehicleScreen.PictureBox1.BackColor = Color.Green
            OnVehicleScreen.PictureBox1.Image = My.Resources.Resources.mic_50_Green()
        End If
    End Sub

    Private Sub LogEvent(type As LogEventType)
        Dim finalMessage As String = ""
        Dim sequenceNumToLog As Integer

        Select Case type
            Case LogEventType.AudioStart
                ' For starting, the sequence number has just been captured in CurrentWAVSequence.
                sequenceNumToLog = CurrentWAVSequence
                finalMessage = $"Audio Recording started at {DateTime.Now:HH:mm:ss} for sequence {sequenceNumToLog:D2}"
            Case LogEventType.AudioStop
                ' For stopping, ALWAYS use the sequence number stored when the recording began.
                ' This is the key to fixing the bug.
                sequenceNumToLog = CurrentWAVSequence
                finalMessage = $"Audio Recording stopped at {DateTime.Now:HH:mm:ss} for sequence {sequenceNumToLog:D2}"
        End Select

        If Not String.IsNullOrEmpty(finalMessage) Then
            MyIncaInterface.WriteEventComment(finalMessage, False)
        End If
    End Sub

    Public Sub StopWAVRecord(audioFilename As String)
        ' Called to stop the WAV recording and save the file to the path and filename that was established.
        Dim synth As New SpeechSynthesizer()
        synth.SelectVoice("Microsoft Zira Desktop")
        synth.Rate = 0

        Try
            ' Stop the recording
            If waveIn IsNot Nothing Then
                ' IMPORTANT: Remove the event handler BEFORE stopping to prevent conflicts
                RemoveHandler waveIn.RecordingStopped, AddressOf waveIn_RecordingStopped
                waveIn.StopRecording()
                ' Dispose immediately and synchronously
                DisposeWaveFile()
                DisposeWaveIn()
                HandleUserMessageLogging("GMRC", "WAV recording stopped and disposed synchronously")
            End If

            LogEvent(LogEventType.AudioStop)
            ' Provide feedback to user if needed
            ProvideFeedbackToUser(synth, "END: Audio")

            ' Handle voice recognition reactivation immediately if needed
            Dim myVoiceRecognition = DataDictionarySingleton.VoiceRecognitionManager.Instance
            If myVoiceRecognition.VoiceWasActivated Then
                Try
                    myVoiceRecognition.ActivateVoiceRecognition()
                    myVoiceRecognition.VoiceWasActivated = False
                    myVoiceRecognition.VoiceActivated = True
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", "Error reactivating voice recognition: " & ex.Message)
                End Try
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "StopWAVRecord Exception: " & ex.Message & " audioFilePath = " & audioFilename & audioFilePath)
        Finally
            ' Clear captured state to avoid stale reuse
            CurrentWAVFilename = ""
            CurrentWAVSequence = 0
        End Try
    End Sub

    ' Method to dispose of waveIn
    Private Sub DisposeWaveIn()
        If waveIn IsNot Nothing Then
            Try
                waveIn.StopRecording()
                waveIn.Dispose()
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", "Error disposing waveIn: " & ex.Message)
            Finally
                waveIn = Nothing
            End Try
        End If
    End Sub

    Private Sub DisposeWaveFile()
        SyncLock waveFileLock
            If waveFile IsNot Nothing Then
                Try
                    ' Dispose of the waveFile object
                    waveFile.Dispose()
                Catch ex As Exception
                    ' Log any exceptions that occur during disposal
                    HandleUserMessageLogging("GMRC", "Error disposing waveFile: " & ex.Message)
                Finally
                    ' Ensure waveFile is set to Nothing to avoid further use
                    waveFile = Nothing
                End Try
            End If
        End SyncLock
    End Sub

    Private Sub UpdateUIElementsForStop()
        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
            OnVehicleScreen.PictureBox1.BackColor = Color.Red
            OnVehicleScreen.PictureBox1.Image = My.Resources.Resources.mic_50_Red()
        End If
    End Sub

    ' Method to handle incoming audio data
    Private Sub OnDataAvailable(sender As Object, e As WaveInEventArgs)
        SyncLock waveFileLock
            Try
                ' Write audio data to the file as it becomes available,
                ' but only if the waveFile object has not been disposed.
                waveFile?.Write(e.Buffer, 0, e.BytesRecorded)
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", "OnDataAvailable Exception: " & ex.Message)
            End Try
        End SyncLock
    End Sub

    Public Sub CreateANNOFile()
        ' Called from SetupDataLogging when the START RECORD or START MEASUREMENT buttons are pressed.
        ' Creates the Annotation file for the recording session.

        Dim snapshotTime As Date = DateTime.Now

        Try
            Using writer As New StreamWriter(ANNOFileName, False)
                ' Build dynamic LiDAR header columns
                Dim lidarHeaders As String = BuildLidarHeaders()

                Dim lines As New List(Of String) From {
                "0, Field ID,Field Name,Value",
                "1,98,CSV Version,0",                      ' ✅ Kept for backward compatibility
                "1,99,CLIHA Version,1",                    ' ✅ Kept for backward compatibility
                $"1,1,Session Name,{SaveSelectedTestName}",
                $"1,2,Driver,{SaveLoginID}",
                $"1,16,Email,{SaveEmailAddress}",          ' ✅ NEW: Email field
                $"1,17,Group,{SaveGroupName}",             ' ✅ NEW: Group field
                "1,4,Country,1",
                "1,3,State,23",
                $"1,5,Start Date,{snapshotTime:MM/dd/yyyy}",
                $"1,6,Start Time,{snapshotTime:HH:mm:ss}",
                $"1,7,End Date,{snapshotTime:MM/dd/yyyy}",
                $"1,8,End Time,{snapshotTime:HH:mm:ss}",
                $"1,9,Notes,{SessionComments}",
                $"1,10,Procedure,{SaveProcedureName}",     ' ✅ WIRED UP: Existing field
                $"1,12,Vehicle,{VehicleNumber}",
                "1,13,Thumbnail,-1",
                "1,14,RecordedMileage,0",
                "1,15,LCCActiveMileage,0",
                $"0,Anno Type ID,Anno Type,Anno Value ID,Anno Value,Anno Enum Type,Anno Enum,Start Seq#,Start (ms),End Seq#,End (ms),Point Seq#,MDA Point (sec),Thumbnail,WAV,MF4 Filename,Mileage,LAT POS,LON POS,LaneClass_Crnt{lidarHeaders}"
            }

                For Each line As String In lines
                    writer.WriteLine(line)
                Next
            End Using
        Catch ex As Exception
            CopyToLog("CreateANNOFile " & ex.Message)
        End Try
    End Sub

    Public Sub WriteFinalPathToSaveData()
        ' Called from uploaddata screen when upload data button is pressed. Also called from StartStopMeasurement and StarStopRecord
        ' Stores the data save path for the current recording session (BaseDataCollectionPath & "\Data\gmcsv" & VehicleNumber & "\" & mySelectedTestName)
        ' This is necessary because when recording is stopped, the last recorded file is not zipped. This to save time if the user
        ' wishes to exit right after stopping. So the zipping of the final file only happens just prior to uploading files. So, if CLEVIR
        ' is exited and then launched prior to uploading files, CLEVIR needs to know where the most recent file that still needs to be zipped is located
        ' so the file can be zipped at the next Start Record event for a new session...

        Try
            HandleUserMessageLogging("GMRC", "WriteFinalPathToSaveData Called...")

            Dim fileName As String = Path.Combine(My.Application.Info.DirectoryPath, "FinalDataSavePath.txt")

            ' Use modern StreamWriter with Using statement for automatic resource disposal
            Using writer As New StreamWriter(fileName, append:=False)
                writer.WriteLine(SaveFinalPathToSaveData)
            End Using

            HandleUserMessageLogging("GMRC", "WriteFinalPathToSaveData: Successfully wrote path to file")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"WriteFinalPathToSaveData: Error writing to file - {ex.Message}")
        End Try
    End Sub

    Public Sub Button_Click(ByVal sender As Object, ByVal e As EventArgs)
        Dim thisButton As Button = DirectCast(sender, Button)
        Dim buttonText As String = thisButton.Text
        Dim parentText As String = thisButton.Parent.Text


        ' Check if the button has associated sub-categories
        Dim annotationIndex As Integer = FindAnnotationIndex(parentText)
        Dim z As Integer = GetZValue(annotationIndex)
        Dim i As Integer = GetIValue(z, buttonText)

        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        Dim subTab = dataDictionary.SubTabs.Values.ElementAtOrDefault(z)
        Dim eventButton = If(i >= 0 AndAlso i < subTab.EventButtons.Count, subTab.EventButtons(i), Nothing)

        ' If there are sub-categories, display the list box for selection and exit
        If eventButton IsNot Nothing AndAlso eventButton.SubCategories.Any(Function(sc) Not String.IsNullOrEmpty(sc)) Then
            DisplayAnnotationListBox(buttonText, z, i)
            Exit Sub
        End If

        ' No sub-categories, proceed to handle annotation
        HandleAnnotationButtons(sender, parentText)
    End Sub

    ' Centralized handler for annotation buttons with optional full annotation text
    Public Sub HandleAnnotationButtons(
    ByVal buttonText As Object,
    Optional ByVal buttonParentText As String = "",
    Optional ByVal fullAnnotationText As String = "",
    Optional ByVal subCategoryName As String = "",
    Optional ByVal listIndex As Integer = 0,
    Optional ByVal listBoxSelected As Boolean = False
    )
        ' Safely cast sender to Button using TryCast
        Dim thisButton As Button = TryCast(buttonText, Button)
        If thisButton Is Nothing Then Exit Sub ' Exit if sender is not a Button

        ' Capture timing information IMMEDIATELY when button is pressed
        Dim buttonPressTime As DateTime = DateTime.Now
        Dim incaTimeAtPress As Integer = MyIncaInterface.GetActualRecordingTimeMs()
        Dim preciseElapsedMs As Double = incaTimeAtPress  ' Already computed

        ' ============================================================
        ' CAPTURE LIDAR FRAME NUMBERS AT ANNOTATION TIME
        ' ============================================================
        Dim lidarFrameSnapshot As String = GetLidarFrameSnapshot()

        ' Create a SpeechSynthesizer instance for feedback
        Dim synth As New SpeechSynthesizer()
        synth.SelectVoice("Microsoft Zira Desktop")
        synth.Rate = 0

        ' Determine the parent text of the button
        ' Filter out unwanted content from the parent text
        Dim sanitizedParentText As String = If(String.IsNullOrEmpty(buttonParentText),
                                       thisButton?.Parent?.Text?.Trim(),
                                       buttonParentText.Trim())

        ' Combine the sanitized parent text with the button text
        Dim tabButtonFeedback = $"{sanitizedParentText} - {buttonText.Text}"

        ' Validate if conditions are correct for recording
        If Not ValidateRecordingConditions() Then Exit Sub

        ' Check if the button press is a duplicate using precise timing
        If IsDuplicatePress(preciseElapsedMs) Then Exit Sub

        ' Initialize the event comment and audio filename
        Dim eventComment As String = ""
        Dim audioFilename As String = String.Empty

        ' Get the CURRENT sequence number from active recording
        Dim currentActiveSequence As String = GetCurrentActiveSequence()
        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

        ' Log for debugging
        'HandleUserMessageLogging("GMRC", $"HandleAnnotationButtons: Using sequence number {currentSeq} from {currentActiveSequence}")

        ' If user selected something from a ListBox
        If listBoxSelected Then
            eventComment = $"{fullAnnotationText} {subCategoryName}"
        End If

        ' Handle WAV recording for non-"ANNOTATION" buttons
        If thisButton.Text <> "ANNOTATION" Then
            ' Reset RecordWAVTime on button press
            GmResidentClient.HandleWavRecording(buttonPressed:=True)
        End If

        ' Process custom or standard annotations
        Dim result As (eventComment As String, AudioFilename As String, AlreadyWritten As Boolean)
        ' Use preciseElapsedMs consistently throughout
        If thisButton.Text.ToUpper() = "ANNOTATION" Then
            ' Process as a custom annotation
            result = HandleCustomAnnotationButton(
            thisButton,
            sanitizedParentText,
            fullAnnotationText,
            preciseElapsedMs,
            currentSeq,
            buttonPressTime,
            subCategoryName,
            listBoxSelected,
            listIndex,
            lidarFrameSnapshot
        )
            eventComment = result.eventComment
        Else
            ' Process as a standard annotation
            result = HandleStandardAnnotationButton(
            thisButton,
            sanitizedParentText,
            fullAnnotationText,
            preciseElapsedMs,
            currentSeq,
            buttonPressTime,
            subCategoryName,
            listBoxSelected,
            listIndex,
            lidarFrameSnapshot
        )
            eventComment = result.eventComment
        End If

        ' Update eventComment and audioFilename with the results

        audioFilename = result.AudioFilename
        Dim currentEventAlreadyWritten As Boolean = result.AlreadyWritten  ' <- Renamed for clarity

        ' Wait until the annotation is allowed
        While Not recordingAllowed AndAlso thisButton IsNot Nothing AndAlso thisButton.Text = "ANNOTATION"
            Application.DoEvents() ' Allow other events to be processed
            Threading.Thread.Sleep(100) ' Sleep for a short period to avoid busy-waiting
        End While

        ' If the user has not canceled, audioFilename will be populated
        If (Not String.IsNullOrEmpty(audioFilename)) AndAlso MyIncaInterface.Recording AndAlso IsNumeric(OnVehicleScreen.TextBox1.Text) Then
            ' Access the singleton instance for voice recognition
            Dim myVoiceRecognition = DataDictionarySingleton.VoiceRecognitionManager.Instance

            If IsWAVRecordingActive() Then
                ' The filename to stop is the one associated with the currently active WAV recording.
                Dim filenameToStop As String = If(Not String.IsNullOrEmpty(CurrentWAVFilename), CurrentWAVFilename, audioFilePath)
                ' Call the refactored StopWAVRecord, which no longer requires the sequence number.
                ' It correctly uses the sequence number that was stored when the recording started.
                StopWAVRecord(filenameToStop)
            End If

            ' Deactivate voice recognition before starting a new recording
            If myVoiceRecognition.VoiceActivated Then
                ' Store the current voice recognition state
                myVoiceRecognition.VoiceWasActivated = myVoiceRecognition.VoiceActivated
                myVoiceRecognition.DeactivateVoiceRecognition()
            Else
                myVoiceRecognition.VoiceWasActivated = False
            End If

            ' Start a new WAV recording for this annotation
            StartWAVRecord(CInt(preciseElapsedMs), audioFilename)
        End If

        ' Create event with precise timing - BUT ONLY IF THIS SPECIFIC EVENT NOT ALREADY WRITTEN
        If Not String.IsNullOrEmpty(eventComment) AndAlso Not currentEventAlreadyWritten Then  ' <- Use current event flag
            ' FIXED: Build MF4 filename with current sequence number
            'Dim currentMf4Filename As String = BuildSequencedFilename(SaveRecordingFileName, currentSeq, "", ".mf4")
            Dim currentMf4Filename As String = Path.GetFileName(currentActiveSequence)

            ' Compile additional time-critical information with correct MF4filename
            Dim baseData As String = String.Join(",",
                                                 currentMf4Filename,
                                                 Format(CurrentMileage, "0.0"),
                                                 Format(CurrentLatitude, "0.0000"),
                                                 Format(CurrentLongitude, "0.0000"),
                                                 Format(LaneClassCurrent, "0"))

            ' Conditionally append LiDAR frame numbers ONLY if LiDAR is enabled
            Dim timeCriticalInfo As String = baseData
            If LidarCaptureEnabled AndAlso LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 Then
                If Not String.IsNullOrEmpty(lidarFrameSnapshot) Then
                    ' Use actual frame numbers
                    timeCriticalInfo &= "," & lidarFrameSnapshot
                Else
                    ' Use -1 placeholders (LiDAR enabled but no data yet)
                    timeCriticalInfo &= "," & String.Join(",", Enumerable.Repeat("-1", LidarDevices.Count))
                End If
            End If
            ' Otherwise: LiDAR disabled → append nothing (columns remain empty)

            ' Write to INCA immediately with precise timing
            WriteToIncaWithRetry(eventComment, lidarFrameSnapshot, preciseElapsedMs, buttonPressTime)

            ' Buffer the CSV write for near-immediate processing
            BufferCsvWrite(eventComment, timeCriticalInfo, buttonPressTime)

            ' ✅ OPTIMIZED: Inject event marker into all active LiDAR captures
            If LidarCaptureStarted Then
                Try
                    ' Parse eventComment to extract structured metadata
                    ' Format examples:
                    '   "3,1000,BUTTON FEEDBACK,1,ACC Event - Forward Vehicle Approach,..."
                    '   "3,1000,ANNOTATION FEEDBACK,1,ANNOTATION,..."

                    Dim eventType As String = "ANNOTATION"
                    Dim eventMessage As String = thisButton.Text

                    ' Extract event type from CSV field 3 (e.g., "BUTTON FEEDBACK")
                    Dim csvParts As String() = eventComment.Split(","c)
                    If csvParts.Length >= 3 Then
                        eventType = csvParts(2).Trim()  ' "BUTTON FEEDBACK" or "ANNOTATION FEEDBACK"
                    End If

                    ' Extract full description from CSV field 5 (e.g., "ACC Event - Forward Vehicle Approach")
                    If csvParts.Length >= 5 Then
                        eventMessage = csvParts(4).Trim()  ' Full description with parent tab context
                    End If

                    ' Inject marker into PCAP for all active devices
                    InjectEventMarker(eventType, eventMessage, currentSeq)

                    HandleUserMessageLogging("GMRC", $"Injected '{eventType}' marker into {LidarDevices.Count} LiDAR capture(s)")

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Failed to inject LiDAR event marker: {ex.Message}")
                End Try
            End If

            HandleUserMessageLogging("GMRC", $"Standard annotation buffered for writing: {eventComment}")

        ElseIf Not String.IsNullOrEmpty(eventComment) AndAlso currentEventAlreadyWritten Then
            ' Still write to INCA even if files were already written (for custom annotations)
            WriteToIncaWithRetry(eventComment, lidarFrameSnapshot, preciseElapsedMs, buttonPressTime)
            HandleUserMessageLogging("GMRC", "Custom annotation already written to files - skipping CSV buffer")
        End If

        ' Similar to ProcessEventBuffer but specifically for INCA write retries
        Dim eventsToRetry As List(Of PendingEvent)
        SyncLock eventBufferLock
            eventsToRetry = eventBuffer.Where(Function(e) e.TimeCriticalInfo.Contains("INCA_RETRY")).ToList()
            ' Remove INCA retry events from main buffer
            eventBuffer = eventBuffer.Where(Function(e) Not e.TimeCriticalInfo.Contains("INCA_RETRY")).ToList()
        End SyncLock

        For Each pendingEvent In eventsToRetry
            Try
                ' Retry writing to INCA
                MyIncaInterface.WriteEventComment($"{pendingEvent.eventComment} [Seq{currentSeq:D2}@{preciseElapsedMs:F0}ms] @{pendingEvent.buttonPressTime:HH:mm:ss.fff}", True)
            Catch ex As Exception
                ' Handle retry failure
                If pendingEvent.RetryCount < pendingEvent.MaxRetries Then
                    pendingEvent.RetryCount += 1
                    SyncLock eventBufferLock
                        eventBuffer.Add(pendingEvent)
                    End SyncLock
                Else
                    HandleUserMessageLogging("GMRC", $"Failed to write to INCA after {pendingEvent.MaxRetries} retries: {ex.Message}")
                End If
            End Try
        Next

        ' Provide user feedback for standard annotation buttons only
        If Not String.IsNullOrEmpty(thisButton?.Text) AndAlso thisButton.Text.ToUpper() <> "ANNOTATION" Then
            ProvideFeedbackToUser(synth, thisButton.Text, tabButtonFeedback, fullAnnotationText)
        End If
    End Sub

    Private Sub WriteToIncaWithRetry(eventComment As String, lidarFrameNumbers As String, elapsedMs As Double, timestamp As DateTime)
        Try
            ' Check if INCA is actually recording
            If Not MyIncaInterface.Recording Then
                HandleUserMessageLogging("GMRC", "WARNING: Attempted to write event but INCA is not recording!")
                Return
            End If

            ' Use the sequence context method for better timing precision across MF4 boundaries
            WriteEventWithSequenceContext(eventComment, elapsedMs, lidarFrameNumbers, timestamp)

            ' Log successful write
            HandleUserMessageLogging("GMRC", $"Successfully wrote to INCA: {eventComment}")

        Catch ex As Exception
            ' ✅ FIXED: Use BufferIncaWrite for INCA retry
            HandleUserMessageLogging("GMRC", $"INCA write failed, buffering for retry: {ex.Message}")
            BufferIncaWrite(eventComment, elapsedMs, timestamp, lidarFrameNumbers)
        End Try
    End Sub

    Private Async Sub BufferCsvWrite(eventComment As String, timeCriticalInfo As String, timestamp As DateTime)
        SyncLock eventBufferLock
            eventBuffer.Add(New PendingEvent With {
                               .eventComment = eventComment,
                               .TimeCriticalInfo = timeCriticalInfo,
                               .buttonPressTime = timestamp,
                               .RetryCount = 0,
                               .MaxRetries = 3
                               })
        End SyncLock

        ' Process buffer asynchronously
        Await ProcessEventBuffer()
    End Sub

    Private Async Sub BufferIncaWrite(eventComment As String, elapsedMs As Double, timestamp As DateTime, lidarFrameNumbers As String)
        SyncLock eventBufferLock
            eventBuffer.Add(New PendingEvent With {
                               .eventComment = $"{eventComment} @{elapsedMs:F0}ms",
                               .TimeCriticalInfo = $"{timestamp:o}|{If(String.IsNullOrEmpty(lidarFrameNumbers), String.Join(",", Enumerable.Repeat("-1", Math.Max(LidarDevices.Count, 1))), lidarFrameNumbers)}",
                               .buttonPressTime = timestamp,
                               .RetryCount = 0,
                               .MaxRetries = 3
                               })
        End SyncLock
        ' Process buffer asynchronously
        Await ProcessEventBuffer()
    End Sub


    Private Async Sub MonitorMf4Transition()
        If DateTime.Now.Subtract(lastMf4Check).TotalMilliseconds > 500 Then
            lastMf4Check = DateTime.Now

            ' Use the new API for more precise timing
            Dim remainingMs As Integer = MyIncaInterface.GetRemainingRecordingTimeMs()

            If remainingMs > 0 Then
                ' We have precise remaining time
                mf4TransitionImminent = remainingMs < 2000 ' 2 seconds

                If mf4TransitionImminent Then
                    ' Force immediate processing with precise timing
                    Await ProcessEventBuffer()

                    ' Prepare for next sequence transition
                    PrepareForSequenceTransition(remainingMs)
                End If
            Else
                ' Fallback to calculation using configurable duration
                Dim currentRecordingTime As Double = MyIncaInterface.GetActualRecordingTimeMs()
                Dim sequenceDurationMs As Double = RecordFileDurationMinutes * 60 * 1000
                Dim elapsedInCurrentSequence As Double = currentRecordingTime Mod sequenceDurationMs
                Dim remainingInSequence As Double = sequenceDurationMs - elapsedInCurrentSequence

                mf4TransitionImminent = remainingInSequence < 2000 ' Within 2 seconds

                If mf4TransitionImminent Then
                    Await ProcessEventBuffer()
                    PrepareForSequenceTransition(CInt(remainingInSequence))
                End If
            End If
        End If
    End Sub


    Private Async Sub PrepareForSequenceTransition(remainingMs As Integer)
        Try
            ' Log the upcoming transition
            HandleUserMessageLogging("GMRC", $"MF4 sequence transition in {remainingMs}ms - preparing buffers")

            ' Force immediate processing of any pending events before transition
            Await ProcessEventBuffer()

            ' If we're very close to transition (< 1 second), pause new event writes briefly
            If remainingMs < 1000 Then
                ' Flag to temporarily hold new annotations until transition completes
                recordingAllowed = False

                ' Wait a brief moment for the transition to occur
                Await Task.Delay(Math.Min(remainingMs + 500, 1500))

                ' ✅ PERFORMANCE: Invalidate cache after sequence transition
                InvalidateRecordingInfoCache()

                ' Re-enable recording after transition
                recordingAllowed = True
                HandleUserMessageLogging("GMRC", "MF4 sequence transition completed - resuming annotations (cache invalidated)")
            End If

            ' Clear any timing flags
            mf4TransitionImminent = False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"PrepareForSequenceTransition error: {ex.Message}")
            ' Ensure recording is re-enabled even if there's an error
            recordingAllowed = True
        End Try
    End Sub

    Private Async Function ProcessEventBuffer() As Task
        ' Don't process if shutdown is in progress or another process is already running
        If Not recordingAllowed OrElse isProcessingBuffer Then
            Return
        End If

        isProcessingBuffer = True

        Try
            Dim eventsToProcess As List(Of PendingEvent)

            SyncLock eventBufferLock
                eventsToProcess = eventBuffer.ToList()
                eventBuffer.Clear()
            End SyncLock

            If eventsToProcess.Count = 0 Then
                Return
            End If

            For Each pendingEvent In eventsToProcess
                Try
                    ' Write to CSV with original timestamp preserved
                    Await WriteToAnnotationFileAsync(pendingEvent.eventComment, pendingEvent.TimeCriticalInfo)
                    Await WriteToAggregateAnnoFileAsync(pendingEvent.eventComment, pendingEvent.TimeCriticalInfo)

                Catch ex As Exception
                    ' Only retry if shutdown is not in progress
                    If recordingAllowed AndAlso pendingEvent.RetryCount < pendingEvent.MaxRetries Then
                        pendingEvent.RetryCount += 1
                        SyncLock eventBufferLock
                            eventBuffer.Add(pendingEvent)
                        End SyncLock
                    Else
                        HandleUserMessageLogging("GMRC", $"Failed to write event after {pendingEvent.MaxRetries} retries: {ex.Message}")
                    End If
                End Try
            Next
        Finally
            isProcessingBuffer = False
        End Try
    End Function

    Private Async Sub ProcessPendingEvents(sender As Object, e As System.Timers.ElapsedEventArgs)
        MonitorMf4Transition()
        Await ProcessEventBuffer()
    End Sub

    Private Sub WriteEventWithSequenceContext(eventComment As String, preciseElapsedMs As Double, lidarFrameNumbers As String, buttonPressTime As DateTime)
        Try
            ' Get current sequence number from the active .tmp file or SaveRecordingFileName
            Dim currentActiveSequence As String = GetCurrentActiveSequence()
            Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

            ' Calculate remaining time in current sequence using configurable duration
            Dim sequenceDurationMs As Double = RecordFileDurationMinutes * 60 * 1000
            Dim elapsedInCurrentSequence As Double = preciseElapsedMs Mod sequenceDurationMs
            Dim remainingMs As Double = sequenceDurationMs - elapsedInCurrentSequence

            ' ✅ Build LiDAR frame suffix for INCA comment
            Dim lidarSuffix As String = ""
            If Not String.IsNullOrEmpty(lidarFrameNumbers) Then
                lidarSuffix = $" LiDAR:{lidarFrameNumbers}"
            Else
                ' Include placeholder if LiDAR is configured but not capturing
                If LidarCaptureEnabled AndAlso LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 Then
                    Dim emptyFrames As String = String.Join(",", Enumerable.Repeat("-1", LidarDevices.Count))
                    lidarSuffix = $" LiDAR:{emptyFrames}"
                End If
            End If

            ' Determine if we're close to a sequence boundary
            If remainingMs < 2000 Then ' Within 2 seconds of transition
                Dim nextSeq As Integer = currentSeq + 1

                ' ✅ Write to current sequence WITH LiDAR frames
                MyIncaInterface.WriteEventComment($"{eventComment} [Seq{currentSeq:D2}@{preciseElapsedMs:F0}ms]{lidarSuffix} @{buttonPressTime:HH:mm:ss.fff}", True)

                ' ✅ Also write a preparation event for the next sequence WITH LiDAR frames
                Dim nextSeqStartMs As Double = preciseElapsedMs + remainingMs
                MyIncaInterface.WriteEventComment($"{eventComment} [NextSeq{nextSeq:D2}@{nextSeqStartMs:F0}ms]{lidarSuffix}", True)

                HandleUserMessageLogging("GMRC", $"Sequence transition imminent: Current={currentSeq:D2}, Next={nextSeq:D2}, Remaining={remainingMs:F0}ms")
            Else
                ' ✅ Normal write WITH LiDAR frames - we're not near a sequence boundary
                MyIncaInterface.WriteEventComment($"{eventComment} [Seq{currentSeq:D2}@{preciseElapsedMs:F0}ms]{lidarSuffix} @{buttonPressTime:HH:mm:ss.fff}", True)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"WriteEventWithSequenceContext failed: {ex.Message}")
            ' Fallback to simple write if sequence context fails
            Try
                MyIncaInterface.WriteEventComment($"{eventComment} @{preciseElapsedMs:F0}ms", True)
            Catch fallbackEx As Exception
                HandleUserMessageLogging("GMRC", $"Fallback event write also failed: {fallbackEx.Message}")
            End Try
        End Try
    End Sub

    Private Async Function WriteToAnnotationFileAsync(
                                                      eventComment As String,
                                                      timeCriticalInfo As String,
                                                      Optional preciseElapsedMs As Double = 0,
                                                      Optional buttonPressTime As DateTime = Nothing
                                                      ) As Task
        Const maxRetries As Integer = 3
        Dim currentRetry As Integer = 0

        While currentRetry <= maxRetries
            Try
                ' Check file access first
                If IsFileInUse(ANNOFileName) AndAlso currentRetry < maxRetries Then
                    ' Move delay outside the try-catch
                    Dim delayTime As Integer = 100 * (currentRetry + 1)
                    currentRetry += 1
                    Await Task.Delay(delayTime)
                    Continue While
                ElseIf IsFileInUse(ANNOFileName) Then
                    Throw New IOException($"Annotation file is still in use after {maxRetries} retry attempts")
                End If

                ' Use async file operations to prevent UI blocking
                Using fs As New FileStream(ANNOFileName, FileMode.Append, FileAccess.Write, FileShare.Read)
                    Using writer As New StreamWriter(fs)
                        Await writer.WriteLineAsync($"{eventComment},{timeCriticalInfo}")
                        Await writer.FlushAsync()
                    End Using
                End Using

                ' Success - exit the retry loop
                Return

            Catch ex As IOException When ex.Message.Contains("being used by another process") AndAlso currentRetry < maxRetries
                ' File is in use - don't use Await here
                currentRetry += 1
                ' Continue to the delay outside the catch block

            Catch ex As Exception
                ' Non-retryable exception
                HandleUserMessageLogging("GMRC", $"WriteToAnnotationFileAsync failed: {ex.Message}")
                Throw
            End Try

            ' Delay outside the try-catch block
            If currentRetry <= maxRetries Then
                Dim exponentialDelay As Integer = CInt(100 * Math.Pow(2, currentRetry - 1))
                Await Task.Delay(exponentialDelay)
            End If
        End While

        ' If we get here, all retries were exhausted
        Throw New IOException($"Failed to write to annotation file after {maxRetries} attempts")
    End Function

    Private Async Function WriteToAggregateAnnoFileAsync(eventComment As String, timeCriticalInfo As String) As Task
        Dim aggregateFileName As String = Path.Combine(My.Application.Info.DirectoryPath, $"{VehicleNumber}_AggregateAnnotations.csv")
        Const maxRetries As Integer = 3
        Dim currentRetry As Integer = 0

        While currentRetry <= maxRetries
            Try
                Dim fileExists As Boolean = File.Exists(aggregateFileName)

                ' Use FileShare.ReadWrite to allow concurrent access - this is the key fix
                Using fs As New FileStream(aggregateFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)
                    Using writer As New StreamWriter(fs)
                        ' Write header if file doesn't exist (first time creating the file)
                        If Not fileExists Then
                            Dim lidarHeaders As String = BuildLidarHeaders()  ' ✅ Generate headers dynamically
                            Await writer.WriteLineAsync($"0,Anno Type ID,Anno Type,Anno Value ID,Anno Value,Anno Enum Type,Anno Enum,Start Seq#,Start (ms),End Seq#,End (ms),Point Seq#,MDA Point (sec),Thumbnail,WAV,MF4 Filename,Mileage,LAT POS,LON POS,LaneClass_Crnt{lidarHeaders}")
                        End If

                        Await writer.WriteLineAsync($"{eventComment},{timeCriticalInfo}")
                        Await writer.FlushAsync()
                    End Using
                End Using

                ' Success - exit the retry loop
                Return

            Catch ex As IOException When currentRetry < maxRetries
                ' File access error - increment retry counter
                currentRetry += 1
                HandleUserMessageLogging("GMRC", $"WriteToAggregateAnnoFileAsync retry {currentRetry}/{maxRetries}: {ex.Message}")

                ' Delay OUTSIDE the catch block to comply with VB.NET 4.8
                ' (Continue to the delay section below)

            Catch ex As Exception
                ' Non-retryable exception
                HandleUserMessageLogging("GMRC", $"WriteToAggregateAnnoFileAsync failed: {ex.Message}")
                Throw
            End Try

            ' Delay outside the try-catch block to comply with VB.NET 4.8 limitations
            If currentRetry <= maxRetries Then
                Dim exponentialDelay As Integer = CInt(50 * Math.Pow(2, currentRetry - 1)) ' Start with 50ms, then 100ms, 200ms
                Await Task.Delay(exponentialDelay)
            End If
        End While

        ' If we get here, all retries were exhausted
        Throw New IOException($"Failed to write to aggregate annotation file after {maxRetries} attempts")
    End Function
    'Helper function to check if a file is in use
    Private Function IsFileInUse(filePath As String) As Boolean
        Try
            Using fs As New FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None)
                Return False
            End Using
        Catch ex As IOException
            Return True
        End Try
    End Function

    ' Check if a WAV recording is currently active
    Private Function IsWAVRecordingActive() As Boolean
        ' Check if waveIn and waveFile are not Nothing and ensure voice recognition is not active
        'Return isRecording
        Return waveIn IsNot Nothing AndAlso waveFile IsNot Nothing
    End Function

    ' Validation of recording conditions
    Private Function ValidateRecordingConditions() As Boolean
        If String.IsNullOrEmpty(ANNOFileName) OrElse Not MyIncaInterface.Recording Then
            MsgBox("You must be in RECORD MODE to enable Writing User Annotations.")
            Return False
        End If

        If OnVehicleScreen.GroupBox5.Visible Then
            Return False
        End If

        If Not File.Exists(ANNOFileName) Then
            CreateANNOFile()
        End If

        Return True
    End Function

    ' Check for duplicate button presses
    Private Function IsDuplicatePress(ByVal preciseElapsedMs As Double) As Boolean
        Static SaveMSec As Double
        If CInt(preciseElapsedMs) = CInt(SaveMSec) Then Return True
        SaveMSec = preciseElapsedMs
        Return False
    End Function

    ' Get sequence number from the filename
    Public Function GetSequenceNumberFromFileName(fileName As String) As Integer
        If String.IsNullOrEmpty(fileName) OrElse Not fileName.Contains(".mf4") Then
            Return 1 ' Default fallback
        End If

        ' Normalize unresolved template tokens if present
        fileName = System.Text.RegularExpressions.Regex.Replace(fileName, "(?i)&CNT\d+", "")
        fileName = System.Text.RegularExpressions.Regex.Replace(fileName, "(?i)\[CNT\d+\]", "")

        ' Find the last underscore before the .mf4 extension
        Dim mf4Index As Integer = fileName.LastIndexOf(".mf4")
        Dim lastUnderscoreIndex As Integer = fileName.LastIndexOf("_", mf4Index)

        If lastUnderscoreIndex > -1 AndAlso lastUnderscoreIndex < mf4Index - 1 Then
            Dim sequencePart As String = fileName.Substring(lastUnderscoreIndex + 1, mf4Index - (lastUnderscoreIndex + 1))
            If IsNumeric(sequencePart) Then
                Return CInt(sequencePart)
            End If
        End If

        ' No explicit sequence suffix found -> treat as first file in sequence
        Return 1
    End Function

    Public Function GetBaseFileName(ByVal fullFileName As String) As String
        If String.IsNullOrEmpty(fullFileName) Then
            Return String.Empty
        End If

        ' First, remove the .mf4 extension
        Dim nameWithoutExt As String = Path.GetFileNameWithoutExtension(fullFileName)

        ' Remove unresolved template tokens if present
        nameWithoutExt = System.Text.RegularExpressions.Regex.Replace(nameWithoutExt, "(?i)&CNT\d+", "")
        nameWithoutExt = System.Text.RegularExpressions.Regex.Replace(nameWithoutExt, "(?i)\[CNT\d+\]", "")

        ' ✅ DIAGNOSTIC: Detect if duplicate sequences exist (should NEVER happen - indicates upstream bug)
        If System.Text.RegularExpressions.Regex.IsMatch(nameWithoutExt, "_\d{1,3}_\d{1,3}$") Then
            HandleUserMessageLogging("GMRC", $"⚠️ BUG DETECTED: Duplicate sequence pattern in filename: {fullFileName}", DisplayMsgBox)
        End If

        ' ✅ SIMPLIFIED FIX: Remove ONLY the last _## pattern (the sequence number)
        ' Trust that upstream code doesn't create duplicates - if they exist, it's a bug to fix at source
        nameWithoutExt = System.Text.RegularExpressions.Regex.Replace(nameWithoutExt, "_\d{1,3}$", "")

        ' Clean up any trailing delimiters
        Return nameWithoutExt.TrimEnd("_"c, "-"c, " "c)
    End Function

    ''' <summary>
    ''' Gets the base name and current sequence number of the active recording.
    ''' ✅ PERFORMANCE OPTIMIZATION: Uses caching to avoid repeated disk I/O operations.
    ''' Cache is invalidated when sequence changes or recording stops.
    ''' </summary>
    ''' <param name="forceRefresh">If True, bypasses cache and queries file system (default: False)</param>
    ''' <returns>A tuple containing the base filename and the current sequence number.</returns>
    Public Function GetCurrentRecordingInfo(Optional forceRefresh As Boolean = False) As (BaseName As String, Sequence As Integer)
        Try
            ' ✅ PERFORMANCE: Check cache first (avoids expensive disk I/O)
            SyncLock recordingCacheLock
                If Not forceRefresh AndAlso Not String.IsNullOrEmpty(cachedRecordingBaseName) AndAlso cachedRecordingSequence > 0 Then
                    ' Cache is valid and recent - return cached values
                    If DateTime.Now.Subtract(cacheLastUpdated).TotalSeconds < 2 Then
                        Return (cachedRecordingBaseName, cachedRecordingSequence)
                    End If
                End If
            End SyncLock

            ' Cache miss or stale - perform expensive file system query
            Dim sequenceNumber As Integer = 1
            Dim baseName As String = ""

            ' 1. Prioritize finding the active .tmp file for the most accurate information
            If Not String.IsNullOrEmpty(FinalPathToSaveData) AndAlso Directory.Exists(FinalPathToSaveData) Then
                Dim tmpFiles As String() = Directory.GetFiles(FinalPathToSaveData, "*.tmp")

                If tmpFiles.Length > 0 Then
                    ' Find the most recently written .tmp file
                    Dim mostRecentTmp As String = tmpFiles.OrderByDescending(Function(f) File.GetLastWriteTime(f)).First()
                    Dim tmpFileName As String = Path.GetFileName(mostRecentTmp)

                    ' The .tmp file format is like: {basename}_{seq}.mf4.{timestamp}.mf4.tmp
                    ' We need to extract the original "{basename}_{seq}.mf4" part
                    Dim firstMf4Index As Integer = tmpFileName.IndexOf(".mf4.")
                    If firstMf4Index > 0 Then
                        Dim originalFileName As String = tmpFileName.Substring(0, firstMf4Index + 4) ' Include .mf4
                        baseName = GetBaseFileName(originalFileName)
                        sequenceNumber = GetSequenceNumberFromFileName(originalFileName)

                        ' ✅ REMOVED: No longer add vehicle number here - it's already in baseName
                        If Not String.IsNullOrEmpty(baseName) AndAlso sequenceNumber > 0 Then
                            ' ✅ PERFORMANCE: Update cache before returning
                            SyncLock recordingCacheLock
                                cachedRecordingBaseName = baseName
                                cachedRecordingSequence = sequenceNumber
                                cacheLastUpdated = DateTime.Now
                            End SyncLock
                            Return (baseName, sequenceNumber)
                        End If
                    End If
                End If
            End If

            ' 2. Fallback to using the globally saved recording filename
            If Not String.IsNullOrEmpty(SaveRecordingFileName) Then
                baseName = GetBaseFileName(SaveRecordingFileName)
                sequenceNumber = GetSequenceNumberFromFileName(SaveRecordingFileName)

                ' ✅ REMOVED: No longer add vehicle number here - it's already in baseName
                If Not String.IsNullOrEmpty(baseName) AndAlso sequenceNumber > 0 Then
                    ' ✅ PERFORMANCE: Update cache before returning
                    SyncLock recordingCacheLock
                        cachedRecordingBaseName = baseName
                        cachedRecordingSequence = sequenceNumber
                        cacheLastUpdated = DateTime.Now
                    End SyncLock
                    Return (baseName, sequenceNumber)
                End If
            End If

            ' 3. Ultimate fallback if no other information is available
            ' ✅ FIXED: Use SelectedTestName which already includes vehicle number
            baseName = SelectedTestName

            ' ✅ PERFORMANCE: Update cache even for fallback values
            SyncLock recordingCacheLock
                cachedRecordingBaseName = baseName
                cachedRecordingSequence = 1
                cacheLastUpdated = DateTime.Now
            End SyncLock
            Return (baseName, 1)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Error in GetCurrentRecordingInfo: {ex.Message}")
            ' Return safe defaults on error - SelectedTestName already has vehicle number
            Return (SelectedTestName, 1)
        End Try
    End Function

    ''' <summary>
    ''' Invalidates the recording info cache.
    ''' Call this when sequence changes (MF4 rotation) or recording stops.
    ''' </summary>
    Public Sub InvalidateRecordingInfoCache()
        SyncLock recordingCacheLock
            cachedRecordingBaseName = ""
            cachedRecordingSequence = 0
            cacheLastUpdated = DateTime.MinValue
        End SyncLock
    End Sub

    ' Handle custom annotation button logic
    Private Function HandleCustomAnnotationButton(
    ByVal thisButton As Button,
    sanitizedParentText As String,
    ByVal parentText As String,
    ByVal preciseElapsedMs As Double,
    ByVal currentSequenceNumber As Integer, ' <- Renamed for clarity
    ByVal buttonPressTime As Date,
    Optional ByVal selectedSubCategory As String = "",
    Optional ByVal listBoxSelected As Boolean = False,
    Optional ByVal listIndex As Integer = 0,
    Optional ByVal lidarFrameSnapshot As String = ""
    ) As (eventComment As String, AudioFilename As String, AlreadyWritten As Boolean)

        Dim dataDictionary = DataDictionarySingleton.GetInstance() ' Reference to the Singleton
        ' Instantiate the CustomAnnotation form and populate the list
        Dim customAnnotationForm As New CustomAnnotation
        Dim buttonIndex As Integer = 0 ' ANNOTATION button index
        customAnnotationForm.ListBox1.Items.Clear()

        ' Construct the audio file names from the active MF4 sequence to avoid "_01_01"
        Dim timestamp As String = Format(preciseElapsedMs, "0")
        Dim baseFromActive As String = Path.GetFileNameWithoutExtension(GetCurrentActiveSequence())
        Dim annoAudioFilename As String = $"{baseFromActive}_{timestamp}.wav"
        Dim audioFilename As String = Path.Combine(FinalPathToSaveData, annoAudioFilename)

        ' FIX: Use sanitizedParentText (which should be "ACC") instead of parentText
        Dim saveCustomAnnoFileName As String = Path.Combine(My.Application.Info.DirectoryPath, $"{sanitizedParentText}_SavedCustomAnnotations.txt")
        ' Add debugging to see what's happening
        HandleUserMessageLogging("GMRC", $"Looking for annotation file: {saveCustomAnnoFileName}")
        HandleUserMessageLogging("GMRC", $"File exists: {File.Exists(saveCustomAnnoFileName)}")

        ' Load saved custom annotations based on the file path defined previously
        Dim savedAnnotations() As String = customAnnotationForm.ReadCustomAnnotationsFile(saveCustomAnnoFileName)

        ' Debug: Log how many annotations were read
        HandleUserMessageLogging("GMRC", $"Read {savedAnnotations.Length} annotations from file")

        customAnnotationForm.ListBox1.Items.AddRange(savedAnnotations)
        customAnnotationForm.Text = $"{sanitizedParentText} Custom Annotation"
        ' ----------------------------------------
        ' Show the form and check which button was pressed
        Dim result As DialogResult = customAnnotationForm.ShowDialog()
        If result = DialogResult.Cancel Then
            ' User clicked Cancel (Button 2). Return empty values for the tuple.
            Return (String.Empty, String.Empty, False)  ' <- Changed third parameter to False
        ElseIf result = DialogResult.OK Then
            GmResidentClient.HandleWavRecording(buttonPressed:=True)
        End If

        ' ----------------------------------------
        ' Initialize EventComment to ensure it is always assigned
        Dim eventComment As String = String.Empty
        Dim alreadyWritten As Boolean = False
        ' If the user pressed OK (Button 1), proceed.
        If Len(customAnnotationForm.TextBox1.Text) > 0 Then
            ' Find the annotation index in the data dictionary
            Dim annotationIndex As Integer = FindAnnotationIndex(sanitizedParentText)
            If annotationIndex >= 0 AndAlso annotationIndex < dataDictionary.AnnotationValueRecords.Count Then
                ' Retrieve the existing record directly from the singleton
                Dim existingRecord As DataDictionarySingleton.AnnotationValueRecord = dataDictionary.AnnotationValueRecords(annotationIndex)
                ' Update the SaveTextString directly
                Dim tempStr As String = customAnnotationForm.TextBox1.Text.Replace(",", "-")
                existingRecord.SaveTextString = tempStr
                ' Update the modified record in the data dictionary
                dataDictionary.AnnotationValueRecords(annotationIndex) = existingRecord
                ' Build the event comment
                Dim descriptionText As String = $"{tempStr}"
                ' Use the active sequence MF4 filename directly
                Dim currentMf4Filename As String = GetCurrentActiveSequence()
                ' Build the event comment with the selected sub-category
                eventComment = BuildEventComment(
                    annotationIndex,
                    buttonIndex,
                    currentSequenceNumber,
                    preciseElapsedMs,
                    buttonPressTime,
                    "ANNOTATION FEEDBACK",
                    descriptionText,
                    annoAudioFilename,
                    selectedSubCategory,
                    lidarFrameSnapshot  ' ✅ Add this parameter
                )

                ' Compile additional time-critical information with correct MF4 filename
                Dim baseData As String = String.Join(",",
                                                     currentMf4Filename,
                                                     Format(CurrentMileage, "0.0"),
                                                     Format(CurrentLatitude, "0.0000"),
                                                     Format(CurrentLongitude, "0.0000"),
                                                     Format(LaneClassCurrent, "0"))

                ' Conditionally append LiDAR data
                Dim timeCriticalInfo As String = baseData
                If LidarCaptureEnabled AndAlso LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 Then
                    If Not String.IsNullOrEmpty(lidarFrameSnapshot) Then
                        timeCriticalInfo &= "," & lidarFrameSnapshot
                    Else
                        timeCriticalInfo &= "," & String.Join(",", Enumerable.Repeat("-1", LidarDevices.Count))
                    End If
                End If

                ' Direct file writing for custom annotations
                Try
                    ' Write directly to annotation files to ensure they're immediately available
                    Using fs As New FileStream(ANNOFileName, FileMode.Append, FileAccess.Write, FileShare.Read)
                        Using writer As New StreamWriter(fs)
                            writer.WriteLine($"{eventComment},{timeCriticalInfo}")
                            writer.Flush()
                        End Using
                    End Using

                    ' Also write to aggregate file
                    Dim aggregateFileName As String = Path.Combine(My.Application.Info.DirectoryPath, $"{VehicleNumber}_AggregateAnnotations.csv")
                    Dim fileExists As Boolean = File.Exists(aggregateFileName)

                    ' Inject marker into PCAP for all active devices
                    InjectEventMarker("ANNOTATION FEEDBACK", descriptionText, currentSequenceNumber)

                    Using fs As New FileStream(aggregateFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)
                        Using writer As New StreamWriter(fs)
                            If Not fileExists Then
                                Dim lidarHeaders As String = BuildLidarHeaders()  ' ✅ Generate headers dynamically
                                writer.WriteLine($"0,Anno Type ID,Anno Type,Anno Value ID,Anno Value,Anno Enum Type,Anno Enum,Start Seq#,Start (ms),End Seq#,End (ms),Point Seq#,MDA Point (sec),Thumbnail,WAV,MF4 Filename,Mileage,LAT POS,LON POS,LaneClass_Crnt{lidarHeaders}")
                            End If
                            writer.WriteLine($"{eventComment},{timeCriticalInfo}")
                            writer.Flush()
                        End Using
                    End Using

                    ' Mark that we've already written to files
                    alreadyWritten = True

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Error writing custom annotation to files: {ex.Message}")
                    ' If writing failed, set alreadyWritten to False so the main handler can try
                    alreadyWritten = False
                End Try
                ' Update the annotation text in the data dictionary
                UpdateCustomAnnotationText(annotationIndex, tempStr)
                ' Update the saved annotations for the current file
                Dim updatedAnnotations() As String = customAnnotationForm.ListBox1.Items.Cast(Of String).ToArray()
                ' Add the new annotation to the saved list if it's not already there
                If Not updatedAnnotations.Contains(tempStr) Then
                    Array.Resize(updatedAnnotations, updatedAnnotations.Length + 1)
                    updatedAnnotations(updatedAnnotations.Length - 1) = tempStr
                End If
                ' Write the updated annotations back to the file using the form's method
                customAnnotationForm.WriteCustomAnnotationFile(saveCustomAnnoFileName, updatedAnnotations)
            Else
                HandleUserMessageLogging("GMRC", "Annotation index out of range in HandleCustomAnnotationButton.", DisplayMsgBox)
            End If
        End If
        ' Return with the alreadyWritten flag
        Return (eventComment, audioFilename, alreadyWritten)
    End Function

    ' HandleStandardAnnotationButton including selectedSubCategory in eventComment
    Private Function HandleStandardAnnotationButton(
                                                    ByVal thisButton As Button,
                                                    ByVal parentText As String,
                                                    ByVal fullAnnotationText As String,
                                                    ByVal preciseElapsedMs As Double,
                                                    ByVal currentSequenceNumber As Integer,
                                                    ByVal buttonPressTime As DateTime,
                                                    Optional ByVal selectedSubCategory As String = "",
                                                    Optional ByVal listBoxSelected As Boolean = False,
                                                    Optional ByVal listIndex As Integer = 0,
                                                    Optional ByVal lidarFrameSnapshot As String = ""
                                                    ) As (eventComment As String, AudioFilename As String, AlreadyWritten As Boolean)  ' <- Added AlreadyWritten flag

        ' Get the annotation index based on the parent text
        Dim annotationIndex As Integer = FindAnnotationIndex(parentText)
        ' Initialize EventComment to ensure it is always assigned
        Dim eventComment As String = String.Empty

        ' Initialize variables for description text and button index
        Dim descriptionText As String
        Dim buttonIndex As Integer

        If listBoxSelected Then
            ' Use the selected sub-category from the list box
            descriptionText = $"{thisButton.Text}: {selectedSubCategory}"
            buttonIndex = listIndex
        Else
            ' Find the description text and button index without the list box
            descriptionText = FindDescriptionText(thisButton.Text)
            Dim z As Integer = GetZValue(annotationIndex)
            buttonIndex = GetIValue(z, thisButton.Text)
        End If

        ' Construct the audio file name from the active sequence to avoid "_01_01"
        Dim timestamp As String = Format(preciseElapsedMs, "0")
        Dim baseFromActive As String = Path.GetFileNameWithoutExtension(GetCurrentActiveSequence())
        Dim annoAudioFilename As String = $"{baseFromActive}_{timestamp}.wav"
        Dim audioFilename As String = Path.Combine(FinalPathToSaveData, annoAudioFilename)

        ' Initialize eventComment to ensure it is always assigned
        ' Build the event comment with the selected sub-category
        eventComment = BuildEventComment(
            annotationIndex,
            buttonIndex,
            currentSequenceNumber,
            preciseElapsedMs,
            buttonPressTime,
            "BUTTON FEEDBACK",
            descriptionText,
            annoAudioFilename,
            selectedSubCategory,
            lidarFrameSnapshot  ' ✅ Add this parameter
        )

        ' Return the eventComment and audioFilename as a tuple
        Return (eventComment, audioFilename, False)
    End Function

    ' GetZValue function uses EnumerationType as Sub-tab ID (if SubTabID is not explicitly available)
    Private Function GetZValue(ByVal annotationIndex As Integer) As Integer
        Dim dataDictionary = DataDictionarySingleton.GetInstance() ' Reference to the Singleton
        Dim subTabID = dataDictionary.AnnotationValueRecords(annotationIndex).EnumerationType ' Use EnumerationType as SubTabID

        ' Find the index of the SubTab in SubTabs that matches the SubTabID
        Dim zIndex As Integer = dataDictionary.SubTabs.Values.ToList().FindIndex(Function(subTab) subTab.TabId = subTabID)

        Return zIndex
    End Function

    ' GetIValue function uses EventButton based on ButtonName from SubTab
    Private Function GetIValue(ByVal z As Integer, ByVal buttonText As String) As Integer
        Dim dataDictionary = DataDictionarySingleton.GetInstance() ' Reference to the Singleton
        Dim subTab = dataDictionary.SubTabs.Values.ElementAtOrDefault(z)

        If subTab IsNot Nothing Then
            ' Find index of the EventButton within the SubTab that matches the ButtonName
            Dim buttonIndex As Integer = subTab.EventButtons.FindIndex(Function(eventButton) eventButton.ButtonName = buttonText)

            Return buttonIndex

        End If

        Return -1 ' Return -1 if no match is found or if SubTab is missing
    End Function

    Private Function BuildSequencedFilename(baseRecordingFileName As String, currentSequence As Integer, timestamp As String, extension As String) As String
        Dim baseFileName As String = Path.GetFileNameWithoutExtension(baseRecordingFileName)

        ' Parse and replace sequence number in filename
        If baseFileName.Contains("_") Then
            Dim parts As String() = baseFileName.Split("_"c)
            If parts.Length >= 5 AndAlso IsNumeric(parts(4)) Then
                ' Replace the sequence part (index 4) with current sequence
                parts(4) = currentSequence.ToString("D2")
                baseFileName = String.Join("_", parts)
            Else
                ' Fallback: append current sequence
                baseFileName = $"{baseFileName}_{currentSequence:D2}"
            End If
        Else
            ' No underscores found, append sequence
            baseFileName = $"{baseFileName}_{currentSequence:D2}"
        End If

        Return $"{baseFileName}_{timestamp}{extension}"
    End Function

    Private Function BuildEventComment(
                                       ByVal annotationIndex As Integer,
                                       ByVal buttonIndex As Integer,
                                       ByVal sequenceNumber As Integer,
                                       ByVal preciseElapsedMs As Double,
                                       ByVal buttonPressTime As DateTime,
                                       ByVal feedbackType As String,
                                       ByVal descriptionText As String,
                                       ByVal annoAudioFilename As String,
                                       Optional ByVal selectedSubCategory As String = "",
                                       Optional ByVal lidarFrameNumbers As String = "" ' <-- NEW PARAMETER
                                       ) As String

        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        Dim annotationValue = dataDictionary.AnnotationValueRecords(annotationIndex)
        Dim eventTypeID = annotationValue.TypeId
        Dim subTabID = annotationValue.EnumerationType
        Dim preciseSecondsFinal As Double = (preciseElapsedMs / 1000)

        Dim timestampStr As String = If(buttonPressTime = DateTime.MinValue,
                                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                        buttonPressTime.ToString("yyyy-MM-dd HH:mm:ss.fff"))

        Dim subTab = dataDictionary.SubTabs.Values.FirstOrDefault(Function(st) st.TabId = subTabID)
        Dim subTabName As String = If(subTab IsNot Nothing, subTab.TabName, "Unknown SubTab")

        ' Build base event comment (unchanged)
        Dim baseComment As String = $"3,1000,{feedbackType},{eventTypeID},{subTabName} Event - {descriptionText},{annotationIndex},{buttonIndex},{sequenceNumber},{preciseElapsedMs},{sequenceNumber},{preciseElapsedMs},{sequenceNumber},{preciseSecondsFinal},0,{annoAudioFilename}"

        '' Append LiDAR frame numbers if available
        'If Not String.IsNullOrEmpty(lidarFrameNumbers) Then
        '    Return $"{baseComment},{lidarFrameNumbers}"
        'Else
        '    ' Append empty placeholders if LiDAR not active
        '    Dim emptyFrames As String = String.Join(",", Enumerable.Repeat("-1", LidarDevices.Count))
        '    Return $"{baseComment},{emptyFrames}"
        'End If
        Return baseComment
    End Function

    ''' <summary>
    ''' Captures the current frame number from all active LiDAR devices.
    ''' Returns a comma-separated string of frame numbers (e.g., "1247,3891,-1")
    ''' Uses -1 for devices that are not capturing or have no data.
    ''' </summary>
    Private Function GetLidarFrameSnapshot() As String
        Try
            If Not LidarCaptureStarted OrElse LidarDevices Is Nothing OrElse LidarDevices.Count = 0 Then
                Return String.Empty
            End If

            Dim frameLabels As New List(Of String)

            For i As Integer = 0 To LidarDevices.Count - 1
                Dim device = LidarDevices(i)
                Dim frameNum As String = If(device.IsCapturing, device.CurrentFrameNumber.ToString(), "-1")
                frameLabels.Add($"{frameNum}")  ' ← Returns: "1247,3891"
            Next

            Return String.Join(",", frameLabels)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"GetLidarFrameSnapshot: {ex.Message}")
            ' Return placeholder values on error
            Return String.Join(",", Enumerable.Range(1, LidarDevices.Count).Select(Function(i) $"-1"))
        End Try
    End Function

    ' Find the index of annotation based on Sub-tab Name
    Private Function FindAnnotationIndex(ByVal parentText As String) As Integer
        Dim dataDictionary = DataDictionarySingleton.GetInstance() ' Singleton reference
        'HandleUserMessageLogging("GMRC", $"FindAnnotationIndex: Looking for parentText '{parentText}'")

        ' Validate input
        If String.IsNullOrEmpty(parentText) Then
            HandleUserMessageLogging("GMRC", "FindAnnotationIndex: parentText is null or empty")
            Return -1
        End If

        ' Validate AnnotationValueRecords exists
        If dataDictionary.AnnotationValueRecords Is Nothing OrElse dataDictionary.AnnotationValueRecords.Count = 0 Then
            HandleUserMessageLogging("GMRC", "FindAnnotationIndex: AnnotationValueRecords is null or empty")
            Return -1
        End If

        ' Iterate over AnnotationValueRecords to find a matching SubTabName
        For i As Integer = 0 To dataDictionary.AnnotationValueRecords.Count - 1
            Dim subTabName = dataDictionary.AnnotationValueRecords(i).SubTabName?.Trim() ' Use SubTabName for matching
            'HandleUserMessageLogging("GMRC", $"FindAnnotationIndex: Comparing '{parentText}' with SubTabName[{i}] '{subTabName}'")

            ' Check if parentText matches SubTabName exactly
            If String.Equals(parentText, subTabName, StringComparison.OrdinalIgnoreCase) Then
                'HandleUserMessageLogging("GMRC", $"FindAnnotationIndex: Match found at index {i}")
                Return i
            End If
        Next

        HandleUserMessageLogging("GMRC", $"FindAnnotationIndex: No match found for parentText: '{parentText}'")
        Return -1 ' Return -1 if no match is found
    End Function

    ' Find description text based on Event Button text and annotation index
    Private Function FindDescriptionText(ByVal buttonText As String) As String
        Dim dataDictionary = DataDictionarySingleton.GetInstance() ' Ensure we have a reference to the singleton

        ' Check if SubTabs has been initialized
        If dataDictionary.SubTabs Is Nothing Then
            'Console.WriteLine("Error: SubTabs is not initialized in the data dictionary.")
            Return ""
        End If

        ' Iterate over each event button in all sub-tabs
        For Each eventButton In dataDictionary.SubTabs.Values.SelectMany(Function(subTab) subTab.EventButtons)
            If eventButton.ButtonName = buttonText Then
                Return buttonText ' Use the button text as the description
            End If
        Next

        ' If no match is found, return an empty string
        Return ""
    End Function

    Private Sub DisplayAnnotationListBox(ByVal description As String, ByVal z As Integer, ByVal i As Integer)
        Dim dataDictionary = DataDictionarySingleton.GetInstance() ' Singleton reference
        Dim subTab = dataDictionary.SubTabs.Values.ElementAtOrDefault(z)
        ' Check if SubTab and EventButton exist at the specified indexes
        If subTab IsNot Nothing AndAlso i >= 0 AndAlso i < subTab.EventButtons.Count Then
            Dim eventButton = subTab.EventButtons(i)
            ' Filter only non-empty sub-categories
            Dim populatedSubCategories = eventButton.SubCategories.Where(Function(sc) Not String.IsNullOrEmpty(sc)).ToList()
            ' Only display ListBox if there are populated sub-categories
            If populatedSubCategories.Count > 0 Then
                ' Clear and populate the ListBox with sub-categories
                OnVehicleScreen.ListBox4.Items.Clear()
                For Each subCategory In populatedSubCategories
                    OnVehicleScreen.ListBox4.Items.Add(subCategory)
                Next
                ' Add a "Cancel" item to allow the user to close the list
                OnVehicleScreen.ListBox4.Items.Add("Cancel")

                ' Set the Tag property to the parent tab name (sub-tab name) for later reference
                OnVehicleScreen.ListBox4.Tag = subTab.TabName
                ' Save the button text for later use
                SaveAnnoButtonText = description

                ' Make GroupBox5 and ListBox4 visible and bring GroupBox5 to the front
                OnVehicleScreen.GroupBox5.Visible = True
                OnVehicleScreen.ListBox4.Visible = True
                OnVehicleScreen.GroupBox5.BringToFront()

                ' Dynamically adjust the height of the ListBox based on the number of items
                Dim itemHeight As Integer = OnVehicleScreen.ListBox4.ItemHeight
                Dim totalItems As Integer = OnVehicleScreen.ListBox4.Items.Count
                totalItems += 1 ' Add an extra item for the "Cancel" button
                Dim maxHeight As Integer = 500  ' Maximum allowed height
                Dim calculatedHeight As Integer = Math.Min(itemHeight * totalItems, maxHeight)
                OnVehicleScreen.ListBox4.Height = calculatedHeight

                ' Optionally, adjust the width of the ListBox to fit the longest item
                Dim maxWidth As Integer = 300  ' Maximum allowed width
                Dim longestItemWidth As Integer = OnVehicleScreen.ListBox4.Items.Cast(Of String)() _
                                               .Max(Function(sc) TextRenderer.MeasureText(sc, OnVehicleScreen.ListBox4.Font).Width)
                OnVehicleScreen.ListBox4.Width = Math.Min(longestItemWidth + SystemInformation.VerticalScrollBarWidth, maxWidth)

                ' After setting ListBox4.Height dynamically...
                Dim extraVerticalSpace As Integer = 40  ' Adjust for GroupBox header, padding, etc.
                OnVehicleScreen.GroupBox5.Height = OnVehicleScreen.ListBox4.Height + extraVerticalSpace

                ' Set focus to the ListBox
                OnVehicleScreen.ListBox4.Focus()
            Else
                ' Optionally log or handle the case where there are no sub-categories
                'Console.WriteLine($"No populated sub-categories defined for button: {eventButton.ButtonName}")
            End If
        Else
            ' Optionally log or handle the case where the specified SubTab or EventButton does not exist.
            'Console.WriteLine($"No valid SubTab or EventButton found at z-index: {z} and i-index: {i}.")
        End If
    End Sub


    Private Sub UpdateCustomAnnotationText(ByVal index As Integer, ByVal newText As String)
        Dim dataDictionary = DataDictionarySingleton.GetInstance()
        Dim found As Boolean = False

        ' Check if the new text already exists in SaveCustomAnnotationText to avoid duplicates
        Dim currentRecord = dataDictionary.AnnotationValueRecords(index)
        If currentRecord.SaveCustomAnnotationText IsNot Nothing Then
            For n As Integer = 0 To UBound(currentRecord.SaveCustomAnnotationText)
                If currentRecord.SaveCustomAnnotationText(n) = newText Then
                    found = True
                    Exit For
                End If
            Next
        End If

        ' If the new text is not found, add it to SaveCustomAnnotationText
        If Not found Then
            Dim originalArray = currentRecord.SaveCustomAnnotationText
            Dim newSize As Integer = If(originalArray Is Nothing, 1, originalArray.Length + 1)

            ' Create a new array with the new size and copy the old data
            Dim tempArray(newSize - 1) As String
            If originalArray IsNot Nothing Then
                Array.Copy(originalArray, tempArray, originalArray.Length)
            End If

            ' Add the new text
            tempArray(newSize - 1) = newText

            ' Update the record with the new array
            currentRecord.SaveCustomAnnotationText = tempArray
            dataDictionary.AnnotationValueRecords(index) = currentRecord ' Re-assign the modified record back to the list
        End If
    End Sub

    Public Sub ReadDebugFile()
        ' Called from Initialize: Reads debug.txt file, extracts configuration information and puts it into variables.
        Dim ConfigFileName As String = My.Application.Info.DirectoryPath & "\\Debug.txt"
        Dim TextLine As String
        Dim Ctr As Integer
        MessageLogLevel = 0
        DebugMessages = 0
        CounterValue = 3
        If File.Exists(ConfigFileName) Then
            Ctr = 0
            ' Use FileStream and StreamReader to read the file
            Using fs As New FileStream(ConfigFileName, FileMode.Open, FileAccess.Read)
                Using sr As New StreamReader(fs)
                    ' Go line by line through debug.txt file to pick out data from pre-defined lines in text file
                    While Not sr.EndOfStream
                        TextLine = sr.ReadLine()
                        Select Case Ctr
                            Case 0
                                CounterValue = Val(TextLine.Substring(TextLine.IndexOf(Chr(9)) + 1))
                            Case 1
                                DebugMessages = Val(TextLine.Substring(TextLine.IndexOf(Chr(9)) + 1))
                            Case 2
                                MessageLogLevel = Val(TextLine.Substring(TextLine.IndexOf(Chr(9)) + 1))
                        End Select
                        Ctr += 1
                    End While
                End Using
            End Using
        End If
    End Sub

    Public Sub WriteLoginIDListFile()
        ' Called from SetupDataLogging and AddUserToList:
        ' Saves the LoginID List to the UserIDList.txt file so if any new IDs have been added, they will be retained for subsequent
        ' use.

        ' Check if LoginIDNameAndFreqAL is null or empty before proceeding
        If LoginIDNameAndFreqAL Is Nothing Then
            HandleUserMessageLogging("GMRC", "WriteLoginIDListFile: LoginIDNameAndFreqAL is Nothing - skipping file write")
            Return
        End If

        If LoginIDNameAndFreqAL.Count = 0 Then
            HandleUserMessageLogging("GMRC", "WriteLoginIDListFile: LoginIDNameAndFreqAL is empty - skipping file write")
            Return
        End If

        Dim fileName As String = Path.Combine(My.Application.Info.DirectoryPath, "UserIDList.txt")

        If File.Exists(fileName) Then
            If Not FileInUse(fileName) Then
                Try
                    ' Use FileStream and StreamWriter to write to the file
                    Using fs As New FileStream(fileName, FileMode.Create, FileAccess.Write)
                        Using writer As New StreamWriter(fs)
                            LoginIDNameAndFreqAL.Sort()
                            LoginIDNameAndFreqAL.Reverse()
                            For Each item In LoginIDNameAndFreqAL
                                writer.WriteLine(item.ToString())
                            Next
                        End Using
                    End Using
                    HandleUserMessageLogging("GMRC", $"WriteLoginIDListFile: Successfully wrote {LoginIDNameAndFreqAL.Count} login IDs to file")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"WriteLoginIDListFile: Error writing to file - {ex.Message}")
                End Try
            Else
                HandleUserMessageLogging("GMRC", "WriteLoginIDListFile: File is in use - skipping write")
            End If
        Else
            HandleUserMessageLogging("GMRC", "WriteLoginIDListFile: UserIDList.txt file does not exist - skipping write")
        End If
    End Sub

    Public Sub CopyINCADatabase()

        'Called when Exiting CLEVIR and Closing INCA from the ExitAppForm.  Copies the existing database to a save file prior to exiting
        'and closing INCA.  We do this because we have had instances of corrupted databases in the past that caused issues in the vehicles.

        'The assumption here is that if CLEVIR can exit cleanly, the INCA database must be intact...

        Try

            If Not Debugger.IsAttached Then

                HandleUserMessageLogging("GMRC", "Backing up INCA Database, please be patient...",,, FlashMsgOn)
                DeleteDirectory(INCADatabase & "_SAVE")
                HandleUserMessageLogging("GMRC", "CopyINCADatabase: Copying INCA Database Directory...")
                FileIO.FileSystem.CopyDirectory(INCADatabase, INCADatabase & "_SAVE", True)
                HandleUserMessageLogging("GMRC", "CopyINCADatabase: INCA Database Directory copied...")

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CopyINCADatabase: " & ex.Message)

        Finally
            UserStatusInfo.Hide()
        End Try


    End Sub

    ''' <summary>
    ''' Checks if a Windows process with the given name is running.
    ''' </summary>
    Public Function IsProcessRunning(ByVal procName As String) As Boolean
        Try
            ' Checks if any process (other than the current one) matches procName
            Dim processes = Process.GetProcessesByName(procName)
            Dim isRunning = processes.Any(Function(p) p.Id <> Process.GetCurrentProcess().Id)

            ' If the process is not running and the process name is "INCA", reset the device raster signals
            If Not isRunning AndAlso procName.Equals("INCA", StringComparison.OrdinalIgnoreCase) Then
                myDeviceRasterSignals = Nothing
            End If

            Return isRunning

        Catch ex As Exception
            ' Log any exceptions that occur
            HandleUserMessageLogging("GMRC", $"Error in IsProcessRunning: {ex.Message}")
            Return False
        End Try
    End Function

    Public Sub CopyATT_TCPFilesToFinalPath()

        'Called from GmResidentClient.exitapp - copies ATT_TCP data to the vehicle data directory if any data exists...

        'C:\ATTJProtection\logs\background

        Dim DirectoryName As String

        DirectoryName = "C:\ATTJProtection\logs\background"

        If Directory.Exists(DirectoryName) Then
            ZipTheDirectory(DirectoryName, "ATT_TCP", BaseDataCollectionPath)
            Directory.Delete(DirectoryName, True)
            Directory.CreateDirectory(DirectoryName)
        End If

    End Sub

    Public Sub LaunchATT_TCP()

        'Called from the initialize routine.  If the ATT_TCP data gathering program exists, it is launched
        'by this routine.  To collect the data, the user must interact directly with the ATT_TCP application.
        'If data is collected, CLEVIR will zip the data folder and copy it to the vehicle data upload directory
        'upon exiting CLEVIR.

        Dim ExecutableFile As String = "C:\ATTJProtection\ATT_TCP_GEN11.exe"
        Dim p As New ProcessStartInfo


        If File.Exists("C:\ATTJProtection\ATT_TCP_GEN11.exe") Then

            p.WindowStyle = ProcessWindowStyle.Minimized
            p.FileName = ExecutableFile

            Process.Start(p)

        End If

    End Sub

    Public Sub CopyToCOMMLog(ByVal inputstr As String)
        ' Called from RegisterSignals and other places: Copies text passed in to a log file, also adds a timestamp.
        ' Also writes invalid signals to the InvalidSignalsLog.csv file.
        ' REFACTORED: Eliminated circular logging references to prevent file lock issues.

        Dim logFilename As String = Path.Combine(My.Application.Info.DirectoryPath, "GM_INCA_Comm.log")
        Dim invalidSignalLog As String = Path.Combine(My.Application.Info.DirectoryPath, "InvalidSignalsLog.csv")
        Dim isNumber As Boolean = IsNumeric(inputstr)
        Dim isInvalidSignal As Boolean = InStr(inputstr, "- INVALID -") > 0 AndAlso InStr(inputstr, "VIDEO_CAMERA_TIMECODE") = 0

        ' Format input string with timestamp
        inputstr = If(isNumber, Format(DateTime.Now, "HH:mm:ss - ") & inputstr, Format(DateTime.Now, "MM/dd/yyyy HH:mm:ss - ") & inputstr)

        Try
            ' Add to StatusString (global variable monitored in InitializationMonitor thread)
            UpdateStatusString(inputstr)

            ' Handle non-numeric input strings (write to Comm Log)
            If Not isNumber Then
                ' Pass suppressErrorLogging=True to prevent circular calls back to CopyToLog
                WriteToFileSafe(logFilename, inputstr, suppressErrorLogging:=True)
            End If

            ' Handle invalid signals
            If isInvalidSignal AndAlso Not ProcessingInvalidSignalsLog Then
                WriteToFileSafe(invalidSignalLog, Mid(inputstr, InStr(inputstr, "- INVALID -") + 12), suppressErrorLogging:=True)
            End If

        Catch ex As Exception
            ' DO NOT call CopyToLog here to break circular reference
            ' Instead, write to Debug output or suppress the error
            Debug.WriteLine($"CopyToCOMMLog Error: {ex.Message}")
        End Try
    End Sub

    ' Helper function to update the StatusString array
    Private Sub UpdateStatusString(ByVal inputstr As String)
        Try
            If StatusString Is Nothing Then
                ReDim StatusString(0)
            Else
                ReDim Preserve StatusString(UBound(StatusString) + 1)
            End If
            StatusString(UBound(StatusString)) = inputstr
        Catch ex As Exception
            ' Silently handle array resize errors to prevent cascading issues
            Debug.WriteLine($"UpdateStatusString Error: {ex.Message}")
        End Try
    End Sub

    ' REFACTORED: New method with retry logic, proper file sharing, and circular reference prevention
    Private Sub WriteToFileSafe(ByVal filePath As String, ByVal text As String, Optional suppressErrorLogging As Boolean = False)
        Const maxRetries As Integer = 3
        Dim currentRetry As Integer = 0

        While currentRetry < maxRetries
            Try
                ' Determine file mode based on existence
                Dim fileMode As FileMode = If(File.Exists(filePath), FileMode.Append, FileMode.Create)

                ' Use FileShare.Read to allow other processes to read while we write
                ' This is critical to prevent file locking issues
                Using fs As New FileStream(filePath, fileMode, FileAccess.Write, FileShare.Read)
                    Using writer As New StreamWriter(fs)
                        writer.WriteLine(text)
                        writer.Flush() ' Ensure data is written immediately
                    End Using
                End Using

                ' Success - exit the retry loop
                Return

            Catch ex As IOException When currentRetry < maxRetries - 1
                ' File is locked or in use - wait and retry with exponential backoff
                currentRetry += 1
                Dim delayMs As Integer = 50 * CInt(Math.Pow(2, currentRetry - 1)) ' 50ms, 100ms, 200ms
                Threading.Thread.Sleep(delayMs)

            Catch ex As Exception
                ' Non-retryable exception or max retries exceeded
                If Not suppressErrorLogging Then
                    ' Only log to Debug output to avoid circular references
                    Debug.WriteLine($"WriteToFileSafe Error after {currentRetry} retries: {ex.Message} - File: {filePath}")
                End If

                ' Exit the retry loop on non-IO exceptions
                If TypeOf ex IsNot IOException Then
                    Exit While
                End If

                currentRetry += 1
            End Try
        End While

        ' If we get here, all retries were exhausted
        If Not suppressErrorLogging Then
            Debug.WriteLine($"WriteToFileSafe: Failed to write to {Path.GetFileName(filePath)} after {maxRetries} attempts")
        End If
    End Sub
    ' Helper function to write a string to a specified file
    ' DEPRECATED: Legacy WriteToFile method - kept for backward compatibility but redirects to WriteToFileSafe
    ' This can be removed once all calling code is updated to use WriteToFileSafe
    Private Sub WriteToFile(ByVal filePath As String, ByVal text As String)
        ' Redirect to the new safe implementation
        WriteToFileSafe(filePath, text, suppressErrorLogging:=False)
    End Sub

    Public Function ImportFileIntoINCA(ByVal Filename As String, ByVal overwrite As Boolean, ByVal discardimpl As Boolean) As Boolean

        'Called from various places, imports a .exp file into INCA...

        HandleUserMessageLogging("GMRC", "ImportFileIntoINCA: Importing " & Filename & " into INCA")
        ImportFileIntoINCA = MyIncaInterface.ImportFileIntoINCA(Filename, overwrite, discardimpl)

    End Function

    ' Add to module initialization
    Private eventProcessingTimer As System.Timers.Timer
    Private shutdownEventProcessingInProgress As Boolean = False

    Public Sub InitializeEventProcessing()
        Try
            ' Ensure we don't create multiple timers
            If eventProcessingTimer IsNot Nothing Then
                eventProcessingTimer.Stop()
                eventProcessingTimer.Dispose()
            End If

            ' Create and configure the timer for event processing
            eventProcessingTimer = New System.Timers.Timer(250) With {
                .AutoReset = True
            } ' Process every 250ms
            AddHandler eventProcessingTimer.Elapsed, AddressOf ProcessPendingEvents
            eventProcessingTimer.Start()

            ' Initialize recordingAllowed flag
            recordingAllowed = True

            HandleUserMessageLogging("GMRC", "Event processing and MF4 transition monitoring initialized")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"InitializeEventProcessing failed: {ex.Message}")
        End Try
    End Sub

    Public Sub StartEventProcessingLoopAsync()
        ' Stop any existing loop before starting a new one.
        If eventProcessingTask IsNot Nothing AndAlso Not eventProcessingTask.IsCompleted Then
            ShutdownEventProcessing()
        End If

        eventProcessingCts = New CancellationTokenSource()
        recordingAllowed = True
        eventProcessingTask = Task.Run(Async Function()
                                           Dim token = eventProcessingCts.Token
                                           While Not token.IsCancellationRequested
                                               Dim errorOccurred As Boolean = False
                                               Try
                                                   ' Perform the monitoring and processing tasks.
                                                   MonitorMf4Transition()
                                                   Await ProcessEventBuffer()

                                               Catch ex As TaskCanceledException
                                                   ' This is expected when the loop is stopped.
                                                   Exit While
                                               Catch ex As Exception
                                                   HandleUserMessageLogging("GMRC", $"EventProcessingLoop Error: {ex.Message}")
                                                   errorOccurred = True
                                               End Try

                                               Try
                                                   If errorOccurred Then
                                                       ' Avoid a tight loop on repeated errors.
                                                       Await Task.Delay(1000, token).ConfigureAwait(False)
                                                   Else
                                                       ' Wait for the next normal interval.
                                                       Await Task.Delay(250, token).ConfigureAwait(False)
                                                   End If
                                               Catch ex As TaskCanceledException
                                                   ' Exit loop if cancellation is requested during the delay.
                                                   Exit While
                                               End Try
                                           End While
                                           HandleUserMessageLogging("GMRC", "Event processing loop has stopped.")
                                       End Function, eventProcessingCts.Token)
        HandleUserMessageLogging("GMRC", "Event processing loop started.")
    End Sub

    Public Sub ShutdownEventProcessing()
        Try
            ' Prevent recursive calls
            If shutdownEventProcessingInProgress Then
                HandleUserMessageLogging("GMRC", "ShutdownEventProcessing already in progress - skipping")
                Return
            End If
            shutdownEventProcessingInProgress = True

            ' Stop the timer first to prevent new events
            If eventProcessingTimer IsNot Nothing Then
                eventProcessingTimer.Stop()
                RemoveHandler eventProcessingTimer.Elapsed, AddressOf ProcessPendingEvents
                eventProcessingTimer.Dispose()
                eventProcessingTimer = Nothing
            End If

            ' Set flag to prevent new events from being buffered
            recordingAllowed = False

            ' Process any remaining buffered events before shutdown (but don't retry)
            ProcessEventBufferFinal()

            ' ✅ PERFORMANCE: Invalidate cache when recording stops
            InvalidateRecordingInfoCache()

            HandleUserMessageLogging("GMRC", "Event processing monitoring shutdown complete (cache invalidated)")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ShutdownEventProcessing error: {ex.Message}")
        Finally
            ' Reset the flag regardless of success/failure
            shutdownEventProcessingInProgress = False
        End Try
    End Sub


    Private Sub ProcessEventBufferFinal()
        ' Final processing without retries to avoid circular calls during shutdown
        Dim eventsToProcess As List(Of PendingEvent)

        SyncLock eventBufferLock
            eventsToProcess = eventBuffer.ToList()
            eventBuffer.Clear()
        End SyncLock

        ' Process events but don't retry on failure during shutdown
        For Each pendingEvent In eventsToProcess
            Try
                ' Synchronous final write attempts only
                Using fs As New FileStream(ANNOFileName, FileMode.Append, FileAccess.Write, FileShare.Read)
                    Using writer As New StreamWriter(fs)
                        writer.WriteLine($"{pendingEvent.eventComment},{pendingEvent.TimeCriticalInfo}")
                        writer.Flush()
                    End Using
                End Using

                ' Also try aggregate file
                Dim aggregateFileName As String = Path.Combine(My.Application.Info.DirectoryPath, $"{VehicleNumber}_AggregateAnnotations.csv")
                Using fs As New FileStream(aggregateFileName, FileMode.Append, FileAccess.Write, FileShare.Read)
                    Using writer As New StreamWriter(fs)
                        writer.WriteLine($"{pendingEvent.eventComment},{pendingEvent.TimeCriticalInfo}")
                        writer.Flush()
                    End Using
                End Using

            Catch ex As Exception
                ' Log but don't retry during shutdown
                HandleUserMessageLogging("GMRC", $"Failed to write final event during shutdown: {ex.Message}")
            End Try
        Next
    End Sub

End Module

