
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class InitForm
    Inherits System.Windows.Forms.Form

    <STAThread()>
    Public Shared Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Application.Run(New InitForm())
    End Sub


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
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.ButtonConfigEditor = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'Button1
        '
        Me.Button1.Font = New System.Drawing.Font("Microsoft Sans Serif", 20.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.Location = New System.Drawing.Point(12, 40)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(276, 42)
        Me.Button1.TabIndex = 0
        Me.Button1.Text = "DRIVE"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Font = New System.Drawing.Font("Microsoft Sans Serif", 18.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button3.Location = New System.Drawing.Point(12, 140)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(276, 35)
        Me.Button3.TabIndex = 2
        Me.Button3.Text = "Exit"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'ButtonConfigEditor
        '
        Me.ButtonConfigEditor.Location = New System.Drawing.Point(12, 95)
        Me.ButtonConfigEditor.Name = "ButtonConfigEditor"
        Me.ButtonConfigEditor.Size = New System.Drawing.Size(276, 35)
        Me.ButtonConfigEditor.TabIndex = 99
        Me.ButtonConfigEditor.Text = "⚙️ Edit Configuration"
        Me.ButtonConfigEditor.UseVisualStyleBackColor = True
        '
        'InitForm
        '
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.ClientSize = New System.Drawing.Size(310, 195)
        Me.ControlBox = False
        Me.Controls.Add(Me.ButtonConfigEditor)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button1)
        Me.Name = "InitForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "(Version 7.5.1)"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents OpenFileDialog1 As System.Windows.Forms.OpenFileDialog
    Friend WithEvents ButtonConfigEditor As Button
End Class
