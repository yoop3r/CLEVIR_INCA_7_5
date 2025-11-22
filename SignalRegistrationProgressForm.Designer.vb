<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SignalRegistrationProgressForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar()
        Me.Label_Header = New System.Windows.Forms.Label()
        Me.Label_TotalSignals = New System.Windows.Forms.Label()
        Me.Label_Success = New System.Windows.Forms.Label()
        Me.Label_Failed = New System.Windows.Forms.Label()
        Me.Label_Skipped = New System.Windows.Forms.Label()
        Me.Label_ElapsedTime = New System.Windows.Forms.Label()
        Me.Label_Percentage = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Location = New System.Drawing.Point(20, 60)
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(440, 30)
        Me.ProgressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous
        Me.ProgressBar1.TabIndex = 1
        '
        'Label_Header
        '
        Me.Label_Header.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold)
        Me.Label_Header.Location = New System.Drawing.Point(12, 15)
        Me.Label_Header.Name = "Label_Header"
        Me.Label_Header.Size = New System.Drawing.Size(460, 42)
        Me.Label_Header.TabIndex = 0
        Me.Label_Header.Text = "Signal Registration in Progress"
        Me.Label_Header.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label_TotalSignals
        '
        Me.Label_TotalSignals.Location = New System.Drawing.Point(20, 125)
        Me.Label_TotalSignals.Name = "Label_TotalSignals"
        Me.Label_TotalSignals.Size = New System.Drawing.Size(440, 20)
        Me.Label_TotalSignals.TabIndex = 3
        Me.Label_TotalSignals.Text = "Total Signals: 0"
        '
        'Label_Success
        '
        Me.Label_Success.Location = New System.Drawing.Point(20, 150)
        Me.Label_Success.Name = "Label_Success"
        Me.Label_Success.Size = New System.Drawing.Size(440, 20)
        Me.Label_Success.TabIndex = 4
        Me.Label_Success.Text = "Successfully registered: 0"
        '
        'Label_Failed
        '
        Me.Label_Failed.ForeColor = System.Drawing.Color.Red
        Me.Label_Failed.Location = New System.Drawing.Point(20, 175)
        Me.Label_Failed.Name = "Label_Failed"
        Me.Label_Failed.Size = New System.Drawing.Size(440, 20)
        Me.Label_Failed.TabIndex = 5
        Me.Label_Failed.Text = "Failed: 0"
        '
        'Label_Skipped
        '
        Me.Label_Skipped.ForeColor = System.Drawing.Color.Gray
        Me.Label_Skipped.Location = New System.Drawing.Point(20, 200)
        Me.Label_Skipped.Name = "Label_Skipped"
        Me.Label_Skipped.Size = New System.Drawing.Size(440, 20)
        Me.Label_Skipped.TabIndex = 6
        Me.Label_Skipped.Text = "Skipped: 0"
        '
        'Label_ElapsedTime
        '
        Me.Label_ElapsedTime.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Italic)
        Me.Label_ElapsedTime.Location = New System.Drawing.Point(20, 235)
        Me.Label_ElapsedTime.Name = "Label_ElapsedTime"
        Me.Label_ElapsedTime.Size = New System.Drawing.Size(440, 29)
        Me.Label_ElapsedTime.TabIndex = 7
        Me.Label_ElapsedTime.Text = "Elapsed Time: 00:00:00"
        Me.Label_ElapsedTime.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label_Percentage
        '
        Me.Label_Percentage.Font = New System.Drawing.Font("Microsoft Sans Serif", 10.0!, System.Drawing.FontStyle.Bold)
        Me.Label_Percentage.Location = New System.Drawing.Point(195, 95)
        Me.Label_Percentage.Name = "Label_Percentage"
        Me.Label_Percentage.Size = New System.Drawing.Size(90, 30)
        Me.Label_Percentage.TabIndex = 2
        Me.Label_Percentage.Text = "0%"
        Me.Label_Percentage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'SignalRegistrationProgressForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(484, 308)
        Me.ControlBox = False
        Me.Controls.Add(Me.Label_ElapsedTime)
        Me.Controls.Add(Me.Label_Skipped)
        Me.Controls.Add(Me.Label_Failed)
        Me.Controls.Add(Me.Label_Success)
        Me.Controls.Add(Me.Label_TotalSignals)
        Me.Controls.Add(Me.Label_Percentage)
        Me.Controls.Add(Me.ProgressBar1)
        Me.Controls.Add(Me.Label_Header)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "SignalRegistrationProgressForm"
        Me.ShowIcon = False
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Signal Registration"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents ProgressBar1 As ProgressBar
    Friend WithEvents Label_Header As Label
    Friend WithEvents Label_TotalSignals As Label
    Friend WithEvents Label_Success As Label
    Friend WithEvents Label_Failed As Label
    Friend WithEvents Label_Skipped As Label
    Friend WithEvents Label_ElapsedTime As Label
    Friend WithEvents Label_Percentage As Label
End Class

