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
        Me.Label_PacketCount = New System.Windows.Forms.Label()
        Me.Label_PacketLoss = New System.Windows.Forms.Label()
        Me.Label_GpsLock = New System.Windows.Forms.Label()
        Me.Label_PtpStatus = New System.Windows.Forms.Label()
        Me.Button_TestOxts = New System.Windows.Forms.Button()
        Me.Button_TestLidar = New System.Windows.Forms.Button()
        Me.Button_TestIntegration = New System.Windows.Forms.Button()
        Me.Button_DiagnoseOxts = New System.Windows.Forms.Button()
        Me.Button_ResetOxtsStats = New System.Windows.Forms.Button()
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
        Me.DataGridView1.Location = New System.Drawing.Point(12, 90)
        Me.DataGridView1.Name = "DataGridView1"
        Me.DataGridView1.RowHeadersWidth = 62
        Me.DataGridView1.Size = New System.Drawing.Size(817, 294)
        Me.DataGridView1.TabIndex = 0
        '
        'Label_Summary
        '
        Me.Label_Summary.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.Label_Summary.AutoSize = True
        Me.Label_Summary.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_Summary.Location = New System.Drawing.Point(439, 396)
        Me.Label_Summary.Name = "Label_Summary"
        Me.Label_Summary.Size = New System.Drawing.Size(345, 15)
        Me.Label_Summary.TabIndex = 1
        Me.Label_Summary.Text = "Total: 0 | ✅ Healthy: 0 | ⚠️ Warning: 0 | ❌ Critical: 0"
        '
        'Button_Refresh
        '
        Me.Button_Refresh.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button_Refresh.Location = New System.Drawing.Point(554, 5)
        Me.Button_Refresh.Name = "Button_Refresh"
        Me.Button_Refresh.Size = New System.Drawing.Size(90, 30)
        Me.Button_Refresh.TabIndex = 2
        Me.Button_Refresh.Text = "Refresh"
        Me.Button_Refresh.UseVisualStyleBackColor = True
        '
        'Button_Close
        '
        Me.Button_Close.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button_Close.Location = New System.Drawing.Point(746, 5)
        Me.Button_Close.Name = "Button_Close"
        Me.Button_Close.Size = New System.Drawing.Size(90, 30)
        Me.Button_Close.TabIndex = 3
        Me.Button_Close.Text = "Close"
        Me.Button_Close.UseVisualStyleBackColor = True
        '
        'Button_Export
        '
        Me.Button_Export.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button_Export.Location = New System.Drawing.Point(650, 5)
        Me.Button_Export.Name = "Button_Export"
        Me.Button_Export.Size = New System.Drawing.Size(90, 30)
        Me.Button_Export.TabIndex = 4
        Me.Button_Export.Text = "Export CSV"
        Me.Button_Export.UseVisualStyleBackColor = True
        '
        'Label_Title
        '
        Me.Label_Title.AutoSize = True
        Me.Label_Title.Font = New System.Drawing.Font("Microsoft Sans Serif", 12.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_Title.Location = New System.Drawing.Point(12, 15)
        Me.Label_Title.Name = "Label_Title"
        Me.Label_Title.Size = New System.Drawing.Size(343, 20)
        Me.Label_Title.TabIndex = 5
        Me.Label_Title.Text = "LiDAR Health Diagnostics (Integrity-First)"
        '
        'GroupBox_OxtsStatus
        '
        Me.GroupBox_OxtsStatus.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox_OxtsStatus.Controls.Add(Me.Label_PacketCount)
        Me.GroupBox_OxtsStatus.Controls.Add(Me.Label_PacketLoss)
        Me.GroupBox_OxtsStatus.Controls.Add(Me.Label_GpsLock)
        Me.GroupBox_OxtsStatus.Controls.Add(Me.Label_PtpStatus)
        Me.GroupBox_OxtsStatus.Font = New System.Drawing.Font("Microsoft Sans Serif", 9.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.GroupBox_OxtsStatus.Location = New System.Drawing.Point(12, 40)
        Me.GroupBox_OxtsStatus.Margin = New System.Windows.Forms.Padding(2)
        Me.GroupBox_OxtsStatus.Name = "GroupBox_OxtsStatus"
        Me.GroupBox_OxtsStatus.Padding = New System.Windows.Forms.Padding(2)
        Me.GroupBox_OxtsStatus.Size = New System.Drawing.Size(817, 44)
        Me.GroupBox_OxtsStatus.TabIndex = 6
        Me.GroupBox_OxtsStatus.TabStop = False
        Me.GroupBox_OxtsStatus.Text = "Precision Time Status"
        '
        'Label_PacketCount
        '
        Me.Label_PacketCount.AutoSize = True
        Me.Label_PacketCount.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_PacketCount.Location = New System.Drawing.Point(522, 16)
        Me.Label_PacketCount.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label_PacketCount.Name = "Label_PacketCount"
        Me.Label_PacketCount.Size = New System.Drawing.Size(139, 13)
        Me.Label_PacketCount.TabIndex = 3
        Me.Label_PacketCount.Text = "NCOM: 0 valid / 0 total"
        Me.Label_PacketCount.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label_PacketLoss
        '
        Me.Label_PacketLoss.AutoSize = True
        Me.Label_PacketLoss.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_PacketLoss.Location = New System.Drawing.Point(323, 16)
        Me.Label_PacketLoss.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label_PacketLoss.Name = "Label_PacketLoss"
        Me.Label_PacketLoss.Size = New System.Drawing.Size(168, 13)
        Me.Label_PacketLoss.TabIndex = 2
        Me.Label_PacketLoss.Text = "Integrity: 100% (Corrupt: 0%)"
        '
        'Label_GpsLock
        '
        Me.Label_GpsLock.AutoSize = True
        Me.Label_GpsLock.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_GpsLock.Location = New System.Drawing.Point(226, 16)
        Me.Label_GpsLock.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label_GpsLock.Name = "Label_GpsLock"
        Me.Label_GpsLock.Size = New System.Drawing.Size(93, 13)
        Me.Label_GpsLock.TabIndex = 1
        Me.Label_GpsLock.Text = "GPS: Unknown"
        '
        'Label_PtpStatus
        '
        Me.Label_PtpStatus.AutoSize = True
        Me.Label_PtpStatus.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.0!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Label_PtpStatus.Location = New System.Drawing.Point(5, 16)
        Me.Label_PtpStatus.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label_PtpStatus.Name = "Label_PtpStatus"
        Me.Label_PtpStatus.Size = New System.Drawing.Size(108, 13)
        Me.Label_PtpStatus.TabIndex = 0
        Me.Label_PtpStatus.Text = "PTP: Initializing..."
        '
        'Button_TestOxts
        '
        Me.Button_TestOxts.Location = New System.Drawing.Point(6, 5)
        Me.Button_TestOxts.Name = "Button_TestOxts"
        Me.Button_TestOxts.Size = New System.Drawing.Size(90, 30)
        Me.Button_TestOxts.TabIndex = 5
        Me.Button_TestOxts.Text = "🔌 Test OXTS"
        Me.Button_TestOxts.UseVisualStyleBackColor = True
        '
        'Button_TestLidar
        '
        Me.Button_TestLidar.Location = New System.Drawing.Point(103, 5)
        Me.Button_TestLidar.Name = "Button_TestLidar"
        Me.Button_TestLidar.Size = New System.Drawing.Size(90, 30)
        Me.Button_TestLidar.TabIndex = 6
        Me.Button_TestLidar.Text = "📡 Test LiDAR"
        Me.Button_TestLidar.UseVisualStyleBackColor = True
        '
        'Button_TestIntegration
        '
        Me.Button_TestIntegration.Location = New System.Drawing.Point(199, 5)
        Me.Button_TestIntegration.Name = "Button_TestIntegration"
        Me.Button_TestIntegration.Size = New System.Drawing.Size(90, 30)
        Me.Button_TestIntegration.TabIndex = 7
        Me.Button_TestIntegration.Text = "🔗 Integration"
        Me.Button_TestIntegration.UseVisualStyleBackColor = True
        '
        'Button_DiagnoseOxts
        '
        Me.Button_DiagnoseOxts.Location = New System.Drawing.Point(296, 5)
        Me.Button_DiagnoseOxts.Name = "Button_DiagnoseOxts"
        Me.Button_DiagnoseOxts.Size = New System.Drawing.Size(107, 30)
        Me.Button_DiagnoseOxts.TabIndex = 8
        Me.Button_DiagnoseOxts.Text = "📊 Integrity Report"
        Me.Button_DiagnoseOxts.UseVisualStyleBackColor = True
        '
        'Button_ResetOxtsStats
        '
        Me.Button_ResetOxtsStats.Location = New System.Drawing.Point(408, 5)
        Me.Button_ResetOxtsStats.Name = "Button_ResetOxtsStats"
        Me.Button_ResetOxtsStats.Size = New System.Drawing.Size(83, 30)
        Me.Button_ResetOxtsStats.TabIndex = 9
        Me.Button_ResetOxtsStats.Text = "🔄 Reset"
        Me.Button_ResetOxtsStats.UseVisualStyleBackColor = True
        '
        'Panel_TestActions
        '
        Me.Panel_TestActions.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Panel_TestActions.Controls.Add(Me.Button_ResetOxtsStats)
        Me.Panel_TestActions.Controls.Add(Me.Button_DiagnoseOxts)
        Me.Panel_TestActions.Controls.Add(Me.Button_TestIntegration)
        Me.Panel_TestActions.Controls.Add(Me.Button_TestLidar)
        Me.Panel_TestActions.Controls.Add(Me.Button_TestOxts)
        Me.Panel_TestActions.Controls.Add(Me.Button_Refresh)
        Me.Panel_TestActions.Controls.Add(Me.Button_Export)
        Me.Panel_TestActions.Controls.Add(Me.Button_Close)
        Me.Panel_TestActions.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.Panel_TestActions.Location = New System.Drawing.Point(0, 423)
        Me.Panel_TestActions.Margin = New System.Windows.Forms.Padding(2)
        Me.Panel_TestActions.Name = "Panel_TestActions"
        Me.Panel_TestActions.Size = New System.Drawing.Size(841, 40)
        Me.Panel_TestActions.TabIndex = 7
        '
        'LidarHealthDetailForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(841, 463)
        Me.Controls.Add(Me.Panel_TestActions)
        Me.Controls.Add(Me.GroupBox_OxtsStatus)
        Me.Controls.Add(Me.Label_Title)
        Me.Controls.Add(Me.Label_Summary)
        Me.Controls.Add(Me.DataGridView1)
        Me.MinimumSize = New System.Drawing.Size(798, 454)
        Me.Name = "LidarHealthDetailForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "LiDAR Health Detail - Integrity Monitor"
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
    Friend WithEvents Button_DiagnoseOxts As System.Windows.Forms.Button
    Friend WithEvents Button_ResetOxtsStats As System.Windows.Forms.Button
    Friend WithEvents Panel_TestActions As System.Windows.Forms.Panel
    Friend WithEvents GroupBox_OxtsStatus As System.Windows.Forms.GroupBox
    Friend WithEvents Label_PtpStatus As System.Windows.Forms.Label
    Friend WithEvents Label_GpsLock As System.Windows.Forms.Label
    Friend WithEvents Label_PacketLoss As System.Windows.Forms.Label
    Friend WithEvents Label_PacketCount As System.Windows.Forms.Label
End Class