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
            Me.ColVehicleNumber, Me.ColProcessors})
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
        'LabelHint
        '
        Me.LabelHint.AutoSize = True
        Me.LabelHint.Location = New System.Drawing.Point(12, 14)
        Me.LabelHint.Name = "LabelHint"
        Me.LabelHint.Size = New System.Drawing.Size(400, 13)
        Me.LabelHint.Text = "One vehicle per config.xml. Processors: enter comma-separated values. Cameras are configured via Edit Cameras."
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
    Friend WithEvents ButtonSave As System.Windows.Forms.Button
    Friend WithEvents ButtonCancel As System.Windows.Forms.Button
    Friend WithEvents LabelHint As System.Windows.Forms.Label
End Class
