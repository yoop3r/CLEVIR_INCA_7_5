Option Strict Off

Imports System.Diagnostics
Imports System.IO
Imports System.Threading

Module CaNalyzerStuff

    'This module contains routines related to CANalyzer, starting, stopping, etc.

    'The app object is defined as a generic object rather than CANalyzer.Application because we need to be able to use
    'one of two different versions of CANalyzer.  If we include the Canalyzer Type library in this project which would allow
    'us to "early bind" to the CANalyzer.application object, we cannot differentiate between canalyzer versions by referencing 
    'different type libraries.  Since they have the exact same name, we can only reference one at a time in the app. so we have
    ' no way of "switching" between the two based on version.  So, we "Late Bind" by defining app as a generic object and set
    'app = CreateObject("CANalyzer.Application").  In this way, the system will use the latest vector canalyzer interop.dll 
    'available on the PC so there will be no conflict.  If only Canalyzer 8.5.62 is installed, that is what will be used, if 9.0
    ' is installed (even if 8.5.62 is also installed) the system will use 9.0.  The only way this becomes as issue is if 
    'someone who has both 8.5.62 and 9.0 installed wishes to use 8.5.62.

    'Public MyGmIncaComm As GM_INCA_CommClass
    Private CanalyzerApp As Object = Nothing 'CANalyzer.Application
    Public CanalyzerCaptureStarted As Boolean 'Set to True when Canalyzer is first launched...
    Public CheckCanAlyzerMessageDisplayed As Boolean

    Private Sub CopyFlexRayFileToFinalPath()
        ' Called from StopCanalyzer - Builds the correct filenames for the CANalyzer produced files and copies them to the
        ' correct directory (the same directory that stores the current recording session INCA files).

        Dim ascfilename As String = String.Empty
        Dim blffilename As String = String.Empty
        Dim mdffilename As String = String.Empty
        Dim mf4Filename As String = String.Empty

        Try
            HandleUserMessageLogging("GMRC", "CopyCANalyzerFileToFinalPath: CANalyzer Stopped - Copying File(s) to final path...")

            ' Use the new sequencing method to get current active sequence
            Dim currentActiveSequence As String = GetCurrentActiveSequence()
            Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

            HandleUserMessageLogging("GMRC", $"CopyCANalyzerFileToFinalPath: Using current sequence {currentSeq:D2} from {currentActiveSequence}")

            ' Build the base filename from the current active sequence (without extension)
            Dim baseFileName As String = Path.GetFileNameWithoutExtension(currentActiveSequence)

            ' Build CANalyzer filenames using the current sequence number
            ascfilename = Path.Combine(FinalPathToSaveData, $"{baseFileName}_{AlternateRecordingMode}.asc")
            blffilename = Path.Combine(FinalPathToSaveData, $"{baseFileName}_{AlternateRecordingMode}.blf")
            mdffilename = Path.Combine(FinalPathToSaveData, $"{baseFileName}_{AlternateRecordingMode}.mdf")
            mf4Filename = Path.Combine(FinalPathToSaveData, $"{baseFileName}_{AlternateRecordingMode}.mf4")

            HandleUserMessageLogging("GMRC", $"CopyCANalyzerFileToFinalPath: Target filenames - ASC: {Path.GetFileName(ascfilename)}, BLF: {Path.GetFileName(blffilename)}, MDF: {Path.GetFileName(mdffilename)}, MF4: {Path.GetFileName(mf4Filename)}")

            ' Now handle the CANalyzer data files, if any.
            Dim dataFilesPath As String = Path.Combine(My.Application.Info.DirectoryPath, "CANalyzer", "DataFiles")
            Dim fileFound As Boolean = False

            If Directory.Exists(dataFilesPath) Then
                Dim files As String() = Directory.GetFiles(dataFilesPath)
                For Each sfile As String In files
                    Dim fileName As String = Path.GetFileName(sfile).ToUpper()
                    WaitForFileAvailability(sfile)

                    Select Case fileName
                        Case "CANALYZERDATA.ASC"
                            File.Copy(sfile, ascfilename, overwrite:=True)
                            File.Delete(sfile)
                            fileFound = True
                            HandleUserMessageLogging("GMRC", $"CopyCANalyzerFileToFinalPath: Copied ASC file to {Path.GetFileName(ascfilename)}")
                        Case "CANALYZERDATA.BLF"
                            File.Copy(sfile, blffilename, overwrite:=True)
                            File.Delete(sfile)
                            fileFound = True
                            HandleUserMessageLogging("GMRC", $"CopyCANalyzerFileToFinalPath: Copied BLF file to {Path.GetFileName(blffilename)}")
                        Case "CANALYZERDATA.MDF"
                            File.Copy(sfile, mdffilename, overwrite:=True)
                            File.Delete(sfile)
                            fileFound = True
                            HandleUserMessageLogging("GMRC", $"CopyCANalyzerFileToFinalPath: Copied MDF file to {Path.GetFileName(mdffilename)}")
                        Case "CANALYZERDATA.MF4"
                            File.Copy(sfile, mf4Filename, overwrite:=True)
                            File.Delete(sfile)
                            fileFound = True
                            HandleUserMessageLogging("GMRC", $"CopyCANalyzerFileToFinalPath: Copied MF4 file to {Path.GetFileName(mf4Filename)}")
                        Case Else
                            ' Unrecognized file, remove it.
                            File.Delete(sfile)
                    End Select
                Next
            End If

            ' If no CANalyzer record files were found and copied, log a message.
            If Not fileFound Then
                HandleUserMessageLogging("GMRC", "No CANalyzer Record File Found.", DisplayMsgBox)
            Else
                HandleUserMessageLogging("GMRC", $"CopyCANalyzerFileToFinalPath: Successfully copied CANalyzer files for sequence {currentSeq:D2}")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CopyCANalyzerFileToFinalPath: {ex.Message}")
        End Try
    End Sub

    ' Helper Function to Wait for File Availability
    Private Sub WaitForFileAvailability(filePath As String)
        While FileInUse(filePath)
            Thread.Sleep(100)
        End While
    End Sub

    ' Helper Function to Check if File is in Use
    Private Function FileInUse(filePath As String) As Boolean
        Try
            Using fs As FileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None)
                ' If we can open exclusively, the file is not in use.
            End Using
            Return False
        Catch
            Return True
        End Try
    End Function

    Public Sub ReadClearCodesConfig(ByRef delayAfterStart As Long, ByRef delayAfterStop As Long)
        ' Reads the ClearCodesDelayTimes.txt file and extracts the DelayAfterStart and DelayAfterStop
        ' settings using modern System.IO methods.

        Dim configFileName As String = Path.Combine(My.Application.Info.DirectoryPath, "ClearCodesDelayTimes.txt")

        ' Set default values
        delayAfterStart = 10000
        delayAfterStop = 30000

        If Not File.Exists(configFileName) Then
            HandleUserMessageLogging("GMRC", "ReadClearCodesConfig: ClearCodesDelayTimes.txt file not found. Using default values.", DisplayMsgBox)
            Return
        End If

        Try
            Dim lines As String() = File.ReadAllLines(configFileName)

            If lines.Length > 0 Then
                Dim parts = lines(0).Split(vbTab)
                If parts.Length > 1 Then
                    Long.TryParse(parts(1), delayAfterStart)
                End If
            End If

            If lines.Length > 1 Then
                Dim parts = lines(1).Split(vbTab)
                If parts.Length > 1 Then
                    Long.TryParse(parts(1), delayAfterStop)
                End If
            End If

            If lines.Length > 2 Then
                HandleUserMessageLogging("GMRC", "ReadClearCodesConfig: There appear to be extra lines in the ClearCodesDelayTimes.txt file.")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Error reading ClearCodesDelayTimes.txt: {ex.Message}", DisplayMsgBox)
        End Try
    End Sub

    Public Sub ClearCodesWithCaNalyzer()

        'Called when user presses the Clear Codes button on the login screen...

        'If the in vehicle PC is set up 
        'to clear codes using CANalyzer (ClearCodes.cfg file in the proper location),
        'ReadClearCodesConfig is called which reads in DelayAFterStart and DelayAFterStop times.  Then CANalyzer is launched,
        'and runs for DelayAFterStart number of seconds, then CANalyzer is stopped and a timer is
        'started which will run for DelayAfterStop time.

        'CANalyzer configuration is set up to use the IG block to send clear codes to FO and HS....

        Dim delayAfterStart As Long
        Dim delayAfterStop As Long

        Try

            If File.Exists(My.Application.Info.DirectoryPath & "\CANalyzer\ClearCodes.cfg") Then 'And Debugger.IsAttached = False Then

                MsgBox("Please make sure the vehicle is in Run prior to continuing.")

                ReadClearCodesConfig(delayAfterStart, delayAfterStop)

                HandleUserMessageLogging("GMRC", "Using CANalyzer to Clear Codes, Please wait...",,, FlashMsgOn)

                If CanalyzerApp Is Nothing Then

                    HandleUserMessageLogging("GMRC", "ClearCodesWithCANalyzer: Launching Canalyzer...")
                    CanalyzerApp = CreateObject("CANalyzer.Application")

                    CanalyzerApp.Open(My.Application.Info.DirectoryPath & "\CANalyzer\ClearCodes.cfg", True, False)

                    CanalyzerCaptureStarted = True

                End If

                HandleUserMessageLogging("GMRC", "ClearCodesWithCANalyzer: Clear Codes Canalyzer Measurement Start...")
                CanalyzerApp.Measurement.Start()
                Thread.Sleep(delayAfterStart)

                If CanalyzerApp.measurement.running = True Then

                    HandleUserMessageLogging("GMRC", "ClearCodesWithCANalyzer: Clear Codes Canalyzer Measurement Started - Stopping Measurement.")
                    CanalyzerApp.Measurement.StopEX()
                    Thread.Sleep(delayAfterStop)

                    If CanalyzerApp.measurement.running = False Then

                        HandleUserMessageLogging("GMRC", "ClearCodesWithCANalyzer: Clear Codes Canalyzer Measurement Stopped.")

                    Else

                        HandleUserMessageLogging("GMRC", "ClearCodesWithCANalyzer: Clear Codes Canalyzer Measurement NOT Stopped within " & delayAfterStop \ 100 & " Seconds. Continuing without clearing codes...",,, FlashMsg3Sec)

                        LoginForm.CheckBox3.Checked = False
                        LoginForm.CheckBox3.Visible = False
                        OnVehicleScreen.Label3.Visible = False

                        QuitCanalyzer()
                        Exit Sub

                    End If

                    UserStatusInfo.Hide()

                    HandleUserMessageLogging("GMRC", "Please start the engine before continuing...", DisplayMsgBox)

                Else

                    HandleUserMessageLogging("GMRC", "Clear Codes Canalyzer Measurement NOT Started within " & delayAfterStart \ 100 & " Seconds.  No code clear performed, Terminaing CANalyzer...",,, FlashMsg3Sec)

                    LoginForm.CheckBox3.Checked = False
                    LoginForm.CheckBox3.Visible = False
                    OnVehicleScreen.Label3.Visible = False
                    QuitCanalyzer()

                End If

            End If

            LoginForm.TopMost = True

        Catch ex As Exception

            HandleUserMessageLogging("GMRC", "ClearCodesWithCANalyzer: " & ex.Message & " Continuing without clearing codes...",,, FlashMsg3Sec)

            LoginForm.CheckBox3.Checked = False
            LoginForm.CheckBox3.Visible = False
            OnVehicleScreen.Label3.Visible = False
            QuitCanalyzer()

            LoginForm.TopMost = True

        Finally
            UserStatusInfo.Hide()
        End Try

    End Sub

    Public Sub StopCanalyzer()
        ' Called from StartStopMeasurement and StopRecording:
        ' Stops measurement in CANalyzer by sending the StopEX command via the Vector CANalyzer API interface.
        ' Also copies the CANalyzer-generated data files to the proper directory.
        Try
            If CanalyzerApp IsNot Nothing Then
                If CanalyzerApp.Measurement.Running Then
                    ' Get current sequence information before stopping
                    Dim currentActiveSequence As String = GetCurrentActiveSequence()
                    Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

                    ' Log the stop event with sequence information
                    MyIncaInterface.WriteEventComment($"{DateTime.Now:HH:mm:ss} Stopping CANalyzer Replay Block Playback for sequence {currentSeq:D2}...", True)

                    ' Try StopEX multiple times to handle intermittent COM/API failures
                    Dim maxAttempts As Integer = 5
                    Dim attempt As Integer = 0
                    Dim stopped As Boolean = False

                    While attempt < maxAttempts AndAlso CanalyzerApp.Measurement.Running
                        attempt += 1
                        Try
                            HandleUserMessageLogging("GMRC", $"StopCanalyzer: Attempting StopEX (Attempt {attempt} of {maxAttempts})...")
                            CanalyzerApp.Measurement.StopEX()
                        Catch ex As Exception
                            HandleUserMessageLogging("GMRC", $"StopCanalyzer: StopEX threw on attempt {attempt}: {ex.Message}")
                        End Try

                        ' Give CANalyzer some time to stop before checking
                        Thread.Sleep(1500)

                        Try
                            If Not CanalyzerApp.Measurement.Running Then
                                stopped = True
                                Exit While
                            End If
                        Catch ex As Exception
                            ' If querying Running throws, log and continue retrying
                            HandleUserMessageLogging("GMRC", $"StopCanalyzer: Error checking Measurement.Running on attempt {attempt}: {ex.Message}")
                        End Try
                    End While

                    If stopped OrElse Not CanalyzerApp.Measurement.Running Then
                        HandleUserMessageLogging("GMRC", "StopCanalyzer: CANalyzer stopped successfully - Copying File(s) to final path...")
                        CopyFlexRayFileToFinalPath()
                        Return
                    End If

                    ' If we reach here, StopEX did not stop the measurement after retries.
                    HandleUserMessageLogging("GMRC", $"StopCanalyzer: CANalyzer measurement still running after {maxAttempts} StopEX attempts.",,, FlashMsg3Sec)
                    OnVehicleScreen.Label3.BackColor = Color.Red

                    ' Inform the user that StopEX failed and a restart may be required
                    MsgBox("There was a problem stopping CANalyzer (StopEX failed). The application will attempt to recover files; you may need to restart CLEVIR if problems persist.", MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "CANalyzer Stop Failed")

                    ' Force a graceful quit to ensure files are written. QuitCanalyzer will attempt Quit() then kill the process.
                    Try
                        HandleUserMessageLogging("GMRC", "StopCanalyzer: Forcing CANalyzer to quit to flush files...")
                        QuitCanalyzer()
                        ' Give the process a moment to terminate and flush files
                        Thread.Sleep(2000)
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", $"StopCanalyzer: Forced quit failed: {ex.Message}")
                    End Try

                    ' Attempt to copy files even if we couldn't stop cleanly
                    Try
                        CopyFlexRayFileToFinalPath()
                    Catch ex As Exception
                        HandleUserMessageLogging("GMRC", $"StopCanalyzer: Failed copying files after forced quit: {ex.Message}")
                    End Try

                    Return
                Else
                    ' Log if the measurement is not running
                    HandleUserMessageLogging("GMRC", "CANalyzer measurement is not running. StopEX command was not sent.",,, FlashMsg3Sec)
                    OnVehicleScreen.Label3.BackColor = Color.Red
                    Return
                End If
            End If
        Catch ex As Exception
            ' Unexpected error: attempt to force stop/quit and copy files
            HandleUserMessageLogging("GMRC", $"StopCanalyzer: ERROR: {ex.Message} Attempting forced quit and file copy...")
            Try
                ' Try to stop once more
                Try
                    CanalyzerApp?.Measurement.StopEX()
                Catch innerEx As Exception
                    HandleUserMessageLogging("GMRC", $"StopCanalyzer: StopEX during exception handling threw: {innerEx.Message}")
                End Try

                Thread.Sleep(1500)

                If CanalyzerApp IsNot Nothing AndAlso CanalyzerApp.Measurement.Running Then
                    QuitCanalyzer()
                    Thread.Sleep(2000)
                End If

                CopyFlexRayFileToFinalPath()
            Catch finalEx As Exception
                HandleUserMessageLogging("GMRC", $"StopCanalyzer: Final cleanup failed: {finalEx.Message}",,, FlashMsg3Sec)
                OnVehicleScreen.Label3.BackColor = Color.Red
                LoginForm.CheckBox3.Checked = False

                ' Inform the user that CANalyzer could not be stopped and a restart may be required
                MsgBox("CANalyzer could not be stopped cleanly and final cleanup failed. Restarting CLEVIR may be required to recover CANalyzer files.", MsgBoxStyle.OkOnly Or MsgBoxStyle.Exclamation, "CANalyzer Stop Failed - Manual Intervention Recommended")

                QuitCanalyzer()
                Return
            End Try
        End Try
        Return
    End Sub

    Public Sub QuitCanalyzer()
        ' Called from LaunchCanalyzer, ClearCodes, and ExitApp:
        ' Closes CANalyzer and sets the CANalyzer app object to Nothing.
        Dim canalyzerProcess As Process()
        Try
            If CanalyzerApp IsNot Nothing Then
                HandleUserMessageLogging("GMRC", "QuitCanalyzer: Quitting CANalyzer...",,, FlashMsgOn)
                ' Attempt to quit CANalyzer
                CanalyzerApp.Quit()
                CanalyzerApp = Nothing
                CanalyzerCaptureStarted = False
                HandleUserMessageLogging("GMRC", "QuitCanalyzer: CANalyzer quit successfully.")
                Return
            Else
                HandleUserMessageLogging("GMRC", "QuitCanalyzer: CANalyzer not running.")
                Return
            End If
        Catch ex As Exception
            ' Log the exception
            HandleUserMessageLogging("GMRC", "QuitCanalyzer: " & ex.Message)
            ' Attempt to kill the CANalyzer process if quitting fails
            canalyzerProcess = Process.GetProcessesByName("CANw64")
            If canalyzerProcess.Length > 0 Then
                Thread.Sleep(1000)
                HandleUserMessageLogging("GMRC", "QuitCanalyzer: Killing CANalyzer...")
                Try
                    canalyzerProcess(0).Kill()
                    HandleUserMessageLogging("GMRC", "QuitCanalyzer: CANalyzer killed successfully.")
                    CanalyzerApp = Nothing
                    CanalyzerCaptureStarted = False
                    Return
                Catch killEx As Exception
                    HandleUserMessageLogging("GMRC", "QuitCanalyzer: Failed to kill CANalyzer - " & killEx.Message)
                    Return
                End Try
            Else
                HandleUserMessageLogging("GMRC", "QuitCanalyzer: No CANalyzer process found to kill.")
                Return
            End If
        Finally
            ' Ensure the user status UI is hidden
            UserStatusInfo.Hide()
        End Try
    End Sub

    Public Function LaunchCanalyzer() As String
        Dim launchCanalyzerReturnString As String = "Success"
        Dim caNalyzerStartDelayTimeMsec As Integer = 40000
        Dim baseDataPath As String = Path.Combine(My.Application.Info.DirectoryPath, "CANalyzer", "DataFiles")
        Dim baseConfigPath As String = Path.Combine(My.Application.Info.DirectoryPath, "CANalyzer")
        Dim dataFiles As String() = {
            "canalyzerdata.asc", "canalyzerdata.blf", "CanalyzerData.mdf",
            "CanalyzerData.mf4", "canalyzerfodata.asc", "CanalyzerfoData.blf"
        }
        Dim caNalyzerStartStopWatch As New Stopwatch()
        Try
            ' Delete old data files with retry logic
            For Each filename In dataFiles
                Dim filePath As String = Path.Combine(baseDataPath, filename)
                Dim retries As Integer = 5
                While retries > 0 AndAlso File.Exists(filePath)
                    Try
                        File.Delete(filePath)
                        Exit While
                    Catch ex As IOException
                        retries -= 1
                        If retries = 0 Then
                            Throw New IOException("Failed to delete the file after multiple retries - ensure CANalyzer is not recording.  'Yes' to continue without CANalyzer, or 'No' to exit CLEVIR.", ex)
                        End If
                        ' Synchronous delay
                        Thread.Sleep(100)
                    End Try
                End While
            Next
            ' Create the CANalyzer application object
            'Dim canalyzerApp As Object = Nothing
            Try
                Dim type As Type = Type.GetTypeFromProgID("CANalyzer.Application")
                CanalyzerApp = Activator.CreateInstance(type)
            Catch ex As Exception
                launchCanalyzerReturnString = "Failed to create CANalyzer object. Ensure CANalyzer is installed."
                Return launchCanalyzerReturnString
            End Try
            ' Load configuration file
            If CanalyzerApp IsNot Nothing Then
                Dim configFilePath As String = Path.Combine(baseConfigPath, AlternateRecordConfig & ".cfg")
                CanalyzerApp.Open(configFilePath, True, False)
                CanalyzerCaptureStarted = True
            Else
                launchCanalyzerReturnString = "CANalyzer object is null. Configuration failed."
                Return launchCanalyzerReturnString
            End If
            ' Load delay time from file if available
            Dim delayFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "CANalyzerStartDelayTimeMSec.txt")

            If File.Exists(delayFilePath) Then
                Dim textline As String = File.ReadAllText(delayFilePath)
                ' Split on any whitespace (spaces, tabs, etc.)
                Dim parts = textline.Split()
                If parts.Length > 1 Then
                    ' The last part should contain the numeric value
                    If Integer.TryParse(parts(parts.Length - 1), caNalyzerStartDelayTimeMsec) = False Then
                        caNalyzerStartDelayTimeMsec = 40000 ' Default if parsing fails
                    End If
                Else
                    caNalyzerStartDelayTimeMsec = 40000 ' Default if no value found
                End If
            End If
            ' Initial delay before starting CANalyzer
            Thread.Sleep(2000)
            ' Start CANalyzer measurement
            caNalyzerStartStopWatch.Start()
            CanalyzerApp.Measurement.Start()
            ' Wait for CANalyzer to start or timeout
            Dim elapsedTime As Integer = 0
            While Not CanalyzerApp.Measurement.Running AndAlso elapsedTime < caNalyzerStartDelayTimeMsec
                Thread.Sleep(500)
                elapsedTime += 500
            End While
            If Not CanalyzerApp.Measurement.Running Then
                launchCanalyzerReturnString = "CANalyzer failed to start within the timeout period."
                Return launchCanalyzerReturnString
            End If
            ' Stop CANalyzer measurement and verify
            CanalyzerApp.Measurement.StopEX()
            Thread.Sleep(2000)
            If Not CanalyzerApp.Measurement.Running Then
                launchCanalyzerReturnString = "Success"
            Else
                launchCanalyzerReturnString = "CANalyzer failed to stop measurement. Manual intervention required."
            End If
        Catch ex As Exception
            launchCanalyzerReturnString = $"Error: {ex.Message}"
        Finally
            ' Clean up UI and CANalyzer state
            UserStatusInfo.Hide()
            If launchCanalyzerReturnString <> "Success" Then
                LoginForm.CheckBox3.Checked = False
                LoginForm.CheckBox3.Visible = False
                OnVehicleScreen.Label3.Visible = False
                QuitCanalyzer()
            End If
        End Try
        Return launchCanalyzerReturnString
    End Function

    Public Sub StartCanalyzer()
        ' Starts CANalyzer measurement.
        ' If CANalyzer is not already initialized, it will be launched and configured first.
        Dim logMessage As String = "Success"
        Try
            ' Check if CANalyzer is available
            If AlternateRecordingMode = "None" Then
                HandleUserMessageLogging("GMRC", "CANalyzer not available on this PC", DisplayMsgBox)
                Return
            End If
            ' Prevent duplicate message display
            If CheckCanAlyzerMessageDisplayed Then Return

            ' Initialize CANalyzer if not already running
            If CanalyzerApp Is Nothing Then
                HandleUserMessageLogging("GMRC", "StartCanalyzer: Launching CANalyzer for the first time...")
                ' Create CANalyzer application object using CreateObject
                Try
                    Dim type As Type = Type.GetTypeFromProgID("CANalyzer.Application")
                    CanalyzerApp = Activator.CreateInstance(type)
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"StartCanalyzer: Failed to create CANalyzer object. {ex.Message}", DisplayMsgBox)
                    Return
                End Try
                ' Open configuration file
                Dim configFilePath As String = Path.Combine(My.Application.Info.DirectoryPath, "CANalyzer", AlternateRecordConfig & ".cfg")
                CanalyzerApp.Open(configFilePath, True, False)
                ' Delete old data files
                Dim dataFiles As String() = {
                    "CanalyzerData.asc", "CanalyzerData.blf", "CanalyzerData.mdf",
                    "CanalyzerData.mf4", "CanalyzerfoData.asc", "canalyzerdata.asc", "canalyzerdata.blf"
                }
                For Each fileName In dataFiles
                    Dim filePath As String = Path.Combine(My.Application.Info.DirectoryPath, "CANalyzer", "DataFiles", fileName)
                    DeleteFileWithRetry(filePath)
                Next
                ' Initial delay before starting CANalyzer
                Thread.Sleep(2000)
                CanalyzerCaptureStarted = True
            Else
                HandleUserMessageLogging("GMRC", $"StartCanalyzer: CANalyzer configuration {AlternateRecordingMode}.cfg already open ...")
            End If

            ' Start the measurement
            HandleUserMessageLogging("GMRC", "StartCanalyzer: Starting measurement...")
            CanalyzerApp.Measurement.Start()
            Thread.Sleep(2000) ' Give it time to start

            If CanalyzerApp.Measurement.Running Then
                ' Get current sequence information for logging
                Dim currentActiveSequence As String = GetCurrentActiveSequence()
                Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)
                MyIncaInterface.WriteEventComment($"{DateTime.Now:HH:mm:ss} CANalyzer Measurement Started for sequence {currentSeq:D2}...", True)
                OnVehicleScreen.Label3.BackColor = Color.Green
            Else
                logMessage = "Unable to start CANalyzer measurement. No CANalyzer data will be available for this session."
            End If

            ' Handle any error messages
            If Not String.IsNullOrEmpty(logMessage) AndAlso logMessage <> "Success" Then
                HandleUserMessageLogging("GMRC", logMessage,,, FlashMsg3Sec)
                OnVehicleScreen.Label3.BackColor = Color.Red
                LoginForm.CheckBox3.Checked = False
            End If
        Catch ex As Exception
            ' Handle exceptions
            logMessage = $"StartCanalyzer: {ex.Message} Unable to start CANalyzer measurement. No CANalyzer data will be available for this session."
            HandleUserMessageLogging("GMRC", logMessage,,, FlashMsg3Sec)
            OnVehicleScreen.Label3.BackColor = Color.Red
            LoginForm.CheckBox3.Checked = False
        End Try
        Return
    End Sub

    ' Helper method to delete a file with retry logic
    Private Sub DeleteFileWithRetry(filePath As String)
        Dim retries As Integer = 5
        While retries > 0 AndAlso File.Exists(filePath)
            Try
                File.Delete(filePath)
                Return
            Catch ex As IOException
                retries -= 1
                If retries = 0 Then
                    Return
                End If
                ' Synchronous delay:
                Thread.Sleep(100)
            End Try
        End While
        ' If the file does not exist, return a message
        Return
    End Sub


End Module
