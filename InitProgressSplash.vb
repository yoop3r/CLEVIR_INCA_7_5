Imports System.Windows.Forms
Imports System.Drawing

Public NotInheritable Class InitProgressSplash
    Inherits Form

    Private ReadOnly _label As Label
    Private ReadOnly _progress As ProgressBar
    Private ReadOnly _cancel As Button

    Public Event CancelRequested As EventHandler

    Public Enum SplashAnchor
        Center
        TopLeft
        TopRight
        BottomLeft
        BottomRight
    End Enum

    Public Sub PositionOnActiveScreen(anchor As SplashAnchor, Optional margin As Integer = 16)
        Dim area = Screen.FromPoint(Cursor.Position).WorkingArea
        Dim x As Integer
        Dim y As Integer
        Select Case anchor
            Case SplashAnchor.Center
                x = CInt(area.Left + (area.Width - Width) / 2)
                y = CInt(area.Top + (area.Height - Height) / 2)
            Case SplashAnchor.TopLeft
                x = area.Left + margin
                y = area.Top + margin
            Case SplashAnchor.TopRight
                x = area.Right - Width - margin
                y = area.Top + margin
            Case SplashAnchor.BottomLeft
                x = area.Left + margin
                y = area.Bottom - Height - margin
            Case SplashAnchor.BottomRight
                x = area.Right - Width - margin
                y = area.Bottom - Height - margin
        End Select
        StartPosition = FormStartPosition.Manual
        Location = New Point(x, y)
    End Sub

    Public Sub New(Optional showCancel As Boolean = True)
        FormBorderStyle = FormBorderStyle.None
        StartPosition = FormStartPosition.Manual
        ShowInTaskbar = False
        TopMost = False
        DoubleBuffered = True
        Opacity = 0.98
        BackColor = Color.FromArgb(36, 36, 36)
        Size = New Size(420, 120)

        _label = New Label() With {
            .ForeColor = Color.White,
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Dock = DockStyle.Top,
            .Height = 60,
            .Font = New Font("Segoe UI", 10.0F, FontStyle.Regular),
            .Padding = New Padding(14),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Text = "Initializing..."
        }
        Controls.Add(_label)

        _progress = New ProgressBar() With {
            .Dock = DockStyle.Top,
            .Height = 22,
            .Style = ProgressBarStyle.Marquee,
            .MarqueeAnimationSpeed = 45
        }
        Controls.Add(_progress)

        _cancel = New Button() With {
            .Dock = DockStyle.Right,
            .Width = 90,
            .Text = "Cancel",
            .Visible = showCancel,
            .BackColor = Color.Gainsboro
        }
        AddHandler _cancel.Click, Sub() RaiseEvent CancelRequested(Me, EventArgs.Empty)
        Dim panel As New Panel() With {.Dock = DockStyle.Top, .Height = 32, .BackColor = Color.Transparent}
        panel.Controls.Add(_cancel)
        Controls.Add(panel)
    End Sub

    Public Sub CenterOnActiveScreen()
        Dim area = Screen.FromPoint(Cursor.Position).WorkingArea
        Location = New Point(CInt(area.Left + (area.Width - Width) / 2),
                             CInt(area.Top + (area.Height - Height) / 2))
    End Sub

    Public Sub SetStatus(message As String)
        If InvokeRequired Then
            Invoke(New Action(Of String)(AddressOf SetStatus), message)
            Return
        End If

        Try
            _label.Text = message
            _label.Refresh()      ' ✅ Force label to repaint immediately
            Me.Refresh()                ' ✅ Force form to repaint
            Application.DoEvents()      ' ✅ Process pending UI messages
        Catch ex As Exception
            ' Ignore errors if form is closing
        End Try
    End Sub

    Public Sub SetProgressStyle(style As ProgressBarStyle)
        If IsHandleCreated AndAlso InvokeRequired Then
            BeginInvoke(New Action(Of ProgressBarStyle)(AddressOf SetProgressStyle), style)
            Return
        End If
        _progress.Style = style
    End Sub

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or &H80 Or &H8000000 Or &H2000000 ' ToolWindow, NoActivate, Composited
            Return cp
        End Get
    End Property

    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property
End Class