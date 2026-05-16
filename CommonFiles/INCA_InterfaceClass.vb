Option Strict Off

Imports System.Diagnostics
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks

Public Class INCA_InterfaceClass

    'The INCA_InterfaceClass contains routines which initiate communication between CLEVIR and INCA via the GM_INCA_CommClass (MyGmIncaComm).  
    'In the original software design, CLEVIR was a client to the GM_INCA_Comm server.  This Client/Server design was changed midway through the 
    'design in favor of single CLEVIR application.  The original intent of the INCA_Interface class was to provide the interface between the
    'CLEVIR client and the server.  So, any action that required interaction directly with INCA via an API call was handled in the INCA_InterfaceClass
    'which would implement methods and properties of the GM_INCA_Comm server.

    'As the software has evolved however, there are now some instances where GM_INCA_CommClass methods are referenced directly from CLEVIR without going
    'through this INCA_InterfaceClass.

    'The INCA_InterfaceClass also exposes various properties which indicate INCA status such As Recording Or MeasurementStarted, etc.

    'Throughout the code, the following construct is prevalent...

    'MyIncaInterface.SOMETHING

    'In the initialization routine, CLEVIR creates a reference to this INCA_InterfaceClass
    'as follows....

    'MyIncaInterface = New INCA_InterfaceClass

    'Henceforth, CLEVIR makes reference to the methods defined in the class such as for
    'example, the Connect method...

    'MyIncaInterface.Connect

    'Throughout the code,when it is necessary to communicate with the GM_INCA_Comm, to either set
    'something in INCA or get something from INCA, in most cases, a method defined in the INCA_InterfaceClass is used
    'which then accesses methods in GM_INCA_Comm...

    'In addition to methods, this class also defines properties which contain INCA status information,
    'such as MeasurementStarted for example.  If some code in GmResidentClient wants to determine
    'whether or not Measurement has been started in INCA, it is done in the following manner...

    'if MyIncaInterface.MeasurementStarted = True ...

    'It is the responsibility of the INCA_InterfaceClass to set this property value appropriately based
    'on the actual MeasurementStarted status of INCA.

    'Private OxtsNcomIpAddress As String ' Set via configuration
    'Private OxtsNcomPort As Integer ' Set via configuration

    Private RasterInfoRetrieved() As Boolean
    Private ReadOnly MaxRetries As Integer = 100
    Private DeviceDataAvailable As Boolean
    Public MyGmIncaComm As GM_INCA_CommClass

    Public myAddedSignals() As IGM_INCA_Comm.DeviceRasterSignalStatus
    Public DeviceRasterSignals() As IGM_INCA_Comm.DeviceRasterSignalStatus
    Public mySignals() As IGM_INCA_Comm.DeviceRasterSignalStatus
    Public myDisplaySignals() As IGM_INCA_Comm.DeviceRasterSignalStatus

    Private _Recording As Boolean
    Private _MeasurementStarted As Boolean
    Private _StopRecordingRequested As Boolean
    Private _Devices() As IGM_INCA_Comm.INCADeviceStatus
    Private _DeviceAcquisitionRates(,) As String
    Private _MeasureElementNamesInDevice(,) As String
    Private _InitialMeasurementStatus As String

    Public DeviceDataRetrieved As Boolean
    Public Cancelit As Boolean
    Public myPreliminaryDisplaySignals() As IGM_INCA_Comm.DeviceRasterSignalStatus

    Public Structure INCA_Variables
        Dim devicename As String
        Dim deviceindex As Integer
        Dim defaultrastername As String
        Dim variablename As String
    End Structure

    Public Structure INCA_Rasters
        Dim devicename As String
        Dim deviceindex As Integer
        Dim rastername As String
        Dim variables() As String
    End Structure

    Public Structure INCA_Device
        Dim devicename As String
        Dim variableinfo() As INCA_Variables
        Dim rasters() As INCA_Rasters
    End Structure

    Public deviceinfo() As INCA_Device

    'INCA_InterfaceClass property definitions...

    Property StopRecordingRequested() As Boolean
        Get
            Return _StopRecordingRequested
        End Get
        Set(ByVal value As Boolean)
            _StopRecordingRequested = value
        End Set
    End Property

    Property InitialMeasurementStatus() As String
        Get
            Return _InitialMeasurementStatus
        End Get
        Set(ByVal value As String)
            _InitialMeasurementStatus = value
        End Set
    End Property
    Property Recording() As Boolean
        Get
            Return _Recording
        End Get
        Set(ByVal value As Boolean)
            _Recording = value
        End Set
    End Property

    Property MeasurementStarted() As Boolean
        Get
            Return _MeasurementStarted
        End Get
        Set(ByVal value As Boolean)
            _MeasurementStarted = value
        End Set
    End Property

    'INCA_InterfaceClass Subroutine and Function definitions...
    Public Function GetActualRecordingTimeMs() As Integer
        Return MyGmIncaComm.GetActualRecordingTimeMs()
    End Function

    Public Function GetRemainingRecordingTimeMs() As Integer
        Return MyGmIncaComm.GetRemainingRecordingTimeMs()
    End Function

    Private Function StopMeasurementAndSaveToFile(fileName As String, fileFormat As String) As Boolean
        Return MyGmIncaComm.StopMeasurementAndSaveToFile(fileName, fileFormat)
    End Function

    Public Function IsTargetOnWorkingPage() As String

        Static Retries As Integer

        Try
            IsTargetOnWorkingPage = MyGmIncaComm.IsTargetOnWorkingPage

            Retries = 0

        Catch ex As Exception
            IsTargetOnWorkingPage = "Unknown"
            Retries += 1
            HandleUserMessageLogging("GMRC", "IsTargetOnWorkingPage: MyGmIncaComm.IsTargetOnWorkingPage Call FAILED - Retries = " & Retries & " Exception: " & ex.Message)
            If Retries > MaxRetries Then
                HandleUserMessageLogging("GMRC", "IsTargetOnWorkingPage: MyGmIncaComm.IsTargetOnWorkingPage Call FAILED - Retries = " & Retries)
            End If

        End Try

    End Function

    Public Function GetCurrentVersion() As String
        GetCurrentVersion = MyGmIncaComm.GetCurrentVersion
    End Function

    Public Function GetAvailableExperimentNames() As String()
        GetAvailableExperimentNames = MyGmIncaComm.GetAvailableExperimentNames()
    End Function

    Public Sub StopDataCollection()
        MyGmIncaComm.StopDataCollection()
    End Sub

    Public Function GetINCAPollingRate() As Double
        GetINCAPollingRate = MyGmIncaComm.GetINCAPollingRate * 1000
    End Function

    Public Sub StartDataCollection(ByVal sleeptime As Integer)
        MyGmIncaComm.StartDataCollection(sleeptime)
    End Sub

    Public Function GetDefaultRasterForMeasureElementInDevice(ByVal devicename As String, ByVal measname As String) As String
        GetDefaultRasterForMeasureElementInDevice = MyGmIncaComm.GetDefaultRasterForMeasureElementInDevice(devicename, measname)
    End Function

    Public Function GetSignalData() As Double()

        'This function not currently used.  Replaced by GetSignalDataWithTime...

        GetSignalData = MyGmIncaComm.GetSignalData
    End Function

    Public Sub CloseINCA()
        'Called from ExitApp and ShutdownAndRestartINCA
        'Unlocks the experiment, closes INCA - Returns after INCA has completely shut down...

        Try
            UnlockExperiment()
            MyGmIncaComm.CloseINCA()

            HandleUserMessageLogging("GMRC", "Waiting for INCA to shut down...",,, FlashMsg1Sec)

            Do While IsProcessRunning("INCA") = True Or IsProcessRunning("TGTSVR") = True
                System.Threading.Thread.Sleep(1000)
            Loop

            HandleUserMessageLogging("GMRC", "INCA Shutdown Complete.",,, FlashMsg1Sec)

        Catch ex As System.Net.Sockets.SocketException When ex.ErrorCode = 10004
            ' ✅ Suppress WSACancelBlockingCall (expected during shutdown)
            HandleUserMessageLogging("GMRC", "INCA connection closed during shutdown (expected)")

        Catch ex As System.IO.IOException When ex.InnerException?.GetType() Is GetType(System.Net.Sockets.SocketException)
            ' ✅ Suppress IOException wrapping SocketException
            HandleUserMessageLogging("GMRC", "INCA socket closed during shutdown (expected)")

        Catch ex As Exception
            ' ❌ Log all other unexpected errors (including during shutdown wait)
            HandleUserMessageLogging("GMRC", $"CloseINCA unexpected error: {ex.Message}")
        End Try
    End Sub

    Public Sub RCI2_Cleanup()
        MyGmIncaComm.RCI2_CleanUp()
    End Sub

    Public Async Function StartStopMeasurement(ByVal sender As System.Object) As Task
        Try
            ' ✅ Safe to access UI controls - we're on UI thread
            sender.parent.Cursor = Cursors.WaitCursor

            If sender.Text = "START MEASUREMENT" Then
                Await StartMeasurementRoutine(sender)
            Else
                StopMeasurementRoutine(sender)
            End If

            sender.parent.Cursor = Cursors.Arrow
            sender.parent.refresh()
            OnVehicleScreen.Button1.Enabled = True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "StartStopMeasurement: " & ex.Message)
        End Try
    End Function

    Private Async Function StartMeasurementRoutine(ByVal sender As System.Object) As Task
        HandleUserMessageLogging("GMRC", "")
        HandleUserMessageLogging("GMRC", "START MEASUREMENT Button Pressed...")
        HandleUserMessageLogging("GMRC", "")

        OnVehicleScreen.Button4.Enabled = False
        If GmResidentClient IsNot Nothing AndAlso GmResidentClient.MyLogin IsNot Nothing Then
            GmResidentClient.MyLogin.Enabled = False
        End If


        If Not InSession Then
            HandleUserMessageLogging("GMRC", "StartStopMeasurement: InSession = False")
            GmResidentClient.StartingMileage = 0
            ' ✅ FIXED: Direct await (function is now async)
            Dim setupResult As Boolean = Await MyIncaInterface.SetupDataLogging(SaveLoginID)
            If Not setupResult Then Exit Function
            InSession = True
        End If

        ' Move blocking INCA call to background thread
        Await Task.Run(Sub() MyIncaInterface.StartMeasurement())

        sender.Text = "STOP MEASUREMENT"
        sender.BackColor = Color.Blue
        sender.ForeColor = Color.White

        ' Move blocking device status check to background thread
        Await GetAvailableDevicesAsync(False)
    End Function

    Private Sub StopMeasurementRoutine(ByVal sender As System.Object)
        HandleUserMessageLogging("GMRC", "STOP MEASUREMENT Button Pressed...")
        HandleUserMessageLogging("GMRC", "")

        ResetFlags()

        If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
            OnVehicleScreen.Button4.Enabled = True
            If GmResidentClient IsNot Nothing AndAlso GmResidentClient.MyLogin IsNot Nothing Then
                GmResidentClient.MyLogin.Enabled = True
            End If
        End If

        SetRecordingIndicator()

        Dim WasRecording As Boolean = MyIncaInterface.GetRecordingState()
        If WasRecording Then
            HandleUserMessageLogging("GMRC", "StartStopMeasurement: Was Recording = True")
            StopRecordingProcess()
        End If

        MyIncaInterface.StopMeasurement()

        UpdateButtonStates(sender, WasRecording)
    End Sub

    Private Shared Sub ResetFlags()
        BackgroundLoopCounterNotUpdating = False
        VideoCameraNotUpdating = False
    End Sub

    Private Sub SetRecordingIndicator()
        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
            OnVehicleScreen.PictureBox1.BackColor = Color.Red
            OnVehicleScreen.PictureBox1.Image = My.Resources.Resources.mic_50_Red()
        End If
    End Sub

    Private Sub StopRecordingProcess()
        Try
            ' Stop alt-recorders if enabled
            If LoginForm.CheckBox3.Checked Then
                If AlternateRecordingMode <> "VehicleSpy" Then
                    StopCanalyzer()
                Else
                    StopVehicleSpy()
                End If
            End If

            If LidarCaptureStarted Then
                StopLidarCapture()
            End If

            ' Optional CAL snapshot
            If LoginForm.CheckBox1.Checked Then
                MyIncaInterface.SaveCalSnapShot("Working")
            End If

            ' ================================================================
            ' ✅ FIXED: Use CompressRecordingFiles() wrapper instead of direct call
            ' ================================================================
            Dim dataPath As String

            dataPath = Path.Combine(BaseDataCollectionPath, "Data", "gmcsv" & VehicleNumber)

            ' Use high-level wrapper for consistency
            If Not CompressRecordingFiles(dataPath) Then
                HandleUserMessageLogging("GMRC", "StopRecordingProcess: Compression completed with warnings")
            End If

            SaveFinalPathToSaveData = FinalPathToSaveData
            WriteFinalPathToSaveData()

            ' UI consistency: reset elapsed timer and label
            If RecorderStopWatch Is Nothing Then RecorderStopWatch = New Stopwatch()
            RecorderStopWatch.Reset()
            OnVehicleScreen.Label8.Text = ""
            OnVehicleScreen.Label5.Text = "Recording Stopped"
            HandleUserMessageLogging("GMRC", "StopRecordingProcess: Recording Stopped")

            ' Avoid flicker: refresh instead of hide/show
            OnVehicleScreen.BringToFront()
            OnVehicleScreen.Refresh()
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "StopRecordingProcess: " & ex.Message)
        End Try
    End Sub

    Private Sub UpdateButtonStates(ByVal sender As System.Object, ByVal WasRecording As Boolean)
        sender.Text = "START MEASUREMENT"
        sender.BackColor = Color.WhiteSmoke
        sender.ForeColor = Color.Black

        sender.parent.Button14.Text = "START RECORD"
        sender.parent.Button14.BackColor = Color.WhiteSmoke
        sender.parent.Button14.ForeColor = Color.Black

        If WasRecording Then
            InSession = False
        End If
    End Sub

    Public Sub StartStopRecord(ByVal sender As System.Object)
        Dim button = TryCast(sender, Button)
        If button Is Nothing Then Return

        Try
            button.Parent.Cursor = Cursors.WaitCursor

            If button.Text = "START RECORD" Then
                HandleStartRecording(button)
            Else ' STOP RECORD
                HandleStopRecording(button)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "StartStopRecord: " & ex.Message)
        Finally
            If button.Parent IsNot Nothing Then
                button.Parent.Cursor = Cursors.Default
                button.Parent.Refresh()
            End If
        End Try
    End Sub

    ''' <summary>
    ''' Handles the logic for starting a new recording session.
    ''' ✅ REFACTORED: Async to support await pattern
    ''' </summary>
    Private Async Sub HandleStartRecording(ByVal startButton As Button)
        ' 1. Finalize files from the previous session if applicable
        If ZipTheMF4Files AndAlso HaveRecorded Then
            If String.IsNullOrEmpty(SaveFinalPathToSaveData) Then
                ReadFinalDataSavePath()
            End If
            If Not String.IsNullOrEmpty(SaveFinalPathToSaveData) Then
                ' ✅ FIXED: Use CompressRecordingFiles() for consistency
                ' This compresses the LAST file from previous session
                If Not CompressRecordingFiles(SaveFinalPathToSaveData) Then
                    HandleUserMessageLogging("GMRC", "HandleStartRecording: Previous session compression completed with warnings")
                End If
                SaveFinalPathToSaveData = ""
            End If
        End If

        ' 2. Ensure user is logged in
        If String.IsNullOrEmpty(SaveLoginID) Then
            HandleUserMessageLogging("GMRC", "Please select a Login ID", DisplayMsgBox)
            Return
        End If

        ' 3. Set up INCA measurement and data logging if not already in a session
        If GetMeasurementStatus() = "False" AndAlso Not InSession Then
            ' ✅ FIXED: Await the async call
            If Not Await MyIncaInterface.SetupDataLogging(SaveLoginID) Then Return
            InSession = True
        End If

        MyIncaInterface.StartMeasurement()

        ' 4. Save calibration snapshots if enabled
        If LoginForm.CheckBox1.Checked Then
            MyIncaInterface.SaveCalSnapShot("Reference")
            MyIncaInterface.SaveCalSnapShot("Working")
        End If

        ' 5. Start the actual recording in INCA
        MyIncaInterface.StartRecording()

        ' 6. Update UI to reflect the new recording state
        If MyIncaInterface.GetRecordingState() Then
            UpdateUIForRecordingStarted(startButton)
        End If
    End Sub

    ''' <summary>
    ''' Handles the logic for stopping the current recording session.
    ''' </summary>
    Private Sub HandleStopRecording(ByVal stopButton As Button)
        HandleUserMessageLogging("GMRC", "STOP RECORD Button Pressed...")

        ' 1. Reset session flags and UI elements
        ResetSessionState()

        ' 2. Save calibration snapshot if enabled
        If LoginForm.CheckBox1.Checked Then
            MyIncaInterface.SaveCalSnapShot("Working")
        End If

        ' 3. Stop the recording in INCA
        MyIncaInterface.StopRecording()
        RecorderStopWatch?.Reset()

        ' 4. Update UI to reflect the stopped state
        UpdateUIForRecordingStopped(stopButton)

        ' 5. Swap Label17 (distance-to-object) back to Button1 (EXIT).
        ' During measurement the background loop hides Button1 so Label17 can occupy
        ' its position.  We must restore the swap here immediately; the background loop
        ' will stop re-hiding Button1 once MeasurementStarted is cleared.
        OnVehicleScreen.Label17.Text = ""
        OnVehicleScreen.Button1.Visible = True

        ' 6. Finalize session (log mileage, process files)
        FinalizeSessionFiles()
    End Sub

    ''' <summary>
    ''' Updates the UI elements to reflect that recording has started.
    ''' </summary>
    Private Sub UpdateUIForRecordingStarted(ByVal startButton As Button)
        If RecorderStopWatch Is Nothing Then RecorderStopWatch = New Stopwatch()
        RecorderStopWatch.Start()

        startButton.Text = "STOP RECORD"
        startButton.BackColor = Color.Red
        startButton.ForeColor = Color.White
        OnVehicleScreen.Refresh()

        Dim measurementButton = TryCast(startButton.Parent.Controls("Button6"), Button)
        If measurementButton IsNot Nothing Then
            measurementButton.Text = "STOP MEASUREMENT"
            measurementButton.BackColor = Color.Blue
            measurementButton.ForeColor = Color.White
            OnVehicleScreen.Refresh()
        End If
    End Sub

    ''' <summary>
    ''' Resets session-related flags and UI components.
    ''' </summary>
    Private Sub ResetSessionState()
        BackgroundLoopCounterNotUpdating = False
        VideoCameraNotUpdating = False
        InSession = False
        OnVehicleScreen.GroupBox5.Visible = False

        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
            OnVehicleScreen.PictureBox1.BackColor = Color.Red
            OnVehicleScreen.PictureBox1.Image = My.Resources.Resources.mic_50_Red()
        End If
    End Sub

    ''' <summary>
    ''' Updates the UI elements to reflect that recording has stopped.
    ''' </summary>
    Private Async Sub UpdateUIForRecordingStopped(ByVal stopButton As Button)
        stopButton.Text = "START RECORD"
        stopButton.BackColor = Color.WhiteSmoke
        stopButton.ForeColor = Color.Black

        ' Also trigger the stop action for the measurement button
        Dim measurementButton = TryCast(OnVehicleScreen.Controls("Button6"), Button)
        If measurementButton IsNot Nothing Then
            Await StartStopMeasurement(measurementButton)
        End If

        OnVehicleScreen.Label5.Text = "Recording Stopped"
    End Sub

    ''' <summary>
    ''' Handles final logging and file processing at the end of a session.
    ''' </summary>
    Private Sub FinalizeSessionFiles()
        GmResidentClient.WriteMileageToAnnoFile("Stop Record")
        GmResidentClient.StartingMileage = 0

        ' ================================================================
        ' ✅ FIXED: Use CompressRecordingFiles() wrapper instead of direct call
        ' ================================================================
        Dim dataPath As String

        dataPath = Path.Combine(BaseDataCollectionPath, "Data", "gmcsv" & VehicleNumber)

        ' Use high-level wrapper for consistency
        If Not CompressRecordingFiles(dataPath) Then
            HandleUserMessageLogging("GMRC", "FinalizeSessionFiles: Compression completed with warnings")
        End If

        SaveFinalPathToSaveData = FinalPathToSaveData
        WriteFinalPathToSaveData()

        ' ✅ DELETED: Legacy .dat API call block (lines 509-528)
        ' INCA tracks last recording file internally - no manual intervention needed.
        ' The old code attempted to call a broken API and constructed incorrect paths.
    End Sub

    Public Function InitINCA(ByVal INCADatabase As String,
                             ByVal INCAWorkspace As String,
                             ByVal INCAExperiment As String,
                             ByVal ForceInit As Boolean,
                             ByRef ErrorMsg As String,
                             ByVal RegisterIntoNewBlankExp As Boolean,
                             Optional ByVal showToasts As Boolean = True) As IGM_INCA_Comm.INIT_STATUS

        If showToasts Then
            'HandleUserMessageLogging("GMRC", "Initializing INCA...")
            'StatusNotifier.Toast("Initializing INCA...", "INCA", 2000)
        Else
            HandleUserMessageLogging("GMRC", "Initializing INCA...")
        End If

        Dim reply = MyGmIncaComm.InitINCA(INCADatabase, INCAWorkspace, INCAExperiment, GmResidentClient.EtasDefaultUserName, ForceInit, ErrorMsg, RegisterIntoNewBlankExp)

        HandleUserMessageLogging("GMRC", "InitINCA Complete: " & reply.ToString)
        If showToasts Then
            StatusNotifier.Toast($"INCA initialization complete: {reply}", "INCA", durationMs:=1000, ensureMainOnTop:=False)
        End If

        Return reply
    End Function

    Public Async Function GetDeviceAcquisitionRatesAsync() As Task
        ' This routine is only used during runtime configuration.
        ' It gets device names and acquisition rates, behaving differently based on whether
        ' the DeviceRasterSignalList.txt file exists.

        ' Exit if data has already been retrieved.
        If DeviceDataRetrieved Then Return

        Try
            HandleUserMessageLogging("GMRC", "GetDeviceAcquisitionRates: Acquiring Device, Raster, Signal Information...")

            ' Initialize device-related arrays if this is the first run.
            If _DeviceAcquisitionRates Is Nothing Then
                ' ✅ FIXED: Direct async call - GetAvailableDevicesAsync already uses Task.Run internally
                Await GetAvailableDevicesAsync(False)

                _DeviceAcquisitionRates = New String(UBound(_Devices), -1) {}
                If deviceinfo Is Nothing Then
                    ReDim deviceinfo(UBound(_Devices))
                    ReDim RasterInfoRetrieved(UBound(_Devices))
                End If
            End If

            Dim listFilePath = Path.Combine(My.Application.Info.DirectoryPath, "DeviceRasterSignalList.txt")

            If File.Exists(listFilePath) Then
                ' If the file exists, parse it to populate data structures.
                Await LoadDeviceDataFromFileAsync(listFilePath)
            Else
                ' If the file does not exist, get data from INCA and build the file.
                Await BuildDeviceDataFromIncaAsync(listFilePath)
            End If

            DeviceDataRetrieved = True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GetDeviceAcquisitonRates: " & ex.Message, DisplayMsgBox)
        End Try
    End Function

    ''' <summary>
    ''' Populates device and raster data by parsing the DeviceRasterSignalList.txt file.
    ''' </summary>
    Private Async Function LoadDeviceDataFromFileAsync(ByVal filePath As String) As Task
        Dim deviceIndex As Integer = -1
        Dim rasterIndex As Integer = 0

        Using reader As New StreamReader(filePath)
            Dim line As String = Await reader.ReadLineAsync()
            While line IsNot Nothing
                If line.StartsWith("Device=") Then
                    deviceIndex += 1
                    rasterIndex = 0
                    Dim deviceNameInFile = line.Substring(7)
                    If deviceIndex > UBound(_Devices) OrElse deviceNameInFile <> _Devices(deviceIndex).myName Then
                        MsgBox("WARNING: INCA Device List does not match devices in DeviceRasterSignalList.txt. This must be resolved.")
                        GmResidentClient.Cursor = Cursors.Arrow
                        Return
                    End If
                ElseIf line.StartsWith("Raster=") Then
                    If rasterIndex > UBound(_DeviceAcquisitionRates, 2) Then
                        ReDim Preserve _DeviceAcquisitionRates(UBound(_Devices), rasterIndex)
                    End If
                    Dim rasterName = line.Substring(7)
                    ReDim Preserve deviceinfo(deviceIndex).rasters(rasterIndex)
                    deviceinfo(deviceIndex).rasters(rasterIndex).rastername = rasterName
                    _DeviceAcquisitionRates(deviceIndex, rasterIndex) = rasterName
                    rasterIndex += 1
                End If

                line = Await reader.ReadLineAsync()
            End While
        End Using
    End Function

    ''' <summary>
    ''' Populates device data from INCA and creates the DeviceRasterSignalList.txt file.
    ''' </summary>
    Private Async Function BuildDeviceDataFromIncaAsync(ByVal filePath As String) As Task
        Dim tempFilePath = Path.Combine(My.Application.Info.DirectoryPath, "tmpDeviceRasterSignalList.txt")

        ' First, retrieve all acquisition rates from INCA. This can be slow.
        For i = 0 To UBound(_Devices)
            Dim currentIndex = i
            Dim rates = Await Task.Run(Function() MyGmIncaComm.GetDeviceAcquisitionRates(_Devices(currentIndex).myName))
            If rates.Length > UBound(_DeviceAcquisitionRates, 2) + 1 Then
                ReDim Preserve _DeviceAcquisitionRates(UBound(_Devices), rates.Length - 1)
            End If
            For j = 0 To UBound(rates)
                _DeviceAcquisitionRates(i, j) = rates(j)
            Next
        Next

        ' Now, build the content for the new file.
        Using writer As New StreamWriter(tempFilePath)
            For i = 0 To UBound(_Devices)
                Await writer.WriteLineAsync("Device=" & _Devices(i).myName)

                ' Write all associated rasters for the current device.
                For j = 0 To UBound(_DeviceAcquisitionRates, 2)
                    If Not String.IsNullOrEmpty(_DeviceAcquisitionRates(i, j)) Then
                        Await writer.WriteLineAsync("Raster=" & _DeviceAcquisitionRates(i, j))
                    Else
                        Exit For
                    End If
                Next

                ' Retrieve and write signals for the device.
                ' This part is complex and involves another method call.
                ' The original logic is preserved but wrapped in Task.Run for asynchrony.
                Dim currentIndex = i
                ' PerformDeviceSignalRetrieval is Async Sub, so just call it directly
                PerformDeviceSignalRetrieval(_Devices(currentIndex).myName)
            Next
        End Using

        ' Replace the old file with the newly created one.
        If File.Exists(filePath) Then File.Delete(filePath)
        File.Move(tempFilePath, filePath)
    End Function

    Private Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function

    Public Function GetAvailableWorkspaces() As String()
        GetAvailableWorkspaces = MyGmIncaComm.GetAvailableWorkspaces
    End Function

    Public Function SetRecordingFileName(ByVal filename As String) As Boolean
        SetRecordingFileName = MyGmIncaComm.SetRecordingFileName(filename)
    End Function
    Public Function SetRecordingPathName(ByVal pathname As String) As Boolean
        SetRecordingPathName = MyGmIncaComm.SetRecordingPathName(pathname)
    End Function

    Private Function GetRecordingPathName() As String
        GetRecordingPathName = MyGmIncaComm.GetRecordingPathName
    End Function

    Public Sub ResetRecords()
        MyGmIncaComm.ResetRecords()
    End Sub

    Public Function GetSignalDataWithTime() As IGM_INCA_Comm.TransferDataWithTime()

        'Called from myBackgroundTasks...
        'Gets all live data for display...

        Static SaveSignalDataWithTime() As IGM_INCA_Comm.TransferDataWithTime
        Static Failed As Boolean

        Try
            GetSignalDataWithTime = MyGmIncaComm.GetSignalDataWithTime

            If GetSignalDataWithTime IsNot Nothing Then
                SaveSignalDataWithTime = GetSignalDataWithTime
            End If

            If Failed = True Then
                HandleUserMessageLogging("GMRC", "GetSignalDataWithTime: MyIncaInterface.GetSignalDataWithTime Returned Data after initial Failure")
                Failed = False
            End If
        Catch ex As Exception
            If Failed = False Then
                HandleUserMessageLogging("GMRC", "GetSignalDataWithTime: MyIncaInterface.GetSignalDataWithTime Exception: " & ex.Message)
                Failed = True
            End If

            GetSignalDataWithTime = SaveSignalDataWithTime
        End Try


    End Function

    'Public Function GetDataArray(ByVal DeviceName As String, ByVal RasterName As String, ByVal NumValidVars As Integer) As IGM_INCA_Comm.INCAData

    '    'This is the older method for aquiring data, by device/raster pair.  We now aquire data
    '    'using the GetSignalDataWithTime function which passes back the data in the order in which we
    '    'registered the signals and is not dependent on first defining device / raster pairs.

    '    Dim noOfRecords As Integer = 1

    '    GetDataArray = MyGmIncaComm.GetDataArray(DeviceName, RasterName, NumValidVars, noOfRecords)

    'End Function

    'Public Function GetINCAMeasureValue(ByVal DeviceName As String, ByVal RasterName As String, ByVal signalname As String) As IGM_INCA_Comm.INCAMeasureValue

    '    'Gets a specific measure value, this is not used currently...

    '    GetINCAMeasureValue = MyGmIncaComm.GetINCAMeasureValue(DeviceName, RasterName, signalname)

    'End Function

    Public Async Function GetAvailableDevicesAsync(ByVal checkForStatus As Boolean) As Task(Of IGM_INCA_Comm.INCADeviceStatus())
        ' Asynchronously gets the available device list from INCA or cache.
        ' If checkForStatus is True, it always fetches a fresh list from INCA.
        ' Otherwise, it returns the cached list if available.

        Try
            ' If a fresh status check is requested, or if the cache is empty, fetch from INCA.
            If checkForStatus OrElse _Devices Is Nothing Then
                ' Run the synchronous INCA call on a background thread to avoid blocking the UI.
                _Devices = Await Task.Run(Function() MyGmIncaComm.GetAvailableDevices())

                ' If this is the first time fetching devices, handle the device list file.
                If _Devices IsNot Nothing Then
                    Await HandleDeviceListFileAsync(_Devices)
                End If
            End If

            Return _Devices

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GetAvailableDevicesAsync Exception: " & ex.Message)
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Checks for the existence of the device list file. If it doesn't exist, creates and populates it.
    ''' </summary>
    ''' <param name="devices">The list of devices fetched from INCA.</param>
    Private Async Function HandleDeviceListFileAsync(ByVal devices As IGM_INCA_Comm.INCADeviceStatus()) As Task
        Dim listFilePath = Path.Combine(My.Application.Info.DirectoryPath, "DeviceRasterSignalList.txt")

        If File.Exists(listFilePath) Then
            DeviceDataAvailable = True
            ' The original function read the file here but didn't use the data.
            ' That logic appears to be fully handled in GetDeviceAcquisitionRates, so we just set the flag.
        Else
            ' File does not exist, so create it and write the device names.
            DeviceDataAvailable = False
            Try
                Using writer As New StreamWriter(listFilePath)
                    For Each device In devices
                        Await writer.WriteLineAsync("Device=" & device.myName)
                    Next
                End Using
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", "HandleDeviceListFileAsync: Failed to create device list file. " & ex.Message)
            End Try
        End If
    End Function

    Public Function GetDataForSignalList(ByVal signallist() As String) As IGM_INCA_Comm.INCAMeasureValue()

        'Returns an array of INCAMeasureValues based on a list of signal names passed in.
        'This routine is not currently used...

        GetDataForSignalList = MyGmIncaComm.GetDataForSignalList(signallist)
    End Function

    Public Async Function GetAllMeasureElementNamesInDevice(ByVal devicename As String) As Task(Of String())

        'NOT USED!!! we use GetAllMeasureElementNamesInDeviceIdx instead...

        Dim x As Integer
        Dim y As Integer
        Dim myMeasureElementNamesInDevice() As String

        ReDim myMeasureElementNamesInDevice(0)

        If _MeasureElementNamesInDevice Is Nothing Then

            Await GetAvailableDevicesAsync(False)

            ReDim _MeasureElementNamesInDevice(UBound(_Devices), 0)

            For x = 0 To UBound(_Devices)
                myMeasureElementNamesInDevice = MyGmIncaComm.GetAllMeasureElementNamesInDevice(_Devices(x).myName)

                If UBound(myMeasureElementNamesInDevice) > UBound(_MeasureElementNamesInDevice, 2) Then
                    ReDim Preserve _MeasureElementNamesInDevice(UBound(_Devices), UBound(myMeasureElementNamesInDevice))
                End If
                For y = 0 To UBound(myMeasureElementNamesInDevice)
                    _MeasureElementNamesInDevice(x, y) = myMeasureElementNamesInDevice(y)
                Next

            Next x

        End If

        For x = 0 To UBound(_Devices)
            If _Devices(x).myName = devicename Then
                For y = 0 To UBound(_MeasureElementNamesInDevice, 2)
                    If _MeasureElementNamesInDevice(x, y) IsNot Nothing Then
                        ReDim Preserve myMeasureElementNamesInDevice(y)
                        myMeasureElementNamesInDevice(y) = _MeasureElementNamesInDevice(x, y)
                    End If
                Next y
                Exit For
            End If
        Next

        Return myMeasureElementNamesInDevice

    End Function

    Private Async Sub PerformDeviceSignalRetrieval(ByVal DeviceName As String, Optional ByVal tmpPath As String = Nothing, Optional ByVal RasterName As String = "") 'As String()

        'Called from GetDeviceAcquisitionRates.  Gets device, raster, and signal information from INCA for the device and (optional) raster
        'name passed in.
        'This routine only does anything if DeviceDataAvailable is false indicating that we do not yet have the DeviceRasterSignalList.txt file
        'available.
        'Refactored to avoid assignments to expression results and to minimize ReDim/Preserve usage.

        Dim measureelementnames() As String = Nothing
        Dim DeviceIndex As Integer = -1
        Dim normalizedDeviceName As String = If(DeviceName, String.Empty).Trim()

        Try
            ' validate devices
            If _Devices Is Nothing OrElse _Devices.Length = 0 Then
                MsgBox("PerformDeviceSignalRetrieval: Invalid Device List")
                Exit Sub
            End If

            ' locate device index
            For i As Integer = 0 To UBound(_Devices)
                If String.Equals(_Devices(i).myName, DeviceName, StringComparison.Ordinal) Then
                    DeviceIndex = i
                    Exit For
                End If
            Next

            If DeviceIndex < 0 Then
                MsgBox("PerformDeviceSignalRetrieval: Invalid Device List")
                Exit Sub
            End If

            If Not DeviceDataAvailable Then
                ' Ensure deviceinfo structure for this device exists
                If deviceinfo Is Nothing Then
                    ReDim deviceinfo(UBound(_Devices))
                End If
                If deviceinfo(DeviceIndex).rasters Is Nothing Then
                    ReDim deviceinfo(DeviceIndex).rasters(0)
                End If

                ' Detect processor-like devices (Va*/Ve* variables)
                If InStr(normalizedDeviceName, "ETK") > 0 Or InStr(normalizedDeviceName, "ACP") > 0 Or InStr(normalizedDeviceName, "XCP:1") Then

                    ' Browse Va* elements
                    measureelementnames = Await BrowseMeasureElementsInDeviceIdxAsync("Va*", DeviceIndex)
                    Dim varList As New List(Of INCA_Variables)
                    If deviceinfo(DeviceIndex).variableinfo IsNot Nothing Then
                        varList.AddRange(deviceinfo(DeviceIndex).variableinfo)
                    End If
                    If measureelementnames IsNot Nothing Then
                        For x = 0 To UBound(measureelementnames)
                            Dim entry As INCA_Variables
                            entry.variablename = measureelementnames(x)
                            If InStr(entry.variablename, "[x]") > 0 Then entry.variablename &= "_[0]"
                            varList.Add(entry)
                        Next
                    End If

                    ' Browse Ve* elements and append
                    measureelementnames = Await BrowseMeasureElementsInDeviceIdxAsync("Ve*", DeviceIndex)
                    If measureelementnames IsNot Nothing Then
                        For x = 0 To UBound(measureelementnames)
                            Dim entry As INCA_Variables
                            entry.variablename = measureelementnames(x)
                            If InStr(entry.variablename, "[x]") > 0 Then entry.variablename &= "_[0]"
                            varList.Add(entry)
                        Next
                    End If

                    deviceinfo(DeviceIndex).variableinfo = If(varList.Count > 0, varList.ToArray(), Nothing)

                    ' Optionally write processor variables to tmpPath
                    If Not String.IsNullOrEmpty(tmpPath) Then
                        Using sw As New System.IO.StreamWriter(tmpPath, True, System.Text.Encoding.UTF8)
                            Dim vars = deviceinfo(DeviceIndex).variableinfo
                            If vars IsNot Nothing Then
                                For y = 0 To UBound(vars)
                                    sw.WriteLine(vars(y).variablename)
                                Next
                            End If
                        End Using
                    End If

                Else
                    ' Non-processor device: build variable and raster lists
                    If Not RasterInfoRetrieved(DeviceIndex) Then
                        measureelementnames = Await GetAllMeasureElementNamesInDeviceIdxAsync(DeviceIndex)
                        If measureelementnames Is Nothing Then measureelementnames = New String() {}

                        Dim varList As New List(Of INCA_Variables)
                        If deviceinfo(DeviceIndex).variableinfo IsNot Nothing Then
                            varList.AddRange(deviceinfo(DeviceIndex).variableinfo)
                        End If

                        Dim rasterList As New List(Of INCA_Rasters)
                        If deviceinfo(DeviceIndex).rasters IsNot Nothing Then
                            rasterList.AddRange(deviceinfo(DeviceIndex).rasters)
                        End If

                        For x = 0 To UBound(measureelementnames)
                            Dim v As INCA_Variables
                            v.variablename = measureelementnames(x)
                            v.defaultrastername = GetDefaultRasterForMeasureElementInDevice(DeviceName, v.variablename)
                            varList.Add(v)

                            ' find raster in current rasterList
                            Dim foundIndex = rasterList.FindIndex(Function(r) String.Equals(r.rastername, v.defaultrastername, StringComparison.Ordinal))
                            If foundIndex >= 0 Then
                                Dim currentVars As New List(Of String)
                                ' copy the struct out, modify it, then store it back
                                Dim tempRaster As INCA_Rasters = rasterList(foundIndex)
                                If tempRaster.variables IsNot Nothing Then currentVars.AddRange(tempRaster.variables)
                                ' maintain previous "empty slot" semantics
                                If currentVars.Count = 0 OrElse Not String.IsNullOrEmpty(currentVars(0)) Then
                                    currentVars.Add(v.variablename)
                                Else
                                    currentVars(0) = v.variablename
                                End If
                                tempRaster.variables = currentVars.ToArray()
                                rasterList(foundIndex) = tempRaster
                            Else
                                Dim newRaster As INCA_Rasters
                                newRaster.rastername = v.defaultrastername
                                newRaster.variables = New String() {v.variablename}
                                rasterList.Add(newRaster)
                            End If
                        Next

                        If varList.Count > 0 Then deviceinfo(DeviceIndex).variableinfo = varList.ToArray()
                        If rasterList.Count > 0 Then deviceinfo(DeviceIndex).rasters = rasterList.ToArray()

                        RasterInfoRetrieved(DeviceIndex) = True
                    End If

                    ' If RasterName specified and tmpPath provided, write that raster's vars to tmpPath
                    If Not String.IsNullOrEmpty(tmpPath) AndAlso Not String.IsNullOrEmpty(RasterName) Then
                        Using sw As New System.IO.StreamWriter(tmpPath, True, System.Text.Encoding.UTF8)
                            Dim rasters = deviceinfo(DeviceIndex).rasters
                            If rasters IsNot Nothing Then
                                For y = 0 To UBound(rasters)
                                    If String.Equals(rasters(y).rastername, RasterName, StringComparison.Ordinal) Then
                                        Dim vars = rasters(y).variables
                                        If vars IsNot Nothing Then
                                            For x = 0 To UBound(vars)
                                                sw.WriteLine(vars(x))
                                            Next
                                        End If
                                        Exit For
                                    End If
                                Next
                            End If
                        End Using
                    End If
                End If
            End If

            DeviceDataRetrieved = True
            Exit Sub

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "PerformDeviceSignalRetrieval: " & ex.Message, DisplayMsgBox)
        End Try
    End Sub

    Public Async Function BrowseMeasureElementsInDeviceIdxAsync(ByVal searchstr As String, ByVal index As Integer) As Task(Of String())

        'Called by various routines.  Takes a search string such as "Ve*" and the index of one of the defined INCA devices, and
        'returns a string array with all variables in the device which match the searchstr criteria.

        Dim y As Integer
        Dim myMeasureElementNamesInDevice() As String

        ReDim myMeasureElementNamesInDevice(0)

        If _MeasureElementNamesInDevice Is Nothing Then
            Await GetAvailableDevicesAsync(False)
        End If

        ReDim _MeasureElementNamesInDevice(UBound(_Devices), 0)

        If _MeasureElementNamesInDevice(index, 0) Is Nothing Then

            myMeasureElementNamesInDevice = MyGmIncaComm.BrowseMeasureElementsInDevice(searchstr, _Devices(index).myName)

            If UBound(myMeasureElementNamesInDevice) > UBound(_MeasureElementNamesInDevice, 2) Then
                ReDim Preserve _MeasureElementNamesInDevice(UBound(_Devices), UBound(myMeasureElementNamesInDevice))
            End If
            For y = 0 To UBound(myMeasureElementNamesInDevice)
                _MeasureElementNamesInDevice(index, y) = myMeasureElementNamesInDevice(y)
            Next

        End If

        For y = 0 To UBound(_MeasureElementNamesInDevice, 2)

            If _MeasureElementNamesInDevice(index, y) IsNot Nothing Then
                ReDim Preserve myMeasureElementNamesInDevice(y)
                myMeasureElementNamesInDevice(y) = _MeasureElementNamesInDevice(index, y)
            Else
                Exit For
            End If
        Next y

        Return myMeasureElementNamesInDevice

    End Function

    Public Function UnlockExperiment() As Boolean
        UnlockExperiment = MyGmIncaComm.UnlockExperiment
    End Function

    Public Async Function GetAllMeasureElementNamesInDeviceIdxAsync(ByVal index As Integer) As Task(Of String())

        'Called by various routines.  Takes the index of one of the defined INCA devices as input, and
        'returns a string array with all variables in the device.

        Dim y As Integer
        Dim myMeasureElementNamesInDevice() As String

        ReDim myMeasureElementNamesInDevice(0)

        If _MeasureElementNamesInDevice Is Nothing Then
            Await GetAvailableDevicesAsync(False)
            ReDim _MeasureElementNamesInDevice(UBound(_Devices), 0)
        End If

        If _MeasureElementNamesInDevice(index, 0) Is Nothing Then

            myMeasureElementNamesInDevice = MyGmIncaComm.GetAllMeasureElementNamesInDevice(_Devices(index).myName)

            If UBound(myMeasureElementNamesInDevice) > UBound(_MeasureElementNamesInDevice, 2) Then
                ReDim Preserve _MeasureElementNamesInDevice(UBound(_Devices), UBound(myMeasureElementNamesInDevice))
            End If
            For y = 0 To UBound(myMeasureElementNamesInDevice)
                _MeasureElementNamesInDevice(index, y) = myMeasureElementNamesInDevice(y)
            Next
        End If

        For y = 0 To UBound(_MeasureElementNamesInDevice, 2)

            If _MeasureElementNamesInDevice(index, y) IsNot Nothing Then
                ReDim Preserve myMeasureElementNamesInDevice(y)
                myMeasureElementNamesInDevice(y) = _MeasureElementNamesInDevice(index, y)
            Else
                Exit For
            End If
        Next y

        Return myMeasureElementNamesInDevice

    End Function

    Function SaveCalSnapShot(ByVal Page As String) As String

        'Calls MyGmIncaComm.SaveCalSnapShot which handles saving Page (passed in, either Working or Reference page) cals to .CSV file...

        Dim teststring As String = ""

        Static MessageDisplayed As Boolean

        Try

            teststring = MyGmIncaComm.SaveCalSnapShot(Page)

            If teststring = "Fatal Error" And MessageDisplayed = False Then
                HandleUserMessageLogging("GMRC", "CAL Snapshots for one or more processor were not saved.  CLEVIR has detected an issue with the INCA Application and will now be shut down.", DisplayMsgBox)
                MessageDisplayed = True
                GmResidentClient.ExitApp("Complete")
            End If

            If teststring = "Failed" And MessageDisplayed = False Then
                HandleUserMessageLogging("GMRC", "CAL Snapshots for one or more processor were not saved.  Please check " & My.Application.Info.DirectoryPath & "\LabFiles directory to verify that LAB files exist (for each processor) which correspond to the software version (i.e. 1818125) that you are using.", DisplayMsgBox)
                MessageDisplayed = True
            End If

            If teststring = "No Processors defined in Workspace" And MessageDisplayed = False Then
                HandleUserMessageLogging("GMRC", "No Processors defined in Workspace.  Save CAL Snapshot not applicable, suggest un-checking the Save CAL Snapshot box on the login screen.", DisplayMsgBox)
                MessageDisplayed = True
            End If

            SaveCalSnapShot = teststring

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "INCA_InterfaceClass: SaveCalSnapshot: " & ex.Message)
            SaveCalSnapShot = teststring
        End Try

    End Function
    ' ===================================================================
    ' Modified StartRecording() method
    ' ===================================================================
    Private Sub StartRecording()
        'Sends the Start Record command to INCA

        HandleUserMessageLogging("GMRC", "StartRecording: Start Recording Requested...")

        _Recording = MyGmIncaComm.StartRecording()

        If _Recording Then
            ' ================================================================
            ' ✅ STAGE 1: Display "Recording Starting..." with yellow background
            ' ================================================================
            OnVehicleScreen.Label5.Visible = True
            OnVehicleScreen.Refresh()

            ' ================================================================
            ' Write Initial Event Comments
            ' ================================================================
            WriteEventComment("Test Vehicle: " & VehicleNumber)
            WriteEventComment("INCA Workspace: " & INCAWorkspace)
            WriteEventComment("INCA Experiment: " & INCAExperiment)
            WriteEventComment("INCA Variable (Signal List) File: " & INCAVariableFile)

            HandleUserMessageLogging("GMRC", "StartRecording: Recording Started")

            ' ================================================================
            ' Enable FusionStatusDisplay Buttons (if visible)
            ' ================================================================
            If FusionStatusDisplay.Visible Then
                FusionStatusDisplay.Button1.Enabled = True
                FusionStatusDisplay.Button2.Enabled = True
                FusionStatusDisplay.Button3.Enabled = True
                FusionStatusDisplay.Button4.Enabled = True
                FusionStatusDisplay.Button5.Enabled = True
                FusionStatusDisplay.Button6.Enabled = True
                FusionStatusDisplay.Button8.Enabled = True
                FusionStatusDisplay.Refresh()
            End If

            ' ================================================================
            ' Enable LMFR Status Display Buttons (if visible)
            ' ================================================================
            If LmfrStatusDisplayGlobalA.Visible Then
                LmfrStatusDisplayGlobalA.Button1.Enabled = True
                LmfrStatusDisplayGlobalA.Refresh()
            End If

            ' ================================================================
            ' Log Reference Dataset Paths
            ' ================================================================
            If ReferenceDataSetDataBasePaths IsNot Nothing Then
                For x = 0 To UBound(ReferenceDataSetDataBasePaths)
                    WriteEventComment(ReferenceDataSetDataBasePaths(x))
                Next
            End If

            ' ================================================================
            ' Log Session Metadata (Comments, Location, System Tested)
            ' ================================================================
            If Not String.IsNullOrEmpty(SessionComments) Then
                WriteEventComment("Recording Session - Comments: " & SessionComments)
            End If

            If Not String.IsNullOrEmpty(SessionLocation) Then
                WriteEventComment("Recording Session - Location: " & SessionLocation)
            End If

            If Not String.IsNullOrEmpty(SessionRing) Then
                WriteEventComment("Recording Session - System Tested: " & SessionRing)
            End If

            ' ================================================================
            ' Set Recording State Flags
            ' ================================================================
            HaveRecorded = True
            GmResidentClient.MyMainTabControl.Enabled = True

            ' ================================================================
            ' Handle Alternate Recording (CANalyzer/VehicleSpy)
            ' ================================================================
            If AlternateRecordingMode <> "None" AndAlso AlternateRecordEnabled Then
                If LoginForm.CheckBox3.Checked Then
                    If AlternateRecordingMode <> "VehicleSpy" Then
                        StartCanalyzer()
                    Else
                        StartVehicleSpy()
                    End If
                ElseIf OnVehicleScreen.Label3.Visible Then
                    HandleUserMessageLogging("GMRC", "Please exit CLEVIR and restart to re-enable " & AlternateRecordingMode & " Recording.", , , FlashMsg3Sec)
                    OnVehicleScreen.Label3.BackColor = Color.Red
                End If
            End If

            ' ════════════════════════════════════════════════════════════════
            ' LiDAR capture + optional time sync provider wiring
            ' ════════════════════════════════════════════════════════════════
            If LidarCaptureEnabled AndAlso LoginForm.CheckBox_LidarCapture.Checked Then
                Try
                    HandleUserMessageLogging("GMRC", "StartRecording: Initializing LiDAR capture...")

                    ' Optional provider wiring (capture must NOT depend on time sync availability)
                    If GmResidentClient.MyTimeSyncProvider IsNot Nothing Then
                        HandleUserMessageLogging("GMRC", $"StartRecording: LiDAR using time sync provider '{GmResidentClient.MyTimeSyncProvider.ProviderName}'")
                        For Each lidar In LidarDevices
                            lidar.SetTimeSyncProvider(GmResidentClient.MyTimeSyncProvider)
                        Next
                    Else
                        HandleUserMessageLogging("GMRC", "StartRecording: LiDAR capture proceeding without time sync provider (system-time timestamps)")
                    End If

                    ' Optional OXTS-specific lock wait only when OXTS is active
                    If GmResidentClient.MyOxtsInterface IsNot Nothing AndAlso GmResidentClient.OxtsWaitForLockOnStart Then
                        StatusNotifier.Toast("Waiting for OXTS GPS lock...", ToastKind.Info, "OXTS", 3000, True)
                        Dim gpsLocked = GmResidentClient.MyOxtsInterface.WaitForGpsLock(timeoutMs:=30000)

                        If Not gpsLocked Then
                            StatusNotifier.ToastError("OXTS GPS lock timeout - timestamps may be inaccurate", "RTK Warning", durationMs:=12000, ensureMainOnTop:=False)
                            HandleUserMessageLogging("GMRC", "StartRecording: OXTS GPS lock failed - continuing with available time source")
                        Else
                            StatusNotifier.Toast($"OXTS locked (offset: {GmResidentClient.MyOxtsInterface.TimeOffset.TotalMilliseconds:F1}ms)", ToastKind.Info)
                            HandleUserMessageLogging("GMRC", $"StartRecording: OXTS locked - TimeOffset: {GmResidentClient.MyOxtsInterface.TimeOffset.TotalMilliseconds:F3}ms")
                        End If
                    End If

                    ' Always start LiDAR capture when enabled
                    HandleUserMessageLogging("GMRC", "StartRecording: Starting LiDAR capture...")
                    StartLidarCapture()

                    ' Inject GPS marker only when OXTS data is available
                    If LidarCaptureStarted AndAlso GmResidentClient.MyOxtsInterface IsNot Nothing Then
                        Dim position = GmResidentClient.MyOxtsInterface.GetCurrentPosition()
                        Dim currentActiveSequence As String = GetCurrentActiveSequence()
                        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

                        Dim msg = $"Lat:{position.Latitude:F8}|Lon:{position.Longitude:F8}|Alt:{position.Altitude:F2}|Hdg:{position.Heading:F2}"

                        For Each lidar In LidarDevices
                            If lidar.IsCapturing Then
                                lidar.InjectEventMarker("GPS_START", msg, currentSeq)
                            End If
                        Next

                        HandleUserMessageLogging("GMRC", $"StartRecording: GPS start markers injected into {LidarDevices.Count} devices")
                    End If
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"StartRecording: LiDAR startup failed - {ex.Message}")
                End Try
            End If

            ' ════════════════════════════════════════════════════════════════
            ' ✅ NEW: Start OXTS NCOM PCAP Capture (parallel to LiDAR)
            ' ════════════════════════════════════════════════════════════════
            If OxtsCaptureEnabled AndAlso LoginForm.CheckBox_LidarCapture.Checked Then
                Try
                    HandleUserMessageLogging("GMRC", "StartRecording: Starting OXTS NCOM capture...")
                    StartOxtsCapture()

                    ' Inject GPS start marker
                    If OxtsCaptureStarted AndAlso GmResidentClient.MyOxtsInterface IsNot Nothing Then
                        Dim position = GmResidentClient.MyOxtsInterface.GetCurrentPosition()
                        Dim currentActiveSequence As String = GetCurrentActiveSequence()
                        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

                        Dim msg = $"Lat:{position.Latitude:F8}|Lon:{position.Longitude:F8}|Alt:{position.Altitude:F2}|Hdg:{position.Heading:F2}"
                        InjectOxtsEventMarker("GPS_START", msg, currentSeq)

                        HandleUserMessageLogging("GMRC", "StartRecording: GPS start marker injected into OXTS PCAP")
                    End If

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"StartRecording: OXTS NCOM capture failed - {ex.Message}")
                End Try
            End If

            ' ================================================================
            ' ✅ STAGE 2: Keep "Recording Starting..." visible for 2 seconds
            ' Then query filename and display
            ' ================================================================
            Thread.Sleep(2000)

            Try
                Dim lastClosedFile As String = MyGmIncaComm.GetLastRecordingFileName()

                ' ✅ FIXED: Increment sequence from last closed file to get CURRENT recording filename
                If Not String.IsNullOrEmpty(lastClosedFile) Then
                    ' Parse sequence number from last closed file (e.g., "_02.mf4" → 2)
                    Dim match = System.Text.RegularExpressions.Regex.Match(lastClosedFile, "_(\d+)\.mf4$",
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase)

                    If match.Success Then
                        Dim lastSeq As Integer = Integer.Parse(match.Groups(1).Value)
                        Dim currentSeq As Integer = lastSeq + 1  ' Increment for current recording

                        ' Reconstruct filename with new sequence
                        Dim baseName As String = System.Text.RegularExpressions.Regex.Replace(
                            lastClosedFile, "_\d+\.mf4$", "",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase)

                        CachedRecordingFilename = $"{baseName}_{currentSeq:D2}.mf4"
                        SaveRecordingFileName = CachedRecordingFilename

                        HandleUserMessageLogging("GMRC",
                            $"StartRecording: Current recording (seq {currentSeq}): {Path.GetFileName(CachedRecordingFilename)}")
                    Else
                        ' Fallback if regex fails - use as-is
                        CachedRecordingFilename = lastClosedFile
                        SaveRecordingFileName = lastClosedFile
                        HandleUserMessageLogging("GMRC",
                            $"StartRecording: Warning - Could not parse sequence from: {lastClosedFile}")
                    End If

                ElseIf String.IsNullOrEmpty(SaveRecordingFileName) OrElse
                       Not SaveRecordingFileName.EndsWith("_01.mf4", StringComparison.OrdinalIgnoreCase) Then
                    ' First recording - predict _01.mf4
                    Dim baseName As String = SaveSelectedTestName
                    If baseName.EndsWith("_") Then baseName = baseName.TrimEnd("_"c)

                    CachedRecordingFilename = $"{baseName}_01.mf4"
                    SaveRecordingFileName = CachedRecordingFilename

                    HandleUserMessageLogging("GMRC",
                        $"StartRecording: New session - initial filename: {Path.GetFileName(CachedRecordingFilename)}")
                Else
                    ' Use existing SaveRecordingFileName (already predicted in SetupDataLogging)
                    CachedRecordingFilename = SaveRecordingFileName

                    HandleUserMessageLogging("GMRC",
                        $"StartRecording: Using SetupDataLogging prediction: {Path.GetFileName(CachedRecordingFilename)}")
                End If

                ' ✅ FIXED: Display only filename, not full path
                Dim displayName As String = If(String.IsNullOrEmpty(CachedRecordingFilename),
                                              "[Pending...]",
                                              Path.GetFileName(CachedRecordingFilename))
                OnVehicleScreen.Label5.Text = $"Recording Filename: {displayName}"
                OnVehicleScreen.Label5.BackColor = SystemColors.Control
                OnVehicleScreen.Refresh()

                LastKnownRecordingTimeMs = 0
                LastFilenameUpdateTime = DateTime.Now

            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"StartRecording: Filename query error - {ex.Message}")
                OnVehicleScreen.Label5.Text = "Recording Filename: [Pending...]"
                OnVehicleScreen.Label5.BackColor = SystemColors.Control
            End Try
        End If
    End Sub

    Private Sub StopRecording()
        'Sends the stop recording command to INCA

        If FusionStatusDisplay.Visible Then
            FusionStatusDisplay.Button1.Enabled = False
            FusionStatusDisplay.Button2.Enabled = False
            FusionStatusDisplay.Button3.Enabled = False
            FusionStatusDisplay.Button4.Enabled = False
            FusionStatusDisplay.Button5.Enabled = False
            FusionStatusDisplay.Button6.Enabled = False
            FusionStatusDisplay.Button8.Enabled = False
            FusionStatusDisplay.Refresh()
        End If

        If LmfrStatusDisplayGlobalA.Visible Then
            LmfrStatusDisplayGlobalA.Button1.Enabled = False
            LmfrStatusDisplayGlobalA.Refresh()
        End If

        HandleUserMessageLogging("GMRC", "StopRecording: Stop Recording Requested...")

        ' Handle alternate recording stop/restart
        If CanalyzerCaptureStarted Then
            Try
                StopCanalyzer()
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"StopRecording: CANalyzer failed to stop - {ex.Message}")
            End Try
        End If

        If VehicleSpyCaptureStarted Then
            Try
                StopVehicleSpy()
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"StopRecording: VehicleSpy failed to stop - {ex.Message}")
            End Try
        End If

        ' ════════════════════════════════════════════════════════════════
        ' ✅ INJECT GPS STOP MARKERS + STOP CAPTURES
        ' ════════════════════════════════════════════════════════════════
        If LidarCaptureStarted OrElse OxtsCaptureStarted Then
            Try
                ' Inject GPS stop markers BEFORE stopping
                If GmResidentClient.MyOxtsInterface IsNot Nothing AndAlso GmResidentClient.MyOxtsInterface.IsGpsLocked Then
                    Dim position = GmResidentClient.MyOxtsInterface.GetCurrentPosition()
                    Dim currentActiveSequence As String = GetCurrentActiveSequence()
                    Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)

                    Dim msg = $"Lat:{position.Latitude:F8}|Lon:{position.Longitude:F8}|Alt:{position.Altitude:F2}"

                    ' Inject into LiDAR devices
                    For Each lidar In LidarDevices
                        If lidar.IsCapturing Then
                            lidar.InjectEventMarker("GPS_STOP", msg, currentSeq)
                        End If
                    Next

                    ' ✅ NEW: Inject into OXTS PCAP
                    If OxtsCaptureStarted Then
                        InjectOxtsEventMarker("GPS_STOP", msg, currentSeq)
                    End If

                    HandleUserMessageLogging("GMRC", "StopRecording: GPS stop markers injected")
                End If

                Thread.Sleep(200) ' Allow markers to flush

                ' Stop LiDAR capture
                If LidarCaptureStarted Then
                    StopLidarCapture()
                End If

                ' ✅ NEW: Stop OXTS NCOM capture
                If OxtsCaptureStarted Then
                    StopOxtsCapture()
                End If

            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"StopRecording: Capture stop failed - {ex.Message}")
            End Try
        End If

        _StopRecordingRequested = True

        HandleUserMessageLogging("GMRC", $"StopRecording: Using session base filename '{SelectedTestName}'")

        If (MyGmIncaComm.StopRecording(SelectedTestName, RecordingFileFormat) = True) Then
            Do While Me.GetRecordingState
                ActiveIncaApiCall = "Waiting For INCA to Report RecordingState False"
                System.Threading.Thread.Sleep(250)
            Loop

            ActiveIncaApiCall = String.Empty

            _Recording = False

        End If
        _StopRecordingRequested = False

        OnVehicleScreen.Label8.Text = ""
        OnVehicleScreen.Label5.Text = "Recording Stopped"
        HandleUserMessageLogging("GMRC", "StopRecording: Recording Stopped")
    End Sub
    Public Function GetLastRecordingFileName() As String
        Try
            Dim result As String = MyGmIncaComm.GetLastRecordingFileName()
            ' Normalize legacy "Invalid" sentinel to empty string for easier caller handling
            If String.Equals(result, "Invalid", StringComparison.OrdinalIgnoreCase) Then
                Return String.Empty
            End If
            Return If(result, String.Empty)
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "GetLastRecordingFileName: Exception - " & ex.Message)
            Return String.Empty
        End Try
    End Function

    Public Function SetLastRecordingFileName(ByVal filename As String) As Boolean
        Try
            If String.IsNullOrEmpty(filename) Then Return False
            If MyGmIncaComm IsNot Nothing Then
                Try
                    Return MyGmIncaComm.SetLastRecordingFileName(filename)
                Catch ex As Exception
                    ' Centralized logging; this helper is used across project
                    HandleUserMessageLogging("GMRC", "INCA_InterfaceClass.SetLastRecordingFileName: " & ex.Message)
                    Return False
                End Try
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "INCA_InterfaceClass.SetLastRecordingFileName: " & ex.Message)
        End Try
        Return False
    End Function

    Public Sub StopMeasurement()

        'Invokes the StopMeasurement method

        Dim reply As Boolean

        Try

            If FusionStatusDisplay.Visible Then
                FusionStatusDisplay.Button1.Enabled = False
                FusionStatusDisplay.Button2.Enabled = False
                FusionStatusDisplay.Button3.Enabled = False
                FusionStatusDisplay.Button4.Enabled = False
                FusionStatusDisplay.Button5.Enabled = False
                FusionStatusDisplay.Button6.Enabled = False
                FusionStatusDisplay.Button8.Enabled = False
                FusionStatusDisplay.Refresh()
            End If

            If LmfrStatusDisplayGlobalA.Visible Then
                LmfrStatusDisplayGlobalA.Button1.Enabled = False
                LmfrStatusDisplayGlobalA.Refresh()
            End If

            If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
                OnVehicleScreen.Button4.Enabled = True
                If GmResidentClient IsNot Nothing AndAlso GmResidentClient.MyLogin IsNot Nothing Then
                    GmResidentClient.MyLogin.Enabled = True
                End If
            End If

            If GetMeasurementStatus() = "True" Then

                StopDataCollection()

                reply = MyGmIncaComm.StopMeasurement()

                If reply = True Then

                    Do While GetMeasurementStatus() = "True"
                        System.Threading.Thread.Sleep(100)
                    Loop

                    _MeasurementStarted = False
                    _Recording = False
                    RecordPlayback.Record.Enabled = False
                    RecordPlayback.StopRecord.Enabled = False
                    RecordPlayback.SelectFile.Enabled = True

                End If

            End If

            CheckRecordingFileNameFormat(displayMsg:=True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "INCA_InterfaceClass: StopMeasurement: " & ex.Message)
        End Try

    End Sub

    Public Sub WriteEventComment(ByVal CommentString As String, Optional ByVal DisplayWindow As Boolean = False)

        'This routine is called when we want to write an event comment into the INCA recording file...
        'We simply pass in the comment string that we want to have written.  When ever we write a comment
        'we display the comment string on the main form for a period of time....

        Dim reply As Boolean

        reply = MyGmIncaComm.WriteEventComment(CommentString, False)

        If DisplayWindow = True Then
            HandleUserMessageLogging("GMRC", CommentString,,, FlashMsg1Sec)
        End If

    End Sub

    Public Function GetLastINCAError() As String
        GetLastINCAError = MyGmIncaComm.GetLastINCAError
    End Function

    Public Function GetRecordingFileFormat() As String
        GetRecordingFileFormat = MyGmIncaComm.GetRecordingFileFormat
    End Function

    Public Function SetRecordingFileFormat(ByVal FileFormat As String) As Boolean
        SetRecordingFileFormat = MyGmIncaComm.SetRecordingFileFormat(FileFormat)
    End Function

    Public Function GetRecordingFileName() As String
        GetRecordingFileName = MyGmIncaComm.GetRecordingFileName
    End Function

    Public Function GetRecordingFileNameTemplate() As String
        GetRecordingFileNameTemplate = MyGmIncaComm.GetRecordingFileNameTemplate
    End Function

    Private Async Function SetupDataLogging(ByVal myLoginID As String) As Task(Of Boolean)
        ' This routine is called from StartStopMeasurement and StartStopRecord.
        ' It sets up the data file name and directory based on the user's ID, current date and time,
        ' and the vehicle number. It also sets the Annotation file name based on the same information.
        ' Integrated: ensure the chosen base recording name is unique on disk so INCA will start sequence at 01.
        ' ✅ REFACTORED: Async with Await pattern

        Try
            ' Initialize the setup success flag
            Dim isSetupSuccessful As Boolean = False

            ' Reset the IgnoreLostDevice flag if needed
            If IgnoreLostDeviceUntilNextRecordingSession Then
                IgnoreLostDeviceUntilNextRecordingSession = False
            End If

            ' Build the session folder name: yyyyMMdd_HHmmss_LoginID
            Dim sessionFolder As String = DateTime.Now.ToString("yyyyMMdd_HHmmss") & "_" & myLoginID
            Dim recordingBaseName As String = sessionFolder & "_" & VehicleNumber
            Dim attempt As Integer = 0

            ' Determine folder paths for collision checking
            Dim sessionFolderPath As String = Path.Combine(BaseDataCollectionPath, "Data")
            Dim vehicleSubfolderPath As String = Path.Combine(sessionFolderPath, "gmcsv" & VehicleNumber)

            ' Loop to ensure uniqueness: if a collision is found (existing folder or mf4 files for the base),
            ' append a tiny unique suffix (milliseconds + counter) and retry.
            Do
                Dim sessionFolderFullPath As String = Path.Combine(vehicleSubfolderPath, sessionFolder)

                Dim collision As Boolean = False

                If Directory.Exists(sessionFolderFullPath) Then
                        Dim files() As String = Directory.GetFiles(sessionFolderFullPath, recordingBaseName & "*.mf4")
                        If files IsNot Nothing AndAlso files.Length > 0 Then
                            collision = True
                        End If
                    End If

                If Not collision Then Exit Do

                ' Collision detected -> modify base and retry
                attempt += 1
                sessionFolder = DateTime.Now.ToString("yyyyMMdd_HHmmss") & "_" & myLoginID & "_" & DateTime.Now.ToString("fff") & "_" & attempt.ToString("D3")
                recordingBaseName = sessionFolder & "_" & VehicleNumber
                Threading.Thread.Sleep(1)
            Loop While attempt < 1000

            SaveSelectedTestName = recordingBaseName

            ' Set up the final data path and annotation file name

            FinalPathToSaveData = Path.Combine(vehicleSubfolderPath, sessionFolder)

            ANNOFileName = Path.Combine(FinalPathToSaveData, SaveSelectedTestName & "_ANNO.csv")

            Dim response As String = "undefined"

            ' Loop until there are no errors or warnings from the INCA API calls
            Do While Not String.IsNullOrEmpty(response)
                FinalPathToSaveData = String.Empty

                ' ✅ FIXED: Await the async call
                response = Await MyGmIncaComm.SetupDataLogging(recordingBaseName, myLoginID)

                If Not String.IsNullOrEmpty(response) Then
                    ' Handle errors or user warnings
                    Select Case response
                        Case "INCA Auto Increment Flag"
                            HandleUserMessageLogging("GMRC", "INCA Auto Increment Flag was set to False in the Experiment. If you are using your own custom experiment, please make sure that the Auto Increment Flag is set to True and the Increment digits value is set to 2. Also, the 'Use date/time in file name' box must be UNCHECKED in the Measurement Recorder Configuration. Then save the experiment and click OK.", DisplayMsgBox)
                        Case "SetRecordingPathName returned FALSE"
                            GmResidentClient.Label3.Text = "Invalid Record PathName"
                            HandleUserMessageLogging("GMRC", response, DisplayMsgBox)
                        Case "Invalid INCA Recording File Format. You MUST change INCA MDF File Type to MDF 4.0. (Options / User Options / Experiment / Measure / MDF File Type)"
                            HandleUserMessageLogging("GMRC", response & " before continuing. Exiting...", DisplayMsgBox)
                            GmResidentClient.ExitApp("Complete")
                        Case "SetRecordingFileName returned FALSE"
                            GmResidentClient.Label4.Text = "Invalid Record Filename, Exiting..."
                            HandleUserMessageLogging("GMRC", response, DisplayMsgBox)
                            GmResidentClient.ExitApp("Complete")
                        Case Else
                            HandleUserMessageLogging("GMRC", "SetupDataLogging ERROR: " & response, DisplayMsgBox)
                            GmResidentClient.ExitApp("Complete")
                    End Select
                Else
                    ' No errors returned; proceed with setup
                    CreateANNOFile()
                    SaveLoginID = myLoginID

                    If Not String.IsNullOrEmpty(SaveLoginID) Then
                        ' ✅ REFACTORED: Only set internal state - don't display filename yet
                        ' StartRecording() will handle user-facing display when recording actually starts

                        ' Initialize SaveRecordingFileName for internal tracking
                        Dim sequenceNumber As Integer = 1
                        Dim predictedFileName As String = $"{recordingBaseName}_{sequenceNumber:D2}.mf4"
                        SaveRecordingFileName = predictedFileName

                        HandleUserMessageLogging("GMRC", $"SetupDataLogging: Session initialized - base: {recordingBaseName}")

                        ' Update other UI elements with path info (not filename yet)
                        GmResidentClient.Label3.Text = MyIncaInterface.GetRecordingPathName()
                        GmResidentClient.Label4.Text = SaveRecordingFileName
                        'LoginForm.Button1.Text = "Enter Application - Logged in as " & SaveLoginID
                    Else
                        'LoginForm.Button1.Text = "Enter Application (You are not logged in)"
                        OnVehicleScreen.Label5.Text = "You are not logged in; you must log in to record data."
                        OnVehicleScreen.Label5.BackColor = Color.LightYellow
                    End If

                    ' Exit the loop since setup was successful
                    Exit Do
                End If
            Loop

            SelectedTestName = $"{recordingBaseName}_"
            isSetupSuccessful = True

            Return isSetupSuccessful

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "INCA_InterfaceClass: SetupDataLogging: " & ex.Message)
            Return False
        End Try
    End Function

    Public Function AddUserToList(ByVal UserName As String) As Boolean
        Return MyGmIncaComm.AddUserToList(UserName)
    End Function

    Public Sub ReadUserIDList()
        MyGmIncaComm.ReadUserIDList()
    End Sub

    'Public Function GetGM_INCA_CommStatus() As String()
    '    GetGM_INCA_CommStatus = MyGmIncaComm.GetGM_INCA_CommStatus
    'End Function

    Public Function HandleWorkspace(ByVal EtasUserName As String, ByVal RegisterIntoNewBlankExp As Boolean) As String
        HandleWorkspace = MyGmIncaComm.HandleWorkspace(EtasUserName, RegisterIntoNewBlankExp)
    End Function


    Public Function ImportFileIntoINCA(ByVal Filename As String, ByVal overwrite As Boolean, ByVal discardimpl As Boolean) As Boolean

        'Called from numerous places when a file needs to be imported into INCA.  Calls a function in MyGmIncaComm that performs
        'the INCA import function.

        ImportFileIntoINCA = MyGmIncaComm.ImportFileIntoINCA(Filename, overwrite, discardimpl)

    End Function

    Function SwitchToWorkingPage() As Boolean
        SwitchToWorkingPage = MyGmIncaComm.SwitchToWorkingPage
    End Function

    Function SwitchToReferencePage() As Boolean
        SwitchToReferencePage = MyGmIncaComm.SwitchToReferencePage
    End Function

    Public Function RegisterSignals(Optional ByVal progressForm As SignalRegistrationProgressForm = Nothing) As Boolean
        Try
            ' Save the initial measurement status and stop measurement if necessary
            _InitialMeasurementStatus = GetMeasurementStatus()

            If _InitialMeasurementStatus = "True" Then
                StopMeasurement()
            End If

            If Not _MeasurementStarted Then
                ' Display appropriate message based on the signal registration mode
                Select Case SignalRegistrationMode.ToUpper()
                    Case "FULL"
                        HandleUserMessageLogging("GMRC", "RegisterSignals: Performing FULL Signal Registration...")
                        StatusNotifier.Toast("RegisterSignals: Performing FULL Signal Registration...", "INCA", durationMs:=1000, ensureMainOnTop:=False)
                    Case "DISPLAYS"
                        HandleUserMessageLogging("GMRC", "RegisterSignals: Registering DISPLAYS and GO/NOGO Signals...")
                        StatusNotifier.Toast("RegisterSignals: Registering DISPLAYS and GO/NOGO Signals...", "INCA", durationMs:=1000, ensureMainOnTop:=False)
                    Case "GO/NOGO"
                        HandleUserMessageLogging("GMRC", "RegisterSignals: Registering GO/NOGO Signals ONLY...")
                        StatusNotifier.Toast("RegisterSignals: Registering GO/NOGO Signals ONLY...", "INCA", durationMs:=1000, ensureMainOnTop:=False)
                    Case "NEW FULL"
                        HandleUserMessageLogging("GMRC", "RegisterSignals: Performing FULL Signal Registration into NEW Blank Experiment...")
                        StatusNotifier.Toast("RegisterSignals: Performing FULL Signal Registration into NEW Blank Experiment...", "INCA", durationMs:=1000, ensureMainOnTop:=False)
                        SignalRegistrationMode = "FULL"
                End Select

                GmResidentClient.ProgressBarEnable = True

                ' ✅ FIXED: Pass progressForm to the underlying implementation
                If SignalRegistrationMode.ToUpper() = "FULL" Then
                    myDisplaySignals = MyGmIncaComm.RegisterSignals(mySignals, progressForm)
                Else
                    myDisplaySignals = MyGmIncaComm.RegisterSignals(myPreliminaryDisplaySignals, progressForm)
                End If

                ' Check if signal registration was successful
                If myDisplaySignals Is Nothing Then
                    Return False
                End If

                ReDim GmResidentClient.VariableNameDataArray(UBound(myDisplaySignals) + 2, 0)

                HandleUserMessageLogging("GMRC",
                $"Signal Registration Complete. {UBound(myDisplaySignals) + 1} Display Signals Registered. {UBound(mySignals) + 1} in Signal List.")
                StatusNotifier.Toast($"Signal Registration Complete. {UBound(myDisplaySignals) + 1} Display Signals Registered. {UBound(mySignals) + 1} in Signal List.", "INCA", durationMs:=1000, ensureMainOnTop:=False)

            Else
                HandleUserMessageLogging("GMRC", "INCA Measurement must be stopped before registering signals.", , , , , , OnVehicleScreen.Label2)
            End If

            ' ✅ Bring OnVehicleScreen to front (consistent with FinalizeUI pattern)
            OnVehicleScreen.BringToFront()
            OnVehicleScreen.Activate()
            OnVehicleScreen.TopMost = False

            ' Restart measurement if it was initially running
            If _InitialMeasurementStatus = "True" Then
                StartStopMeasurement(OnVehicleScreen.Button6).GetAwaiter().GetResult()
            End If

            ' Check for specific error in signal registration status
            Dim tempStatus As String = myDisplaySignals(0).Status

            If tempStatus.Contains("Not enough storage is available to process this command") Then
                HandleUserMessageLogging("GMRC", "RegisterSignals: RedisplayOnVehicleForm(Main) called due to insufficient storage.")
                RedisplayOnVehicleForm("Main")

                MessageBox.Show("Fatal INCA Windows Memory Error. Please shut down and retry FULL Signal Registration - this is a known issue. FULL Signal Registration will work on the second try!")

                SignalRegistrationMode = "FULL"

                If Not SaveExperiment() Then
                    HandleUserMessageLogging("GMRC", "RegisterSignals: Save Experiment returned FALSE")
                Else
                    HandleUserMessageLogging("GMRC", "RegisterSignals: Save Experiment returned TRUE")
                End If

                ' Clean up and exit the application
                GmResidentClient.StopTestProcess = True
                GmResidentClient.MyTestThread = Nothing

                GmResidentClient.Hide()
                GmResidentClient.TopMost = False
                OnVehicleScreen.TopMost = False

                GmResidentClient.ExitApp()
            End If

            Return True

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"INCA_InterfaceClass.RegisterSignals: {ex.Message}", DisplayMsgBox)
            Return False
        End Try
    End Function

    Public Function GetReferenceDataSetDataBasePaths() As String()
        GetReferenceDataSetDataBasePaths = MyGmIncaComm.GetReferenceDataSetDataBasePaths
    End Function

    Public Function SaveExperiment() As Boolean
        SaveExperiment = MyGmIncaComm.SaveExperiment
    End Function

    Public Sub SetProjectDatabaseInfo()
        MyGmIncaComm.SetProjectDatabaseInfo()
    End Sub
    Public Function GetWorkingDataSetDataBasePaths() As String()
        GetWorkingDataSetDataBasePaths = MyGmIncaComm.GetWorkingDataSetDataBasePaths
    End Function

    Public Function GetRecordingState() As Boolean
        Try
            ' Attempt to retrieve the recording state
            Return MyGmIncaComm.GetRecordingState
        Catch ex As Exception
            ' Log the exception and return False as a default value
            HandleUserMessageLogging("GMRC", "GetRecordingState: Exception - " & ex.Message)
            Return False
        End Try
    End Function

    Public Function GetMeasurementStatus() As String

        Static SaveMeasurementStatus As String
        Static MeasurementStatus As String
        Static Retries As Integer

        If Len(MeasurementStatus) > 0 Then
            SaveMeasurementStatus = MeasurementStatus
        End If

        Try
            MeasurementStatus = MyGmIncaComm.GetMeasurementStatus
            GetMeasurementStatus = MeasurementStatus

            Retries = 0

        Catch ex As Exception

            GetMeasurementStatus = SaveMeasurementStatus
            Retries += 1
            HandleUserMessageLogging("GMRC", "GetMeasurementStatus: MyGmIncaComm.GetMeasurementStatus Call FAILED - Retries = " & Retries & " Exception: " & ex.Message)
            If Retries > MaxRetries Then
                HandleUserMessageLogging("GMRC", "GetMeasurementStatus: MyGmIncaComm.GetMeasurementStatus Call FAILED - Retries = " & Retries & " INCA Communication Failure Returned")
                GetMeasurementStatus = "INCA Communication Failure"
            End If

        End Try

    End Function

    Public Function ConnectToInca() As String
        'Called from various places when connection to INCA must be established.  Connection to INCA occurs at different times based on
        'what the user wants to do, so initial opening or connecting to INCA can happen in different places.  Once INCA connection is established,
        'CLEVIR remains connected, so this function will just return an indication of connection established if already connected...

        'Launches INCA if INCA not running or connects to INCA if INCA is running...
        'Also locks INCA and attaches to the INCA database.
        'Locking INCA will cause a special message to be displayed if user tries to close INCA while the CDM Interface application is still running.
        'Returns "True" if all okay, otherwise, returns string indicating nature of error.

        ConnectToInca = MyGmIncaComm.ConnectToInca
    End Function

    Public Sub CloseExperiment()
        MyGmIncaComm.CloseExperiment()
    End Sub

    Public Function ClearINCAMonitor() As Boolean

        ClearINCAMonitor = MyGmIncaComm.ClearINCAMonitor

    End Function
    Public Function InitializeHardware() As Boolean

        InitializeHardware = MyGmIncaComm.InitializeHardware()

    End Function

    Public Function WriteMonitorLogFileToPathUsingFileName(ByVal pathname As String, ByVal filename As String) As Boolean
        WriteMonitorLogFileToPathUsingFileName = MyGmIncaComm.WriteMonitorLogFileToPathUsingFileName(pathname, filename)
    End Function

    Public Sub StopAndStartRecording()
        'this routine stops the recording and restarts it.  It is called from myBackground tasks which monitors the record timer while we are
        'recording.  if we set a record duration, then we record for that amount of time,save and immediately start recording a second file.
        'we continue in this manner until the stop record button is pressed.

        Dim stopSuccessful As Boolean = False
        Dim startSuccessful As Boolean = False
        Dim retryCount As Integer = 0
        Const MaxRetries As Integer = 3
        Const RetryDelayMs As Integer = 1000

        Try
            HandleUserMessageLogging("GMRC", "StopAndStartRecording: Beginning sequence change...")

            ' Verify we're in a valid state before proceeding
            If MyGmIncaComm Is Nothing Then
                HandleUserMessageLogging("GMRC", "StopAndStartRecording: ERROR - MyGmIncaComm is not initialized", DisplayMsgBox)
                Exit Sub
            End If

            ' Check current recording state before attempting to stop
            Dim wasRecording As Boolean = False
            Try
                wasRecording = GetRecordingState()
                HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Current recording state: {wasRecording}")
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"StopAndStartRecording: WARNING - Could not verify recording state: {ex.Message}")
                ' Continue anyway as we may still need to attempt the sequence
            End Try

            ' Stop recording with retry logic
            Do While retryCount <= MaxRetries And Not stopSuccessful
                Try
                    If retryCount > 0 Then
                        HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Retry attempt {retryCount} for stop operation")
                        System.Threading.Thread.Sleep(RetryDelayMs)
                    End If

                    StopRecording()

                    ' Verify the stop was successful
                    System.Threading.Thread.Sleep(500) ' Brief delay to allow state to update
                    If Not GetRecordingState() Then
                        stopSuccessful = True
                        Dim currentActiveSequence As String = GetCurrentActiveSequence()
                        Dim currentSeq As Integer = GetSequenceNumberFromFileName(currentActiveSequence)
                        ' ✅ NEW: Mark sequence boundary in PCAP
                        If LidarCaptureStarted Then
                            InjectEventMarker("SEQUENCE", $"File Rotation: Seq {currentSeq:D2} → {currentSeq + 1:D2}", currentSeq)
                        End If

                        If OxtsCaptureStarted Then
                            InjectOxtsEventMarker("SEQUENCE", $"File Rotation: Seq {currentSeq:D2} → {currentSeq + 1:D2}", currentSeq)
                        End If

                        HandleUserMessageLogging("GMRC", "StopAndStartRecording: Stop recording successful")

                        ' Add to StopAndStartRecording after stop
                        If ZipTheMF4Files Then
                            ' ✅ ASYNC FIX: Don't block on compression
                            Task.Run(Sub()
                                         Try
                                             Dim compressionStopwatch As New Stopwatch()
                                             compressionStopwatch.Start()

                                             Dim compressedCount = CompressFilesWithLockDetection(
                                        FinalPathToSaveData,
                                        maxRetries:=5,
                                        retryDelaySeconds:=10,
                                        deleteOriginal:=False
                                        )

                                             compressionStopwatch.Stop()

                                             HandleUserMessageLogging("GMRC",
                                                             $"✅ Background compression: {compressedCount} files in {compressionStopwatch.ElapsedMilliseconds}ms")

                                             ' Only warn if compression is taking excessive time
                                             If compressionStopwatch.ElapsedMilliseconds > 30000 Then
                                                 HandleUserMessageLogging("GMRC",
                                                                 "⚠️ WARNING: Compression took >30 seconds - disk may be slow")
                                             End If

                                         Catch ex As Exception
                                             HandleUserMessageLogging("GMRC",
                                                             $"❌ Background compression error: {ex.Message}")
                                         End Try
                                     End Sub)

                            ' Don't wait - log and continue immediately
                            HandleUserMessageLogging("GMRC",
                                                     $"⚡ Compression queued for {Path.GetFileName(FinalPathToSaveData)}")
                        End If

                    Else
                        HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Stop recording may not have completed (attempt {retryCount + 1})")
                    End If

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Exception during stop operation (attempt {retryCount + 1}): {ex.Message}")
                End Try

                retryCount += 1
            Loop

            If Not stopSuccessful Then
                HandleUserMessageLogging("GMRC", $"StopAndStartRecording: FAILED to stop recording after {MaxRetries} attempts", DisplayMsgBox)
                Exit Sub
            End If

            ' Reset and restart the stopwatch
            Try
                If INCACommCheckStopWatch IsNot Nothing Then
                    INCACommCheckStopWatch.Reset()
                    INCACommCheckStopWatch.Start()
                    If Not String.IsNullOrEmpty(APICommErrorMsgDelayTime) Then
                        INCACommCheckWarningTime = Val(APICommErrorMsgDelayTime)
                        HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Reset IncaCommCheckStopWatch to {INCACommCheckWarningTime}")
                    End If
                End If

            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"StopAndStartRecording: WARNING - Stopwatch reset failed: {ex.Message}")
            End Try

            ' Check recording file name format
            Try
                If CheckRecordingFileNameFormat(displayMsg:=False) = False Then
                    HandleUserMessageLogging("GMRC", "StopAndStartRecording: Recording file name format check failed", DisplayMsgBox)
                    Exit Sub
                End If
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Exception during file name format check: {ex.Message}", DisplayMsgBox)
                Exit Sub
            End Try

            ' Start recording with retry logic
            retryCount = 0
            Do While retryCount <= MaxRetries And Not startSuccessful
                Try
                    If retryCount > 0 Then
                        HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Retry attempt {retryCount} for start operation")
                        System.Threading.Thread.Sleep(RetryDelayMs)
                    End If

                    StartRecording()

                    ' Verify the start was successful
                    System.Threading.Thread.Sleep(500) ' Brief delay to allow state to update
                    If GetRecordingState() Then
                        startSuccessful = True
                        HandleUserMessageLogging("GMRC", "StopAndStartRecording: Start recording successful")
                    Else
                        HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Start recording may not have completed (attempt {retryCount + 1})")
                    End If

                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Exception during start operation (attempt {retryCount + 1}): {ex.Message}")
                End Try

                retryCount += 1
            Loop

            If Not startSuccessful Then
                HandleUserMessageLogging("GMRC", $"StopAndStartRecording: FAILED to start recording after {MaxRetries} attempts", DisplayMsgBox)
                Exit Sub
            End If

            ' Final stopwatch reset after successful start
            Try
                If INCACommCheckStopWatch IsNot Nothing Then
                    INCACommCheckStopWatch.Reset()
                    INCACommCheckStopWatch.Start()
                    If Not String.IsNullOrEmpty(APICommErrorMsgDelayTime) Then
                        INCACommCheckWarningTime = Val(APICommErrorMsgDelayTime)
                        HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Final reset IncaCommCheckStopWatch to {INCACommCheckWarningTime}")
                    End If
                End If
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", $"StopAndStartRecording: WARNING - Final stopwatch reset failed: {ex.Message}")
            End Try

            HandleUserMessageLogging("GMRC", "StopAndStartRecording: Sequence change completed successfully")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"StopAndStartRecording: CRITICAL EXCEPTION - {ex.Message}", DisplayMsgBox)

            ' Emergency recovery attempt
            Try
                HandleUserMessageLogging("GMRC", "StopAndStartRecording: Attempting emergency recovery...")
                If GetRecordingState() Then
                    ' If we're still recording, try to continue normally
                    HandleUserMessageLogging("GMRC", "StopAndStartRecording: Recording state maintained during error")
                Else
                    ' If not recording, try to restart
                    HandleUserMessageLogging("GMRC", "StopAndStartRecording: Attempting to restart recording after error")
                    StartRecording()
                End If
            Catch recoveryEx As Exception
                HandleUserMessageLogging("GMRC", $"StopAndStartRecording: Emergency recovery also failed: {recoveryEx.Message}", DisplayMsgBox)
            End Try

        End Try
    End Sub

    ' This subroutine starts the measurement process in INCA.
    Private Sub StartMeasurement()

        'Invokes the StartMeasurement method on the GM_INCA_Comm server.

        Try

            OnVehicleScreen.Button4.Enabled = False
            If GmResidentClient IsNot Nothing AndAlso GmResidentClient.MyLogin IsNot Nothing Then
                GmResidentClient.MyLogin.Enabled = False
            Else
                HandleUserMessageLogging("Error", "GmResidentClient or MyLogin is not initialized.")
            End If

            If GetMeasurementStatus() = "False" Then

                If MyGmIncaComm.StartMeasurement() = True Then

                    Do While GetMeasurementStatus() = "False"
                        System.Threading.Thread.Sleep(100)
                    Loop

                    _MeasurementStarted = True

                    RecordPlayback.Record.Enabled = True
                    RecordPlayback.StopRecord.Enabled = True

                    RecordPlayback.PlayPauseButton.Enabled = False
                    RecordPlayback.StopButton.Enabled = False
                    RecordPlayback.Reset.Enabled = False
                    RecordPlayback.StepBack.Enabled = False
                    RecordPlayback.StepForward.Enabled = False
                    RecordPlayback.SelectFile.Enabled = False

                    StartDataCollection(GmResidentClient.DataCollectionRate)

                    If PATAC = False Then
                        OnVehicleScreen.Button5.Enabled = True
                    End If

                End If

            Else

                _MeasurementStarted = True

                RecordPlayback.Record.Enabled = True
                RecordPlayback.StopRecord.Enabled = True

                RecordPlayback.PlayPauseButton.Enabled = False
                RecordPlayback.StopButton.Enabled = False
                RecordPlayback.Reset.Enabled = False
                RecordPlayback.StepBack.Enabled = False
                RecordPlayback.StepForward.Enabled = False
                RecordPlayback.SelectFile.Enabled = False

                StartDataCollection(GmResidentClient.DataCollectionRate)

            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "INCA_InterfaceClass: StartMeasurement: " & ex.Message)
        End Try

    End Sub

End Class
