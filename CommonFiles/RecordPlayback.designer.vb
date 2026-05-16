<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class RecordPlayback
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(RecordPlayback))
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar()
        Me.StopRecord = New System.Windows.Forms.Button()
        Me.Record = New System.Windows.Forms.Button()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.HScrollBar1 = New System.Windows.Forms.HScrollBar()
        Me.SelectFile = New System.Windows.Forms.Button()
        Me.Reset = New System.Windows.Forms.Button()
        Me.StepBack = New System.Windows.Forms.Button()
        Me.StepForward = New System.Windows.Forms.Button()
        Me.StopButton = New System.Windows.Forms.Button()
        Me.PlayPauseButton = New System.Windows.Forms.Button()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.ProgressBar1)
        Me.GroupBox1.Controls.Add(Me.StopRecord)
        Me.GroupBox1.Controls.Add(Me.Record)
        Me.GroupBox1.Location = New System.Drawing.Point(12, 12)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(100, 88)
        Me.GroupBox1.TabIndex = 1
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "RECORD"
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Location = New System.Drawing.Point(6, 65)
        Me.ProgressBar1.Maximum = 5000
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(86, 15)
        Me.ProgressBar1.Step = 1
        Me.ProgressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous
        Me.ProgressBar1.TabIndex = 2
        '
        'StopRecord
        '
        Me.StopRecord.Enabled = False
        Me.StopRecord.Image = CType(resources.GetObject("StopRecord.Image"), System.Drawing.Image)
        Me.StopRecord.Location = New System.Drawing.Point(52, 19)
        Me.StopRecord.Name = "StopRecord"
        Me.StopRecord.Size = New System.Drawing.Size(40, 40)
        Me.StopRecord.TabIndex = 1
        Me.StopRecord.UseVisualStyleBackColor = True
        '
        'Record
        '
        Me.Record.BackColor = System.Drawing.Color.Red
        Me.Record.Enabled = False
        Me.Record.Image = CType(resources.GetObject("Record.Image"), System.Drawing.Image)
        Me.Record.Location = New System.Drawing.Point(6, 19)
        Me.Record.Name = "Record"
        Me.Record.Size = New System.Drawing.Size(40, 40)
        Me.Record.TabIndex = 0
        Me.Record.UseVisualStyleBackColor = False
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.Label2)
        Me.GroupBox2.Controls.Add(Me.Label1)
        Me.GroupBox2.Controls.Add(Me.HScrollBar1)
        Me.GroupBox2.Controls.Add(Me.SelectFile)
        Me.GroupBox2.Controls.Add(Me.Reset)
        Me.GroupBox2.Controls.Add(Me.StepBack)
        Me.GroupBox2.Controls.Add(Me.StepForward)
        Me.GroupBox2.Controls.Add(Me.StopButton)
        Me.GroupBox2.Controls.Add(Me.PlayPauseButton)
        Me.GroupBox2.Location = New System.Drawing.Point(118, 12)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(285, 88)
        Me.GroupBox2.TabIndex = 2
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "PLAYBACK"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(255, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.Label2.Location = New System.Drawing.Point(91, 33)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(180, 13)
        Me.Label2.TabIndex = 11
        Me.Label2.Text = "Loading Playback File Please Wait..."
        Me.Label2.Visible = False
        '
        'Label1
        '
        Me.Label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label1.Location = New System.Drawing.Point(6, 65)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(75, 15)
        Me.Label1.TabIndex = 10
        Me.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'HScrollBar1
        '
        Me.HScrollBar1.Enabled = False
        Me.HScrollBar1.LargeChange = 1
        Me.HScrollBar1.Location = New System.Drawing.Point(87, 65)
        Me.HScrollBar1.Name = "HScrollBar1"
        Me.HScrollBar1.Size = New System.Drawing.Size(188, 15)
        Me.HScrollBar1.TabIndex = 9
        '
        'SelectFile
        '
        Me.SelectFile.Image = CType(resources.GetObject("SelectFile.Image"), System.Drawing.Image)
        Me.SelectFile.Location = New System.Drawing.Point(6, 19)
        Me.SelectFile.Name = "SelectFile"
        Me.SelectFile.Size = New System.Drawing.Size(75, 40)
        Me.SelectFile.TabIndex = 8
        Me.SelectFile.UseVisualStyleBackColor = True
        '
        'Reset
        '
        Me.Reset.Enabled = False
        Me.Reset.Image = CType(resources.GetObject("Reset.Image"), System.Drawing.Image)
        Me.Reset.Location = New System.Drawing.Point(87, 19)
        Me.Reset.Name = "Reset"
        Me.Reset.Size = New System.Drawing.Size(40, 40)
        Me.Reset.TabIndex = 7
        Me.Reset.UseVisualStyleBackColor = True
        '
        'StepBack
        '
        Me.StepBack.Enabled = False
        Me.StepBack.Image = CType(resources.GetObject("StepBack.Image"), System.Drawing.Image)
        Me.StepBack.Location = New System.Drawing.Point(124, 19)
        Me.StepBack.Name = "StepBack"
        Me.StepBack.Size = New System.Drawing.Size(40, 40)
        Me.StepBack.TabIndex = 6
        Me.StepBack.UseVisualStyleBackColor = True
        '
        'StepForward
        '
        Me.StepForward.Enabled = False
        Me.StepForward.Image = CType(resources.GetObject("StepForward.Image"), System.Drawing.Image)
        Me.StepForward.Location = New System.Drawing.Point(235, 19)
        Me.StepForward.Name = "StepForward"
        Me.StepForward.Size = New System.Drawing.Size(40, 40)
        Me.StepForward.TabIndex = 5
        Me.StepForward.UseVisualStyleBackColor = True
        '
        'StopButton
        '
        Me.StopButton.Enabled = False
        Me.StopButton.Image = CType(resources.GetObject("StopButton.Image"), System.Drawing.Image)
        Me.StopButton.Location = New System.Drawing.Point(161, 19)
        Me.StopButton.Name = "StopButton"
        Me.StopButton.Size = New System.Drawing.Size(40, 40)
        Me.StopButton.TabIndex = 3
        Me.StopButton.UseVisualStyleBackColor = True
        '
        'PlayPauseButton
        '
        Me.PlayPauseButton.Enabled = False
        Me.PlayPauseButton.Image = CType(resources.GetObject("PlayPauseButton.Image"), System.Drawing.Image)
        Me.PlayPauseButton.Location = New System.Drawing.Point(198, 19)
        Me.PlayPauseButton.Name = "PlayPauseButton"
        Me.PlayPauseButton.Size = New System.Drawing.Size(40, 40)
        Me.PlayPauseButton.TabIndex = 2
        Me.PlayPauseButton.UseVisualStyleBackColor = True
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'RecordPlayback
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(408, 108)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Name = "RecordPlayback"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Record/Playback"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents Record As System.Windows.Forms.Button
    Friend WithEvents PlayPauseButton As System.Windows.Forms.Button
    Friend WithEvents StopButton As System.Windows.Forms.Button
    Friend WithEvents StepForward As System.Windows.Forms.Button
    Friend WithEvents StepBack As System.Windows.Forms.Button
    Friend WithEvents Reset As System.Windows.Forms.Button
    Friend WithEvents SelectFile As System.Windows.Forms.Button
    Friend WithEvents StopRecord As System.Windows.Forms.Button
    Friend WithEvents ProgressBar1 As System.Windows.Forms.ProgressBar
    Friend WithEvents HScrollBar1 As System.Windows.Forms.HScrollBar
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents OpenFileDialog1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents Label2 As System.Windows.Forms.Label
End Class
