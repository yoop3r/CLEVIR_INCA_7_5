Option Strict Off
Option Explicit On
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms

Public Class OnVehicleScreen
    ' Add constants at the top of the class
    Private Const MIN_FORM_WIDTH As Integer = 800
    Private Const MIN_FORM_HEIGHT As Integer = 605
    Private Const RIGHT_MARGIN As Integer = 10
    Private Const BUTTON_SPACING As Integer = 20
    Private Const STATUS_LABEL_MARGIN As Integer = 445
    Private Const MAX_LISTBOX_HEIGHT As Integer = 350
    Private Const MAX_LISTBOX_WIDTH As Integer = 300

    ' LiDAR health monitoring thresholds
    Private Const LIDAR_WARNING_LOSS_THRESHOLD As Double = 5.0  ' % packet loss for warning
    Private Const LIDAR_CRITICAL_LOSS_THRESHOLD As Double = 20.0  ' % packet loss for critical
    Private Const LIDAR_TIMEOUT_SECONDS As Integer = 5  ' Seconds without packets = device stopped
    Private Const LIDAR_STARTUP_GRACE_SECONDS As Integer = 8  ' Grace period after capture start before "no data" is treated as Critical

    ' Boolean flag to track the state of the action
    'Private isListBoxVisible As Boolean = False

    'This is the primary On Vehicle Display.

    'During Initialization, this form is set up as TopMost = True, which means that
    'it is always displayed in the front of all other windows.  This window cannot be resized or moved.
    'The window is positioned at the top left corner of the On Vehicle PC Monitor.  The percentage of
    'screen area that it occupies depends on the screen resolution set.

    'When running CLEVIR on a user laptop other than the On Vehicle PC, the user may emulate the in vehicle PC display, which
    'will display this form in the same manner as is typical for in vehicle use, or the user may indidate that they do not
    'wish to emulate the on vehicle display, which will display the GmResidentClient form, which is a movable window with 
    'a drop down menu and has the OnVehicleScreen superimposed onto it.

    ' Declare Timer

    ''' <summary>
    ''' Enum to represent LiDAR system health status
    ''' </summary>
    Private Enum LidarHealthStatus
        Healthy = 0
        Warning = 1
        Critical = 2
    End Enum

    ' Timer Tick event handler
    Private Sub Timer_LidarStats_Tick(sender As Object, e As EventArgs) Handles Timer_LidarStats.Tick
        Try
            ' Only update if LiDAR is actively capturing
            If LidarCaptureStarted AndAlso LidarDevices IsNot Nothing Then
                ' Calculate health status
                Dim healthStatus As LidarHealthStatus = CalculateLidarHealth()

                ' Update UI with color-coded status
                UpdateLidarStatusUI(isCapturing:=True, deviceCount:=LidarDevices.Count, healthStatus:=healthStatus)
            End If
        Catch ex As Exception
            HandleUserMessageLogging("OVS", $"Timer_LidarStats_Tick: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Calculates the overall health status of all LiDAR devices
    ''' </summary>
    Private Function CalculateLidarHealth() As LidarHealthStatus
        Try
            If LidarDevices Is Nothing OrElse LidarDevices.Count = 0 Then
                Return LidarHealthStatus.Healthy
            End If

            Dim stoppedDevices As Integer = 0
            Dim warningDevices As Integer = 0

            For Each lidar In LidarDevices
                ' Check if device has NEVER received packets (comms never established)
                If Not lidar.LastPacketTimestamp.HasValue AndAlso lidar.PacketCount = 0 Then
                    ' Suppress false-critical during startup grace period
                    Dim inGrace As Boolean = lidar.CaptureStartedAt.HasValue AndAlso
                        (DateTime.Now - lidar.CaptureStartedAt.Value).TotalSeconds <= LIDAR_STARTUP_GRACE_SECONDS
                    If Not inGrace Then
                        stoppedDevices += 1
                    End If
                    Continue For
                End If

                ' Check if device stopped (no packets in last interval)
                If lidar.LastPacketTimestamp.HasValue AndAlso
                   (DateTime.Now - lidar.LastPacketTimestamp.Value).TotalSeconds > LIDAR_TIMEOUT_SECONDS Then
                    stoppedDevices += 1
                    Continue For
                End If

                ' Check packet loss percentage
                Dim totalPackets As Long = lidar.PacketCount + lidar.DroppedPackets
                If totalPackets > 100 Then ' Only check after significant packet count
                    Dim lossPercent As Double = (CDbl(lidar.DroppedPackets) / CDbl(totalPackets)) * 100.0

                    If lossPercent >= LIDAR_CRITICAL_LOSS_THRESHOLD Then
                        stoppedDevices += 1 ' Treat >20% loss as critical
                    ElseIf lossPercent >= LIDAR_WARNING_LOSS_THRESHOLD Then
                        warningDevices += 1
                    End If
                End If
            Next

            ' Determine overall status
            If stoppedDevices > 0 Then
                Return LidarHealthStatus.Critical
            ElseIf warningDevices > 0 Then
                Return LidarHealthStatus.Warning
            Else
                Return LidarHealthStatus.Healthy
            End If

        Catch ex As Exception
            HandleUserMessageLogging("OVS", $"CalculateLidarHealth: {ex.Message}")
            Return LidarHealthStatus.Healthy
        End Try
    End Function

    Private Async Sub Button6_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button6.Click
        Try
            SetButtonsEnabled(False)
            Cursor = Cursors.WaitCursor

            GmResidentClient.StopTestProcess = True

            ' ✅ FIXED: Direct await - no Task.Run
            Await MyIncaInterface.StartStopMeasurement(sender)

        Catch ex As Exception
            HandleUserMessageLogging("Error", $"Start/Stop Measurement failed: {ex.Message}")
        Finally
            SetButtonsEnabled(True)
            Cursor = Cursors.Arrow
        End Try
    End Sub

    Private Sub OnVehicleScreen_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles Me.FormClosing
        ' Check if the recording state is active
        If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.GetRecordingState Then
            ' Prevent the form from closing
            e.Cancel = True
            ' Notify the user
            MessageBox.Show("Recording is active. Please stop the recording before exiting.", "Recording in Progress", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Else
            Button1_Click(sender, e)
            e.Cancel = True
        End If
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button2.Click

        'This is the CONFIG button
        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "CONFIG Button Pressed...")
        HandleUserMessageLogging("GMRC", " ")

        GmResidentClient.StopTestProcess = True

        GmResidentClient.Visible = True

        If OperatingMode <> OperatingModes.ResOnVpc Then

            Top = GmResidentClient.Top + 60
            Left = GmResidentClient.Left
            Activate()
            BringToFront()

        End If

    End Sub

    Private Sub Button1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button1.Click
        ' This is the EXIT button on the OnVehicleScreen form
        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "EXIT Button Pressed...")
        HandleUserMessageLogging("GMRC", " ")
        ExitPressed = True

        ' Perform any additional cleanup
        If INCACommCheckStopWatch IsNot Nothing Then
            INCACommCheckStopWatch = Nothing
        End If
        If EnableDIDPull = True Then
            Button6.Enabled = False ' Start Measurement
            Button14.Enabled = False ' Start Record
            HandleUserMessageLogging("GMRC", "Preparing to read DID Information, please wait...",,, FlashMsgOn)
            EnableEndZipFileCheck = StartVehicleSpy()
            If EnableEndZipFileCheck = True Then
                HandleUserMessageLogging("GMRC", "Sending " & FunctionBlockString & " command...")
                If SendVSpyCommand(FunctionBlockString) = True Then
                    Threading.Thread.Sleep(1000)
                    HandleUserMessageLogging("GMRC", "Function Block start command sent successfully. Please wait while DID information is collected...",,, FlashMsgOn)
                    Exit Sub
                Else
                    HandleUserMessageLogging("GMRC", "Function Block start command failed. No DID Information will be collected...",,, FlashMsgOn)
                    EnableEndZipFileCheck = False
                    Threading.Thread.Sleep(2000)
                End If
            End If
        End If

        ' Exit the application
        If UCase(CLEVIRFlavor) = "DEVELOPMENT" Then
            GmResidentClient.ExitApp()
        Else
            ShutdownWindows = True
            GmResidentClient.ExitApp("Complete")
        End If
    End Sub


    Public Sub PictureBox1_Click(ByVal sender As Object, ByVal e As EventArgs) Handles PictureBox1.Click
        MicrophoneClick(sender)
        ' Convert the click point to ListBox coordinates if necessary.
        HideListBox4IfClickedOutside()
    End Sub

    Public Sub Button23_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button23.Click
        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "VOICE Button Pressed...")
        HandleUserMessageLogging("GMRC", " ")

        GmResidentClient.StopTestProcess = True

        ' ✅ CORRECTED: Toggle the button state based on BackColor
        Dim button As Button = DirectCast(sender, Button)

        If button.BackColor = Color.LightGreen Then
            ' Currently enabled → Disable voice commands
            ActivateDeactivateVoiceCommands(sender, "disabled")
        Else
            ' Currently disabled → Enable voice commands
            ActivateDeactivateVoiceCommands(sender, "enabled")
        End If
    End Sub

    ''' <summary>
    ''' Handles right-click on VOICE button to show available voice commands
    ''' </summary>
    Private Sub Button23_MouseDown(sender As Object, e As MouseEventArgs) Handles Button23.MouseDown
        ' Only respond to right-click
        If e.Button = MouseButtons.Right Then
            HandleUserMessageLogging("GMRC", "VOICE Button Right-Clicked - Showing Voice Commands...")

            ' Access the singleton instance
            Dim dataDictionary = DataDictionarySingleton.GetInstance()

            If GroupBox6.Visible Then
                ' Hide the list box if it is currently visible
                GroupBox6.Visible = False
                ListBox3.Visible = False
            Else
                ' Populate ListBox3 if it is not already populated
                If ListBox3.Items.Count = 0 Then
                    For Each command As String In dataDictionary.Commands.Keys
                        ListBox3.Items.Add(command)
                    Next
                End If

                ' Dynamic Sizing Logic for ListBox3
                Dim itemHeight As Integer = ListBox3.ItemHeight
                Dim totalItems As Integer = ListBox3.Items.Count + 1
                Dim maxHeight As Integer = MAX_LISTBOX_HEIGHT
                Dim calculatedHeight As Integer = Math.Min(itemHeight * totalItems, maxHeight)
                ListBox3.Height = calculatedHeight

                ' Adjust the width to fit the longest item
                Dim maxWidth As Integer = MAX_LISTBOX_WIDTH
                Dim longestItemWidth As Integer = ListBox3.Items.Cast(Of String)() _
                    .Max(Function(cmd) TextRenderer.MeasureText(cmd, ListBox3.Font).Width)
                ListBox3.Width = Math.Min(longestItemWidth + SystemInformation.VerticalScrollBarWidth, maxWidth)

                ' Position the GroupBox near the button (instead of centering)
                ' This provides better visual feedback that it's related to Button23
                GroupBox6.Left = Button23.Left + Button23.Width - GroupBox6.Width
                GroupBox6.Top = Button23.Top + Button23.Height + 5 ' 5px gap below button

                ' Show and bring to front
                GroupBox6.Visible = True
                ListBox3.Visible = True
                GroupBox6.BringToFront()
                ListBox3.Focus()
            End If
        End If
    End Sub

    Private Sub Button4_Click(ByVal sender As System.Object, ByVal e As EventArgs) Handles Button4.Click
        ' This is the LOGIN button on OnVehicleScreen

        ' Check if recording is active - if so, prevent login
        If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.GetRecordingState Then
            MessageBox.Show("Cannot access login while recording is active. Please stop the recording first.", "Recording in Progress", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "LOGIN Button Pressed...")
        HandleUserMessageLogging("GMRC", " ")

        OnLoginScreen = True
        GmResidentClient.StopTestProcess = True
        InSession = False

        ' ✅ REMOVED: These controls no longer exist on LoginForm
        ' LoginForm.GroupBox1.Visible = False
        ' LoginForm.Button43.Visible = False
        ' LoginForm.Button1.Enabled = True
        ' LoginForm.Button1.Text = "Enter Application - Logged in as " & SaveLoginID
        ' LoginForm.ListBox1 manipulation

        ' ✅ NEW: Ensure SaveLoginID has a default value
        If String.IsNullOrWhiteSpace(SaveLoginID) Then
            SaveLoginID = "Demo"
        End If

        ' Close SelectDisplays if visible
        If SelectDisplays.Visible = True Then
            SelectDisplays.Close()
        End If

        ' ✅ FIXED: Show LoginForm as modal dialog and handle result
        Try
            ' Center on parent and show as dialog
            LoginForm.StartPosition = FormStartPosition.CenterParent
            Dim result As DialogResult = LoginForm.ShowDialog(Me)

            ' Handle user selection
            Select Case result
                Case DialogResult.OK
                    ' User logged in successfully
                    HandleUserMessageLogging("GMRC", $"User logged in as '{SaveLoginID}' from OnVehicleScreen")

                    ' Update status label
                    If Not String.IsNullOrWhiteSpace(SaveLoginID) Then
                        Me.Label5.Text = "Logged in as " & SaveLoginID
                    End If

                Case DialogResult.Retry
                    ' User clicked "Select Different Workspace/Experiment" button
                    HandleUserMessageLogging("GMRC", "User requested workspace change from OnVehicleScreen")
                    ' Show SoftwareVersionSelect form (this will be handled by calling code)
                    ' For now, just log the request

                Case Else
                    ' User closed form without selecting (X button triggers Exit handler)
                    HandleUserMessageLogging("GMRC", "Login form closed without selection")
            End Select

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Button4_Click error: {ex.Message}")
        Finally
            OnLoginScreen = False
        End Try
    End Sub

    Public Sub Button14_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button14.Click
        Try
            Button1.Enabled = False
            Button14.Enabled = False
            Button6.Enabled = False
            Cursor = Cursors.WaitCursor

            GmResidentClient.StopTestProcess = True

            ' ============================================================
            ' INCA Recording Start/Stop (handles LiDAR internally)
            ' ============================================================
            MyIncaInterface.StartStopRecord(sender)

            ' ============================================================
            ' Update LiDAR Status UI and Start/Stop Timer
            ' ============================================================
            Dim isRecording As Boolean = MyIncaInterface.GetRecordingState()

            If isRecording AndAlso LidarCaptureStarted Then
                ' Recording started - update UI and start timer
                UpdateLidarStatusUI(isCapturing:=True, deviceCount:=LidarDevices.Count)
                Timer_LidarStats.Start()
            Else
                ' Recording stopped - update UI and stop timer
                Timer_LidarStats.Stop()
                UpdateLidarStatusUI(isCapturing:=False)
            End If

        Catch ex As Exception
            HandleUserMessageLogging("OVS", $"Button14_Click: {ex.Message}", DisplayMsgBox)

        Finally
            Button14.Enabled = True
            Button6.Enabled = True
            Button1.Enabled = True
            Cursor = Cursors.Arrow
            Me.Refresh() ' Force UI update to reflect new button states immediately
        End Try
    End Sub

    Private Sub Button3_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button3.Click

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "DISPLAYS Button Pressed...")
        HandleUserMessageLogging("GMRC", " ")

        GmResidentClient.StopTestProcess = True
        SelectDisplays.Show()
        SelectDisplays.BringToFront()
        SelectDisplays.Refresh() ' Force UI update to reflect new button states immediately
    End Sub

    Private Sub Label2_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Label2.Click
        'GmResidentClient.CancelCameraSearch = True
        ' Convert the click point to ListBox coordinates if necessary.
        HideListBox4IfClickedOutside()
    End Sub

    Private Sub OnVehicleScreen_Resize(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Resize
        ' Only adjust layout if form is maximized or being resized
        If Me.WindowState = FormWindowState.Maximized Then
            'OrElse Me.WindowState = FormWindowState.Normal Then
            AdjustControlLayout()
        End If
    End Sub

    Private Sub AdjustControlLayout()
        ' Calculate available space
        Dim formWidth As Integer = Me.ClientSize.Width
        Dim formHeight As Integer = Me.ClientSize.Height

        ' Adjust main status label to span the width
        If Label5 IsNot Nothing Then
            Label5.Width = formWidth - STATUS_LABEL_MARGIN ' Leave room for right-side controls
        End If

        ' Reposition right-aligned buttons
        If Button1 IsNot Nothing Then ' EXIT button
            Button1.Left = formWidth - Button1.Width - RIGHT_MARGIN
        End If

        If Button23 IsNot Nothing Then ' Voice command button
            Button23.Left = formWidth - Button23.Width - Button1.Width - BUTTON_SPACING
        End If

        ' Adjust the main content area
        If GroupBox1 IsNot Nothing Then
            GroupBox1.Width = formWidth - 20
            GroupBox1.Height = formHeight - GroupBox1.Top - RIGHT_MARGIN
        End If

        ' Adjust ListBox1 if visible
        If ListBox1 IsNot Nothing AndAlso ListBox1.Visible Then
            ListBox1.Width = formWidth - 120
            ListBox1.Height = formHeight - ListBox1.Top - 10
        End If

        ' Center popup groupboxes when they're shown
        'CenterPopupControls()
    End Sub

    Private Sub CenterPopupControls()
        ' Center the popup GroupBoxes when they're visible
        If GroupBox3 IsNot Nothing AndAlso GroupBox3.Visible Then
            GroupBox3.Left = (Me.ClientSize.Width - GroupBox3.Width) \ 2
            GroupBox3.Top = (Me.ClientSize.Height - GroupBox3.Height) \ 2
        End If

        If GroupBox5 IsNot Nothing AndAlso GroupBox5.Visible Then
            GroupBox5.Left = (Me.ClientSize.Width - GroupBox5.Width) \ 2
            GroupBox5.Top = (Me.ClientSize.Height - GroupBox5.Height) \ 2
        End If

        If GroupBox6 IsNot Nothing AndAlso GroupBox6.Visible Then
            GroupBox6.Left = (Me.ClientSize.Width - GroupBox6.Width) \ 2
            GroupBox6.Top = (Me.ClientSize.Height - GroupBox6.Height) \ 2
        End If
    End Sub

    Private Sub OnVehicleScreen_Activated(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Activated

    End Sub

    Private Sub OnVehicleScreen_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Click, GroupBox5.MouseCaptureChanged, ListBox4.Click
        GmResidentClient.StopTestProcess = True
        ' Convert the click point to ListBox coordinates if necessary.
        HideListBox4IfClickedOutside()
    End Sub

    Private Sub OnVehicleScreen_Shown(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Shown

    End Sub

    Private Sub OnVehicleScreen_Load(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Load
        ' ================================================================
        ' 1. Initialize annotation buttons and custom UI controls
        ' ================================================================
        Label2.Visible = False
        GmResidentClient?.InitializeAndSetupMainTabControl()  ' Creates event annotation buttons
        SetupCustomListBox()                                   ' Enables custom drawing for subcategory dropdown
        SetupControlAnchoring()                                ' Makes form responsive to screen size changes

        ' ================================================================
        ' 2. Disable features not supported in DATALOGGING mode
        ' ================================================================
        If InStr(UCase(CLEVIRFlavor), "DATALOGGING") > 0 Then
            Button3.Enabled = False ' Disable Displays button
            Button4.Enabled = False ' Disable login button
        End If

        ' ================================================================
        ' 3. Synchronize recording duration from UI to backend
        ' ================================================================
        ' CRITICAL: This ensures GM_INCA_Comm knows the recording duration
        ' BEFORE the first recording starts (ComboBox1_SelectedValueChanged
        ' only fires when user manually changes the value, NOT on form load)
        Try
            If Not String.IsNullOrWhiteSpace(ComboBox1.Text) Then
                Dim mins As Integer = -1
                Dim textVal As String = ComboBox1.Text.Trim().ToUpperInvariant()
                If textVal = "ALL" Then
                    mins = -1
                ElseIf Not Integer.TryParse(ComboBox1.Text, mins) Then
                    mins = 1
                    ComboBox1.Text = "1"
                End If

                RecordFileDurationMinutes = mins

                If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                    MyIncaInterface.MyGmIncaComm.SetRecordingFileDurationMinutes(mins)
                End If
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "OnVehicleScreen_Load: Failed to init recording duration: " & ex.Message)
        End Try

        ' ================================================================
        ' 4. Initialize LiDAR status monitoring UI (if configured)
        ' ================================================================
        InitializeLidarStatusPanel()

        ' ================================================================
        ' 5. Initialize Voice Commands Button State
        ' ================================================================
        Try
            ' Set initial state to DISABLED (gray)
            Button23.BackColor = SystemColors.Control
            Button23.ForeColor = Color.Black
            Button23.Text = "VOICE"

            Dim tooltip As New ToolTip()
            tooltip.SetToolTip(Button23, "Voice Commands DISABLED - Click to enable | Right-click for command list")

            ' ✅ Hide Label6 since it's no longer used
            Label6.Visible = False

            ' ✅ OPTIONAL: Auto-initialize voice recognition in background
            If VoiceRecognitionInstance Is Nothing Then
                Try
                    VoiceRecognitionInstance = New VoiceRecognitionClass()
                    VoiceRecognitionInstance.InitVoice()
                    HandleUserMessageLogging("GMRC", "Voice Recognition initialized (inactive)")
                Catch ex As Exception
                    HandleUserMessageLogging("GMRC", $"Voice recognition initialization failed: {ex.Message}")
                    Button23.Enabled = False
                End Try
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Failed to initialize voice button: {ex.Message}")
        End Try
    End Sub
    ''' <summary>
    ''' Initializes the LiDAR status monitoring panel based on config.xml settings.
    ''' </summary>
    Private Sub InitializeLidarStatusPanel()
        Try
            ' Initialize the timer if it hasn't been created yet
            If Timer_LidarStats Is Nothing Then
                Timer_LidarStats = New Timer With {
                .Interval = 1000 ' Update every 1 second (adjust as needed)
            }
                AddHandler Timer_LidarStats.Tick, AddressOf Timer_LidarStats_Tick
            End If

            ' Always show the panel if LiDAR devices are configured (regardless of capture state)
            If LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 Then
                GroupBox_LidarStatus.Visible = True

                ' Set initial status based on whether capture is enabled
                If LidarCaptureEnabled Then
                    ' Capture enabled but not started yet - show as "Stopped" (gray)
                    UpdateLidarStatusUI(isCapturing:=False, deviceCount:=LidarDevices.Count)
                Else
                    ' Capture disabled - show as "Disabled" with appropriate styling
                    Label_LidarStatus.Text = $"● LiDAR ({LidarDevices.Count}) - Disabled"
                    Label_LidarStatus.BackColor = Color.DarkGray
                    Label_LidarStatus.ForeColor = Color.White
                    GroupBox_LidarStatus.ForeColor = Color.DarkGray

                    ' Disable click interaction when capture is disabled
                    Label_LidarStatus.Cursor = Cursors.Default

                    ' Update tooltip to reflect disabled state
                    Dim tooltip As New ToolTip()
                    tooltip.SetToolTip(Label_LidarStatus, "LiDAR capture is disabled in configuration")
                End If

                ' Compact GroupBox height for single-line status only
                GroupBox_LidarStatus.Height = 50  ' Compact height for status label only

                ' Make label clickable only if capture is enabled
                If LidarCaptureEnabled AndAlso Label_LidarStatus IsNot Nothing Then
                    Label_LidarStatus.Cursor = Cursors.Hand
                    ' Add tooltip to indicate it's clickable
                    Dim tooltip As New ToolTip()
                    tooltip.SetToolTip(Label_LidarStatus, "Click for detailed LiDAR health diagnostics")
                End If

                HandleUserMessageLogging("OVS", $"LiDAR status panel initialized ({LidarDevices.Count} device(s)) - Capture {If(LidarCaptureEnabled, "Enabled", "Disabled")}")
            Else
                ' No devices configured - hide the panel entirely
                GroupBox_LidarStatus.Visible = False
                HandleUserMessageLogging("OVS", "LiDAR status panel hidden - No devices configured")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("OVS", $"InitializeLidarStatusPanel: {ex.Message}")
            GroupBox_LidarStatus.Visible = False
        End Try
    End Sub

    ''' <summary>
    ''' Updates the LiDAR status UI with current capture state and statistics.
    ''' Simplified to show only the main status indicator - details available via click.
    ''' </summary>
    Private Sub UpdateLidarStatusUI(isCapturing As Boolean, Optional deviceCount As Integer = 0, Optional healthStatus As LidarHealthStatus = LidarHealthStatus.Healthy)
        Try
            If Not GroupBox_LidarStatus.Visible Then Return

            ' Update label text and background colors based on capture state
            If isCapturing Then
                ' ✅ CHANGE: Set text color to WHITE when recording for better contrast
                Label_LidarStatus.ForeColor = Color.White

                Select Case healthStatus
                    Case LidarHealthStatus.Healthy
                        Label_LidarStatus.BackColor = Color.Green
                        Label_LidarStatus.Text = $"● LiDAR ({deviceCount}) - Healthy"

                    Case LidarHealthStatus.Warning
                        Label_LidarStatus.BackColor = Color.Orange
                        Label_LidarStatus.Text = $"● LiDAR ({deviceCount}) - WARNING"

                    Case LidarHealthStatus.Critical
                        Label_LidarStatus.BackColor = Color.Red
                        Label_LidarStatus.Text = $"● LiDAR ({deviceCount}) - CRITICAL"
                End Select
            Else
                ' Stopped state - keep black text for gray background
                Label_LidarStatus.ForeColor = Color.Black
                Label_LidarStatus.Text = $"● LiDAR ({deviceCount}) - Stopped"
                Label_LidarStatus.BackColor = Color.LightGray
                GroupBox_LidarStatus.ForeColor = Color.Gray  ' Border matches
            End If

            ' Keep GroupBox border color black for all states
            If isCapturing Then
                GroupBox_LidarStatus.ForeColor = Color.Black
            End If

        Catch ex As Exception
            HandleUserMessageLogging("OVS", $"UpdateLidarStatusUI: {ex.Message}")
        End Try
    End Sub

    Private Sub SetupControlAnchoring()
        ' Allow the form to be resizable
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MinimumSize = New Size(MIN_FORM_WIDTH, MIN_FORM_HEIGHT)

        ' Main control buttons - keep them anchored to top-left
        Button6.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        Button14.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        Button7.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        Button3.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        Button10.Anchor = AnchorStyles.Top Or AnchorStyles.Left

        ' Right-side buttons - anchor to top-right
        Button23.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Button1.Anchor = AnchorStyles.Top Or AnchorStyles.Right ' EXIT button
        Button2.Anchor = AnchorStyles.Top Or AnchorStyles.Right ' CONFIG button
        Button4.Anchor = AnchorStyles.Top Or AnchorStyles.Right ' LOGIN button

        ' Status labels - stretch across the top
        Label5.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right

        ' Control labels and combo - anchor appropriately
        Label7.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        ComboBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        Label9.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Label8.Anchor = AnchorStyles.Top Or AnchorStyles.Right

        ' GroupBox4 - stretch across the width
        GroupBox4.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right

        ' Main content area - GroupBox1 should expand with form
        GroupBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right

        ' ListBox1 should also expand
        ListBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right

        ' Popup groupboxes maintain their current behavior
        GroupBox3.Anchor = AnchorStyles.None ' Cameras dropdown
        GroupBox5.Anchor = AnchorStyles.None ' SubCategory dropdown  
        GroupBox6.Anchor = AnchorStyles.None ' Voice Commands dropdown
    End Sub

    Private Sub Label17_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Label17.Click
        GmResidentClient.ShowCustomScreen("Secret Squirrel Screen")
    End Sub

    Private Sub Button5_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button5.Click

        'Save Cal Snap Shot button on OnVehicleScreen form...
        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "SAVE CAL SNAPSHOT Button Pressed...")
        HandleUserMessageLogging("GMRC", " ")

        MyIncaInterface.SaveCalSnapShot("Working")

    End Sub

    Private Sub Label8_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Label8.Click
        ' Convert the click point to ListBox coordinates if necessary.
        HideListBox4IfClickedOutside()
    End Sub

    Private Sub ComboBox1_SelectedValueChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ComboBox1.SelectedValueChanged
        Try
            If String.IsNullOrWhiteSpace(ComboBox1.Text) Then Return

            Dim mins As Integer = -1 ' -1 means ALL
            Dim textVal As String = ComboBox1.Text.Trim().ToUpperInvariant()

            If textVal = "ALL" Then
                mins = -1
            ElseIf Integer.TryParse(ComboBox1.Text, mins) Then
                ' Valid numeric input
                If mins < 0 OrElse mins > 60 Then
                    HandleUserMessageLogging("Warning", "Record duration should be between 0-60 minutes. Using default 1 minute.")
                    mins = 1
                End If
            Else
                HandleUserMessageLogging("Error", "Invalid input for record duration; defaulting to 1 minute")
                mins = 1
                ComboBox1.Text = "1"
            End If

            ' Update local/global UI state (keeps legacy usage)
            RecordFileDurationMinutes = mins

            ' Inform GM_INCA_Comm so it can gate expensive checks
            Try
                If MyIncaInterface IsNot Nothing AndAlso MyIncaInterface.MyGmIncaComm IsNot Nothing Then
                    MyIncaInterface.MyGmIncaComm.SetRecordingFileDurationMinutes(mins)
                End If
            Catch ex As Exception
                HandleUserMessageLogging("GMRC", "ComboBox1_SelectedValueChanged: Failed to set recording duration in GM_INCA_Comm: " & ex.Message)
            End Try

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ComboBox1_SelectedValueChanged: " & ex.Message)
        End Try
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(ByVal sender As Object, ByVal e As EventArgs) Handles ComboBox1.SelectedIndexChanged

    End Sub

    Private Sub TextBox1_TextChanged(ByVal sender As Object, ByVal e As EventArgs) Handles TextBox1.TextChanged

        'the duration of the wav file recording is set using the textbox that is in the middle
        'of the microphone display at the upper right of the resident client display.  So the
        'recording of the voice will go for the duration indicated, if 0, then it will go until
        'the user stops it by pressing on the microphone.
        If Len(TextBox1.Text) > 0 Then
            RecordWAVTime = TextBox1.Text
        End If

    End Sub

    ''' <summary>
    ''' Click handler for LiDAR status label - shows detail form
    ''' </summary>
    Private Sub Label_LidarStatus_Click(sender As Object, e As EventArgs) Handles Label_LidarStatus.Click
        Try
            ' Only show details if capture is enabled
            If Not LidarCaptureEnabled Then
                MessageBox.Show("LiDAR capture is disabled in configuration.", "LiDAR Status", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            If LidarDevices IsNot Nothing AndAlso LidarDevices.Count > 0 Then
                Dim detailForm As New LidarHealthDetailForm(LidarDevices, GmResidentClient)
                detailForm.ShowDialog(Me)
            Else
                MessageBox.Show("No LiDAR devices configured.", "LiDAR Status", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            HandleUserMessageLogging("OVS", $"Label_LidarStatus_Click: {ex.Message}")
        End Try
    End Sub
    Private Sub Button7_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button7.Click

        If GroupBox3.Visible Then
            ' Hide the list box if it is currently visible
            GroupBox3.Visible = False
            ListBox2.Visible = False

        Else
            ' Populate ListBox2 if it is not already populated
            If ListBox2.Items.Count = 0 Then
                For x As Integer = 0 To UBound(CameraNames)
                    ListBox2.Items.Add(CameraNames(x))
                Next
                ' Optionally add a "Cancel" item
                'ListBox2.Items.Add("Cancel")
            End If

            ' --- Begin Dynamic Sizing Logic for ListBox3 (mirroring ListBox4) ---
            Dim itemHeight As Integer = ListBox2.ItemHeight
            Dim totalItems As Integer = ListBox2.Items.Count
            ' (Added an extra count when the Cancel item wasn't yet in the list.
            ' Here we add Cancel to the list so we can use the count directly.)
            totalItems += 1
            Dim maxHeight As Integer = MAX_LISTBOX_HEIGHT  ' Maximum allowed height
            Dim calculatedHeight As Integer = Math.Min(itemHeight * totalItems, maxHeight)
            ListBox2.Height = calculatedHeight

            ' Adjust the width to fit the longest item
            Dim maxWidth As Integer = MAX_LISTBOX_WIDTH  ' Maximum allowed width
            Dim longestItemWidth As Integer = ListBox2.Items.Cast(Of String)() _
                    .Max(Function(cmd) TextRenderer.MeasureText(cmd, ListBox2.Font).Width)
            ListBox2.Width = Math.Min(longestItemWidth + SystemInformation.VerticalScrollBarWidth, maxWidth)
            ' --- End Dynamic Sizing Logic for ListBox2 ---

            ' Finally, show and bring ListBox2 to the front
            GroupBox3.Visible = True
            ListBox2.Visible = True
            GroupBox3.BringToFront()
            ' Set focus to the ListBox
            ListBox2.Focus()
        End If
    End Sub

    Private Sub RadioButton1_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles RadioButton1.CheckedChanged

        If RadioButton1.Checked = True Then
            If MyIncaInterface.SwitchToWorkingPage = False Then
                HandleUserMessageLogging("GMRC", "Switch to Working Page FAILED")
                If Not IO.File.Exists(My.Application.Info.DirectoryPath & "\IgnoreChecksumMismatch.txt") Then
                    HandleUserMessageLogging("GMRC", "INCA Switch To Working Page FAILED.", DisplayMsgBox)
                End If

            Else
                HandleUserMessageLogging("GMRC", "User Switched to Working Page")
            End If
        End If

    End Sub

    Private Sub RadioButton2_CheckedChanged(ByVal sender As Object, ByVal e As EventArgs) Handles RadioButton2.CheckedChanged

        If RadioButton2.Checked = True Then
            If MyIncaInterface.SwitchToReferencePage = False Then
                HandleUserMessageLogging("GMRC", "RadioButton2_CheckedChanged: Switch to Reference Page FAILED")
                If Not IO.File.Exists(My.Application.Info.DirectoryPath & "\IgnoreChecksumMismatch.txt") Then
                    HandleUserMessageLogging("GMRC", "RadioButton2_CheckedChanged: Switch to Reference Page FAILED", DisplayMsgBox)
                End If

            Else
                HandleUserMessageLogging("GMRC", "RadioButton2_CheckedChanged: User Switched to Reference Page")
            End If
        End If

    End Sub

    Private Sub Button6_DoubleClick(ByVal sender As Object, ByVal e As EventArgs) Handles Button6.DoubleClick

    End Sub

    Private Sub Label5_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Label5.Click
        ' Convert the click point to ListBox coordinates if necessary.
        HideListBox4IfClickedOutside()
    End Sub

    Private Sub Button8_Click(ByVal sender As Object, ByVal e As EventArgs) Handles Button8.Click

        HandleUserMessageLogging("GMRC", " ")
        HandleUserMessageLogging("GMRC", "CALIBRATE Button Pressed...")
        HandleUserMessageLogging("GMRC", " ")

        If Form1.Visible = False Then
            Form1.Show()
        Else
            Form1.BringToFront()
            Form1.Refresh()
        End If

        Form1.WhichDisplayIsInFront = "Calibrate"

    End Sub

    Private Sub ListBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedIndexChanged

        If ListBox2.Items(ListBox2.SelectedIndex).ToString <> "NA" Then
            Process.Start("http://" & GmResidentClient.CameraIpAddresses(ListBox2.SelectedIndex))
        End If

        'GroupBox3.Visible = False
    End Sub

    Private Sub ListBox2_SelectedValueChanged(sender As Object, e As EventArgs) Handles ListBox2.SelectedValueChanged

    End Sub

    Private Sub ListBox2_Click(sender As Object, e As EventArgs) Handles ListBox2.Click
        GroupBox3.Visible = False
    End Sub

    Private Sub SetupCustomListBox()
        ' Set the ListBox to owner-draw mode
        ListBox4.DrawMode = DrawMode.OwnerDrawFixed
        ListBox4.ItemHeight = 40  ' ✅ ADD THIS: Taller items for better touch targets
        AddHandler ListBox4.DrawItem, AddressOf ListBox4_DrawItem
    End Sub

    ' Add this event handler to OnVehicleScreen.vb (in the designer or code-behind)
    Private Sub ListBox4_DrawItem(sender As Object, e As DrawItemEventArgs) Handles ListBox4.DrawItem
        If e.Index < 0 Then Return

        Try
            ' Draw the background
            e.DrawBackground()

            ' Get the item text
            Dim itemText As String = ListBox4.Items(e.Index).ToString()

            ' ✅ MATCH MAIN BUTTON STYLING
            Dim bgColor As Color
            Dim borderColor As Color = Color.DarkBlue  ' ← Match main button border
            Dim textColor As Color = Color.Black

            ' Determine background color based on selection and item type
            If itemText.Trim().Equals("Cancel", StringComparison.OrdinalIgnoreCase) Then
                ' Cancel item - distinct styling but consistent with main buttons
                bgColor = If((e.State And DrawItemState.Selected) = DrawItemState.Selected,
                     Color.LightGray, Color.WhiteSmoke)  ' MouseDown color when selected
            Else
                ' Normal items - match main button hover/selection behavior
                bgColor = If((e.State And DrawItemState.Selected) = DrawItemState.Selected,
                     Color.LightBlue, Color.White)  ' MouseOver color when selected
            End If

            ' Draw background with selection highlight
            Using bgBrush As New SolidBrush(bgColor)
                e.Graphics.FillRectangle(bgBrush, e.Bounds)
            End Using

            ' ✅ MATCH MAIN BUTTON: 2px DarkBlue border (no rounding)
            Using pen As New Pen(borderColor, 2)  ' ← Match main button border thickness
                Dim borderRect As New Rectangle(e.Bounds.X + 1, e.Bounds.Y + 1,
                                        e.Bounds.Width - 2, e.Bounds.Height - 2)
                e.Graphics.DrawRectangle(pen, borderRect)
            End Using

            ' ✅ MATCH MAIN BUTTON: Arial 10pt Bold text
            Dim buttonFont As New Font("Arial", 10, FontStyle.Bold)  ' ← Match main button font

            ' Draw the text centered
            Using textBrush As New SolidBrush(textColor)
                Dim textFormat As New StringFormat With {
                .Alignment = StringAlignment.Center,
                .LineAlignment = StringAlignment.Center
            }
                e.Graphics.DrawString(itemText, buttonFont, textBrush, e.Bounds, textFormat)
            End Using

            ' Draw focus rectangle if the item has focus
            e.DrawFocusRectangle()

            ' Clean up font
            buttonFont.Dispose()

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ListBox4_DrawItem error: {ex.Message}")
        End Try
    End Sub

    Private Sub ListBox4_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox4.SelectedIndexChanged
        Try
            ' Ensure a sub-category is selected
            If ListBox4.SelectedIndex >= 0 Then
                Dim selectedSubCategory As String = ListBox4.SelectedItem.ToString()

                ' If the user selected "Cancel", hide the drop-down and exit.
                If selectedSubCategory.Trim().ToLower() = "cancel" Then
                    GroupBox5.Hide()
                    ListBox4.Hide()
                    Exit Sub
                End If

                Dim buttonText As String = SaveAnnoButtonText  ' Original button text
                Dim parentText As String = ListBox4.Tag.ToString()  ' Parent tab text
                Dim listIndex As Integer = ListBox4.SelectedIndex  ' Selected index
                Dim listBoxSelected As Boolean = True

                ' Hide GroupBox5 and ListBox4 after a valid selection
                GroupBox5.Hide()
                ListBox4.Hide()

                ' Create a button instance to represent the selected button
                Dim thisButton As New Button With {.Text = buttonText}

                ' Construct the full comment here and pass it directly
                Dim fullComment As String = $"{parentText} {buttonText} - {selectedSubCategory}"

                ' Call HandleAnnotationButtons in Module1, passing necessary data including SequenceNumber
                HandleAnnotationButtons(thisButton, parentText, fullComment, selectedSubCategory, listIndex, listBoxSelected)

                ' Measure the width of the selected sub-category text
                Dim textSize As Size = TextRenderer.MeasureText(selectedSubCategory, ListBox4.Font)

                ' Adjust the width of the ListBox to fit the text
                ListBox4.Width = textSize.Width + SystemInformation.VerticalScrollBarWidth ' Add scrollbar width for padding
            End If
        Catch ex As Exception
            ' Log the error message or handle it as needed
            HandleUserMessageLogging("Error", ex.Message)
            MessageBox.Show("An error occurred while processing the selection. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OpenFileDialog1_FileOk(sender As Object, e As CancelEventArgs) Handles OpenFileDialog1.FileOk

    End Sub

    Private Sub OnVehicleScreen_MouseDown(sender As Object, e As MouseEventArgs) Handles Me.MouseDown
        ' Convert the click point to ListBox coordinates if necessary.
        HideListBox4IfClickedOutside()
    End Sub

    Private Sub OnVehicleScreen_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        ' Only handle keys A–Z
        If e.KeyCode >= Keys.A AndAlso e.KeyCode <= Keys.Z Then

            ' Convert to uppercase char (e.g., "A", "B", "C" ...)
            Dim pressedKey As String = ChrW(e.KeyCode)

            ' Grab the singleton
            Dim dataDict = DataDictionarySingleton.GetInstance()

            ' Loop each sub-tab
            For Each kvp In dataDict.SubTabs
                Dim subTabName As String = kvp.Key
                Dim subTabObj As DataDictionarySingleton.SubTab = kvp.Value

                ' Each sub-tab has a list of EventButtons
                For Each eButton In subTabObj.EventButtons
                    ' Compare pressedKey to the event button's name (or the UI button's text)
                    If eButton.ButtonName.ToUpper() = pressedKey Then
                        ' Found a match -> call the centralized handler
                        ' We pass the actual UIButton, plus optional arguments (e.g., subTabName)
                        If eButton.UIButton IsNot Nothing Then
                            HandleAnnotationButtons(eButton.UIButton,
                                                    buttonParentText:=subTabName,
                                                    fullAnnotationText:=eButton.ButtonName)
                        End If

                        ' Once handled, we can exit (unless you need to keep searching)
                        Exit Sub
                    End If
                Next
            Next
        End If
    End Sub

    Private Async Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
        Try
            Await Button9_ClickAsync()
        Catch ex As Exception
            HandleUserMessageLogging("Error", $"Button9_Click: {ex.Message}")
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Function Button9_ClickAsync() As Task
        Try
            If Button9.BackColor = Color.LightGreen Then
                Button9.BackColor = SystemColors.Control
                HandleUserMessageLogging("GMRC", "Processing Escalations Disabled",,, FlashMsg1Sec)
                ProcessEscalations = False

            ElseIf Button9.BackColor = SystemColors.Control Then
                Button9.BackColor = Color.LightGreen
                HandleUserMessageLogging("GMRC", "Processing Escalations Enabled",,, FlashMsg1Sec)
                ProcessEscalations = True

            ElseIf Button9.BackColor = Color.Yellow Then
                HandleUserMessageLogging("GMRC", "Processing Escalations PENDING - User Message Displayed...",, )
                MsgBox("Escalation Processing functionality is PENDING and will be enabled when first recording session is stopped...")

            Else ' Color Red...
                HandleUserMessageLogging("GMRC", "OnVehicleScreen.Button9 Pressed. Processing Escalations Not Available...",, )

                If UsingFlashDrive = False Then
                    If IO.Directory.Exists(NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\UpdatedFiles\ProcessEscalationFiles") Then

                        If MsgBox("Escalation Processing functionality is not available on this PC. Would you like to make it available?", vbYesNo) = vbYes Then
                            HandleUserMessageLogging("GMRC", "User Chose to enable Escalation Processing...",, )
                            HandleUserMessageLogging("GMRC", "Copying Files...",,, FlashMsgOn)

                            ' Use async file copying with proper UI feedback
                            Await CopyEscalationFilesAsync()

                            Button9.BackColor = Color.Yellow
                            HandleUserMessageLogging("GMRC", "Processing Escalations PENDING",,, FlashMsg2Sec)
                        End If
                    Else
                        MsgBox("Escalation Processing functionality is not available on this PC.")
                    End If
                Else
                    MsgBox("Escalation Processing functionality is not available on this PC.")
                End If
            End If

        Catch ex As Exception
            HandleUserMessageLogging("Error", $"Button9_ClickAsync: {ex.Message}")
            Throw ' Re-throw to be caught by the synchronous wrapper
        End Try
    End Function

    Private Async Function CopyEscalationFilesAsync() As Task
        Try
            ' Disable the button during the operation
            Button9.Enabled = False
            Cursor = Cursors.WaitCursor

            ' Perform file copying on a background thread
            Await Task.Run(Sub()
                               Try
                                   Dim sourceDir As String = NetworkDriveMapping & CLEVIRBaseDir & "\Updated CLEVIR Files for Vehicles\CLEVIR Executables - Installs - Support Files\UpdatedFiles\ProcessEscalationFiles"
                                   Dim dir As New IO.DirectoryInfo(sourceDir)
                                   Dim files As IO.FileInfo() = dir.GetFiles()

                                   For Each file In files
                                       Dim destPath As String = IO.Path.Combine(My.Application.Info.DirectoryPath, file.Name)
                                       IO.File.Copy(file.FullName, destPath, True)
                                   Next

                               Catch ex As Exception
                                   ' Log any file operation errors
                                   HandleUserMessageLogging("Error", $"File copy operation failed: {ex.Message}")
                                   Throw
                               End Try
                           End Sub)

        Catch ex As Exception
            HandleUserMessageLogging("Error", $"CopyEscalationFilesAsync: {ex.Message}")
            Throw
        Finally
            ' Always restore UI state
            Button9.Enabled = True
            Cursor = Cursors.Arrow
            UserStatusInfo.Hide()
        End Try
    End Function

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click

        If GmResidentClient.MyTdGraphicsContainer.Visible = False Then
            GmResidentClient.MyTdGraphicsContainer.Left = Left
            GmResidentClient.MyTdGraphicsContainer.Top = Top
        End If
        GmResidentClient.MyTdGraphicsContainer.ControlBox = True
        GmResidentClient.MyTdGraphicsContainer.Show()
        GmResidentClient.MyTdGraphicsContainer.BringToFront()
    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click
        TriggerWAVRecording = True
        ' Convert the click point to ListBox coordinates if necessary.
        HideListBox4IfClickedOutside()
    End Sub

    Private Sub GroupBox4_Enter(sender As Object, e As EventArgs) Handles GroupBox4.Enter

    End Sub

    Private Sub GroupBox5_Enter(sender As Object, e As EventArgs) Handles GroupBox5.Enter

    End Sub

    Private Sub ListBox3_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox3.SelectedIndexChanged

    End Sub

    Private Sub Label9_Click(sender As Object, e As EventArgs) Handles Label9.Click
        ' Convert the click point to ListBox coordinates if necessary.
        HideListBox4IfClickedOutside()
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub GroupBox1_Enter(sender As Object, e As EventArgs) Handles GroupBox1.Enter

    End Sub

    Private Sub Label3_Click(sender As Object, e As EventArgs) Handles Label3.Click

    End Sub

    Private Sub HideListBox4IfClickedOutside()
        If ListBox4.Visible Then
            Dim clickPoint As Point = Me.PointToClient(Cursor.Position)
            If Not ListBox4.Bounds.Contains(clickPoint) Then
                ListBox4.Visible = False
            End If
        End If
    End Sub

    Private Sub SetButtonsEnabled(enabled As Boolean)
        Button6.Enabled = enabled
        Button14.Enabled = enabled
        Button1.Enabled = enabled
    End Sub

    Private Async Sub ExecuteWithButtonDisablingAsync(action As Func(Of Task))
        Try
            SetButtonsEnabled(False)
            Cursor = Cursors.WaitCursor

            ' Run on background thread to avoid blocking UI
            Await Task.Run(action)

        Catch ex As Exception
            HandleUserMessageLogging("Error", $"Background action failed: {ex.Message}")
        Finally
            SetButtonsEnabled(True)
            Cursor = Cursors.Arrow
        End Try
    End Sub

    Private Sub ConfigureListBoxSize(listBox As ListBox, maxHeight As Integer, maxWidth As Integer)
        Dim itemHeight As Integer = listBox.ItemHeight
        Dim totalItems As Integer = listBox.Items.Count + 1
        Dim calculatedHeight As Integer = Math.Min(itemHeight * totalItems, maxHeight)
        listBox.Height = calculatedHeight

        If listBox.Items.Count > 0 Then
            Dim longestItemWidth As Integer = listBox.Items.Cast(Of String)() _
                    .Max(Function(item) TextRenderer.MeasureText(item, listBox.Font).Width)
            listBox.Width = Math.Min(longestItemWidth + SystemInformation.VerticalScrollBarWidth, maxWidth)
        End If
    End Sub

End Class
