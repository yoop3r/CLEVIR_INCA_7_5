<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FlashingStatus
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
        Me.ListBox1 = New System.Windows.Forms.ListBox()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.CheckBox2 = New System.Windows.Forms.CheckBox()
        Me.CheckBox3 = New System.Windows.Forms.CheckBox()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.RadioButton5 = New System.Windows.Forms.RadioButton()
        Me.RadioButton4 = New System.Windows.Forms.RadioButton()
        Me.RadioButton3 = New System.Windows.Forms.RadioButton()
        Me.RadioButton2 = New System.Windows.Forms.RadioButton()
        Me.RadioButton1 = New System.Windows.Forms.RadioButton()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.FolderBrowserDialog1 = New System.Windows.Forms.FolderBrowserDialog()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.ListBox2 = New System.Windows.Forms.ListBox()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.RadioButton8 = New System.Windows.Forms.RadioButton()
        Me.RadioButton7 = New System.Windows.Forms.RadioButton()
        Me.RadioButton6 = New System.Windows.Forms.RadioButton()
        Me.Button5 = New System.Windows.Forms.Button()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'ListBox1
        '
        Me.ListBox1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListBox1.FormattingEnabled = True
        Me.ListBox1.HorizontalScrollbar = True
        Me.ListBox1.Location = New System.Drawing.Point(12, 249)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(700, 277)
        Me.ListBox1.TabIndex = 0
        '
        'Button1
        '
        Me.Button1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.Location = New System.Drawing.Point(596, 532)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(116, 26)
        Me.Button1.TabIndex = 1
        Me.Button1.Text = "Exit"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'CheckBox2
        '
        Me.CheckBox2.AutoSize = True
        Me.CheckBox2.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CheckBox2.Location = New System.Drawing.Point(12, 169)
        Me.CheckBox2.Name = "CheckBox2"
        Me.CheckBox2.Size = New System.Drawing.Size(253, 17)
        Me.CheckBox2.TabIndex = 3
        Me.CheckBox2.Text = "Flash Controller(s) from New Workspace"
        Me.CheckBox2.UseVisualStyleBackColor = True
        '
        'CheckBox3
        '
        Me.CheckBox3.AutoSize = True
        Me.CheckBox3.Enabled = False
        Me.CheckBox3.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CheckBox3.Location = New System.Drawing.Point(12, 191)
        Me.CheckBox3.Name = "CheckBox3"
        Me.CheckBox3.Size = New System.Drawing.Size(263, 17)
        Me.CheckBox3.TabIndex = 4
        Me.CheckBox3.Text = "Login as Demo and Drive (No Code Clear)"
        Me.CheckBox3.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button2.Location = New System.Drawing.Point(466, 532)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(124, 26)
        Me.Button2.TabIndex = 5
        Me.Button2.Text = "Import"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.RadioButton5)
        Me.GroupBox1.Controls.Add(Me.RadioButton4)
        Me.GroupBox1.Controls.Add(Me.RadioButton3)
        Me.GroupBox1.Controls.Add(Me.RadioButton2)
        Me.GroupBox1.Controls.Add(Me.RadioButton1)
        Me.GroupBox1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox1.Location = New System.Drawing.Point(12, 12)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(700, 151)
        Me.GroupBox1.TabIndex = 7
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Type of Import"
        '
        'RadioButton5
        '
        Me.RadioButton5.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton5.Location = New System.Drawing.Point(6, 98)
        Me.RadioButton5.Name = "RadioButton5"
        Me.RadioButton5.Size = New System.Drawing.Size(679, 23)
        Me.RadioButton5.TabIndex = 4
        Me.RadioButton5.Text = "Import A2L / PTP for 3 processors and Create Workspace (High Content with FCM)"
        Me.RadioButton5.UseVisualStyleBackColor = True
        '
        'RadioButton4
        '
        Me.RadioButton4.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton4.Location = New System.Drawing.Point(6, 74)
        Me.RadioButton4.Name = "RadioButton4"
        Me.RadioButton4.Size = New System.Drawing.Size(694, 23)
        Me.RadioButton4.TabIndex = 3
        Me.RadioButton4.Text = "Import A2L / PTP for 2 processors and Create Workspace (High Content No FCM, or L" &
    "ow Content or ACP3 with FCM)"
        Me.RadioButton4.UseVisualStyleBackColor = True
        '
        'RadioButton3
        '
        Me.RadioButton3.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton3.Location = New System.Drawing.Point(6, 122)
        Me.RadioButton3.Name = "RadioButton3"
        Me.RadioButton3.Size = New System.Drawing.Size(679, 23)
        Me.RadioButton3.TabIndex = 2
        Me.RadioButton3.Text = "Import Workspace (Workspace matches vehicle instrumentation hardware)"
        Me.RadioButton3.UseVisualStyleBackColor = True
        '
        'RadioButton2
        '
        Me.RadioButton2.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton2.Location = New System.Drawing.Point(6, 48)
        Me.RadioButton2.Name = "RadioButton2"
        Me.RadioButton2.Size = New System.Drawing.Size(679, 23)
        Me.RadioButton2.TabIndex = 1
        Me.RadioButton2.Text = "Import A2L / PTP and Create Workspace (Low Content, ACP2, ACP3, ACP4 or FCM STAnd" &
    " Alone)"
        Me.RadioButton2.UseVisualStyleBackColor = True
        '
        'RadioButton1
        '
        Me.RadioButton1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.RadioButton1.Location = New System.Drawing.Point(6, 22)
        Me.RadioButton1.Name = "RadioButton1"
        Me.RadioButton1.Size = New System.Drawing.Size(679, 23)
        Me.RadioButton1.TabIndex = 0
        Me.RadioButton1.Text = "Import and Convert Workspace (CSAV2 Vehicles)"
        Me.RadioButton1.UseVisualStyleBackColor = True
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'Button3
        '
        Me.Button3.Enabled = False
        Me.Button3.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button3.Location = New System.Drawing.Point(12, 532)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(120, 26)
        Me.Button3.TabIndex = 8
        Me.Button3.Text = "Retry Flashing"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.CheckBox1.Location = New System.Drawing.Point(12, 213)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(231, 17)
        Me.CheckBox1.TabIndex = 9
        Me.CheckBox1.Text = "Configure For New Software Version"
        Me.CheckBox1.UseVisualStyleBackColor = True
        Me.CheckBox1.Visible = False
        '
        'Button4
        '
        Me.Button4.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button4.Location = New System.Drawing.Point(138, 532)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(322, 26)
        Me.Button4.TabIndex = 10
        Me.Button4.Text = "Select Existing Workspace in INCA to Flash"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'ListBox2
        '
        Me.ListBox2.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListBox2.FormattingEnabled = True
        Me.ListBox2.HorizontalScrollbar = True
        Me.ListBox2.Location = New System.Drawing.Point(12, 249)
        Me.ListBox2.Name = "ListBox2"
        Me.ListBox2.Size = New System.Drawing.Size(700, 277)
        Me.ListBox2.Sorted = True
        Me.ListBox2.TabIndex = 11
        Me.ListBox2.Visible = False
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.RadioButton8)
        Me.GroupBox2.Controls.Add(Me.RadioButton7)
        Me.GroupBox2.Controls.Add(Me.RadioButton6)
        Me.GroupBox2.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox2.Location = New System.Drawing.Point(537, 169)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(175, 74)
        Me.GroupBox2.TabIndex = 12
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "File Selection Method"
        '
        'RadioButton8
        '
        Me.RadioButton8.AutoSize = True
        Me.RadioButton8.Location = New System.Drawing.Point(7, 34)
        Me.RadioButton8.Name = "RadioButton8"
        Me.RadioButton8.Size = New System.Drawing.Size(169, 17)
        Me.RadioButton8.TabIndex = 2
        Me.RadioButton8.Text = "Semi-Automatic Selection"
        Me.RadioButton8.UseVisualStyleBackColor = True
        '
        'RadioButton7
        '
        Me.RadioButton7.AutoSize = True
        Me.RadioButton7.Checked = True
        Me.RadioButton7.Location = New System.Drawing.Point(7, 17)
        Me.RadioButton7.Name = "RadioButton7"
        Me.RadioButton7.Size = New System.Drawing.Size(138, 17)
        Me.RadioButton7.TabIndex = 1
        Me.RadioButton7.TabStop = True
        Me.RadioButton7.Text = "Automatic Selection"
        Me.RadioButton7.UseVisualStyleBackColor = True
        '
        'RadioButton6
        '
        Me.RadioButton6.AutoSize = True
        Me.RadioButton6.Location = New System.Drawing.Point(7, 51)
        Me.RadioButton6.Name = "RadioButton6"
        Me.RadioButton6.Size = New System.Drawing.Size(123, 17)
        Me.RadioButton6.TabIndex = 0
        Me.RadioButton6.Text = "Manual Selection"
        Me.RadioButton6.UseVisualStyleBackColor = True
        '
        'Button5
        '
        Me.Button5.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button5.Location = New System.Drawing.Point(331, 181)
        Me.Button5.Name = "Button5"
        Me.Button5.Size = New System.Drawing.Size(100, 26)
        Me.Button5.TabIndex = 13
        Me.Button5.Text = "Test"
        Me.Button5.UseVisualStyleBackColor = True
        Me.Button5.Visible = False
        '
        'FlashingStatus
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(715, 561)
        Me.Controls.Add(Me.Button5)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.ListBox2)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.CheckBox1)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.CheckBox3)
        Me.Controls.Add(Me.CheckBox2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.ListBox1)
        Me.Name = "FlashingStatus"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "IMPORT SOFTWARE AND CALS - FLASH - DRIVE!"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents ListBox1 As System.Windows.Forms.ListBox
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents CheckBox2 As System.Windows.Forms.CheckBox
    Friend WithEvents CheckBox3 As System.Windows.Forms.CheckBox
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents RadioButton2 As RadioButton
    Friend WithEvents RadioButton1 As RadioButton
    Friend WithEvents RadioButton3 As RadioButton
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents RadioButton4 As RadioButton
    Friend WithEvents FolderBrowserDialog1 As FolderBrowserDialog
    Friend WithEvents Button3 As Button
    Friend WithEvents CheckBox1 As CheckBox
    Friend WithEvents RadioButton5 As RadioButton
    Friend WithEvents Button4 As Button
    Friend WithEvents ListBox2 As ListBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents RadioButton7 As RadioButton
    Friend WithEvents RadioButton6 As RadioButton
    Friend WithEvents Button5 As Button
    Friend WithEvents RadioButton8 As RadioButton
End Class
