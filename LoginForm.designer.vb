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
        components = New Container()
        Label4 = New Label()
        Button43 = New Button()
        LinkLabel_WhatsNew = New LinkLabel()
        ToolTip1 = New ToolTip(components)
        GroupBox_SessionMetadata = New GroupBox()
        Label_Group = New Label()
        ComboBox_Group = New ComboBox()
        Label_Procedure = New Label()
        ComboBox_Procedure = New ComboBox()
        Label_Email = New Label()
        TextBox_Email = New TextBox()
        GroupBox_SessionMetadata.SuspendLayout()
        SuspendLayout()
        ' 
        ' Label4
        ' 
        Label4.BackColor = Color.White
        Label4.BorderStyle = BorderStyle.Fixed3D
        Label4.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label4.Location = New Point(27, 517)
        Label4.Name = "Label4"
        Label4.Size = New Size(408, 16)
        Label4.TabIndex = 6
        Label4.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Button43
        ' 
        Button43.BackColor = SystemColors.Control
        Button43.Font = New Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button43.ForeColor = Color.Black
        Button43.Location = New Point(354, 10)
        Button43.Name = "Button43"
        Button43.Size = New Size(100, 80)
        Button43.TabIndex = 11
        Button43.Text = "EXIT"
        Button43.UseVisualStyleBackColor = True
        ' 
        ' LinkLabel_WhatsNew
        ' 
        LinkLabel_WhatsNew.AutoSize = True
        LinkLabel_WhatsNew.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LinkLabel_WhatsNew.Location = New Point(12, 12)
        LinkLabel_WhatsNew.Name = "LinkLabel_WhatsNew"
        LinkLabel_WhatsNew.Size = New Size(75, 15)
        LinkLabel_WhatsNew.TabIndex = 30
        LinkLabel_WhatsNew.TabStop = True
        LinkLabel_WhatsNew.Text = "What's New?"
        ' 
        ' GroupBox_SessionMetadata
        ' 
        GroupBox_SessionMetadata.Controls.Add(Label_Group)
        GroupBox_SessionMetadata.Controls.Add(ComboBox_Group)
        GroupBox_SessionMetadata.Controls.Add(Label_Procedure)
        GroupBox_SessionMetadata.Controls.Add(ComboBox_Procedure)
        GroupBox_SessionMetadata.Controls.Add(Label_Email)
        GroupBox_SessionMetadata.Controls.Add(TextBox_Email)
        GroupBox_SessionMetadata.Font = New Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        GroupBox_SessionMetadata.Location = New Point(12, 348)
        GroupBox_SessionMetadata.Name = "GroupBox_SessionMetadata"
        GroupBox_SessionMetadata.Size = New Size(442, 140)
        GroupBox_SessionMetadata.TabIndex = 20
        GroupBox_SessionMetadata.TabStop = False
        GroupBox_SessionMetadata.Text = "Session Details (Required)"
        ' 
        ' Label_Group
        ' 
        Label_Group.AutoSize = True
        Label_Group.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        Label_Group.Location = New Point(39, 30)
        Label_Group.Name = "Label_Group"
        Label_Group.Size = New Size(97, 19)
        Label_Group.TabIndex = 0
        Label_Group.Text = "ADAS Group:"
        ' 
        ' ComboBox_Group
        ' 
        ComboBox_Group.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        ComboBox_Group.FormattingEnabled = True
        ComboBox_Group.Location = New Point(144, 27)
        ComboBox_Group.Name = "ComboBox_Group"
        ComboBox_Group.Size = New Size(280, 23)
        ComboBox_Group.TabIndex = 21
        ' 
        ' Label_Procedure
        ' 
        Label_Procedure.AutoSize = True
        Label_Procedure.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        Label_Procedure.Location = New Point(61, 65)
        Label_Procedure.Name = "Label_Procedure"
        Label_Procedure.Size = New Size(75, 19)
        Label_Procedure.TabIndex = 2
        Label_Procedure.Text = "Test Type:"
        ' 
        ' ComboBox_Procedure
        ' 
        ComboBox_Procedure.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        ComboBox_Procedure.FormattingEnabled = True
        ComboBox_Procedure.Location = New Point(143, 62)
        ComboBox_Procedure.Name = "ComboBox_Procedure"
        ComboBox_Procedure.Size = New Size(280, 23)
        ComboBox_Procedure.TabIndex = 22
        ' 
        ' Label_Email
        ' 
        Label_Email.AutoSize = True
        Label_Email.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        Label_Email.Location = New Point(88, 100)
        Label_Email.Name = "Label_Email"
        Label_Email.Size = New Size(49, 19)
        Label_Email.TabIndex = 4
        Label_Email.Text = "Email:"
        ' 
        ' TextBox_Email
        ' 
        TextBox_Email.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextBox_Email.Location = New Point(143, 97)
        TextBox_Email.Name = "TextBox_Email"
        TextBox_Email.Size = New Size(280, 23)
        TextBox_Email.TabIndex = 23
        ' 
        ' LoginForm
        ' 
        AllowDrop = True
        AutoScaleMode = AutoScaleMode.None
        BackColor = Color.LightGray
        ClientSize = New Size(470, 554)
        Controls.Add(GroupBox_SessionMetadata)
        Controls.Add(Button43)
        Controls.Add(Label4)
        Controls.Add(LinkLabel_WhatsNew)
        FormBorderStyle = FormBorderStyle.FixedToolWindow
        KeyPreview = True
        Name = "LoginForm"
        Padding = New Padding(3)
        Text = "User Login"
        GroupBox_SessionMetadata.ResumeLayout(False)
        GroupBox_SessionMetadata.PerformLayout()
        ResumeLayout(False)
        PerformLayout()

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
    Friend WithEvents LinkLabel_WhatsNew As System.Windows.Forms.LinkLabel
End Class
