Imports System.Drawing
Imports System.Windows.Forms
Imports System.Collections.Generic

Public Module StatusNotifier
    ' Toast severity for styling
    Public Enum ToastKind
        Info
        Warning
        [Error]
        Success
    End Enum

    ' Track open toasts to stack them
    Private ReadOnly _openToasts As New List(Of Form)
    Private ReadOnly _toastMargin As Integer = 12

    ' NEW: track sticky toasts by id
    Private NotInheritable Class ToastState
        Public Property Form As ToastForm
        Public Property EnsureMainOnTop As Boolean
    End Class
    Private ReadOnly _stickyToasts As New Dictionary(Of Guid, ToastState)

    ' Blocking prompts (decisions)
    Public Function Confirm(message As String, Optional title As String = "CLEVIR") As Boolean
        Return ShowMessage(message, title, MessageBoxIcon.Question, MessageBoxButtons.YesNo) = DialogResult.Yes
    End Function

    Public Sub Info(message As String, Optional title As String = "CLEVIR")
        ShowMessage(message, title, MessageBoxIcon.Information, MessageBoxButtons.OK)
    End Sub

    Public Sub Warn(message As String, Optional title As String = "CLEVIR")
        ShowMessage(message, title, MessageBoxIcon.Warning, MessageBoxButtons.OK)
    End Sub

    Public Sub [Error](message As String, Optional title As String = "CLEVIR")
        ShowMessage(message, title, MessageBoxIcon.[Error], MessageBoxButtons.OK)
    End Sub

    ' ════════════════════════════════════════════════════════════════════════
    ' ✅ NEW: Non-blocking error toast (shorthand for Toast with Error kind)
    ' ════════════════════════════════════════════════════════════════════════
    ''' <summary>
    ''' Shows a non-blocking error toast notification with red styling.
    ''' </summary>
    ''' <param name="message">Error message to display</param>
    ''' <param name="title">Optional title (default: empty string)</param>
    ''' <param name="durationMs">Auto-dismiss duration in milliseconds (default: 5000)</param>
    ''' <param name="ensureMainOnTop">Restore main form focus after toast closes (default: True)</param>
    Public Sub ToastError(message As String, Optional title As String = "", Optional durationMs As Integer = 5000, Optional ensureMainOnTop As Boolean = True)
        RunOnUi(Sub() ShowToastInternal(message, title, durationMs, ToastKind.Error, ensureMainOnTop))
    End Sub

    ' Non-blocking status (toast) – keep existing API; ensure OnVehicleScreen restored after by default
    Public Sub Toast(message As String, Optional title As String = "", Optional durationMs As Integer = 5000, Optional ensureMainOnTop As Boolean = True)
        RunOnUi(Sub() ShowToastInternal(message, title, durationMs, ToastKind.Info, ensureMainOnTop))
    End Sub

    ' Overload with severity styling
    Public Sub Toast(message As String, kind As ToastKind, Optional title As String = "", Optional durationMs As Integer = 5000, Optional ensureMainOnTop As Boolean = True)
        RunOnUi(Sub() ShowToastInternal(message, title, durationMs, kind, ensureMainOnTop))
    End Sub

    ' NEW: sticky toast API (no auto-fade) — returns a Guid to dismiss later
    Public Function ToastSticky(message As String,
                                Optional title As String = "",
                                Optional kind As ToastKind = ToastKind.Info,
                                Optional ensureMainOnTop As Boolean = True) As Guid
        Dim id = Guid.NewGuid()
        RunOnUi(Sub() ShowToastStickyInternal(id, message, title, kind, ensureMainOnTop))
        Return id
    End Function

    ' NEW: dismiss sticky toast, optionally with fade
    Public Sub ToastDismiss(id As Guid, Optional fade As Boolean = True)
        RunOnUi(
            Sub()
                If Not _stickyToasts.ContainsKey(id) Then Return
                Dim state = _stickyToasts(id)
                _stickyToasts.Remove(id)
                Dim toast = state.Form
                If toast Is Nothing OrElse toast.IsDisposed Then Return

                Dim keepMainOnTop = state.EnsureMainOnTop
                If fade Then
                    toast.BeginFadeOutAndClose(Sub() SafeCloseToast(toast, keepMainOnTop))
                Else
                    SafeCloseToast(toast, keepMainOnTop)
                End If
            End Sub)
    End Sub

    ' Core plumbing
    Private Function ShowMessage(message As String, title As String,
                                 icon As MessageBoxIcon, buttons As MessageBoxButtons) As DialogResult
        Dim result As DialogResult = DialogResult.None
        RunOnUi(Sub()
                    Dim owner = GetOwner()
                    result = If(owner Is Nothing OrElse owner.IsDisposed OrElse Not owner.IsHandleCreated,
                                MessageBox.Show(message, title, buttons, icon),
                                MessageBox.Show(owner, message, title, buttons, icon))
                End Sub)
        Return result
    End Function

    Private Sub RunOnUi(action As Action)
        Dim owner = GetOwner()
        If owner IsNot Nothing AndAlso owner.IsHandleCreated AndAlso owner.InvokeRequired Then
            Try
                owner.BeginInvoke(action)
            Catch
                ' Owner might be disposed between selection and invoke; run directly as a last resort
                Try
                    action()
                Catch
                End Try
            End Try
        Else
            ' If no valid owner, execute directly (MessageBox.Show is thread-safe; toast creates its own UI loop).
            action()
        End If
    End Sub

    ' NEW: detect active modal
    Private Function HasActiveModalForm() As Boolean
        If Application.OpenForms Is Nothing Then Return False
        For Each f As Form In Application.OpenForms
            If f IsNot Nothing AndAlso Not f.IsDisposed AndAlso f.IsHandleCreated AndAlso f.Visible AndAlso f.Modal Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Function GetOwner() As Form
        ' Prefer any active modal dialog as owner to keep toasts scoped without reactivating main windows
        If Application.OpenForms IsNot Nothing AndAlso Application.OpenForms.Count > 0 Then
            For Each f As Form In Application.OpenForms
                If f IsNot Nothing AndAlso Not f.IsDisposed AndAlso f.IsHandleCreated AndAlso f.Modal Then
                    Return f
                End If
            Next
        End If

        If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed AndAlso OnVehicleScreen.IsHandleCreated Then Return OnVehicleScreen
        If GmResidentClient IsNot Nothing AndAlso Not GmResidentClient.IsDisposed AndAlso GmResidentClient.IsHandleCreated Then Return GmResidentClient
        If Application.OpenForms IsNot Nothing AndAlso Application.OpenForms.Count > 0 Then
            For Each f As Form In Application.OpenForms
                If f IsNot Nothing AndAlso Not f.IsDisposed AndAlso f.IsHandleCreated Then
                    Return f
                End If
            Next
        End If
        Return Nothing
    End Function

    ' Double-buffered, non-activating toast form (does not steal focus)
    Private NotInheritable Class ToastForm
        Inherits Form

        Private ReadOnly _lifetimeTimer As Timer
        Private ReadOnly _fadeTimer As Timer

        Public Sub New(durationMs As Integer)
            Me.FormBorderStyle = FormBorderStyle.None
            Me.StartPosition = FormStartPosition.Manual
            Me.ShowInTaskbar = False
            Me.TopMost = True
            Me.DoubleBuffered = True
            Me.Opacity = 0.95

            ' Root timers to this form's lifetime
            _lifetimeTimer = New Timer With {.Interval = Math.Max(500, durationMs)}
            _fadeTimer = New Timer With {.Interval = 30}
            AddHandler Me.FormClosed, Sub()
                                          Try
                                              _lifetimeTimer.Stop() : _fadeTimer.Stop()
                                              _lifetimeTimer.Dispose() : _fadeTimer.Dispose()
                                          Catch
                                          End Try
                                      End Sub
        End Sub

        Public Sub StartLifetime(onLifetimeElapsed As Action, onFadeTick As Action)
            AddHandler _lifetimeTimer.Tick,
                Sub()
                    _lifetimeTimer.Stop()
                    _fadeTimer.Start()
                    onLifetimeElapsed?.Invoke()
                End Sub
            AddHandler _fadeTimer.Tick, Sub() onFadeTick?.Invoke()
            _lifetimeTimer.Start()
        End Sub

        ' NEW: start fade and close on demand (for sticky toasts)
        Public Sub BeginFadeOutAndClose(onDone As Action)
            Try
                AddHandler _fadeTimer.Tick,
                    Sub()
                        If Me.IsDisposed Then Return
                        Me.Opacity -= 0.06
                        If Me.Opacity <= 0 Then
                            _fadeTimer.Stop()
                            onDone?.Invoke()
                        End If
                    End Sub
                _fadeTimer.Start()
            Catch
            End Try
        End Sub

        Protected Overrides ReadOnly Property CreateParams As CreateParams
            Get
                Dim cp = MyBase.CreateParams
                ' WS_EX_TOOLWINDOW (0x80), WS_EX_NOACTIVATE (0x08000000), WS_EX_COMPOSITED (0x02000000)
                cp.ExStyle = cp.ExStyle Or &H80 Or &H8000000 Or &H2000000
                Return cp
            End Get
        End Property

        Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

    ' EXISTING auto-fading toast
    Private Sub ShowToastInternal(message As String, title As String, durationMs As Integer, kind As ToastKind, ensureMainOnTop As Boolean)
        ' ════════════════════════════════════════════════════════════════════════
        ' ✅ FIX: Don't use owner if LoginForm/modal is active to avoid interference
        ' ════════════════════════════════════════════════════════════════════════
        Dim owner = GetOwner()
        Dim loginActive As Boolean =
            (LoginForm IsNot Nothing AndAlso Not LoginForm.IsDisposed AndAlso
             LoginForm.IsHandleCreated AndAlso LoginForm.Visible)
        Dim modalActive As Boolean = HasActiveModalForm()

        ' If modal/login active, show without owner to avoid focus issues
        If loginActive OrElse modalActive Then
            owner = Nothing
        End If

        Dim colors = GetToastColors(kind)
        Dim bg As Color = colors.Back
        Dim fg As Color = colors.Fore

        Dim toast As New ToastForm(durationMs) With {
            .BackColor = bg,
            .Size = New Size(420, If(String.IsNullOrEmpty(title), 80, 100))
        }

        ' Click-to-dismiss (form or label)
        AddHandler toast.Click, Sub() SafeCloseToast(toast, ensureMainOnTop)

        Dim lbl As New Label With {
            .ForeColor = fg,
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Text = If(String.IsNullOrEmpty(title), message, title & Environment.NewLine & message),
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Regular),
            .Padding = New Padding(12),
            .Dock = DockStyle.Fill
        }
        AddHandler lbl.Click, Sub() SafeCloseToast(toast, ensureMainOnTop)
        toast.Controls.Add(lbl)

        ' Optional severity stripe
        Dim stripe As New Panel With {
            .BackColor = GetToastStripeColor(kind),
            .Dock = DockStyle.Left,
            .Width = 5
        }
        toast.Controls.Add(stripe)
        stripe.BringToFront()

        ' Positioning: use owner's screen if available; otherwise, the screen at cursor
        Dim screenArea As Rectangle
        If owner IsNot Nothing AndAlso Not owner.IsDisposed AndAlso owner.IsHandleCreated Then
            screenArea = Screen.FromControl(owner).WorkingArea
        Else
            screenArea = Screen.FromPoint(Cursor.Position).WorkingArea
        End If

        ' Stack toasts upwards from bottom-right
        Dim yOffset As Integer = _toastMargin
        For i = _openToasts.Count - 1 To 0 Step -1
            Dim open = _openToasts(i)
            If open Is Nothing OrElse open.IsDisposed Then
                _openToasts.RemoveAt(i)
            Else
                yOffset += open.Height + _toastMargin
            End If
        Next
        toast.Location = New Point(screenArea.Right - toast.Width - _toastMargin, screenArea.Bottom - toast.Height - yOffset)

        ' Track toast and auto-dismiss with fade out
        _openToasts.Add(toast)
        AddHandler toast.FormClosed, Sub() _openToasts.Remove(toast)

        ' Start timers rooted to toast
        toast.StartLifetime(
            onLifetimeElapsed:=Sub() ' no-op here; fade timer starts in StartLifetime
                               End Sub,
            onFadeTick:=Sub()
                            If toast.IsDisposed Then Return
                            toast.Opacity -= 0.06
                            If toast.Opacity <= 0 Then
                                SafeCloseToast(toast, ensureMainOnTop)
                            End If
                        End Sub)

        Try
            ' ✅ FIX: Show without owner to ensure visibility regardless of parent state
            ' TopMost property ensures toast appears on top anyway
            toast.Show()  ' Always show without owner for maximum visibility
            toast.Refresh()
        Catch
            SafeCloseToast(toast, ensureMainOnTop)
        End Try
    End Sub

    ' NEW: sticky (no auto-fade) toast creation
    Private Sub ShowToastStickyInternal(id As Guid, message As String, title As String, kind As ToastKind, ensureMainOnTop As Boolean)
        Dim owner = GetOwner()

        ' If a modal dialog (e.g., LoginForm.ShowDialog) is active, show modeless and don't restore focus
        Dim loginActive As Boolean =
            (LoginForm IsNot Nothing AndAlso Not LoginForm.IsDisposed AndAlso LoginForm.IsHandleCreated AndAlso LoginForm.Visible)
        Dim modalActive As Boolean = HasActiveModalForm()

        Dim localEnsureMainOnTop As Boolean = Not loginActive And Not modalActive AndAlso ensureMainOnTop
        If loginActive Or modalActive Then
            owner = Nothing
        End If

        Dim colors = GetToastColors(kind)
        Dim bg As Color = colors.Back
        Dim fg As Color = colors.Fore

        ' Use a positive dummy duration to initialize timers, but don't start lifetime
        Dim toast As New ToastForm(2000) With {
            .BackColor = bg,
            .Size = New Size(420, If(String.IsNullOrEmpty(title), 80, 100))
        }

        ' Click-to-dismiss -> dismiss sticky id
        AddHandler toast.Click, Sub() ToastDismiss(id, fade:=True)

        Dim lbl As New Label With {
            .ForeColor = fg,
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Text = If(String.IsNullOrEmpty(title), message, title & Environment.NewLine & message),
            .Font = New Font("Segoe UI", 9.0F, FontStyle.Regular),
            .Padding = New Padding(12),
            .Dock = DockStyle.Fill
        }
        AddHandler lbl.Click, Sub() ToastDismiss(id, fade:=True)
        toast.Controls.Add(lbl)

        Dim stripe As New Panel With {
            .BackColor = GetToastStripeColor(kind),
            .Dock = DockStyle.Left,
            .Width = 5
        }
        toast.Controls.Add(stripe)
        stripe.BringToFront()

        ' Position on same screen as owner (or cursor)
        Dim screenArea As Rectangle
        If owner IsNot Nothing AndAlso Not owner.IsDisposed AndAlso owner.IsHandleCreated Then
            screenArea = Screen.FromControl(owner).WorkingArea
        Else
            screenArea = Screen.FromPoint(Cursor.Position).WorkingArea
        End If

        ' Stack toasts upwards from bottom-right
        Dim yOffset As Integer = _toastMargin
        For i = _openToasts.Count - 1 To 0 Step -1
            Dim open = _openToasts(i)
            If open Is Nothing OrElse open.IsDisposed Then
                _openToasts.RemoveAt(i)
            Else
                yOffset += open.Height + _toastMargin
            End If
        Next
        toast.Location = New Point(screenArea.Right - toast.Width - _toastMargin, screenArea.Bottom - toast.Height - yOffset)

        ' Track toast and dismissal
        _openToasts.Add(toast)
        AddHandler toast.FormClosed, Sub() _openToasts.Remove(toast)

        ' Keep modeless and low churn during modal/login
        If loginActive Or modalActive Then
            toast.TopMost = False
        End If

        ' Register sticky state
        _stickyToasts(id) = New ToastState With {
            .Form = toast,
            .EnsureMainOnTop = localEnsureMainOnTop
        }

        Try
            ' ✅ FIX: Always show without owner for maximum visibility
            ' TopMost property (when set) ensures toast appears on top
            toast.Show()  ' Don't use owner - ensures visibility regardless of parent state
            toast.Refresh()
        Catch
            ' If show failed, cleanup state
            _stickyToasts.Remove(id)
            SafeCloseToast(toast, localEnsureMainOnTop)
        End Try
    End Sub

    Private Sub SafeCloseToast(toast As Form, ensureMainOnTop As Boolean)
        Try
            If toast Is Nothing Then Return
            If Not toast.IsDisposed Then
                toast.Hide()
                toast.Close()
                toast.Dispose()
            End If
        Catch
        Finally
            If ensureMainOnTop Then
                RestoreMainFormFocus()
            End If
        End Try
    End Sub

    Private Sub RestoreMainFormFocus()
        Try
            Dim target As Form = Nothing
            If OnVehicleScreen IsNot Nothing AndAlso Not OnVehicleScreen.IsDisposed Then
                target = OnVehicleScreen
            ElseIf GmResidentClient IsNot Nothing AndAlso Not GmResidentClient.IsDisposed Then
                target = GmResidentClient
            End If

            If target Is Nothing Then Return

            If target.WindowState = FormWindowState.Minimized Then
                target.WindowState = FormWindowState.Normal
            End If

            ' Bring to top without permanently forcing TopMost
            target.TopMost = True
            target.BringToFront()
            target.Activate()
            target.TopMost = False
        Catch
        End Try
    End Sub

    Private Function GetToastColors(kind As ToastKind) As (Back As Color, Fore As Color)
        Select Case kind
            Case ToastKind.Warning
                Return (Color.FromArgb(60, 50, 10), Color.Gold)
            Case ToastKind.Error
                Return (Color.FromArgb(60, 10, 10), Color.FromArgb(255, 200, 200))
            Case Else
                Return (Color.FromArgb(40, 40, 40), Color.White)
        End Select
    End Function

    Private Function GetToastStripeColor(kind As ToastKind) As Color
        Select Case kind
            Case ToastKind.Warning : Return Color.Gold
            Case ToastKind.Error : Return Color.IndianRed
            Case Else : Return Color.DeepSkyBlue
        End Select
    End Function
End Module