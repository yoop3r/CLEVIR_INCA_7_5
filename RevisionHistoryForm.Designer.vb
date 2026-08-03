<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class RevisionHistoryForm
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
        Me.TextBoxHistory = New System.Windows.Forms.TextBox()
        Me.ButtonClose = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'TextBoxHistory
        '
        Me.TextBoxHistory.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBoxHistory.Font = New System.Drawing.Font("Consolas", 9.5!)
        Me.TextBoxHistory.Location = New System.Drawing.Point(12, 12)
        Me.TextBoxHistory.Multiline = True
        Me.TextBoxHistory.Name = "TextBoxHistory"
        Me.TextBoxHistory.ReadOnly = True
        Me.TextBoxHistory.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.TextBoxHistory.Size = New System.Drawing.Size(560, 400)
        Me.TextBoxHistory.TabIndex = 0
        Me.TextBoxHistory.TabStop = False
        '
        'ButtonClose
        '
        Me.ButtonClose.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonClose.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonClose.Location = New System.Drawing.Point(497, 418)
        Me.ButtonClose.Name = "ButtonClose"
        Me.ButtonClose.Size = New System.Drawing.Size(75, 30)
        Me.ButtonClose.TabIndex = 1
        Me.ButtonClose.Text = "Close"
        Me.ButtonClose.UseVisualStyleBackColor = True
        '
        'RevisionHistoryForm
        '
        Me.AcceptButton = Me.ButtonClose
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(584, 461)
        Me.Controls.Add(Me.ButtonClose)
        Me.Controls.Add(Me.TextBoxHistory)
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.MinimumSize = New System.Drawing.Size(400, 300)
        Me.Name = "RevisionHistoryForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "What's New - Revision History"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents TextBoxHistory As System.Windows.Forms.TextBox
    Friend WithEvents ButtonClose As System.Windows.Forms.Button
End Class
