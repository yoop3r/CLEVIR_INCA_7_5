Option Strict Off

Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Net
Imports System.Net.Sockets

Module VehicleSpyStuff

    'This module contains routines related to Vehicle Spy, starting, stopping, etc.

    'Const VSPY_LAUNCH_WAIT_TIME As Integer = 20
    Private Const VSPY_LAUNCH_WAIT_TIME As Integer = 90
    Private Const VSPY_START_WAIT_TIME As Integer = 15
    Private Const VSPY_STOP_WAIT_TIME As Integer = 10
    Private Const VSPY_CONNECT_RETRY_COUNT As Integer = 5          ' ✅ NEW: Number of connection retry attempts
    Private Const VSPY_CONNECT_RETRY_DELAY_MS As Integer = 2000    ' ✅ NEW: Delay between connection retries
    Private Const VSPY_SOCKET_TIMEOUT_MS As Integer = 5000         ' ✅ NEW: Socket send/receive timeout
    Private Const VSPY_API_READY_WAIT_MS As Integer = 3000         ' ✅ NEW: Wait for API to be ready after process starts
    Private Const VEHICLE_SPY_DIRECTORY As String = "C:\Program Files\Vehicle Spy 3"
    Private _vehicleSpyCacheDirectory As String = Nothing
    Private ReadOnly Property VEHICLE_SPY_CACHE_DIRECTORY As String
        Get
            If _vehicleSpyCacheDirectory Is Nothing Then
                Dim possibleBasePaths() As String = {"C:\IntrepidCS\Vehicle Spy 3", "C:\Eng_Apps\Vehicle Spy 3"}
                For Each basePath In possibleBasePaths
                    Dim fullCachePath = Path.Combine(basePath, "DataCache")
                    If Directory.Exists(fullCachePath) Then
                        _vehicleSpyCacheDirectory = basePath
                        Exit For
                    End If
                Next
                If _vehicleSpyCacheDirectory Is Nothing Then
                    ' Default to the primary location if neither exists
                    _vehicleSpyCacheDirectory = "C:\IntrepidCS\Vehicle Spy 3"
                End If
            End If
            Return _vehicleSpyCacheDirectory
        End Get
    End Property

    Private sock As Socket
    Private myVSPYprocess As Process
    Private ReadOnly VSpyDefaultConfigFileName As String = "IntrepidTest.vs3" ' Default config file that should come with the application - If this file is present, it indicates that the DID Pull functionality is available and properly configured.
    Private EnableDIDPullFunctionality As Boolean '= True

    Public VSpySelectedConfigFileName As String = VSpyDefaultConfigFileName
    Public VehicleSpyCaptureStarted As Boolean 'Set to True when VehicleSpy is first launched...
    Public CheckVSpyMessageDisplayed As Boolean
    Public MAX_NUM_DID_PULL_FILES As Integer = 81
    'Public Const MAX_NUM_DID_PULL_FILES As Integer = 2

    Public FunctionBlockString As String
    Public EnableDIDPull As Boolean
    Public EnableStartZipFileCheck As Boolean
    Public EnableEndZipFileCheck As Boolean
    Public ReadOnly DefaultVSpyDataDirectory As String = "C:\IntrepidCS\Vehicle Spy 3\Data Directory\Default"
    Public DIDPullTriggerZippingKey As String = "5844"

    Public Sub HandleDIDPull()
        ' This routine checks for the existence of the EnableDIDPull.txt file. If it exists, this indicates that
        ' the system is set up to pull DID information. If the user agrees, a command is sent to VSpy to
        ' initiate the DID Pull.
        Try
            ' 1. Pre-condition check
            If Not VehicleSpyCaptureStarted Then
                Return
            End If

            ' 2. Read the DID Pull configuration file if it exists
            Dim didPullConfigFile As String = Path.Combine(My.Application.Info.DirectoryPath, "EnableDIDPull.txt")
            If File.Exists(didPullConfigFile) Then
                ' Use StreamReader for modern, robust file reading.
                Using reader As New StreamReader(didPullConfigFile)
                    FunctionBlockString = reader.ReadLine()

                    Dim maxFilesLine As String = reader.ReadLine()
                    If maxFilesLine IsNot Nothing Then
                        Integer.TryParse(maxFilesLine, MAX_NUM_DID_PULL_FILES)
                    End If

                    Dim triggerKeyLine As String = reader.ReadLine()
                    If triggerKeyLine IsNot Nothing Then
                        DIDPullTriggerZippingKey = triggerKeyLine
                    End If
                End Using
                EnableDIDPullFunctionality = True
            End If

            ' 3. Prompt user and initiate the DID pull if enabled
            If EnableDIDPullFunctionality Then
                EnableDIDPull = (MsgBox("Enable DID Pull at the START and END of this ride?", vbYesNo) = vbYes)

                If EnableDIDPull Then
                    OnVehicleScreen.Button6.Enabled = False ' Start Measurement
                    OnVehicleScreen.Button14.Enabled = False ' Start Record

                    HandleUserMessageLogging("GMRC", "Preparing to read DID Information, please wait...",,, FlashMsgOn)
                    EnableStartZipFileCheck = StartVehicleSpy()

                    If EnableStartZipFileCheck Then
                        HandleUserMessageLogging("GMRC", $"HandleDIDPull: Sending ""{FunctionBlockString}"" command...")
                        If SendVSpyCommand(FunctionBlockString) Then
                            HandleUserMessageLogging("GMRC", $"""{FunctionBlockString}"" command sent successfully - Collecting DID Information...",,, FlashMsgOn)
                        Else
                            HandleUserMessageLogging("GMRC", $"""{FunctionBlockString}"" command failed - NO DID Information will be collected...",,, FlashMsg2Sec)
                            EnableStartZipFileCheck = False
                        End If
                    Else
                        ' Re-enable buttons if the start process failed.
                        OnVehicleScreen.Button6.Enabled = True ' Start Measurement
                        OnVehicleScreen.Button14.Enabled = True ' Start Record
                    End If
                End If
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"HandleDIDPull Exception: {ex.Message}", DisplayMsgBox)
        End Try
    End Sub

    Public Sub ClearCodesWithVSpy()
        Dim DelayAfterStart As Long
        Dim DelayAfterStop As Long
        Dim ExecutableFile As String = Path.Combine(VEHICLE_SPY_DIRECTORY, "vspy3.exe")
        Dim errorfound As Boolean = False

        Try
            MsgBox("Please make sure the vehicle is in Run prior to continuing.")

            ReadClearCodesConfig(DelayAfterStart, DelayAfterStop)

            UserStatusInfo.Label1.Text = "Using VehicleSpy to Clear Codes, Please wait..."
            Threading.Thread.Sleep(2000)

            If LaunchVSpy("ClearCodes") = "Success" Then
                If StartVehicleSpy("ClearCodes") Then
                    Threading.Thread.Sleep(DelayAfterStart)

                    Dim commands = {"dg(dgn0).start", "dg(dgn1).start", "dg(dgn2).start", "dg(dgn3).start"}
                    For Each cmd In commands
                        HandleUserMessageLogging("GMRC", $"ClearCodesWithVSpy: Sending {cmd} command...")
                        If Not SendVSpyCommand(cmd) Then
                            UserStatusInfo.Label1.Text = $"{cmd} command failed"
                            errorfound = True
                            Exit For
                        End If
                        Threading.Thread.Sleep(3000)
                    Next

                    If errorfound Then
                        UserStatusInfo.Label1.Text = "VehicleSpy ClearCodes failed. Continuing without clearing codes..."
                        Threading.Thread.Sleep(3000)
                    End If

                    UserStatusInfo.Hide()

                    HandleUserMessageLogging("GMRC", "ClearCodesWithVSpy: Sending stop command...")
                    If SendVSpyCommand("stop") Then
                        Threading.Thread.Sleep(DelayAfterStop)
                        MsgBox("Please start the engine before continuing...")
                    Else
                        UserStatusInfo.Label1.Text = "VSpy Stop command failed - Clear Codes did not complete successfully..."
                        Threading.Thread.Sleep(3000)
                    End If

                    ' Clear DataCache files
                    Dim dataCachePath = Path.Combine(VEHICLE_SPY_CACHE_DIRECTORY, "DataCache")
                    If Directory.Exists(dataCachePath) Then
                        For Each filePath In Directory.GetFiles(dataCachePath)
                            File.Delete(filePath)
                        Next
                    End If

                Else
                    UserStatusInfo.Label1.Text = "StartVehicleSpy returned false, no clear codes performed..."
                    Threading.Thread.Sleep(3000)
                End If
            Else
                UserStatusInfo.Label1.Text = "LaunchVSpy returned false, no clear codes performed..."
                Threading.Thread.Sleep(3000)
            End If

            LoginForm.TopMost = True

        Catch ex As Exception
            UserStatusInfo.Label1.Text = $"ClearCodesWithVSpy: {ex.Message}, no clear codes performed..."
            Threading.Thread.Sleep(3000)
            QuitVehicleSpy()
        End Try
    End Sub

    Private Sub CopyVSpyFileToFinalPath()
        ' Called from StopVehicleSpy - Builds the correct filenames for the VehicleSpy produced files and copies them to the
        ' correct directory (the same directory that stores the current recording session INCA files).
        Dim lastrecordfilename As String
        Dim currentrecordfilename As String
        Dim vspyfilename As String
        Dim FileFound As Boolean = False

        Try
            HandleUserMessageLogging("GMRC", "CopyVSpyFileToFinalPath: VehicleSpy Stopped - Copying File(s) to final path...")

            ' Retrieve the last recorded filename from the interface.
            lastrecordfilename = MyIncaInterface.GetLastRecordingFileName
            ' If the last recorded filename is empty, use current record filename.
            If String.IsNullOrEmpty(lastrecordfilename) Then
                currentrecordfilename = MyIncaInterface.GetRecordingFileName
            End If
            ' Determine if the last recorded file exists and is from the same test directory.
            Dim isSameTestSession As Boolean = False
            ' Check if the last recorded filename starts with the expected test path.
            ' This logic assumes lastrecordfilename should contain FinalPathToSaveData and SelectedTestName if continuing the same test session.
            Dim expectedTestPath As String = Path.Combine(FinalPathToSaveData, SelectedTestName)


            If lastrecordfilename.StartsWith(expectedTestPath, StringComparison.OrdinalIgnoreCase) AndAlso File.Exists(lastrecordfilename) Then
                isSameTestSession = True
            End If

            ' If continuing the same test, extract increment and build filenames
            If isSameTestSession Then
                ' Extract the increment from the last recorded filename
                ' Example filenames:
                '   20220314_085456_DRVR04_6MDV4982_99.mf4    -> Increment: "99"
                '   20220314_085456_DRVR04_6MDV4982_100.mf4   -> Increment: "100"

                Dim fileNameOnly As String = Path.GetFileName(lastrecordfilename) ' e.g. "20220314_085456_DRVR04_6MDV4982_99.mf4"
                Dim parts() As String = fileNameOnly.Split("_"c)
                Dim lastPart As String = parts.Last() ' e.g. "99.mf4" or "100.mf4"
                Dim incrementStr As String = lastPart.Substring(0, lastPart.LastIndexOf("."c)) ' "99" or "100"
                Dim incrementValue As Integer = Val(incrementStr)

                ' Determine formatting string based on length of the increment
                Dim formatString As String = If(incrementStr.Length = 2, "00", "000")
                ' Base name: everything except the increment and extension
                ' If fileNameOnly = "20220314_085456_DRVR04_6MDV4982_99.mf4"
                ' Length of incrementStr = 2, extension length = 4 (".mf4")
                ' Remove last (2 + 4) = 6 chars
                Dim baseName As String = fileNameOnly.Substring(0, fileNameOnly.Length - (incrementStr.Length + 4))
                ' Combine back with the directory
                Dim baseFullPath As String = Path.Combine(Path.GetDirectoryName(lastrecordfilename), baseName)

                ' Build new filenames with increment + 1
                Dim newIncrement As String = Format(incrementValue + 1, formatString)
                vspyfilename = baseFullPath & AlternateRecordingMode & "_" & newIncrement & ".vsb"
            Else
                vspyfilename = Path.Combine(FinalPathToSaveData, SelectedTestName & AlternateRecordingMode & "_01.vsb")
            End If

            ' Handle files in DataCache
            Dim dataCachePath = Path.Combine(VEHICLE_SPY_CACHE_DIRECTORY, "DataCache")
            If Directory.Exists(dataCachePath) Then
                For Each filePath In Directory.GetFiles(dataCachePath)
                    FileFound = True

                    While FileInUse(filePath)
                        Threading.Thread.Sleep(100)
                    End While

                    File.Copy(filePath, vspyfilename, True)
                    File.Delete(filePath)

                    HandleUserMessageLogging("GMRC", $"CopyVSpyFileToFinalPath: {filePath} copied and deleted.")
                    Exit For
                Next
            End If

            If Not FileFound Then
                HandleUserMessageLogging("GMRC", "No VehicleSpy Record File Found. Please verify VehicleSpy is functional.", DisplayMsgBox)
                OnVehicleScreen.Label3.BackColor = Color.Red
            Else
                OnVehicleScreen.Label3.BackColor = Color.DarkGray
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"CopyVSpyFileToFinalPath: {ex.Message}")
        End Try
    End Sub

    Public Sub StopVehicleSpy()
        ' Stops the current VehicleSpy measurement and copies the resulting data file.
        Try
            ' 1. Pre-condition checks (Guard Clauses)
            If myVSPYprocess Is Nothing OrElse myVSPYprocess.HasExited Then
                HandleUserMessageLogging("GMRC", "StopVehicleSpy: Process is not running. Nothing to stop.")
                Return
            End If

            If SendVSpyRequest("isrunning?") <> "1" Then
                HandleUserMessageLogging("GMRC", "StopVehicleSpy: Measurement is not running. Stop command not sent.")
                Return
            End If

            ' 2. Stop the measurement
            MyIncaInterface.WriteEventComment(Format$(DateTime.Now, "HH:mm:ss") & " " & "Stopping VehicleSpy Replay Block Playback...", True)
            If Not SendVSpyCommand("stop") Then
                HandleStopFailure("Failed to send stop command to VehicleSpy.")
                Return
            End If

            ' 3. Verify that the measurement has stopped and copy the data file
            If WaitForVSpyState("0", VSPY_STOP_WAIT_TIME) Then
                HandleUserMessageLogging("GMRC", "StopVehicleSpy: Measurement stopped successfully.")
                CopyVSpyFileToFinalPath()
            Else
                HandleStopFailure("VehicleSpy measurement did not stop within the time limit.")
                ' As a fallback, attempt to kill the process to prevent a hung state.
                QuitVehicleSpy()
            End If

        Catch ex As Exception
            HandleStopFailure($"StopVehicleSpy Exception: {ex.Message}")
            ' If a critical exception occurs, quit VSpy to ensure a clean state for the next run.
            QuitVehicleSpy()
        End Try
    End Sub

    Private Sub HandleStopFailure(errorMessage As String)
        ' Centralized handler for any failure during the stop sequence.
        HandleUserMessageLogging("GMRC", errorMessage,,, FlashMsg3Sec)
        OnVehicleScreen.Label3.BackColor = Color.Red
        If VSpySelectedConfigFileName = VSpyDefaultConfigFileName Then
            LoginForm.CheckBox3.Checked = False
        End If
    End Sub

    Public Function StartVehicleSpy(Optional ByVal Mode As String = "") As Boolean
        ' Ensures VehicleSpy is running and ready for a new measurement session.
        ' This involves a start-stop-start sequence for a clean state.
        Try
            ' 1. Initial validation and guard clauses
            If Not CanStartVehicleSpy(Mode) Then
                Return False
            End If

            ' 2. Core logic: Ensure VSpy is running and reset for measurement
            Dim success As Boolean
            If myVSPYprocess Is Nothing OrElse myVSPYprocess.HasExited Then
                ' Scenario A: VSpy process is not running. Launch and verify it.
                HandleUserMessageLogging("GMRC", "StartVehicleSpy: Process not running. Starting fresh...",, )
                success = HandleFirstTimeStart()
            Else
                ' Scenario B: VSpy process is already running. Reset the measurement.
                HandleUserMessageLogging("GMRC", "StartVehicleSpy: Process already running. Resetting measurement...",, )
                success = HandleMeasurementReset()
            End If

            ' 3. Final status update based on success or failure
            If success Then
                HandleUserMessageLogging("GMRC", "StartVehicleSpy: Ready for measurement.",, )
                If String.IsNullOrEmpty(Mode) Then
                    MyIncaInterface.WriteEventComment(Format$(DateTime.Now, "HH:mm:ss") & " " & "VehicleSpy Replay Block Playback Started...", True)
                    OnVehicleScreen.Label3.BackColor = Color.Green
                End If
            Else
                HandleStartFailure("Failed to start or reset VehicleSpy measurement.")
            End If

            Return success

        Catch ex As Exception
            HandleStartFailure($"StartVehicleSpy Exception: {ex.Message}")
            Return False
        End Try
    End Function

    Private Function CanStartVehicleSpy(ByVal Mode As String) As Boolean
        ' Performs initial checks to determine if VehicleSpy can be started.
        If AlternateRecordingMode = "None" Then
            MsgBox("VehicleSpy not available on this PC")
            Return False
        End If

        If CheckVSpyMessageDisplayed Then
            Return False
        End If

        INCACommCheckStopWatch?.Stop()

        If Not String.IsNullOrEmpty(Mode) Then
            HandleUserMessageLogging("GMRC", "Clearing Codes with VSpy...",,, FlashMsgOn)
        End If

        Return True
    End Function

    Private Function HandleFirstTimeStart() As Boolean
        ' Handles the sequence for starting VehicleSpy when the process is not yet running.
        If Not StartVSpyProcess() Then Return False
        If Not ConnectToVSpy() Then Return False
        If Not VerifyVSpyCommunication() Then Return False
        Return True
    End Function

    Private Function HandleMeasurementReset() As Boolean
        ' Handles the sequence for resetting VehicleSpy when it's already running.
        ' Step 1: If a measurement is active, stop it.
        If SendVSpyRequest("isrunning?") = "1" Then
            HandleUserMessageLogging("GMRC", "StartVehicleSpy: Measurement is active. Stopping it first...")
            If Not SendVSpyCommand("stop") Then
                HandleUserMessageLogging("GMRC", "StartVehicleSpy: Failed to send stop command.")
                Return False
            End If
            If Not WaitForVSpyState("0", VSPY_STOP_WAIT_TIME) Then
                HandleUserMessageLogging("GMRC", "StartVehicleSpy: Timed out waiting for measurement to stop.")
                Return False
            End If
            HandleUserMessageLogging("GMRC", "StartVehicleSpy: Measurement stopped successfully.")
        End If

        ' Step 2: Clear cache and start a new measurement.
        ClearDataCacheFiles()
        HandleUserMessageLogging("GMRC", "StartVehicleSpy: Starting new measurement...")
        If Not SendVSpyCommand("start") Then
            HandleUserMessageLogging("GMRC", "StartVehicleSpy: Failed to send start command.")
            Return False
        End If
        If Not WaitForVSpyState("1", VSPY_START_WAIT_TIME) Then
            HandleUserMessageLogging("GMRC", "StartVehicleSpy: Timed out waiting for new measurement to start.")
            Return False
        End If

        Return True
    End Function

    Private Sub HandleStartFailure(errorMessage As String)
        ' Centralized handler for any failure during the start sequence.
        HandleUserMessageLogging("GMRC", errorMessage,,, FlashMsg3Sec)
        OnVehicleScreen.Label3.BackColor = Color.Red
        If VSpySelectedConfigFileName = VSpyDefaultConfigFileName Then
            LoginForm.CheckBox3.Checked = False
        End If
    End Sub

    Public Sub QuitVehicleSpy()
        ' Kills the VehicleSpy process and cleans up associated resources.
        ' Called from LaunchVSpy, ExitApp, and StopVehicleSpy.
        HandleUserMessageLogging("GMRC", "QuitVehicleSpy Called.")

        Try
            ' Check if the process object exists and is still running.
            If myVSPYprocess IsNot Nothing AndAlso Not myVSPYprocess.HasExited Then
                HandleUserMessageLogging("GMRC", "QuitVehicleSpy: Terminating VehicleSpy process...",,, FlashMsgOn)
                myVSPYprocess.Kill()
            Else
                HandleUserMessageLogging("GMRC", "QuitVehicleSpy: VehicleSpy process not running or already exited.")
            End If
        Catch ex As Exception
            ' Log any errors that occur during process termination.
            HandleUserMessageLogging("GMRC", "QuitVehicleSpy: Error while terminating process: " & ex.Message)
        Finally
            ' This block ensures that cleanup happens regardless of exceptions.

            ' Close and release the socket resource.
            If sock IsNot Nothing Then
                HandleUserMessageLogging("GMRC", "QuitVehicleSpy: Closing socket...")
                sock.Close()
            End If

            ' Reset all state variables to ensure a clean state.
            sock = Nothing
            myVSPYprocess = Nothing
            VehicleSpyCaptureStarted = False

            ' Hide the status UI.
            UserStatusInfo.Hide()
        End Try
    End Sub

    Private Function SendVSpyRequest(requestTxt As String) As String
        ' Sends a request to VehicleSpy via the Text API and returns the result.
        ' A request returns a value (e.g., "isrunning?" returns "1" or "0").
        Const ERROR_RESPONSE As String = "er"

        Try
            HandleUserMessageLogging("GMRC", $"SendVSpyRequest Called: {requestTxt}...")

            ' Ensure the socket is connected before proceeding.
            If Not IsSocketConnected() Then
                HandleUserMessageLogging("GMRC", "SendVSpyRequest: Socket disconnected. Attempting to reconnect...")
                If Not ConnectToVSpy() Then
                    HandleUserMessageLogging("GMRC", "SendVSpyRequest: Reconnection failed.")
                    Return ERROR_RESPONSE
                End If
            End If

            ' Prepare and send the request data.
            Dim requestData As Byte() = Encoding.ASCII.GetBytes(requestTxt & vbCrLf)
            sock.Send(requestData, 0, requestData.Length, SocketFlags.None)

            ' Receive the response from the socket.
            Dim receiveBuffer(255) As Byte
            Dim bytesReceived As Integer = sock.Receive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None)
            Dim response As String = Encoding.ASCII.GetString(receiveBuffer, 0, bytesReceived).Trim()

            ' Parse the response.
            If response.StartsWith("ok", StringComparison.OrdinalIgnoreCase) Then
                ' For a successful response like "ok isrunning 1", return the last part ("1").
                Dim parts() As String = response.Split(" "c)
                Return parts.Last()
            Else
                ' For an error response, return the first two characters (e.g., "er").
                Return response.Substring(0, 2)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"SendVSpyRequest: {ex.Message}")
            OnVehicleScreen.Label3.BackColor = Color.Red
            Return ERROR_RESPONSE
        End Try
    End Function

    Private Function IsSocketConnected() As Boolean
        ' Checks if the socket is still connected in a non-blocking way.
        If sock Is Nothing Then
            Return False
        End If
        ' Returns true if the socket is still connected, false otherwise.
        Return Not (sock.Poll(1, SelectMode.SelectRead) AndAlso sock.Available = 0)
    End Function

    Public Function SendVSpyCommand(ByVal commandTxt As String) As Boolean
        ' Sends a command to VehicleSpy via the Text API and returns true on success.
        ' A command only returns "ok" or "er" to indicate success or failure.
        Try
            HandleUserMessageLogging("GMRC", $"SendVSpyCommand Called: {commandTxt}...")

            ' Ensure the socket is connected before proceeding.
            If Not IsSocketConnected() Then
                HandleUserMessageLogging("GMRC", "SendVSpyCommand: Socket disconnected. Attempting to reconnect...")
                If Not ConnectToVSpy() Then
                    HandleUserMessageLogging("GMRC", "SendVSpyCommand: Reconnection failed.")
                    Return False
                End If
            End If

            ' Prepare and send the command data.
            Dim commandData As Byte() = Encoding.ASCII.GetBytes(commandTxt & vbCrLf)
            sock.Send(commandData, 0, commandData.Length, SocketFlags.None)

            ' A delay is often needed to allow the command to be processed.
            System.Threading.Thread.Sleep(1000)

            ' Receive the response from the socket.
            Dim receiveBuffer(255) As Byte
            Dim bytesReceived As Integer = sock.Receive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None)
            Dim response As String = Encoding.ASCII.GetString(receiveBuffer, 0, bytesReceived)

            ' A response starting with "ok" indicates success.
            Return response.StartsWith("ok", StringComparison.OrdinalIgnoreCase)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"SendVSpyCommand: {ex.Message}")
            OnVehicleScreen.Label3.BackColor = Color.Red
            Return False
        End Try
    End Function

    Private Function ConnectToVSpy() As Boolean
        ' Connects to the VehicleSpy Text API via a TCP socket on the local machine.
        ' ✅ IMPROVED: Added retry logic, socket timeouts, and better error handling.
        Const VSPY_PORT As Integer = 8000

        HandleUserMessageLogging("GMRC", "ConnectToVSpy Called...")

        For attempt As Integer = 1 To VSPY_CONNECT_RETRY_COUNT
            Try
                ' Ensure any existing socket is properly closed and disposed before creating a new one.
                If sock IsNot Nothing Then
                    Try
                        sock.Shutdown(SocketShutdown.Both)
                    Catch
                        ' Ignore shutdown errors - socket may already be disconnected
                    End Try
                    sock.Close()
                    sock.Dispose()
                    sock = Nothing
                End If

                ' Since VehicleSpy runs on the same PC, connect to the loopback address.
                HandleUserMessageLogging("GMRC", $"ConnectToVSpy: Attempt {attempt}/{VSPY_CONNECT_RETRY_COUNT} - Connecting to {IPAddress.Loopback}:{VSPY_PORT}...")

                Dim endpoint As New IPEndPoint(IPAddress.Loopback, VSPY_PORT)
                sock = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

                ' ✅ Set socket timeouts to prevent indefinite hangs
                sock.SendTimeout = VSPY_SOCKET_TIMEOUT_MS
                sock.ReceiveTimeout = VSPY_SOCKET_TIMEOUT_MS

                ' ✅ Use async connect with timeout for more control
                Dim connectResult As IAsyncResult = sock.BeginConnect(endpoint, Nothing, Nothing)
                Dim connectSuccess As Boolean = connectResult.AsyncWaitHandle.WaitOne(VSPY_SOCKET_TIMEOUT_MS, True)

                If connectSuccess AndAlso sock.Connected Then
                    sock.EndConnect(connectResult)
                    HandleUserMessageLogging("GMRC", $"ConnectToVSpy: Connection successful on attempt {attempt}.")

                    ' ✅ Verify the connection is actually working by sending a simple ping
                    If VerifySocketConnection() Then
                        Return True
                    Else
                        HandleUserMessageLogging("GMRC", "ConnectToVSpy: Connection established but verification failed.")
                        ' Fall through to retry
                    End If
                Else
                    HandleUserMessageLogging("GMRC", $"ConnectToVSpy: Connection timed out on attempt {attempt}.")
                    ' Clean up the failed async operation
                    Try
                        sock.Close()
                    Catch
                    End Try
                End If

            Catch ex As SocketException
                ' Handle specific socket errors for better logging.
                HandleUserMessageLogging("GMRC", $"ConnectToVSpy: Attempt {attempt} SocketException: {ex.Message} (ErrorCode: {ex.SocketErrorCode})")
            Catch ex As Exception
                ' Handle any other unexpected errors.
                HandleUserMessageLogging("GMRC", $"ConnectToVSpy: Attempt {attempt} Exception: {ex.Message}")
            End Try

            ' ✅ Wait before retrying (except on last attempt)
            If attempt < VSPY_CONNECT_RETRY_COUNT Then
                HandleUserMessageLogging("GMRC", $"ConnectToVSpy: Waiting {VSPY_CONNECT_RETRY_DELAY_MS}ms before retry...")
                Threading.Thread.Sleep(VSPY_CONNECT_RETRY_DELAY_MS)
            End If
        Next

        ' All retry attempts failed
        HandleUserMessageLogging("GMRC", $"ConnectToVSpy: Failed after {VSPY_CONNECT_RETRY_COUNT} attempts.")
        OnVehicleScreen.Label3.BackColor = Color.Red
        Return False
    End Function

    ''' <summary>
    ''' ✅ NEW: Verifies the socket connection is working by attempting a simple request.
    ''' </summary>
    Private Function VerifySocketConnection() As Boolean
        Try
            If sock Is Nothing OrElse Not sock.Connected Then
                Return False
            End If

            ' Send a simple "isrunning?" query to verify the API is responsive
            Dim testData As Byte() = Encoding.ASCII.GetBytes("isrunning?" & vbCrLf)
            sock.Send(testData, 0, testData.Length, SocketFlags.None)

            ' Wait for response with timeout (already set on socket)
            Dim receiveBuffer(255) As Byte
            Dim bytesReceived As Integer = sock.Receive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None)

            If bytesReceived > 0 Then
                Dim response As String = Encoding.ASCII.GetString(receiveBuffer, 0, bytesReceived).Trim()
                HandleUserMessageLogging("GMRC", $"VerifySocketConnection: Got response: {response}")
                ' Any response (even "er") means the API is responding
                Return True
            End If

            Return False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"VerifySocketConnection: {ex.Message}")
            Return False
        End Try
    End Function

    Public Function LaunchVSpy(Optional ByVal Mode As String = "") As String
        ' Launches and verifies the VehicleSpy application.
        HandleUserMessageLogging("GMRC", "LaunchVSpy called...",, )
        INCACommCheckStopWatch?.Stop()

        Try
            ' Step 1: Start the Vehicle Spy process if it's not already running.
            If Not StartVSpyProcess() Then
                Return HandleLaunchFailure("Could not launch Vehicle Spy process.")
            End If

            ' Step 2: Connect to the Vehicle Spy socket API.
            If Not ConnectToVSpy() Then
                Return HandleLaunchFailure("Could not connect to VSpy API.")
            End If

            ' Step 3: Load the specified configuration file.
            If Not LoadVSpyConfiguration() Then
                Return HandleLaunchFailure("Could not locate or load VehicleSpy configuration file.")
            End If

            ' Step 4: If not in a special mode (like 'ClearCodes'), verify communication.
            If String.IsNullOrEmpty(Mode) Then
                If Not VerifyVSpyCommunication() Then
                    Return HandleLaunchFailure("VehicleSpy communication verification failed.")
                End If
            End If

            ' If all steps passed, the launch is successful.
            HandleUserMessageLogging("GMRC", "LaunchVSpy successful.")
            UserStatusInfo.Hide()
            ClearDataCacheFiles()
            Return "Success"

        Catch ex As Exception
            Return HandleLaunchFailure($"LaunchVSpy Exception: {ex.Message}")
        End Try
    End Function

    Private Function StartVSpyProcess() As Boolean
        ' Starts the vspy3.exe process and waits for it to become active.
        ' ✅ IMPROVED: Added exception handling, main window check, and better logging.

        Try
            ' Check if process is already running and valid
            If myVSPYprocess IsNot Nothing AndAlso Not myVSPYprocess.HasExited Then
                HandleUserMessageLogging("GMRC", "StartVSpyProcess: Process already running (PID: " & myVSPYprocess.Id & ")")
                Return True
            End If

            ' Also check if VSpy is running externally (user may have started it manually)
            If IsProcessRunning("VSPY3") Then
                HandleUserMessageLogging("GMRC", "StartVSpyProcess: External VSpy process detected. Attaching...")
                Dim existingProcesses = Process.GetProcessesByName("VSPY3")
                If existingProcesses.Length > 0 Then
                    myVSPYprocess = existingProcesses(0)
                    VehicleSpyCaptureStarted = True
                    Return True
                End If
            End If

            HandleUserMessageLogging("GMRC", "StartVSpyProcess: Launching VehicleSpy process...",,, FlashMsgOn)

            Dim executablePath As String = Path.Combine(VEHICLE_SPY_DIRECTORY, "vspy3.exe")
            If Not File.Exists(executablePath) Then
                HandleUserMessageLogging("GMRC", "StartVSpyProcess: Executable not found at " & executablePath)
                Return False
            End If

            ' ✅ Use ProcessStartInfo for more control
            Dim startInfo As New ProcessStartInfo()
            startInfo.FileName = executablePath
            startInfo.UseShellExecute = True
            startInfo.WindowStyle = ProcessWindowStyle.Normal

            ' ✅ Start process with exception handling
            Try
                myVSPYprocess = Process.Start(startInfo)
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"StartVSpyProcess: Failed to start process: {ex.Message}")
                Return False
            End Try

            If myVSPYprocess Is Nothing Then
                HandleUserMessageLogging("GMRC", "StartVSpyProcess: Process.Start returned Nothing")
                Return False
            End If

            HandleUserMessageLogging("GMRC", $"StartVSpyProcess: Process started (PID: {myVSPYprocess.Id}). Waiting for initialization...")

            ' ✅ Phase 1: Wait for the process to be running
            Dim stopwatch As Stopwatch = Stopwatch.StartNew()
            While Not IsProcessRunning("VSPY3")
                If stopwatch.Elapsed.TotalSeconds > VSPY_LAUNCH_WAIT_TIME Then
                    HandleUserMessageLogging("GMRC", "StartVSpyProcess: Timeout waiting for process to start.")
                    Return False
                End If
                If myVSPYprocess.HasExited Then
                    HandleUserMessageLogging("GMRC", $"StartVSpyProcess: Process exited unexpectedly (ExitCode: {myVSPYprocess.ExitCode})")
                    Return False
                End If
                Threading.Thread.Sleep(500)
            End While

            HandleUserMessageLogging("GMRC", $"StartVSpyProcess: Process running after {stopwatch.ElapsedMilliseconds}ms")

            ' ✅ Phase 2: Wait for the main window to be created (indicates GUI is ready)
            stopwatch.Restart()
            Dim mainWindowFound As Boolean = False
            While stopwatch.Elapsed.TotalSeconds < 30 ' Additional 30 second timeout for window
                myVSPYprocess.Refresh() ' Refresh process info
                If myVSPYprocess.MainWindowHandle <> IntPtr.Zero Then
                    mainWindowFound = True
                    HandleUserMessageLogging("GMRC", $"StartVSpyProcess: Main window detected after {stopwatch.ElapsedMilliseconds}ms")
                    Exit While
                End If
                If myVSPYprocess.HasExited Then
                    HandleUserMessageLogging("GMRC", $"StartVSpyProcess: Process exited while waiting for window (ExitCode: {myVSPYprocess.ExitCode})")
                    Return False
                End If
                Threading.Thread.Sleep(500)
            End While

            If Not mainWindowFound Then
                HandleUserMessageLogging("GMRC", "StartVSpyProcess: WARNING - Main window not detected, but continuing...")
                ' Don't fail here - some configurations may not have a visible window
            End If

            ' ✅ Phase 3: Additional delay to let the Text API server start
            HandleUserMessageLogging("GMRC", $"StartVSpyProcess: Waiting {VSPY_API_READY_WAIT_MS}ms for API to initialize...")
            Threading.Thread.Sleep(VSPY_API_READY_WAIT_MS)

            VehicleSpyCaptureStarted = True
            HandleUserMessageLogging("GMRC", "StartVSpyProcess: VehicleSpy process ready.")
            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"StartVSpyProcess: Exception: {ex.Message}")
            Return False
        End Try
    End Function

    Private Function LoadVSpyConfiguration() As Boolean
        ' Loads the selected configuration file into Vehicle Spy.
        Dim configPath As String = Path.Combine(My.Application.Info.DirectoryPath, "VehicleSpy", VSpySelectedConfigFileName)
        If Not File.Exists(configPath) Then
            Return False
        End If

        HandleUserMessageLogging("GMRC", "Loading VehicleSpy configuration: " & configPath)
        SendVSpyCommand("loadfile " & configPath)
        ' The loadfile command does not provide a reliable status return, so a delay is necessary.
        Threading.Thread.Sleep(5000)
        Return True
    End Function

    Private Function VerifyVSpyCommunication() As Boolean
        ' Performs a start/stop sequence to verify the API is responsive.
        HandleUserMessageLogging("GMRC", "Verifying VehicleSpy communication...")

        ' Start measurement and wait for it to be running.
        If Not SendVSpyCommand("start") Then Return False
        If Not WaitForVSpyState("1", VSPY_START_WAIT_TIME) Then
            HandleUserMessageLogging("GMRC", "VehicleSpy did not start measurement within the time limit.")
            Return False
        End If
        HandleUserMessageLogging("GMRC", "VehicleSpy measurement started successfully.")

        ' Stop measurement and wait for it to be stopped.
        If Not SendVSpyCommand("stop") Then Return False
        If Not WaitForVSpyState("0", VSPY_STOP_WAIT_TIME) Then
            HandleUserMessageLogging("GMRC", "VehicleSpy did not stop measurement within the time limit.")
            Return False
        End If
        HandleUserMessageLogging("GMRC", "VehicleSpy measurement stopped successfully.")

        Return True
    End Function

    Private Function WaitForVSpyState(expectedState As String, timeoutSeconds As Integer) As Boolean
        ' Polls the "isrunning?" status until it matches the expected state or times out.
        Dim stopwatch As Stopwatch = Stopwatch.StartNew()
        While SendVSpyRequest("isrunning?") <> expectedState
            If stopwatch.Elapsed.TotalSeconds > timeoutSeconds Then
                Return False ' Timed out.
            End If
            Threading.Thread.Sleep(1000)
        End While
        Return True ' Expected state was reached.
    End Function

    Private Sub ClearDataCacheFiles()
        ' Clears any temporary files from the Vehicle Spy DataCache directory.
        Dim dataCachePath = Path.Combine(VEHICLE_SPY_CACHE_DIRECTORY, "DataCache")
        If Directory.Exists(dataCachePath) Then
            For Each filePath In Directory.GetFiles(dataCachePath)
                Try
                    File.Delete(filePath)
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Failed to delete cache file {filePath}: {ex.Message}")
                End Try
            Next
        End If
    End Sub

    Private Function HandleLaunchFailure(errorMessage As String) As String
        ' Centralized handler for any failure during the launch sequence.
        HandleUserMessageLogging("GMRC", "LaunchVSpy failed: " & errorMessage)

        ' Update UI to reflect failure.
        UserStatusInfo.Hide()
        LoginForm.CheckBox3.Checked = False
        LoginForm.CheckBox3.Visible = False
        OnVehicleScreen.Label3.Visible = False

        ' Clean up resources.
        QuitVehicleSpy()

        Return "VehicleSpy Setup Verification failed. " & errorMessage
    End Function

End Module
