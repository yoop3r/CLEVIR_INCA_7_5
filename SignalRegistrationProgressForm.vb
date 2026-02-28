Imports System.Threading
Imports System.Threading.Tasks

Public Class SignalRegistrationProgressForm
    ' Progress tracking
    Private _totalSignals As Integer = 0
    Private _processedSignals As Integer = 0
    Private _successCount As Integer = 0
    Private _failureCount As Integer = 0
    Private _skippedCount As Integer = 0
    Private _startTime As DateTime = DateTime.Now
    Private _isCachedRegistration As Boolean = False

    ' Timer for elapsed time display
    Private WithEvents UpdateTimer As New System.Windows.Forms.Timer()

    Public Sub New(totalSignals As Integer)
        InitializeComponent()

        _totalSignals = totalSignals
        ProgressBar1.Maximum = totalSignals
        ProgressBar1.Value = 0

        ' ✅ CRITICAL: Force form to be visible and on top
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.TopMost = True
        Me.ShowInTaskbar = True
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.ControlBox = False  ' Disable close button during registration

        ' Update elapsed time every second
        UpdateTimer.Interval = 1000
        UpdateTimer.Start()

        ' Initialize labels
        Label_TotalSignals.Text = $"Total Signals: {totalSignals}"
        Label_Success.Text = "Successfully registered: 0"
        Label_Failed.Text = "Failed: 0"
        Label_Skipped.Text = "Skipped: 0"
        Label_ElapsedTime.Text = "Elapsed Time: 00:00:00"
    End Sub

    ''' <summary>
    ''' Show cached registration mode with visual indicator
    ''' </summary>
    Public Sub ShowCachedRegistrationMode(cacheSignalCount As Integer)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() ShowCachedRegistrationMode(cacheSignalCount))
            Return
        End If

        _isCachedRegistration = True

        ' Update title to indicate cached registration
        Me.Text = "Signal Registration (From Cache)"

        ' Update labels to show cache mode
        Label_TotalSignals.Text = $"📦 Registering from cache: {cacheSignalCount} signals"
        Label_TotalSignals.ForeColor = Color.DarkGreen

        Label_Success.Text = "Subscribing signals to measurement group..."
        Label_Success.ForeColor = Color.DarkBlue

        Label_Failed.Text = ""
        Label_Skipped.Text = "⚡ Fast registration - signals already in INCA"

        ' Set progress bar to marquee style for initial validation
        ProgressBar1.Style = ProgressBarStyle.Marquee
        ProgressBar1.MarqueeAnimationSpeed = 30

        Me.Refresh()
        Application.DoEvents()
    End Sub

    ''' <summary>
    ''' Update progress during cached registration
    ''' </summary>
    Public Sub UpdateCachedProgress(subscribedCount As Integer, totalCount As Integer)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() UpdateCachedProgress(subscribedCount, totalCount))
            Return
        End If

        ' Switch to continuous progress bar if we were in marquee mode
        If ProgressBar1.Style = ProgressBarStyle.Marquee Then
            ProgressBar1.Style = ProgressBarStyle.Continuous
            ProgressBar1.Maximum = totalCount
        End If

        ProgressBar1.Value = Math.Min(subscribedCount, totalCount)

        ' Update labels
        Label_Success.Text = $"Subscribed: {subscribedCount} / {totalCount}"

        ' Update percentage
        Dim percentage As Integer = CInt((subscribedCount / CDbl(totalCount)) * 100)
        Label_Percentage.Text = $"{percentage}%"

        Me.Refresh()
        Application.DoEvents()
    End Sub

    ''' <summary>
    ''' Thread-safe method to update progress from background thread
    ''' </summary>
    Public Sub UpdateProgress(successCount As Integer, failureCount As Integer, skippedCount As Integer)
        If Me.InvokeRequired Then
            Me.Invoke(Sub() UpdateProgress(successCount, failureCount, skippedCount))
            Return
        End If

        _successCount = successCount
        _failureCount = failureCount
        _skippedCount = skippedCount
        _processedSignals = successCount + failureCount + skippedCount

        ' Update progress bar
        ProgressBar1.Value = Math.Min(_processedSignals, _totalSignals)

        ' Update labels
        Label_Success.Text = $"Successfully registered: {_successCount} / {_totalSignals}"
        Label_Failed.Text = $"Failed: {_failureCount}"
        Label_Skipped.Text = $"Skipped: {_skippedCount}"

        ' Update percentage
        Dim percentage As Integer = CInt((_processedSignals / CDbl(_totalSignals)) * 100)
        Label_Percentage.Text = $"{percentage}%"

        ' ✅ CRITICAL: Force redraw
        Me.Refresh()
        Application.DoEvents()
    End Sub

    ''' <summary>
    ''' Update elapsed time display
    ''' </summary>
    Private Sub UpdateTimer_Tick(sender As Object, e As EventArgs) Handles UpdateTimer.Tick
        Dim elapsed As TimeSpan = DateTime.Now - _startTime
        Label_ElapsedTime.Text = $"Elapsed Time: {elapsed:hh\:mm\:ss}"
    End Sub

    ''' <summary>
    ''' Show completion status and close form
    ''' </summary>
    Public Sub ShowCompletion(success As Boolean, Optional errorMessage As String = "")
        UpdateTimer.Stop()

        ' ✅ ENSURE WE'RE ON UI THREAD
        If Me.InvokeRequired Then
            Me.Invoke(Sub() ShowCompletion(success, errorMessage))
            Return
        End If

        If success Then
            ' ✅ SHOW SUCCESS MESSAGE BRIEFLY
            If _isCachedRegistration Then
                Label_Success.Text = "✓ Cache Registration Complete!"
                Label_Skipped.Text = "⚡ Faster startup using cached signals"
            Else
                Label_Success.Text = "✓ Registration Complete!"
            End If
            Label_Success.ForeColor = Color.Green
            ProgressBar1.Style = ProgressBarStyle.Continuous
            ProgressBar1.Value = ProgressBar1.Maximum
            Me.Refresh()
            Thread.Sleep(1500)  ' Show for 1.5 seconds

            Me.DialogResult = DialogResult.OK
            Me.Close()
        Else
            MessageBox.Show(
                errorMessage,
                "Signal Registration Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        UpdateTimer.Stop()
        MyBase.OnFormClosing(e)
    End Sub
End Class