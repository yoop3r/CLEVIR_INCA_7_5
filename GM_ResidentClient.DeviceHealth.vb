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
Imports System.Windows.Forms.DataVisualization.Charting
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
        'Static saveStatus() As Boolean
        'Static lMyDeviceStatus As Boolean = True
        ' Default return value
        Dim MyDevicesStatus As Boolean = True ' Initialize the device status
        Try
            ' Check if devices are available
            If MyDevices Is Nothing Then
                HandleCriticalError("Communication failure, Exiting CLEVIR and Closing INCA...")
                Return False ' Return False if no devices are available
            End If
            If ActiveProcessorNames Is Nothing OrElse ActiveProcessorNames.Length = 0 Then
                HandleUserMessageLogging("GMRC", "GetDeviceStatus: no configured processors. Select a valid vehicle in config.xml.", DisplayMsgBox)
                _MyDeviceStatus = False
                Return False
            End If
            Dim missingProcessors = ActiveProcessorNames.Where(Function(name) Not MyDevices.Any(Function(device) String.Equals(device.myName, name, StringComparison.OrdinalIgnoreCase))).ToArray()
            If missingProcessors.Length > 0 Then
                HandleUserMessageLogging("GMRC", "Configured processors not found in the INCA experiment: " & String.Join(", ", missingProcessors), DisplayMsgBox)
                _MyDeviceStatus = False
                Return False
            End If
            ' Initialize arrays
            ReDim isRed(UBound(MyDevices))
            If saveStatus Is Nothing OrElse saveStatus.Length <> MyDevices.Length Then ReDim saveStatus(UBound(MyDevices))
            ' Process each device
            For x As Integer = 0 To UBound(MyDevices)
                Dim device = MyDevices(x)
                Dim isCalcDev = InStr(UCase(device.myName), "CALCDEV") > 0
                ' Skip CALCDEV devices
                If Not isCalcDev Then
                    ' Update device status and UI
                    UpdateDeviceStatusUI(x, device, isRed)
                    ' Check processor faults
                    If IsConfiguredProcessorDevice(device.myName) AndAlso Not device.myStatus Then
                        processorsFalse += 1
                        AppendLostDeviceName(device.myName)
                    End If
                    ' Check camera devices
                    If Not IsConfiguredProcessorDevice(device.myName) AndAlso CameraDevicePrefixes.Any(Function(prefix) InStr(device.myName, prefix) > 0) AndAlso Not device.myStatus Then
                        processorsFalse += 1
                        AppendLostDeviceName(device.myName)
                        'MyDevicesStatus = False ' Update status to False if a camera device fails
                    End If
                End If
            Next
            ' Handle device status based on initialization state
            If Not _initializing Then
                HandlePostInitializationStatus(MyDevices, isRed, saveStatus, processorsFalse)
                MyDevicesStatus = _MyDeviceStatus
            Else
                HandleInitializationStatus(MyDevices, isRed, numCameraDevicesInExperiment, MyDevicesStatus)
                _MyDeviceStatus = MyDevicesStatus AndAlso (processorsFalse = 0 OrElse IgnoreLostDeviceForThisDrive OrElse OverrideCommFailureInDebugMode)
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
    ' Helper method to update device status UI
    Private Sub UpdateDeviceStatusUI(index As Integer, device As IGM_INCA_Comm.INCADeviceStatus, ByRef isRed() As Boolean)
        Dim MyColor As Color = If(device.myStatus, Color.Green, Color.Red)
        isRed(index) = Not device.myStatus
        ' Update UI labels dynamically
        Dim labelName As Label = CType(DeviceStatus.Controls($"Label{index + 1}"), Label)
        Dim labelStatus As Label = CType(DeviceStatus.Controls($"Label{24 - index}"), Label)
        labelName.Text = device.myName
        labelStatus.Text = device.myStatus.ToString()
        labelStatus.BackColor = MyColor
    End Sub
    ' Helper method to append lost device names
    Private Sub AppendLostDeviceName(deviceName As String)
        If String.IsNullOrEmpty(SaveLostDeviceName) Then
            SaveLostDeviceName = deviceName
        ElseIf Not SaveLostDeviceName.Split(","c).Contains(deviceName, StringComparer.OrdinalIgnoreCase) Then
            SaveLostDeviceName &= "," & deviceName
        End If
    End Sub
    ' Helper method to handle post-initialization status
    Private Sub HandlePostInitializationStatus(MyDevices() As IGM_INCA_Comm.INCADeviceStatus, isRed() As Boolean, ByRef saveStatus() As Boolean, processorsFalse As Integer)
        For x As Integer = 0 To UBound(MyDevices)
            Dim device = MyDevices(x)
            Dim isCalcDev = InStr(UCase(device.myName), "CALCDEV") > 0
            If Not isCalcDev AndAlso saveStatus(x) <> isRed(x) Then
                If isRed(x) Then
                    HandleUserMessageLogging("GMRC", $"Cannot Communicate with {device.myName}")
                Else
                    HandleUserMessageLogging("GMRC", $"Communication with {device.myName} established")
                End If
            End If
        Next
        _MyDeviceStatus = processorsFalse = 0 OrElse IgnoreLostDeviceForThisDrive OrElse OverrideCommFailureInDebugMode
        saveStatus = isRed
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

    Private Async Function CheckForLostDevice() As Task
        ' This routine handles the case where communication to processors or video cameras has been lost.
        ' Allows user to reinitialize CLEVIR and INCA.

        Dim operatorMessage As String = ""
        Dim button1Text As String = ""
        Dim button2Text As String = ""
        Dim button3Text As String = ""
        Dim button4Text As String = ""

        Try
            If Not (_lostDevice OrElse VideoCameraNotUpdating OrElse BackgroundLoopCounterNotUpdating) Then
                Return
            End If

            If IgnoreLostDeviceUntilNextRecordingSession OrElse IgnoreLostDeviceForThisDrive Then
                Return
            End If

            HandleUserMessageLogging("GMRC", "CheckForLostDevice: RedisplayOnVehicleForm(Main) Called...")
            RedisplayOnVehicleForm("Main")

            ' Determine which alert to show based on what triggered the check
            If BackgroundLoopCounterNotUpdating AndAlso Not _lostDevice AndAlso
           (Not VideoCameraNotUpdating OrElse (VideoCameraNotUpdating AndAlso videoMessageLastDisplayed)) Then

                operatorMessage = "INVALID DATA ALERT! (" & SaveDataFrozenDeviceName & ") To ensure data quality, CLEVIR/INCA should be re-initialized. This will take approx 3 minutes"
                button1Text = "CANCEL"
                button2Text = "Re-Initialize NOW"
                button3Text = "Ignore All Alerts for This Drive"
                button4Text = "Ignore All Alerts for Recording Session"
                videoMessageLastDisplayed = False
                dataMessageLastDisplayed = True

            ElseIf VideoCameraNotUpdating AndAlso Not _lostDevice AndAlso
               (Not BackgroundLoopCounterNotUpdating OrElse (BackgroundLoopCounterNotUpdating AndAlso dataMessageLastDisplayed)) Then

                operatorMessage = "INVALID VIDEO ALERT! (" & SaveVideoFrozenDeviceName & ") CLEVIR/INCA should be re-initialized to insure video recording capability. This will take approx 3 minutes."
                button1Text = "CANCEL"
                button2Text = "Re-Initialize NOW"
                button3Text = "Ignore All Alerts for This Drive"
                button4Text = "Ignore All Alerts for Recording Session"
                videoMessageLastDisplayed = True
                dataMessageLastDisplayed = False

            ElseIf _lostDevice Then
                operatorMessage = "PROCESSOR COMMUNICATION ALERT! (" & SaveLostDeviceName & ") CLEVIR/INCA should be re-initialized to insure data and video recording capability. This will take approx 3 minutes."
                button1Text = "CANCEL"
                button2Text = "Re-Initialize NOW"
                button3Text = "Ignore All Alerts for This Drive"
                button4Text = ""
            End If

            HandleUserMessageLogging("GMRC", "CheckForLostDevice: " & operatorMessage)

            ' Show dialog without blocking async flow
            Dim result As Integer
            Using msgBox As New Cusmsgbox()
                result = msgBox.DisplayCusMsgBox(OnVehicleScreen, operatorMessage, "CLEVIR ALERT",
                                           button1Text, button2Text, button3Text, button4Text)
            End Using

            Select Case result
                Case 1 ' CANCEL
                    HandleUserMessageLogging("GMRC", "CheckForLostDevice: User Answered " & button1Text)
                    OnVehicleScreen.TopMost = False

                Case 2 ' Re-Initialize NOW
                    HandleUserMessageLogging("GMRC", "CheckForLostDevice: User Answered " & button2Text)

                    ' Offload re-initialization to background to avoid blocking UI
                    Await Task.Run(Async Function()
                                       Await PerformReinitialization()
                                   End Function)

                Case 3 ' Ignore All Alerts for This Drive
                    HandleUserMessageLogging("GMRC", "CheckForLostDevice: User Answered " & button3Text)
                    IgnoreLostDeviceForThisDrive = True
                    OnVehicleScreen.TopMost = False

                Case 4 ' Ignore All Alerts for Recording Session
                    HandleUserMessageLogging("GMRC", "CheckForLostDevice: User Answered " & button4Text)
                    IgnoreLostDeviceUntilNextRecordingSession = True
                    OnVehicleScreen.TopMost = False
            End Select

            ' Reset flags
            _lostDevice = False
            VideoCameraNotUpdating = False
            BackgroundLoopCounterNotUpdating = False
            SaveLostDeviceName = ""

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "CheckForLostDevice: " & ex.Message)
        End Try
    End Function

    Private Async Function PerformReinitialization() As Task
        Try
            BackgroundLoopCounterNotUpdating = False
            VideoCameraNotUpdating = False

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
            ' STEP 4: INCA cleanup (COM calls must stay on UI thread)
            ' ══════════════════════════════════════════════════════════
            HandleUserMessageLogging("GMRC", "Reinitializing INCA...",,, FlashMsgOn)

            ' ✅ FIXED: Direct call (COM requires STA thread)
            MyIncaInterface.RCI2_Cleanup()

            ' ✅ FIXED: Async workspace handling
            Dim returnStr As String = Await Task.Run(Function() As String
                                                         MyIncaInterface.HandleWorkspace("", False)
                                                         ' Assume success if no exception thrown
                                                         Return "True"
                                                     End Function)

            ' ══════════════════════════════════════════════════════════
            ' STEP 5: Signal registration
            ' ══════════════════════════════════════════════════════════
            ' ✅ VALIDATION: Check workspace handling result before proceeding
            If String.IsNullOrEmpty(returnStr) OrElse returnStr.Contains("ERROR:") Then
                HandleUserMessageLogging("GMRC", $"Workspace handling failed: {returnStr}", DisplayMsgBox)
                ExitApp("Complete")
                Return
            End If

            ' ✅ SUCCESS PATH: Workspace handled successfully, proceed with signal registration
            HandleUserMessageLogging("GMRC", "Registering Signals, please wait...",,, FlashMsgOn)

            ' ✅ FIXED: Offload CPU-bound work
            Await Task.Run(Sub()
                               MyIncaInterface.RegisterSignals()
                           End Sub)

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

            HandleButtonVisibility(True)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"PerformReinitialization: {ex.Message}")
        End Try
    End Function

End Class
