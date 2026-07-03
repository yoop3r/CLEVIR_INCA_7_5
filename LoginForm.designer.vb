<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class LoginForm
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing Then
                If components IsNot Nothing Then
                    components.Dispose()
                End If

                _loginSubmitButton?.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Button43 = New System.Windows.Forms.Button()
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.GroupBox_SessionMetadata = New System.Windows.Forms.GroupBox()
        Me.Label_Group = New System.Windows.Forms.Label()
        Me.ComboBox_Group = New System.Windows.Forms.ComboBox()
        Me.Label_Procedure = New System.Windows.Forms.Label()
        Me.ComboBox_Procedure = New System.Windows.Forms.ComboBox()
        Me.Label_Email = New System.Windows.Forms.Label()
        Me.TextBox_Email = New System.Windows.Forms.TextBox()
        Me.GroupBox_SessionMetadata.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label4
        '
        Me.Label4.BackColor = System.Drawing.Color.White
        Me.Label4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label4.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.Location = New System.Drawing.Point(27, 452)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(408, 50)
        Me.Label4.TabIndex = 6
        Me.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Button43
        '
        Me.Button43.BackColor = System.Drawing.SystemColors.Control
        Me.Button43.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button43.ForeColor = System.Drawing.Color.Black
        Me.Button43.Location = New System.Drawing.Point(354, 10)
        Me.Button43.Name = "Button43"
        Me.Button43.Size = New System.Drawing.Size(100, 80)
        Me.Button43.TabIndex = 11
        Me.Button43.Text = "EXIT"
        Me.Button43.UseVisualStyleBackColor = True
        '
        'ToolTip1
        '
        AddHandler Me.ToolTip1.Popup, AddressOf Me.ToolTip1_Popup
        '
        'GroupBox_SessionMetadata
        '
        Me.GroupBox_SessionMetadata.Controls.Add(Me.Label_Group)
        Me.GroupBox_SessionMetadata.Controls.Add(Me.ComboBox_Group)
        Me.GroupBox_SessionMetadata.Controls.Add(Me.Label_Procedure)
        Me.GroupBox_SessionMetadata.Controls.Add(Me.ComboBox_Procedure)
        Me.GroupBox_SessionMetadata.Controls.Add(Me.Label_Email)
        Me.GroupBox_SessionMetadata.Controls.Add(Me.TextBox_Email)
        Me.GroupBox_SessionMetadata.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox_SessionMetadata.Location = New System.Drawing.Point(12, 331)
        Me.GroupBox_SessionMetadata.Name = "GroupBox_SessionMetadata"
        Me.GroupBox_SessionMetadata.Size = New System.Drawing.Size(442, 140)
        Me.GroupBox_SessionMetadata.TabIndex = 20
        Me.GroupBox_SessionMetadata.TabStop = False
        Me.GroupBox_SessionMetadata.Text = "Session Details (Required)"
        '
        'Label_Group
        '
        Me.Label_Group.AutoSize = True
        Me.Label_Group.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.Label_Group.Location = New System.Drawing.Point(39, 30)
        Me.Label_Group.Name = "Label_Group"
        Me.Label_Group.Size = New System.Drawing.Size(135, 28)
        Me.Label_Group.TabIndex = 0
        Me.Label_Group.Text = "ADAS Group:"
        '
        'ComboBox_Group
        '
        Me.ComboBox_Group.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ComboBox_Group.FormattingEnabled = True
        Me.ComboBox_Group.Location = New System.Drawing.Point(144, 27)
        Me.ComboBox_Group.Name = "ComboBox_Group"
        Me.ComboBox_Group.Size = New System.Drawing.Size(280, 33)
        Me.ComboBox_Group.TabIndex = 21
        '
        'Label_Procedure
        '
        Me.Label_Procedure.AutoSize = True
        Me.Label_Procedure.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.Label_Procedure.Location = New System.Drawing.Point(61, 65)
        Me.Label_Procedure.Name = "Label_Procedure"
        Me.Label_Procedure.Size = New System.Drawing.Size(106, 28)
        Me.Label_Procedure.TabIndex = 2
        Me.Label_Procedure.Text = "Test Type:"
        '
        'ComboBox_Procedure
        '
        Me.ComboBox_Procedure.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ComboBox_Procedure.FormattingEnabled = True
        Me.ComboBox_Procedure.Location = New System.Drawing.Point(143, 62)
        Me.ComboBox_Procedure.Name = "ComboBox_Procedure"
        Me.ComboBox_Procedure.Size = New System.Drawing.Size(280, 33)
        Me.ComboBox_Procedure.TabIndex = 22
        '
        'Label_Email
        '
        Me.Label_Email.AutoSize = True
        Me.Label_Email.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.Label_Email.Location = New System.Drawing.Point(88, 100)
        Me.Label_Email.Name = "Label_Email"
        Me.Label_Email.Size = New System.Drawing.Size(69, 28)
        Me.Label_Email.TabIndex = 4
        Me.Label_Email.Text = "Email:"
        '
        'TextBox_Email
        '
        Me.TextBox_Email.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBox_Email.Location = New System.Drawing.Point(143, 97)
        Me.TextBox_Email.Name = "TextBox_Email"
        Me.TextBox_Email.Size = New System.Drawing.Size(280, 31)
        Me.TextBox_Email.TabIndex = 23
        '
        'LoginForm
        '
        Me.AllowDrop = True
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.BackColor = System.Drawing.Color.LightGray
        Me.ClientSize = New System.Drawing.Size(470, 529)
        Me.Controls.Add(Me.GroupBox_SessionMetadata)
        Me.Controls.Add(Me.Button43)
        Me.Controls.Add(Me.Label4)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.KeyPreview = True
        Me.Name = "LoginForm"
        Me.Padding = New System.Windows.Forms.Padding(3)
        Me.Text = "User Login"
        Me.GroupBox_SessionMetadata.ResumeLayout(False)
        Me.GroupBox_SessionMetadata.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Button43 As System.Windows.Forms.Button
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents GroupBox_SessionMetadata As System.Windows.Forms.GroupBox
    Friend WithEvents Label_Group As System.Windows.Forms.Label
    Friend WithEvents ComboBox_Group As System.Windows.Forms.ComboBox
    Friend WithEvents Label_Procedure As System.Windows.Forms.Label
    Friend WithEvents ComboBox_Procedure As System.Windows.Forms.ComboBox
    Friend WithEvents Label_Email As System.Windows.Forms.Label
    Friend WithEvents TextBox_Email As System.Windows.Forms.TextBox
End Class
