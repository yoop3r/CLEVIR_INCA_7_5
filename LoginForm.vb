Option Strict Off
Option Explicit On
Imports System.IO
Imports System.Drawing.Drawing2D

Public Class LoginForm
    'Streamlined login form displaying up to 5 user login buttons.
    'Allows signal registration mode selection, workspace changes, and LiDAR/recording configuration.

    Private _isExitButtonClick As Boolean = False
    Private _isInitializing As Boolean = True
    Private _loginSubmitButton As Button = Nothing ' Reference to the LOGIN button

    ''' <summary>
    ''' ✅ ENHANCED: Handle LOGIN button click (form submission) with required field validation
    ''' </summary>
    Private Sub LoginSubmit_Click(sender As Object, e As EventArgs)
        Try
            ' ═══════════════════════════════════════════════════════════════════
            ' GUARD 2: Validate Required Session Metadata Fields
            ' ═══════════════════════════════════════════════════════════════════
            Dim missingFields As New List(Of String)()

            ' Validate ADAS Group field
            If String.IsNullOrWhiteSpace(ComboBox_Group.Text) Then
                missingFields.Add("ADAS Group")
            End If

            ' Validate Test Type field
            If String.IsNullOrWhiteSpace(ComboBox_Procedure.Text) Then
                missingFields.Add("Test Type")
            End If

            ' Validate Email field
            If String.IsNullOrWhiteSpace(TextBox_Email.Text) Then
                missingFields.Add("Email")
            End If

            ' If any required fields are missing, show error and abort login
            If missingFields.Count > 0 Then
                Dim fieldList As String = String.Join(", ", missingFields)
                Dim message As String = If(missingFields.Count = 1,
                    $"The following required field is missing: {fieldList}" & vbCrLf & vbCrLf &
                    "Please provide this information to enable data traceability.",
                    $"The following required fields are missing: {fieldList}" & vbCrLf & vbCrLf &
                    "Please provide all required information to enable data traceability.")

                StatusNotifier.Warn(message, "Required Fields Missing")
                HandleUserMessageLogging("GMRC", $"LoginForm: LOGIN blocked - missing required fields: {fieldList}")

                ' Highlight first missing field for user convenience
                If missingFields.Contains("ADAS Group") Then
                    ComboBox_Group.Focus()
                ElseIf missingFields.Contains("Test Type") Then
                    ComboBox_Procedure.Focus()
                ElseIf missingFields.Contains("Email") Then
                    TextBox_Email.Focus()
                End If

                Return ' Stay on login form
            End If

            DebugMode = DebugKey

            ' ═══════════════════════════════════════════════════════════════════
            ' Capture Session Metadata (All Fields Now Required)
            ' ═══════════════════════════════════════════════════════════════════
            Try
                ' Trim and capture required fields
                SaveGroupName = ComboBox_Group.Text.Trim()
                SaveProcedureName = ComboBox_Procedure.Text.Trim()
                SaveEmailAddress = TextBox_Email.Text.Trim()
                SaveLoginID = GetDriverIDFromEmail(SaveEmailAddress)

                ' Validate email format (now mandatory since field is required)
                If Not IsValidEmail(SaveEmailAddress) Then
                    Dim result = StatusNotifier.Confirm(
                        $"Email format appears invalid: {SaveEmailAddress}" & vbCrLf & vbCrLf &
                        "Continue anyway?",
                        "Email Validation"
                    )
                    If Not result Then
                        TextBox_Email.Focus()
                        Return ' Stay on login form
                    End If
                End If

                HandleUserMessageLogging("LoginForm",
                    $"Session metadata captured - Driver: [{SaveLoginID}], ADAS Group: [{SaveGroupName}], Test Type: [{SaveProcedureName}], Email: [{SaveEmailAddress}]")

            Catch ex As Exception
                ' Don't block login on metadata errors
                HandleUserMessageLogging("LoginForm", $"Error capturing session metadata: {ex.Message}")
            End Try

            HandleUserMessageLogging("GMRC", $"LoginForm: User logged in as '{SaveLoginID}'")
            StatusNotifier.Toast($"LoginForm: User logged in as '{SaveLoginID}'", "Login", durationMs:=1000, ensureMainOnTop:=False)
            Me.DialogResult = DialogResult.OK ' ✅ Close form gracefully


        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"LoginSubmit_Click: {ex.Message}", DisplayMsgBox)
        End Try
    End Sub

    Private Sub HandleFullSigRegSelected(Optional ByVal onLogin As Boolean = False)
        'Warns user about FULL signal registration performance impact

        Dim msgBoxText As String = "FULL signal registration will take a LONG time. This is typically not necessary when running CLEVIR in a vehicle. Are you sure you want to do a FULL signal registration?"

        If ClevirAdministrator = False Then
            If (SaveSignalRegistrationMode <> "FULL" And onLogin = False) Then
                If MsgBox(msgBoxText, vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", "User answered YES to FULL signal registration...")
                    SignalRegistrationMode = "FULL"
                Else
                    HandleUserMessageLogging("GMRC", "User answered NO to FULL signal registration...")
                    SignalRegistrationMode = SaveSignalRegistrationMode
                End If
            ElseIf (SaveSignalRegistrationMode = "FULL" And onLogin = True) Then
                If MsgBox(msgBoxText, vbYesNo) = vbYes Then
                    HandleUserMessageLogging("GMRC", "User answered YES to FULL signal registration...")
                    SignalRegistrationMode = "FULL"
                Else
                    HandleUserMessageLogging("GMRC", "User answered NO to FULL signal registration...")
                    SignalRegistrationMode = "DISPLAYS"
                End If
            End If

            Select Case SignalRegistrationMode
                Case "DISPLAYS"
                    RadioButton2.Checked = True
                Case "GO/NOGO"
                    RadioButton3.Checked = True
            End Select
        End If

        SaveSignalRegistrationMode = SignalRegistrationMode
        HandleUserMessageLogging("GMRC", "FULL Signal Registration Mode was selected. Final Signal Registration Mode is " & SaveSignalRegistrationMode)
    End Sub

    Private Sub LoginForm_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        OnLoginScreen = True
    End Sub

    Private Sub LoginForm_Deactivate(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Deactivate
        OnLoginScreen = False
    End Sub

    Private Sub LoginForm_FormClosing(ByVal sender As Object, ByVal e As FormClosingEventArgs) Handles Me.FormClosing

        ' ✅ FIX: Don't re-close during exit
        If exitInProgress Then
            e.Cancel = False  ' Allow close
            Return
        End If

        If Me.DialogResult = DialogResult.OK OrElse Me.DialogResult = DialogResult.Retry Then
            HandleUserMessageLogging("GMRC", $"LoginForm_FormClosing: Programmatic close via DialogResult={Me.DialogResult}, allowing close")
            e.Cancel = False
            Return
        End If

        If e.CloseReason = CloseReason.UserClosing AndAlso Not _isExitButtonClick Then
            HandleUserMessageLogging("GMRC", "LoginForm_FormClosing: User clicked X, calling Exit handler")
            Button43_Click(sender, EventArgs.Empty)
            e.Cancel = True
        Else
            HandleUserMessageLogging("GMRC", $"LoginForm_FormClosing: Programmatic close, CloseReason={e.CloseReason}, _isExitButtonClick={_isExitButtonClick}")
            e.Cancel = False
        End If
    End Sub

    Private Sub LoginForm_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        'Alphabetical key enables DebugMode (bypass checksum mismatch termination)
        If e.KeyCode >= 65 And e.KeyCode <= 90 Then
            DebugKey = True
        End If
    End Sub

    Private Sub LoginForm_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        If PATAC = True Or CurrentVehicleUsage = "VALIDATION" Then
            Me.CheckBox1.Visible = False
            RadioButton4.Visible = False
        End If

        If ClevirAdministrator = True Then
            RadioButton4.Enabled = True
        End If

        ' ✅ Apply signal registration mode from config.xml (deferred initialization)
        ApplySignalRegistrationModeFromConfig()

        If ConfigureForNewSoftwareVersion = True Then
            RadioButton4.Checked = True
        End If

        _isInitializing = True
        InitializeAlternateRecordingCheckbox()  ' ✅ ADD THIS
        InitializeLidarCheckbox()

        ' ═══════════════════════════════════════════════════════════════════
        ' Populate Session Metadata Dropdowns
        ' ═══════════════════════════════════════════════════════════════════
        Try
            ComboBox_Group.Items.Clear()
            ComboBox_Group.Items.AddRange(PredefinedGroups)
            ComboBox_Group.SelectedIndex = -1 ' No default selection

            ComboBox_Procedure.Items.Clear()
            ComboBox_Procedure.Items.AddRange(PredefinedProcedures)
            ComboBox_Procedure.SelectedIndex = -1

            ' ✅ CRITICAL: Ensure controls are enabled for user interaction
            ComboBox_Group.Enabled = True
            ComboBox_Group.TabStop = True
            ComboBox_Procedure.Enabled = True
            ComboBox_Procedure.TabStop = True
            TextBox_Email.Enabled = True
            TextBox_Email.TabStop = True

            ' Setup ToolTips (updated to reflect required status)
            Dim toolTip As New ToolTip()
            toolTip.SetToolTip(ComboBox_Group, "[REQUIRED] Enter your functional group name (e.g., ADAS, Infotainment)")
            toolTip.SetToolTip(ComboBox_Procedure, "[REQUIRED] Enter the test procedure or project name")
            toolTip.SetToolTip(TextBox_Email, "[REQUIRED] Enter your email address for data traceability")

        Catch ex As Exception
            HandleUserMessageLogging("LoginForm", $"Error initializing session metadata: {ex.Message}")
        End Try

        ' ═══════════════════════════════════════════════════════════════════
        ' ✅ NEW: Create LOGIN button (positioned next to EXIT button)
        ' ═══════════════════════════════════════════════════════════════════
        Try
            ' Find the EXIT button (Button43) to position LOGIN next to it
            Dim exitButton As Button = Button43

            ' Create LOGIN button with matching styling
            _loginSubmitButton = New Button With {
                .Text = "LOGIN",
                .Size = exitButton.Size, ' Match EXIT button size
                .Font = exitButton.Font, ' Match EXIT button font
                .BackColor = System.Drawing.SystemColors.Control,
                .ForeColor = System.Drawing.SystemColors.ControlText,
                .FlatStyle = FlatStyle.Flat,
                .Cursor = Cursors.Hand,
                .TabStop = True,
                .Enabled = True,
                .UseVisualStyleBackColor = True
            }

            ' Configure flat appearance
            _loginSubmitButton.FlatAppearance.BorderColor = Color.SteelBlue
            _loginSubmitButton.FlatAppearance.BorderSize = 1
            _loginSubmitButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 241, 251)
            _loginSubmitButton.FlatAppearance.MouseDownBackColor = Color.LightGray

            ' Position LOGIN button to the left of EXIT button with 10px spacing
            _loginSubmitButton.Left = exitButton.Left - _loginSubmitButton.Width - 10
            _loginSubmitButton.Top = exitButton.Top

            ' Add to form and wire up event
            Me.Controls.Add(_loginSubmitButton)
            _loginSubmitButton.BringToFront()
            AddHandler _loginSubmitButton.Click, AddressOf LoginSubmit_Click

            ' ✅ Route Enter key to LOGIN button (standard WinForms AcceptButton pattern)
            Me.AcceptButton = _loginSubmitButton

            HandleUserMessageLogging("GMRC", "LoginForm: LOGIN button created (disabled until driver selected)")

        Catch ex As Exception
            HandleUserMessageLogging("LoginForm", $"Error creating LOGIN button: {ex.Message}")
        End Try

        ' ✅ IMPORTANT: Set _isInitializing to False AFTER all initialization
        _isInitializing = False

        ' ═══════════════════════════════════════════════════════════════════
        ' Update Label4 with current config info (MOVED FROM MODULE1)
        ' ═══════════════════════════════════════════════════════════════════
        Try
            If Not String.IsNullOrEmpty(INCAVariableFile) AndAlso Not String.IsNullOrEmpty(INCAExperiment) Then
                Label4.Text = $"{Path.GetFileName(INCAVariableFile)} / {INCAExperiment}"
            Else
                Label4.Text = "Config not yet verified"
            End If
        Catch ex As Exception
            HandleUserMessageLogging("LoginForm", $"Error updating Label4: {ex.Message}")
            Label4.Text = "Config info unavailable"
        End Try

        ' ═══════════════════════════════════════════════════════════════════
        ' ✅ NOTE: CheckBox3 configuration now handled by InitializeAlternateRecordingCheckbox()
        ' ═══════════════════════════════════════════════════════════════════

        HandleUserMessageLogging("GMRC", "LoginForm_Load: Initialization complete, form ready for user interaction")
    End Sub

    ''' <summary>
    ''' ✅ ENHANCED: Initialize Alternate Recording checkbox with validation (mirrors LiDAR logic)
    ''' </summary>
    Private Sub InitializeAlternateRecordingCheckbox()
        Try
            ' Temporarily remove handler to prevent spurious events during initialization
            RemoveHandler CheckBox3.CheckedChanged, AddressOf CheckBox3_CheckedChanged

            ' ✅ NEW: Validate AlternateRecordingMode configuration
            If String.IsNullOrEmpty(AlternateRecordingMode) OrElse AlternateRecordingMode = "None" Then
                CheckBox3.Text = "Alternate Recording (Not Configured)"
                CheckBox3.Checked = False
                CheckBox3.Enabled = False
                CheckBox3.Visible = False
                HandleUserMessageLogging("GMRC", "LoginForm: Alternate Recording disabled - mode set to 'None' or empty")
                AddHandler CheckBox3.CheckedChanged, AddressOf CheckBox3_CheckedChanged
                Return
            End If

            ' ✅ NEW: Set descriptive text based on recording mode
            Select Case AlternateRecordingMode
                Case "CANalyzer"
                    CheckBox3.Text = "Enable CANalyzer Recording"
                    CheckBox3.Visible = True
                    CheckBox3.Enabled = True
                    CheckBox3.Checked = AlternateRecordEnabled
                    HandleUserMessageLogging("GMRC", $"LoginForm: CANalyzer Recording checkbox initialized to {AlternateRecordEnabled} from config.xml")

                Case "VehicleSpy"
                    CheckBox3.Text = "Enable VehicleSpy Recording"
                    CheckBox3.Visible = True
                    CheckBox3.Enabled = True
                    CheckBox3.Checked = AlternateRecordEnabled
                    HandleUserMessageLogging("GMRC", $"LoginForm: VehicleSpy Recording checkbox initialized to {AlternateRecordEnabled} from config.xml")

                Case Else
                    ' ✅ NEW: Handle invalid/unknown modes
                    CheckBox3.Text = $"Alternate Recording (Invalid Mode: {AlternateRecordingMode})"
                    CheckBox3.Checked = False
                    CheckBox3.Enabled = False
                    CheckBox3.Visible = True ' Keep visible to show error
                    HandleUserMessageLogging("GMRC", $"LoginForm: WARNING - Invalid AlternateRecordingMode '{AlternateRecordingMode}', disabling checkbox")
            End Select

            ' Re-attach handler
            AddHandler CheckBox3.CheckedChanged, AddressOf CheckBox3_CheckedChanged

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"InitializeAlternateRecordingCheckbox: {ex.Message}")
            Try
                CheckBox3.Text = "Alternate Recording (Error)"
                CheckBox3.Checked = False
                CheckBox3.Enabled = False
                AddHandler CheckBox3.CheckedChanged, AddressOf CheckBox3_CheckedChanged
            Catch innerEx As Exception
                HandleUserMessageLogging("GMRC", $"InitializeAlternateRecordingCheckbox: Critical failure: {innerEx.Message}")
            End Try
        End Try
    End Sub

    Private Sub InitializeLidarCheckbox()
        Try
            RemoveHandler CheckBox_LidarCapture.CheckedChanged, AddressOf CheckBox_LidarCapture_CheckedChanged

            If LidarDevices Is Nothing Then
                CheckBox_LidarCapture.Text = "LiDAR Capture (Configuration Error)"
                CheckBox_LidarCapture.Checked = False
                CheckBox_LidarCapture.Enabled = False
                HandleUserMessageLogging("GMRC", "LoginForm: LidarDevices is Nothing - disabling checkbox")
                AddHandler CheckBox_LidarCapture.CheckedChanged, AddressOf CheckBox_LidarCapture_CheckedChanged
                Return
            End If

            If LidarDevices.Count > 0 Then
                CheckBox_LidarCapture.Text = $"Enable LiDAR Capture ({LidarDevices.Count} device(s))"
                CheckBox_LidarCapture.Enabled = True
                CheckBox_LidarCapture.Checked = LidarCaptureEnabled
                HandleUserMessageLogging("GMRC", $"LoginForm: LiDAR checkbox initialized to {LidarCaptureEnabled} from config.xml ({LidarDevices.Count} device(s) configured)")
            Else
                CheckBox_LidarCapture.Text = "LiDAR Capture (Not Configured)"
                CheckBox_LidarCapture.Enabled = False
                CheckBox_LidarCapture.Checked = False
                HandleUserMessageLogging("GMRC", "LoginForm: LiDAR capture unavailable - no devices configured in collection")
            End If

            AddHandler CheckBox_LidarCapture.CheckedChanged, AddressOf CheckBox_LidarCapture_CheckedChanged

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"InitializeLidarCheckbox: {ex.Message}")
            Try
                CheckBox_LidarCapture.Text = "LiDAR Capture (Error)"
                CheckBox_LidarCapture.Enabled = False
                CheckBox_LidarCapture.Checked = False
                AddHandler CheckBox_LidarCapture.CheckedChanged, AddressOf CheckBox_LidarCapture_CheckedChanged
            Catch innerEx As Exception
                HandleUserMessageLogging("GMRC", $"InitializeLidarCheckbox: Critical failure in error handler: {innerEx.Message}")
            End Try
        End Try
    End Sub

    Private Sub Button43_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button43.Click
        'Exit Button
        Try
            HandleUserMessageLogging("GMRC", "Login Form Exit Button Pressed...")
            _isExitButtonClick = True
            Me.TopMost = False

            If ProjectDatabasePaths Is Nothing Then
                GmResidentClient?.Hide()
                GmResidentClient?.Close()
            Else
                Me.Hide()
            End If

            OnLoginScreen = False
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "LoginForm Exit Error: " & ex.Message)
        Finally
            _isExitButtonClick = False
        End Try
    End Sub

    Private Sub RadioButton1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButton1.CheckedChanged
        'FULL Signal Registration Mode
        If RadioButton1.Checked = True Then
            HandleUserMessageLogging("GMRC", "FULL Selection made...")
            If ClevirAdministrator = False Then
                HandleFullSigRegSelected()
            Else
                SignalRegistrationMode = "FULL"
                SaveSignalRegistrationMode = SignalRegistrationMode
            End If
        End If
    End Sub

    Private Sub RadioButton2_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton2.CheckedChanged
        'DISPLAYS Signal Registration Mode
        If RadioButton2.Checked = True Then
            HandleUserMessageLogging("GMRC", "DISPLAYS Selection made...")
            SignalRegistrationMode = "DISPLAYS"
            SaveSignalRegistrationMode = SignalRegistrationMode
        End If
    End Sub

    Private Sub RadioButton3_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton3.CheckedChanged
        'GO/NOGO Signal Registration Mode
        If RadioButton3.Checked = True Then
            HandleUserMessageLogging("GMRC", "GO/NOGO Selection made...")
            SignalRegistrationMode = "GO/NOGO"
            SaveSignalRegistrationMode = SignalRegistrationMode
        End If
    End Sub

    ' ═══════════════════════════════════════════════════════════════════
    ' Email Validation Helper (Optional but Recommended)
    ' ═══════════════════════════════════════════════════════════════════
    Private Function IsValidEmail(email As String) As Boolean
        Try
            Dim emailPattern As String = "^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$"
            Return System.Text.RegularExpressions.Regex.IsMatch(email, emailPattern)
        Catch
            Return False ' Assume invalid on any error
        End Try
    End Function

    ''' <summary>
    ''' Derives a login ID from an email address.
    ''' Extracts the username (before @), strips non-alphanumeric characters, and uppercases the result.
    ''' Falls back to DRVR000 if the email is blank, missing @, or yields an empty alphanumeric string.
    ''' Examples: john.smith@gm.com -> JOHNSMITH | x@gm.com -> X | invalid -> DRVR000
    ''' </summary>
    Private Function GetDriverIDFromEmail(email As String) As String
        Try
            If String.IsNullOrWhiteSpace(email) Then Return "DRVR000"
            Dim atIndex As Integer = email.IndexOf("@"c)
            If atIndex <= 0 Then Return "DRVR000"
            Dim username As String = email.Substring(0, atIndex)
            Dim alphaNumeric As String = New String(username.Where(Function(c) Char.IsLetterOrDigit(c)).ToArray())
            If String.IsNullOrEmpty(alphaNumeric) Then Return "DRVR000"
            Return alphaNumeric.ToUpperInvariant()
        Catch
            Return "DRVR000"
        End Try
    End Function

    Private Sub RadioButton4_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles RadioButton4.CheckedChanged
        'CREATE NEW FROM BLANK EXP (Admin only)
        If RadioButton4.Checked = True Then
            SignalRegistrationMode = "NEW FULL"
        End If
    End Sub

    Private Sub CheckBox1_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox1.CheckedChanged

        If CheckBox1.Checked = True Then
            HandleUserMessageLogging("GMRC", "Save Calibration Snapshots Selection made...")
        End If
    End Sub

    Private Sub CheckBox3_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox3.CheckedChanged
        If CheckBox3.Checked = True Then

            HandleUserMessageLogging("GMRC", "Enable Alternate Recording Mode Selection made...")
        Else
            HandleUserMessageLogging("GMRC", "Disable Alternate Recording Mode Selection made...")
        End If

    End Sub

    Private Sub CheckBox_LidarCapture_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox_LidarCapture.CheckedChanged
        If CheckBox_LidarCapture.Checked = True Then
            HandleUserMessageLogging("GMRC", "Enable Lidar Capture Selection made...")
        Else
            HandleUserMessageLogging("GMRC", "Disable Lidar Capture Selection made...")
            If LidarCaptureStarted Then
                StopLidarCapture()
            End If
        End If

        LidarCaptureEnabled = CheckBox_LidarCapture.Checked
    End Sub

    Private Sub LoginForm_Shown(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Shown
        OnLoginScreen = True
        OnVehicleScreen.SendToBack()

        Me.TopMost = True
        Me.BringToFront()
        Me.Activate()
        Me.Focus()

        If Not Me.Visible Then
            HandleUserMessageLogging("GMRC", "LoginForm_Shown: WARNING - Form.Visible = False, forcing Show()")
            Me.Show()
        End If

        HandleUserMessageLogging("GMRC", $"LoginForm_Shown: Visible={Me.Visible}, TopMost={Me.TopMost}, WindowState={Me.WindowState}")
    End Sub

    Private Sub LoginForm_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        HandleUserMessageLogging("GMRC", $"LoginForm_VisibleChanged: Visible={Me.Visible}")
    End Sub

    ''' <summary>
    ''' ✅ NEW: Apply signal registration mode from configuration after form initialization
    ''' This prevents premature form instantiation during config loading
    ''' </summary>
    Private Sub ApplySignalRegistrationModeFromConfig()
        Try
            ' Temporarily disable event handlers to prevent cascading events
            _isInitializing = True

            Select Case SignalRegistrationMode
                Case "FULL"
                    RadioButton1.Checked = True
                    HandleUserMessageLogging("GMRC", "LoginForm: Applied FULL signal registration mode from config")
                Case "DISPLAYS"
                    RadioButton2.Checked = True
                    HandleUserMessageLogging("GMRC", "LoginForm: Applied DISPLAYS signal registration mode from config")
                Case "GO/NOGO"
                    RadioButton3.Checked = True
                    HandleUserMessageLogging("GMRC", "LoginForm: Applied GO/NOGO signal registration mode from config")
                Case "NEW FULL"
                    RadioButton4.Checked = True
                    HandleUserMessageLogging("GMRC", "LoginForm: Applied NEW FULL signal registration mode from config")
                Case Else
                    ' Default to DISPLAYS if mode is invalid
                    RadioButton2.Checked = True
                    HandleUserMessageLogging("GMRC", $"LoginForm: Unknown mode '{SignalRegistrationMode}', defaulting to DISPLAYS")
            End Select

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ApplySignalRegistrationModeFromConfig: {ex.Message}")
            ' Fail-safe: default to DISPLAYS
            Try
                RadioButton2.Checked = True
            Catch
                ' Suppress secondary errors
            End Try
        Finally
            _isInitializing = False
        End Try
    End Sub

    Private Sub ToolTip1_Popup(sender As Object, e As PopupEventArgs)

    End Sub
End Class
