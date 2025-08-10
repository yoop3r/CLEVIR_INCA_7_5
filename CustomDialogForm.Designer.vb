<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class CustomDialogForm
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
        Me.btnContinue = New System.Windows.Forms.Button()
        Me.btnExit = New System.Windows.Forms.Button()
        Me.lblErrorMessage = New System.Windows.Forms.Label()
        Me.lblInformToolMessage = New System.Windows.Forms.Label()
        Me.continueWithout = New System.Windows.Forms.Label()
        Me.exitCLEVIR = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'btnContinue
        '
        Me.btnContinue.Location = New System.Drawing.Point(63, 166)
        Me.btnContinue.Name = "btnContinue"
        Me.btnContinue.Size = New System.Drawing.Size(90, 40)
        Me.btnContinue.TabIndex = 0
        Me.btnContinue.Text = "Continue"
        Me.btnContinue.UseVisualStyleBackColor = True
        '
        'btnExit
        '
        Me.btnExit.Location = New System.Drawing.Point(557, 166)
        Me.btnExit.Name = "btnExit"
        Me.btnExit.Size = New System.Drawing.Size(90, 40)
        Me.btnExit.TabIndex = 1
        Me.btnExit.Text = "Exit"
        Me.btnExit.UseVisualStyleBackColor = True
        '
        'lblErrorMessage
        '
        Me.lblErrorMessage.Location = New System.Drawing.Point(239, 19)
        Me.lblErrorMessage.Name = "lblErrorMessage"
        Me.lblErrorMessage.Size = New System.Drawing.Size(408, 50)
        Me.lblErrorMessage.TabIndex = 2
        Me.lblErrorMessage.Text = "Message"
        '
        'lblInformToolMessage
        '
        Me.lblInformToolMessage.AutoSize = True
        Me.lblInformToolMessage.Location = New System.Drawing.Point(59, 18)
        Me.lblInformToolMessage.Name = "lblInformToolMessage"
        Me.lblInformToolMessage.Size = New System.Drawing.Size(178, 20)
        Me.lblInformToolMessage.TabIndex = 3
        Me.lblInformToolMessage.Text = "TestTools Initialization - "
        '
        'continueWithout
        '
        Me.continueWithout.AutoSize = True
        Me.continueWithout.Location = New System.Drawing.Point(62, 76)
        Me.continueWithout.Name = "continueWithout"
        Me.continueWithout.Size = New System.Drawing.Size(222, 20)
        Me.continueWithout.TabIndex = 7
        Me.continueWithout.Text = "Selecting 'Continue' will inhibit:"
        '
        'exitCLEVIR
        '
        Me.exitCLEVIR.AutoSize = True
        Me.exitCLEVIR.Location = New System.Drawing.Point(62, 111)
        Me.exitCLEVIR.Name = "exitCLEVIR"
        Me.exitCLEVIR.Size = New System.Drawing.Size(228, 20)
        Me.exitCLEVIR.TabIndex = 8
        Me.exitCLEVIR.Text = "Selecting 'Exit' will Exit CLEVIR"
        '
        'CustomDialogForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(716, 230)
        Me.Controls.Add(Me.exitCLEVIR)
        Me.Controls.Add(Me.continueWithout)
        Me.Controls.Add(Me.lblInformToolMessage)
        Me.Controls.Add(Me.lblErrorMessage)
        Me.Controls.Add(Me.btnExit)
        Me.Controls.Add(Me.btnContinue)
        Me.Name = "CustomDialogForm"
        Me.Text = "CustomDialogForm"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btnContinue As Button
    Friend WithEvents btnExit As Button
    Friend WithEvents lblErrorMessage As Label
    Friend WithEvents lblInformToolMessage As Label
    Friend WithEvents continueWithout As Label
    Friend WithEvents exitCLEVIR As Label
End Class
