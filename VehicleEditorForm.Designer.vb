<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class VehicleEditorForm
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
        Me.DataGridViewVehicles = New System.Windows.Forms.DataGridView()
        Me.ColVehicleNumber = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColProcessors = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColCameraPositions = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColCanMonitors = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColDataUploadPath = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColCLEVIRFilesPath = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColZipMF4Files = New System.Windows.Forms.DataGridViewComboBoxColumn()
        Me.ColConfigName = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ButtonAddVehicle = New System.Windows.Forms.Button()
        Me.ButtonRemoveVehicle = New System.Windows.Forms.Button()
        Me.ButtonSave = New System.Windows.Forms.Button()
        Me.ButtonCancel = New System.Windows.Forms.Button()
        Me.LabelHint = New System.Windows.Forms.Label()
        CType(Me.DataGridViewVehicles, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'DataGridViewVehicles
        '
        Me.DataGridViewVehicles.AllowUserToAddRows = False
        Me.DataGridViewVehicles.AllowUserToDeleteRows = False
        Me.DataGridViewVehicles.AllowUserToOrderColumns = True
        Me.DataGridViewVehicles.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridViewVehicles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.DataGridViewVehicles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridViewVehicles.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {
            Me.ColVehicleNumber, Me.ColProcessors,
            Me.ColCameraPositions, Me.ColCanMonitors,
            Me.ColDataUploadPath, Me.ColCLEVIRFilesPath,
            Me.ColZipMF4Files, Me.ColConfigName})
        Me.DataGridViewVehicles.Location = New System.Drawing.Point(12, 40)
        Me.DataGridViewVehicles.Name = "DataGridViewVehicles"
        Me.DataGridViewVehicles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.DataGridViewVehicles.Size = New System.Drawing.Size(1070, 390)
        Me.DataGridViewVehicles.TabIndex = 0
        '
        'ColVehicleNumber
        '
        Me.ColVehicleNumber.HeaderText = "Vehicle Number"
        Me.ColVehicleNumber.Name = "ColVehicleNumber"
        '
        'ColProcessors
        '
        Me.ColProcessors.HeaderText = "Processors (comma-sep)"
        Me.ColProcessors.Name = "ColProcessors"
        Me.ColProcessors.ToolTipText = "Up to 6 processor names e.g. ACP3_MCU,NA,NA,NA,NA,NA"
        '
        'ColCameraPositions
        '
        Me.ColCameraPositions.HeaderText = "Camera Positions (comma-sep)"
        Me.ColCameraPositions.Name = "ColCameraPositions"
        Me.ColCameraPositions.ToolTipText = "Up to 9 camera position names e.g. FRONT,REAR,NA,..."
        '
        'ColCanMonitors
        '
        Me.ColCanMonitors.HeaderText = "CAN Monitors (comma-sep)"
        Me.ColCanMonitors.Name = "ColCanMonitors"
        Me.ColCanMonitors.ToolTipText = "4 CAN monitor IDs e.g. 886,886,886,886"
        '
        'ColDataUploadPath
        '
        Me.ColDataUploadPath.HeaderText = "Data Upload Path"
        Me.ColDataUploadPath.Name = "ColDataUploadPath"
        Me.ColDataUploadPath.ToolTipText = "e.g. ACP3\VehicleData\"
        '
        'ColCLEVIRFilesPath
        '
        Me.ColCLEVIRFilesPath.HeaderText = "CLEVIR Files Path"
        Me.ColCLEVIRFilesPath.Name = "ColCLEVIRFilesPath"
        Me.ColCLEVIRFilesPath.ToolTipText = "e.g. Current\ACP3"
        '
        'ColZipMF4Files
        '
        Me.ColZipMF4Files.HeaderText = "Zip MF4 Files"
        Me.ColZipMF4Files.Items.AddRange(New Object() {"True", "False"})
        Me.ColZipMF4Files.Name = "ColZipMF4Files"
        '
        'ColConfigName
        '
        Me.ColConfigName.HeaderText = "Config Name"
        Me.ColConfigName.Name = "ColConfigName"
        Me.ColConfigName.ToolTipText = "e.g. ACP3_1P1C"
        '
        'LabelHint
        '
        Me.LabelHint.AutoSize = True
        Me.LabelHint.Location = New System.Drawing.Point(12, 14)
        Me.LabelHint.Name = "LabelHint"
        Me.LabelHint.Size = New System.Drawing.Size(400, 13)
        Me.LabelHint.Text = "Processors / Camera Positions / CAN Monitors: enter comma-separated values."
        '
        'ButtonAddVehicle
        '
        Me.ButtonAddVehicle.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonAddVehicle.Location = New System.Drawing.Point(12, 440)
        Me.ButtonAddVehicle.Name = "ButtonAddVehicle"
        Me.ButtonAddVehicle.Size = New System.Drawing.Size(110, 27)
        Me.ButtonAddVehicle.TabIndex = 2
        Me.ButtonAddVehicle.Text = "Add Vehicle"
        Me.ButtonAddVehicle.UseVisualStyleBackColor = True
        '
        'ButtonRemoveVehicle
        '
        Me.ButtonRemoveVehicle.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonRemoveVehicle.Location = New System.Drawing.Point(130, 440)
        Me.ButtonRemoveVehicle.Name = "ButtonRemoveVehicle"
        Me.ButtonRemoveVehicle.Size = New System.Drawing.Size(120, 27)
        Me.ButtonRemoveVehicle.TabIndex = 3
        Me.ButtonRemoveVehicle.Text = "Remove Selected"
        Me.ButtonRemoveVehicle.UseVisualStyleBackColor = True
        '
        'ButtonSave
        '
        Me.ButtonSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonSave.Location = New System.Drawing.Point(920, 440)
        Me.ButtonSave.Name = "ButtonSave"
        Me.ButtonSave.Size = New System.Drawing.Size(75, 27)
        Me.ButtonSave.TabIndex = 4
        Me.ButtonSave.Text = "Save"
        Me.ButtonSave.UseVisualStyleBackColor = True
        '
        'ButtonCancel
        '
        Me.ButtonCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonCancel.Location = New System.Drawing.Point(1005, 440)
        Me.ButtonCancel.Name = "ButtonCancel"
        Me.ButtonCancel.Size = New System.Drawing.Size(75, 27)
        Me.ButtonCancel.TabIndex = 5
        Me.ButtonCancel.Text = "Cancel"
        Me.ButtonCancel.UseVisualStyleBackColor = True
        '
        'VehicleEditorForm
        '
        Me.AcceptButton = Me.ButtonSave
        Me.CancelButton = Me.ButtonCancel
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1094, 480)
        Me.Controls.Add(Me.LabelHint)
        Me.Controls.Add(Me.DataGridViewVehicles)
        Me.Controls.Add(Me.ButtonAddVehicle)
        Me.Controls.Add(Me.ButtonRemoveVehicle)
        Me.Controls.Add(Me.ButtonSave)
        Me.Controls.Add(Me.ButtonCancel)
        Me.MinimumSize = New System.Drawing.Size(900, 400)
        Me.Name = "VehicleEditorForm"
        Me.Text = "Vehicle Configuration Editor"
        CType(Me.DataGridViewVehicles, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents DataGridViewVehicles As System.Windows.Forms.DataGridView
    Friend WithEvents ColVehicleNumber As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColProcessors As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColCameraPositions As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColCanMonitors As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColDataUploadPath As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColCLEVIRFilesPath As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColZipMF4Files As System.Windows.Forms.DataGridViewComboBoxColumn
    Friend WithEvents ColConfigName As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ButtonAddVehicle As System.Windows.Forms.Button
    Friend WithEvents ButtonRemoveVehicle As System.Windows.Forms.Button
    Friend WithEvents ButtonSave As System.Windows.Forms.Button
    Friend WithEvents ButtonCancel As System.Windows.Forms.Button
    Friend WithEvents LabelHint As System.Windows.Forms.Label
End Class
