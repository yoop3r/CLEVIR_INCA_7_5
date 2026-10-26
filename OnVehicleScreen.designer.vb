<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class OnVehicleScreen
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

    ' ================================================================
    ' LiDAR Status UI Controls
    ' ================================================================
    Friend WithEvents GroupBox_LidarStatus As System.Windows.Forms.GroupBox
    Friend WithEvents Label_LidarStatus As System.Windows.Forms.Label
    ' These are no longer needed in the main UI (detail form has the stats)
    ' Friend WithEvents Label_Lidar1Stats As System.Windows.Forms.Label
    ' Friend WithEvents Label_Lidar2Stats As System.Windows.Forms.Label
    Friend WithEvents Timer_LidarStats As System.Windows.Forms.Timer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As ComponentResourceManager = New ComponentResourceManager(GetType(OnVehicleScreen))
        Button6 = New Button()
        Button14 = New Button()
        Button1 = New Button()
        GroupBox6 = New GroupBox()
        ListBox3 = New ListBox()
        Label6 = New Label()
        Button23 = New Button()
        Label5 = New Label()
        ComboBox1 = New ComboBox()
        Label9 = New Label()
        Label8 = New Label()
        Label7 = New Label()
        GroupBox1 = New GroupBox()
        Label2 = New Label()
        GroupBox_LidarStatus = New GroupBox()
        Label_LidarStatus = New Label()
        ListBox1 = New ListBox()
        GroupBox4 = New GroupBox()
        Button2 = New Button()
        TextBox1 = New TextBox()
        Button3 = New Button()
        Button4 = New Button()
        Label17 = New Label()
        Button5 = New Button()
        Label1 = New Label()
        PictureBox1 = New PictureBox()
        Button7 = New Button()
        GroupBox2_PageSwitch = New GroupBox()
        RadioButton2 = New RadioButton()
        RadioButton1 = New RadioButton()
        Button8 = New Button()
        Label3 = New Label()
        GroupBox3 = New GroupBox()
        ListBox2 = New ListBox()
        GroupBox5 = New GroupBox()
        ListBox4 = New ListBox()
        OpenFileDialog1 = New OpenFileDialog()
        CheckBox1 = New CheckBox()
        Button9 = New Button()
        Button10 = New Button()
        GroupBox6.SuspendLayout()
        GroupBox1.SuspendLayout()
        GroupBox_LidarStatus.SuspendLayout()
        CType(PictureBox1, ISupportInitialize).BeginInit()
        GroupBox2_PageSwitch.SuspendLayout()
        GroupBox3.SuspendLayout()
        GroupBox5.SuspendLayout()
        SuspendLayout()
        ' 
        ' Button6
        ' 
        Button6.FlatStyle = FlatStyle.System
        Button6.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button6.ForeColor = Color.Black
        Button6.Location = New Point(95, 35)
        Button6.Name = "Button6"
        Button6.Size = New Size(80, 67)
        Button6.TabIndex = 47
        Button6.Text = "START MEASUREMENT"
        Button6.UseVisualStyleBackColor = True
        ' 
        ' Button14
        ' 
        Button14.FlatStyle = FlatStyle.System
        Button14.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button14.ForeColor = Color.Black
        Button14.Location = New Point(182, 35)
        Button14.Name = "Button14"
        Button14.Size = New Size(80, 67)
        Button14.TabIndex = 48
        Button14.Text = "START RECORD"
        Button14.UseVisualStyleBackColor = True
        ' 
        ' Button1
        ' 
        Button1.FlatStyle = FlatStyle.System
        Button1.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button1.ForeColor = Color.Black
        Button1.Location = New Point(710, 35)
        Button1.Name = "Button1"
        Button1.Size = New Size(80, 67)
        Button1.TabIndex = 60
        Button1.Text = "EXIT"
        Button1.UseVisualStyleBackColor = True
        ' 
        ' GroupBox6
        ' 
        GroupBox6.AutoSize = True
        GroupBox6.AutoSizeMode = AutoSizeMode.GrowAndShrink
        GroupBox6.BackgroundImageLayout = ImageLayout.None
        GroupBox6.Controls.Add(ListBox3)
        GroupBox6.FlatStyle = FlatStyle.System
        GroupBox6.Location = New Point(600, 215)
        GroupBox6.Name = "GroupBox6"
        GroupBox6.Size = New Size(190, 67)
        GroupBox6.TabIndex = 92
        GroupBox6.TabStop = False
        GroupBox6.Text = "Voice Commands"
        GroupBox6.Visible = False
        ' 
        ' ListBox3
        ' 
        ListBox3.Font = New Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ListBox3.FormattingEnabled = True
        ListBox3.Location = New Point(0, 25)
        ListBox3.Name = "ListBox3"
        ListBox3.Size = New Size(184, 20)
        ListBox3.TabIndex = 61
        ' 
        ' Label6
        ' 
        Label6.BackColor = Color.Transparent
        Label6.Font = New Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label6.ForeColor = Color.Transparent
        Label6.ImageAlign = ContentAlignment.MiddleRight
        Label6.Location = New Point(600, 106)
        Label6.Margin = New Padding(0)
        Label6.Name = "Label6"
        Label6.Size = New Size(190, 26)
        Label6.TabIndex = 63
        Label6.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Button23
        ' 
        Button23.Enabled = False
        Button23.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button23.ForeColor = Color.Black
        Button23.Location = New Point(536, 70)
        Button23.Name = "Button23"
        Button23.Size = New Size(80, 32)
        Button23.TabIndex = 62
        Button23.Text = "VOICE"
        Button23.UseVisualStyleBackColor = True
        ' 
        ' Label5
        ' 
        Label5.BorderStyle = BorderStyle.Fixed3D
        Label5.Font = New Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label5.ImageAlign = ContentAlignment.MiddleRight
        Label5.Location = New Point(95, 6)
        Label5.Name = "Label5"
        Label5.Size = New Size(347, 25)
        Label5.TabIndex = 66
        Label5.TextAlign = ContentAlignment.MiddleLeft
        Label5.UseCompatibleTextRendering = True
        ' 
        ' ComboBox1
        ' 
        ComboBox1.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold)
        ComboBox1.FormattingEnabled = True
        ComboBox1.Items.AddRange(New Object() {"1", "2", "3", "4", "5", "10", "15", "20"})
        ComboBox1.Location = New Point(277, 105)
        ComboBox1.Name = "ComboBox1"
        ComboBox1.Size = New Size(50, 21)
        ComboBox1.TabIndex = 70
        ' 
        ' Label9
        ' 
        Label9.BackColor = Color.Transparent
        Label9.Font = New Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label9.ImageAlign = ContentAlignment.MiddleRight
        Label9.Location = New Point(399, 104)
        Label9.Margin = New Padding(0)
        Label9.Name = "Label9"
        Label9.Size = New Size(158, 25)
        Label9.TabIndex = 69
        Label9.Text = "Time In Recording"
        Label9.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' Label8
        ' 
        Label8.BackColor = Color.WhiteSmoke
        Label8.BorderStyle = BorderStyle.Fixed3D
        Label8.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label8.Location = New Point(561, 106)
        Label8.Name = "Label8"
        Label8.Size = New Size(42, 22)
        Label8.TabIndex = 68
        Label8.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Label7
        ' 
        Label7.BackColor = Color.Transparent
        Label7.Font = New Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label7.ImageAlign = ContentAlignment.MiddleRight
        Label7.Location = New Point(89, 104)
        Label7.Margin = New Padding(0)
        Label7.Name = "Label7"
        Label7.Size = New Size(187, 25)
        Label7.TabIndex = 67
        Label7.Text = "Set Record Duration (Minutes)"
        Label7.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' GroupBox1
        ' 
        GroupBox1.BackColor = SystemColors.Control
        GroupBox1.Controls.Add(Label2)
        GroupBox1.Location = New Point(435, 493)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(800, 610)
        GroupBox1.TabIndex = 71
        GroupBox1.TabStop = False
        ' 
        ' Label2
        ' 
        Label2.Font = New Font("Microsoft Sans Serif", 14.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label2.ForeColor = SystemColors.ControlText
        Label2.Location = New Point(0, 277)
        Label2.Name = "Label2"
        Label2.Size = New Size(800, 73)
        Label2.TabIndex = 28
        Label2.Text = "Label2"
        Label2.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' GroupBox_LidarStatus
        ' 
        GroupBox_LidarStatus.BackColor = Color.Transparent
        GroupBox_LidarStatus.Controls.Add(Label_LidarStatus)
        GroupBox_LidarStatus.Font = New Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        GroupBox_LidarStatus.Location = New Point(268, 36)
        GroupBox_LidarStatus.Name = "GroupBox_LidarStatus"
        GroupBox_LidarStatus.Size = New Size(173, 59)
        GroupBox_LidarStatus.TabIndex = 100
        GroupBox_LidarStatus.TabStop = False
        GroupBox_LidarStatus.Text = "Status"
        GroupBox_LidarStatus.Visible = False
        ' 
        ' Label_LidarStatus
        ' 
        Label_LidarStatus.BorderStyle = BorderStyle.Fixed3D
        Label_LidarStatus.Cursor = Cursors.Hand
        Label_LidarStatus.Font = New Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Label_LidarStatus.ForeColor = Color.Black
        Label_LidarStatus.Location = New Point(6, 21)
        Label_LidarStatus.Name = "Label_LidarStatus"
        Label_LidarStatus.Size = New Size(161, 25)
        Label_LidarStatus.TabIndex = 0
        Label_LidarStatus.Text = "LiDAR (0) - Stopped"
        Label_LidarStatus.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' ListBox1
        ' 
        ListBox1.FormattingEnabled = True
        ListBox1.Location = New Point(28, 354)
        ListBox1.Name = "ListBox1"
        ListBox1.Size = New Size(694, 394)
        ListBox1.TabIndex = 29
        ListBox1.Visible = False
        ' 
        ' GroupBox4
        ' 
        GroupBox4.BackColor = Color.WhiteSmoke
        GroupBox4.Font = New Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        GroupBox4.ForeColor = Color.Black
        GroupBox4.Location = New Point(5, 129)
        GroupBox4.Name = "GroupBox4"
        GroupBox4.Size = New Size(785, 75)
        GroupBox4.TabIndex = 72
        GroupBox4.TabStop = False
        ' 
        ' Button2
        ' 
        Button2.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button2.ForeColor = Color.Black
        Button2.Location = New Point(622, 35)
        Button2.Name = "Button2"
        Button2.Size = New Size(80, 32)
        Button2.TabIndex = 73
        Button2.Text = "CONFIG"
        Button2.UseVisualStyleBackColor = True
        ' 
        ' TextBox1
        ' 
        TextBox1.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold)
        TextBox1.Location = New Point(28, 56)
        TextBox1.Name = "TextBox1"
        TextBox1.Size = New Size(39, 20)
        TextBox1.TabIndex = 74
        TextBox1.TextAlign = HorizontalAlignment.Center
        TextBox1.Visible = False
        ' 
        ' Button3
        ' 
        Button3.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button3.ForeColor = Color.Black
        Button3.Location = New Point(447, 71)
        Button3.Name = "Button3"
        Button3.Size = New Size(80, 32)
        Button3.TabIndex = 75
        Button3.Text = "DISPLAYS"
        Button3.UseVisualStyleBackColor = True
        ' 
        ' Button4
        ' 
        Button4.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button4.ForeColor = Color.Black
        Button4.Location = New Point(622, 70)
        Button4.Name = "Button4"
        Button4.Size = New Size(80, 32)
        Button4.TabIndex = 76
        Button4.Text = "LOGIN"
        Button4.UseVisualStyleBackColor = True
        ' 
        ' Label17
        ' 
        Label17.Font = New Font("Microsoft Sans Serif", 21.0F, FontStyle.Bold, GraphicsUnit.Pixel, CByte(0))
        Label17.Location = New Point(705, 35)
        Label17.Name = "Label17"
        Label17.Size = New Size(85, 67)
        Label17.TabIndex = 77
        Label17.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Button5
        ' 
        Button5.Enabled = False
        Button5.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button5.ForeColor = Color.Black
        Button5.Location = New Point(535, 69)
        Button5.Name = "Button5"
        Button5.Size = New Size(80, 32)
        Button5.TabIndex = 78
        Button5.Text = "SAVE CAL"
        Button5.UseVisualStyleBackColor = True
        Button5.Visible = False
        ' 
        ' Label1
        ' 
        Label1.BorderStyle = BorderStyle.Fixed3D
        Label1.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label1.Location = New Point(710, 6)
        Label1.Name = "Label1"
        Label1.Size = New Size(80, 23)
        Label1.TabIndex = 79
        Label1.TextAlign = ContentAlignment.MiddleCenter
        Label1.Visible = False
        ' 
        ' PictureBox1
        ' 
        PictureBox1.BackColor = Color.Red
        PictureBox1.BorderStyle = BorderStyle.Fixed3D
        PictureBox1.Image = CType(resources.GetObject("PictureBox1.Image"), Image)
        PictureBox1.Location = New Point(5, 6)
        PictureBox1.Name = "PictureBox1"
        PictureBox1.Size = New Size(84, 80)
        PictureBox1.SizeMode = PictureBoxSizeMode.CenterImage
        PictureBox1.TabIndex = 38
        PictureBox1.TabStop = False
        ' 
        ' Button7
        ' 
        Button7.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button7.ForeColor = Color.Black
        Button7.Location = New Point(447, 35)
        Button7.Name = "Button7"
        Button7.Size = New Size(80, 33)
        Button7.TabIndex = 80
        Button7.Text = "CAMERA"
        Button7.UseVisualStyleBackColor = True
        ' 
        ' GroupBox2_PageSwitch
        ' 
        GroupBox2_PageSwitch.BackColor = Color.Transparent
        GroupBox2_PageSwitch.Controls.Add(RadioButton2)
        GroupBox2_PageSwitch.Controls.Add(RadioButton1)
        GroupBox2_PageSwitch.Font = New Font("Microsoft Sans Serif", 8F, FontStyle.Bold)
        GroupBox2_PageSwitch.Location = New Point(532, -5)
        GroupBox2_PageSwitch.Name = "GroupBox2_PageSwitch"
        GroupBox2_PageSwitch.Size = New Size(106, 40)
        GroupBox2_PageSwitch.TabIndex = 81
        GroupBox2_PageSwitch.TabStop = False
        ' 
        ' RadioButton2
        ' 
        RadioButton2.Location = New Point(60, 12)
        RadioButton2.Name = "RadioButton2"
        RadioButton2.Size = New Size(56, 26)
        RadioButton2.TabIndex = 1
        RadioButton2.TabStop = True
        RadioButton2.Text = "RP"
        RadioButton2.UseVisualStyleBackColor = True
        RadioButton2.Visible = False
        ' 
        ' RadioButton1
        ' 
        RadioButton1.Location = New Point(12, 12)
        RadioButton1.Name = "RadioButton1"
        RadioButton1.Size = New Size(59, 26)
        RadioButton1.TabIndex = 0
        RadioButton1.TabStop = True
        RadioButton1.Text = "WP"
        RadioButton1.UseVisualStyleBackColor = True
        RadioButton1.Visible = False
        ' 
        ' Button8
        ' 
        Button8.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button8.Location = New Point(5, 90)
        Button8.Name = "Button8"
        Button8.Size = New Size(84, 40)
        Button8.TabIndex = 82
        Button8.Text = "CALIBRATE"
        Button8.UseVisualStyleBackColor = True
        Button8.Visible = False
        ' 
        ' Label3
        ' 
        Label3.BackColor = SystemColors.ActiveBorder
        Label3.BorderStyle = BorderStyle.Fixed3D
        Label3.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label3.Location = New Point(645, 6)
        Label3.Name = "Label3"
        Label3.Size = New Size(62, 23)
        Label3.TabIndex = 84
        Label3.Text = "Alt Rcrd"
        Label3.TextAlign = ContentAlignment.MiddleCenter
        Label3.Visible = False
        ' 
        ' GroupBox3
        ' 
        GroupBox3.AutoSize = True
        GroupBox3.AutoSizeMode = AutoSizeMode.GrowAndShrink
        GroupBox3.Controls.Add(ListBox2)
        GroupBox3.FlatStyle = FlatStyle.System
        GroupBox3.Location = New Point(302, 213)
        GroupBox3.Name = "GroupBox3"
        GroupBox3.Size = New Size(131, 52)
        GroupBox3.TabIndex = 85
        GroupBox3.TabStop = False
        GroupBox3.Text = "Cameras"
        GroupBox3.Visible = False
        ' 
        ' ListBox2
        ' 
        ListBox2.Font = New Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ListBox2.FormattingEnabled = True
        ListBox2.Location = New Point(0, 26)
        ListBox2.Name = "ListBox2"
        ListBox2.Size = New Size(125, 4)
        ListBox2.TabIndex = 0
        ' 
        ' GroupBox5
        ' 
        GroupBox5.AutoSize = True
        GroupBox5.AutoSizeMode = AutoSizeMode.GrowAndShrink
        GroupBox5.BackgroundImageLayout = ImageLayout.None
        GroupBox5.Controls.Add(ListBox4)
        GroupBox5.FlatStyle = FlatStyle.System
        GroupBox5.Font = New Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        GroupBox5.Location = New Point(129, 213)
        GroupBox5.Name = "GroupBox5"
        GroupBox5.Size = New Size(72, 109)
        GroupBox5.TabIndex = 88
        GroupBox5.TabStop = False
        GroupBox5.Text = "SubCategory"
        GroupBox5.Visible = False
        ' 
        ' ListBox4
        ' 
        ListBox4.DrawMode = DrawMode.OwnerDrawFixed
        ListBox4.Font = New Font("Microsoft Sans Serif", 14F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ListBox4.FormattingEnabled = True
        ListBox4.ItemHeight = 50
        ListBox4.Location = New Point(6, 33)
        ListBox4.Name = "ListBox4"
        ListBox4.Size = New Size(60, 54)
        ListBox4.TabIndex = 3
        ' 
        ' OpenFileDialog1
        ' 
        OpenFileDialog1.FileName = "OpenFileDialog1"
        ' 
        ' CheckBox1
        ' 
        CheckBox1.AutoSize = True
        CheckBox1.BackColor = Color.Transparent
        CheckBox1.Font = New Font("Microsoft Sans Serif", 8F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBox1.Location = New Point(611, 109)
        CheckBox1.Name = "CheckBox1"
        CheckBox1.Size = New Size(135, 17)
        CheckBox1.TabIndex = 89
        CheckBox1.Text = "Enable DTC Debug"
        CheckBox1.UseVisualStyleBackColor = False
        CheckBox1.Visible = False
        ' 
        ' Button9
        ' 
        Button9.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button9.ForeColor = Color.Black
        Button9.Location = New Point(447, 6)
        Button9.Name = "Button9"
        Button9.Size = New Size(80, 26)
        Button9.TabIndex = 90
        Button9.Text = "Proc Esc"
        Button9.UseVisualStyleBackColor = True
        Button9.Visible = False
        ' 
        ' Button10
        ' 
        Button10.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button10.ForeColor = Color.Black
        Button10.Location = New Point(535, 35)
        Button10.Name = "Button10"
        Button10.Size = New Size(80, 32)
        Button10.TabIndex = 91
        Button10.Text = "T/D VIEW"
        Button10.UseVisualStyleBackColor = True
        ' 
        ' OnVehicleScreen
        ' 
        AllowDrop = True
        AutoScaleMode = AutoScaleMode.None
        AutoSizeMode = AutoSizeMode.GrowAndShrink
        AutoValidate = AutoValidate.EnableAllowFocusChange
        BackColor = SystemColors.Control
        ClientSize = New Size(804, 581)
        Controls.Add(GroupBox_LidarStatus)
        Controls.Add(Label5)
        Controls.Add(Button9)
        Controls.Add(GroupBox5)
        Controls.Add(ListBox1)
        Controls.Add(CheckBox1)
        Controls.Add(TextBox1)
        Controls.Add(GroupBox3)
        Controls.Add(GroupBox6)
        Controls.Add(Label3)
        Controls.Add(Button8)
        Controls.Add(GroupBox2_PageSwitch)
        Controls.Add(Label1)
        Controls.Add(GroupBox1)
        Controls.Add(Button2)
        Controls.Add(Label8)
        Controls.Add(Label7)
        Controls.Add(Label6)
        Controls.Add(Button23)
        Controls.Add(Button1)
        Controls.Add(Button14)
        Controls.Add(Button6)
        Controls.Add(PictureBox1)
        Controls.Add(GroupBox4)
        Controls.Add(Button3)
        Controls.Add(Button4)
        Controls.Add(Button5)
        Controls.Add(Label17)
        Controls.Add(Button7)
        Controls.Add(ComboBox1)
        Controls.Add(Label9)
        Controls.Add(Button10)
        FormBorderStyle = FormBorderStyle.FixedSingle
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        KeyPreview = True
        Name = "OnVehicleScreen"
        Text = "OnVehicleScreen"
        GroupBox6.ResumeLayout(False)
        GroupBox1.ResumeLayout(False)
        GroupBox_LidarStatus.ResumeLayout(False)
        CType(PictureBox1, ISupportInitialize).EndInit()
        GroupBox2_PageSwitch.ResumeLayout(False)
        GroupBox3.ResumeLayout(False)
        GroupBox5.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()

    End Sub
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents ListBox3 As System.Windows.Forms.ListBox
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Label9 As System.Windows.Forms.Label
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents ListBox1 As System.Windows.Forms.ListBox
    Friend WithEvents Label2 As Label
    Friend WithEvents GroupBox4 As System.Windows.Forms.GroupBox
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents TextBox1 As System.Windows.Forms.TextBox
    Public WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Public WithEvents Button6 As System.Windows.Forms.Button
    Public WithEvents Button14 As System.Windows.Forms.Button
    Public WithEvents Button23 As System.Windows.Forms.Button
    Public WithEvents Button1 As System.Windows.Forms.Button
    Public WithEvents Button4 As System.Windows.Forms.Button
    Public WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents Label17 As System.Windows.Forms.Label
    Public WithEvents Button5 As System.Windows.Forms.Button
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Button7 As System.Windows.Forms.Button
    Friend WithEvents GroupBox2_PageSwitch As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton2 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton1 As System.Windows.Forms.RadioButton
    Friend WithEvents Button8 As System.Windows.Forms.Button
    Friend WithEvents Label3 As Label
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents ListBox2 As ListBox
    Friend WithEvents GroupBox5 As GroupBox
    Friend WithEvents ListBox4 As ListBox
    Friend WithEvents GroupBox6 As GroupBox
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents CheckBox1 As CheckBox
    Friend WithEvents Button9 As Button
    Friend WithEvents Button10 As Button
End Class
