<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class LidarHealthDetailForm
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
        Me.DataGridView1 = New System.Windows.Forms.DataGridView()
        Me.Label_Summary = New System.Windows.Forms.Label()
        Me.Button_Refresh = New System.Windows.Forms.Button()
        Me.Button_Close = New System.Windows.Forms.Button()
        Me.Button_Export = New System.Windows.Forms.Button()
        Me.Label_Title = New System.Windows.Forms.Label()
        Me.GroupBox_OxtsStatus = New System.Windows.Forms.GroupBox()
        Me.Label_PacketLoss = New System.Windows.Forms.Label()
        Me.Label_GpsLock = New System.Windows.Forms.Label()
        Me.Label_PtpStatus = New System.Windows.Forms.Label()
        Me.Button_TestOxts = New System.Windows.Forms.Button()
        Me.Button_TestLidar = New System.Windows.Forms.Button()
        Me.Button_TestIntegration = New System.Windows.Forms.Button()
        Me.Panel_TestActions = New System.Windows.Forms.Panel()
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox_OxtsStatus.SuspendLayout()
        Me.Panel_TestActions.SuspendLayout()
        Me.SuspendLayout()
        '
        'DataGridView1
        '
        Me.DataGridView1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView1.Location = New System.Drawing.Point(18, 98)
        Me.DataGridView1.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.Size = New System.Drawing.Size(1140, 493)
        Me.DataGridView1.TabIndex = 0
        '
        'Label_Summary
        '
        Me.Label_Summary.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label_Summary.AutoSize = True
        Me.Label_Summary.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_Summary.Location = New System.Drawing.Point(749, 610)
        Me.Label_Summary.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label_Summary.Name = "Label_Summary"
        Me.Label_Summary.Size = New System.Drawing.Size(407, 22)
        Me.Label_Summary.TabIndex = 1
        Me.Label_Summary.Text = "Total: 0 | Healthy: 0 | Warning: 0 | Critical: 0"
        '
        'Button_Refresh
        '
        Me.Button_Refresh.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button_Refresh.Location = New System.Drawing.Point(747, 7)
        Me.Button_Refresh.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Button_Refresh.Name = "Button_Refresh"
        Me.Button_Refresh.Size = New System.Drawing.Size(135, 46)
        Me.Button_Refresh.TabIndex = 2
        Me.Button_Refresh.Text = "Refresh"
        Me.Button_Refresh.UseVisualStyleBackColor = True
        AddHandler Me.Button_Refresh.Click, AddressOf Me.Button_Refresh_Click
        '
        'Button_Close
        '
        Me.Button_Close.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button_Close.Location = New System.Drawing.Point(1035, 7)
        Me.Button_Close.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Button_Close.Name = "Button_Close"
        Me.Button_Close.Size = New System.Drawing.Size(135, 46)
        Me.Button_Close.TabIndex = 3
        Me.Button_Close.Text = "Close"
        Me.Button_Close.UseVisualStyleBackColor = True
        AddHandler Me.Button_Close.Click, AddressOf Me.Button_Close_Click
        '
        'Button_Export
        '
        Me.Button_Export.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button_Export.Location = New System.Drawing.Point(891, 7)
        Me.Button_Export.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Button_Export.Name = "Button_Export"
        Me.Button_Export.Size = New System.Drawing.Size(135, 46)
        Me.Button_Export.TabIndex = 4
        Me.Button_Export.Text = "Export CSV"
        Me.Button_Export.UseVisualStyleBackColor = True
        AddHandler Me.Button_Export.Click, AddressOf Me.Button_Export_Click
        '
        'Label_Title
        '
        Me.Label_Title.AutoSize = True
        Me.Label_Title.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_Title.Location = New System.Drawing.Point(18, 23)
        Me.Label_Title.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label_Title.Name = "Label_Title"
        Me.Label_Title.Size = New System.Drawing.Size(311, 29)
        Me.Label_Title.TabIndex = 5
        Me.Label_Title.Text = "LiDAR Health Diagnostics"
        '
        'GroupBox_OxtsStatus
        '
        Me.GroupBox_OxtsStatus.Controls.Add(Me.Label_PacketLoss)
        Me.GroupBox_OxtsStatus.Controls.Add(Me.Label_GpsLock)
        Me.GroupBox_OxtsStatus.Controls.Add(Me.Label_PtpStatus)
        Me.GroupBox_OxtsStatus.Dock = System.Windows.Forms.DockStyle.Top
        Me.GroupBox_OxtsStatus.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox_OxtsStatus.Location = New System.Drawing.Point(0, 0)
        Me.GroupBox_OxtsStatus.Name = "GroupBox_OxtsStatus"
        Me.GroupBox_OxtsStatus.Size = New System.Drawing.Size(1176, 80)
        Me.GroupBox_OxtsStatus.TabIndex = 6
        Me.GroupBox_OxtsStatus.TabStop = False
        Me.GroupBox_OxtsStatus.Text = "OXTS RT3000 Status"
        '
        'Label_PacketLoss
        '
        Me.Label_PacketLoss.AutoSize = True
        Me.Label_PacketLoss.Location = New System.Drawing.Point(401, 32)
        Me.Label_PacketLoss.Name = "Label_PacketLoss"
        Me.Label_PacketLoss.Size = New System.Drawing.Size(159, 22)
        Me.Label_PacketLoss.TabIndex = 2
        Me.Label_PacketLoss.Text = "Packet Loss: 0%"
        '
        'Label_GpsLock
        '
        Me.Label_GpsLock.AutoSize = True
        Me.Label_GpsLock.Location = New System.Drawing.Point(203, 32)
        Me.Label_GpsLock.Name = "Label_GpsLock"
        Me.Label_GpsLock.Size = New System.Drawing.Size(193, 22)
        Me.Label_GpsLock.TabIndex = 1
        Me.Label_GpsLock.Text = "GPS Lock: Unknown"
        '
        'Label_PtpStatus
        '
        Me.Label_PtpStatus.AutoSize = True
        Me.Label_PtpStatus.Location = New System.Drawing.Point(18, 30)
        Me.Label_PtpStatus.Name = "Label_PtpStatus"
        Me.Label_PtpStatus.Size = New System.Drawing.Size(168, 22)
        Me.Label_PtpStatus.TabIndex = 0
        Me.Label_PtpStatus.Text = "PTP: Initializing..."
        '
        'Button_TestOxts
        '
        Me.Button_TestOxts.Location = New System.Drawing.Point(9, 7)
        Me.Button_TestOxts.Margin = New System.Windows.Forms.Padding(5, 4, 5, 4)
        Me.Button_TestOxts.Name = "Button_TestOxts"
        Me.Button_TestOxts.Size = New System.Drawing.Size(135, 46)
        Me.Button_TestOxts.TabIndex = 5
        Me.Button_TestOxts.Text = "🔌 Test OXTS"
        Me.Button_TestOxts.UseVisualStyleBackColor = True
        AddHandler Me.Button_TestOxts.Click, AddressOf Me.TestOxtsConnection_Click
        '
        'Button_TestLidar
        '
        Me.Button_TestLidar.Location = New System.Drawing.Point(154, 7)
        Me.Button_TestLidar.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Button_TestLidar.Name = "Button_TestLidar"
        Me.Button_TestLidar.Size = New System.Drawing.Size(135, 46)
        Me.Button_TestLidar.TabIndex = 6
        Me.Button_TestLidar.Text = "📡 Test LiDAR"
        Me.Button_TestLidar.UseVisualStyleBackColor = True
        AddHandler Me.Button_TestLidar.Click, AddressOf Me.TestLidarCapture_Click
        '
        'Button_TestIntegration
        '
        Me.Button_TestIntegration.Location = New System.Drawing.Point(299, 7)
        Me.Button_TestIntegration.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.Button_TestIntegration.Name = "Button_TestIntegration"
        Me.Button_TestIntegration.Size = New System.Drawing.Size(135, 46)
        Me.Button_TestIntegration.TabIndex = 7
        Me.Button_TestIntegration.Text = "🔗 Integration"
        Me.Button_TestIntegration.UseVisualStyleBackColor = True
        AddHandler Me.Button_TestIntegration.Click, AddressOf Me.TestOxtsLidarIntegration_Click
        '
        'Panel_TestActions
        '
        Me.Panel_TestActions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel_TestActions.Controls.Add(Me.Button_TestIntegration)
        Me.Panel_TestActions.Controls.Add(Me.Button_TestLidar)
        Me.Panel_TestActions.Controls.Add(Me.Button_TestOxts)
        Me.Panel_TestActions.Controls.Add(Me.Button_Refresh)
        Me.Panel_TestActions.Controls.Add(Me.Button_Export)
        Me.Panel_TestActions.Controls.Add(Me.Button_Close)
        Me.Panel_TestActions.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.Panel_TestActions.Location = New System.Drawing.Point(0, 652)
        Me.Panel_TestActions.Name = "Panel_TestActions"
        Me.Panel_TestActions.Size = New System.Drawing.Size(1176, 60)
        Me.Panel_TestActions.TabIndex = 7
        '
        'LidarHealthDetailForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1176, 712)
        Me.Controls.Add(Me.Panel_TestActions)
        Me.Controls.Add(Me.GroupBox_OxtsStatus)
        Me.Controls.Add(Me.Label_Title)
        Me.Controls.Add(Me.Label_Summary)
        Me.Controls.Add(Me.DataGridView1)
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.MinimumSize = New System.Drawing.Size(1189, 678)
        Me.Name = "LidarHealthDetailForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "LiDAR Health Detail"
        CType(Me.DataGridView1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox_OxtsStatus.ResumeLayout(False)
        Me.GroupBox_OxtsStatus.PerformLayout()
        Me.Panel_TestActions.ResumeLayout(False)
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents DataGridView1 As DataGridView
    Friend WithEvents Label_Summary As Label
    Friend WithEvents Button_Refresh As Button
    Friend WithEvents Button_Close As Button
    Friend WithEvents Button_Export As Button
    Friend WithEvents Label_Title As Label
    Friend WithEvents Button_TestOxts As System.Windows.Forms.Button
    Friend WithEvents Button_TestLidar As System.Windows.Forms.Button
    Friend WithEvents Button_TestIntegration As System.Windows.Forms.Button
    Friend WithEvents Panel_TestActions As System.Windows.Forms.Panel
    Friend WithEvents GroupBox_OxtsStatus As System.Windows.Forms.GroupBox
    Friend WithEvents Label_PtpStatus As System.Windows.Forms.Label
    Friend WithEvents Label_GpsLock As System.Windows.Forms.Label
    Friend WithEvents Label_PacketLoss As System.Windows.Forms.Label
End Class