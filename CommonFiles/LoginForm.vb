Option Strict Off
Option Explicit On
Imports System.IO
Imports System.Drawing.Drawing2D

Public Class LoginForm
    'Streamlined login form displaying up to 5 user login buttons.
    'Allows signal registration mode selection, workspace changes, and LiDAR/recording configuration.

    Private _loginButton() As Button
    Private _isExitButtonClick As Boolean = False
    Private _isInitializing As Boolean = True
    Private _lastClickTime As DateTime = DateTime.MinValue

    ' ✨ NEW: Track active animation timers to prevent memory leaks
    Private _activeTimers As New Dictionary(Of Button, Timer)

    ' ✅ NEW: Track selected driver for LOGIN button workflow
    Private _selectedDriverButton As Button = Nothing
    Private _loginSubmitButton As Button = Nothing ' Reference to the LOGIN button

    ''' <summary>
    ''' ✅ REFACTORED: Driver button click now SELECTS driver (doesn't submit)
    ''' </summary>
    Private Sub LoginButton_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        ' ✅ GUARD: Prevent premature action during initialization
        If _isInitializing Then
            HandleUserMessageLogging("GMRC", "LoginForm: Ignoring LoginButton click during initialization")
            Return
        End If

        ' ✅ DEBOUNCE: Prevent rapid double-clicks (critical for touch screens)
        Dim now As DateTime = DateTime.Now
        If (now - _lastClickTime).TotalMilliseconds < 500 Then
            HandleUserMessageLogging("GMRC", "LoginForm: Ignoring rapid click (debounce)")
            Return
        End If
        _lastClickTime = now

        Dim clickedButton As Button = TryCast(sender, Button)
        If clickedButton Is Nothing Then Return

        ' ✅ NEW BEHAVIOR: Select driver button (don't submit)
        SelectDriverButton(clickedButton)
    End Sub

    ''' <summary>
    ''' ✅ NEW: Handles driver button selection with visual feedback
    ''' </summary>
    Private Sub SelectDriverButton(selectedButton As Button)
        Try
            ' Deselect previously selected button (if any)
            If _selectedDriverButton IsNot Nothing AndAlso _selectedDriverButton IsNot selectedButton Then
                ResetDriverButtonStyle(_selectedDriverButton)
            End If

            ' Select the new button
            _selectedDriverButton = selectedButton
            ApplySelectedDriverStyle(selectedButton)

            ' Enable the LOGIN button now that a driver is selected
            If _loginSubmitButton IsNot Nothing Then
                _loginSubmitButton.Enabled = True
            End If

            HandleUserMessageLogging("GMRC", $"LoginForm: Driver '{selectedButton.Text}' selected (ready to login)")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"SelectDriverButton: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Apply visual styling to show button is selected
    ''' </summary>
    Private Sub ApplySelectedDriverStyle(btn As Button)
        Try
            btn.Tag = "selected" ' Mark as selected
            btn.BackColor = Color.FromArgb(100, 149, 237) ' CornflowerBlue
            btn.ForeColor = Color.White
            btn.Font = New Font("Segoe UI", 12, FontStyle.Bold)

            ' Add prominent border
            btn.FlatAppearance.BorderColor = Color.DodgerBlue
            btn.FlatAppearance.BorderSize = 4

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ApplySelectedDriverStyle: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Reset button to normal styling
    ''' </summary>
    Private Sub ResetDriverButtonStyle(btn As Button)
        Try
            btn.Tag = "normal" ' Mark as normal
            btn.BackColor = Color.FromArgb(240, 248, 255) ' AliceBlue
            btn.ForeColor = Color.FromArgb(25, 25, 112) ' MidnightBlue
            btn.Font = New Font("Segoe UI", 12, FontStyle.Bold)

            ' Reset border
            btn.FlatAppearance.BorderColor = Color.DarkBlue
            btn.FlatAppearance.BorderSize = 2

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ResetDriverButtonStyle: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' ✅ ENHANCED: Handle LOGIN button click (form submission) with required field validation
    ''' </summary>
    Private Sub LoginSubmit_Click(sender As Object, e As EventArgs)
        Try
            ' ═══════════════════════════════════════════════════════════════════
            ' GUARD 1: Ensure driver is selected
            ' ═══════════════════════════════════════════════════════════════════
            If _selectedDriverButton Is Nothing Then
                StatusNotifier.Warn("Please select a driver before logging in", "Driver Required")
                HandleUserMessageLogging("GMRC", "LoginForm: LOGIN clicked with no driver selected")
                Return
            End If

            ' ═══════════════════════════════════════════════════════════════════
            ' GUARD 2: Validate Required Session Metadata Fields
            ' ═══════════════════════════════════════════════════════════════════
            Dim missingFields As New List(Of String)()

            ' Validate Group field
            If String.IsNullOrWhiteSpace(ComboBox_Group.Text) Then
                missingFields.Add("Group")
            End If

            ' Validate Procedure field
            If String.IsNullOrWhiteSpace(ComboBox_Procedure.Text) Then
                missingFields.Add("Procedure")
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
                If missingFields.Contains("Group") Then
                    ComboBox_Group.Focus()
                ElseIf missingFields.Contains("Procedure") Then
                    ComboBox_Procedure.Focus()
                ElseIf missingFields.Contains("Email") Then
                    TextBox_Email.Focus()
                End If

                Return ' Stay on login form
            End If

            ' Capture selected driver name
            SaveLoginID = _selectedDriverButton.Text
            DebugMode = DebugKey

            ' ═══════════════════════════════════════════════════════════════════
            ' Capture Session Metadata (All Fields Now Required)
            ' ═══════════════════════════════════════════════════════════════════
            Try
                ' Trim and capture required fields
                SaveGroupName = ComboBox_Group.Text.Trim()
                SaveProcedureName = ComboBox_Procedure.Text.Trim()
                SaveEmailAddress = TextBox_Email.Text.Trim()

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
                    $"Session metadata captured - Driver: [{SaveLoginID}], Group: [{SaveGroupName}], Procedure: [{SaveProcedureName}], Email: [{SaveEmailAddress}]")

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

    Private Sub ReadUserIdList()
        'Populates up to 5 dynamic login buttons from UserIDList with modern styling

        Dim users As ArrayList
        Dim buttonIndex As Integer = 0

        Try
            HandleUserMessageLogging("GMRC", "ReadUserIDList: Reading UserIDList File...")

            ' ✅ CRITICAL: Clear Panel1 of any existing buttons from previous loads
            If Panel1 IsNot Nothing Then
                For Each ctrl As Control In Panel1.Controls.OfType(Of Button)().ToList()
                    ' ✅ Stop and dispose of any active animation timers
                    If _activeTimers.ContainsKey(ctrl) Then
                        _activeTimers(ctrl).Stop()
                        _activeTimers(ctrl).Dispose()
                        _activeTimers.Remove(ctrl)
                    End If

                    ' Remove hover event handlers before disposal
                    RemoveHandler ctrl.MouseEnter, AddressOf LoginButton_MouseEnter
                    RemoveHandler ctrl.MouseLeave, AddressOf LoginButton_MouseLeave
                    Panel1.Controls.Remove(ctrl)
                    ctrl.Dispose()
                Next
            End If

            users = LoginIDNameAndFreqAL

            If users IsNot Nothing AndAlso users.Count > 0 Then
                HandleUserMessageLogging("GMRC", $"ReadUserIDList: Found {users.Count} users in list")

                ' ✅ Cap at 5 users (check up to 10 entries to find 5 non-DEMO)
                For x As Integer = 0 To Math.Min(users.Count - 1, 9)
                    Dim userName As String = users(x).ToString()

                    ' ✅ Skip demo users
                    If InStr(UCase(userName), "DEMO") > 0 Then
                        HandleUserMessageLogging("GMRC", $"ReadUserIDList: Skipping DEMO user: {userName}")
                        Continue For
                    End If

                    ' ✅ Stop after 6 valid users
                    If buttonIndex >= 6 Then
                        HandleUserMessageLogging("GMRC", $"ReadUserIDList: Reached maximum of 6 users")
                        Exit For
                    End If

                    ' ✅ Extract just the username (remove frequency prefix if present)
                    Dim displayName As String = If(Len(userName) > 7,
                                               Mid(Trim(userName), 8, Len(userName) - 7),
                                               Trim(userName))

                    ' ✅ Create button and add to Panel1 with enhanced styling
                    ReDim Preserve _loginButton(buttonIndex)
                    _loginButton(buttonIndex) = New Button With {
                       .Parent = Panel1,
                       .BackColor = Color.DarkBlue,
                       .ForeColor = Color.FromArgb(25, 25, 112), ' MidnightBlue - professional text
                       .UseVisualStyleBackColor = False,
                       .FlatStyle = FlatStyle.System,
                       .Size = New System.Drawing.Size(150, 70),
                       .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                       .Text = displayName,
                       .Cursor = Cursors.Hand,
                       .TabStop = True,
                       .TabIndex = buttonIndex,
                       .Tag = "normal" ' Track state for hover effects
                   }

                    ' ✅ Enhanced flat appearance with smooth transitions
                    With _loginButton(buttonIndex).FlatAppearance
                        .BorderColor = Color.DarkBlue
                        .BorderSize = 2
                        .MouseDownBackColor = Color.LightGray
                        .MouseOverBackColor = Color.LightBlue
                    End With

                    ' ✅ Position buttons vertically with consistent spacing
                    _loginButton(buttonIndex).Left = 10
                    _loginButton(buttonIndex).Top = 10 + ((70 + 10) * buttonIndex)
                    _loginButton(buttonIndex).Visible = True

                    ' ✅ Wire up event handlers
                    AddHandler _loginButton(buttonIndex).Click, AddressOf Me.LoginButton_Click
                    AddHandler _loginButton(buttonIndex).MouseEnter, AddressOf Me.LoginButton_MouseEnter
                    AddHandler _loginButton(buttonIndex).MouseLeave, AddressOf Me.LoginButton_MouseLeave

                    HandleUserMessageLogging("GMRC", $"ReadUserIDList: Created button {buttonIndex + 1} for '{displayName}'")

                    buttonIndex += 1
                Next

                If buttonIndex = 0 Then
                    ' ✅ No users found - show helpful message
                    Dim noUsersLabel As New Label With {
                       .Parent = Panel1,
                       .Text = "No users configured" & vbCrLf & vbCrLf & "Contact administrator",
                       .Font = New Font("Segoe UI", 10, FontStyle.Italic),
                       .ForeColor = Color.DimGray,
                       .TextAlign = ContentAlignment.MiddleCenter,
                       .Dock = DockStyle.Fill
                   }
                    Panel1.Controls.Add(noUsersLabel)
                    HandleUserMessageLogging("GMRC", "ReadUserIDList: WARNING - No valid users found (all were DEMO)")
                Else
                    HandleUserMessageLogging("GMRC", $"ReadUserIDList: Successfully created {buttonIndex} login button(s)")
                End If
            Else
                HandleUserMessageLogging("GMRC", "ReadUserIDList: WARNING - LoginIDNameAndFreqAL is Nothing or empty")
            End If

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", "ReadUserIDList: " & ex.Message, DisplayMsgBox)
        End Try
    End Sub

    ''' <summary>
    ''' ✨ ENHANCED: Animated color fade when mouse enters a login button
    ''' ✅ UPDATED: Don't animate if button is selected
    ''' </summary>
    Private Sub LoginButton_MouseEnter(sender As Object, e As EventArgs)
        Dim btn As Button = TryCast(sender, Button)

        ' ✅ Don't animate if button is selected or not in normal state
        If btn Is Nothing OrElse btn.Tag?.ToString() <> "normal" Then Return

        ' ✅ Stop any existing animation for this button
        If _activeTimers.ContainsKey(btn) Then
            _activeTimers(btn).Stop()
            _activeTimers(btn).Dispose()
            _activeTimers.Remove(btn)
        End If

        ' ✅ Target colors for hover state
        Dim targetColor As Color = Color.FromArgb(176, 224, 230) ' PowderBlue
        Dim targetForeColor As Color = Color.FromArgb(0, 0, 139) ' DarkBlue

        ' ✅ Store original colors for smooth transition
        Dim startColor As Color = btn.BackColor
        Dim startForeColor As Color = btn.ForeColor

        ' ✅ Create animated fade timer (20ms interval = 50 FPS smooth animation)
        Dim fadeTimer As New Timer With {.Interval = 20}
        _activeTimers(btn) = fadeTimer

        AddHandler fadeTimer.Tick, Sub()
                                       Try
                                           ' ✅ Gradually transition BackColor over ~200ms (10 ticks * 20ms)
                                           If btn.BackColor <> targetColor Then
                                               Dim currentR As Integer = btn.BackColor.R
                                               Dim currentG As Integer = btn.BackColor.G
                                               Dim currentB As Integer = btn.BackColor.B

                                               ' ✅ Calculate new RGB values (move 1/10th of distance per tick)
                                               Dim newR As Integer = Math.Min(currentR + CInt((targetColor.R - currentR) / 3), targetColor.R)
                                               Dim newG As Integer = Math.Min(currentG + CInt((targetColor.G - currentG) / 3), targetColor.G)
                                               Dim newB As Integer = Math.Min(currentB + CInt((targetColor.B - currentB) / 3), targetColor.B)

                                               btn.BackColor = Color.FromArgb(newR, newG, newB)

                                               ' ✅ Also animate foreground color
                                               Dim currentFR As Integer = btn.ForeColor.R
                                               Dim currentFG As Integer = btn.ForeColor.G
                                               Dim currentFB As Integer = btn.ForeColor.B

                                               Dim newFR As Integer = Math.Max(currentFR - CInt((currentFR - targetForeColor.R) / 3), targetForeColor.R)
                                               Dim newFG As Integer = Math.Max(currentFG - CInt((currentFG - targetForeColor.G) / 3), targetForeColor.G)
                                               Dim newFB As Integer = Math.Max(currentFB - CInt((currentFB - targetForeColor.B) / 3), targetForeColor.B)

                                               btn.ForeColor = Color.FromArgb(newFR, newFG, newFB)
                                           Else
                                               ' ✅ Animation complete - apply final styling
                                               btn.Font = New Font(btn.Font, FontStyle.Bold Or FontStyle.Underline)
                                               btn.Size = New Size(155, 72) ' Slightly larger
                                               btn.Left = 8

                                               ' ✅ Stop and cleanup timer
                                               fadeTimer.Stop()
                                               If _activeTimers.ContainsKey(btn) Then
                                                   _activeTimers.Remove(btn)
                                               End If
                                               fadeTimer.Dispose()

                                               HandleUserMessageLogging("GMRC", $"LoginButton_MouseEnter: Animation complete for '{btn.Text}'")
                                           End If

                                       Catch ex As Exception
                                           ' ✅ Fail gracefully if button disposed during animation
                                           fadeTimer.Stop()
                                           If _activeTimers.ContainsKey(btn) Then
                                               _activeTimers.Remove(btn)
                                           End If
                                           fadeTimer.Dispose()
                                       End Try
                                   End Sub

        fadeTimer.Start()
        HandleUserMessageLogging("GMRC", $"LoginButton_MouseEnter: Started animated hover for '{btn.Text}'")
    End Sub

    ''' <summary>
    ''' ✨ ENHANCED: Animated color fade when mouse leaves
    ''' ✅ UPDATED: Don't animate if button is selected
    ''' </summary>
    Private Sub LoginButton_MouseLeave(sender As Object, e As EventArgs)
        Dim btn As Button = TryCast(sender, Button)

        ' ✅ Don't animate if button is selected or not in normal state
        If btn Is Nothing OrElse btn.Tag?.ToString() <> "normal" Then Return

        ' ✅ Stop any existing animation for this button
        If _activeTimers.ContainsKey(btn) Then
            _activeTimers(btn).Stop()
            _activeTimers(btn).Dispose()
            _activeTimers.Remove(btn)
        End If

        ' ✅ Target colors for normal state
        Dim targetColor As Color = Color.FromArgb(240, 248, 255) ' AliceBlue
        Dim targetForeColor As Color = Color.FromArgb(25, 25, 112) ' MidnightBlue

        ' ✅ Remove underline immediately (not animated)
        btn.Font = New Font("Segoe UI", 12, FontStyle.Bold)
        btn.Size = New Size(150, 70)
        btn.Left = 10

        ' ✅ Create animated fade-out timer
        Dim fadeTimer As New Timer With {.Interval = 20}
        _activeTimers(btn) = fadeTimer

        AddHandler fadeTimer.Tick, Sub()
                                       Try
                                           ' ✅ Gradually transition back to normal colors
                                           If btn.BackColor <> targetColor Then
                                               Dim currentR As Integer = btn.BackColor.R
                                               Dim currentG As Integer = btn.BackColor.G
                                               Dim currentB As Integer = btn.BackColor.B

                                               Dim newR As Integer = Math.Max(currentR - CInt((currentR - targetColor.R) / 3), targetColor.R)
                                               Dim newG As Integer = Math.Max(currentG - CInt((currentG - targetColor.G) / 3), targetColor.G)
                                               Dim newB As Integer = Math.Max(currentB - CInt((currentB - targetColor.B) / 3), targetColor.B)

                                               btn.BackColor = Color.FromArgb(newR, newG, newB)

                                               ' ✅ Also animate foreground color
                                               Dim currentFR As Integer = btn.ForeColor.R
                                               Dim currentFG As Integer = btn.ForeColor.G
                                               Dim currentFB As Integer = btn.ForeColor.B

                                               Dim newFR As Integer = Math.Min(currentFR + CInt((targetForeColor.R - currentFR) / 3), targetForeColor.R)
                                               Dim newFG As Integer = Math.Min(currentFG + CInt((targetForeColor.G - currentFG) / 3), targetForeColor.G)
                                               Dim newFB As Integer = Math.Min(currentFB + CInt((targetForeColor.B - currentFB) / 3), targetForeColor.B)

                                               btn.ForeColor = Color.FromArgb(newFR, newFG, newFB)
                                           Else
                                               ' ✅ Animation complete
                                               fadeTimer.Stop()
                                               If _activeTimers.ContainsKey(btn) Then
                                                   _activeTimers.Remove(btn)
                                               End If
                                               fadeTimer.Dispose()

                                               HandleUserMessageLogging("GMRC", $"LoginButton_MouseLeave: Animation complete for '{btn.Text}'")
                                           End If

                                       Catch ex As Exception
                                           fadeTimer.Stop()
                                           If _activeTimers.ContainsKey(btn) Then
                                               _activeTimers.Remove(btn)
                                           End If
                                           fadeTimer.Dispose()
                                       End Try
                                   End Sub

        fadeTimer.Start()
        HandleUserMessageLogging("GMRC", $"LoginButton_MouseLeave: Started animated fade-out for '{btn.Text}'")
    End Sub

    ''' <summary>
    ''' ✨ NEW: Paint gradient background on Panel1
    ''' </summary>
    Private Sub Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Panel1.Paint
        Try
            Dim panel As Panel = TryCast(sender, Panel)
            If panel Is Nothing Then Return

            ' ✅ Create subtle vertical gradient (top-to-bottom)
            Using brush As New LinearGradientBrush(
                panel.ClientRectangle,
                Color.FromArgb(245, 245, 250), ' Light lavender top
                Color.FromArgb(230, 240, 250), ' Light blue bottom
                LinearGradientMode.Vertical)

                e.Graphics.FillRectangle(brush, panel.ClientRectangle)
            End Using

            ' ✅ Optional: Add subtle border around panel
            Using pen As New Pen(Color.SteelBlue, 1)
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1)
            End Using

        Catch ex As Exception
            ' Fail silently - gradient is cosmetic
            HandleUserMessageLogging("GMRC", $"Panel1_Paint: {ex.Message}")
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

        ' ✅ Cleanup all animation timers before closing
        For Each kvp In _activeTimers.ToList()
            kvp.Value.Stop()
            kvp.Value.Dispose()
        Next
        _activeTimers.Clear()

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
        ' ✅ Enable double-buffering on Panel1 to prevent flicker during gradient paint
        If Panel1 IsNot Nothing Then
            Panel1.GetType().GetProperty("DoubleBuffered",
            Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)?.SetValue(Panel1, True, Nothing)
        End If

        ReadUserIdList()

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
                .Enabled = False, ' ✅ Start disabled (no driver selected yet)
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
        ' (called earlier in LoginForm_Load, after ReadUserIdList)
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