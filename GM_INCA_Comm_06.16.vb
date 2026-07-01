Option Strict Off

Imports System
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Runtime.Remoting.Lifetime
Imports System.Threading
Imports System.Threading.Tasks
Imports de.etas.cebra.toolAPI.Common
'ETAS INCA API
Imports de.etas.cebra.toolAPI.Inca
Imports RCI2dotNet

'The GM_INCA_Comm class contains the IGM_INCA_Comm interface, the GM_INCA_CommClass definition and 
'functions and routines which support the GM_INCA_Comm functionality.  Uses the INCA API to communicate
'with INCA...
Public Interface IGM_INCA_Comm
    Function GetActualRecordingTimeMs() As Integer
    Function GetRemainingRecordingTimeMs() As Integer
    Function StopMeasurementAndSaveToFile(fileName As String, fileFormat As String) As Boolean
    Function AddUserToList(ByVal UserName As String) As Boolean
    Function BrowseMeasureElementsInDevice(ByVal searchstr As String, ByVal devicename As String) As String()
    Function ClearINCAMonitor() As Boolean
    Function ConnectToInca() As String
    Function GetAllMeasureElementNamesInDevice(ByVal devicename As String) As String()
    Function GetAllActiveMeasureLabels() As String()
    Function GetAvailableDevices() As INCADeviceStatus()
    Function GetAvailableExperimentNames() As String()
    Function GetDataArray(ByVal DeviceName As String, ByVal RasterName As String, ByVal NumValidVars As Integer, ByRef NoOfRecords As Integer) As INCAData
    Function GetDataArrayForSignal(ByVal signalname As String) As INCAData
    Function GetDataForSignalList(ByVal signalist() As String) As INCAMeasureValue()
    Function GetDataHistoryForDeviceRasterPair(ByVal devicename As String, ByVal rastername As String, ByVal numberofrecords As UInteger) As INCAData
    Function GetDataHistoryForSignal(ByVal signalname As String, ByVal numberofrecords As UInteger) As INCAData
    Function GetDefaultRasterForMeasureElementInDevice(ByVal devicename As String, ByVal measname As String) As String
    Function GetDeviceAcquisitionRates(ByVal Device As String) As String()
    Function GetDeviceRasterPairData() As Double(,)
    Function GetDeviceRasterPairForSignal(ByVal signalname As String) As IGM_INCA_Comm.DeviceRasterSignalStatus
    Function GetGM_INCA_CommStatus() As String()
    Function GetINCAMeasureValue(ByVal devicename As String, ByVal rastername As String, ByVal signalname As String) As INCAMeasureValue
    Function GetINCAPollingRate() As Double
    Function GetCurrentVersion() As String
    Function GetLastINCAError() As String
    Function GetLastRecordingFileName() As String
    Function GetMeasurementStatus() As String
    Function GetRecordingFileFormat() As String
    Function GetRecordingFileName() As String
    Function GetRecordingFileNameTemplate() As String
    Function GetRecordingPathName() As String
    Function GetRecordingState() As Boolean
    Function GetReferenceDataSetDataBasePaths() As String()
    Function GetRegisteredSignals() As IGM_INCA_Comm.DeviceRasterSignalStatus()
    Function GetSignalData() As Double()
    Function GetSignalDataWithTime() As TransferDataWithTime()
    Function GetWorkingDataSetDataBasePaths() As String()
    Function GetAvailableWorkspaces() As String()
    Function HandleWorkspace(ByVal EtasUserName As String, ByVal RegisterIntoNewBlankExp As Boolean) As String
    Function ImportFileIntoINCA(ByVal Filename As String, Optional ByVal overwrite As Boolean = True, Optional ByVal discardimpl As Boolean = False) As Boolean
    Function InitializeHardware() As Boolean
    Function InitINCA(ByVal database As String, ByVal workspace As String, ByVal experiment As String, ByVal EtasUserName As String, ByVal ForceInit As Boolean, ByRef ErrorMsg As String, ByVal RegisterIntoNewBlankExp As Boolean) As IGM_INCA_Comm.INIT_STATUS
    Function IsTargetOnWorkingPage() As String
    Function RegisterSignals(
                             ByVal DeviceRasterSignals() As DeviceRasterSignalStatus,
                             Optional ByVal progressForm As SignalRegistrationProgressForm = Nothing
                             ) As DeviceRasterSignalStatus()
    Function SaveCalSnapShot(ByVal Page As String) As String
    Function SaveExperiment() As Boolean
    Function SetRecordingFileFormat(ByVal FileFormat As String) As Boolean
    Function SetRecordingFileName(ByVal FileName As String) As Boolean
    Function SetRecordingPathName(ByVal PathName As String) As Boolean
    Function SetupDataLogging(ByVal mySelectedTestName As String, ByVal LoginIDStr As String) As Task(Of String)
    Function StartMeasurement() As Boolean
    Function StopMeasurement() As Boolean
    Function StartRecording() As Boolean
    Function StopRecording(ByVal PathFileName As String, ByVal RecordingFileFormat As String) As Boolean
    Function SwitchToReferencePage() As Boolean
    Function SwitchToWorkingPage() As Boolean
    Function UnlockExperiment() As Boolean
    Function WriteEventComment(ByVal CommentString As String, ByVal aFlag As Boolean) As Boolean
    Function WriteMonitorLogFileToPathUsingFileName(ByVal pathname As String, ByVal filename As String) As Boolean
    Function SetLastRecordingFileName(ByVal fileName As String) As Boolean

    Sub CloseExperiment()
    Sub CloseINCA()
    Sub RCI2_CleanUp()
    Sub ResetRecords()
    Sub StartDataCollection(ByVal sleeptime As Integer)
    Sub StopDataCollection()
    Sub SetProjectDatabaseInfo()
    Sub ReadUserIDList()

    Enum INIT_STATUS
        ALREADY_INITIALIZED = 0
        REINITIALIZED = 1
        INIT_SUCCESSFUL = 2
        INIT_UNSUCCESSFUL = 3
    End Enum

    <Serializable()>
    Structure DeviceRasterSignalStatus
        Dim DeviceName As String
        Dim RasterName As String
        Dim SignalName As String
        Dim Status As String
        Dim DeviceRasterPairNum As Integer
        Dim DeviceRasterPairVarNum As Integer
        Dim Value As Double
        Dim ForceRegister As Boolean
    End Structure

    <Serializable()>
    Structure DeviceRasterPairs
        Dim DeviceName As String
        Dim RasterName As String
        Dim NumValidVars As Integer
    End Structure

    <Serializable()>
    Structure INCAData
        Public myData() As Double
        Public myTime() As Double
        Public myStatus As Boolean
    End Structure

    <Serializable()>
    Structure TransferData
        Public SignalData() As Double
    End Structure

    <Serializable()>
    Structure TransferDataWithTime
        Public SignalData As Double
        Public TimeStamp As Double
    End Structure

    <Serializable()>
    Structure INCAMeasureValue

        Public myValue As Double
        Public myStatus As Boolean

    End Structure

    <Serializable()>
    Structure INCADeviceStatus

        Public myName As String
        Public myStatus As Boolean
        Public myDeviceType As String

    End Structure
End Interface

''' <summary>
''' ✅ NEW: Persistent registration cache to skip re-registration on subsequent startups
''' Stores the last successfully registered signals in a JSON file alongside the signal list CSV.
''' </summary>
Public Class SignalRegistrationCache
    Private Shared ReadOnly CacheLock As New Object()

    ''' <summary>
    ''' Gets the cache file path based on the signal list CSV path.
    ''' e.g., "C:\CLEVIR\SignalLists\MySignals.csv" → "C:\CLEVIR\SignalLists\MySignals.registration_cache.json"
    ''' </summary>
    Public Shared Function GetCacheFilePath(signalListPath As String) As String
        If String.IsNullOrEmpty(signalListPath) Then Return Nothing
        Return Path.ChangeExtension(signalListPath, ".registration_cache.json")
    End Function

    ''' <summary>
    ''' Checks if cached registration is still valid by comparing:
    ''' 1. Cache file exists
    ''' 2. Signal list CSV hasn't been modified since cache was created
    ''' 3. INCA experiment name matches
    ''' </summary>
    Public Shared Function IsCacheValid(signalListPath As String, experimentName As String) As Boolean
        Try
            Dim cachePath As String = GetCacheFilePath(signalListPath)
            If String.IsNullOrEmpty(cachePath) OrElse Not File.Exists(cachePath) Then
                Return False
            End If

            ' Check if signal list is newer than cache
            If File.Exists(signalListPath) Then
                Dim csvModified As DateTime = File.GetLastWriteTimeUtc(signalListPath)
                Dim cacheModified As DateTime = File.GetLastWriteTimeUtc(cachePath)
                If csvModified > cacheModified Then
                    HandleUserMessageLogging("COMM", "SignalRegistrationCache: CSV modified since last cache - invalidating")
                    Return False
                End If
            End If

            ' Read and validate cache content
            Dim cacheContent As String = File.ReadAllText(cachePath)
            If String.IsNullOrEmpty(cacheContent) Then Return False

            ' Parse experiment name from cache (simple JSON parsing)
            If Not cacheContent.Contains($"""ExperimentName"":""{experimentName}""") Then
                HandleUserMessageLogging("COMM", "SignalRegistrationCache: Experiment name mismatch - invalidating")
                Return False
            End If

            Return True

        Catch ex As Exception
            HandleUserMessageLogging("COMM", $"SignalRegistrationCache.IsCacheValid: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Loads cached signal keys (DeviceName|RasterName|SignalName) from the cache file.
    ''' Returns Nothing if cache is invalid or empty.
    ''' </summary>
    Public Shared Function LoadCachedSignalKeys(signalListPath As String) As HashSet(Of String)
        Try
            SyncLock CacheLock
                Dim cachePath As String = GetCacheFilePath(signalListPath)
                If String.IsNullOrEmpty(cachePath) OrElse Not File.Exists(cachePath) Then
                    Return Nothing
                End If

                Dim cacheContent As String = File.ReadAllText(cachePath)
                Dim result As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                ' ✅ FIXED: Use line-by-line parsing to handle signal names with brackets like [x]
                ' Format: {"ExperimentName":"...", "SignalCount":123, "SignalKeys":["key1","key2",...]}
                Dim inSignalKeys As Boolean = False
                Dim lines() As String = cacheContent.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)

                For Each line In lines
                    Dim trimmedLine As String = line.Trim()

                    ' Detect start of SignalKeys array
                    If trimmedLine.Contains("""SignalKeys"":[") Then
                        inSignalKeys = True
                        Continue For
                    End If

                    ' Detect end of SignalKeys array
                    If inSignalKeys AndAlso trimmedLine = "]" Then
                        Exit For
                    End If

                    ' Parse signal key lines (format: "DeviceName|RasterName|SignalName",)
                    If inSignalKeys Then
                        ' Remove leading/trailing quotes, commas, and whitespace
                        Dim cleanKey As String = trimmedLine.TrimEnd(","c).Trim().Trim(""""c)
                        If Not String.IsNullOrEmpty(cleanKey) AndAlso cleanKey.Contains("|") Then
                            result.Add(cleanKey)
                        End If
                    End If
                Next

                HandleUserMessageLogging("COMM", $"SignalRegistrationCache: Loaded {result.Count} cached signal keys")
                Return result
            End SyncLock

        Catch ex As Exception
            HandleUserMessageLogging("COMM", $"SignalRegistrationCache.LoadCachedSignalKeys: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Saves the successfully registered signals to the cache file.
    ''' </summary>
    Public Shared Sub SaveCache(signalListPath As String, experimentName As String, signals() As IGM_INCA_Comm.DeviceRasterSignalStatus)
        Try
            SyncLock CacheLock
                Dim cachePath As String = GetCacheFilePath(signalListPath)
                If String.IsNullOrEmpty(cachePath) Then Return

                ' Build simple JSON manually (no external dependencies)
                Dim sb As New Text.StringBuilder()
                sb.AppendLine("{")
                sb.AppendLine($"  ""ExperimentName"":""{experimentName}"",")
                sb.AppendLine($"  ""CreatedUtc"":""{DateTime.UtcNow:o}"",")
                sb.AppendLine($"  ""SignalCount"":{signals.Length},")
                sb.AppendLine("  ""SignalKeys"":[")

                Dim validSignals = signals.Where(Function(s) s.Status = "Valid").ToArray()
                For i As Integer = 0 To validSignals.Length - 1
                    Dim signal = validSignals(i)
                    Dim key As String = $"{signal.DeviceName}|{signal.RasterName}|{signal.SignalName}"
                    Dim comma As String = If(i < validSignals.Length - 1, ",", "")
                    sb.AppendLine($"    ""{key}""{comma}")
                Next

                sb.AppendLine("  ]")
                sb.AppendLine("}")

                File.WriteAllText(cachePath, sb.ToString())
                HandleUserMessageLogging("COMM", $"SignalRegistrationCache: Saved {validSignals.Length} signals to {Path.GetFileName(cachePath)}")
            End SyncLock

        Catch ex As Exception
            HandleUserMessageLogging("COMM", $"SignalRegistrationCache.SaveCache: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Invalidates (deletes) the cache file.
    ''' </summary>
    Public Shared Sub InvalidateCache(signalListPath As String)
        Try
            Dim cachePath As String = GetCacheFilePath(signalListPath)
            If Not String.IsNullOrEmpty(cachePath) AndAlso File.Exists(cachePath) Then
                File.Delete(cachePath)
                HandleUserMessageLogging("COMM", "SignalRegistrationCache: Cache invalidated")
            End If
        Catch ex As Exception
            HandleUserMessageLogging("COMM", $"SignalRegistrationCache.InvalidateCache: {ex.Message}")
        End Try
    End Sub
End Class

Public Class GM_INCA_CommClass : Inherits MarshalByRefObject


    Implements IGM_INCA_Comm

    Private ResetDataCollectionProcessVars As Boolean
    Private mySleepTime As Integer
    Private myDeviceRasterPair() As IGM_INCA_Comm.DeviceRasterPairs  'Set in HandleDeviceRasterPairs which is called from GM_INCA_CommClass - RegisterSignals
    Private DeviceRasterPairData(,) As Double
    Private mySignalData() As Double
    Private mySignalDataWithTime() As IGM_INCA_Comm.TransferDataWithTime
    Private myThread As Thread
    Private rci2 As RCI2
    Private CurrentINCAVersion As String

    ' Fields
    Private myinca As Inca
    'Private MyHWC As Object
    'Private INCADatabase As String
    'Private CurrentIncaVersion As String
    'Private Initialized As Boolean
    Public ReadOnly Property IncaInstance As Inca
        Get
            Return myinca
        End Get
    End Property
    'Private InitForm As Form

    Private MyExperimentEnvironment As ExperimentEnvironment
    Private myExperiment As Experiment
    Public myIncaOnlineExperiment As IncaOnlineExperiment
    Public myExpEnvView As ExperimentView

    ' Add module-level cache
    Private _cachedActiveLabels As String() = Nothing
    Private _activeLabelsTimestamp As DateTime = DateTime.MinValue
    Private Const ACTIVE_LABELS_CACHE_SECONDS As Integer = 30

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Private Shared Function LoadLibrary(lpFileName As String) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function FreeLibrary(hModule As IntPtr) As Boolean
    End Function

    ' Modified helper function
    Private Function GetCachedActiveMeasureLabels() As String()
        ' Return cached labels if still valid
        If _cachedActiveLabels IsNot Nothing AndAlso
           DateTime.Now.Subtract(_activeLabelsTimestamp).TotalSeconds < ACTIVE_LABELS_CACHE_SECONDS Then
            Return _cachedActiveLabels
        End If

        ' Refresh cache
        _cachedActiveLabels = GetAllActiveMeasureLabels()
        _activeLabelsTimestamp = DateTime.Now
        Return _cachedActiveLabels
    End Function

    Public Function SetLastRecordingFileName(ByVal fileName As String) As Boolean Implements IGM_INCA_Comm.SetLastRecordingFileName
        ' Sets the INCA recorder's "last recording filename" property.
        ' Caller must ensure no acquisition is running when calling this (API requirement).
        ActiveIncaApiCall = "GetLastRecordingFileName"
        Try
            If myIncaOnlineExperiment Is Nothing Then
                HandleUserMessageLogging("COMM", "GetLastRecordingFileName: myIncaOnlineExperiment is Nothing")
                Return False
            End If

            ' Direct API call (INCA 7.4+)
            Dim ok As String = myIncaOnlineExperiment.GetLastRecordingFileName()
            HandleUserMessageLogging("COMM", "GetLastRecordingFileName returned " & ok.ToString() & " for " & fileName)
            Return ok
        Catch ex As Exception
            HandleUserMessageLogging("COMM", "GetLastRecordingFileName Exception: " & ex.Message)
            Return False
        Finally
            ActiveIncaApiCall = String.Empty
        End Try
    End Function

    Public Function GetActualRecordingTimeMs() As Integer Implements IGM_INCA_Comm.GetActualRecordingTimeMs
        If myIncaOnlineExperiment IsNot Nothing Then
            Return myIncaOnlineExperiment.GetActualRecordingTimeMs()
        Else
            Return -1 ' or another value indicating not available
        End If
    End Function

    Public Function GetRemainingRecordingTimeMs() As Integer Implements IGM_INCA_Comm.GetRemainingRecordingTimeMs
        If myIncaOnlineExperiment IsNot Nothing Then
            Return myIncaOnlineExperiment.GetRemainingRecordingTimeMs()
        Else
            Return -1
        End If
    End Function

    ''' <summary>
    ''' Stop measurement and save to file using INCA 7.5+ APIs.
    ''' Tries methods in order of preference:
    ''' 1. StopMeasurementAndSave() - Uses current template (best for auto-increment)
    ''' 2. StopMeasurementAndSaveAs() - Saves with specific filename
    ''' 3. StopMeasurementAndSaveToFile() - Legacy method (fallback)
    ''' </summary>
    Public Function StopMeasurementAndSaveToFile(fileName As String, fileFormat As String) As Boolean Implements IGM_INCA_Comm.StopMeasurementAndSaveToFile
        Try
            If myIncaOnlineExperiment Is Nothing Then
                HandleUserMessageLogging("COMM", "StopMeasurementAndSaveToFile: myIncaOnlineExperiment is Nothing")
                Return False
            End If

            ActiveIncaApiCall = "StopMeasurementAndSaveToFile"

            ' ✅ DIAGNOSTIC: Log entry state
            Dim recordingState As Boolean = myIncaOnlineExperiment.GetRecordingState()
            HandleUserMessageLogging("COMM", $"StopMeasurementAndSaveToFile: ENTRY - recordingState={recordingState}, fileName='{fileName}', fileFormat='{fileFormat}'")

            ' If not recording, nothing to stop
            If Not recordingState Then
                HandleUserMessageLogging("COMM", "StopMeasurementAndSaveToFile: ❌ Not currently recording, nothing to stop")
                Return False
            End If

            ' ✅ STRATEGY 1: Try StopMeasurementAndSave() - Uses template (preferred for auto-increment)
            Try
                HandleUserMessageLogging("COMM", "StopMeasurementAndSaveToFile: Trying StopMeasurementAndSave() (uses template)")
                myIncaOnlineExperiment.StopMeasurementAndSave()
                HandleUserMessageLogging("COMM", "StopMeasurementAndSaveToFile: ✅ SUCCESS - StopMeasurementAndSave()")
                Return True
            Catch ex As MissingMethodException
                HandleUserMessageLogging("COMM", "StopMeasurementAndSaveToFile: StopMeasurementAndSave() not available (method doesn't exist)")
            Catch ex As Exception
                HandleUserMessageLogging("COMM", $"StopMeasurementAndSaveToFile: StopMeasurementAndSave() failed: {ex.Message}")
            End Try

            ' ✅ STRATEGY 2: Try StopMeasurementAndSaveAs(fileName, fileFormat)
            Try
                HandleUserMessageLogging("COMM", $"StopMeasurementAndSaveToFile: Trying StopMeasurementAndSaveAs('{fileName}', '{fileFormat}')")
                myIncaOnlineExperiment.StopMeasurementAndSaveAs(fileName)
                HandleUserMessageLogging("COMM", "StopMeasurementAndSaveToFile: ✅ SUCCESS - StopMeasurementAndSaveAs()")
                Return True
            Catch ex As MissingMethodException
                HandleUserMessageLogging("COMM", "StopMeasurementAndSaveToFile: StopMeasurementAndSaveAs() not available (method doesn't exist)")
            Catch ex As Exception
                HandleUserMessageLogging("COMM", $"StopMeasurementAndSaveToFile: StopMeasurementAndSaveAs() failed: {ex.Message}")
            End Try

            ' ✅ STRATEGY 3: Fallback to StopMeasurementAndSaveToFile(fileName, fileFormat) - Legacy method
            Try
                HandleUserMessageLogging("COMM", $"StopMeasurementAndSaveToFile: Trying StopMeasurementAndSaveToFile('{fileName}', '{fileFormat}') - fallback")
                Dim result As Boolean = myIncaOnlineExperiment.StopMeasurementAndSaveToFile(fileName, fileFormat)
                If result Then
                    HandleUserMessageLogging("COMM", "StopMeasurementAndSaveToFile: ✅ SUCCESS - StopMeasurementAndSaveToFile() (fallback)")
                Else
                    HandleUserMessageLogging("COMM", "StopMeasurementAndSaveToFile: ❌ FAILED - StopMeasurementAndSaveToFile() returned False")
                End If
                Return result
            Catch ex As Exception
                HandleUserMessageLogging("COMM", $"StopMeasurementAndSaveToFile: ❌ ALL METHODS FAILED - Last error: {ex.Message}")
                Return False
            End Try

        Catch ex As Exception
            HandleUserMessageLogging("COMM", $"StopMeasurementAndSaveToFile: ❌ Unhandled exception: {ex.Message}")
            Return False
        Finally
            ActiveIncaApiCall = String.Empty
        End Try
    End Function

    ''' <summary>
    ''' Publicly settable recording file duration (minutes) — the UI (OnVehicleScreen) should call SetRecordingFileDurationMinutes
    ''' with the value selected in ComboBox1. Use -1 to mean "ALL" (always check).
    ''' </summary>
    Private _recordingFileDurationMinutes As Integer = -1

    Public Sub SetRecordingFileDurationMinutes(ByVal minutes As Integer)
        _recordingFileDurationMinutes = minutes
        HandleUserMessageLogging("COMM", "SetRecordingFileDurationMinutes: " & minutes.ToString)
    End Sub

    Public Function GetRecordingFileDurationMinutes() As Integer
        Return _recordingFileDurationMinutes
    End Function

    ''' <summary>
    ''' Helper used by the UI polling loop (e.g. HandleUpdatesWhenRecording) to decide whether to test
    ''' for a newly closed recording file.  This reduces repeated checks on every loop.
    ''' Strategy:
    '''  - If duration is -1 (ALL) -> always check.
    '''  - If experiment is not recording -> check (to handle state changes).
    '''  - Otherwise use INCA's remaining-recording-time to gate checks:
    '''  - If remaining less than or equal to (pollIntervalMs + marginMs) then return True (we are near rotation)
    ''' </summary>
    Public Function IsTimeToCheckForNewClosedFile(ByVal pollIntervalMs As Integer, Optional ByVal marginMs As Integer = 5000) As Boolean
        Try
            ' If user selected ALL, we keep legacy behaviour (check every loop)
            If _recordingFileDurationMinutes = -1 Then
                Return True
            End If

            If myIncaOnlineExperiment Is Nothing Then
                ' No experiment object -> be conservative and check
                Return True
            End If

            ' If not recording, we should check to catch file closure that happened earlier
            Dim recordingState As Boolean = False
            Try
                recordingState = myIncaOnlineExperiment.GetRecordingState()
            Catch ex As Exception
                HandleUserMessageLogging("COMM", "IsTimeToCheckForNewClosedFile: GetRecordingState failed: " & ex.Message)
                ' if unable to query state, fall back to checking
                Return True
            End Try

            If Not recordingState Then
                Return True
            End If

            ' If INCA provides remaining time, use it to decide if we should check now.
            Try
                Dim remainingMs As Integer = myIncaOnlineExperiment.GetRemainingRecordingTimeMs()
                ' Negative or zero means unknown/unavailable -> fall back to checking
                If remainingMs <= 0 Then
                    Return True
                End If

                ' If remaining time is within the poll window + margin, check now
                If remainingMs <= (pollIntervalMs + marginMs) Then
                    Return True
                End If

                ' Otherwise skip check for now
                Return False
            Catch ex As Exception
                HandleUserMessageLogging("COMM", "IsTimeToCheckForNewClosedFile: GetRemainingRecordingTimeMs failed: " & ex.Message)
                ' unable to query remaining time -> check
                Return True
            End Try
        Catch ex As Exception
            HandleUserMessageLogging("COMM", "IsTimeToCheckForNewClosedFile: " & ex.Message)
            Return True
        End Try
    End Function

    Public Sub CloseExperiment() Implements IGM_INCA_Comm.CloseExperiment

        'Called from ConvertUpdatedWorkspace and HandleFlashAndDrive: unlocks and closes the INCA experiment.

        ActiveIncaApiCall = "CloseExperiment"

        myIncaOnlineExperiment.UnlockExperiment()
        myinca.UnlockTool()
        myExpEnvView.Close()
        myExperiment = Nothing
        MyHWC = Nothing

        ActiveIncaApiCall = String.Empty

    End Sub

    Private Function HandleDeviceRasterPairs(ByVal DeviceName As String, ByVal RasterName As String) As Integer

        'Handles the building of the devicerasterpair structure.
        'Used to map the data from INCA, or from a playback file, to the display
        'items, either in the display grids, or in the top down view.

        'DeviceRasterPairs are unique combinations of device name and raster name.

        Dim DeviceRasterPairFound As Boolean
        Dim i As Short
        Dim SaveDRPIndex = -1

        If myDeviceRasterPair Is Nothing Then

            ReDim myDeviceRasterPair(0)
            myDeviceRasterPair(0).DeviceName = DeviceName
            myDeviceRasterPair(0).RasterName = RasterName

            SaveDRPIndex = 0

            myDeviceRasterPair(0).NumValidVars = 1

        Else

            For i = 0 To UBound(myDeviceRasterPair)
                If (myDeviceRasterPair(i).DeviceName <> DeviceName) Or (myDeviceRasterPair(i).RasterName <> RasterName) Then
                    DeviceRasterPairFound = False
                Else
                    DeviceRasterPairFound = True
                    myDeviceRasterPair(i).NumValidVars = myDeviceRasterPair(i).NumValidVars + 1
                    SaveDRPIndex = i
                    Exit For
                End If
            Next
            If DeviceRasterPairFound = False Then
                ReDim Preserve myDeviceRasterPair(UBound(myDeviceRasterPair) + 1)
                myDeviceRasterPair(UBound(myDeviceRasterPair)).DeviceName = DeviceName
                myDeviceRasterPair(UBound(myDeviceRasterPair)).RasterName = RasterName

                SaveDRPIndex = UBound(myDeviceRasterPair)

                'If we have defined a new Device Raster pair, we must also initialize the
                'number of valid variables for this device raster pair to 1.

                myDeviceRasterPair(SaveDRPIndex).NumValidVars = 1

            End If
        End If

        HandleDeviceRasterPairs = SaveDRPIndex


    End Function

    Public Function GetCurrentVersion() As String Implements IGM_INCA_Comm.GetCurrentVersion

        'Returns the INCA version being used  --  CurrentINCAVersion is a global set in LaunchINCA ...

        GetCurrentVersion = CurrentINCAVersion

    End Function


    Public Function GetAvailableWorkspaces() As String() Implements IGM_INCA_Comm.GetAvailableWorkspaces

        'Returns a string array of all workspace names in the CLEVIR Setup\Workspaces folder...

        Dim x As Integer
        Dim y As Integer
        Dim myfolder As IncaFolder

        Dim MytempHWC As HardwareConfiguration

        Dim NotAWorkspace As Boolean

        Dim MyDatabaseItems() As DataBaseItem

        Dim savestringarray As String()
        ReDim savestringarray(0)
        savestringarray(0) = ""

        Try

            ActiveIncaApiCall = "GetAvailableWorkspaces"

            HandleUserMessageLogging("GMRC", "GetAvailableWorkspaces Called...")

            myfolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Workspaces")

            MyDatabaseItems = myfolder.BrowseDataBaseItem("*")

            If MyDatabaseItems IsNot Nothing Then

                If MyDatabaseItems.Length > 0 Then

                    y = 0
                    For x = 0 To UBound(MyDatabaseItems)

                        'change this to eliminate system.invalidcastexception errors, check MyDatabaseItems(x).IsHardwareConfiguration instead...

                        Try
                            MytempHWC = MyDatabaseItems(x)
                        Catch
                            If Err.Number = 13 Then
                                NotAWorkspace = True
                            End If
                        Finally
                            If NotAWorkspace = False Then

                                'If (Mid(MyDatabaseItems(x).GetName(), 1, 5) = "ASE34" Or
                                ' Mid(MyDatabaseItems(x).GetName(), 1, 5) = "ASE37" Or
                                'IsNumeric(Mid(MyDatabaseItems(x).GetName(), 1, 9)) Or
                                'Mid(MyDatabaseItems(x).GetName(), 1, 3) = "FCM" Or
                                'Mid(MyDatabaseItems(x).GetName(), 1, 3) = "FCM100" Or
                                'Mid(MyDatabaseItems(x).GetName(), 1, 3) = "ACP") And InStr(MyDatabaseItems(x).GetName(), "WorkspaceTemplate") = 0 Then

                                If InStr(MyDatabaseItems(x).GetName(), "WorkspaceTemplate") = 0 Then

                                    ReDim Preserve savestringarray(y)
                                    savestringarray(y) = MyDatabaseItems(x).GetName()
                                    y += 1

                                End If

                            Else
                                NotAWorkspace = False
                            End If
                        End Try
                    Next x

                Else
                    HandleUserMessageLogging("GMRC", "No Workspaces found in " & myfolder.GetNameWithPath,, )
                    savestringarray(0) = "No Workspaces found in " & myfolder.GetNameWithPath
                End If

            Else

                HandleUserMessageLogging("GMRC", "No Workspaces found in " & myfolder.GetNameWithPath,, )
                savestringarray(0) = "No Workspaces found in " & myfolder.GetNameWithPath

            End If

            'GetAvailableWorkspaces = savestringarray

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GetAvailableWorkspaces - Returned " & ex.Message, DisplayMsgBox, )
            savestringarray(0) = "GetAvailableWorkspaces - Returned " & ex.Message
            'GetAvailableWorkspaces = savestringarray

        Finally
            ActiveIncaApiCall = String.Empty
            GetAvailableWorkspaces = savestringarray
        End Try



    End Function

    Public Function UnlockExperiment() As Boolean Implements IGM_INCA_Comm.UnlockExperiment

        'Unlocks the experiment.  The experiment is locked when opened initially. Locking causes
        'a message box to be displayed by INCA when the user attempts to close the experiment, indicating
        'that the experiment was launched by a "third party app" and has been locked.  It is just
        'for information to the user and allows them to decide if they want to close the experiment.

        UnlockExperiment = False

        If myIncaOnlineExperiment IsNot Nothing Then

            UnlockExperiment = myIncaOnlineExperiment.UnlockExperiment()

        End If

    End Function


    Public Function GetRecordingState() As Boolean Implements IGM_INCA_Comm.GetRecordingState

        'Returns the INCA recording state obtained from the OnlineExperiment 

        ActiveIncaApiCall = "GetRecordingState"

        GetRecordingState = myIncaOnlineExperiment.GetRecordingState

        ActiveIncaApiCall = String.Empty
    End Function

    Public Function AddUserToList(ByVal UserName As String) As Boolean Implements IGM_INCA_Comm.AddUserToList

        'Keeps track of the number of times a user has logged in and / or creates a new user login entry in the UserIDList.txt file...

        Dim x As Integer
        Dim Found As Boolean

        AddUserToList = True

        For x = 0 To LoginIDNameAndFreqAL.Count - 1

            If InStr(UCase(LoginIDNameAndFreqAL(x).ToString), UCase(UserName)) > 0 Then
                Found = True
                AddUserToList = False
                Exit For
            End If

        Next

        If Found = False Then

            LoginIDNameAndFreqAL.Add("000001 " & UserName)

            WriteLoginIDListFile()

        End If

    End Function

    Public Function InitializeHardware() As Boolean Implements IGM_INCA_Comm.InitializeHardware

        'Initializes INCA hardware based on the current hardware configuration...

        Try
            If MyExperimentEnvironment IsNot Nothing AndAlso MyHWC IsNot Nothing Then
                HandleUserMessageLogging("COMM", "Initializing Hardware...")

                InitializeHardware = MyHWC.InitializeHardware()

                If InitializeHardware Then
                    HandleUserMessageLogging("COMM", "Hardware Initialized")
                End If
            Else
                InitializeHardware = False
            End If
        Catch ex As Exception
            HandleUserMessageLogging("COMM", "InitializeHardware: " & ex.Message)
            InitializeHardware = False
        End Try

    End Function

    Public Function ClearINCAMonitor() As Boolean Implements IGM_INCA_Comm.ClearINCAMonitor
        ClearINCAMonitor = myinca.ClearMonitor
    End Function

    Public Function WriteMonitorLogFileToPathUsingFileName(ByVal pathname As String, ByVal filename As String) As Boolean Implements IGM_INCA_Comm.WriteMonitorLogFileToPathUsingFileName

        WriteMonitorLogFileToPathUsingFileName = myinca.WriteMonitorLogFileToPathUsingFileName(pathname, filename)

    End Function

    Public Function GetAvailableExperimentNames() As String() Implements IGM_INCA_Comm.GetAvailableExperimentNames

        Dim x As Integer
        Dim tempstr As String
        Dim savestringarray() As String
        Dim y As Integer
        Dim myfolder As IncaFolder
        Dim MyDatabaseItems() As DataBaseItem

        ActiveIncaApiCall = "GetAvailableExperimentNames"

        ReDim savestringarray(0)

        HandleUserMessageLogging("GMRC", "INCA Comm: GetAvailableExperimentNames Called...")

        myfolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Experiments")

        MyDatabaseItems = myfolder.BrowseDataBaseItem("Empty Experiment")

        If MyDatabaseItems Is Nothing Or UBound(MyDatabaseItems) < 0 Then
            myfolder.AddExperimentEnvironment("Empty Experiment")
        End If

        MyDatabaseItems = myfolder.BrowseDataBaseItem("*")

        If MyDatabaseItems IsNot Nothing Then

            y = 0
            For x = 0 To UBound(MyDatabaseItems)

                If MyDatabaseItems(x).IsExperimentEnvironment = True Then

                    If InStr(MyDatabaseItems(x).GetNameWithPath, "CLEVIR Setup\Experiments") > 0 Then
                        tempstr = MyDatabaseItems(x).GetName()
                        ReDim Preserve savestringarray(y)
                        savestringarray(y) = tempstr
                        y += 1
                    End If

                End If

            Next x

        Else

            HandleUserMessageLogging("GMRC", "INCA Comm: No Experiments in " & myfolder.GetNameWithPath)
            savestringarray(0) = "Invalid"
            GetAvailableExperimentNames = savestringarray

            ActiveIncaApiCall = String.Empty
            Exit Function
        End If

        HandleUserMessageLogging("GMRC", "INCA Comm: Found valid experiments in " & myfolder.GetNameWithPath)

        GetAvailableExperimentNames = savestringarray

        ActiveIncaApiCall = String.Empty

    End Function

    Public Function GetINCAPollingRate() As Double Implements IGM_INCA_Comm.GetINCAPollingRate
        GetINCAPollingRate = myinca.GetPollingCycleTimeTS
    End Function

    Private Sub DataCollectionProcess()
        ' This routine runs continuously after a start request from the client until it receives a stop
        ' request from the client. It collects data from all signals registered for each device/raster pair.
        ' Static variables retain their values between calls
        Static hasRunBefore As Boolean = False
        Static saveDeviceRasterPairData(,) As Double
        Static saveTimeStamp() As Double
        Static recordCounter() As Integer

        ' Reset static variables if the reset flag is set
        If ResetDataCollectionProcessVars Then
            ResetStaticVariables(hasRunBefore, saveDeviceRasterPairData, saveTimeStamp, recordCounter)
        End If

        ' Ensure arrays are sized for device/raster pairs
        InitializeArrays(saveDeviceRasterPairData, saveTimeStamp, recordCounter)

        ' Log when data collection begins
        If Not hasRunBefore Then
            HandleUserMessageLogging("COMM", "Data Collection Initiated....")
            hasRunBefore = True
        End If

        ' Main collection loop - check the cancellation flag on each iteration.
        Do While Not _cancellationRequested
            Try
                CollectDataForAllPairs(saveDeviceRasterPairData, saveTimeStamp, recordCounter)
            Catch ex As Exception
                HandleUserMessageLogging("COMM", "DataCollectionProcess - Exception = " & ex.Message)
            End Try

            ' Sleep before the next iteration
            Thread.Sleep(mySleepTime)
        Loop

        ' Optionally log that the data collection process is ending
        HandleUserMessageLogging("COMM", "Data Collection Process Exiting....")
    End Sub

    ' Resets static variables to their initial state
    Private Sub ResetStaticVariables(ByRef hasRunBefore As Boolean,
                                 ByRef saveDeviceRasterPairData(,) As Double,
                                 ByRef saveTimeStamp() As Double,
                                 ByRef recordCounter() As Integer)
        ResetDataCollectionProcessVars = False
        hasRunBefore = False
        saveDeviceRasterPairData = Nothing
        saveTimeStamp = Nothing
        recordCounter = Nothing
    End Sub

    ' Ensures arrays are properly initialized for device/raster pairs
    Private Sub InitializeArrays(ByRef saveDeviceRasterPairData(,) As Double,
                             ByRef saveTimeStamp() As Double,
                             ByRef recordCounter() As Integer)
        ReDim DeviceRasterPairData(UBound(myDeviceRasterPair), 0)
        If saveDeviceRasterPairData Is Nothing Then
            ReDim saveDeviceRasterPairData(UBound(myDeviceRasterPair), 0)
            ReDim saveTimeStamp(UBound(myDeviceRasterPair))
        End If
        ReDim recordCounter(UBound(myDeviceRasterPair))
    End Sub

    ' Collects data for all device/raster pairs
    Private Sub CollectDataForAllPairs(ByRef saveDeviceRasterPairData(,) As Double,
                                   ByRef saveTimeStamp() As Double,
                                   ByRef recordCounter() As Integer)
        For pairIndex As Integer = 0 To UBound(myDeviceRasterPair)
            Dim noOfRecords As Integer = 25
            Dim incaData As IGM_INCA_Comm.INCAData =
            GetDataArray(
                myDeviceRasterPair(pairIndex).DeviceName,
                myDeviceRasterPair(pairIndex).RasterName,
                myDeviceRasterPair(pairIndex).NumValidVars,
                noOfRecords
            )
            If noOfRecords > 0 Then
                ' Process valid records
                recordCounter(pairIndex) = 0
                ProcessValidRecords(
                pairIndex,
                noOfRecords,
                incaData,
                DeviceRasterPairData,
                saveDeviceRasterPairData,
                saveTimeStamp
            )
            Else
                ' Process no records returned
                recordCounter(pairIndex) += 1
                ProcessNoRecords(
                pairIndex,
                recordCounter,
                DeviceRasterPairData,
                saveDeviceRasterPairData,
                saveTimeStamp
            )
            End If
        Next
    End Sub

    ''' <summary>
    ''' Processes valid records (noOfRecords > 0) for a particular device/raster pair.
    ''' Updates DeviceRasterPairData(), saveDeviceRasterPairData(), saveTimeStamp(), 
    ''' and also writes the data to myDeviceRasterSignals/mySignalData/mySignalDataWithTime.
    ''' </summary>
    Private Sub ProcessValidRecords(pairIndex As Integer,
                                    noOfRecords As Integer,
                                    incaData As IGM_INCA_Comm.INCAData,
                                    ByRef DeviceRasterPairData(,) As Double,
                                    ByRef saveDeviceRasterPairData(,) As Double,
                                    ByRef saveTimeStamp() As Double)

        For varIndex As Integer = 0 To myDeviceRasterPair(pairIndex).NumValidVars - 1

            ' Ensure array dimensions are large enough
            If varIndex > UBound(DeviceRasterPairData, 2) Then
                ReDim Preserve DeviceRasterPairData(UBound(myDeviceRasterPair), varIndex)
                ReDim Preserve saveDeviceRasterPairData(UBound(myDeviceRasterPair), varIndex)
            End If

            ' If the latest time is non-zero, update data from the newest record
            If incaData.myTime(noOfRecords - 1) <> 0 Then
                DeviceRasterPairData(pairIndex, varIndex) =
                    incaData.myData((varIndex * noOfRecords) + (noOfRecords - 1))
                saveDeviceRasterPairData(pairIndex, varIndex) =
                    DeviceRasterPairData(pairIndex, varIndex)
                saveTimeStamp(pairIndex) = incaData.myTime(noOfRecords - 1)
            Else
                ' Otherwise, keep the last known good value
                DeviceRasterPairData(pairIndex, varIndex) =
                    saveDeviceRasterPairData(pairIndex, varIndex)
            End If

            ' Copy to the matching signal objects
            UpdateSignalData(pairIndex, varIndex, DeviceRasterPairData, incaData.myTime(noOfRecords - 1))
        Next
    End Sub


    ''' <summary>
    ''' Processes the scenario where no records are returned (noOfRecords &lt;= 0).
    ''' Increments the counter and either reverts data to the saved values or zeros them out.
    ''' </summary>
    Private Sub ProcessNoRecords(pairIndex As Integer,
                             ByRef recordCounter() As Integer,
                             ByRef DeviceRasterPairData(,) As Double,
                             ByRef saveDeviceRasterPairData(,) As Double,
                             ByRef saveTimeStamp() As Double)

        For varIndex As Integer = 0 To UBound(DeviceRasterPairData, 2)
            ' Ensure array dimensions are large enough
            If varIndex > UBound(DeviceRasterPairData, 2) Then
                ReDim Preserve DeviceRasterPairData(UBound(myDeviceRasterPair), varIndex)
                ReDim Preserve saveDeviceRasterPairData(UBound(myDeviceRasterPair), varIndex)
            End If

            ' Revert to saved data
            DeviceRasterPairData(pairIndex, varIndex) = saveDeviceRasterPairData(pairIndex, varIndex)

            ' If counter has exceeded threshold, reset to zero
            If recordCounter(pairIndex) > CounterValue Then
                saveDeviceRasterPairData(pairIndex, varIndex) = 0
            End If

            ' Copy to the matching signals
            UpdateSignalData(pairIndex, varIndex, DeviceRasterPairData, saveTimeStamp(pairIndex))
        Next

        ' Reset counter if exceeded
        If recordCounter(pairIndex) > CounterValue Then
            recordCounter(pairIndex) = 0
        End If
    End Sub


    ''' <summary>
    ''' Updates myDeviceRasterSignals(), mySignalData(), and mySignalDataWithTime() for
    ''' the appropriate device/raster signal match.
    ''' </summary>
    Private Sub UpdateSignalData(pairIndex As Integer,
                                 varIndex As Integer,
                                 ByRef DeviceRasterPairData(,) As Double,
                                 currentTimeStamp As Double)

        For z As Integer = 0 To UBound(myDeviceRasterSignals)
            If myDeviceRasterSignals(z).Status = "Valid" Then
                If (myDeviceRasterSignals(z).DeviceRasterPairNum = pairIndex) AndAlso
                   (myDeviceRasterSignals(z).DeviceRasterPairVarNum = varIndex) Then

                    myDeviceRasterSignals(z).Value = DeviceRasterPairData(pairIndex, varIndex)
                    mySignalData(z) = DeviceRasterPairData(pairIndex, varIndex)
                    mySignalDataWithTime(z).SignalData = DeviceRasterPairData(pairIndex, varIndex)
                    mySignalDataWithTime(z).TimeStamp = currentTimeStamp

                    Exit For
                End If
            End If
        Next
    End Sub

    Public Function GetGM_INCA_CommStatus() As String() Implements IGM_INCA_Comm.GetGM_INCA_CommStatus

        'Not currently used...

        'Returns the most recent status message that has been written to the GM_INCA_Comm log file.

        'This is used by the GM_INCA_Server to display the status of the GM_INCA_Comm interface on 
        'its display.

        'This is primarily being used for debug purposes during development.

        GetGM_INCA_CommStatus = StatusString
    End Function

    Public Function GetMeasurementStatus() As String Implements IGM_INCA_Comm.GetMeasurementStatus

        'This function returns a string "true", "false" or "invalid" to indicate whether INCA is 
        'in measurement mode

        'This function is used by the GmResidentClient when StopMeasurement or StartMeasurement is called
        'It is also used by the RegisterSignals function to determine if measurement must be stopped
        'prior to registering signals.

        Dim myLocalExperiment As Experiment

        ActiveIncaApiCall = "GetMeasurementStatus"

        Try

            GetMeasurementStatus = "Invalid"

            If myinca IsNot Nothing Then

                myLocalExperiment = myinca.GetOpenedExperiment()

                If myLocalExperiment IsNot Nothing Then

                    Select Case myIncaOnlineExperiment.IsMeasurementRunning
                        Case True
                            GetMeasurementStatus = "True"
                        Case False
                            GetMeasurementStatus = "False"
                    End Select

                End If

            End If

        Catch ex As Exception

            HandleUserMessageLogging("COMM", "GetMeasurementStatus Failure " & ex.Message)
            GetMeasurementStatus = "GetMeasurementStatus Failure"

        Finally
            ActiveIncaApiCall = String.Empty
        End Try


    End Function

    Public Function GetDeviceRasterPairData() As Double(,) Implements IGM_INCA_Comm.GetDeviceRasterPairData

        'This function passes back the DeviceRasterPairData that is maintained by the DataCollectionProcess background
        'task.  This is the old way of doing things, we now request signal data based on a list of signal
        'names maintained in the client.  The client no longer needs to be aware of the device/raster pairs
        'that need to be used to collect the data.  This is now all handled in the server and not on the
        'client side.

        'The GmResidentClient does not currently use this function.

        GetDeviceRasterPairData = DeviceRasterPairData
    End Function

    Public Function GetSignalData() As Double() Implements IGM_INCA_Comm.GetSignalData

        'This function not currently used.  Replaced by GetSignalDataWithTime...

        Dim tferData As IGM_INCA_Comm.TransferData

        tferData.SignalData = mySignalData

        GetSignalData = tferData.SignalData

    End Function

    Public Function GetSignalDataWithTime() As IGM_INCA_Comm.TransferDataWithTime() Implements IGM_INCA_Comm.GetSignalDataWithTime

        'This function passes back the signaldata that is maintained by the DataCollectionProcess background
        'task

        'This function is currently used by CLEVIR as the primary method for acquiring
        'signal data from INCA

        Dim tferData() As IGM_INCA_Comm.TransferDataWithTime

        Try

            tferData = mySignalDataWithTime
            GetSignalDataWithTime = tferData

        Catch ex As Exception
            HandleUserMessageLogging("COMM", " GetSignalDataWithTime - Exception = " & ex.Message)
            GetSignalDataWithTime = Nothing
        End Try

    End Function

    Private _cancellationRequested As Boolean = False

    Public Sub StartDataCollection(ByVal sleeptime As Integer) Implements IGM_INCA_Comm.StartDataCollection
        Try
            If myThread Is Nothing Then
                HandleUserMessageLogging("COMM", "Starting Data Collection - DataCollectionRate = " & sleeptime & "....")
                mySleepTime = sleeptime
                _cancellationRequested = False
                myThread = New Thread(AddressOf Me.DataCollectionProcess) With {
                    .IsBackground = True
                }
                myThread.Start()
            Else
                HandleUserMessageLogging("COMM", "Data Collection Running....")
                If sleeptime <> mySleepTime Then
                    HandleUserMessageLogging("COMM", "Data update rate changed from " & mySleepTime & " to " & sleeptime)
                    mySleepTime = sleeptime
                End If
            End If
        Catch ex As Exception
            HandleUserMessageLogging("COMM", "StartDataCollection - Failed to start or update data collection thread: " & ex.Message)
            ' If the thread failed to start, ensure it's null so a retry is possible.
            myThread = Nothing
        End Try
    End Sub

    Public Sub StopDataCollection() Implements IGM_INCA_Comm.StopDataCollection
        If myThread IsNot Nothing Then
            _cancellationRequested = True
            myThread.Join()  ' Wait for the thread to finish its loop gracefully.
            myThread = Nothing
            HandleUserMessageLogging("COMM", "Data Collection Stopped....")
        Else
            HandleUserMessageLogging("COMM", "Data Collection Not Running....")
        End If
    End Sub

    Public Overrides Function InitializeLifetimeService() As Object

        'This function no longer used...

        Dim lease As ILease = Nothing
        lease = DirectCast(MyBase.InitializeLifetimeService(), ILease)
        If lease IsNot Nothing Then
            lease.InitialLeaseTime = TimeSpan.FromMinutes(20)
            lease.RenewOnCallTime = TimeSpan.FromMinutes(20)
            lease.SponsorshipTimeout = TimeSpan.FromMinutes(20)

        End If
        Return (lease)

    End Function

    Public Function GetDefaultRasterForMeasureElementInDevice(ByVal devicename As String, ByVal measname As String) As String Implements IGM_INCA_Comm.GetDefaultRasterForMeasureElementInDevice

        'Pass in a device name and a signal name and this function passes back the default raster.
        'This is useful especially for CAN Monitoring signals because the default raster is not
        'readily accessible in INCA.

        'This function is called by the GmResidentClient whenever a new signal name is selected by the
        'user when configuring a grid display.

        Dim x As Short
        Dim myExperimentDevices() As ExperimentDevice

        myExperimentDevices = myExperiment.GetAllDevices()

        For x = 0 To UBound(myExperimentDevices)
            If myExperimentDevices(x).GetName = devicename Then
                Exit For
            End If
        Next

        If x > UBound(myExperimentDevices) Then
            GetDefaultRasterForMeasureElementInDevice = Nothing
        Else
            GetDefaultRasterForMeasureElementInDevice = myIncaOnlineExperiment.GetDefaultRasterForMeasureElementInDevice(measname, myExperimentDevices(x))
        End If

    End Function

    Public Function BrowseMeasureElementsInDevice(ByVal searchstr As String, ByVal devicename As String) As String() Implements IGM_INCA_Comm.BrowseMeasureElementsInDevice

        'Takes search string and device name (strings) as arguments and passes back
        'a string array containing all measure element names which match the search string that exists
        'in the device specified.

        Dim x As Short
        Dim y As Integer
        Dim myMeasureElementNames() As String

        Dim myExperimentDevices() As ExperimentDevice
        Dim myMeasureElements() As MeasureElement

        myMeasureElementNames = Nothing

        myExperimentDevices = myExperiment.GetAllDevices()

        For x = 0 To UBound(myExperimentDevices)
            If myExperimentDevices(x).GetName = devicename Then
                Exit For
            End If
        Next
        If x > UBound(myExperimentDevices) Then
            BrowseMeasureElementsInDevice = Nothing
        Else
            myMeasureElements = myIncaOnlineExperiment.BrowseMeasureElementInDevice(searchstr, myExperimentDevices(x))

            ReDim myMeasureElementNames(UBound(myMeasureElements))

            For y = 0 To UBound(myMeasureElements)
                myMeasureElementNames(y) = myMeasureElements(y).GetName

            Next
        End If

        BrowseMeasureElementsInDevice = myMeasureElementNames

    End Function


    Public Function GetAllMeasureElementNamesInDevice(ByVal devicename As String) As String() Implements IGM_INCA_Comm.GetAllMeasureElementNamesInDevice

        'Takes a device name argument and passes back a string array which contains all measurement
        'element names associated with the specified device name.

        Dim x As Short
        Dim y As Long
        Dim myMeasureElementNames() As String

        Dim myExperimentDevices() As ExperimentDevice
        Dim myMeasureElements() As MeasureElement

        myMeasureElementNames = Nothing

        myExperimentDevices = myExperiment.GetAllDevices()

        For x = 0 To UBound(myExperimentDevices)
            If myExperimentDevices(x).GetName = devicename Then
                Exit For
            End If
        Next
        If x > UBound(myExperimentDevices) Then
            GetAllMeasureElementNamesInDevice = Nothing
        Else
            myMeasureElements = myIncaOnlineExperiment.GetAllMeasureElementsInDevice(myExperimentDevices(x))

            ReDim myMeasureElementNames(UBound(myMeasureElements))

            For y = 0 To UBound(myMeasureElements)
                myMeasureElementNames(y) = myMeasureElements(y).GetName
            Next
        End If

        GetAllMeasureElementNamesInDevice = myMeasureElementNames

    End Function

    Public Function GetAllActiveMeasureLabels() As String() Implements IGM_INCA_Comm.GetAllActiveMeasureLabels

        'Passes back an array of strings with all active measure labels defined in the current
        'INCA experiment.

        ActiveIncaApiCall = "GetAllActiveMeasureLabels"

        If myIncaOnlineExperiment IsNot Nothing Then
            GetAllActiveMeasureLabels = myIncaOnlineExperiment.GetAllActiveMeasureLabels
        Else
            GetAllActiveMeasureLabels = Nothing
        End If

        ActiveIncaApiCall = String.Empty

    End Function

    Public Function GetLastRecordingFileName() As String Implements IGM_INCA_Comm.GetLastRecordingFileName

        'This function returns a string with the most recently defined recording file name.
        'If the GM_INCA_Comm interface is not currently connected to an INCA experiment,
        '"Invalid" is returned.

        'This function is used by CLEVIR when the voice record WAV file is stopped so that
        'the WAV file can be properly named based on the most recent recording file name.

        ActiveIncaApiCall = "GetLastRecordingFileName"

        If myIncaOnlineExperiment IsNot Nothing Then
            GetLastRecordingFileName = myIncaOnlineExperiment.GetLastRecordingFileName
        Else
            GetLastRecordingFileName = "Invalid"
        End If

        ActiveIncaApiCall = String.Empty

    End Function

    Public Function WriteEventComment(ByVal CommentString As String, ByVal aFlag As Boolean) As Boolean Implements IGM_INCA_Comm.WriteEventComment

        'This function writes an event comment based on the comment string passed in and returns
        'the status of the request.

        'This function is used by the GmResidentClient to write event comments.  Currently, the
        'aFlag argument is always false.

        'The aFlag argument, If True, the internal, predefined event comment will be written together 
        'with CommentString. False will ignore any internal comment and write only the argument CommentString. 


        If myIncaOnlineExperiment IsNot Nothing Then
            If myIncaOnlineExperiment.GetRecordingState = True Then
                WriteEventComment = myIncaOnlineExperiment.WriteEventComment(CommentString, aFlag)
                If WriteEventComment = True Then
                    HandleUserMessageLogging("COMM", "WriteEventComment - " & CommentString)
                Else
                    HandleUserMessageLogging("COMM", "WriteEventComment (" & CommentString & ") returned FALSE")
                End If

            Else
                WriteEventComment = False
                HandleUserMessageLogging("COMM", "GetRecordingState returned FALSE")
            End If
        Else
            WriteEventComment = False
        End If

    End Function

    Public Sub ResetRecords() Implements IGM_INCA_Comm.ResetRecords

        'This function is currently used internally.

        'This function is not currently used by the GmResidentClient

        rci2.IncaResetRecords()

    End Sub

    Private Function AddHeaderToFile(ByVal filename As String) As String

        Dim fnum As Integer
        Dim textarray() As String = Nothing
        Dim x As Integer = 0

        'My.Application.Info.DirectoryPath & "\LABFiles\" LabFileNameArray(y)

        '[SETTINGS]
        ';V1.1
        'MultiRasterSeparator;&
        '

        AddHeaderToFile = ""

        If System.IO.File.Exists(filename) Then
            fnum = FreeFile()
            FileOpen(fnum, filename, OpenMode.Input)
            Do While Not EOF(fnum)
                ReDim Preserve textarray(x)
                textarray(x) = LineInput(fnum)
                x += 1
            Loop

            FileClose(fnum)
            System.IO.File.Delete(filename)

            fnum = FreeFile()
            FileOpen(fnum, filename, OpenMode.Output)

            PrintLine(fnum, "[SETTINGS]")
            PrintLine(fnum, "Version;V1.1")
            PrintLine(fnum, "MultiRasterSeparator;&")
            PrintLine(fnum, "")

            For x = 0 To UBound(textarray)
                If x = 0 Then
                    PrintLine(fnum, textarray(x))
                Else
                    PrintLine(fnum, textarray(x) & ";")
                End If

            Next

            PrintLine(fnum, "")

            FileClose(fnum)

            'System.Threading.Thread.Sleep(5000)

        Else
            AddHeaderToFile = "FileNotFound"
        End If

    End Function

    Public Function SaveCalSnapShot(ByVal Page As String) As String Implements IGM_INCA_Comm.SaveCalSnapShot

        'Saves either Reference or Working dataset calibration data to a .CSV file.  This command
        'can be invoked from the Client on a button press, or can be enabled to take place when the
        'system transitions out of record mode.

        'Need .lab files for master and slave
        'need to open cdm
        'need to get dataset and perform list function using .lab file
        'need to put in proper directory
        'need to save both master and slave working datasets
        'need to do the same thing before going into record mode, but with reference dataset
        'need to do the same thing with working dataset any time stop record button is pressed

        Dim DB() As String
        Dim myDataSetPath As String
        Dim myDataSetName As String
        Dim x As Integer
        Dim y As Integer
        Dim i As Integer
        Dim SubFolder() As String = Nothing
        Dim Version() As String
        Dim StartPos As Integer
        Dim EndPos As Integer
        Dim Length As Integer
        Dim CompleteDatasetName As String
        Dim SaveFileName As String = ""
        Dim LabFileNameArray() As String
        Dim CDM As EtasCDMToolbox
        Dim myAsap2SourceProject() As Asap2Project
        Dim CreateNewLabFile As Boolean
        Dim LabFileNamePrefix() As String
        Dim ProjectDatabaseProcessorPaths() As String = Nothing

        Try

            HandleUserMessageLogging("GMRC", "Saving " & Page & " CAL Snapshots...")

            LabFileNamePrefix = Nothing

            If WorkingDataSetDataBasePaths IsNot Nothing Then

                SaveCalSnapShot = "success"

                CDM = Nothing

                ReDim myAsap2SourceProject(0)
                ReDim DB(0)
                ReDim Version(0)

                'Ubound(WorkingDataSetDataBasePath) is always the same as Ubound(ReferencDataSetDataBasePath)
                'so we can use it regardless of which Page is passed in "Working" or "Reference"

                For x = 0 To UBound(WorkingDataSetDataBasePaths)

                    ReDim Preserve DB(x)
                    ReDim Preserve Version(x)

                    Select Case Page
                        Case "Working"
                            DB(x) = WorkingDataSetDataBasePaths(x)
                        Case "Reference"
                            DB(x) = ReferenceDataSetDataBasePaths(x)
                    End Select

                    'Determine whether we are dealing with IMX6, or Komodo processor...

                    ReDim Preserve LabFileNamePrefix(x)

                    If InStr(DB(x), "\A17_IP") > 0 Then
                        'StartPos = 8
                        StartPos = InStr(DB(x), "\") + 8
                        'EndPos = 14
                        LabFileNamePrefix(x) = "IP_"
                    ElseIf InStr(DB(x), "\A17_IR") > 0 Then
                        StartPos = 6
                        StartPos = InStr(DB(x), "\") + 6
                        'EndPos = 12
                        LabFileNamePrefix(x) = "IR_"
                    ElseIf InStr(DB(x), "\A17IP") > 0 Then
                        'StartPos = 6
                        StartPos = InStr(DB(x), "\") + 6
                        'EndPos = 12
                        LabFileNamePrefix(x) = "IP_"
                    ElseIf InStr(DB(x), "\A17IR") > 0 Then
                        'StartPos = 6
                        StartPos = InStr(DB(x), "\") + 6
                        'EndPos = 12
                        LabFileNamePrefix(x) = "IR_"
                    ElseIf InStr(DB(x), "\A17K") > 0 Then
                        'StartPos = 7
                        StartPos = InStr(DB(x), "\") + 7
                        'EndPos = 13
                        'LabFileNamePrefix(x) = Mid(DB(x), 4, 3) & "_"
                        LabFileNamePrefix(x) = Mid(DB(x), InStr(DB(x), "\") + 4, 3) & "_"
                    ElseIf InStr(DB(x), "\A17_K") > 0 Then
                        'StartPos = 9
                        StartPos = InStr(DB(x), "\") + 8
                        'EndPos = 15
                        'LabFileNamePrefix(x) = Mid(DB(x), 5, 3) & "_"
                        LabFileNamePrefix(x) = Mid(DB(x), InStr(DB(x), "\") + 5, 3) & "_"
                    ElseIf InStr(DB(x), "ASE34_LC") > 0 Then
                        'StartPos = 10
                        StartPos = InStr(DB(x), "\") + 10
                        'EndPos = 16
                        LabFileNamePrefix(x) = "LC_"
                        'HC CHANGE
                    ElseIf InStr(DB(x), "ASE37_HC") > 0 Then
                        'StartPos = 11
                        StartPos = InStr(DB(x), "\") + 11
                        EndPos = 17
                        LabFileNamePrefix(x) = Mid(DB(x), 7, 3) & "_"
                    End If

                    EndPos = StartPos + 6

                    'Extract Version Number from DB Name

                    Length = (EndPos - StartPos) + 1

                    HandleUserMessageLogging("GMRC", "SaveCalSnapShot: " & DB(x))
                    Version(x) = Mid(DB(x), StartPos, Length)
                    HandleUserMessageLogging("GMRC", "SaveCalSnapShot: Version(" & x & ") = " & Version(x))

                    ReDim Preserve myAsap2SourceProject(x)
                    myAsap2SourceProject(x) = Nothing
                Next

                'Look thru all project database paths to find those that correspond to the processors...

                For x = 0 To UBound(ProjectDatabasePaths) 'Global set in SetProjectDatabaseInfo...

                    HandleUserMessageLogging("GMRC", "SaveCalSnapShot: ProjectDatabasePath(" & x & ")" & ProjectDatabasePaths(x))

                    'HC CHANGE
                    If InStr(ProjectDatabasePaths(x), "ASE37") > 0 Or InStr(ProjectDatabasePaths(x), "A17") > 0 Or InStr(ProjectDatabasePaths(x), "ASE34") > 0 Then 'if A17 or ASE37 or ASE34 is in string, then we have a processor...

                        ReDim Preserve ProjectDatabaseProcessorPaths(i)
                        ProjectDatabaseProcessorPaths(i) = ProjectDatabasePaths(x)
                        i += 1

                    End If

                Next

                For y = 0 To UBound(DB)

                    ReDim Preserve LabFileNameArray(y)
                    LabFileNameArray(y) = ""
                    CreateNewLabFile = False

                    If CalSnapShotFilesAreSet Is Nothing Then
                        ReDim CalSnapShotFilesAreSet(UBound(DB))
                        For i = 0 To UBound(DB)
                            CalSnapShotFilesAreSet(i) = False
                        Next i
                    End If

                    myDataSetPath = DB(y)
                    myDataSetName = Right(myDataSetPath, Len(myDataSetPath) - InStrRev(myDataSetPath, "\"))

                    LabFileNameArray(y) = My.Application.Info.DirectoryPath & "\LabFiles\" & LabFileNamePrefix(y) & Version(y) & ".LAB"

                    CompleteDatasetName = ProjectDatabaseProcessorPaths(y) & "\" & myDataSetPath

                    'here we determine if we need a new or want to re-create a lab file...

                    If System.IO.File.Exists(LabFileNameArray(y)) = False And CalSnapShotFilesAreSet(y) = False Then
                        HandleUserMessageLogging("GMRC", "SaveCalSnapShot: " & LabFileNameArray(y) & " not found.  Creating new lab file...")
                        CreateNewLabFile = True
                    Else
                        If CalSnapShotFilesAreSet(y) = False Then
                            'If MsgBox(LabFileNameArray(y) & " exists, Create New File?", vbYesNo) = vbYes Then
                            CreateNewLabFile = True
                            'End If
                        End If
                    End If

                    'If CDM Is Nothing Then
                    'CDM = myinca.GetCDMToolbox 'attach to the Calibration Data Manager (CDM) interface
                    'End If

                    If CDM IsNot Nothing Then
                        CDM = Nothing
                        CDM = myinca.GetCDMToolbox 'attach to the Calibration Data Manager (CDM) interface
                    Else
                        CDM = myinca.GetCDMToolbox 'attach to the Calibration Data Manager (CDM) interface
                    End If

                    'here we create the new labfile if necessary based on the project dataset...

                    If CreateNewLabFile = True Then

                        If CDM.RemoveAllDestinations = True Then

                            If CDM.SetSourceDataSet(CompleteDatasetName) = True Then

                                'Sets the output file format to anOutputFormat. Valid anOutputFormat are ASCII, HTML, DCM, CVX" 

                                If CDM.SetOutputFormat("HTML") = True Then

                                    If CDM.SetResultFilePath(My.Application.Info.DirectoryPath & "\LABFiles\") = True Then

                                        If CDM.AddLabelsToListUsingMask("K*") = True Then

                                            HandleUserMessageLogging("GMRC", "SaveCalSnapShot: Creating " & LabFileNameArray(y))
                                            If CDM.SaveLabelListToFile(LabFileNameArray(y)) = True Then
                                                'CopyToLog("SaveCalSnapShot: CDM.SaveLabelListToFile = True")
                                            Else
                                                HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.SaveLabelListToFile(" & LabFileNameArray(y) & " returned False")
                                                SaveCalSnapShot = "CDM.SaveLabelListToFile(" & LabFileNameArray(y) & " returned False"
                                                Exit Function
                                            End If

                                        Else
                                            HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.AddLabelsToListUsingMask('K*') returned False")
                                            SaveCalSnapShot = "CDM.AddLabelsToListUsingMask('K*') returned False"
                                            Exit Function
                                        End If

                                    Else
                                        HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.SetResultFilePath(" & My.Application.Info.DirectoryPath & "\LABFiles\ returned false")
                                        SaveCalSnapShot = "CDM.SetResultFilePath(" & My.Application.Info.DirectoryPath & "\LABFiles\ returned false"
                                        Exit Function
                                    End If

                                Else
                                    HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.SetOutputFormat(HTML) returned false")
                                    SaveCalSnapShot = "CDM.SetOutputFormat(HTML) returned false"
                                    Exit Function
                                End If

                            Else
                                HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.SetSourceDataSet(" & CompleteDatasetName & " returned False")
                                SaveCalSnapShot = "CDM.SetSourceDataSet(" & CompleteDatasetName & " returned False"
                                Exit Function
                            End If

                        Else
                            HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.RemoveAllDestinations returned false")
                            SaveCalSnapShot = "CDM.RemoveAllDestinations returned false"
                            Exit Function
                        End If

                        If Len(AddHeaderToFile(LabFileNameArray(y))) = 0 Then

                            CreateNewLabFile = False
                            CalSnapShotFilesAreSet(y) = True

                            CDM = Nothing

                        Else
                            HandleUserMessageLogging("GMRC", "SaveCalSnapShot: AddHeaderToFile returned FileNoFound")
                            SaveCalSnapShot = "AddHeaderToFile returned FileNotFound"
                            Exit Function
                        End If

                    Else
                        CreateNewLabFile = False
                        CalSnapShotFilesAreSet(y) = True
                    End If

                    'Here is where we load the list of calibrations (based on .lab file) and list them all to
                    'a .CSV file using the INCA Calibration Data Manager...

                    If CDM Is Nothing Then
                        CDM = myinca.GetCDMToolbox 'attach to the Calibration Data Manager (CDM) interface
                    End If

                    If CDM.RemoveAllDestinations = True Then

                        Select Case Page
                            Case "Reference"
                                SaveFileName = LabFileNamePrefix(y) & "RefPageCals_" & Format(DateTime.Now, "yyyyMMdd_HHmmss")
                            Case "Working"
                                SaveFileName = LabFileNamePrefix(y) & "WrkPageCals_" & Format(DateTime.Now, "yyyyMMdd_HHmmss")
                        End Select

                        If CDM.SetSourceDataSet(CompleteDatasetName) = True Then

                            'FinalPathToSaveData is a global that is set in SetupDataLogging...
                            'So, CSV file is saved into the same directory as the .mf4, etc. files...

                            If CDM.SetResultFilePath(FinalPathToSaveData) = True Then

                                If CDM.SetResultFilePrefix(SaveFileName) = True Then

                                    HandleUserMessageLogging("GMRC", "SaveCalSnapShot: Labfile =  " & LabFileNameArray(y))
                                    If CDM.LoadLabelListFromFile(LabFileNameArray(y)) = True Then

                                        If CDM.SetOutputFormat("CVX") = True Then

                                            If CDM.List = True Then
                                                'CopyToLog("SaveCalSnapShot: CDM.List success = True")
                                            Else
                                                HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.List returned False")
                                                SaveCalSnapShot = "CDM.List returned False"
                                                Exit Function
                                            End If

                                        Else
                                            HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.SetOutputFormat('CVX') returned False")
                                            SaveCalSnapShot = "CDM.SetOutputFormat('CVX') returned False"
                                            Exit Function
                                        End If

                                    Else
                                        HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.LoadLabelListFromFile(" & LabFileNameArray(y) & ") returned False")
                                        SaveCalSnapShot = "CDM.LoadLabelListFromFile(" & LabFileNameArray(y) & ") returned False"
                                        Exit Function
                                    End If
                                Else
                                    HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.SetResultFilePrefix(" & SaveFileName & ") returned False")
                                    SaveCalSnapShot = "CDM.SetResultFilePrefix(" & SaveFileName & ") returned False"
                                    Exit Function
                                End If

                            Else
                                HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.SetResultFilePath(" & FinalPathToSaveData & " returned false")
                                SaveCalSnapShot = "CDM.SetResultFilePath(" & FinalPathToSaveData & " returned false"
                                Exit Function
                            End If

                        Else
                            HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.SetSourceDataSet(" & CompleteDatasetName & " returned False")
                            SaveCalSnapShot = "CDM.SetSourceDataSet(" & CompleteDatasetName & " returned False"
                            Exit Function
                        End If

                    Else
                        HandleUserMessageLogging("GMRC", "SaveCalSnapShot: CDM.RemoveAllDestinations returned false")
                        SaveCalSnapShot = "CDM.RemoveAllDestinations returned false"
                        Exit Function
                    End If

                Next y

            Else
                HandleUserMessageLogging("GMRC", "SaveCalSnapShot: No Processors defined in Workspace...")
                SaveCalSnapShot = "No Processors defined in Workspace"
            End If

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "SaveCalSnapShot: - " & ex.Message)
            SaveCalSnapShot = "SaveCalSnapShot Failed - " & ex.Message

        End Try

    End Function
    Public Sub CloseINCA() Implements IGM_INCA_Comm.CloseINCA

        Try

            ActiveIncaApiCall = "CloseINCA"

            If myinca IsNot Nothing Then
                HandleUserMessageLogging("COMM", "Shutting down INCA...")
                RCI2_CleanUp()
                myinca.UnlockTool()
                If myinca.CloseTool() = True Then
                    HandleUserMessageLogging("COMM", "INCA Shut Down.")
                Else
                    HandleUserMessageLogging("COMM", "INCA Failed to Shut Down Properly.")
                End If

                myinca = Nothing
                MyHWC = Nothing
                Initialized = False

            Else
                HandleUserMessageLogging("COMM", "Ignoring INCA Shut Down request, INCA not running...")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GM_INCA_CommClass.CloseINCA: " & ex.Message, DisplayMsgBox)
        End Try

        ActiveIncaApiCall = String.Empty

    End Sub

    Public Sub RCI2_CleanUp() Implements IGM_INCA_Comm.RCI2_CleanUp

        'This routine is called by the server prior to termination.  Here we call reset records,
        'delete all measure elements and set the rci2 object to "nothing".  This effectively removes
        'all hooks into the rci2 interface and makes for a clean shutdown.

        Dim y As Short

        If rci2 IsNot Nothing Then

            If myDeviceRasterSignals IsNot Nothing Then

                rci2.IncaResetRecords()

                'Do not forget to unregister the measure elements!

                For y = 0 To UBound(myDeviceRasterSignals)
                    rci2.IncaDeleteMeasureElement(myDeviceRasterSignals(y).DeviceName, myDeviceRasterSignals(y).RasterName, myDeviceRasterSignals(y).SignalName)
                Next y

            End If

            myDeviceRasterSignals = Nothing
            rci2 = Nothing
            SetExperimentDispatch = False

        End If

    End Sub

    Public Function GetDeviceRasterPairForSignal(ByVal signalname As String) As IGM_INCA_Comm.DeviceRasterSignalStatus Implements IGM_INCA_Comm.GetDeviceRasterPairForSignal

        'Takes a string which represents the name of a valid signal (measure element) in INCA
        'and returns a structure which contains the following information

        'DeviceName
        'RasterName
        'SignalName
        'Status

        'This function is not currently implemented in the GmResidentClient

        Dim SignalValid As Boolean

        SignalValid = False

        If UBound(myDeviceRasterSignals) >= 0 Then
            If myDeviceRasterSignals(0).Status <> "Invalid" Then
                For x = 0 To UBound(myDeviceRasterSignals)
                    If myDeviceRasterSignals(x).SignalName = signalname And myDeviceRasterSignals(x).Status <> "Invalid" Then
                        myDeviceRasterSignal = myDeviceRasterSignals(x)
                        SignalValid = True
                        Exit For
                    End If
                Next
            End If
        End If

        If Not SignalValid Then
            myDeviceRasterSignal.DeviceName = "Undefined"
            myDeviceRasterSignal.RasterName = "Undefined"
            myDeviceRasterSignal.SignalName = signalname
            myDeviceRasterSignal.Status = "Invalid"

        End If

        GetDeviceRasterPairForSignal = myDeviceRasterSignal

    End Function
    Public Function GetINCAMeasureValue(ByVal devicename As String, ByVal rastername As String, ByVal signalname As String) As IGM_INCA_Comm.INCAMeasureValue Implements IGM_INCA_Comm.GetINCAMeasureValue

        'Takes a device name, raster name and signal name and returns its current value
        'and status as a INCAMeasureValue

        'This function is not currently used by the GmResidentClient

        Dim myMeasureValue As IGM_INCA_Comm.INCAMeasureValue
        Dim myRCI2Value As RCI2.A_MeasureValue

        myMeasureValue.myStatus = rci2.IncaGetMeasureValue(devicename, rastername, signalname, myRCI2Value)
        myMeasureValue.myValue = myRCI2Value.value
        GetINCAMeasureValue = myMeasureValue

    End Function

    Public Function GetDataArrayForSignal(ByVal signalname As String) As IGM_INCA_Comm.INCAData Implements IGM_INCA_Comm.GetDataArrayForSignal

        'Takes a signal name as an argument and determines which device / raster pair it belongs to, then
        'passes back all data (INCAData) from that device / raster pair.

        'INCAData type is as follows...

        'Public Class INCAData

        'Public myData() As Double
        'Public myTime() As Double
        'Public myStatus As Boolean

        'End Class

        'This function is not currently implemented in the GmResidentClient

        Dim myINCAData As IGM_INCA_Comm.INCAData
        Dim noOfRecords As Short = 1
        Dim SignalValid As Boolean

        SignalValid = False

        myINCAData = Nothing

        If myDeviceRasterSignals IsNot Nothing Then

            ReDim myINCAData.myData(UBound(myDeviceRasterSignals))
            ReDim myINCAData.myTime(UBound(myDeviceRasterSignals))

            If UBound(myDeviceRasterSignals) >= 0 Then
                If myDeviceRasterSignals(0).Status <> "Invalid" Then
                    For x = 0 To UBound(myDeviceRasterSignals)
                        If myDeviceRasterSignals(x).SignalName = signalname And myDeviceRasterSignals(x).Status <> "Invalid" Then
                            myINCAData.myStatus = rci2.IncaGetRecords(myDeviceRasterSignals(x).DeviceName, myDeviceRasterSignals(x).RasterName, noOfRecords, myINCAData.myTime, myINCAData.myData)
                            SignalValid = True
                            Exit For
                        End If
                    Next
                End If
            End If
        End If

        If SignalValid = False Then
            ReDim myINCAData.myData(0)
            ReDim myINCAData.myTime(0)
            myINCAData.myData(0) = 0
            myINCAData.myTime(0) = 0
            myINCAData.myStatus = "Invalid"
        End If

        GetDataArrayForSignal = myINCAData

    End Function

    Public Function GetDataForSignalList(ByVal signalist() As String) As IGM_INCA_Comm.INCAMeasureValue() Implements IGM_INCA_Comm.GetDataForSignalList

        'Takes a string array argument (a list of signal names) and passes back INCAMeasureValues
        'for each requested signal  INCAMeasureValue structure is defined below.

        'Public Class INCAMeasureValue

        'Public myValue As Double
        'Public myStatus As Boolean

        'End Class

        'This function is not currently used in the GmResidentClient

        Dim x As Short
        Dim y As Short
        Dim noOfRecords As Short = 1

        Dim myMeasureValues() As IGM_INCA_Comm.INCAMeasureValue ' = New INCAMeasureValue

        Dim myRCI2MeasureValue As RCI2.A_MeasureValue

        ReDim myMeasureValues(0)

        If myDeviceRasterSignals IsNot Nothing Then

            If UBound(myDeviceRasterSignals) >= 0 Then
                If myDeviceRasterSignals(0).Status <> "Invalid" Then
                    For y = 0 To UBound(signalist)
                        For x = 0 To UBound(myDeviceRasterSignals)
                            If myDeviceRasterSignals(x).SignalName = signalist(y) And myDeviceRasterSignals(x).Status <> "Invalid" Then

                                ReDim Preserve myMeasureValues(y)

                                'myMeasureValues(y) = New INCAMeasureValue
                                myMeasureValues(y).myStatus = rci2.IncaGetMeasureValue(myDeviceRasterSignals(x).DeviceName, myDeviceRasterSignals(x).RasterName, signalist(y), myRCI2MeasureValue)
                                myMeasureValues(y).myValue = myRCI2MeasureValue.value
                                Exit For
                            End If
                        Next
                    Next y

                End If
            End If
        End If

        GetDataForSignalList = myMeasureValues

    End Function
    Public Function GetDataHistoryForSignal(ByVal signalname As String, ByVal numberofrecords As UInteger) As IGM_INCA_Comm.INCAData Implements IGM_INCA_Comm.GetDataHistoryForSignal

        'This function is incomplete and not used at this time....

        Dim myINCAData As IGM_INCA_Comm.INCAData

        myINCAData = Nothing

        GetDataHistoryForSignal = myINCAData

    End Function
    Public Function GetDataHistoryForDeviceRasterPair(ByVal devicename As String, ByVal rastername As String, ByVal numberofrecords As UInteger) As IGM_INCA_Comm.INCAData Implements IGM_INCA_Comm.GetDataHistoryForDeviceRasterPair

        'This function is incomplete and not used at this time....

        Dim myINCAData As IGM_INCA_Comm.INCAData

        myINCAData = Nothing

        GetDataHistoryForDeviceRasterPair = myINCAData

    End Function

    Public Function SetRecordingFileName(ByVal FileName As String) As Boolean Implements IGM_INCA_Comm.SetRecordingFileName

        'This function takes a string which represents a filename and sets the recording file name
        'to this input string

        '⚠️ WARNING: SetRecordingFileName() OVERRIDES the template system completely!
        '   When using EnsureRecordingFileNameTemplateHasCounter() with autoincrement,
        '   DO NOT call SetRecordingFileName() as it will break the counter sequence.
        '   The template system should be used exclusively via SetupDataLogging().

        'This function is used by the GmResidentClient when setting up data logging to set the correct
        'recording file name.

        ActiveIncaApiCall = "SetRecordingFileName"

        SetRecordingFileName = myIncaOnlineExperiment.SetRecordingFileName(FileName)

        ActiveIncaApiCall = String.Empty
    End Function

    Public Function SetRecordingPathName(ByVal PathName As String) As Boolean Implements IGM_INCA_Comm.SetRecordingPathName

        'This function takes a string which represents a recording path name and sets the recording path name
        'to this input string

        'This function is used by the GmResidentClient when setting up data logging to set the correct
        'recording path name.

        ActiveIncaApiCall = "SetRecordingPathName"

        SetRecordingPathName = myIncaOnlineExperiment.SetRecordingPathName(PathName)

        ActiveIncaApiCall = String.Empty
    End Function

    Public Function SetRecordingFileFormat(ByVal FileFormat As String) As Boolean Implements IGM_INCA_Comm.SetRecordingFileFormat

        'Takes a string as an argument and sets the recording file format based on this string

        'This function is not currently implemented by the GmResidentClient

        SetRecordingFileFormat = myIncaOnlineExperiment.SetRecordingFileFormat(FileFormat)
    End Function

    Public Function GetRecordingFileFormat() As String Implements IGM_INCA_Comm.GetRecordingFileFormat

        'This function returns a string indicating the recording file format extension.

        'GmResidentClient uses this function when setting up data logging so that when the recording
        'is stopped, the correct recording file name is used with the correct file extension.

        ActiveIncaApiCall = "GetRecordingFileFormat"

        GetRecordingFileFormat = myIncaOnlineExperiment.GetPrimaryRecordingFormatFileExtension

        ActiveIncaApiCall = String.Empty
    End Function

    Public Function GetRecordingFileName() As String Implements IGM_INCA_Comm.GetRecordingFileName

        'Returns the current recording file name.

        'This function is used by the GmResidentClient when setting up data logging and when stopping
        'a WAV recording to make sure that the correct file names are used

        ActiveIncaApiCall = "GetRecordingFileName"

        GetRecordingFileName = myIncaOnlineExperiment.GetRecordingFileName

        ActiveIncaApiCall = String.Empty

    End Function

    Public Function GetRecordingFileNameTemplate() As String Implements IGM_INCA_Comm.GetRecordingFileNameTemplate

        'Returns the current recording file name.

        'This function is used by the GmResidentClient when setting up data logging and when stopping
        'a WAV recording to make sure that the correct file names are used

        ActiveIncaApiCall = "GetRecordingFileNameTemplate"

        GetRecordingFileNameTemplate = myIncaOnlineExperiment.GetRecordingFileNameTemplate

        ActiveIncaApiCall = String.Empty

    End Function

    Public Function GetRecordingPathName() As String Implements IGM_INCA_Comm.GetRecordingPathName

        'This function returns the current recording file path name

        'This function is used by the GmResidentClient for display purposes to allow
        'the user to verify that the path is correct for the record file.

        ActiveIncaApiCall = "GetRecordingPathName"

        GetRecordingPathName = myIncaOnlineExperiment.GetRecordingPathName

        ActiveIncaApiCall = String.Empty
    End Function

    Public Function GetLastINCAError() As String Implements IGM_INCA_Comm.GetLastINCAError

        'This function returns the last (or current) INCA error.  The string is a zero length string
        'if there is no Last INCA error found.

        'This function is used internally by the GetDataArray function.

        'This function is not currently used by the GmResidentClient

        Dim maxValue As Integer
        maxValue = 1024

        Dim returnString As New System.Text.StringBuilder(maxValue + 1)

        ActiveIncaApiCall = "GetLastINCAError"

        HandleUserMessageLogging("COMM", "GetLastINCAError Calling rci2.IncaGetLastError")
        rci2.IncaGetLastError(returnString, maxValue)
        HandleUserMessageLogging("COMM", "rci2.IncaGetLastError returned " & returnString.ToString)
        GetLastINCAError = returnString.ToString

        ActiveIncaApiCall = String.Empty

    End Function

    Public Function GetDataArray(ByVal DeviceName As String, ByVal RasterName As String, ByVal NumValidVars As Integer, ByRef noOfRecords As Integer) As IGM_INCA_Comm.INCAData Implements IGM_INCA_Comm.GetDataArray

        'Takes device name, raster name and number of valid signals for the device / raster pair as arguments
        'Uses rci2.IncaGetRecords to get the most current signal data for all signals registered which
        'are associated with the device / raster pair requested.

        'This function is used internally as well as exposed to the client.  Internally, the DataCollectionProcess
        'background task calls this function to get all of the data for all registered signals by 
        'device / raster pair.

        'This function can also be assessed by the client to do the same thing.  However, the current
        'client side implementation for aquiring signal data is to call GetSignalData which passes back
        'all signal data by signal rather than by device / raster pair.

        Dim myINCAData As IGM_INCA_Comm.INCAData

        Dim LastIncaError As String

        Try
            ' --- ADDED SAFEGUARD ---
            If rci2 Is Nothing Then
                ' rci2 is not initialized, likely during a re-initialization.
                ' Return Nothing to prevent a crash. The data collection loop will try again.
                HandleUserMessageLogging("COMM", "GetDataArray: rci2 is not initialized. Skipping data fetch.")
                Return Nothing
            End If
            ' --- END SAFEGUARD ---

            ReDim myINCAData.myData(noOfRecords * NumValidVars)
            ReDim myINCAData.myTime(noOfRecords)

            myINCAData.myStatus = rci2.IncaGetRecords(DeviceName, RasterName, noOfRecords, myINCAData.myTime, myINCAData.myData)
            'If Debug = True Then
            'CopyToCOMMLog("INCA Comm: noOfRecords = " & noOfRecords & " - " & DeviceName & " - " & RasterName)
            'End If

            'Do While noOfRecords = 0
            'CopyToCOMMLog(noOfRecords & "  DN - " & DeviceName & " RN - " & RasterName & " S - " & myINCAData.myStatus & " T - " & myINCAData.myTime(0) & " D - " & myINCAData.myData(0))
            'noOfRecords = 1
            'myINCAData.myStatus = rci2.IncaGetRecords(DeviceName, RasterName, noOfRecords, myINCAData.myTime, myINCAData.myData)
            'Loop

            'For x = 0 To UBound(myINCAData.myData)
            'CopyToCOMMLog(noOfRecords & " DN - " & DeviceName & " RN - " & RasterName & " S - " & myINCAData.myStatus & " T - " & myINCAData.myTime(x) & " D - " & myINCAData.myData(x))
            'Next x

            GetDataArray = myINCAData

        Catch ex As Exception

            HandleUserMessageLogging("COMM", "GetDataArray Calling GetLastINCAError")
            LastIncaError = GetLastINCAError()
            If Len(LastIncaError) > 0 Then
                HandleUserMessageLogging("COMM", "INCA Error = " & LastIncaError & " Exception = " & ex.Message)
            Else
                HandleUserMessageLogging("COMM", "RCI2 ERROR: Return Status = " & myINCAData.myStatus & " Exception = " & ex.Message)
            End If

            GetDataArray = Nothing

        End Try

    End Function

    Public Function GetRegisteredSignals() As IGM_INCA_Comm.DeviceRasterSignalStatus() Implements IGM_INCA_Comm.GetRegisteredSignals

        'This function returns a list of all registered signals in the structure which contains

        'DeviceName
        'RasterName
        'SignalName
        'Status

        'This function is not currently used by the GmResidentClient

        GetRegisteredSignals = myDeviceRasterSignals

    End Function

    Public Function RegisterSignals(
                                    ByVal DeviceRasterSignals() As IGM_INCA_Comm.DeviceRasterSignalStatus,
                                    Optional ByVal progressForm As SignalRegistrationProgressForm = Nothing
                                    ) As IGM_INCA_Comm.DeviceRasterSignalStatus() Implements IGM_INCA_Comm.RegisterSignals

        ' ✅ Performance tracking
        Dim stopwatch As New Stopwatch()
        stopwatch.Start()

        Dim successCount As Integer = 0
        Dim failureCount As Integer = 0
        Dim skippedCount As Integer = 0
        Dim firstDelayApplied As Boolean = False

        ' ✅ Cache mode check (Optimization #4)
        Dim isFullRegistration As Boolean = String.Equals(SignalRegistrationMode, "FULL", StringComparison.OrdinalIgnoreCase)
        Const PROGRESS_UPDATE_INTERVAL As Integer = 10 ' Update every 10 signals

        Try
            ' Initialize rci2 if needed
            If rci2 Is Nothing Then
                rci2 = New RCI2(Path.Combine(My.Application.Info.DirectoryPath, "incaRci2.dll"))
            End If

            ' ============================================================
            ' ✅ Check persistent registration cache for fast startup
            ' ============================================================
            ' IMPORTANT: If SignalRegistrationMode is "FULL", skip cache and force full registration.
            ' Cache is only used for "DISPLAYS" or "GO/NOGO" modes to speed up subsequent startups.
            If myDeviceRasterSignals Is Nothing AndAlso Not PlaybackMode AndAlso Not isFullRegistration Then
                HandleUserMessageLogging("COMM", $"RegisterSignals: Mode is '{SignalRegistrationMode}' - checking cache...")

                Dim cacheValid As Boolean = SignalRegistrationCache.IsCacheValid(INCAVariableFile, INCAExperiment)

                If cacheValid Then
                    HandleUserMessageLogging("COMM", "RegisterSignals: Cache file is valid, verifying INCA state...")

                    ' Load cached signal keys
                    Dim cachedKeys As HashSet(Of String) = SignalRegistrationCache.LoadCachedSignalKeys(INCAVariableFile)

                    If cachedKeys IsNot Nothing AndAlso cachedKeys.Count > 0 Then
                        ' Get what INCA currently has registered
                        Dim activeLabels As String() = GetCachedActiveMeasureLabels()

                        HandleUserMessageLogging("COMM", $"RegisterSignals: Cache has {cachedKeys.Count} keys, INCA has {If(activeLabels IsNot Nothing, activeLabels.Length, 0)} active labels")

                        If activeLabels IsNot Nothing AndAlso activeLabels.Length > 0 Then
                            ' ✅ SIMPLIFIED: Build a lookup of active signals from INCA
                            ' Key format: "DeviceName|SignalName" (no raster - INCA doesn't return it)
                            Dim activeSignalSet As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                            For Each label In activeLabels
                                ' INCA format: "SignalName\DeviceName"
                                Dim sepIdx As Integer = label.IndexOf("\"c)
                                If sepIdx >= 0 Then
                                    Dim signalName As String = label.Substring(0, sepIdx)
                                    Dim deviceName As String = label.Substring(sepIdx + 1)
                                    activeSignalSet.Add($"{deviceName}|{signalName}")
                                Else
                                    ' No separator - add as-is
                                    activeSignalSet.Add(label)
                                End If
                            Next

                            ' ✅ Check if all INCOMING signals are active in INCA
                            Dim allIncomingActive As Boolean = True
                            Dim missingCount As Integer = 0

                            For Each signal In DeviceRasterSignals
                                If String.IsNullOrWhiteSpace(signal.SignalName) Then Continue For

                                Dim lookupKey As String = $"{signal.DeviceName}|{signal.SignalName}"
                                If Not activeSignalSet.Contains(lookupKey) Then
                                    missingCount += 1
                                    If missingCount <= 5 Then
                                        HandleUserMessageLogging("COMM", $"RegisterSignals: Signal not active in INCA: {lookupKey}")
                                    End If
                                    allIncomingActive = False
                                End If
                            Next

                            HandleUserMessageLogging("COMM", $"RegisterSignals: {missingCount} signals not active in INCA (of {DeviceRasterSignals.Length} incoming)")

                            If allIncomingActive Then
                                ' ✅ CACHE HIT! All incoming signals are already registered in INCA
                                stopwatch.Stop()
                                HandleUserMessageLogging("COMM", $"RegisterSignals: ✅ CACHE HIT - All {DeviceRasterSignals.Length} signals already registered in INCA ({stopwatch.ElapsedMilliseconds}ms)")

                                ' ✅ Update progress form to show cached registration mode
                                progressForm?.ShowCachedRegistrationMode(DeviceRasterSignals.Length)

                                ' Setup INCA experiment dispatch (needed for data collection)
                                If SetExperimentDispatch = False Then
                                    SetExperimentDispatch = rci2.IncaSetExperimentDispatch(myExpEnvView)
                                End If

                                ' ✅ CRITICAL FIX: Must call IncaAddMeasureElement to subscribe signals for data collection
                                ' Cache validation confirmed signals EXIST in INCA, but they may not be in the measurement group
                                HandleUserMessageLogging("COMM", $"RegisterSignals (Cache): Adding {DeviceRasterSignals.Length} signals to measurement group...")
                                Dim cachedResultList As New List(Of IGM_INCA_Comm.DeviceRasterSignalStatus)(DeviceRasterSignals.Length)
                                Dim subscribeCount As Integer = 0
                                Dim totalToSubscribe As Integer = DeviceRasterSignals.Length

                                For Each signal In DeviceRasterSignals
                                    If String.IsNullOrWhiteSpace(signal.SignalName) Then Continue For

                                    Dim validSignal = signal
                                    Try
                                        ' Add signal to measurement group (fast - no validation needed since we verified it exists)
                                        rci2.IncaAddMeasureElement(signal.DeviceName, signal.RasterName, signal.SignalName, RCI2.A_MeasureDisplayMode.A_MEASURE_NO_DISPLAY)
                                        validSignal.Status = "Valid"
                                        validSignal.ForceRegister = True
                                        subscribeCount += 1
                                    Catch ex As Exception
                                        ' Signal may already be in measurement group, still mark as valid
                                        validSignal.Status = "Valid"
                                        validSignal.ForceRegister = True
                                        subscribeCount += 1
                                    End Try
                                    cachedResultList.Add(validSignal)

                                    ' ✅ Update progress form every 50 signals
                                    If subscribeCount Mod 50 = 0 Then
                                        progressForm?.UpdateCachedProgress(subscribeCount, totalToSubscribe)
                                    End If
                                Next

                                ' Final progress update
                                progressForm?.UpdateCachedProgress(subscribeCount, totalToSubscribe)

                                HandleUserMessageLogging("COMM", $"RegisterSignals (Cache): Subscribed {subscribeCount} signals to measurement group")

                                myDeviceRasterSignals = cachedResultList.ToArray()
                                HandleUserMessageLogging("COMM", $"RegisterSignals: Built {myDeviceRasterSignals.Length} valid signals from cache")

                                ' ✅ Setup device-raster pairs EXACTLY like normal registration
                                ResetRecords()
                                ReDim mySignalData(myDeviceRasterSignals.Length - 1)
                                ReDim mySignalDataWithTime(myDeviceRasterSignals.Length - 1)

                                For i As Integer = 0 To myDeviceRasterSignals.Length - 1
                                    If myDeviceRasterSignals(i).Status = "Valid" AndAlso myDeviceRasterSignals(i).ForceRegister Then
                                        myDeviceRasterSignals(i).DeviceRasterPairNum = HandleDeviceRasterPairs(
                                            myDeviceRasterSignals(i).DeviceName,
                                            myDeviceRasterSignals(i).RasterName)
                                        myDeviceRasterSignals(i).DeviceRasterPairVarNum = myDeviceRasterPair(myDeviceRasterSignals(i).DeviceRasterPairNum).NumValidVars - 1
                                    End If
                                Next

                                ' Log device-raster pair summary (same as normal registration)
                                If myDeviceRasterPair IsNot Nothing Then
                                    For i As Integer = 0 To UBound(myDeviceRasterPair)
                                        HandleUserMessageLogging("COMM", $"RegisterSignals (Cache): DeviceRasterPair {i} - {myDeviceRasterPair(i).NumValidVars} vars - {myDeviceRasterPair(i).DeviceName}/{myDeviceRasterPair(i).RasterName}")
                                    Next
                                Else
                                    HandleUserMessageLogging("COMM", "RegisterSignals (Cache): WARNING - myDeviceRasterPair is Nothing!")
                                End If

                                Return myDeviceRasterSignals
                            Else
                                HandleUserMessageLogging("COMM", $"RegisterSignals: Cache miss - {missingCount} signals not active in INCA, proceeding with registration")
                            End If
                        End If
                    End If
                End If
            ElseIf isFullRegistration Then
                ' ✅ Explicit FULL mode requested - skip cache entirely
                HandleUserMessageLogging("COMM", $"RegisterSignals: Mode is 'FULL' - bypassing cache, forcing complete registration")
                ' Optionally invalidate the cache when FULL mode is used
                ' SignalRegistrationCache.InvalidateCache(INCAVariableFile)
            End If

            ' ============================================================
            ' STEP 1: Determine signals to process
            ' ============================================================
            Dim signalsToProcessArray() As IGM_INCA_Comm.DeviceRasterSignalStatus

            If myDeviceRasterSignals Is Nothing OrElse isFullRegistration Then
                ' Fresh start - process all incoming signals
                signalsToProcessArray = DeviceRasterSignals
                HandleUserMessageLogging("COMM", $"RegisterSignals: Processing {signalsToProcessArray.Length} signals (FULL mode)")
            Else
                ' ✅ Optimized existing signal handling (Optimization #5)
                Dim existingMap As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)

                ' Build lookup map of existing signals
                For i As Integer = 0 To myDeviceRasterSignals.Length - 1
                    Dim key As String = $"{myDeviceRasterSignals(i).DeviceName}|{myDeviceRasterSignals(i).RasterName}|{myDeviceRasterSignals(i).SignalName}"
                    If Not existingMap.ContainsKey(key) Then
                        existingMap.Add(key, i)
                    End If
                Next

                ' Start with copy of existing signals
                Dim tempArray() As IGM_INCA_Comm.DeviceRasterSignalStatus = CType(myDeviceRasterSignals.Clone(), IGM_INCA_Comm.DeviceRasterSignalStatus())
                Dim addedCount As Integer = 0

                ' Process incoming signals
                For Each incomingSignal In DeviceRasterSignals
                    Dim key As String = $"{incomingSignal.DeviceName}|{incomingSignal.RasterName}|{incomingSignal.SignalName}"

                    If existingMap.ContainsKey(key) Then
                        Dim idx As Integer = existingMap(key)

                        ' Update ForceRegister if changed from False to True
                        If incomingSignal.ForceRegister AndAlso Not tempArray(idx).ForceRegister Then
                            tempArray(idx).Status = String.Empty
                            tempArray(idx).ForceRegister = True

                            ' Remove from INCA to re-register
                            If PlaybackMode = False Then
                                rci2.IncaDeleteMeasureElement(tempArray(idx).DeviceName, tempArray(idx).RasterName, tempArray(idx).SignalName)
                            End If
                        Else
                            ' No changes - keep existing signal as-is
                            tempArray(idx).ForceRegister = False
                        End If
                    Else
                        ' New signal - add to array
                        ReDim Preserve tempArray(tempArray.Length)
                        tempArray(tempArray.Length - 1) = incomingSignal
                        addedCount += 1
                    End If
                Next

                signalsToProcessArray = tempArray
                HandleUserMessageLogging("COMM", $"RegisterSignals: Added {addedCount} new signals to existing {myDeviceRasterSignals.Length}")

                ' ============================================================
                ' ✅ EARLY EXIT: Skip all processing if nothing changed
                ' ============================================================
                If addedCount = 0 Then
                    ' Check if all existing signals are already valid
                    Dim allValid As Boolean = True
                    Dim needsReRegister As Boolean = False

                    For i As Integer = 0 To myDeviceRasterSignals.Length - 1
                        If myDeviceRasterSignals(i).Status <> "Valid" Then
                            allValid = False
                            Exit For
                        End If
                        If myDeviceRasterSignals(i).ForceRegister AndAlso String.IsNullOrEmpty(myDeviceRasterSignals(i).Status) Then
                            needsReRegister = True
                            Exit For
                        End If
                    Next

                    If allValid AndAlso Not needsReRegister Then
                        stopwatch.Stop()
                        HandleUserMessageLogging("COMM", $"RegisterSignals: ✅ EARLY EXIT - All {myDeviceRasterSignals.Length} signals already registered in INCA ({stopwatch.ElapsedMilliseconds}ms)")
                        progressForm?.UpdateProgress(myDeviceRasterSignals.Length, 0, 0)
                        Return myDeviceRasterSignals
                    End If
                End If
            End If

            ' ============================================================
            ' STEP 2: Setup INCA experiment dispatch
            ' ============================================================
            If SetExperimentDispatch = False AndAlso PlaybackMode = False Then
                SetExperimentDispatch = rci2.IncaSetExperimentDispatch(myExpEnvView)

                If Not SetExperimentDispatch Then
                    HandleUserMessageLogging("COMM", "RegisterSignals: SetExperimentDispatch FAILED")
                    myDeviceRasterSignals = Nothing
                    Initialized = False
                    Return Nothing
                End If
            End If

            HandleUserMessageLogging("COMM", "RegisterSignals: Experiment Dispatch Set")

            ' ============================================================
            ' STEP 3: Build active labels cache (FULL mode only)
            ' ============================================================
            Dim activeLabelsMap As Dictionary(Of String, String) = Nothing

            If isFullRegistration Then
                ' ✅ Use cached active labels (Optimization #1)
                Dim activeLabels As String() = GetCachedActiveMeasureLabels()

                If activeLabels IsNot Nothing Then
                    activeLabelsMap = New Dictionary(Of String, String)(activeLabels.Length, StringComparer.OrdinalIgnoreCase)

                    For Each label In activeLabels
                        Dim sepIdx As Integer = label.IndexOf("\"c)
                        Dim signalName As String = If(sepIdx >= 0, label.Substring(0, sepIdx), label)
                        Dim deviceName As String = If(sepIdx >= 0, label.Substring(sepIdx + 1), String.Empty)

                        Dim key As String = $"{signalName}|{deviceName}"
                        If Not activeLabelsMap.ContainsKey(key) Then
                            activeLabelsMap.Add(key, label)
                        End If
                    Next

                    HandleUserMessageLogging("COMM", $"RegisterSignals: Cached {activeLabels.Length} active labels")
                End If
            End If

            ' ============================================================
            ' STEP 4: Reset data collection structures
            ' ============================================================
            ResetDataCollectionProcessVars = True
            myDeviceRasterPair = Nothing

            Dim resultList As New List(Of IGM_INCA_Comm.DeviceRasterSignalStatus)(signalsToProcessArray.Length)
            Dim processedCount As Integer = 0

            ' ✅ Reduced UI update frequency (Optimization #3)
            'Const UI_UPDATE_INTERVAL As Integer = 50

            ' ============================================================
            ' STEP 5: Process each signal
            ' ============================================================
            For signalIndex As Integer = 0 To UBound(signalsToProcessArray)
                Dim signal As IGM_INCA_Comm.DeviceRasterSignalStatus = signalsToProcessArray(signalIndex)
                Dim shouldRegister As Boolean = True

                ' Skip blank signal names
                If String.IsNullOrWhiteSpace(signal.SignalName) Then
                    shouldRegister = False
                End If

                ' ✅ Optimized skip check (Optimization #2)
                If shouldRegister AndAlso Not signal.ForceRegister AndAlso isFullRegistration AndAlso activeLabelsMap IsNot Nothing Then
                    Dim key As String = $"{signal.SignalName}|{signal.DeviceName}"
                    Dim altKey1 As String = $"{signal.SignalName}|ACP"
                    Dim altKey2 As String = $"{signal.SignalName}|XCP:1"

                    ' ✅ Use TryGetValue instead of ContainsKey (Optimization #2)
                    Dim dummy As String = Nothing
                    If activeLabelsMap.TryGetValue(key, dummy) OrElse
                   (signal.DeviceName = "XCP:1" AndAlso activeLabelsMap.TryGetValue(altKey1, dummy)) OrElse
                   (signal.DeviceName = "ACP" AndAlso activeLabelsMap.TryGetValue(altKey2, dummy)) Then
                        skippedCount += 1
                        shouldRegister = False
                    End If
                ElseIf Not signal.ForceRegister Then
                    shouldRegister = False
                End If

                ' Register signal if needed
                If signal.Status <> "Valid" AndAlso shouldRegister Then
                    If PlaybackMode = False Then
                        Dim measureElement As Object = rci2.IncaAddMeasureElement(
                        signal.DeviceName,
                        signal.RasterName,
                        signal.SignalName,
                        RCI2.A_MeasureDisplayMode.A_MEASURE_NO_DISPLAY)

                        If measureElement IsNot Nothing Then
                            successCount += 1
                            signal.Status = "Valid"
                        Else
                            ' Get error message from INCA
                            failureCount += 1
                            Dim errorMsg As New System.Text.StringBuilder(1024)
                            rci2.IncaGetLastError(errorMsg, 1024)
                            signal.Status = errorMsg.ToString()

                            ' Handle critical errors
                            If signal.Status.Contains("Can't create view on file mapping") Then
                                If Not firstDelayApplied Then
                                    firstDelayApplied = True
                                    Thread.Sleep(DelayForFirstInvalidTime)

                                    If signal.Status.ToUpper().Contains("WINDOWS ERROR") Then
                                        HandleUserMessageLogging("COMM", "RegisterSignals: Access denied error - exiting")
                                        HandleUserMessageLogging("GMRC", "RegisterSignals: Signal Registration FAILED. Suggest running CLEVIR as administrator due to IT permissions issues.", DisplayMsgBox)
                                        myDeviceRasterSignals = Nothing
                                        Initialized = False
                                        Return Nothing
                                    End If
                                End If

                                HandleUserMessageLogging("COMM", $"RegisterSignals: INCA Memory Error after {successCount} signals")
                                HandleUserMessageLogging("GMRC", $"RegisterSignals: INCA Memory Error after {successCount} signals. This is a known INCA issue when creating a new experiment. Workaround: Exit CLEVIR, restart, and perform FULL signal registration.", DisplayMsgBox)
                                myDeviceRasterSignals = Nothing
                                Initialized = False
                                Return Nothing
                            Else
                                ' Format error summary
                                Dim errorSummary As String
                                If signal.Status.Contains("is full") Then
                                    errorSummary = "RASTER FULL"
                                ElseIf signal.Status.Contains("check the spelling") Then
                                    errorSummary = "SIGNAL NAME DOES NOT EXIST"
                                ElseIf signal.Status.Contains("does not exist in device") Then
                                    errorSummary = "RASTER NAME DOES NOT EXIST"
                                Else
                                    errorSummary = signal.Status
                                End If

                                HandleUserMessageLogging("COMM", $"RegisterSignals: INVALID - {signal.SignalName},{signal.DeviceName},{signal.RasterName},{errorSummary}")
                            End If
                        End If
                    Else
                        ' Playback mode - mark as valid
                        successCount += 1
                        signal.Status = "Valid"
                    End If
                ElseIf signal.Status <> "Valid" Then
                    signal.Status = "UnReg"
                Else
                    successCount += 1
                End If

                ' Add to result list
                If signal.Status = "Valid" OrElse signal.ForceRegister Then
                    resultList.Add(signal)
                End If

                processedCount += 1

                ' ✅ Batched UI updates (Optimization #3)
                If processedCount Mod PROGRESS_UPDATE_INTERVAL = 0 AndAlso progressForm IsNot Nothing Then
                    progressForm.UpdateProgress(successCount, failureCount, skippedCount)
                End If
            Next

            progressForm?.UpdateProgress(successCount, failureCount, skippedCount)

            ' ============================================================
            ' STEP 6: Finalize registration
            ' ============================================================
            myDeviceRasterSignals = resultList.ToArray()

            ' ✅ Performance logging
            stopwatch.Stop()
            HandleUserMessageLogging("COMM", $"RegisterSignals: Complete in {stopwatch.ElapsedMilliseconds}ms - Success: {successCount}, Failed: {failureCount}, Skipped: {skippedCount}")

            ' Setup device-raster pairs for data collection
            ResetRecords()
            ReDim mySignalData(myDeviceRasterSignals.Length - 1)
            ReDim mySignalDataWithTime(myDeviceRasterSignals.Length - 1)

            For i As Integer = 0 To myDeviceRasterSignals.Length - 1
                If myDeviceRasterSignals(i).Status = "Valid" AndAlso myDeviceRasterSignals(i).ForceRegister Then
                    myDeviceRasterSignals(i).DeviceRasterPairNum = HandleDeviceRasterPairs(
                    myDeviceRasterSignals(i).DeviceName,
                    myDeviceRasterSignals(i).RasterName)

                    myDeviceRasterSignals(i).DeviceRasterPairVarNum = myDeviceRasterPair(myDeviceRasterSignals(i).DeviceRasterPairNum).NumValidVars - 1
                End If
            Next

            ' Log device-raster pair summary
            For i As Integer = 0 To UBound(myDeviceRasterPair)
                HandleUserMessageLogging("COMM", $"RegisterSignals: DeviceRasterPair {i} - {myDeviceRasterPair(i).NumValidVars} vars - {myDeviceRasterPair(i).DeviceName}/{myDeviceRasterPair(i).RasterName}")
            Next

            ' ✅ Save successful registration to persistent cache
            If Not PlaybackMode AndAlso myDeviceRasterSignals IsNot Nothing AndAlso myDeviceRasterSignals.Length > 0 Then
                Try
                    SignalRegistrationCache.SaveCache(INCAVariableFile, INCAExperiment, myDeviceRasterSignals)
                Catch cacheEx As Exception
                    HandleUserMessageLogging("COMM", $"RegisterSignals: Failed to save cache: {cacheEx.Message}")
                End Try
            End If

            Return myDeviceRasterSignals

        Catch ex As Exception
            HandleUserMessageLogging("COMM", $"RegisterSignals: Exception - {ex.Message}")
            HandleUserMessageLogging("GMRC", $"RegisterSignals: Signal Registration FAILED - {ex.Message}", DisplayMsgBox)
            myDeviceRasterSignals = Nothing
            Initialized = False
            Return Nothing
        End Try
    End Function


    ''' <summary>
    ''' Main entry point to connect to INCA. 
    ''' Called from Connect..., tries to either launch INCA if it is not running 
    ''' or connect to an existing instance otherwise.
    ''' </summary>
    Public Function ConnectToInca() As String Implements IGM_INCA_Comm.ConnectToInca
        Dim result As String

        Try
            If Not IsProcessRunning("INCA") Then
                result = LaunchAndInitializeInca()
            Else
                result = ConnectToRunningInca()
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ERROR in ConnectToInca: " & ex.Message)
            result = ex.Message
        End Try

        Return result
    End Function

    ''' <summary>
    ''' Launches a new instance of INCA and initializes it.
    ''' </summary>
    Private Function LaunchAndInitializeInca() As String
        ' If there's an existing reference to INCA, close it before launching a new one
        'CheckForAndInstallINCAVideoAddon()
        If myinca Is Nothing Then
            HandleUserMessageLogging("GMRC", "ConnectToInca: Launching INCA...")
        Else
            myinca.CloseTool()
            myinca = Nothing
            MyHWC = Nothing
            Initialized = False
            HandleUserMessageLogging("GMRC", "ConnectToInca: Closing existing tool and launching INCA...")
        End If

        ' Create a new instance of INCA
        myinca = New Inca
        CurrentINCAVersion = myinca.GetCurrentVersion

        If Not VerifyCorrectIncaVersion(CurrentINCAVersion) Then
            myinca.UnlockTool()
            myinca.CloseTool()
            InitForm.Close()
            End ' Terminates the application
        End If

        'myinca.LockTool() ' If locking is required
        InitForm.Show()
        InitForm.BringToFront()
        InitForm.Refresh()

        Dim dbResult As String = HandleDatabase()

        If dbResult = "True" Then
            HandleUserMessageLogging("GMRC", "ConnectToInca: Database " & INCADatabase & " Selected...")
            Return "True"
        Else
            HandleUserMessageLogging("GMRC", "ConnectToInca: INCA NOT Running, HandleDatabase returned " & dbResult)
            Return "INCA NOT Running, HandleDatabase returned " & dbResult
        End If

    End Function

    ''' <summary>
    ''' Connects to an already running instance of INCA, 
    ''' either using an existing myinca reference or by creating a new one.
    ''' </summary>
    Private Function ConnectToRunningInca() As String
        HandleUserMessageLogging("GMRC", "ConnectToInca: INCA Already Running")

        If myinca IsNot Nothing Then
            ' We already have a reference to an INCA instance
            CurrentINCAVersion = myinca.GetCurrentVersion

            If Not VerifyCorrectIncaVersion(CurrentINCAVersion) Then
                myinca.UnlockTool()
                myinca.CloseTool()
                InitForm.Close()
                End
            End If

            HandleUserMessageLogging("GMRC", "ConnectToInca: Using Existing INCA Connection...")

            Dim dbResult As String = HandleDatabase()
            If dbResult = "True" Then
                HandleUserMessageLogging("GMRC", "ConnectToInca: Database " & INCADatabase & " Selected...")
                Return "True"
            Else
                HandleUserMessageLogging("GMRC", "ConnectToInca: INCA Running - myInca was valid, HandleDatabase returned " & dbResult)
                Return "INCA Running - myInca was valid, HandleDatabase returned " & dbResult
            End If

        Else
            ' Process is running, but our reference is Nothing. Create a new one.
            HandleUserMessageLogging("GMRC", "ConnectToInca: Launching / Connecting to INCA...")

            myinca = New Inca
            CurrentINCAVersion = myinca.GetCurrentVersion

            If Not VerifyCorrectIncaVersion(CurrentINCAVersion) Then
                myinca.UnlockTool()
                myinca.CloseTool()
                InitForm.Close()
                End
            End If

            'CheckForAndInstallINCAVideoAddon()

            ' myinca.LockTool() ' If locking is required

            Dim dbResult As String = HandleDatabase()
            If dbResult = "True" Then
                HandleUserMessageLogging("GMRC", "ConnectToInca: Database " & INCADatabase & " Selected...")
                Return "True"
            Else
                HandleUserMessageLogging("GMRC", "ConnectToInca: INCA Running - myInca was Nothing, HandleDatabase returned " & dbResult)
                Return "INCA Running - myInca was Nothing, HandleDatabase returned " & dbResult
            End If
        End If
    End Function

    ''' <summary>
    ''' Example stub for handling the database connection within INCA.
    ''' Returns "True" or an error string.
    ''' </summary>
    Private Function HandleDatabase() As String
        Const INCA_GET_DATABASE_WAIT_TIME As Integer = 45 ' in seconds
        Dim result As String = "False"

        ' Add logging to debug
        HandleUserMessageLogging("GMRC", "HandleDatabase: Starting, INCADatabase = '" & INCADatabase & "'")

        ' First, ensure myinca is properly initialized
        If myinca Is Nothing Then
            HandleUserMessageLogging("GMRC", "HandleDatabase: ERROR - myinca is Nothing!")
            Return "myinca reference is not initialized"
        End If

        ' Try to get the current database
        Try
            myActualDatabase = myinca.GetCurrentDataBase()

            If myActualDatabase IsNot Nothing Then
                HandleUserMessageLogging("GMRC", "HandleDatabase: Got database immediately: " & myActualDatabase.GetName())
            Else
                HandleUserMessageLogging("GMRC", "HandleDatabase: No database currently open, will wait...")
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleDatabase: Exception getting current database: " & ex.Message)
        End Try

        ' If no DB is currently open, wait and retry
        If myActualDatabase Is Nothing Then
            Dim myStopWatch = Stopwatch.StartNew()
            Dim attemptCount As Integer = 0

            Do While myStopWatch.Elapsed.TotalSeconds < INCA_GET_DATABASE_WAIT_TIME
                Threading.Thread.Sleep(1000)
                attemptCount += 1

                Try
                    myActualDatabase = myinca.GetCurrentDataBase()

                    If myActualDatabase IsNot Nothing Then
                        HandleUserMessageLogging("GMRC", "HandleDatabase: Got database on attempt " & attemptCount & ": " & myActualDatabase.GetName())
                        Exit Do
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", "HandleDatabase: Attempt " & attemptCount & " failed: " & ex.Message)
                End Try
            Loop

            myStopWatch.Stop()
        End If

        ' Now we either have a DB or we don't
        If myActualDatabase IsNot Nothing Then
            ' Get the current database name
            Dim currentDatabaseName As String = myActualDatabase.GetName()
            HandleUserMessageLogging("GMRC", "HandleDatabase: Current DB name = '" & currentDatabaseName & "'")

            ' If INCADatabase is empty, use the current database
            If String.IsNullOrEmpty(INCADatabase) Then
                HandleUserMessageLogging("GMRC", "HandleDatabase: No database specified, using current database: " & currentDatabaseName)
                INCADatabase = currentDatabaseName  ' Set it to the current database
                SetupClevirDatabase(myActualDatabase)
                result = "True"
            Else
                ' Compare database names
                HandleUserMessageLogging("GMRC", "HandleDatabase: Requested DB name = '" & INCADatabase & "'")

                ' Fix the comparison - GetName() might return a full path, INCADatabase might be just the name
                Dim dbNameOnly As String = System.IO.Path.GetFileName(currentDatabaseName)
                Dim requestedNameOnly As String = System.IO.Path.GetFileName(INCADatabase)

                HandleUserMessageLogging("GMRC", "HandleDatabase: Comparing '" & dbNameOnly & "' with '" & requestedNameOnly & "'")

                If Not String.Equals(dbNameOnly, requestedNameOnly, StringComparison.OrdinalIgnoreCase) Then
                    ' If different, close the current DB and open the one we want
                    HandleUserMessageLogging("GMRC", "HandleDatabase: Database mismatch, closing current and opening requested...")

                    Try
                        myinca.CloseDatabase()
                        Threading.Thread.Sleep(500) ' Give INCA time to close

                        myActualDatabase = myinca.OpenDataBase(INCADatabase)

                        If myActualDatabase IsNot Nothing Then
                            HandleUserMessageLogging("GMRC", "HandleDatabase: Successfully opened " & myActualDatabase.GetName())
                            SetupClevirDatabase(myActualDatabase)
                            result = "True"
                        Else
                            HandleUserMessageLogging("GMRC", "HandleDatabase: Failed to open requested database")
                            result = "Requested Database not found"
                        End If
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", "HandleDatabase: Exception switching databases: " & ex.Message)
                        result = "Failed to switch database: " & ex.Message
                    End Try
                Else
                    ' Database already matches
                    HandleUserMessageLogging("GMRC", "HandleDatabase: Database already matches")
                    SetupClevirDatabase(myActualDatabase)
                    result = "True"
                End If
            End If
        Else
            ' No current DB after waiting
            If String.IsNullOrEmpty(INCADatabase) Then
                ' No database specified and none is open - this is an error
                HandleUserMessageLogging("GMRC", "HandleDatabase: No database specified and none is currently open")
                result = "No database specified and none is currently open in INCA"
            Else
                ' Try opening the requested DB directly
                HandleUserMessageLogging("GMRC", "HandleDatabase: No database open after waiting, attempting to open " & INCADatabase)

                Try
                    myActualDatabase = myinca.OpenDataBase(INCADatabase)

                    If myActualDatabase IsNot Nothing Then
                        HandleUserMessageLogging("GMRC", "HandleDatabase: Successfully opened " & myActualDatabase.GetName())
                        SetupClevirDatabase(myActualDatabase)
                        result = "True"
                    Else
                        HandleUserMessageLogging("GMRC", "HandleDatabase: Failed to open database")
                        result = "Requested Database not found, may be due to a previous INCA session " &
                             "abnormally shutting down or INCA being unable to obtain a valid License. " &
                             "Launch INCA from the Desktop ICON. If INCA launches normally, launch CLEVIR to continue..."
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", "HandleDatabase: Exception opening database: " & ex.Message)
                    result = "Failed to open database: " & ex.Message
                End Try
            End If
        End If

        Return result
    End Function

    Private Function Connect(ByVal database As String, ByVal workspace As String, ByVal experiment As String, ByVal EtasUserName As String, ByVal ForceInit As Boolean, ByVal RegisterIntoNewBlankExp As Boolean) As String

        'Called from INITInca.  Checks if INCA is already running and if we are already initialized, then
        'directs things accordingly.

        'If we need to establish a connection with INCA, we will do so using the ConnectToInca function.
        'Once connected, we will handle the workspace selection, etc., using the HandleWorkspace function.

        Dim returnstring As String
        Dim HWreturnstring As String
        Dim RequiredINCAVersion As String = ""

        Connect = "True"
        INCADatabase = database
        INCAWorkspace = workspace
        'If Len(experiment) > 0 Then
        INCAExperiment = experiment
        'End If

        'Initialized will equal false if we are starting up for the first time.  If we are switching users
        'we will pass ForceInit = True so we will re-initialize if already initialized...

        INCARunning = IsProcessRunning("INCA")

        If Initialized = True And ForceInit = False And INCARunning = True Then
            Connect = "Already Initialized"
            HandleUserMessageLogging("GMRC", "Connect: Already Initialized....")
        Else
            HandleUserMessageLogging("GMRC", "Connect: Initializing (Initialized = " & Initialized & " ForceInit = " & ForceInit & " INCARunning = " & INCARunning & ")")
            returnstring = MyIncaInterface.ConnectToInca()

            If returnstring = "True" And Len(INCAExperiment) > 0 Then

                HWreturnstring = HandleWorkspace(EtasUserName, RegisterIntoNewBlankExp)

                Initialized = True
                HandleUserMessageLogging("GMRC", "Connect: Initialized set to True")

                If HWreturnstring = "True" Then
                    HandleUserMessageLogging("GMRC", "Connect: HWreturnstring returned True")
                    If ForceInit = True Then
                        Connect = "Re-Initialized"
                    End If
                ElseIf InStr(HWreturnstring, "WARNING") > 0 Then
                    Connect = HWreturnstring
                    HandleUserMessageLogging("GMRC", "Connect: HandleWorkspace Returned " & HWreturnstring)
                Else
                    Connect = "HandleWorkspace Returned " & HWreturnstring
                    HandleUserMessageLogging("GMRC", "Connect: HandleWorkspace Returned " & HWreturnstring)
                End If
            Else
                Connect = returnstring
            End If

        End If

    End Function

    Public Function InitINCA(ByVal database As String, ByVal workspace As String,
                 ByVal experiment As String, ByVal EtasUserName As String,
                 ByVal ForceInit As Boolean, ByRef ErrorMsg As String,
                 ByVal RegisterIntoNewBlankExp As Boolean) As IGM_INCA_Comm.INIT_STATUS _
Implements IGM_INCA_Comm.InitINCA

        Try
            HandleUserMessageLogging("COMM", "InitINCA: InitINCA Called")

            ' ✅ Declare and validate DLL path
            Dim dllPath As String = Path.Combine(My.Application.Info.DirectoryPath, "incaRci2.dll")

            If Not File.Exists(dllPath) Then
                ErrorMsg = "incaRci2.dll not found at: " & dllPath
                HandleUserMessageLogging("COMM", "ERROR: " & ErrorMsg)
                Return IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL
            End If

            ' ✅ Create RCI2 instance (dependencies now resolved via PATH)
            HandleUserMessageLogging("COMM", "Creating RCI2 instance...")
            rci2 = New RCI2(dllPath)
            HandleUserMessageLogging("COMM", "RCI2 instance created successfully")

            ' ✅ Proceed with INCA connection
            Dim returnstring As String = Connect(database, workspace, experiment,
                                     EtasUserName, ForceInit, RegisterIntoNewBlankExp)

            Select Case returnstring
                Case "True"
                    HandleUserMessageLogging("COMM", "InitINCA: Initialized = True")
                    InitINCA = IGM_INCA_Comm.INIT_STATUS.INIT_SUCCESSFUL
                    Initialized = True
                Case "Already Initialized"
                    HandleUserMessageLogging("COMM", "InitINCA: " & returnstring)
                    InitINCA = IGM_INCA_Comm.INIT_STATUS.ALREADY_INITIALIZED
                    Initialized = True
                Case "Re-Initialized"
                    HandleUserMessageLogging("COMM", "InitINCA: " & returnstring)
                    InitINCA = IGM_INCA_Comm.INIT_STATUS.REINITIALIZED
                    Initialized = True
                Case Else
                    If InStr(returnstring, "WARNING") > 0 Then
                        HandleUserMessageLogging("COMM", "InitINCA: " & returnstring & " Initialized set to True")
                        ErrorMsg = returnstring
                        InitINCA = IGM_INCA_Comm.INIT_STATUS.INIT_SUCCESSFUL
                        Initialized = True
                    Else
                        HandleUserMessageLogging("COMM", "InitINCA: " & returnstring & " Initialized set to False")
                        ErrorMsg = returnstring
                        InitINCA = IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL
                        Initialized = False
                    End If
            End Select

        Catch ex As Exception
            ErrorMsg = "InitINCA Exception: " & ex.Message & vbCrLf & ex.StackTrace
            HandleUserMessageLogging("COMM", ErrorMsg)
            Return IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL
        End Try

        Return InitINCA
    End Function

    Private Function EnsureRecordingFileNameTemplateHasCounter(Optional preferredBaseName As String = Nothing) As Boolean
        Try
            If myIncaOnlineExperiment Is Nothing Then
                HandleUserMessageLogging("COMM", "EnsureRecordingFileNameTemplateHasCounter: Online experiment not available")
                Return False
            End If

            ' ✅ SIMPLIFIED: No reflection needed - INCA 7.5+ guaranteed to have these methods
            Dim currentTemplate As String = myIncaOnlineExperiment.GetRecordingFileNameTemplate()

            ' ✅ DIAGNOSTIC: Log entry parameters and current state
            Dim hasParameter As Boolean = Not String.IsNullOrWhiteSpace(preferredBaseName)
            HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: ENTRY - hasParameter={hasParameter}, preferredBaseName='{If(preferredBaseName, "(null)")}', currentTemplate='{currentTemplate}'")

            ' ✅ Check if autoincrement is already enabled
            Dim autoIncrementEnabled As Boolean = myIncaOnlineExperiment.GetRecordingFileAutoincrementFlag()
            HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: autoIncrementEnabled={autoIncrementEnabled}")

            ' ✅ UPDATED: Check for ANY counter format: &CNT2, [CNT2], or &[CNT2]
            Dim hasValidCounterToken As Boolean = False
            Dim needsCleaning As Boolean = False

            If Not String.IsNullOrWhiteSpace(currentTemplate) Then
                ' ✅ Check if template ends with ANY valid counter pattern (not just CNT2/CNT3)
                ' Pattern: (_&CNT\d+|_\[CNT\d+\])$ matches "_&CNT1", "_&CNT2", "_[CNT3]" etc. at end
                Dim endsWithValidToken As Boolean = System.Text.RegularExpressions.Regex.IsMatch(
                currentTemplate,
                "(_&\[?CNT\d+\]?|_\[CNT\d+\])$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)

                ' Count how many counter tokens exist ANYWHERE in the template
                Dim counterMatches As Integer = System.Text.RegularExpressions.Regex.Matches(
                currentTemplate,
                "(?i)&\[?CNT\d+\]?|\[CNT\d+\]").Count

                ' ✅ Determine if template is valid or needs cleaning
                If endsWithValidToken AndAlso counterMatches = 1 Then
                    ' Template is correctly formatted: exactly one counter at end
                    hasValidCounterToken = True
                    HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: Template valid - ends with counter, total count = 1")
                ElseIf counterMatches > 1 Then
                    ' Multiple counter tokens found (e.g., "_&[CNT2]_&[CNT2]" or "_&[CNT2]_&[CNT1]")
                    needsCleaning = True
                    HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: Template needs cleaning - {counterMatches} counter tokens found")
                ElseIf counterMatches = 1 AndAlso Not endsWithValidToken Then
                    ' Counter token exists but not at end (e.g., "_&[CNT2]_base")
                    needsCleaning = True
                    HandleUserMessageLogging("COMM", "EnsureRecordingFileNameTemplateHasCounter: Template needs cleaning - counter not at end")
                End If
            End If

            ' ✅ Early exit if template is valid and autoincrement is enabled
            ' CRITICAL: Only exit early if NO preferred base name provided (verification mode)
            If hasValidCounterToken AndAlso autoIncrementEnabled AndAlso Not hasParameter Then
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: ✅ EARLY EXIT - Template already valid: '{currentTemplate}'")
                myIncaOnlineExperiment.DisableRecordingFileDateTimeSuffix()
                Return True
            End If

            ' ✅ DIAGNOSTIC: Log why we didn't exit early
            If Not hasValidCounterToken Then
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: No early exit - hasValidCounterToken=FALSE")
            ElseIf Not autoIncrementEnabled Then
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: No early exit - autoIncrementEnabled=FALSE")
            ElseIf hasParameter Then
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: No early exit - hasParameter=TRUE (setting new base name)")
            End If

            ' ✅ Clean up template ONLY if it has duplicates or malformed tokens
            If needsCleaning Then
                Dim originalTemplate As String = currentTemplate
                ' Remove all counter formats
                ' ✅ MULTI-PASS CLEANING: Remove all counter artifacts (valid tokens + orphaned characters)
                ' Pass 1: Remove valid counter tokens (&[CNT2], &[CNT3], etc.)
                currentTemplate = System.Text.RegularExpressions.Regex.Replace(
                currentTemplate,
                "(?i)&\[?CNT\d+\]?|\[CNT\d+\]",
                String.Empty)
                ' This handles malformed templates like "base_&_&" or "base_[_]"
                currentTemplate = System.Text.RegularExpressions.Regex.Replace(currentTemplate, "[&\[\]]", String.Empty)
                ' This handles "base___" -> "base_" or "base_-_" -> "base_"
                currentTemplate = System.Text.RegularExpressions.Regex.Replace(currentTemplate, "[_\-]+", "_")
                ' Pass 4: Remove trailing underscores/dashes that remain
                currentTemplate = currentTemplate.TrimEnd("_"c, "-"c, " "c)
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: Cleaned '{originalTemplate}' to '{currentTemplate}'")
            End If

            ' ✅ Build the new template (BASE NAME + SEPARATOR + COUNTER VARIABLE)
            ' CRITICAL: Template MUST include the counter variable (&[CNT2], &[CNT3], etc.) as documented in INCA API.
            ' The counter variable tells INCA:
            '   1. Where to place the counter (after the separator)
            '   2. How to format it (CNT2 = at least 2 digits: 01, 02, ... 99, 100, 101...)
            ' Example: "baseName_&[CNT2]" expands to "baseName_01.mf4", "baseName_02.mf4", etc.
            Dim newTemplate As String

            If hasParameter Then
                ' ✅ MODE 1: User provided a specific base name (called from SetupDataLogging)
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: MODE 1 - Setting new base name: '{preferredBaseName}'")
                Dim sanitized As String = preferredBaseName
                ' Remove any existing counter tokens and trailing separators
                sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                "(?i)&\[?CNT\d+\]?|\[CNT\d+\]",
                String.Empty).Trim()
                sanitized = sanitized.TrimEnd("_"c, "-"c, " "c)

                newTemplate = If(String.IsNullOrWhiteSpace(sanitized & "_"), "&[CNT2]", sanitized & "_" & "&[CNT2]")
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: MODE 1 - New template: '{newTemplate}'")
            ElseIf hasValidCounterToken Then
                ' ✅ MODE 2: Template already valid - keep it as-is (shouldn't reach here due to early exit)
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: MODE 2 - Template already valid, keeping: '{currentTemplate}'")
                newTemplate = currentTemplate
            Else
                ' ✅ MODE 3: Template corrupted - rebuild using cleaned base name (called from StartRecording)
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: MODE 3 - Fixing corrupted template: '{currentTemplate}'")
                Dim cleaned As String = If(String.IsNullOrWhiteSpace(currentTemplate), "", currentTemplate)
                cleaned = cleaned.TrimEnd("_"c, "-"c, " "c)
                newTemplate = If(String.IsNullOrWhiteSpace(cleaned & "_"), "&[CNT2]", cleaned & "_" & "&[CNT2]")
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: MODE 3 - Fixed template: '{newTemplate}'")
            End If

            ' ✅ Set the template if it changed
            If Not String.Equals(currentTemplate, newTemplate, StringComparison.OrdinalIgnoreCase) Then
                ' ✅ SIMPLIFIED: Direct API call (no reflection)
                Dim setResult As Boolean = myIncaOnlineExperiment.SetRecordingFileNameTemplate(newTemplate)
                If Not setResult Then
                    ' ✅ IMPROVED DIAGNOSTICS: Log state information to help diagnose why template change failed
                    Dim recordingState As Boolean = False
                    Dim measurementState As Boolean = False
                    Try
                        recordingState = myIncaOnlineExperiment.GetRecordingState()
                    Catch
                        ' Ignore - just for diagnostics
                    End Try
                    Try
                        measurementState = myIncaOnlineExperiment.IsMeasurementRunning
                    Catch
                        ' Ignore - just for diagnostics
                    End Try

                    HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: ❌ FAILED to set template - recordingState={recordingState}, measurementState={measurementState}")
                    HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: ❌ Attempted to change '{currentTemplate}' to '{newTemplate}'")
                    HandleUserMessageLogging("COMM", "EnsureRecordingFileNameTemplateHasCounter: ❌ INCA may require measurement/recording to be stopped before template changes")
                    Return False
                End If
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: ✅ UPDATED template from '{currentTemplate}' to '{newTemplate}'")
            Else
                HandleUserMessageLogging("COMM", $"EnsureRecordingFileNameTemplateHasCounter: Template unchanged: '{newTemplate}'")
            End If

            ' ✅ Enable autoincrement flag (only if not already enabled)
            ' NOTE: The counter format (01 vs 001) is controlled by the template variable (&CNT2 vs &CNT3)
            ' NOT by a separate API call - SetRecordingFileAutoincrementDigits does not exist in INCA API!
            If Not autoIncrementEnabled Then
                myIncaOnlineExperiment.SetRecordingFileAutoincrementFlag(True)
                HandleUserMessageLogging("COMM", "EnsureRecordingFileNameTemplateHasCounter: ✅ Enabled autoincrement flag")
            Else
                HandleUserMessageLogging("COMM", "EnsureRecordingFileNameTemplateHasCounter: Autoincrement already enabled")
            End If

            ' ✅ Additional settings
            myIncaOnlineExperiment.DisableRecordingFileDateTimeSuffix()
            SaveExperiment()

            Return True

        Catch ex As Exception
            HandleUserMessageLogging("COMM", "EnsureRecordingFileNameTemplateHasCounter: " & ex.Message)
            Return False
        End Try
    End Function


    Public Function GetAvailableDevices() As IGM_INCA_Comm.INCADeviceStatus() Implements IGM_INCA_Comm.GetAvailableDevices

        'Passes back a string array containing all available device names associated with the current INCA
        'project

        'Will pass back a single device name of "Invalid" in array position 0 if there is no
        'valid INCA experiment object when the call is made.

        'This Function is used by the GmResidentClient INCA_InterfaceClass in various routines
        'which are required to support basic GmResidentClient functionality.

        Dim myDevices() As ExperimentDevice
        Dim x As Short
        Dim tempstr() As IGM_INCA_Comm.INCADeviceStatus

        ActiveIncaApiCall = "GetAvailableDevices"

        ReDim tempstr(0)

        Try

            tempstr(0).myName = "Invalid"
            tempstr(0).myStatus = False

            If myIncaOnlineExperiment IsNot Nothing Then

                myDevices = myIncaOnlineExperiment.GetAllDevices
                If myDevices IsNot Nothing Then
                    ReDim tempstr(UBound(myDevices))
                    For x = 0 To UBound(myDevices)
                        tempstr(x).myName = myDevices(x).GetName
                        tempstr(x).myDeviceType = myDevices(x).GetDeviceType.ToString
                        tempstr(x).myStatus = myDevices(x).IsActive
                    Next x
                End If

            End If

        Catch ex As Exception

            HandleUserMessageLogging("COMM", "GetAvailableDevices Error " & ex.Message)

        Finally
            GetAvailableDevices = tempstr
            ActiveIncaApiCall = String.Empty
        End Try

    End Function

    Public Function GetDeviceAcquisitionRates(ByVal Device As String) As String() Implements IGM_INCA_Comm.GetDeviceAcquisitionRates

        'Takes a string identifying a valid device associated with the INCA project and passes back
        'a string array which contains a list of all valid acquisitionrates for that devide.

        'This function is used by the GmResidentClient to populate a selection list for use
        'when configuring a signal to be icorporated into a grid.

        Dim myDevice As ExperimentDevice

        myDevice = myIncaOnlineExperiment.GetDevice(Device)

        GetDeviceAcquisitionRates = myDevice.GetAllAcquisitionRates()

    End Function

    Public Function StartMeasurement() As Boolean Implements IGM_INCA_Comm.StartMeasurement

        'Starts measurement if not already in Measurement mode and returns the status of the
        'start measurement request.

        'This function is used by the GmResidentClient to start measurement.

        ActiveIncaApiCall = "StartMeasurement"

        ResetRecords()

        If MeasurementStarted = True Then
            StartMeasurement = True
            ActiveIncaApiCall = String.Empty
            Exit Function
        End If

        StartMeasurement = False

        If myIncaOnlineExperiment IsNot Nothing Then
            MeasurementStarted = myIncaOnlineExperiment.StartMeasurement
            If MeasurementStarted = False Then
                HandleUserMessageLogging("COMM", "Measurement Started returned false")
            End If
            StartMeasurement = MeasurementStarted
        End If

        If PATAC = False Then
            OnVehicleScreen.Button5.Enabled = True
        End If

        ActiveIncaApiCall = String.Empty


    End Function

    Public Function StopMeasurement() As Boolean Implements IGM_INCA_Comm.StopMeasurement

        'Stops measurement if in Measurement mode and returns the status of the
        'stop measurement request.

        'This function is used by the GmResidentClient to stop measurement.      

        ActiveIncaApiCall = "StopMeasurement"

        StopMeasurement = False

        If myIncaOnlineExperiment IsNot Nothing Then

            StopMeasurement = myIncaOnlineExperiment.StopMeasurement

            Do While GetRecordingState() = True
                ActiveIncaApiCall = "Waiting for INCA To Return RecordingState = False"
                System.Threading.Thread.Sleep(100)
            Loop
            ActiveIncaApiCall = String.Empty

            Do While GetMeasurementStatus() = "True"
                ActiveIncaApiCall = "Waiting for INCA To Return MeasurentStatus = False"
                System.Threading.Thread.Sleep(100)
            Loop
            ActiveIncaApiCall = String.Empty

            MeasurementStarted = StopMeasurement <> True
            ResetRecords()
        End If

        ActiveIncaApiCall = String.Empty

    End Function

    Public Function StartRecording() As Boolean Implements IGM_INCA_Comm.StartRecording
        ActiveIncaApiCall = "StartRecording"

        Try
            If myIncaOnlineExperiment IsNot Nothing AndAlso myIncaOnlineExperiment.GetRecordingState() = False Then
                ' ✅ Alternative fix handles filenames directly (no template dependency)
                If myIncaOnlineExperiment.StartRecording() Then
                    Return myIncaOnlineExperiment.GetRecordingState()
                End If
            End If
            Return False
        Catch ex As Exception
            HandleUserMessageLogging("COMM", "StartRecording: " & ex.Message)
            Return False
        Finally
            ActiveIncaApiCall = String.Empty
        End Try
    End Function
    Public Function StopRecording(ByVal PathFileName As String, ByVal RecordingFileFormat As String) As Boolean Implements IGM_INCA_Comm.StopRecording

        'Stops recording if in record mode and returns the status of the
        'stop recording request.

        'This function is used by the GmResidentClient to stop recording.

        ActiveIncaApiCall = "StopRecording"

        Try
            If myIncaOnlineExperiment IsNot Nothing Then
                myIncaOnlineExperiment.StopRecording(PathFileName, RecordingFileFormat)
                Return True
            Else
                Return False
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "StopRecording: " & ex.Message)
            Return False
        Finally
            ActiveIncaApiCall = String.Empty
        End Try

    End Function


    Public Function IsTargetOnWorkingPage() As String Implements IGM_INCA_Comm.IsTargetOnWorkingPage

        Dim myWorkbaseDevice As WorkbaseDevice
        Dim myDevices() As ExperimentDevice
        Dim x As Integer

        Try

            ActiveIncaApiCall = "IsTargetOnWorkingPage"

            IsTargetOnWorkingPage = "False"

            myDevices = myIncaOnlineExperiment.GetAllDevices

            For x = 0 To UBound(myDevices)
                If myDevices(x).IsWorkbaseDevice = True Then
                    myWorkbaseDevice = myDevices(x)
                    If myWorkbaseDevice.IsTargetOnWorkPage = True Then
                        IsTargetOnWorkingPage = "True"
                        Exit For
                    End If
                End If
            Next

        Catch ex As Exception
            HandleUserMessageLogging("COMM", "IsTargetOnWorkingPage Exception: " & ex.Message)
            IsTargetOnWorkingPage = "Unknown"

        Finally
            ActiveIncaApiCall = String.Empty
        End Try

    End Function

    Public Function GetReferenceDataSetDataBasePaths() As String() Implements IGM_INCA_Comm.GetReferenceDataSetDataBasePaths

        Dim myWorkbaseDevice As WorkbaseDevice
        Dim myDevices() As ExperimentDevice
        Dim x As Integer
        Dim ctr As Integer

        Dim _ReferenceDataSetDataBasePaths() As String

        _ReferenceDataSetDataBasePaths = Nothing
        ctr = 0

        myDevices = myIncaOnlineExperiment.GetAllDevices

        For x = 0 To UBound(myDevices)
            If myDevices(x).IsWorkbaseDevice = True Then
                myWorkbaseDevice = myDevices(x)
                ReDim Preserve _ReferenceDataSetDataBasePaths(ctr)
                _ReferenceDataSetDataBasePaths(ctr) = myWorkbaseDevice.GetReferenceDataSetDataBasePath
                HandleUserMessageLogging("COMM", "ReferenceDataSetDataBasePaths(" & ctr & ") = " & _ReferenceDataSetDataBasePaths(ctr))

                myWorkbaseDevice.EnableDownloadDifferencesAfterIgnitionOffOn()

                ctr += 1
            End If
        Next

        GetReferenceDataSetDataBasePaths = _ReferenceDataSetDataBasePaths

    End Function

    Public Sub SetProjectDatabaseInfo() Implements IGM_INCA_Comm.SetProjectDatabaseInfo

        Dim x As Integer
        Dim myDevices() As ExperimentDevice

        ProjectDatabasePaths = Nothing

        myDevices = myIncaOnlineExperiment.GetAllDevices

        For x = 0 To UBound(myDevices)
            If Len(myDevices(x).GetProjectDataBasePath) > 0 Then

                'These are globals...

                ReDim Preserve ProjectDatabasePaths(x)
                ReDim Preserve ProjectDatabaseNames(x)


                ProjectDatabasePaths(x) = myDevices(x).GetProjectDataBasePath

                'Check this for CSAV2 behavior...

                ProjectDatabaseNames(x) = Mid(ProjectDatabasePaths(x), InStrRev(ProjectDatabasePaths(x), "\") + 1, Len(ProjectDatabasePaths(x)))

                HandleUserMessageLogging("COMM", "ProjectDatabasePath( " & x & " ) for " & myDevices(x).GetName & " - " & myDevices(x).GetProjectDataBasePath)
            End If
        Next

    End Sub

    Public Function GetWorkingDataSetDataBasePaths() As String() Implements IGM_INCA_Comm.GetWorkingDataSetDataBasePaths

        Dim myWorkbaseDevice As WorkbaseDevice
        Dim myDevices() As ExperimentDevice
        Dim x As Integer
        Dim ctr As Integer

        Dim _WorkingDataSetDataBasePaths() As String

        _WorkingDataSetDataBasePaths = Nothing
        ctr = 0

        myDevices = myIncaOnlineExperiment.GetAllDevices

        For x = 0 To UBound(myDevices)
            If myDevices(x).IsWorkbaseDevice = True Then
                myWorkbaseDevice = myDevices(x)
                ReDim Preserve _WorkingDataSetDataBasePaths(ctr)
                _WorkingDataSetDataBasePaths(ctr) = myWorkbaseDevice.GetWorkingDataSetDataBasePath
                HandleUserMessageLogging("COMM", "WorkingDataSetDataBasePath(" & ctr & ") = " & _WorkingDataSetDataBasePaths(ctr))
                ctr += 1
            End If
        Next

        GetWorkingDataSetDataBasePaths = _WorkingDataSetDataBasePaths

    End Function

    Public Function HandleWorkspace(ByVal EtasUserName As String, ByVal RegisterIntoNewBlankExp As Boolean) As String Implements IGM_INCA_Comm.HandleWorkspace

        'Refactored: high level orchestration delegates details to helpers for clarity and testability.
        Dim returnstring As String = "False"

        Try
            ' 1) Close any open experiment/view
            If Not CloseOpenExperiment() Then
                returnstring = "HandleWorkspace: ERROR: CLEVIR Cannot be started with an Experiment open in INCA"
                HandleUserMessageLogging("GMRC", returnstring)
                Return returnstring
            End If

            ' 2) Find workspace(s)
            Dim MyDatabaseItems() As DataBaseItem = myActualDatabase.BrowseItem(INCAWorkspace)
            If MyDatabaseItems Is Nothing OrElse UBound(MyDatabaseItems) < 0 Then
                returnstring = "HandleWorkspace: ERROR: MyDatabaseItems is nothing"
                HandleUserMessageLogging("GMRC", returnstring)
                Return returnstring
            End If

            ' 3) Choose correct workspace index (handles duplicates)
            Dim WorkspaceNum As Integer = SelectWorkspaceIndex(MyDatabaseItems)
            If WorkspaceNum < 0 Then
                returnstring = "HandleWorkspace: ERROR: Workspace selection failed"
                HandleUserMessageLogging("GMRC", returnstring)
                Return returnstring
            End If

            ' 4) Assign selected workspace as hardware configuration (MyHWC)
            MyHWC = MyDatabaseItems(WorkspaceNum)

            ' 5) Optionally create experiment from template
            If RegisterIntoNewBlankExp Then
                If Len(CanTemplateExperimentName) = 0 Then
                    CanTemplateExperimentName = GProjectAbbreviation & "_CAN_Template_Exp"
                End If

                OnVehicleScreen.TopMost = True
                HandleUserMessageLogging("GMRC", "Using " & CanTemplateExperimentName & " to create the new experiment " & INCAExperiment, DisplayMsgBox, )
                OnVehicleScreen.TopMost = False

                If Not CreateExperimentFromTemplateIfRequested(CanTemplateExperimentName, INCAExperiment) Then
                    returnstring = "HandleWorkspace: ERROR: " & CanTemplateExperimentName & " not found"
                    HandleUserMessageLogging("GMRC", returnstring, DisplayMsgBox)
                    Return returnstring
                End If
            End If

            ' 6) Locate requested experiment environment
            MyDatabaseItems = myActualDatabase.BrowseItem(INCAExperiment)
            If MyDatabaseItems Is Nothing OrElse UBound(MyDatabaseItems) < 0 Then
                returnstring = "HandleWorkspace: ERROR: Invalid INCA Experiment: Experiment Name - " & INCAExperiment
                HandleUserMessageLogging("GMRC", returnstring)
                Return returnstring
            End If

            If Not MyDatabaseItems(0).IsExperimentEnvironment Then
                returnstring = "HandleWorkspace: ERROR: Invalid INCA Experiment: Experiment Name - " & INCAExperiment
                HandleUserMessageLogging("GMRC", returnstring)
                Return returnstring
            End If

            ' 7) Create ExperimentEnvironment and initialize hardware / open experiment
            MyExperimentEnvironment = CastDbItemToExpEnv(MyDatabaseItems(0))

            If Not OpenExperimentEnvironmentAndSetHW() Then
                ' OpenExperimentEnvironmentAndSetHW sets logging on failure
                returnstring = "HandleWorkspace: ERROR: Failed to set experiment environment or hardware"
                HandleUserMessageLogging("GMRC", returnstring)
                Return returnstring
            End If

            ' 8) After experiment opened, process devices and set up project info
            If Not ProcessDevicesAfterOpen() Then
                ' ProcessDevicesAfterOpen will set an appropriate message in returnstring on failure
                returnstring = "HandleWorkspace: ERROR: Failed during device post-processing"
                HandleUserMessageLogging("GMRC", returnstring)
                Return returnstring
            End If

            ' 9) If everything succeeded, return "True" (preserve legacy behavior)
            returnstring = "True"
            HandleUserMessageLogging("GMRC", "HandleWorkspace: " & returnstring)
            Return returnstring

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "HandleWorkspace: ERROR: " & ex.Message)
            Return "HandleWorkspace: ERROR: " & ex.Message
        End Try

    End Function

    ' Opens the experiment environment, initializes hardware if required, sets hardware configuration
    ' and opens the experiment view. Returns True on success.
    Private Function OpenExperimentEnvironmentAndSetHW() As Boolean
        Try
            HandleUserMessageLogging("GMRC", "HandleWorkspace: Initializing experiment environment and hardware")

            ' Determine whether hardware initialization is required
            Dim tmpExperimentEnvironment As ExperimentEnvironment = MyHWC.GetAssignedExperimentEnvironment()
            Dim HardwareInitializationRequired As Boolean = True

            If tmpExperimentEnvironment IsNot Nothing Then
                If tmpExperimentEnvironment.GetName() = MyExperimentEnvironment.GetName() Then
                    ' Already assigned to same experiment, check if devices are active -> then hardware init not required
                    Dim curExp As Experiment = myinca.GetOpenedExperiment()
                    If curExp IsNot Nothing AndAlso curExp.IsIncaOnlineExperiment Then
                        Dim devices() As ExperimentDevice = curExp.GetAllDevices()
                        Dim allActive As Boolean = True
                        For i As Integer = 0 To UBound(devices)
                            If Not devices(i).IsActive Then
                                allActive = False
                                Exit For
                            End If
                        Next
                        If allActive Then HardwareInitializationRequired = False
                    End If
                Else
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: ExperimentName Changed from " & tmpExperimentEnvironment.GetName & " to " & MyExperimentEnvironment.GetName)
                End If
            End If

            HandleUserMessageLogging("GMRC", "HandleWorkspace: HardwareInitializationRequired = " & HardwareInitializationRequired)

            ' Always set experiment hardware configuration into the ExperimentEnvironment first.
            ' Historically INCA expects the ExperimentEnvironment to be configured before hardware init.
            SetExperimentDispatch = False
            HandleUserMessageLogging("GMRC", "HandleWorkspace: Setting Hardware Configuration (pre-init)")
            If Not MyExperimentEnvironment.SetHardwareConfiguration(MyHWC) Then
                HandleUserMessageLogging("GMRC", "HandleWorkspace: ERROR: MyExperimentEnvironment.SetHardwareConfiguration(MyHWC) = False (pre-init)")
                Return False
            End If

            ' Ensure the HardwareConfiguration knows about the ExperimentEnvironment as well.
            ' Some INCA APIs require the reciprocal assignment before InitializeHardware is called.
            Try
                Dim setExpOk As Boolean = False
                Try
                    setExpOk = MyHWC.SetExperimentEnvironment(MyExperimentEnvironment)
                Catch exSetExp As Exception
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: MyHWC.SetExperimentEnvironment threw: " & exSetExp.Message)
                End Try

                If setExpOk Then
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: MyHWC.SetExperimentEnvironment succeeded")
                Else
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: MyHWC.SetExperimentEnvironment returned FALSE or not supported - continuing (may still work)")
                End If
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", "HandleWorkspace: Failed to call MyHWC.SetExperimentEnvironment: " & ex.Message)
            End Try

            ' Initialize hardware if required (single diagnostic attempt)
            Dim hardwareInitFailed As Boolean = False

            If HardwareInitializationRequired Then
                HandleUserMessageLogging("GMRC", "HandleWorkspace: Initializing Hardware... (diagnostic)")

                If MyHWC Is Nothing Then
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: MyHWC is Nothing - cannot initialize hardware")
                    Return False
                End If

                ' Log HWC and assigned experiment environment names for diagnostics
                Try
                    Dim hwcName As String = MyHWC.GetName()
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: MyHWC name = " & hwcName)
                Catch exInner As Exception
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: MyHWC.GetName threw: " & exInner.Message)
                End Try
                Try
                    Dim assignedExp As ExperimentEnvironment = MyHWC.GetAssignedExperimentEnvironment()
                    If assignedExp IsNot Nothing Then
                        HandleUserMessageLogging("GMRC", "HandleWorkspace: MyHWC assigned experiment = " & assignedExp.GetName())
                    Else
                        HandleUserMessageLogging("GMRC", "HandleWorkspace: MyHWC has no assigned experiment environment")
                    End If
                Catch exInner2 As Exception
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: Failed to read MyHWC.GetAssignedExperimentEnvironment: " & exInner2.Message)
                End Try

                Dim initOk As Boolean = False
                Try
                    ' small delay to allow INCA to settle in hardwareless scenarios
                    Threading.Thread.Sleep(100)
                    initOk = MyHWC.InitializeHardware()
                Catch exInit As Exception
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: InitializeHardware threw exception: " & exInit.Message)
                    hardwareInitFailed = True
                    ' try to get last rci2 error if available
                    Try
                        Dim sb As New System.Text.StringBuilder(1024)
                        If rci2 IsNot Nothing Then
                            rci2.IncaGetLastError(sb, sb.Capacity)
                            If sb.Length > 0 Then HandleUserMessageLogging("GMRC", "HandleWorkspace: rci2.IncaGetLastError: " & sb.ToString())
                        Else
                            HandleUserMessageLogging("GMRC", "HandleWorkspace: rci2 is Nothing; cannot query last INCA error.")
                        End If
                    Catch exErr As Exception
                        HandleUserMessageLogging("GMRC", "HandleWorkspace: Failed to query rci2 error: " & exErr.Message)
                    End Try
                End Try

                If Not initOk Then
                    ' Log failure but DO NOT immediately fail startup in hardwareless environment.
                    ' Historically CLEVIR runs without hardware connected and INCA prompts are bypassed;
                    ' we continue, allowing later device checks to report missing hardware to the user.
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: WARNING: InitializeHardware returned FALSE - continuing without hardware init (will detect missing devices later)")
                    hardwareInitFailed = True

                    ' attempt to capture INCA last error for diagnostics
                    Try
                        Dim sb As New System.Text.StringBuilder(1024)
                        If rci2 IsNot Nothing Then
                            rci2.IncaGetLastError(sb, sb.Capacity)
                            If sb.Length > 0 Then HandleUserMessageLogging("GMRC", "HandleWorkspace: rci2.IncaGetLastError: " & sb.ToString())
                        Else
                            HandleUserMessageLogging("GMRC", "HandleWorkspace: rci2 is Nothing; cannot query last INCA error.")
                        End If
                    Catch exErr As Exception
                        HandleUserMessageLogging("GMRC", "HandleWorkspace: Failed to query rci2 error: " & exErr.Message)
                    End Try
                    ' Do not return False here — continue to open the experiment.
                Else
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: Hardware Initialized")
                End If
            End If

            ' Open experiment (if not already open)
            myExperiment = myinca.GetOpenedExperiment()
            If myExperiment Is Nothing Then
                HandleUserMessageLogging("GMRC", "HandleWorkspace: Opening INCA Experiment - Please be patient...")
                StatusNotifier.Toast("HandleWorkspace: Opening INCA Experiment - Please be patient...", "INCA", durationMs:=1000, ensureMainOnTop:=False)

                myExpEnvView = MyExperimentEnvironment.OpenExperiment()
                If myExpEnvView Is Nothing Then
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: ERROR: Could not open Experiment")
                    Return False
                End If

                myExperiment = myExpEnvView.GetExperiment()
                HandleUserMessageLogging("GMRC", "HandleWorkspace: myExperiment = myExpEnvView.GetExperiment")
            Else
                HandleUserMessageLogging("GMRC", "HandleWorkspace: Experiment already opened")
                ' If experiment is already open, ensure it is online experiment
                If Not myExperiment.IsIncaOnlineExperiment Then
                    HandleUserMessageLogging("GMRC", "HandleWorkspace: IsIncaOnlineExperiment = False")
                    Return False
                End If
            End If

            ' Verify we now have an online experiment
            If myExperiment Is Nothing OrElse Not myExperiment.IsIncaOnlineExperiment Then
                HandleUserMessageLogging("GMRC", "HandleWorkspace: ERROR: IsIncaOnlineExperiment = False")
                Return False
            End If

            ' Set server reference to the online experiment
            myIncaOnlineExperiment = myExperiment
            HandleUserMessageLogging("GMRC", "HandleWorkspace: myIncaOnlineExperiment set")

            ' If initialization failed earlier, surface a warning in status (but do not block)
            If hardwareInitFailed Then
                HandleUserMessageLogging("GMRC", "HandleWorkspace: WARNING: Hardware initialization failed earlier - device connectivity will be reported by subsequent checks.")
            End If

            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "OpenExperimentEnvironmentAndSetHW: " & ex.Message)
            Return False
        End Try
    End Function

    ' Processes devices after the experiment has been opened and configured.
    ' Returns True on success. Preserves original behavior: creates Master lists in debug, sets FlashParameters, 
    ' checks device conformity and builds ProjectDatabasePaths, Working/Reference dataset paths, etc.
    Private Function ProcessDevicesAfterOpen() As Boolean
        Try
            Dim SaveCalList As Boolean = False
            Dim SaveMeasValList As Boolean = False
            Dim fnum As Integer = 0
            Dim fnum2 As Integer = 0
            Dim Calcnt As Integer = 0
            Dim MeasCnt As Integer = 0
            Dim cnt As Integer = 0
            Dim returnstring As String = String.Empty
            Dim tempstr As String = String.Empty

            HandleUserMessageLogging("GMRC", "HandleWorkspace: Setting  myDevices = myIncaOnlineExperiment.GetAllDevices")
            Dim myDevices() As ExperimentDevice = myIncaOnlineExperiment.GetAllDevices

            ' Decide whether to create master lists (legacy, only do in Debug)
            If Not File.Exists(Path.Combine(My.Application.Info.DirectoryPath, "MasterCalList.txt")) Then SaveCalList = True
            If Not File.Exists(Path.Combine(My.Application.Info.DirectoryPath, "MasterMeasVarList.txt")) Then SaveMeasValList = True

            HandleUserMessageLogging("GMRC", "HandleWorkspace: Looping through all of myDevices...")
            For x As Integer = 0 To UBound(myDevices)
                If myDevices(x).IsWorkbaseDevice = True And InStr(UCase(myDevices(x).GetName), "CALCDEV") = 0 Then
                    If Debugger.IsAttached Then
                        Dim myExperimentElementsInDevice() As ExperimentElement
                        If SaveCalList Then
                            myExperimentElementsInDevice = BrowseExperimentElementsInDevice("K*", myDevices(x))
                            If fnum = 0 Then
                                fnum = FreeFile()
                                FileOpen(fnum, Path.Combine(My.Application.Info.DirectoryPath, "MasterCalList.txt"), OpenMode.Output)
                            End If
                            For y As Integer = 0 To UBound(myExperimentElementsInDevice)
                                If myExperimentElementsInDevice(y).IsCalibrationElement Then
                                    ReDim Preserve myCalInfo(Calcnt)
                                    myCalInfo(Calcnt).CalName = myExperimentElementsInDevice(y).GetName
                                    myCalInfo(Calcnt).DeviceName = myDevices(x).GetName
                                    If myExperimentElementsInDevice(y).IsScalar Then
                                        myCalInfo(Calcnt).CalType = "Scalar"
                                    ElseIf myExperimentElementsInDevice(y).IsArray Then
                                        myCalInfo(Calcnt).CalType = "Array"
                                    ElseIf myExperimentElementsInDevice(y).IsOneDTable Then
                                        myCalInfo(Calcnt).CalType = "OneDTable"
                                    ElseIf myExperimentElementsInDevice(y).IsTwoDTable Then
                                        myCalInfo(Calcnt).CalType = "TwoDTable"
                                    ElseIf myExperimentElementsInDevice(y).IsDistribution Then
                                        myCalInfo(Calcnt).CalType = "Distribution"
                                    Else
                                        myCalInfo(Calcnt).CalType = "?"
                                    End If
                                    If myExperimentElementsInDevice(y).IsMatrix Then
                                        myCalInfo(Calcnt).IsMatrix = True
                                    End If
                                    PrintLine(fnum, myCalInfo(Calcnt).DeviceName & Chr(9) & myCalInfo(Calcnt).CalName)
                                    Calcnt += 1
                                End If
                            Next
                        End If

                        If SaveMeasValList Then
                            If fnum2 = 0 Then
                                fnum2 = FreeFile()
                                FileOpen(fnum2, Path.Combine(My.Application.Info.DirectoryPath, "MasterMeasVarList.txt"), OpenMode.Output)
                            End If
                            Dim tempElements() As ExperimentElement = BrowseExperimentElementsInDevice("Va*", myDevices(x))
                            For y As Integer = 0 To UBound(tempElements)
                                If tempElements(y).IsMeasureElement Then
                                    ReDim Preserve myMeasInfo(MeasCnt)
                                    myMeasInfo(MeasCnt).MeasName = tempElements(y).GetName
                                    myMeasInfo(MeasCnt).DeviceName = myDevices(x).GetName
                                    PrintLine(fnum2, myMeasInfo(MeasCnt).DeviceName & Chr(9) & myMeasInfo(MeasCnt).MeasName)
                                    MeasCnt += 1
                                End If
                            Next
                            tempElements = BrowseExperimentElementsInDevice("Ve*", myDevices(x))
                            For y As Integer = 0 To UBound(tempElements)
                                If tempElements(y).IsMeasureElement Then
                                    ReDim Preserve myMeasInfo(MeasCnt)
                                    myMeasInfo(MeasCnt).MeasName = tempElements(y).GetName
                                    myMeasInfo(MeasCnt).DeviceName = myDevices(x).GetName
                                    PrintLine(fnum2, myMeasInfo(MeasCnt).DeviceName & Chr(9) & myMeasInfo(MeasCnt).MeasName)
                                    MeasCnt += 1
                                End If
                            Next
                        End If
                    End If

                    ' Device-specific flash parameter logic (preserve original rules)
                    Dim myWorkbaseDevice As WorkbaseDevice = myDevices(x)
                    If InStr(myDevices(x).GetName, "FCM") > 0 Or InStr(myDevices(x).GetName, "FCM100") > 0 Then
                        cnt = x
                        FlashParameters(cnt).FlashType = "Flash_NONE"
                        FlashParameters(cnt).DeviceName = myDevices(x).GetName
                    End If
                    If InStr(myDevices(x).GetName, "ACP2_MCU") > 0 Or InStr(myDevices(x).GetName, "ACP3_MCU") > 0 Or InStr(myDevices(x).GetName, "ACP4_MCU") > 0 Then
                        cnt = x
                        FlashParameters(cnt).FlashType = "Flash_NONE"
                        FlashParameters(cnt).DeviceName = myDevices(x).GetName
                    End If
                    If InStr(myDevices(x).GetName, "HCF") > 0 Or InStr(myDevices(x).GetName, "HCS") > 0 Or InStr(myDevices(x).GetName, "ASE37") > 0 Then
                        cnt = x
                        FlashParameters(cnt).FlashType = "Flash_NONE"
                        FlashParameters(cnt).DeviceName = myDevices(x).GetName
                    End If
                    If InStr(myDevices(x).GetName, "XETK:1") > 0 Or InStr(myDevices(x).GetName, "LC") > 0 Or InStr(myDevices(x).GetName, "ASE34") > 0 Then
                        cnt = x
                        FlashParameters(cnt).FlashType = "Flash_NONE"
                        FlashParameters(cnt).DeviceName = myDevices(x).GetName
                    End If

                    ' Non-XCP device checks
                    If InStr(myDevices(x).GetName, "XCP") = 0 Then
                        If myWorkbaseDevice.CheckCodePageConform() = False Then
                            returnstring = "CheckCodePageConform Returned FALSE for " & myDevices(x).GetName
                            If InStr(FlashParameters(cnt).DeviceName, "FCM") > 0 Then
                                FlashParameters(cnt).FlashType = "Flash_AC"
                            Else
                                FlashParameters(cnt).FlashType = "Flash_BAC"
                            End If
                        Else
                            HandleUserMessageLogging("GMRC", "HandleWorkspace: CheckCodePageConform Returned TRUE for " & myDevices(x).GetName)
                        End If

                        If myWorkbaseDevice.DownloadWorkPage() = True Then
                            HandleUserMessageLogging("GMRC", "HandleWorkspace: myWorkbaseDevice.DownloadWorkPage returned TRUE for " & myDevices(x).GetName)
                        Else
                            returnstring = "Download Working Page = FAIL for " & myDevices(x).GetName
                        End If

                        If myWorkbaseDevice.CheckDataPagesConform() = False Then
                            returnstring = "CheckDataPagesConform Returned FALSE for " & myDevices(x).GetName
                            If FlashParameters(cnt).FlashType <> "Flash_BAC" And FlashParameters(cnt).FlashType <> "Flash_AC" Then
                                FlashParameters(cnt).FlashType = "Flash_CAL"
                            End If
                        Else
                            HandleUserMessageLogging("GMRC", "HandleWorkspace: CheckDataPagesConform Returned TRUE for " & myDevices(x).GetName)
                        End If
                    End If

                    ' Track disconnected devices
                    If myDevices(x).IsActive = False Then
                        If tempstr = "" Then
                            tempstr = myDevices(x).GetName
                            FlashParameters(cnt).FlashType = "Flash_NOCONNECT"
                        Else
                            tempstr = tempstr & ", " & myDevices(x).GetName
                            FlashParameters(cnt).FlashType = "Flash_NOCONNECT"
                        End If
                    End If

                    HandleUserMessageLogging("GMRC", "HandleWorkspace: " & myDevices(x).GetName & " - Is Active = " & myDevices(x).IsActive)
                End If
            Next

            ' Close any open file handles used for debug lists
            If fnum > 0 Then FileClose(fnum)
            If fnum2 > 0 Then FileClose(fnum2)

            ' Build return message for disconnected devices if any
            If Len(tempstr) > 0 Then
                If InStr(tempstr, ",") > 0 Then
                    returnstring = "The following devices are NOT CONNECTED " & tempstr
                Else
                    returnstring = "Device " & tempstr & " is NOT CONNECTED."
                End If
            End If

            ' Set project database info (legacy call)
            SetProjectDatabaseInfo()

            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ProcessDevicesAfterOpen: " & ex.Message)
            Return False
        End Try
    End Function

    ' Helper: close any open experiment/view. Returns True on success.
    Private Function CloseOpenExperiment() As Boolean
        Try
            myExperiment = myinca.GetOpenedExperiment()
            If myExperiment Is Nothing Then Return True

            If myExperiment.IsIncaOnlineExperiment Then
                myIncaOnlineExperiment = myExperiment
                myIncaOnlineExperiment.UnlockExperiment()
            End If

            Dim view As ExperimentView = myinca.GetOpenedExperimentView()
            If view IsNot Nothing Then
                If Not view.Close() Then
                    HandleUserMessageLogging("GMRC", "CloseOpenExperiment: myExperimentView.Close returned false...")
                    Return False
                End If
            End If

            Return True
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CloseOpenExperiment: " & ex.Message)
            Return False
        End Try
    End Function

    ' Helper: choose workspace index from returned database items.
    ' - items: array returned by myActualDatabase.BrowseItem(INCAWorkspace)
    ' - returns selected index (0..UBound) or -1 if none.
    Private Function SelectWorkspaceIndex(ByVal items() As DataBaseItem) As Integer
        If items Is Nothing OrElse items.Length = 0 Then Return -1

        Dim chosenIndex As Integer = 0
        Dim foundInClevirSetup As Boolean = False
        Dim saveFolderName As String = ""

        For i As Integer = 0 To UBound(items)
            Dim parentPath As String = items(i).GetParentFolder.GetNameWithPath()
            HandleUserMessageLogging("GMRC", "SelectWorkspaceIndex: Candidate workspace in " & parentPath & " folder.")
            If String.Equals(parentPath, "CLEVIR SETUP\WORKSPACES", StringComparison.OrdinalIgnoreCase) Then
                chosenIndex = i
                foundInClevirSetup = True
                Exit For
            End If
            saveFolderName = parentPath
            chosenIndex = i
        Next

        If UBound(items) > 0 Then
            HandleUserMessageLogging("GMRC", "SelectWorkspaceIndex: Duplicate workspaces detected. Using index " & chosenIndex & " from " & saveFolderName)
        End If

        Return chosenIndex
    End Function

    ' Helper: create experiment from a template when requested.
    ' Returns True on success, False on failure (logs messages).
    Private Function CreateExperimentFromTemplateIfRequested(ByVal templateName As String, ByVal newExperimentName As String) As Boolean
        Try
            If String.IsNullOrEmpty(templateName) Then
                HandleUserMessageLogging("GMRC", "CreateExperimentFromTemplateIfRequested: Template name empty")
                Return False
            End If

            Dim myfolder As IncaFolder = myActualDatabase.GetFolder("CLEVIR Setup").GetSubFolder("Experiments")
            Dim existing() As DataBaseItem = myfolder.BrowseDataBaseItem(newExperimentName)
            If existing IsNot Nothing AndAlso UBound(existing) >= 0 Then
                ' If exists, remove first item so we re-create
                myfolder.RemoveComponent(existing(0))
            End If

            Dim templateItems() As DataBaseItem = myActualDatabase.BrowseItem(templateName)
            If templateItems Is Nothing OrElse UBound(templateItems) < 0 Then
                HandleUserMessageLogging("GMRC", "CreateExperimentFromTemplateIfRequested: Template " & templateName & " not found")
                Return False
            End If

            templateItems(0).Copy(newExperimentName)
            HandleUserMessageLogging("GMRC", "CreateExperimentFromTemplateIfRequested: Copied " & templateName & " to " & newExperimentName)
            Return True
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CreateExperimentFromTemplateIfRequested: " & ex.Message, DisplayMsgBox)
            Return False
        End Try
    End Function

    Private Function BrowseExperimentElementsInDevice(ByVal mySearchString As String, ByVal myDevice As ExperimentDevice) As ExperimentElement()

        'Called from HandleWorkspace: Returns an array of ExperimentElements found in the device that match the mySearchString criteria...

        BrowseExperimentElementsInDevice = myIncaOnlineExperiment.BrowseExperimentElementInDevice(mySearchString, myDevice)

    End Function

    Public Function ImportFileIntoINCA(ByVal Filename As String, Optional ByVal overwrite As Boolean = True, Optional ByVal discardimpl As Boolean = False) As Boolean Implements IGM_INCA_Comm.ImportFileIntoINCA

        'Import an exported experiment, workspace or entire folder structure into INCA...

        ImportFileIntoINCA = False

        If myActualDatabase IsNot Nothing And Len(Filename) > 0 Then
            If InStr(Filename, ".exp") > 0 Then
                ImportFileIntoINCA = myActualDatabase.ImportFromFile(Filename, overwrite, discardimpl)
            End If
        End If

    End Function

    Public Async Function SetupDataLogging(ByVal mySelectedTestName As String, ByVal LoginIDStr As String) As Task(Of String) Implements IGM_INCA_Comm.SetupDataLogging

        ' Sets the INCA Recording Pathname and Filename. This assumes that the INCA experiment is set up to automatically increment and add the suffix 
        ' "session number" such as 01, 02 etc. to the recording filename each time recording is stopped and started for a particular recording session.
        ' When a new SelectedTestName is passed in at the beginning of a new recording session (When STOP RECORDING and START RECORDING button is pressed)
        ' the number will be set to 01 again.
        '
        ' Refactor notes:
        ' - Consistent naming: sessionFolder (timestamp_loginID), 
        '   sessionFolderPath (base data directory), vehicleSubfolderPath (vehicle-specific folder),
        '   sessionFolderFullPath (complete session path).
        ' - Reduced redundant directory checks and improved path creation using Path.Combine.
        ' - Simplified LoginID frequency update and made it robust to unexpected formats.
        ' - Clear, consistent error messages and more specific exception handling for path-related errors.
        ' - Preserves original return contract: empty string "" indicates success; non-empty string is an error.
        ' - ✅ REFACTORED: Proper async/await pattern (no blocking .Result)

        Try
            ActiveIncaApiCall = "SetupDataLogging"

            ' ✅ FIXED: Use Await instead of blocking .Result
            If Not Await CheckAutoIncrementFlagAsync() Then
                ActiveIncaApiCall = String.Empty
                Return "INCA Auto Increment Flag"
            End If

            ' Save login id and update frequency list (robust to format)
            SaveLoginID = LoginIDStr
            'If LoginIDNameAndFreqAL IsNot Nothing Then
            '    For idx As Integer = 0 To LoginIDNameAndFreqAL.Count - 1
            '        Dim entry As String = LoginIDNameAndFreqAL(idx).ToString()
            '        Dim parts() As String = entry.Split(New Char() {" "c}, 2)
            '        If parts.Length = 2 AndAlso parts(1).Equals(LoginIDStr, StringComparison.OrdinalIgnoreCase) Then
            '            Dim number As Integer = 0
            '            Integer.TryParse(parts(0), number)
            '            number += 1
            '            LoginIDNameAndFreqAL(idx) = number.ToString("000000") & " " & parts(1)
            '            Exit For
            '        End If
            '    Next
            'Else
            '    ' ensure the collection exists and add default if missing
            '    LoginIDNameAndFreqAL = New ArrayList From {"000001 " & LoginIDStr}
            'End If

            'WriteLoginIDListFile()

            ' Build the session folder name: yyyyMMdd_HHmmss_LoginID
            Dim sessionFolder As String = DateTime.Now.ToString("yyyyMMdd_HHmmss") & "_" & LoginIDStr
            Dim recordingBaseName As String = sessionFolder & "_" & VehicleNumber

            ' Determine and create base data directories
            Dim sessionFolderPath As String = Path.Combine(BaseDataCollectionPath, "Data")
            Dim vehicleSubfolderPath As String = Path.Combine(sessionFolderPath, "gmcsv" & VehicleNumber)
            Dim sessionFolderFullPath As String


            If Not Directory.Exists(vehicleSubfolderPath) Then
                Directory.CreateDirectory(vehicleSubfolderPath)
                HandleUserMessageLogging("COMM", "Base Vehicle Validation Path: " & vehicleSubfolderPath & " Created")
            End If
            sessionFolderFullPath = Path.Combine(vehicleSubfolderPath, sessionFolder)

            ' Store the complete session path
            FinalPathToSaveData = sessionFolderFullPath

            ' Keep legacy value for compatibility/logging
            mySelectedTestName = recordingBaseName & "_"

            ' Ensure the final session directory exists
            If Not Directory.Exists(sessionFolderFullPath) Then
                Directory.CreateDirectory(sessionFolderFullPath)
                HandleUserMessageLogging("COMM", "Session Path: " & sessionFolderFullPath & " Created")
            End If

            ' Configure INCA recording path
            If SetRecordingPathName(sessionFolderFullPath) = False Then
                HandleUserMessageLogging("COMM", "SetRecordingPathName returned FALSE")
                Return "SetRecordingPathName returned FALSE"
            End If

            HandleUserMessageLogging("COMM", "Session Path: " & GetRecordingPathName() & " Retrieved")

            ' Validate recording file format
            RecordingFileFormat = GetRecordingFileFormat()
            HandleUserMessageLogging("COMM", "INCA Recording File Format: " & RecordingFileFormat)
            If Not String.Equals(RecordingFileFormat, "mf4", StringComparison.OrdinalIgnoreCase) Then
                Return "Invalid INCA Recording File Format. You MUST change INCA MDF File Type to MDF 4.0.  (Options / User Options / Experiment / Measure / MDF File Type)"
            End If

            ' ✅ ALTERNATIVE FIX: Set filename with trailing underscore, let INCA append counter
            ' THEORY: SetRecordingFileName("basename_") + autoincrement → "basename_01.mf4", "basename_02.mf4"
            ' This bypasses the template system entirely, avoiding INCA's underscore-stripping bug.
            Try
                HandleUserMessageLogging("COMM", $"SetupDataLogging: Setting recording filename to '{recordingBaseName}_' (with trailing underscore)")

                ' Set filename with trailing underscore
                If Not SetRecordingFileName(recordingBaseName & "_") Then
                    HandleUserMessageLogging("COMM", "SetupDataLogging: ❌ SetRecordingFileName failed")
                    Return "SetRecordingFileName returned FALSE"
                End If

                ' Save experiment to persist settings
                SaveExperiment()

            Catch ex As Exception
                HandleUserMessageLogging("COMM", $"SetupDataLogging: ❌ Exception setting filename: {ex.Message}")
                Return $"SetRecordingFileName exception: {ex.Message}"
            End Try

            ' Predict first sequence filename
            Dim predictedFirstFile As String = $"{recordingBaseName}_{1:D2}.mf4"
            CachedRecordingFilename = predictedFirstFile
            LastKnownRecordingTimeMs = 0 ' Reset rotation detector
            LastFilenameUpdateTime = DateTime.Now

            HandleUserMessageLogging("COMM", $"SetupDataLogging: Cached initial filename: {CachedRecordingFilename}")

            ' Success - empty string indicates success according to existing caller expectations
            Return String.Empty

        Catch ex As UnauthorizedAccessException
            HandleUserMessageLogging("COMM", "GM_INCA_CommClass.SetupDataLogging: " & ex.Message)
            Return "GM_INCA_CommClass.SetupDataLogging: " & ex.Message & " Please make sure that the BaseDataCollectionPath (" & BaseDataCollectionPath & ") defined in the config.xml file is valid.  Exiting..."
        Catch ex As DirectoryNotFoundException
            HandleUserMessageLogging("COMM", "GM_INCA_CommClass.SetupDataLogging: " & ex.Message)
            Return "GM_INCA_CommClass.SetupDataLogging: " & ex.Message & " Please make sure that the BaseDataCollectionPath (" & BaseDataCollectionPath & ") defined in the config.xml file is valid.  Exiting..."
        Catch ex As Exception
            HandleUserMessageLogging("COMM", "GM_INCA_CommClass.SetupDataLogging: " & ex.Message)
            Return "GM_INCA_CommClass.SetupDataLogging: " & ex.Message
        Finally
            ActiveIncaApiCall = String.Empty
        End Try

    End Function

    Public Sub ReadUserIDList() Implements IGM_INCA_Comm.ReadUserIDList

        'Reads contents of userIDList.txt file and puts it into an arraylist.
        'Modernized to use File IO APIs, robust parsing and clearer error handling.
        Dim UserIDFileName As String = Path.Combine(My.Application.Info.DirectoryPath, "UserIDList.txt")

        Try
            LoginIDNameAndFreqAL = New ArrayList
            HandleUserMessageLogging("GMRC", "Reading UserIDList File...")

            ' If file doesn't exist -> create default and persist
            If Not File.Exists(UserIDFileName) Then
                HandleUserMessageLogging("GMRC", UserIDFileName & " does not exist, adding default username...")
                LoginIDNameAndFreqAL.Add("000001 DRVR00")
                WriteLoginIDListFile()
                HandleUserMessageLogging("GMRC", "Reading UserIDList File Complete")
                Return
            End If

            ' If file is locked/in use, keep previous behavior (skip read)
            If FileInUse(UserIDFileName) Then
                HandleUserMessageLogging("GMRC", UserIDFileName & " is in use, skipping read.")
                Return
            End If

            Dim lines() As String = File.ReadAllLines(UserIDFileName)

            If lines Is Nothing OrElse lines.Length = 0 OrElse String.IsNullOrWhiteSpace(lines(0)) Then
                ' corrupted or empty file -> fallback default and persist
                LoginIDNameAndFreqAL.Add("000001 DRVR00")
                HandleUserMessageLogging("GMRC", UserIDFileName & " appears corrupted, using default name and frequency...")
                WriteLoginIDListFile()
                HandleUserMessageLogging("GMRC", "Reading UserIDList File Complete")
                Return
            End If

            Dim first As String = lines(0).Trim()

            ' Detect new format: starts with 6-digit number then space and username (e.g. "000001 DRVR00")
            Dim isNewFormat As Boolean = False
            If first.Length >= 6 Then
                Dim candidate As String = first.Substring(0, 6)
                Dim tmpNum As Integer
                If Integer.TryParse(candidate, tmpNum) Then
                    isNewFormat = True
                End If
            End If

            If isNewFormat Then
                ' New format - copy lines directly (trimmed)
                For Each ln In lines
                    If Not String.IsNullOrWhiteSpace(ln) Then
                        LoginIDNameAndFreqAL.Add(ln.Trim())
                    End If
                Next
            Else
                ' Old format - expected "User<TAB>Number" (convert to "000001 User")
                For Each ln In lines
                    If String.IsNullOrWhiteSpace(ln) Then Continue For
                    Dim raw As String = ln.Trim()
                    Dim tabPos As Integer = raw.IndexOf(vbTab)
                    If tabPos >= 0 Then
                        Dim userPart As String = raw.Substring(0, tabPos).Trim()
                        Dim numPart As String = raw.Substring(tabPos + 1).Trim()
                        Dim parsedNum As Integer
                        If Integer.TryParse(numPart, parsedNum) Then
                            LoginIDNameAndFreqAL.Add(parsedNum.ToString("000000") & " " & userPart)
                        Else
                            ' fallback if numeric parse fails
                            LoginIDNameAndFreqAL.Add("000001 " & userPart)
                        End If
                    Else
                        ' No tab found - try whitespace split with last token numeric, else treat as username-only
                        Dim parts() As String = raw.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)
                        If parts.Length >= 2 Then
                            Dim lastToken As String = parts(parts.Length - 1)
                            Dim parsedNum As Integer
                            If Integer.TryParse(lastToken, parsedNum) Then
                                Dim userName As String = String.Join(" ", parts, 0, parts.Length - 1)
                                LoginIDNameAndFreqAL.Add(parsedNum.ToString("000000") & " " & userName)
                            Else
                                LoginIDNameAndFreqAL.Add("000001 " & raw)
                            End If
                        Else
                            LoginIDNameAndFreqAL.Add("000001 " & raw)
                        End If
                    End If
                Next
                ' Persist converted (new-format) list back to disk
                WriteLoginIDListFile()
            End If

            HandleUserMessageLogging("GMRC", "Reading UserIDList File Complete")

        Catch ex As Exception
            ' On any failure, fallback to a safe default and persist it
            HandleUserMessageLogging("GMRC", "ReadUserIDList: " & ex.Message)
            HandleUserMessageLogging("GMRC", UserIDFileName & " appears corrupted, using default name and frequency...")
            LoginIDNameAndFreqAL = New ArrayList From {"000001 kz0612"}
            Try
                If Not FileInUse(UserIDFileName) Then WriteLoginIDListFile()
            Catch writeEx As Exception
                HandleUserMessageLogging("GMRC", "ReadUserIDList: Failed to write default list: " & writeEx.Message)
            End Try
        End Try

    End Sub

    Public Sub New()
        HandleUserMessageLogging("COMM", "New Connection Established.")
    End Sub

    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Public Function SaveExperiment() As Boolean Implements IGM_INCA_Comm.SaveExperiment
        SaveExperiment = myExpEnvView.SaveExperiment
    End Function

    Public Function SwitchToReferencePage() As Boolean Implements IGM_INCA_Comm.SwitchToReferencePage

        Dim myWorkbaseDevice As WorkbaseDevice

        Dim x As Short

        Dim myDevices() As ExperimentDevice

        ActiveIncaApiCall = "SwitchToReferencePage"

        SwitchToReferencePage = True

        myDevices = myIncaOnlineExperiment.GetAllDevices

        For x = 0 To UBound(myDevices)

            If myDevices(x).IsWorkbaseDevice = True Then
                myWorkbaseDevice = myDevices(x)

                If InStr(myDevices(x).GetName, "XCP") = 0 Then

                    If myWorkbaseDevice.SwitchToReferencePage() = False Then
                        HandleUserMessageLogging("COMM", "SwitchToReferencePage Returned FALSE for " & myDevices(x).GetName)
                        SwitchToReferencePage = False
                    Else
                        HandleUserMessageLogging("COMM", "SwitchToReferencePage Returned TRUE for " & myDevices(x).GetName)
                    End If

                End If

            End If

        Next

        ActiveIncaApiCall = String.Empty

    End Function

    Public Function SwitchToWorkingPage() As Boolean Implements IGM_INCA_Comm.SwitchToWorkingPage

        Dim myWorkbaseDevice As WorkbaseDevice

        Dim x As Short

        Dim myDevices() As ExperimentDevice

        ActiveIncaApiCall = "SwitchToWorkingPage"

        SwitchToWorkingPage = True

        myDevices = myIncaOnlineExperiment.GetAllDevices

        For x = 0 To UBound(myDevices)

            If myDevices(x).IsWorkbaseDevice = True Then
                myWorkbaseDevice = myDevices(x)

                If InStr(myDevices(x).GetName, "XCP") = 0 Then

                    If myWorkbaseDevice.SwitchToWorkingPage() = False Then
                        HandleUserMessageLogging("COMM", "SwitchToReferencePage Returned FALSE for " & myDevices(x).GetName)
                        SwitchToWorkingPage = False
                    Else
                        HandleUserMessageLogging("COMM", "SwitchToReferencePage Returned TRUE for " & myDevices(x).GetName)
                    End If

                End If

            End If

        Next

        ActiveIncaApiCall = String.Empty

    End Function
End Class
