<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class LoginForm
    Inherits System.Windows.Forms.Form

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

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.Button43 = New System.Windows.Forms.Button()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.RadioButton4 = New System.Windows.Forms.RadioButton()
        Me.RadioButton3 = New System.Windows.Forms.RadioButton()
        Me.RadioButton2 = New System.Windows.Forms.RadioButton()
        Me.RadioButton1 = New System.Windows.Forms.RadioButton()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.CheckBox3 = New System.Windows.Forms.CheckBox()
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.CheckBox_LidarCapture = New System.Windows.Forms.CheckBox()
        Me.GroupBox_SessionMetadata = New System.Windows.Forms.GroupBox()
        Me.Label_Group = New System.Windows.Forms.Label()
        Me.ComboBox_Group = New System.Windows.Forms.ComboBox()
        Me.Label_Procedure = New System.Windows.Forms.Label()
        Me.ComboBox_Procedure = New System.Windows.Forms.ComboBox()
        Me.Label_Email = New System.Windows.Forms.Label()
        Me.TextBox_Email = New System.Windows.Forms.TextBox()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox_SessionMetadata.SuspendLayout()
        Me.SuspendLayout()
        '
        'Label4
        '
        Me.Label4.BackColor = System.Drawing.Color.White
        Me.Label4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label4.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.Location = New System.Drawing.Point(206, 452)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(343, 50)
        Me.Label4.TabIndex = 6
        Me.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Panel1
        '
        Me.Panel1.BackColor = System.Drawing.Color.WhiteSmoke
        Me.Panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel1.Font = New System.Drawing.Font("Segoe UI", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Panel1.Location = New System.Drawing.Point(10, 10)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(170, 492)
        Me.Panel1.TabIndex = 18
        '
        'Button43
        '
        Me.Button43.BackColor = System.Drawing.SystemColors.Control
        Me.Button43.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button43.ForeColor = System.Drawing.Color.Black
        Me.Button43.Location = New System.Drawing.Point(532, 10)
        Me.Button43.Name = "Button43"
        Me.Button43.Size = New System.Drawing.Size(100, 80)
        Me.Button43.TabIndex = 11
        Me.Button43.Text = "EXIT"
        Me.Button43.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.RadioButton4)
        Me.GroupBox1.Controls.Add(Me.RadioButton3)
        Me.GroupBox1.Controls.Add(Me.RadioButton2)
        Me.GroupBox1.Controls.Add(Me.RadioButton1)
        Me.GroupBox1.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox1.Location = New System.Drawing.Point(190, 15)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(334, 158)
        Me.GroupBox1.TabIndex = 12
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Signal Registration Mode"
        '
        'RadioButton4
        '
        Me.RadioButton4.AutoSize = True
        Me.RadioButton4.Enabled = False
        Me.RadioButton4.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.RadioButton4.Location = New System.Drawing.Point(16, 130)
        Me.RadioButton4.Name = "RadioButton4"
        Me.RadioButton4.Size = New System.Drawing.Size(235, 23)
        Me.RadioButton4.TabIndex = 3
        Me.RadioButton4.TabStop = True
        Me.RadioButton4.Text = "CREATE NEW FROM BLANK EXP"
        Me.RadioButton4.UseVisualStyleBackColor = True
        '
        'RadioButton3
        '
        Me.RadioButton3.AutoSize = True
        Me.RadioButton3.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton3.Location = New System.Drawing.Point(16, 95)
        Me.RadioButton3.Name = "RadioButton3"
        Me.RadioButton3.Size = New System.Drawing.Size(97, 23)
        Me.RadioButton3.TabIndex = 2
        Me.RadioButton3.TabStop = True
        Me.RadioButton3.Text = "GO/NOGO"
        Me.RadioButton3.UseVisualStyleBackColor = True
        '
        'RadioButton2
        '
        Me.RadioButton2.AutoSize = True
        Me.RadioButton2.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton2.Location = New System.Drawing.Point(15, 60)
        Me.RadioButton2.Name = "RadioButton2"
        Me.RadioButton2.Size = New System.Drawing.Size(91, 23)
        Me.RadioButton2.TabIndex = 1
        Me.RadioButton2.TabStop = True
        Me.RadioButton2.Text = "DISPLAYS"
        Me.RadioButton2.UseVisualStyleBackColor = True
        '
        'RadioButton1
        '
        Me.RadioButton1.AutoSize = True
        Me.RadioButton1.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton1.Location = New System.Drawing.Point(15, 25)
        Me.RadioButton1.Name = "RadioButton1"
        Me.RadioButton1.Size = New System.Drawing.Size(58, 23)
        Me.RadioButton1.TabIndex = 0
        Me.RadioButton1.TabStop = True
        Me.RadioButton1.Text = "FULL"
        Me.RadioButton1.UseVisualStyleBackColor = True
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CheckBox1.Location = New System.Drawing.Point(205, 185)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(216, 24)
        Me.CheckBox1.TabIndex = 13
        Me.CheckBox1.Text = "Save Calibration Snapshots"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'CheckBox3
        '
        Me.CheckBox3.AutoSize = True
        Me.CheckBox3.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CheckBox3.Location = New System.Drawing.Point(205, 218)
        Me.CheckBox3.Name = "CheckBox3"
        Me.CheckBox3.Size = New System.Drawing.Size(204, 24)
        Me.CheckBox3.TabIndex = 16
        Me.CheckBox3.Text = "Enable CANalyzer Record"
        Me.CheckBox3.UseVisualStyleBackColor = True
        '
        'ToolTip1
        '
        AddHandler Me.ToolTip1.Popup, AddressOf Me.ToolTip1_Popup
        '
        'CheckBox_LidarCapture
        '
        Me.CheckBox_LidarCapture.AutoSize = True
        Me.CheckBox_LidarCapture.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CheckBox_LidarCapture.Location = New System.Drawing.Point(205, 251)
        Me.CheckBox_LidarCapture.Name = "CheckBox_LidarCapture"
        Me.CheckBox_LidarCapture.Size = New System.Drawing.Size(181, 24)
        Me.CheckBox_LidarCapture.TabIndex = 10
        Me.CheckBox_LidarCapture.Text = "Enable LiDAR Capture"
        Me.CheckBox_LidarCapture.UseVisualStyleBackColor = True
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
        Me.GroupBox_SessionMetadata.Location = New System.Drawing.Point(190, 295)
        Me.GroupBox_SessionMetadata.Name = "GroupBox_SessionMetadata"
        Me.GroupBox_SessionMetadata.Size = New System.Drawing.Size(400, 140)
        Me.GroupBox_SessionMetadata.TabIndex = 20
        Me.GroupBox_SessionMetadata.TabStop = False
        Me.GroupBox_SessionMetadata.Text = "Session Details (Required)"
        '
        'Label_Group
        '
        Me.Label_Group.AutoSize = True
        Me.Label_Group.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.Label_Group.Location = New System.Drawing.Point(10, 30)
        Me.Label_Group.Name = "Label_Group"
        Me.Label_Group.Size = New System.Drawing.Size(97, 19)
        Me.Label_Group.TabIndex = 0
        Me.Label_Group.Text = "ADAS Group:"
        '
        'ComboBox_Group
        '
        Me.ComboBox_Group.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ComboBox_Group.FormattingEnabled = True
        Me.ComboBox_Group.Location = New System.Drawing.Point(110, 27)
        Me.ComboBox_Group.Name = "ComboBox_Group"
        Me.ComboBox_Group.Size = New System.Drawing.Size(280, 23)
        Me.ComboBox_Group.TabIndex = 21
        '
        'Label_Procedure
        '
        Me.Label_Procedure.AutoSize = True
        Me.Label_Procedure.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.Label_Procedure.Location = New System.Drawing.Point(10, 65)
        Me.Label_Procedure.Name = "Label_Procedure"
        Me.Label_Procedure.Size = New System.Drawing.Size(75, 19)
        Me.Label_Procedure.TabIndex = 2
        Me.Label_Procedure.Text = "Test Type:"
        '
        'ComboBox_Procedure
        '
        Me.ComboBox_Procedure.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ComboBox_Procedure.FormattingEnabled = True
        Me.ComboBox_Procedure.Location = New System.Drawing.Point(110, 62)
        Me.ComboBox_Procedure.Name = "ComboBox_Procedure"
        Me.ComboBox_Procedure.Size = New System.Drawing.Size(280, 23)
        Me.ComboBox_Procedure.TabIndex = 22
        '
        'Label_Email
        '
        Me.Label_Email.AutoSize = True
        Me.Label_Email.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.Label_Email.Location = New System.Drawing.Point(10, 100)
        Me.Label_Email.Name = "Label_Email"
        Me.Label_Email.Size = New System.Drawing.Size(49, 19)
        Me.Label_Email.TabIndex = 4
        Me.Label_Email.Text = "Email:"
        '
        'TextBox_Email
        '
        Me.TextBox_Email.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBox_Email.Location = New System.Drawing.Point(111, 97)
        Me.TextBox_Email.Name = "TextBox_Email"
        Me.TextBox_Email.Size = New System.Drawing.Size(280, 23)
        Me.TextBox_Email.TabIndex = 23
        '
        'LoginForm
        '
        Me.AllowDrop = True
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.BackColor = System.Drawing.Color.LightGray
        Me.ClientSize = New System.Drawing.Size(641, 529)
        Me.Controls.Add(Me.GroupBox_SessionMetadata)
        Me.Controls.Add(Me.CheckBox3)
        Me.Controls.Add(Me.CheckBox_LidarCapture)
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.Button43)
        Me.Controls.Add(Me.Panel1)
        Me.Controls.Add(Me.Label4)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow
        Me.KeyPreview = True
        Me.Name = "LoginForm"
        Me.Padding = New System.Windows.Forms.Padding(3)
        Me.ShowInTaskbar = False
        Me.Text = "User Login"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox_SessionMetadata.ResumeLayout(False)
        Me.GroupBox_SessionMetadata.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents Button43 As System.Windows.Forms.Button
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton1 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton3 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton2 As System.Windows.Forms.RadioButton
    Friend WithEvents CheckBox1 As System.Windows.Forms.CheckBox
    Friend WithEvents RadioButton4 As System.Windows.Forms.RadioButton
    Friend WithEvents CheckBox3 As System.Windows.Forms.CheckBox
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents CheckBox_LidarCapture As System.Windows.Forms.CheckBox
    Friend WithEvents GroupBox_SessionMetadata As System.Windows.Forms.GroupBox
    Friend WithEvents Label_Group As System.Windows.Forms.Label
    Friend WithEvents ComboBox_Group As System.Windows.Forms.ComboBox
    Friend WithEvents Label_Procedure As System.Windows.Forms.Label
    Friend WithEvents ComboBox_Procedure As System.Windows.Forms.ComboBox
    Friend WithEvents Label_Email As System.Windows.Forms.Label
    Friend WithEvents TextBox_Email As System.Windows.Forms.TextBox
End Class
