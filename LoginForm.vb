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

        _isInitializing = True

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

        ' ═══════════════════════════════════════════════════════════════
        ' Add ⚙️ Config Editor button below EXIT button
        ' ═══════════════════════════════════════════════════════════════
        Try
            Dim configEditorButton As New Button With {
                .Name = "ButtonConfigEditor",
                .Text = "⚙️ Config",
                .Size = New Size(Button43.Width, 37),
                .Font = New System.Drawing.Font("Segoe UI", 9.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CByte(0)),
                .Location = New Point(Button43.Left, Button43.Bottom + 8),
                .UseVisualStyleBackColor = True,
                .Cursor = Cursors.Hand
            }
            Me.Controls.Add(configEditorButton)
            configEditorButton.BringToFront()
            AddHandler configEditorButton.Click, AddressOf LoginForm_ConfigEditor_Click
            HandleUserMessageLogging("GMRC", "LoginForm: Config Editor button created")
        Catch ex As Exception
            HandleUserMessageLogging("LoginForm", $"Error creating Config Editor button: {ex.Message}")
        End Try

        ' ═══════════════════════════════════════════════════════════════
        ' System Configuration summary (read-only, populated from config.xml)
        ' ═══════════════════════════════════════════════════════════════
        Try
            Dim cfgBox As New GroupBox With {
                .Name = "GroupBox_ConfigSummary",
                .Text = "System Configuration",
                .Font = New System.Drawing.Font("Segoe UI", 9.0F, System.Drawing.FontStyle.Bold),
                .Location = New Point(12, 143),
                .Size = New Size(442, 180),
                .BackColor = System.Drawing.Color.White
            }
            Dim tlp As New TableLayoutPanel With {
                .Name = "Table_ConfigSummary",
                .ColumnCount = 2,
                .Dock = DockStyle.Fill,
                .Padding = New Padding(6, 4, 6, 4),
                .CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            }
            tlp.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 155))
            tlp.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
            PopulateConfigTable(tlp)
            cfgBox.Controls.Add(tlp)
            Me.Controls.Add(cfgBox)
            cfgBox.BringToFront()
            HandleUserMessageLogging("GMRC", "LoginForm: Config summary panel created")
        Catch ex As Exception
            HandleUserMessageLogging("LoginForm", $"Error creating config summary: {ex.Message}")
        End Try

        ' Label4 is superseded by the config summary panel
        Label4.Visible = False
        Me.ClientSize = New Size(470, 496)

        ' ✅ IMPORTANT: Set _isInitializing to False AFTER all initialization
        _isInitializing = False

        HandleUserMessageLogging("GMRC", "LoginForm_Load: Initialization complete, form ready for user interaction")
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

    Private Sub LoginForm_ConfigEditor_Click(sender As Object, e As EventArgs)
        Try
            HandleUserMessageLogging("GMRC", "LoginForm: Configuration Editor button pressed...")

            Dim editor As New ConfigurationEditorForm()
            If editor.ShowDialog(Me) = DialogResult.OK Then
                If MsgBox("Configuration updated. Reload settings now?", vbYesNo + vbQuestion) = vbYes Then
                    ReadConfigFile()

                    ' ── Lightweight validation on the freshly reloaded globals ─────
                    ' (VerifyConfigFiles is cached; this targets only what a user can
                    '  change in the editor and checks it before INCA initialization.)
                    Dim issues As New List(Of String)()
                    If String.IsNullOrWhiteSpace(INCADatabase) Then
                        issues.Add("INCA database path is empty")
                    End If
                    If String.IsNullOrWhiteSpace(INCAExperiment) Then
                        issues.Add("INCA experiment is not set")
                    End If
                    If String.IsNullOrWhiteSpace(INCAVariableFile) OrElse Not File.Exists(INCAVariableFile) Then
                        issues.Add($"Signal list file not found:{vbCrLf}  {INCAVariableFile}")
                    End If

                    ' Refresh the read-only config summary panel with reloaded values
                    RefreshConfigSummary()

                    If issues.Count > 0 Then
                        Dim issueList As String = String.Join(vbCrLf & "  • ", issues)
                        StatusNotifier.Warn(
                            $"Configuration reloaded but the following issues were found:{vbCrLf}" &
                            $"  • {issueList}{vbCrLf}{vbCrLf}" &
                            "Please correct these before logging in.",
                            "Configuration Warning")
                        HandleUserMessageLogging("GMRC", $"LoginForm: Config reload issues: {String.Join("; ", issues)}")
                    Else
                        StatusNotifier.Toast(
                            "Configuration updated. All changes take effect when INCA initializes after you log in.",
                            "Config", durationMs:=4000, ensureMainOnTop:=False)
                        HandleUserMessageLogging("GMRC", "LoginForm: Configuration reloaded - all files verified, changes take effect after login")
                    End If
                End If
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"LoginForm ConfigEditor launch failed: {ex.Message}", DisplayMsgBox)
        End Try
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

    Private Sub ToolTip1_Popup(sender As Object, e As PopupEventArgs)

    End Sub

    ' ═══════════════════════════════════════════════════════════════
    ' Config Summary Helpers
    ' ═══════════════════════════════════════════════════════════════

    ''' <summary>
    ''' Clears and repopulates a two-column TableLayoutPanel with current config values.
    ''' Col 0 (155px fixed) = label; Col 1 (fill) = value.
    ''' </summary>
    Private Sub PopulateConfigTable(tlp As TableLayoutPanel)
        tlp.SuspendLayout()
        tlp.Controls.Clear()
        tlp.RowStyles.Clear()
        tlp.RowCount = 0

        Dim rowFont As New System.Drawing.Font("Segoe UI", 8.5F)
        Dim rows As New List(Of KeyValuePair(Of String, String))()

        ' Vehicle
        Dim vn As String = If(String.IsNullOrWhiteSpace(VehicleNumber) OrElse
                              String.Equals(VehicleNumber, "UNDEFINED", StringComparison.OrdinalIgnoreCase),
                              "(not set)", VehicleNumber)
        rows.Add(New KeyValuePair(Of String, String)("Vehicle:", vn))

        ' Alternate Recorder
        Dim recText As String = If(AlternateRecordEnabled,
                                   If(String.IsNullOrWhiteSpace(AlternateRecordConfig), "(not set)", AlternateRecordConfig),
                                   "None")
        rows.Add(New KeyValuePair(Of String, String)("Alt. Recorder:", recText))

        ' Data Collection Path
        rows.Add(New KeyValuePair(Of String, String)("Data Collection Path:",
                 If(String.IsNullOrWhiteSpace(BaseDataCollectionPath), "(not set)", BaseDataCollectionPath)))

        ' Compression
        Dim compTypes As New List(Of String)()
        If CompressMF4 Then compTypes.Add("MF4")
        If CompressPCAP Then compTypes.Add("PCAP")
        If CompressASC Then compTypes.Add("ASC")
        If CompressVSB Then compTypes.Add("VSB")
        rows.Add(New KeyValuePair(Of String, String)("Compression:",
                 If(compTypes.Count > 0, String.Join(", ", compTypes), "None")))

        ' Camera(s)
        Dim enabledCameras As New List(Of CameraConfig)(ConfiguredCameras.Values.Where(Function(c) c.Enabled))
        If enabledCameras.Count = 0 Then
            rows.Add(New KeyValuePair(Of String, String)("Camera(s):", "None configured"))
        Else
            Dim camParts As New List(Of String)()
            For i As Integer = 0 To enabledCameras.Count - 1
                camParts.Add($"{i + 1}:{enabledCameras(i).Position}")
            Next
            rows.Add(New KeyValuePair(Of String, String)("Camera(s):",
                     $"{enabledCameras.Count}, {String.Join(", ", camParts)}"))
        End If

        ' LiDAR(s)
        If Not LidarCaptureEnabled OrElse LidarDevices.Count = 0 Then
            rows.Add(New KeyValuePair(Of String, String)("LiDAR(s):", "Not enabled"))
        Else
            Dim enabledLidars As New List(Of LidarDevice)(LidarDevices.Where(Function(d) d.Enabled))
            If enabledLidars.Count = 0 Then
                rows.Add(New KeyValuePair(Of String, String)("LiDAR(s):", "None enabled"))
            Else
                Dim lidarParts As New List(Of String)()
                For i As Integer = 0 To enabledLidars.Count - 1
                    Dim lbl As String = If(Not String.IsNullOrWhiteSpace(enabledLidars(i).Orientation),
                                           enabledLidars(i).Orientation, enabledLidars(i).DeviceId)
                    lidarParts.Add($"{i + 1}:{lbl}")
                Next
                rows.Add(New KeyValuePair(Of String, String)("LiDAR(s):",
                         $"{enabledLidars.Count}, {String.Join(", ", lidarParts)}"))
            End If
        End If

        ' Workspace
        rows.Add(New KeyValuePair(Of String, String)("Workspace:",
                 If(String.IsNullOrWhiteSpace(INCAWorkspace), "(not set)", INCAWorkspace)))

        ' Experiment
        rows.Add(New KeyValuePair(Of String, String)("Experiment:",
                 If(String.IsNullOrWhiteSpace(INCAExperiment), "(not set)", INCAExperiment)))

        ' Populate table rows
        tlp.RowCount = rows.Count
        For i As Integer = 0 To rows.Count - 1
            tlp.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            Dim keyLbl As New Label With {
                .Text = rows(i).Key,
                .Font = rowFont,
                .AutoSize = True,
                .Anchor = AnchorStyles.Left Or AnchorStyles.Top,
                .Margin = New Padding(0, 2, 4, 1)
            }
            Dim valLbl As New Label With {
                .Text = rows(i).Value,
                .Font = rowFont,
                .AutoSize = True,
                .Anchor = AnchorStyles.Left Or AnchorStyles.Top,
                .Margin = New Padding(0, 2, 0, 1)
            }
            tlp.Controls.Add(keyLbl, 0, i)
            tlp.Controls.Add(valLbl, 1, i)
        Next

        tlp.ResumeLayout()
    End Sub

    ''' <summary>
    ''' Refreshes the config table in-place after a config reload.
    ''' </summary>
    Private Sub RefreshConfigSummary()
        Try
            Dim cfgBox = TryCast(Me.Controls("GroupBox_ConfigSummary"), GroupBox)
            If cfgBox Is Nothing Then Return
            Dim tlp = TryCast(cfgBox.Controls("Table_ConfigSummary"), TableLayoutPanel)
            If tlp Is Nothing Then Return
            PopulateConfigTable(tlp)
        Catch ex As Exception
            HandleUserMessageLogging("LoginForm", $"RefreshConfigSummary error: {ex.Message}")
        End Try
    End Sub

End Class
