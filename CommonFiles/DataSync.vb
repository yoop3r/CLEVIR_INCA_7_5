Imports System.IO
Imports System.Threading
Imports System.Net.NetworkInformation
Imports System.Runtime.InteropServices
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Public Class SyncConfiguration
    Public Property raid_json_file As String
    Public Property legacy_json_file As String
    Public Property legacy_path As String
    Public Property sync_raid_path As String
    Public Property robocopy_ssd_script As String
    Public Property robocopy_raid_script As String
    Public Property sync_ssd_drive As String
    Public Property sync_raid_drive As String
    Public Property legacy_data_source As String
    Public Property log_path As String
    Public Property sync_to_raid_data_source As String
    Public Property debug_mode As Boolean
    Public Property sleep_for As Integer
End Class

Public Class Configuration
    Public Property sync As List(Of SyncConfiguration)
End Class

Public Class RootObject
    Public Property version As Integer
    Public Property configuration As Configuration
End Class

Public Class SyncScript
    Implements IDisposable

    Private _disposed As Boolean = False
    Private _syncThread As Thread

    ' Object Declarations
    Private objShell As New Process()
    Private Const CONFIG_FILE_PATH As String = "C:\CSVScripts\sync_config.json" ' Path to the script configuration file

    ' Configuration Variables
    Dim strLogName As String
    Dim objFSO As Object
    Dim objWMIService As Object
    Dim objLogFile As StreamWriter
    Dim strJsonRecordStatus As Boolean
    Dim objNetwork As Object

    Dim RAID_JSON_FILE, LEGACY_JSON_FILE, LEGACY_PATH, SYNC_RAID_PATH, ROBOCOPY_SSD_SCRIPT, ROBOCOPY_RAID_SCRIPT, SYNC_SSD_DRIVE, SYNC_RAID_DRIVE, LEGACY_DATA_SOURCE, LOG_PATH, SYNC_TO_RAID_DATA_SOURCE As String
    Dim DEBUG_MODE As Boolean
    Dim SLEEP_FOR As Integer

    ' State Variables
    Private previousRecording As String = "R1"
    Private strRecording As String = "R1"
    Private dataChanged As Boolean = False
    Private keepRunning As Boolean = True
    Private lastLoggedState As String = ""

    Private strSSDDataFolder, strRAIDFolder, strDataDestination, strLogFilePath As String
    Private jsonConfigurationFileExists As Boolean = False

    ' Update Init to use the new threading approach
    Public Sub StartSynchronization()
        Dim userChoice As MsgBoxResult = MsgBox("Start Data Synchronization Script?",
                                                CType(MsgBoxStyle.YesNo + MsgBoxStyle.Question, MsgBoxStyle),
                                                "Start Script")
        If userChoice = MsgBoxResult.Yes Then
            ReadJsonConfigFile(CONFIG_FILE_PATH)
            InitializeLogFile()
            StartMainLoop() ' Start the background thread
        Else
            MessageBox.Show("Script not started.", "Script Stopped", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If

    End Sub

    ' Read Configuration File (sync_config.json)
    Private Sub ReadJsonConfigFile(configFilePath As String)
        If File.Exists(configFilePath) Then
           ' Dim jsonString As String = File.ReadAllText(configFilePath)
            ParseSyncConfig(configFilePath)
        Else
            HandleMissingConfig(configFilePath)
        End If
    End Sub

    ' Parse sync_config.json Configurations
    ' Read and Parse Config File
    Private Sub ParseSyncConfig(configFilePath As String)
        If Not File.Exists(configFilePath) Then
            HandleMissingConfig(configFilePath)
            Return
        End If
        Dim jsonString As String = File.ReadAllText(configFilePath)
        Dim rootObject = JsonConvert.DeserializeObject(Of RootObject)(jsonString)
        ' Assuming you want to use the first "sync" configuration
        Dim syncConfig = rootObject.configuration.sync.FirstOrDefault()
        If syncConfig IsNot Nothing Then
            RAID_JSON_FILE = syncConfig.raid_json_file.Replace("\\", "\")
            LEGACY_JSON_FILE = syncConfig.legacy_json_file.Replace("\\", "\")
            LEGACY_PATH = syncConfig.legacy_path.Replace("\\", "\")
            SYNC_RAID_PATH = syncConfig.sync_raid_path.Replace("\\", "\")
            ROBOCOPY_SSD_SCRIPT = syncConfig.robocopy_ssd_script.Replace("\\", "\")
            ROBOCOPY_RAID_SCRIPT = syncConfig.robocopy_raid_script.Replace("\\", "\")
            SYNC_SSD_DRIVE = syncConfig.sync_ssd_drive.Replace("\\", "\")
            SYNC_RAID_DRIVE = syncConfig.sync_raid_drive.Replace("\\", "\")
            LEGACY_DATA_SOURCE = syncConfig.legacy_data_source.Replace("\\", "\")
            LOG_PATH = syncConfig.log_path.Replace("\\", "\")
            SYNC_TO_RAID_DATA_SOURCE = syncConfig.sync_to_raid_data_source.Replace("\\", "\")
            DEBUG_MODE = syncConfig.debug_mode
            SLEEP_FOR = syncConfig.sleep_for
        Else
            MessageBox.Show("No sync configuration found in the JSON file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    ' Handle missing configuration file
    Private Sub HandleMissingConfig(configFilePath As String)
        Dim result = MessageBox.Show($"Configuration file not found at: {configFilePath}" & vbCrLf & "Do you want to continue?", "Config Missing", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If result = DialogResult.No Then
            HandleError("Script terminated due to missing configuration file.")
            Environment.Exit(1)
        End If
    End Sub

    ' Initialize the log file with error handling
    Private Sub InitializeLogFile()
        Try
            ' Generate log file name with timestamp
            Dim strDate As String = DateTime.Now.ToString("MMddyyyy")
            Dim strTime As String = DateTime.Now.ToString("HHmmss")
            strLogName = $"{strDate}.{strTime}_{Environment.MachineName}.log"
            strLogFilePath = Path.Combine(LOG_PATH, strLogName)

            ' Ensure the log directory exists
            If Not Directory.Exists(LEGACY_DATA_SOURCE) Then
                Directory.CreateDirectory(LEGACY_DATA_SOURCE)
            End If

            ' Close existing log file if open
            If objLogFile IsNot Nothing Then
                objLogFile.Close()
                objLogFile.Dispose()
            End If

            ' Create new log file
            objLogFile = New StreamWriter(strLogFilePath, True)

            ' Write initial log entry
            objLogFile.WriteLine($"Log file initialized at {DateTime.Now}")
            objLogFile.WriteLine($"Machine Name: {Environment.MachineName}")
            objLogFile.WriteLine("----------------------------------------")
            objLogFile.Flush()

            ' Close existing log file if open
            If objLogFile IsNot Nothing Then
                objLogFile.Close()
                objLogFile.Dispose()
            End If

        Catch ex As Exception
            HandleError($"Failed to initialize log file: {ex.Message}")
            ' Rethrow if this is a critical error that should stop execution
            Throw
        End Try
    End Sub

    ' Main Loop initialization method
    Private Sub StartMainLoop()
        ' Create new thread for main loop
        _syncThread = New Thread(AddressOf MainLoopWorker)
        _syncThread.IsBackground = True ' Make it a background thread so it closes with the application
        _syncThread.Name = "DataSyncThread" ' Name the thread for debugging
        _syncThread.Start()
    End Sub

    ' Main Loop worker method that runs on separate thread
    Private Sub MainLoopWorker()
        Try
            Do While keepRunning
                Try
                    ' Step 1: Read and process SOC recording metadata file
                    ReadSocRecordingMetadata()

                    ' Step 2: Update the JSON file path based on availability
                    UpdateJsonFilePath()

                    ' Step 3: Run the Robocopy command with the determined parameters
                    RunRobocopy(ROBOCOPY_SSD_SCRIPT, LEGACY_DATA_SOURCE, SYNC_RAID_PATH, strSSDDataFolder, False)


                    ' Step 4: Pause before the next iteration
                    Thread.Sleep(SLEEP_FOR) ' Ensure this is in milliseconds

        Catch ex As Exception
        HandleError($"Error in MainLoopWorker: {ex.Message}")
        Exit Do ' Exit loop if a critical error occurs
        End Try
        Loop
        Finally
        ' Cleanup when the loop ends
        WriteLog("MainLoopWorker thread terminated")
        End Try
    End Sub

    ' Method to stop the main loop gracefully
    Private Sub StopMainLoop()
        keepRunning = False
        If _syncThread IsNot Nothing AndAlso _syncThread.IsAlive Then
            ' Wait for thread to finish, but don't hang forever
            _syncThread.Join(5000) ' Wait up to 5 seconds
        End If
    End Sub

    ' Read SOC Recording Metadata File (soc_recording_metadata.json)
    Private Sub ReadSocRecordingMetadata()
        jsonConfigurationFileExists = File.Exists(RAID_JSON_FILE)
        If Not jsonConfigurationFileExists Then
            HandleError("SOC recording metadata file not found.")
            strRecording = "R0"
            strSSDDataFolder = SYNC_SSD_DRIVE & LEGACY_PATH
            Return
        End If

        Dim jsonString As String = File.ReadAllText(RAID_JSON_FILE)
        Dim jsonDoc = JObject.Parse(jsonString)

        Dim recordingPath As JToken
        If jsonDoc.TryGetValue("recordingPath", recordingPath) Then
            strRAIDFolder = recordingPath.ToString().Replace("/", "\") & "\mcu"
            strSSDDataFolder = Path.Combine(SYNC_SSD_DRIVE, strRAIDFolder)
        Else
            strSSDDataFolder = Path.Combine(SYNC_SSD_DRIVE, LEGACY_PATH)
        End If

        Dim isRecordingToken As JToken
        Dim isRecording As Boolean = False

        If jsonDoc.TryGetValue("isRecording", isRecordingToken) AndAlso Boolean.TryParse(isRecordingToken.ToString(), isRecording) AndAlso isRecording Then
            strRecording = "R1"
        Else
            strRecording = "R0"
        End If

    End Sub

    ' Update the JSON file path based on availability
    Private Sub UpdateJsonFilePath()
        Dim currentState = If(jsonConfigurationFileExists, If(strRecording = "R1", "Vistool RAID Online, Recording", "Vistool RAID Online, not Recording"), "Vistool RAID Offline")
        If currentState <> lastLoggedState Then
            WriteLog($"{DateTime.Now}: {currentState} - destination path: {strSSDDataFolder}")
            lastLoggedState = currentState
        End If
    End Sub

    ' Ensure Drive Exists
    Private Sub EnsureDriveExists(drivePath As String, driveType As String)
        Dim userChoice As DialogResult

        Do While Not DoesDriveExist(drivePath)
            Dim msg As String = $"{driveType} Drive {drivePath} not found. Do you want to continue waiting?"
            userChoice = MessageBox.Show(msg,
                                         $"{driveType} Drive Missing",
                                         MessageBoxButtons.YesNo,
                                         MessageBoxIcon.Question)

            If userChoice = DialogResult.No Then
                HandleError($"{driveType} Drive {drivePath} does not exist. Exiting.")
                Dispose()
                Exit Sub
            End If
            Thread.Sleep(5000) ' 5 seconds delay before retry
        Loop
    End Sub

    Private Function DoesDriveExist(drivePath As String) As Boolean
        Try
            ' Extract drive letter from path (e.g., "D:\path" -> "D:\")
            Dim driveLetter As String = Path.GetPathRoot(drivePath)
            If String.IsNullOrEmpty(driveLetter) Then Return False

            ' Check if the drive exists using DriveInfo
            Return DriveInfo.GetDrives().Any(Function(d) d.Name.Equals(driveLetter, StringComparison.OrdinalIgnoreCase))
        Catch ex As Exception
            HandleError($"Error checking drive existence: {ex.Message}")
            Return False
        End Try
    End Function

    ' Run Robocopy
    Private Sub RunRobocopy(scriptPath As String, source As String, destination As String, sync_raid_path As String, isRaidSync As Boolean)
        EnsureDriveExists(SYNC_SSD_DRIVE, "SSD")
        Dim rcErrorLevel As Integer = Shell($"cmd /c {scriptPath} ""{source}"" ""{destination}""", AppWinStyle.Hide, True)
        HandleRobocopyResult(rcErrorLevel, destination, isRaidSync)
    End Sub

    ' Handle Robocopy Return Codes
    Private Sub HandleRobocopyResult(rcErrorLevel As Integer, destination As String, isRaidSync As Boolean)
        Select Case rcErrorLevel
            Case 16 : HandleErrorAndPrompt("Serious error during Robocopy to " & destination & ". Check drive or permissions. Code: " & rcErrorLevel)
            Case 8 : HandleErrorAndPrompt("Critical failure during Robocopy to " & destination & ". Code: " & rcErrorLevel)
            Case 7, 3 : LogSuccess("Files copied to " & destination & " with mismatches or extra files present.  Code: " & rcErrorLevel)
            Case 6 : WriteLog("Additional files and mismatched files exist. No files were copied to " & destination & " and no failures were encountered. Code: " & rcErrorLevel)
            Case 5 : LogSuccess("Some files were copied to " & destination & ". Some files were mismatched. No failure was encountered. Code: " & rcErrorLevel)
            Case 4 : WriteLog("Some Mismatched files or directories were detected on " & destination & ". Examine the output log. Housekeeping might be required. Code: " & rcErrorLevel)
            Case 2 : WriteLog("Some Extra files or directories were detected. No files were copied to " & destination & ". Code:" & rcErrorLevel)
            Case 1 : LogSuccess("Files successfully copied to " & destination & ". :" & rcErrorLevel)
            Case 0 : WriteLog("No copying was done - data already synchronized to " & destination & ". No errors occurred. :" & rcErrorLevel)
            Case Else : WriteLog("Unknown Robocopy return code while copying to " & destination & " Code: " & rcErrorLevel)
        End Select

        ' Check if data has changed and we're not syncing to RAID
        If dataChanged And (Not isRaidSync) And strRecording <> "R0" Then
            ' Run Robocopy to RAID
            RunRobocopyToRAID()
        End If
    End Sub

    ' Run Robocopy to RAID
    Private Sub RunRobocopyToRAID()
        RunRobocopy(ROBOCOPY_RAID_SCRIPT, SYNC_SSD_DRIVE & strRAIDFolder, SYNC_RAID_PATH, strRAIDFolder, True)
    End Sub

    ' Log success message
    Private Sub LogSuccess(msg As String)
        WriteLog($"System time: {Now}: Status: {msg}")
        dataChanged = True
    End Sub

    ' Handle Robocopy Errors and Prompt User
    Private Sub HandleErrorAndPrompt(msg As String)
        WriteLog($"System time: {Now}: ERROR: {msg}")
        Dim userChoice As MsgBoxResult = MsgBox($"{msg} Stop synchronization script?", CType(MsgBoxStyle.YesNo + MsgBoxStyle.Question, MsgBoxStyle), "Error Detected")
        If userChoice = MsgBoxResult.Yes Then keepRunning = False
    End Sub

    ' Debug logging
    Private Sub DebugLog(sEntry As String)
        If DEBUG_MODE Then
            Console.WriteLine($"DEBUG: {sEntry}")
            WriteLog($"systime: {Now}: DEBUG: {sEntry}")
        End If
    End Sub

    ' Write log entries with error handling
    Private Sub WriteLog(sEntry As String)
        Try
            ' Ensure the log file path is set and the log file is initialized
            If String.IsNullOrEmpty(strLogFilePath) OrElse Not File.Exists(strLogFilePath) Then
                InitializeLogFile()
            End If

            ' If the log file path is valid, write to the log
            If Not String.IsNullOrEmpty(strLogFilePath) Then
                Using objLogFile As StreamWriter = New StreamWriter(strLogFilePath, True)
                    objLogFile.WriteLine(sEntry)
                End Using
            Else
                HandleError($"Log file path is not set or file is not accessible. Entry skipped: {sEntry}")
            End If
        Catch ex As Exception
            HandleError($"Failed to write to log file: {ex.Message}")
        End Try
    End Sub



    '' Handle errors
    'Private Sub HandleError(msg As String)
    '    WriteLog($"System time: {Now}: ERROR: {msg}")
    'End Sub

    ' Handle Error
    Private Sub HandleError(msg As String)
        WriteLog("ERROR: " & msg)
        MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub
    ' Update Cleanup to handle thread shutdown
    Private Sub Cleanup()
        Try
            ' Stop the main loop thread
            StopMainLoop()

            ' Close and dispose objLogFile if applicable
            If objLogFile IsNot Nothing Then
                objLogFile.Close()
                objLogFile = Nothing
            End If

            ' Clean up other disposable objects
            If objFSO IsNot Nothing Then
                Marshal.ReleaseComObject(objFSO)
                objFSO = Nothing
            End If

            ' Ensure any other COM objects are released
            If objWMIService IsNot Nothing Then
                Marshal.ReleaseComObject(objWMIService)
                objWMIService = Nothing
            End If

            If objShell IsNot Nothing Then
                Marshal.ReleaseComObject(objShell)
                objShell = Nothing
            End If

            If objNetwork IsNot Nothing Then
                Marshal.ReleaseComObject(objNetwork)
                objNetwork = Nothing
            End If
        Catch ex As Exception
            HandleError($"Failed to close all objects: {ex.Message}")
        End Try
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _disposed Then
            If disposing Then
                ' Clean up managed resources
                StopMainLoop()
                Cleanup()
            End If
            _disposed = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overrides Sub Finalize()
        Dispose(False)
        MyBase.Finalize()
    End Sub


End Class
