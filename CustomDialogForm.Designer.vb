<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class CustomDialogForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
        Me.btnContinue = New System.Windows.Forms.Button()
        Me.btnExit = New System.Windows.Forms.Button()
        Me.lblErrorMessage = New System.Windows.Forms.Label()
        Me.lblInformToolMessage = New System.Windows.Forms.Label()
        Me.continueWithout = New System.Windows.Forms.Label()
        Me.exitCLEVIR = New System.Windows.Forms.Label()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'btnContinue
        '
        Me.btnContinue.Location = New System.Drawing.Point(63, 175)
        Me.btnContinue.Name = "btnContinue"
        Me.btnContinue.Size = New System.Drawing.Size(90, 40)
        Me.btnContinue.TabIndex = 0
        Me.btnContinue.Text = "Continue"
        Me.btnContinue.UseVisualStyleBackColor = True
        '
        'btnExit
        '
        Me.btnExit.Location = New System.Drawing.Point(557, 175)
        Me.btnExit.Name = "btnExit"
        Me.btnExit.Size = New System.Drawing.Size(90, 40)
        Me.btnExit.TabIndex = 1
        Me.btnExit.Text = "Exit"
        Me.btnExit.UseVisualStyleBackColor = True
        '
        'lblErrorMessage
        '
        Me.lblErrorMessage.Location = New System.Drawing.Point(12, 9)
        Me.lblErrorMessage.Name = "lblErrorMessage"
        Me.lblErrorMessage.Size = New System.Drawing.Size(692, 61)
        Me.lblErrorMessage.TabIndex = 2
        Me.lblErrorMessage.Text = "Message"
        '
        'lblInformToolMessage
        '
        Me.lblInformToolMessage.AutoSize = True
        Me.lblInformToolMessage.Location = New System.Drawing.Point(63, 77)
        Me.lblInformToolMessage.Name = "lblInformToolMessage"
        Me.lblInformToolMessage.Size = New System.Drawing.Size(178, 20)
        Me.lblInformToolMessage.TabIndex = 3
        Me.lblInformToolMessage.Text = "TestTools Initialization - "
        '
        'continueWithout
        '
        Me.continueWithout.AutoSize = True
        Me.continueWithout.Location = New System.Drawing.Point(62, 107)
        Me.continueWithout.Name = "continueWithout"
        Me.continueWithout.Size = New System.Drawing.Size(222, 20)
        Me.continueWithout.TabIndex = 7
        Me.continueWithout.Text = "Selecting 'Continue' will inhibit:"
        '
        'exitCLEVIR
        '
        Me.exitCLEVIR.AutoSize = True
        Me.exitCLEVIR.Location = New System.Drawing.Point(62, 139)
        Me.exitCLEVIR.Name = "exitCLEVIR"
        Me.exitCLEVIR.Size = New System.Drawing.Size(228, 20)
        Me.exitCLEVIR.TabIndex = 8
        Me.exitCLEVIR.Text = "Selecting 'Exit' will Exit CLEVIR"
        '
        'Button1
        '
        Me.Button1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Button1.Location = New System.Drawing.Point(15, 224)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(150, 38)
        Me.Button1.TabIndex = 9
        Me.Button1.Text = "Button1"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Button2.Location = New System.Drawing.Point(185, 224)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(150, 38)
        Me.Button2.TabIndex = 10
        Me.Button2.Text = "Button2"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button3.Location = New System.Drawing.Point(355, 224)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(150, 38)
        Me.Button3.TabIndex = 11
        Me.Button3.Text = "Button3"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'Button4
        '
        Me.Button4.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button4.Location = New System.Drawing.Point(525, 224)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(150, 38)
        Me.Button4.TabIndex = 12
        Me.Button4.Text = "Button4"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'CustomDialogForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(716, 274)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
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
    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents Button3 As Button
    Friend WithEvents Button4 As Button
End Class
