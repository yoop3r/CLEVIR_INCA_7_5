Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading

Module SyncScript
    ' Object Declarations
    Dim objShell As Object = CreateObject("WScript.Shell")
    Dim objNetwork As Object = CreateObject("WScript.Network")
    Dim strLogFilePath As String
    Dim strLogName As String
    Dim dataChanged As Boolean = False
    Dim lastLoggedState As String = ""
    Dim keepRunning As Boolean = True

    ' Configuration Variables
    Dim RAID_JSON_FILE, LEGACY_JSON_FILE, LEGACY_PATH, SYNC_RAID_PATH, ROBOCOPY_SSD_SCRIPT, ROBOCOPY_RAID_SCRIPT, SYNC_SSD_DRIVE, SYNC_RAID_DRIVE, LEGACY_DATA_SOURCE, LOG_PATH, SYNC_TO_RAID_DATA_SOURCE As String
    Dim DEBUG_MODE As Boolean
    Dim SLEEP_FOR As Integer

    Const CONFIG_FILE_PATH As String = "C:\CSVScripts\sync_config.json" ' Path to the script configuration file

    Sub Main()
        Dim userChoice As MsgBoxResult = MsgBox("Start Data Synchronization Script?", MsgBoxStyle.YesNo + MsgBoxStyle.Question, "Start Script")
        If userChoice = MsgBoxResult.Yes Then
            ReadJsonConfigFile(CONFIG_FILE_PATH)
            InitializeLogFile()
            MainLoop()
        Else
            MsgBox("Script not started.", MsgBoxStyle.Information, "Script Stopped")
        End If

        Cleanup()
    End Sub

    ' Read Configuration File (sync_config.json)
    Sub ReadJsonConfigFile(configFilePath As String)
        If File.Exists(configFilePath) Then
            Dim jsonString As String = File.ReadAllText(configFilePath)
            ParseSyncConfig(jsonString)
        Else
            HandleMissingConfig(configFilePath)
        End If
    End Sub

    ' Parse sync_config.json Configurations
    Sub ParseSyncConfig(jsonString As String)
        Dim regex As New Regex("""(raid_json_file|legacy_json_file|legacy_path|sync_raid_path|robocopy_ssd_script|robocopy_raid_script|sync_ssd_drive|sync_raid_drive|legacy_data_source|log_path|sync_to_raid_data_source|debug_mode|sleep_for)""\s*:\s*""([^""]*)""", RegexOptions.IgnoreCase)
        Dim matches As MatchCollection = regex.Matches(jsonString)

        For Each match As Match In matches
            AssignJsonValues(match.Groups(1).Value, match.Groups(2).Value)
        Next
    End Sub

    ' Assign sync_config.json Values to Variables
    Sub AssignJsonValues(key As String, value As String)
        Select Case key
            Case "raid_json_file" : RAID_JSON_FILE = value.Replace("\\", "\")
            Case "legacy_json_file" : LEGACY_JSON_FILE = value.Replace("\\", "\")
            Case "legacy_path" : LEGACY_PATH = value.Replace("\\", "\")
            Case "sync_raid_path" : SYNC_RAID_PATH = value.Replace("\\", "\")
            Case "robocopy_ssd_script" : ROBOCOPY_SSD_SCRIPT = value.Replace("\\", "\")
            Case "robocopy_raid_script" : ROBOCOPY_RAID_SCRIPT = value.Replace("\\", "\")
            Case "sync_ssd_drive" : SYNC_SSD_DRIVE = value.Replace("\\", "\")
            Case "sync_raid_drive" : SYNC_RAID_DRIVE = value.Replace("\\", "\")
            Case "legacy_data_source" : LEGACY_DATA_SOURCE = value.Replace("\\", "\")
            Case "log_path" : LOG_PATH = value.Replace("\\", "\")
            Case "sync_to_raid_data_source" : SYNC_TO_RAID_DATA_SOURCE = value.Replace("\\", "\")
            Case "debug_mode" : DEBUG_MODE = Boolean.Parse(value)
            Case "sleep_for" : SLEEP_FOR = Integer.Parse(value)
        End Select
    End Sub

    ' Initialize the log file with error handling
    Sub InitializeLogFile()
        Dim strDate As String = DateTime.Now.ToString("MMddyyyy")
        Dim strTime As String = DateTime.Now.ToString("HHmmss")
        strLogName = $"{strDate}.{strTime}_{objNetwork.ComputerName}.log"
        strLogFilePath = Path.Combine(LOG_PATH, strLogName)

        ' Ensure the log file directory exists
        If Not Directory.Exists(LEGACY_DATA_SOURCE) Then
            Directory.CreateDirectory(LEGACY_DATA_SOURCE)
        End If

        ' Create or open the log file to ensure it exists, then close it immediately
        Using objLogFile As StreamWriter = New StreamWriter(strLogFilePath, True)
        End Using
    End Sub

    ' Handle Missing Configuration File Scenario
    Sub HandleMissingConfig(configFilePath As String)
        Dim msg As String = $"Configuration file not found at: {configFilePath}{vbCrLf}Do you want to continue?"
        Dim userChoice As MsgBoxResult = MsgBox(msg, MsgBoxStyle.YesNo + MsgBoxStyle.Question, "Config Missing")
        If userChoice = MsgBoxResult.No Then
            HandleError("Script terminated due to missing configuration file.")
            Environment.Exit(0)
        End If
    End Sub

    ' Main Loop
    Sub MainLoop()
        Do While keepRunning
            Try
                ' Step 1: Read and process SOC recording metadata file
                ReadSocRecordingMetadata()

                ' Step 2: Update the JSON file path based on availability
                UpdateJsonFilePath()

                ' Step 3: Run the Robocopy command with the determined parameters
                RunRobocopy(ROBOCOPY_SSD_SCRIPT, LEGACY_DATA_SOURCE, SYNC_SSD_DRIVE & LEGACY_PATH, False)

                ' Step 4: Pause before the next iteration
                Thread.Sleep(SLEEP_FOR) ' Ensure this is in milliseconds

            Catch ex As Exception
                HandleError($"Error: {ex.Message}")
                Exit Do ' Exit loop if a critical error occurs
            End Try
        Loop
    End Sub

    ' Read SOC Recording Metadata File (soc_recording_metadata.json)
    Sub ReadSocRecordingMetadata()
        If File.Exists(RAID_JSON_FILE) Then
            Dim jsonString As String = File.ReadAllText(RAID_JSON_FILE)
            ParseSocRecordingMetadata(jsonString)
        Else
            HandleError("SOC recording metadata file not found.")
        End If
    End Sub

    ' Parse soc_recording_metadata.json
    Sub ParseSocRecordingMetadata(jsonString As String)
        Dim regex As New Regex("""recordingPath""\s*:\s*""([^""]+)""", RegexOptions.IgnoreCase)
        Dim match As Match = regex.Match(jsonString)
        If match.Success Then
            Dim unixPath As String = match.Groups(1).Value
            Dim windowsPath As String = unixPath.Replace("/", "\")
            ' ...existing code...
        End If

        regex = New Regex("""isRecording""\s*:\s*(true|false)\s*,?", RegexOptions.IgnoreCase)
        match = regex.Match(jsonString)
        If match.Success Then
            ' ...existing code...
        End If
    End Sub

    ' Update the JSON file path based on availability
    Sub UpdateJsonFilePath()
        ' ...existing code...
    End Sub

    ' Ensure Drive Exists
    Sub EnsureDriveExists(drivePath As String, driveType As String)
        ' ...existing code...
    End Sub

    ' Run Robocopy
    Sub RunRobocopy(scriptPath As String, sourcePath As String, destinationPath As String, isRaidSync As Boolean)
        EnsureDriveExists(SYNC_SSD_DRIVE, "SSD")
        Dim rcErrorLevel As Integer = Shell($"cmd /c {scriptPath} ""{sourcePath}"" ""{destinationPath}""", AppWinStyle.Hide, True)
        HandleRobocopyResult(rcErrorLevel, destinationPath, isRaidSync)
    End Sub

    ' Handle Robocopy Return Codes
    Sub HandleRobocopyResult(rcErrorLevel As Integer, destinationPath As String, isRaidSync As Boolean)
        ' ...existing code...
    End Sub

    ' Function to delete files from the source directory
    Function DeleteSourceFiles() As Boolean
        ' ...existing code...
    End Function

    ' Run Robocopy to RAID
    Sub RunRobocopyToRAID()
        RunRobocopy(ROBOCOPY_RAID_SCRIPT, SYNC_SSD_DRIVE & LEGACY_PATH, SYNC_RAID_PATH & LEGACY_PATH, True)
    End Sub

    ' Log success message
    Sub LogSuccess(msg As String)
        WriteLog($"System time: {Now}: Status: {msg}")
        dataChanged = True
    End Sub

    ' Handle Robocopy Errors and Prompt User
    Sub HandleErrorAndPrompt(msg As String)
        WriteLog($"System time: {Now}: ERROR: {msg}")
        Dim userChoice As MsgBoxResult = MsgBox($"{msg} Stop synchronization script?", MsgBoxStyle.YesNo + MsgBoxStyle.Question, "Error Detected")
        If userChoice = MsgBoxResult.Yes Then keepRunning = False
    End Sub

    ' Debug logging
    Sub DebugLog(sEntry As String)
        If DEBUG_MODE Then
            Console.WriteLine($"DEBUG: {sEntry}")
            WriteLog($"systime: {Now}: DEBUG: {sEntry}")
        End If
    End Sub

    ' Write log entries with error handling
    Sub WriteLog(sEntry As String)
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

    ' Handle errors
    Sub HandleError(msg As String)
        WriteLog($"System time: {Now}: ERROR: {msg}")
    End Sub

    ' Clean up objects and resources before exiting
    Sub Cleanup()
        ' ...existing code...
    End Sub
End Module
