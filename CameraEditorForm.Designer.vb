<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class CameraEditorForm
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
        Me.DataGridViewCameras = New System.Windows.Forms.DataGridView()
        Me.ColPosition = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColIPAddress = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColEnabled = New System.Windows.Forms.DataGridViewComboBoxColumn()
        Me.ButtonSave = New System.Windows.Forms.Button()
        Me.ButtonCancel = New System.Windows.Forms.Button()
        Me.LabelHint = New System.Windows.Forms.Label()
        CType(Me.DataGridViewCameras, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'DataGridViewCameras
        '
        Me.DataGridViewCameras.AllowUserToAddRows = True
        Me.DataGridViewCameras.AllowUserToDeleteRows = True
        Me.DataGridViewCameras.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridViewCameras.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridViewCameras.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ColPosition, Me.ColIPAddress, Me.ColEnabled})
        Me.DataGridViewCameras.Location = New System.Drawing.Point(12, 44)
        Me.DataGridViewCameras.Name = "DataGridViewCameras"
        Me.DataGridViewCameras.Size = New System.Drawing.Size(560, 360)
        Me.DataGridViewCameras.TabIndex = 0
        '
        'ColPosition
        '
        Me.ColPosition.HeaderText = "Position"
        Me.ColPosition.Name = "ColPosition"
        Me.ColPosition.ToolTipText = "Mount position e.g. FRONT, REAR, LEFTREAR, HMI"
        Me.ColPosition.Width = 120
        '
        'ColIPAddress
        '
        Me.ColIPAddress.HeaderText = "IP Address"
        Me.ColIPAddress.Name = "ColIPAddress"
        Me.ColIPAddress.Width = 140
        '
        'ColEnabled
        '
        Me.ColEnabled.HeaderText = "Enabled"
        Me.ColEnabled.Items.AddRange(New Object() {"true", "false"})
        Me.ColEnabled.Name = "ColEnabled"
        Me.ColEnabled.Width = 80
        '
        'LabelHint
        '
        Me.LabelHint.AutoSize = False
        Me.LabelHint.Location = New System.Drawing.Point(12, 12)
        Me.LabelHint.Size = New System.Drawing.Size(560, 28)
        Me.LabelHint.Text = "Each row is a <Camera> entry. Position = mount label (FRONT/REAR/…). MaxCameras is auto-calculated."
        Me.LabelHint.Font = New System.Drawing.Font("Segoe UI", 8.5F)
        Me.LabelHint.ForeColor = System.Drawing.Color.DarkSlateGray
        '
        'ButtonSave
        '
        Me.ButtonSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonSave.Location = New System.Drawing.Point(413, 416)
        Me.ButtonSave.Name = "ButtonSave"
        Me.ButtonSave.Size = New System.Drawing.Size(75, 28)
        Me.ButtonSave.TabIndex = 1
        Me.ButtonSave.Text = "Save"
        Me.ButtonSave.UseVisualStyleBackColor = True
        '
        'ButtonCancel
        '
        Me.ButtonCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonCancel.Location = New System.Drawing.Point(497, 416)
        Me.ButtonCancel.Name = "ButtonCancel"
        Me.ButtonCancel.Size = New System.Drawing.Size(75, 28)
        Me.ButtonCancel.TabIndex = 2
        Me.ButtonCancel.Text = "Cancel"
        Me.ButtonCancel.UseVisualStyleBackColor = True
        '
        'CameraEditorForm
        '
        Me.AcceptButton = Me.ButtonSave
        Me.CancelButton = Me.ButtonCancel
        Me.ClientSize = New System.Drawing.Size(584, 456)
        Me.Controls.Add(Me.LabelHint)
        Me.Controls.Add(Me.DataGridViewCameras)
        Me.Controls.Add(Me.ButtonSave)
        Me.Controls.Add(Me.ButtonCancel)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "CameraEditorForm"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Camera Configuration"
        CType(Me.DataGridViewCameras, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents DataGridViewCameras As System.Windows.Forms.DataGridView
    Friend WithEvents ColPosition As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColIPAddress As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColEnabled As System.Windows.Forms.DataGridViewComboBoxColumn
    Friend WithEvents ButtonSave As System.Windows.Forms.Button
    Friend WithEvents ButtonCancel As System.Windows.Forms.Button
    Friend WithEvents LabelHint As System.Windows.Forms.Label
End Class
