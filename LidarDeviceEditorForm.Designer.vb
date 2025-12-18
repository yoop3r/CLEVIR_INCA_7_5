<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class LidarDeviceEditorForm
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
        Me.DataGridViewLidar = New System.Windows.Forms.DataGridView()
        Me.ColID = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColEnabled = New System.Windows.Forms.DataGridViewComboBoxColumn()
        Me.ColAdapterGuid = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColIPAddress = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColDataPort = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColIMUPort = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ButtonSave = New System.Windows.Forms.Button()
        Me.ButtonCancel = New System.Windows.Forms.Button()
        CType(Me.DataGridViewLidar, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'DataGridViewLidar
        '
        Me.DataGridViewLidar.AllowUserToOrderColumns = True
        Me.DataGridViewLidar.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridViewLidar.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridViewLidar.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ColID, Me.ColEnabled, Me.ColAdapterGuid, Me.ColIPAddress, Me.ColDataPort, Me.ColIMUPort})
        Me.DataGridViewLidar.Location = New System.Drawing.Point(12, 12)
        Me.DataGridViewLidar.Name = "DataGridViewLidar"
        Me.DataGridViewLidar.Size = New System.Drawing.Size(676, 400)
        Me.DataGridViewLidar.TabIndex = 0
        '
        'ColID
        '
        Me.ColID.HeaderText = "ID"
        Me.ColID.Name = "ColID"
        Me.ColID.Width = 50
        '
        'ColEnabled
        '
        Me.ColEnabled.HeaderText = "Enabled"
        Me.ColEnabled.Items.AddRange(New Object() {"True", "False"})
        Me.ColEnabled.Name = "ColEnabled"
        Me.ColEnabled.Width = 80
        '
        'ColAdapterGuid
        '
        Me.ColAdapterGuid.HeaderText = "Adapter GUID"
        Me.ColAdapterGuid.Name = "ColAdapterGuid"
        Me.ColAdapterGuid.Width = 250
        '
        'ColIPAddress
        '
        Me.ColIPAddress.HeaderText = "IP Address"
        Me.ColIPAddress.Name = "ColIPAddress"
        Me.ColIPAddress.Width = 120
        '
        'ColDataPort
        '
        Me.ColDataPort.HeaderText = "Data Port"
        Me.ColDataPort.Name = "ColDataPort"
        Me.ColDataPort.Width = 80
        '
        'ColIMUPort
        '
        Me.ColIMUPort.HeaderText = "IMU Port"
        Me.ColIMUPort.Name = "ColIMUPort"
        Me.ColIMUPort.Width = 80
        '
        'ButtonSave
        '
        Me.ButtonSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonSave.Location = New System.Drawing.Point(532, 425)
        Me.ButtonSave.Name = "ButtonSave"
        Me.ButtonSave.Size = New System.Drawing.Size(75, 23)
        Me.ButtonSave.TabIndex = 1
        Me.ButtonSave.Text = "Save"
        Me.ButtonSave.UseVisualStyleBackColor = True
        '
        'ButtonCancel
        '
        Me.ButtonCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonCancel.Location = New System.Drawing.Point(613, 425)
        Me.ButtonCancel.Name = "ButtonCancel"
        Me.ButtonCancel.Size = New System.Drawing.Size(75, 23)
        Me.ButtonCancel.TabIndex = 2
        Me.ButtonCancel.Text = "Cancel"
        Me.ButtonCancel.UseVisualStyleBackColor = True
        '
        'LidarDeviceEditorForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(700, 460)
        Me.Controls.Add(Me.ButtonCancel)
        Me.Controls.Add(Me.ButtonSave)
        Me.Controls.Add(Me.DataGridViewLidar)
        Me.Name = "LidarDeviceEditorForm"
        Me.Text = "LiDAR Device Editor"
        CType(Me.DataGridViewLidar, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents DataGridViewLidar As DataGridView
    Friend WithEvents ColID As DataGridViewTextBoxColumn
    Friend WithEvents ColEnabled As DataGridViewComboBoxColumn
    Friend WithEvents ColAdapterGuid As DataGridViewTextBoxColumn
    Friend WithEvents ColIPAddress As DataGridViewTextBoxColumn
    Friend WithEvents ColDataPort As DataGridViewTextBoxColumn
    Friend WithEvents ColIMUPort As DataGridViewTextBoxColumn
    Friend WithEvents ButtonSave As Button
    Friend WithEvents ButtonCancel As Button
End Class
