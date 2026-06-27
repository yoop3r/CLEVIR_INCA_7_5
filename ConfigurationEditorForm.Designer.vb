<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ConfigurationEditorForm
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
        Me.DataGridViewParams = New System.Windows.Forms.DataGridView()
        Me.ColumnParameter = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColumnValue = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColumnDescription = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ButtonSave = New System.Windows.Forms.Button()
        Me.ButtonCancel = New System.Windows.Forms.Button()
        Me.LabelStatus = New System.Windows.Forms.Label()
        CType(Me.DataGridViewParams, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'DataGridViewParams
        '
        Me.DataGridViewParams.AllowUserToAddRows = False
        Me.DataGridViewParams.AllowUserToDeleteRows = False
        Me.DataGridViewParams.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridViewParams.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridViewParams.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ColumnParameter, Me.ColumnValue, Me.ColumnDescription})
        Me.DataGridViewParams.Location = New System.Drawing.Point(18, 69)
        Me.DataGridViewParams.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.DataGridViewParams.Name = "DataGridViewParams"
        Me.DataGridViewParams.Size = New System.Drawing.Size(1422, 692)
        Me.DataGridViewParams.TabIndex = 2
        '
        'ColumnParameter
        '
        Me.ColumnParameter.HeaderText = "Parameter Name"
        Me.ColumnParameter.Name = "ColumnParameter"
        Me.ColumnParameter.ReadOnly = True
        Me.ColumnParameter.Width = 200
        '
        'ColumnValue
        '
        Me.ColumnValue.HeaderText = "Value"
        Me.ColumnValue.Name = "ColumnValue"
        Me.ColumnValue.Width = 300
        '
        'ColumnDescription
        '
        Me.ColumnDescription.HeaderText = "Description"
        Me.ColumnDescription.Name = "ColumnDescription"
        Me.ColumnDescription.ReadOnly = True
        Me.ColumnDescription.Width = 240
        '
        'ButtonSave
        '
        Me.ButtonSave.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonSave.Location = New System.Drawing.Point(1032, 785)
        Me.ButtonSave.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.ButtonSave.Name = "ButtonSave"
        Me.ButtonSave.Size = New System.Drawing.Size(180, 54)
        Me.ButtonSave.TabIndex = 4
        Me.ButtonSave.Text = "💾 Save"
        Me.ButtonSave.UseVisualStyleBackColor = True
        '
        'ButtonCancel
        '
        Me.ButtonCancel.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonCancel.Location = New System.Drawing.Point(837, 785)
        Me.ButtonCancel.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.ButtonCancel.Name = "ButtonCancel"
        Me.ButtonCancel.Size = New System.Drawing.Size(180, 54)
        Me.ButtonCancel.TabIndex = 3
        Me.ButtonCancel.Text = "❌ Cancel"
        Me.ButtonCancel.UseVisualStyleBackColor = True
        '
        'LabelStatus
        '
        Me.LabelStatus.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.LabelStatus.AutoSize = True
        Me.LabelStatus.Location = New System.Drawing.Point(18, 800)
        Me.LabelStatus.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.LabelStatus.Name = "LabelStatus"
        Me.LabelStatus.Size = New System.Drawing.Size(55, 20)
        Me.LabelStatus.TabIndex = 6
        Me.LabelStatus.Text = "Ready"
        '
        'ConfigurationEditorForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonCancel
        Me.ClientSize = New System.Drawing.Size(1458, 863)
        Me.Controls.Add(Me.LabelStatus)
        Me.Controls.Add(Me.ButtonSave)
        Me.Controls.Add(Me.ButtonCancel)
        Me.Controls.Add(Me.DataGridViewParams)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "ConfigurationEditorForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "CLEVIR Configuration Editor"
        CType(Me.DataGridViewParams, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents DataGridViewParams As DataGridView
    Friend WithEvents ButtonSave As Button
    Friend WithEvents ButtonCancel As Button
    Friend WithEvents LabelStatus As Label
    Friend WithEvents ColumnParameter As DataGridViewTextBoxColumn
    Friend WithEvents ColumnValue As DataGridViewTextBoxColumn
    Friend WithEvents ColumnDescription As DataGridViewTextBoxColumn
End Class

