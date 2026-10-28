Option Strict Off
Option Explicit On

Imports System.Collections.Concurrent
Imports System.Diagnostics
Imports System.IO
Imports System.Net.NetworkInformation
Imports System.Speech.Synthesis
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml
Imports SharpPcap

Partial Public Class GmResidentClient
    ''' <summary>
    ''' Reads the device status from INCA or other hardware.
    ''' Returns False if device is not okay.
    ''' </summary>
    Private Async Function GetDeviceStatus() As Task(Of Boolean)
        Dim CameraDevicePrefixes As String() = {"FCM"}
        ' Initialize variables
        Dim MyDevices() As IGM_INCA_Comm.INCADeviceStatus = Await MyIncaInterface.GetAvailableDevicesAsync(True)
        Dim isRed() As Boolean
        Dim processorsFalse As Integer = 0
        Dim numCameraDevicesInExperiment As Integer = 0
        ' Default return value
        Dim MyDevicesStatus As Boolean = True ' Initialize the device status
        Try
            _deviceHealthKnown = False
            ' Check if devices are available
            If MyDevices Is Nothing Then
                If _initializing Then
                    HandleCriticalError("Communication failure, Exiting CLEVIR and Closing INCA...")
                    Return False
                End If

                ' At runtime one failed read must not end a drive: exit only if INCA stays unreachable.
                _deviceReadFailures += 1
                HandleUserMessageLogging("GMRC", $"GetDeviceStatus: INCA device list unavailable ({_deviceReadFailures}/{MaxConsecutiveDeviceReadFailures})")
                If _deviceReadFailures >= MaxConsecutiveDeviceReadFailures Then
                    HandleCriticalError("Communication failure, Exiting CLEVIR and Closing INCA...")
                    Return False
                End If
                Return _MyDeviceStatus
            End If
            If _deviceReadFailures > 0 Then
                HandleUserMessageLogging("GMRC", $"GetDeviceStatus: INCA device list available again after {_deviceReadFailures} failed read(s)")
                _deviceReadFailures = 0
            End If
            If ActiveProcessorNames Is Nothing OrElse ActiveProcessorNames.Length = 0 Then
                ReportDeviceConfigProblemOnce("GetDeviceStatus: no configured processors. Select a valid vehicle in config.xml.")
                RecordDeviceHealth(False, {"no processors configured"}, True)
                _MyDeviceStatus = False
                Return False
            End If
            Dim missingProcessors = ActiveProcessorNames.Where(Function(name) Not MyDevices.Any(Function(device) String.Equals(device.myName, name, StringComparison.OrdinalIgnoreCase))).ToArray()
            If missingProcessors.Length > 0 Then
                ReportDeviceConfigProblemOnce("Configured processors not found in the INCA experiment: " & String.Join(", ", missingProcessors))
                RecordDeviceHealth(False, missingProcessors, True)
                _MyDeviceStatus = False
                Return False
            End If
            _lastDeviceConfigProblem = ""
            Dim lostNames As New List(Of String)
            Dim anyProcessorLost As Boolean = False
            ' Initialize arrays
            ReDim isRed(UBound(MyDevices))
            ' Process each device
            For x As Integer = 0 To UBound(MyDevices)
                Dim device = MyDevices(x)
                ' Debug fault injection (no-op in release); not during (re)initialization so it can complete.
                If Not _initializing AndAlso IsLostDeviceInjected(device.myName) Then device.myStatus = False
                Dim isCalcDev = InStr(UCase(device.myName), "CALCDEV") > 0
                ' Skip CALCDEV devices
                If Not isCalcDev Then
                    ' Update device status and UI
                    UpdateDeviceStatusUI(x, device, isRed)
                    ' Check processor faults
                    If IsConfiguredProcessorDevice(device.myName) AndAlso Not device.myStatus Then
                        processorsFalse += 1
                        lostNames.Add(device.myName)
                        anyProcessorLost = True
                    End If
                    ' Check camera devices
                    If Not IsConfiguredProcessorDevice(device.myName) AndAlso CameraDevicePrefixes.Any(Function(prefix) InStr(device.myName, prefix) > 0) AndAlso Not device.myStatus Then
                        processorsFalse += 1
                        lostNames.Add(device.myName)
                    End If
                End If
            Next
            RecordDeviceHealth(processorsFalse = 0, lostNames, anyProcessorLost)
            ' Handle device status based on initialization state
            If Not _initializing Then
                HandlePostInitializationStatus(MyDevices, isRed, processorsFalse)
                MyDevicesStatus = _MyDeviceStatus
            Else
                HandleInitializationStatus(MyDevices, isRed, numCameraDevicesInExperiment, MyDevicesStatus)
                _MyDeviceStatus = MyDevicesStatus AndAlso (processorsFalse = 0 OrElse IgnoreLostDeviceForThisDrive)
            End If
            ' Return the updated device status
            Return MyDevicesStatus
        Catch ex As Exception
            ' Log the exception
            HandleUserMessageLogging("GMRC", "GetDeviceStatus: " & ex.Message,,)
            ' Return False in case of an exception
            Return False
        End Try
    End Function

    ' Helper method to handle critical errors
    Private Sub HandleCriticalError(message As String)
        OnVehicleScreen.TopMost = True
        HandleUserMessageLogging("GMRC", message, DisplayMsgBox,)
        ExitApp("Complete")
    End Sub
    ' Polled every 5 s: show the message box once per distinct problem, not on every poll.
    Private Sub ReportDeviceConfigProblemOnce(message As String)
        If String.Equals(message, _lastDeviceConfigProblem, StringComparison.Ordinal) Then Return
        _lastDeviceConfigProblem = message
        HandleUserMessageLogging("GMRC", message, DisplayMsgBox)
    End Sub
    ' Helper method to update device status UI
    Private Sub UpdateDeviceStatusUI(index As Integer, device As IGM_INCA_Comm.INCADeviceStatus, ByRef isRed() As Boolean)
        Dim MyColor As Color = If(device.myStatus, Color.Green, Color.Red)
        isRed(index) = Not device.myStatus
        ' DeviceStatus has 12 name/status label pairs: Label1..12 = names, Label24..13 = statuses.
        If index >= DeviceStatusLabelPairs Then
            If Not _deviceStatusOverflowLogged Then
                _deviceStatusOverflowLogged = True
                HandleUserMessageLogging("GMRC", $"UpdateDeviceStatusUI: more than {DeviceStatusLabelPairs} devices; '{device.myName}' and later are monitored but not shown on the Device Status form")
            End If
            Return
        End If
        Dim labelName As Label = TryCast(DeviceStatus.Controls($"Label{index + 1}"), Label)
        Dim labelStatus As Label = TryCast(DeviceStatus.Controls($"Label{2 * DeviceStatusLabelPairs - index}"), Label)
        If labelName Is Nothing OrElse labelStatus Is Nothing Then Return
        labelName.Text = device.myName
        labelStatus.Text = device.myStatus.ToString()
        labelStatus.BackColor = MyColor
    End Sub
    ' Helper method to handle post-initialization status
    Private Sub HandlePostInitializationStatus(MyDevices() As IGM_INCA_Comm.INCADeviceStatus, isRed() As Boolean, processorsFalse As Integer)
        ' Keyed by device name so a reordered or resized device list can't log the wrong device.
        For x As Integer = 0 To UBound(MyDevices)
            Dim device = MyDevices(x)
            Dim isCalcDev = InStr(UCase(device.myName), "CALCDEV") > 0
            If isCalcDev OrElse String.IsNullOrEmpty(device.myName) Then Continue For
            Dim wasRed As Boolean = False
            _lastDeviceFault.TryGetValue(device.myName, wasRed)
            If wasRed <> isRed(x) Then
                If isRed(x) Then
                    HandleUserMessageLogging("GMRC", $"Cannot Communicate with {device.myName}")
                Else
                    HandleUserMessageLogging("GMRC", $"Communication with {device.myName} established")
                End If
            End If
            _lastDeviceFault(device.myName) = isRed(x)
        Next
        _MyDeviceStatus = processorsFalse = 0 OrElse IgnoreLostDeviceForThisDrive
    End Sub
    ' Helper method to handle initialization status
    Private Sub HandleInitializationStatus(MyDevices() As IGM_INCA_Comm.INCADeviceStatus, isRed() As Boolean, ByRef numCameraDevicesInExperiment As Integer, ByRef MyDevicesStatus As Boolean)
        For x As Integer = 0 To UBound(MyDevices)
            Dim device = MyDevices(x)
            Dim isCalcDev = InStr(UCase(device.myName), "CALCDEV") > 0
            If Not isCalcDev AndAlso isRed(x) Then
                HandleUserMessageLogging("GMRC", $"Cannot Communicate with {device.myName} at Initialization...")
                If Not Debugger.IsAttached Then
                    Dim dlgResult As DialogResult
                    Using topmostOwner As New Form() With {
                        .TopMost = True,
                        .Size = New System.Drawing.Size(1, 1),
                        .StartPosition = FormStartPosition.CenterScreen,
                        .ShowInTaskbar = False,
                        .FormBorderStyle = FormBorderStyle.None,
                        .Opacity = 0
                    }
                        topmostOwner.Show()
                        dlgResult = MessageBox.Show(
                            topmostOwner,
                            $"Cannot Communicate with {device.myName}, Continue Initialization?",
                            "Device Communication Warning",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning)
                    End Using
                    If dlgResult = DialogResult.No Then
                        HandleUserMessageLogging("GMRC", "User chose not to continue with initialization.")
                        MyDevicesStatus = False
                        ExitApp("Complete")
                        Return
                    Else
                        HandleUserMessageLogging("GMRC", "User chose to continue with initialization.")
                    End If
                End If
            End If
            ' Count camera devices
            If InStr(UCase(device.myDeviceType), "VIDEO") > 0 Then
                numCameraDevicesInExperiment += 1
            End If
        Next
    End Sub

    Private Async Function PerformReinitialization() As Task
        Try
            CloseDeviceAlert()
            BackgroundLoopCounterNotUpdating = False
            VideoCameraNotUpdating = False
            ClearVideoLoss("Re-Initialize")

            ' ══════════════════════════════════════════════════════════
            ' STEP 1: Stop Recording (UI thread safe)
            ' ══════════════════════════════════════════════════════════
            If MyIncaInterface.GetRecordingState() Then
                ' ✅ FIXED: Direct call on UI thread (no Invoke needed)
                MyIncaInterface.StartStopRecord(OnVehicleScreen.Button14)
            ElseIf MyIncaInterface.GetMeasurementStatus() = "True" Then
                Await MyIncaInterface.StartStopMeasurement(OnVehicleScreen.Button6)
            End If

            ' ══════════════════════════════════════════════════════════
            ' STEP 2: Update UI (already on UI thread)
            ' ══════════════════════════════════════════════════════════
            HandleButtonVisibility(False)

            _incaInitStarted = True
            _initializing = True

            ' ══════════════════════════════════════════════════════════
            ' STEP 3: Camera check (non-blocking background work)
            ' ══════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", "Checking for Cameras...",,, FlashMsgOn)

            ' ✅ FIXED: Offload only the network I/O (not UI updates)
            Await CheckForCameras(initialWaitTime:=CameraWaitTime, camerapingtime:=CameraPingTime, showToasts:=True)

            ' ══════════════════════════════════════════════════════════
            ' STEP 4: Restart INCA (COM calls must stay on UI thread)
            ' ══════════════════════════════════════════════════════════
            ' A full restart is required: after a device is lost or reconnected INCA's hardware layer (TGTSVR) can be
            ' left hung, and re-opening the experiment in the same INCA process then blocks in InitializeHardware.
            HandleUserMessageLogging("GMRC", "Re-Initialize: Restarting INCA...",,, FlashMsgOn)

            CheckForExperiment = False
            MyIncaInterface.CloseINCA() ' RCI2 cleanup, CloseTool, waits up to 60 s for INCA and TGTSVR to exit
            KillLeftoverIncaProcesses()
            Await Task.Delay(IncaRestartSettleMs)

            Dim errorMsg As String = ""
            Dim initStatus As IGM_INCA_Comm.INIT_STATUS = MyIncaInterface.InitINCA(INCADatabase, INCAWorkspace, INCAExperiment, False, errorMsg, False)

            ' ══════════════════════════════════════════════════════════
            ' STEP 5: Signal registration
            ' ══════════════════════════════════════════════════════════
            If initStatus = IGM_INCA_Comm.INIT_STATUS.INIT_UNSUCCESSFUL Then
                HandleUserMessageLogging("GMRC", $"Re-initialization failed - INCA initialization returned: {errorMsg}. CLEVIR will exit; please restart CLEVIR and INCA.", DisplayMsgBox)
                ExitApp("Complete")
                Return
            End If

            HandleUserMessageLogging("GMRC", "Registering Signals, please wait...",,, FlashMsgOn)

            ' UI thread: RegisterSignals drives OnVehicleScreen (a per-thread default instance) and INCA COM.
            MyIncaInterface.RegisterSignals()

            HandleUserMessageLogging("GMRC", "Registering Signals complete.",,, FlashMsgOn)

            ' ══════════════════════════════════════════════════════════
            ' STEP 6: Final cleanup (UI thread)
            ' ══════════════════════════════════════════════════════════
            UserStatusInfo.Hide()

            ' ✅ Bring OnVehicleScreen to front (consistent with FinalizeUI pattern)
            OnVehicleScreen.BringToFront()
            OnVehicleScreen.Activate()
            OnVehicleScreen.TopMost = False

            Await GetDeviceStatus()

            _incaInitStarted = False
            _initializing = False
            If Not PlaybackMode Then CheckForExperiment = True

            HandleButtonVisibility(True)
            HandleUserMessageLogging("GMRC", "Re-Initialize: INCA restarted and signals registered")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"PerformReinitialization: {ex.Message}. If CLEVIR does not recover, exit using 'Exit CLEVIR And Close INCA', then restart CLEVIR.", DisplayMsgBox)
            _incaInitStarted = False
            _initializing = False
            If Not PlaybackMode Then CheckForExperiment = True
            HandleButtonVisibility(True)
        End Try
    End Function

    ' Matches ShutdownAndRestartInca (Restart INCA menu): lets the ETAS hardware settle before INCA is relaunched.
    Private Const IncaRestartSettleMs As Integer = 18000

    ''' <summary>INCA or TGTSVR still running after the graceful close (e.g. Target Server hung on a lost device).</summary>
    Private Sub KillLeftoverIncaProcesses()
        For Each processName As String In {"INCA", "TGTSVR"}
            For Each proc As Process In Process.GetProcessesByName(processName)
                Using proc
                    HandleUserMessageLogging("GMRC", $"Re-Initialize: {processName} still running after INCA close - terminating")
                    KillProcessSafely(proc, processName)
                End Using
            Next
        Next
    End Sub

End Class
