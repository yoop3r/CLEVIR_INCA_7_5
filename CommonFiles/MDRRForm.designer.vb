<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> Partial Class MDRRForm
#Region "Windows Form Designer generated code "
    <System.Diagnostics.DebuggerNonUserCode()> Public Sub New()
        MyBase.New()
        'This call is required by the Windows Form Designer.
        InitializeComponent()
    End Sub
    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> Protected Overloads Overrides Sub Dispose(ByVal Disposing As Boolean)
        If Disposing Then
            If Not components Is Nothing Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(Disposing)
    End Sub
    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer
    Public ToolTip1 As System.Windows.Forms.ToolTip
    Public WithEvents Label4 As Microsoft.VisualBasic.Compatibility.VB6.LabelArray
    Public WithEvents Picture1 As Microsoft.VisualBasic.Compatibility.VB6.PictureBoxArray
    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MDRRForm))
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.Label4 = New Microsoft.VisualBasic.Compatibility.VB6.LabelArray(Me.components)
        Me.Picture1 = New Microsoft.VisualBasic.Compatibility.VB6.PictureBoxArray(Me.components)
        Me.AxMSChart1 = New AxMSChart20Lib.AxMSChart
        Me.MSChart1 = New AxMSChart20Lib.AxMSChart
        Me.AxMSChart2 = New AxMSChart20Lib.AxMSChart
        Me.AxMSChart3 = New AxMSChart20Lib.AxMSChart
        CType(Me.Label4, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.Picture1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.AxMSChart1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.MSChart1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.AxMSChart2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.AxMSChart3, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'AxMSChart1
        '
        Me.AxMSChart1.Location = New System.Drawing.Point(394, 3)
        Me.AxMSChart1.Name = "AxMSChart1"
        Me.AxMSChart1.OcxState = CType(resources.GetObject("AxMSChart1.OcxState"), System.Windows.Forms.AxHost.State)
        Me.AxMSChart1.Size = New System.Drawing.Size(368, 249)
        Me.AxMSChart1.TabIndex = 23
        '
        'MSChart1
        '
        Me.MSChart1.Location = New System.Drawing.Point(2, 3)
        Me.MSChart1.Name = "MSChart1"
        Me.MSChart1.OcxState = CType(resources.GetObject("MSChart1.OcxState"), System.Windows.Forms.AxHost.State)
        Me.MSChart1.Size = New System.Drawing.Size(367, 249)
        Me.MSChart1.TabIndex = 24
        '
        'AxMSChart2
        '
        Me.AxMSChart2.Location = New System.Drawing.Point(2, 280)
        Me.AxMSChart2.Name = "AxMSChart2"
        Me.AxMSChart2.OcxState = CType(resources.GetObject("AxMSChart2.OcxState"), System.Windows.Forms.AxHost.State)
        Me.AxMSChart2.Size = New System.Drawing.Size(367, 235)
        Me.AxMSChart2.TabIndex = 25
        '
        'AxMSChart3
        '
        Me.AxMSChart3.Location = New System.Drawing.Point(394, 280)
        Me.AxMSChart3.Name = "AxMSChart3"
        Me.AxMSChart3.OcxState = CType(resources.GetObject("AxMSChart3.OcxState"), System.Windows.Forms.AxHost.State)
        Me.AxMSChart3.Size = New System.Drawing.Size(368, 235)
        Me.AxMSChart3.TabIndex = 26
        '
        'MDRRForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 14.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ActiveBorder
        Me.ClientSize = New System.Drawing.Size(784, 527)
        Me.Controls.Add(Me.AxMSChart3)
        Me.Controls.Add(Me.AxMSChart2)
        Me.Controls.Add(Me.MSChart1)
        Me.Controls.Add(Me.AxMSChart1)
        Me.Cursor = System.Windows.Forms.Cursors.Default
        Me.Font = New System.Drawing.Font("Arial", 8.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Location = New System.Drawing.Point(11, 30)
        Me.Name = "MDRRForm"
        Me.RightToLeft = System.Windows.Forms.RightToLeft.No
        Me.Text = "MDRR Status"
        CType(Me.Label4, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.Picture1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.AxMSChart1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.MSChart1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.AxMSChart2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.AxMSChart3, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Public WithEvents AxMSChart1 As AxMSChart20Lib.AxMSChart
    Public WithEvents MSChart1 As AxMSChart20Lib.AxMSChart
    Public WithEvents AxMSChart2 As AxMSChart20Lib.AxMSChart
    Public WithEvents AxMSChart3 As AxMSChart20Lib.AxMSChart
#End Region
End Class