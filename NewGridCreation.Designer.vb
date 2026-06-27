<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class NewGridCreation
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
        Me.ComboBox1 = New System.Windows.Forms.ComboBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.ListBox3 = New System.Windows.Forms.ListBox()
        Me.ListBox2 = New System.Windows.Forms.ListBox()
        Me.ListBox1 = New System.Windows.Forms.ListBox()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.TextBox2 = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GridTitle = New System.Windows.Forms.TextBox()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.ComboBox1)
        Me.GroupBox1.Controls.Add(Me.Label5)
        Me.GroupBox1.Controls.Add(Me.Label4)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Controls.Add(Me.Button3)
        Me.GroupBox1.Controls.Add(Me.ListBox3)
        Me.GroupBox1.Controls.Add(Me.ListBox2)
        Me.GroupBox1.Controls.Add(Me.ListBox1)
        Me.GroupBox1.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox1.Location = New System.Drawing.Point(8, 123)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(683, 390)
        Me.GroupBox1.TabIndex = 83
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Multi-Signal Single Data Column Grid Creation"
        '
        'ComboBox1
        '
        Me.ComboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.Simple
        Me.ComboBox1.FormattingEnabled = True
        Me.ComboBox1.Location = New System.Drawing.Point(144, 54)
        Me.ComboBox1.Name = "ComboBox1"
        Me.ComboBox1.Size = New System.Drawing.Size(171, 305)
        Me.ComboBox1.TabIndex = 91
        '
        'Label5
        '
        Me.Label5.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label5.Location = New System.Drawing.Point(321, 27)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(356, 20)
        Me.Label5.TabIndex = 90
        Me.Label5.Text = "Available Signals"
        Me.Label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label4
        '
        Me.Label4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label4.Location = New System.Drawing.Point(144, 27)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(171, 20)
        Me.Label4.TabIndex = 89
        Me.Label4.Text = "Available Rasters"
        Me.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label3
        '
        Me.Label3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label3.Location = New System.Drawing.Point(4, 27)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(134, 20)
        Me.Label3.TabIndex = 88
        Me.Label3.Text = "Available Devices"
        Me.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Button3
        '
        Me.Button3.Enabled = False
        Me.Button3.Location = New System.Drawing.Point(572, 363)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(105, 23)
        Me.Button3.TabIndex = 87
        Me.Button3.Text = "Populate Grid"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'ListBox3
        '
        Me.ListBox3.FormattingEnabled = True
        Me.ListBox3.Location = New System.Drawing.Point(321, 54)
        Me.ListBox3.Name = "ListBox3"
        Me.ListBox3.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended
        Me.ListBox3.Size = New System.Drawing.Size(356, 303)
        Me.ListBox3.TabIndex = 86
        '
        'ListBox2
        '
        Me.ListBox2.FormattingEnabled = True
        Me.ListBox2.Location = New System.Drawing.Point(391, 133)
        Me.ListBox2.Name = "ListBox2"
        Me.ListBox2.Size = New System.Drawing.Size(171, 303)
        Me.ListBox2.TabIndex = 85
        Me.ListBox2.Visible = False
        '
        'ListBox1
        '
        Me.ListBox1.FormattingEnabled = True
        Me.ListBox1.Location = New System.Drawing.Point(7, 54)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(131, 303)
        Me.ListBox1.TabIndex = 84
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.Button1)
        Me.GroupBox2.Controls.Add(Me.TextBox2)
        Me.GroupBox2.Controls.Add(Me.Label2)
        Me.GroupBox2.Controls.Add(Me.TextBox1)
        Me.GroupBox2.Controls.Add(Me.Label1)
        Me.GroupBox2.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox2.Location = New System.Drawing.Point(452, 12)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(239, 105)
        Me.GroupBox2.TabIndex = 84
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Unpopulated Grid Creation"
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(17, 70)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(209, 23)
        Me.Button1.TabIndex = 84
        Me.Button1.Text = "Create"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'TextBox2
        '
        Me.TextBox2.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBox2.Location = New System.Drawing.Point(198, 46)
        Me.TextBox2.Name = "TextBox2"
        Me.TextBox2.Size = New System.Drawing.Size(28, 22)
        Me.TextBox2.TabIndex = 83
        Me.TextBox2.Text = "2"
        '
        'Label2
        '
        Me.Label2.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.Label2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label2.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label2.Location = New System.Drawing.Point(17, 46)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(184, 21)
        Me.Label2.TabIndex = 82
        Me.Label2.Text = "Number of Rows"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'TextBox1
        '
        Me.TextBox1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBox1.Location = New System.Drawing.Point(198, 25)
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.Size = New System.Drawing.Size(28, 22)
        Me.TextBox1.TabIndex = 81
        Me.TextBox1.Text = "1"
        '
        'Label1
        '
        Me.Label1.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.Label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label1.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label1.Location = New System.Drawing.Point(17, 25)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(184, 21)
        Me.Label1.TabIndex = 80
        Me.Label1.Text = "Number of Columns"
        Me.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'GridTitle
        '
        Me.GridTitle.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GridTitle.Location = New System.Drawing.Point(100, 12)
        Me.GridTitle.Name = "GridTitle"
        Me.GridTitle.Size = New System.Drawing.Size(100, 22)
        Me.GridTitle.TabIndex = 86
        Me.GridTitle.Text = "undefined"
        '
        'Label8
        '
        Me.Label8.BackColor = System.Drawing.SystemColors.ButtonFace
        Me.Label8.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label8.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label8.Location = New System.Drawing.Point(24, 12)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(80, 21)
        Me.Label8.TabIndex = 85
        Me.Label8.Text = "Grid Title"
        Me.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.BackColor = System.Drawing.Color.Yellow
        Me.Label14.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D
        Me.Label14.Font = New System.Drawing.Font("Microsoft Sans Serif", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label14.Location = New System.Drawing.Point(24, 60)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(61, 20)
        Me.Label14.TabIndex = 87
        Me.Label14.Text = "Label14"
        Me.Label14.Visible = False
        '
        'NewGridCreation
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(703, 517)
        Me.Controls.Add(Me.Label14)
        Me.Controls.Add(Me.GridTitle)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Name = "NewGridCreation"
        Me.ShowInTaskbar = False
        Me.Text = "Create New Grid"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents TextBox2 As System.Windows.Forms.TextBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents TextBox1 As System.Windows.Forms.TextBox
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Button3 As System.Windows.Forms.Button
    Friend WithEvents ListBox3 As System.Windows.Forms.ListBox
    Friend WithEvents ListBox2 As System.Windows.Forms.ListBox
    Friend WithEvents ListBox1 As System.Windows.Forms.ListBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label5 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents GridTitle As System.Windows.Forms.TextBox
    Friend WithEvents Label8 As System.Windows.Forms.Label
    Friend WithEvents ComboBox1 As System.Windows.Forms.ComboBox
    Friend WithEvents Label14 As System.Windows.Forms.Label
End Class
