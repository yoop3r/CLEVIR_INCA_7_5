<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class UploadDataScreen
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
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.RadioButton1 = New System.Windows.Forms.RadioButton()
        Me.RadioButton15 = New System.Windows.Forms.RadioButton()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.RadioButton10 = New System.Windows.Forms.RadioButton()
        Me.RadioButton7 = New System.Windows.Forms.RadioButton()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.CheckBox4 = New System.Windows.Forms.CheckBox()
        Me.CheckBox3 = New System.Windows.Forms.CheckBox()
        Me.CheckBox2 = New System.Windows.Forms.CheckBox()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.RadioButton11 = New System.Windows.Forms.RadioButton()
        Me.RadioButton9 = New System.Windows.Forms.RadioButton()
        Me.RadioButton6 = New System.Windows.Forms.RadioButton()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.RadioButton3 = New System.Windows.Forms.RadioButton()
        Me.RadioButton4 = New System.Windows.Forms.RadioButton()
        Me.GroupBox6 = New System.Windows.Forms.GroupBox()
        Me.RadioButton18 = New System.Windows.Forms.RadioButton()
        Me.RadioButton17 = New System.Windows.Forms.RadioButton()
        Me.RadioButton12 = New System.Windows.Forms.RadioButton()
        Me.RadioButton5 = New System.Windows.Forms.RadioButton()
        Me.FolderBrowserDialog1 = New System.Windows.Forms.FolderBrowserDialog()
        Me.GroupBox7 = New System.Windows.Forms.GroupBox()
        Me.RadioButton16 = New System.Windows.Forms.RadioButton()
        Me.ListBox1 = New System.Windows.Forms.ListBox()
        Me.RadioButton13 = New System.Windows.Forms.RadioButton()
        Me.RadioButton14 = New System.Windows.Forms.RadioButton()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.Button5 = New System.Windows.Forms.Button()
        Me.GroupBox8 = New System.Windows.Forms.GroupBox()
        Me.RadioButton8 = New System.Windows.Forms.RadioButton()
        Me.RadioButton2 = New System.Windows.Forms.RadioButton()
        Me.CheckBox5 = New System.Windows.Forms.CheckBox()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox6.SuspendLayout()
        Me.GroupBox7.SuspendLayout()
        Me.GroupBox8.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.GroupBox2)
        Me.GroupBox1.Controls.Add(Me.Button2)
        Me.GroupBox1.Controls.Add(Me.Button1)
        Me.GroupBox1.Controls.Add(Me.GroupBox5)
        Me.GroupBox1.Controls.Add(Me.GroupBox4)
        Me.GroupBox1.Controls.Add(Me.GroupBox3)
        Me.GroupBox1.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox1.Location = New System.Drawing.Point(2, 181)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(779, 357)
        Me.GroupBox1.TabIndex = 0
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "File Upload Parameters"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.RadioButton1)
        Me.GroupBox2.Controls.Add(Me.RadioButton15)
        Me.GroupBox2.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox2.Location = New System.Drawing.Point(436, 25)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(333, 110)
        Me.GroupBox2.TabIndex = 7
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "MF4 File Delete Prior to Upload"
        '
        'RadioButton1
        '
        Me.RadioButton1.AutoSize = True
        Me.RadioButton1.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton1.Location = New System.Drawing.Point(6, 64)
        Me.RadioButton1.Name = "RadioButton1"
        Me.RadioButton1.Size = New System.Drawing.Size(192, 28)
        Me.RadioButton1.TabIndex = 2
        Me.RadioButton1.TabStop = True
        Me.RadioButton1.Text = "Upload MF4 Files"
        Me.RadioButton1.UseVisualStyleBackColor = True
        '
        'RadioButton15
        '
        Me.RadioButton15.AutoSize = True
        Me.RadioButton15.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton15.Location = New System.Drawing.Point(6, 25)
        Me.RadioButton15.Name = "RadioButton15"
        Me.RadioButton15.Size = New System.Drawing.Size(325, 28)
        Me.RadioButton15.TabIndex = 0
        Me.RadioButton15.TabStop = True
        Me.RadioButton15.Text = "Delete MF4 Files Before Upload"
        Me.RadioButton15.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(606, 307)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(164, 41)
        Me.Button2.TabIndex = 6
        Me.Button2.Text = "Exit"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Button1
        '
        Me.Button1.Enabled = False
        Me.Button1.Location = New System.Drawing.Point(436, 307)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(164, 41)
        Me.Button1.TabIndex = 5
        Me.Button1.Text = "Upload Data"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.RadioButton10)
        Me.GroupBox5.Controls.Add(Me.RadioButton7)
        Me.GroupBox5.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox5.Location = New System.Drawing.Point(436, 141)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(333, 145)
        Me.GroupBox5.TabIndex = 4
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "File Deletion"
        '
        'RadioButton10
        '
        Me.RadioButton10.AutoSize = True
        Me.RadioButton10.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton10.Location = New System.Drawing.Point(6, 43)
        Me.RadioButton10.Name = "RadioButton10"
        Me.RadioButton10.Size = New System.Drawing.Size(235, 28)
        Me.RadioButton10.TabIndex = 2
        Me.RadioButton10.TabStop = True
        Me.RadioButton10.Text = "Delete Uploaded Files"
        Me.RadioButton10.UseVisualStyleBackColor = True
        '
        'RadioButton7
        '
        Me.RadioButton7.AutoSize = True
        Me.RadioButton7.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton7.Location = New System.Drawing.Point(6, 82)
        Me.RadioButton7.Name = "RadioButton7"
        Me.RadioButton7.Size = New System.Drawing.Size(321, 28)
        Me.RadioButton7.TabIndex = 1
        Me.RadioButton7.TabStop = True
        Me.RadioButton7.Text = "DO NOT Delete Uploaded Files"
        Me.RadioButton7.UseVisualStyleBackColor = True
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.CheckBox4)
        Me.GroupBox4.Controls.Add(Me.CheckBox3)
        Me.GroupBox4.Controls.Add(Me.CheckBox2)
        Me.GroupBox4.Controls.Add(Me.CheckBox1)
        Me.GroupBox4.Controls.Add(Me.RadioButton11)
        Me.GroupBox4.Controls.Add(Me.RadioButton9)
        Me.GroupBox4.Controls.Add(Me.RadioButton6)
        Me.GroupBox4.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox4.Location = New System.Drawing.Point(6, 141)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(410, 214)
        Me.GroupBox4.TabIndex = 2
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "File Types"
        '
        'CheckBox4
        '
        Me.CheckBox4.AutoSize = True
        Me.CheckBox4.Enabled = False
        Me.CheckBox4.Location = New System.Drawing.Point(208, 180)
        Me.CheckBox4.Name = "CheckBox4"
        Me.CheckBox4.Size = New System.Drawing.Size(107, 20)
        Me.CheckBox4.TabIndex = 7
        Me.CheckBox4.Text = "ANNO Files"
        Me.CheckBox4.UseVisualStyleBackColor = True
        '
        'CheckBox3
        '
        Me.CheckBox3.AutoSize = True
        Me.CheckBox3.Enabled = False
        Me.CheckBox3.Location = New System.Drawing.Point(208, 150)
        Me.CheckBox3.Name = "CheckBox3"
        Me.CheckBox3.Size = New System.Drawing.Size(98, 20)
        Me.CheckBox3.TabIndex = 6
        Me.CheckBox3.Text = "WAV Files"
        Me.CheckBox3.UseVisualStyleBackColor = True
        '
        'CheckBox2
        '
        Me.CheckBox2.AutoSize = True
        Me.CheckBox2.Enabled = False
        Me.CheckBox2.Location = New System.Drawing.Point(72, 180)
        Me.CheckBox2.Name = "CheckBox2"
        Me.CheckBox2.Size = New System.Drawing.Size(118, 20)
        Me.CheckBox2.TabIndex = 5
        Me.CheckBox2.Text = "Camera Files"
        Me.CheckBox2.UseVisualStyleBackColor = True
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Enabled = False
        Me.CheckBox1.Location = New System.Drawing.Point(72, 150)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(97, 20)
        Me.CheckBox1.TabIndex = 4
        Me.CheckBox1.Text = "Data Files"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'RadioButton11
        '
        Me.RadioButton11.AutoSize = True
        Me.RadioButton11.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton11.Location = New System.Drawing.Point(16, 64)
        Me.RadioButton11.Name = "RadioButton11"
        Me.RadioButton11.Size = New System.Drawing.Size(343, 28)
        Me.RadioButton11.TabIndex = 3
        Me.RadioButton11.TabStop = True
        Me.RadioButton11.Text = "Data Files and Front Camera Only"
        Me.RadioButton11.UseVisualStyleBackColor = True
        '
        'RadioButton9
        '
        Me.RadioButton9.AutoSize = True
        Me.RadioButton9.Font = New System.Drawing.Font("Microsoft Sans Serif", 15.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton9.Location = New System.Drawing.Point(16, 105)
        Me.RadioButton9.Name = "RadioButton9"
        Me.RadioButton9.Size = New System.Drawing.Size(239, 29)
        Me.RadioButton9.TabIndex = 2
        Me.RadioButton9.TabStop = True
        Me.RadioButton9.Text = "Selected File Types"
        Me.RadioButton9.UseVisualStyleBackColor = True
        '
        'RadioButton6
        '
        Me.RadioButton6.AutoSize = True
        Me.RadioButton6.Font = New System.Drawing.Font("Microsoft Sans Serif", 15.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton6.Location = New System.Drawing.Point(16, 25)
        Me.RadioButton6.Name = "RadioButton6"
        Me.RadioButton6.Size = New System.Drawing.Size(174, 29)
        Me.RadioButton6.TabIndex = 0
        Me.RadioButton6.TabStop = True
        Me.RadioButton6.Text = "All File Types"
        Me.RadioButton6.UseVisualStyleBackColor = True
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.RadioButton3)
        Me.GroupBox3.Controls.Add(Me.RadioButton4)
        Me.GroupBox3.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox3.Location = New System.Drawing.Point(6, 25)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(410, 110)
        Me.GroupBox3.TabIndex = 1
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Files"
        '
        'RadioButton3
        '
        Me.RadioButton3.AutoSize = True
        Me.RadioButton3.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton3.Location = New System.Drawing.Point(18, 66)
        Me.RadioButton3.Name = "RadioButton3"
        Me.RadioButton3.Size = New System.Drawing.Size(385, 28)
        Me.RadioButton3.TabIndex = 1
        Me.RadioButton3.TabStop = True
        Me.RadioButton3.Text = "Data Associated with Annotations Only"
        Me.RadioButton3.UseVisualStyleBackColor = True
        '
        'RadioButton4
        '
        Me.RadioButton4.AutoSize = True
        Me.RadioButton4.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton4.Location = New System.Drawing.Point(18, 25)
        Me.RadioButton4.Name = "RadioButton4"
        Me.RadioButton4.Size = New System.Drawing.Size(281, 28)
        Me.RadioButton4.TabIndex = 0
        Me.RadioButton4.TabStop = True
        Me.RadioButton4.Text = "All Files Of Selected Types"
        Me.RadioButton4.UseVisualStyleBackColor = True
        '
        'GroupBox6
        '
        Me.GroupBox6.Controls.Add(Me.RadioButton18)
        Me.GroupBox6.Controls.Add(Me.RadioButton17)
        Me.GroupBox6.Controls.Add(Me.RadioButton12)
        Me.GroupBox6.Controls.Add(Me.RadioButton5)
        Me.GroupBox6.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox6.Location = New System.Drawing.Point(8, 8)
        Me.GroupBox6.Name = "GroupBox6"
        Me.GroupBox6.Size = New System.Drawing.Size(360, 127)
        Me.GroupBox6.TabIndex = 1
        Me.GroupBox6.TabStop = False
        Me.GroupBox6.Text = "File Upload Destination"
        '
        'RadioButton18
        '
        Me.RadioButton18.Enabled = False
        Me.RadioButton18.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton18.Location = New System.Drawing.Point(12, 95)
        Me.RadioButton18.Name = "RadioButton18"
        Me.RadioButton18.Size = New System.Drawing.Size(343, 28)
        Me.RadioButton18.TabIndex = 3
        Me.RadioButton18.TabStop = True
        Me.RadioButton18.Text = "Upload HC Trailering Data to Default"
        Me.RadioButton18.UseVisualStyleBackColor = True
        '
        'RadioButton17
        '
        Me.RadioButton17.Enabled = False
        Me.RadioButton17.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton17.Location = New System.Drawing.Point(12, 70)
        Me.RadioButton17.Name = "RadioButton17"
        Me.RadioButton17.Size = New System.Drawing.Size(343, 28)
        Me.RadioButton17.TabIndex = 2
        Me.RadioButton17.TabStop = True
        Me.RadioButton17.Text = "Upload HC Ride Data to Default"
        Me.RadioButton17.UseVisualStyleBackColor = True
        '
        'RadioButton12
        '
        Me.RadioButton12.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton12.Location = New System.Drawing.Point(12, 45)
        Me.RadioButton12.Name = "RadioButton12"
        Me.RadioButton12.Size = New System.Drawing.Size(251, 28)
        Me.RadioButton12.TabIndex = 1
        Me.RadioButton12.TabStop = True
        Me.RadioButton12.Text = "Specify Upload Drive"
        Me.RadioButton12.UseVisualStyleBackColor = True
        '
        'RadioButton5
        '
        Me.RadioButton5.Enabled = False
        Me.RadioButton5.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton5.Location = New System.Drawing.Point(12, 20)
        Me.RadioButton5.Name = "RadioButton5"
        Me.RadioButton5.Size = New System.Drawing.Size(342, 29)
        Me.RadioButton5.TabIndex = 0
        Me.RadioButton5.TabStop = True
        Me.RadioButton5.Text = "Upload To Default Drive (Typically Q:)"
        Me.RadioButton5.UseVisualStyleBackColor = True
        '
        'GroupBox7
        '
        Me.GroupBox7.Controls.Add(Me.RadioButton16)
        Me.GroupBox7.Controls.Add(Me.ListBox1)
        Me.GroupBox7.Controls.Add(Me.RadioButton13)
        Me.GroupBox7.Controls.Add(Me.RadioButton14)
        Me.GroupBox7.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox7.Location = New System.Drawing.Point(374, 8)
        Me.GroupBox7.Name = "GroupBox7"
        Me.GroupBox7.Size = New System.Drawing.Size(395, 127)
        Me.GroupBox7.TabIndex = 2
        Me.GroupBox7.TabStop = False
        Me.GroupBox7.Text = "Users"
        '
        'RadioButton16
        '
        Me.RadioButton16.AutoSize = True
        Me.RadioButton16.Font = New System.Drawing.Font("Microsoft Sans Serif", 15.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton16.Location = New System.Drawing.Point(10, 91)
        Me.RadioButton16.Name = "RadioButton16"
        Me.RadioButton16.Size = New System.Drawing.Size(150, 29)
        Me.RadioButton16.TabIndex = 3
        Me.RadioButton16.Text = "Today Only"
        Me.RadioButton16.UseVisualStyleBackColor = True
        '
        'ListBox1
        '
        Me.ListBox1.FormattingEnabled = True
        Me.ListBox1.ItemHeight = 20
        Me.ListBox1.Location = New System.Drawing.Point(250, 16)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(137, 104)
        Me.ListBox1.TabIndex = 2
        '
        'RadioButton13
        '
        Me.RadioButton13.AutoSize = True
        Me.RadioButton13.Font = New System.Drawing.Font("Microsoft Sans Serif", 15.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton13.Location = New System.Drawing.Point(10, 55)
        Me.RadioButton13.Name = "RadioButton13"
        Me.RadioButton13.Size = New System.Drawing.Size(219, 29)
        Me.RadioButton13.TabIndex = 1
        Me.RadioButton13.Text = "Current User Only"
        Me.RadioButton13.UseVisualStyleBackColor = True
        '
        'RadioButton14
        '
        Me.RadioButton14.AutoSize = True
        Me.RadioButton14.Checked = True
        Me.RadioButton14.Font = New System.Drawing.Font("Microsoft Sans Serif", 15.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton14.Location = New System.Drawing.Point(10, 20)
        Me.RadioButton14.Name = "RadioButton14"
        Me.RadioButton14.Size = New System.Drawing.Size(125, 29)
        Me.RadioButton14.TabIndex = 0
        Me.RadioButton14.TabStop = True
        Me.RadioButton14.Text = "All Users"
        Me.RadioButton14.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button3.Location = New System.Drawing.Point(710, 141)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(71, 41)
        Me.Button3.TabIndex = 8
        Me.Button3.Text = "Exit"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'Button4
        '
        Me.Button4.Enabled = False
        Me.Button4.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button4.Location = New System.Drawing.Point(583, 141)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(121, 41)
        Me.Button4.TabIndex = 7
        Me.Button4.Text = "Upload Data"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'Button5
        '
        Me.Button5.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button5.Location = New System.Drawing.Point(463, 141)
        Me.Button5.Name = "Button5"
        Me.Button5.Size = New System.Drawing.Size(114, 41)
        Me.Button5.TabIndex = 9
        Me.Button5.Text = "Advanced"
        Me.Button5.UseVisualStyleBackColor = True
        Me.Button5.Visible = False
        '
        'GroupBox8
        '
        Me.GroupBox8.Controls.Add(Me.RadioButton8)
        Me.GroupBox8.Controls.Add(Me.RadioButton2)
        Me.GroupBox8.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox8.Location = New System.Drawing.Point(12, 141)
        Me.GroupBox8.Name = "GroupBox8"
        Me.GroupBox8.Size = New System.Drawing.Size(250, 41)
        Me.GroupBox8.TabIndex = 10
        Me.GroupBox8.TabStop = False
        Me.GroupBox8.Text = "Network Connection"
        '
        'RadioButton8
        '
        Me.RadioButton8.AutoSize = True
        Me.RadioButton8.Location = New System.Drawing.Point(153, 15)
        Me.RadioButton8.Name = "RadioButton8"
        Me.RadioButton8.Size = New System.Drawing.Size(81, 20)
        Me.RadioButton8.TabIndex = 1
        Me.RadioButton8.Text = "GM LAN"
        Me.RadioButton8.UseVisualStyleBackColor = True
        '
        'RadioButton2
        '
        Me.RadioButton2.AutoSize = True
        Me.RadioButton2.Location = New System.Drawing.Point(25, 15)
        Me.RadioButton2.Name = "RadioButton2"
        Me.RadioButton2.Size = New System.Drawing.Size(86, 20)
        Me.RadioButton2.TabIndex = 0
        Me.RadioButton2.Text = "Wireless"
        Me.RadioButton2.UseVisualStyleBackColor = True
        '
        'CheckBox5
        '
        Me.CheckBox5.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CheckBox5.Location = New System.Drawing.Point(339, 136)
        Me.CheckBox5.Name = "CheckBox5"
        Me.CheckBox5.Size = New System.Drawing.Size(118, 53)
        Me.CheckBox5.TabIndex = 11
        Me.CheckBox5.Text = "Exit Windows After Upload"
        Me.CheckBox5.UseVisualStyleBackColor = True
        '
        'UploadDataScreen
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(786, 541)
        Me.ControlBox = False
        Me.Controls.Add(Me.CheckBox5)
        Me.Controls.Add(Me.GroupBox8)
        Me.Controls.Add(Me.Button5)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.GroupBox7)
        Me.Controls.Add(Me.GroupBox6)
        Me.Controls.Add(Me.GroupBox1)
        Me.Name = "UploadDataScreen"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Data Upload"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox6.ResumeLayout(False)
        Me.GroupBox7.ResumeLayout(False)
        Me.GroupBox7.PerformLayout()
        Me.GroupBox8.ResumeLayout(False)
        Me.GroupBox8.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents GroupBox4 As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton6 As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBox3 As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton3 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton4 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton9 As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBox5 As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton10 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton7 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton11 As System.Windows.Forms.RadioButton
    Friend WithEvents CheckBox4 As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBox3 As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBox2 As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBox1 As System.Windows.Forms.CheckBox
    Friend WithEvents GroupBox6 As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton12 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton5 As System.Windows.Forms.RadioButton
    Friend WithEvents FolderBrowserDialog1 As System.Windows.Forms.FolderBrowserDialog
    Friend WithEvents GroupBox7 As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton13 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton14 As System.Windows.Forms.RadioButton
    Friend WithEvents ListBox1 As System.Windows.Forms.ListBox
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton1 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton15 As System.Windows.Forms.RadioButton
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents Button4 As System.Windows.Forms.Button
    Friend WithEvents Button5 As System.Windows.Forms.Button
    Friend WithEvents GroupBox8 As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButton8 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton2 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton16 As System.Windows.Forms.RadioButton
    Friend WithEvents CheckBox5 As CheckBox
    Friend WithEvents RadioButton17 As RadioButton
    Friend WithEvents RadioButton18 As RadioButton
End Class
