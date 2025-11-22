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
            Label_Success.Text = "✓ Registration Complete!"
            Label_Success.ForeColor = Color.Green
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