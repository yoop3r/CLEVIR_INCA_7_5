<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class AnnotationInterfaceConfigure
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
        Me.components = New System.ComponentModel.Container()
        Me.ListBox1 = New System.Windows.Forms.ListBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.ListBox2 = New System.Windows.Forms.ListBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.ListBox3 = New System.Windows.Forms.ListBox()
        Me.ListBox4_Annotations = New System.Windows.Forms.ListBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.MenuStrip1 = New System.Windows.Forms.MenuStrip()
        Me.ExitToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.ToolStripMenuItem3 = New System.Windows.Forms.ToolStripMenuItem()
        Me.SaveAndUpdateToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.UpdateAndSaveAsToolStripMenuItem = New System.Windows.Forms.ToolStripMenuItem()
        Me.ExitToolStripMenuItem1 = New System.Windows.Forms.ToolStripMenuItem()
        Me.SaveFileDialog1 = New System.Windows.Forms.SaveFileDialog()
        Me.OpenFileDialog1 = New System.Windows.Forms.OpenFileDialog()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.ListBox5 = New System.Windows.Forms.ListBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.MenuStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'ListBox1
        '
        Me.ListBox1.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListBox1.FormattingEnabled = True
        Me.ListBox1.ItemHeight = 20
        Me.ListBox1.Location = New System.Drawing.Point(6, 50)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(210, 204)
        Me.ListBox1.TabIndex = 0
        Me.ToolTip1.SetToolTip(Me.ListBox1, "Right Click to Remove Item, Double Click to Change Item Text")
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(3, 31)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(129, 16)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "Annotation Category"
        '
        'ListBox2
        '
        Me.ListBox2.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListBox2.FormattingEnabled = True
        Me.ListBox2.ItemHeight = 20
        Me.ListBox2.Location = New System.Drawing.Point(6, 310)
        Me.ListBox2.Name = "ListBox2"
        Me.ListBox2.Size = New System.Drawing.Size(210, 184)
        Me.ListBox2.TabIndex = 3
        Me.ToolTip1.SetToolTip(Me.ListBox2, "Right Click to Remove Item, Double Click to Change Item Text")
        Me.ListBox2.Visible = False
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(2, 291)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(144, 16)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "Driver Feedback Tabs"
        Me.Label2.Visible = False
        '
        'ListBox3
        '
        Me.ListBox3.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListBox3.FormattingEnabled = True
        Me.ListBox3.ItemHeight = 20
        Me.ListBox3.Location = New System.Drawing.Point(223, 50)
        Me.ListBox3.Name = "ListBox3"
        Me.ListBox3.Size = New System.Drawing.Size(262, 444)
        Me.ListBox3.TabIndex = 5
        Me.ToolTip1.SetToolTip(Me.ListBox3, "Right Click to Remove Item, Double Click to Change Item Text")
        Me.ListBox3.Visible = False
        '
        'ListBox4
        '
        Me.ListBox4_Annotations.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListBox4_Annotations.FormattingEnabled = True
        Me.ListBox4_Annotations.ItemHeight = 20
        Me.ListBox4_Annotations.Location = New System.Drawing.Point(529, 50)
        Me.ListBox4_Annotations.Name = "ListBox4"
        Me.ListBox4_Annotations.Size = New System.Drawing.Size(215, 104)
        Me.ListBox4_Annotations.TabIndex = 6
        Me.ToolTip1.SetToolTip(Me.ListBox4_Annotations, "If not Undefined, Right Click to Remove Item.  If Undefined, Double Click to Add " &
        "New Sub-Category, or Double Click to Change Item Text")
        Me.ListBox4_Annotations.Visible = False
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label3.Location = New System.Drawing.Point(220, 31)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(140, 16)
        Me.Label3.TabIndex = 7
        Me.Label3.Text = "Annotation Button Text"
        Me.Label3.Visible = False
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label4.Location = New System.Drawing.Point(526, 31)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(208, 16)
        Me.Label4.TabIndex = 8
        Me.Label4.Text = "Annotation Button Sub-Categories"
        Me.Label4.Visible = False
        '
        'Button1
        '
        Me.Button1.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button1.Location = New System.Drawing.Point(6, 260)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(211, 28)
        Me.Button1.TabIndex = 9
        Me.Button1.Text = "Add Annotation Category"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button2.Location = New System.Drawing.Point(5, 500)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(211, 35)
        Me.Button2.TabIndex = 10
        Me.Button2.Text = "Add Driver Feedback Tab"
        Me.Button2.UseVisualStyleBackColor = True
        Me.Button2.Visible = False
        '
        'Button3
        '
        Me.Button3.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button3.Location = New System.Drawing.Point(223, 500)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(300, 35)
        Me.Button3.TabIndex = 11
        Me.Button3.Text = "Add Annotation Button"
        Me.Button3.UseVisualStyleBackColor = True
        Me.Button3.Visible = False
        '
        'ToolTip1
        '
        Me.ToolTip1.AutomaticDelay = 0
        Me.ToolTip1.AutoPopDelay = 5000
        Me.ToolTip1.InitialDelay = 0
        Me.ToolTip1.ReshowDelay = 0
        Me.ToolTip1.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info
        '
        'MenuStrip1
        '
        Me.MenuStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ExitToolStripMenuItem})
        Me.MenuStrip1.Location = New System.Drawing.Point(0, 0)
        Me.MenuStrip1.Name = "MenuStrip1"
        Me.MenuStrip1.Size = New System.Drawing.Size(749, 29)
        Me.MenuStrip1.TabIndex = 15
        Me.MenuStrip1.Text = "MenuStrip1"
        '
        'ExitToolStripMenuItem
        '
        Me.ExitToolStripMenuItem.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.ToolStripMenuItem1, Me.ToolStripMenuItem3, Me.SaveAndUpdateToolStripMenuItem, Me.UpdateAndSaveAsToolStripMenuItem, Me.ExitToolStripMenuItem1})
        Me.ExitToolStripMenuItem.Font = New System.Drawing.Font("Segoe UI", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        Me.ExitToolStripMenuItem.Size = New System.Drawing.Size(49, 25)
        Me.ExitToolStripMenuItem.Text = "File"
        '
        'ToolStripMenuItem1
        '
        Me.ToolStripMenuItem1.Name = "ToolStripMenuItem1"
        Me.ToolStripMenuItem1.Size = New System.Drawing.Size(440, 26)
        Me.ToolStripMenuItem1.Text = "Select Annotation Dictionary File..."
        '
        'ToolStripMenuItem3
        '
        Me.ToolStripMenuItem3.Name = "ToolStripMenuItem3"
        Me.ToolStripMenuItem3.Size = New System.Drawing.Size(440, 26)
        Me.ToolStripMenuItem3.Text = "Revert to DEFAULT Annotation Dictionary File..."
        '
        'SaveAndUpdateToolStripMenuItem
        '
        Me.SaveAndUpdateToolStripMenuItem.Enabled = False
        Me.SaveAndUpdateToolStripMenuItem.Name = "SaveAndUpdateToolStripMenuItem"
        Me.SaveAndUpdateToolStripMenuItem.Size = New System.Drawing.Size(440, 26)
        Me.SaveAndUpdateToolStripMenuItem.Text = "Update and Save"
        '
        'UpdateAndSaveAsToolStripMenuItem
        '
        Me.UpdateAndSaveAsToolStripMenuItem.Name = "UpdateAndSaveAsToolStripMenuItem"
        Me.UpdateAndSaveAsToolStripMenuItem.Size = New System.Drawing.Size(440, 26)
        Me.UpdateAndSaveAsToolStripMenuItem.Text = "Save As..."
        '
        'ExitToolStripMenuItem1
        '
        Me.ExitToolStripMenuItem1.Name = "ExitToolStripMenuItem1"
        Me.ExitToolStripMenuItem1.Size = New System.Drawing.Size(440, 26)
        Me.ExitToolStripMenuItem1.Text = "Exit"
        '
        'OpenFileDialog1
        '
        Me.OpenFileDialog1.FileName = "OpenFileDialog1"
        '
        'Button4
        '
        Me.Button4.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Button4.Location = New System.Drawing.Point(526, 500)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(218, 35)
        Me.Button4.TabIndex = 16
        Me.Button4.Text = "Exit"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'ListBox5
        '
        Me.ListBox5.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.ListBox5.FormattingEnabled = True
        Me.ListBox5.ItemHeight = 20
        Me.ListBox5.Location = New System.Drawing.Point(491, 50)
        Me.ListBox5.Name = "ListBox5"
        Me.ListBox5.Size = New System.Drawing.Size(32, 444)
        Me.ListBox5.TabIndex = 17
        Me.ToolTip1.SetToolTip(Me.ListBox5, "Right Click to Remove Item, Double Click to Change Item Text")
        Me.ListBox5.Visible = False
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label5.Location = New System.Drawing.Point(493, 31)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(26, 16)
        Me.Label5.TabIndex = 18
        Me.Label5.Text = "HK"
        Me.Label5.Visible = False
        '
        'AnnotationInterfaceConfigure
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(749, 540)
        Me.ControlBox = False
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.ListBox5)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.ListBox4_Annotations)
        Me.Controls.Add(Me.ListBox3)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.ListBox2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.ListBox1)
        Me.Controls.Add(Me.MenuStrip1)
        Me.MainMenuStrip = Me.MenuStrip1
        Me.Name = "AnnotationInterfaceConfigure"
        Me.Text = "Annotation Interface Configuration - "
        Me.MenuStrip1.ResumeLayout(False)
        Me.MenuStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents ListBox1 As ListBox
    Friend WithEvents Label1 As Label
    Friend WithEvents ListBox2 As ListBox
    Friend WithEvents Label2 As Label
    Friend WithEvents ListBox3 As ListBox
    Friend WithEvents ListBox4_Annotations As ListBox
    Friend WithEvents Label3 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents Button3 As Button
    Friend WithEvents ToolTip1 As ToolTip
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents ExitToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem1 As ToolStripMenuItem
    Friend WithEvents ExitToolStripMenuItem1 As ToolStripMenuItem
    Friend WithEvents SaveAndUpdateToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents UpdateAndSaveAsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SaveFileDialog1 As SaveFileDialog
    Friend WithEvents OpenFileDialog1 As OpenFileDialog
    Friend WithEvents ToolStripMenuItem3 As ToolStripMenuItem
    Friend WithEvents Button4 As Button
    Friend WithEvents ListBox5 As ListBox
    Friend WithEvents Label5 As Label
End Class
