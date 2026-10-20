Imports System.Xml

''' <summary>
''' Dialog for editing the single vehicle (&lt;Vehicle&gt;: VehicleNumber and Processors) stored at
''' the root of config.xml.  One vehicle per config.xml; a legacy &lt;Vehicles&gt; layout is migrated
''' on save.  Receives the live XmlDocument from ConfigurationEditorForm so changes are written
''' back into the same document that will be saved to disk on OK.
''' </summary>
Public Class VehicleEditorForm

    Private _xmlDoc As XmlDocument
    Private _isDirty As Boolean = False

    ' Column indices — must match the DataGridViewVehicles column order
    Private Const COL_NUMBER As Integer = 0
    Private Const COL_PROCESSORS As Integer = 1

    Public Sub New(xmlDoc As XmlDocument)
        InitializeComponent()
        _xmlDoc = xmlDoc
        Me.TopMost = True
        Me.ShowInTaskbar = False
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' LOAD
    ' ─────────────────────────────────────────────────────────────
    Private Sub VehicleEditorForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Vehicle Configuration Editor"
        Me.Size = New Size(1100, 520)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = False
        LoadVehicles()
        Me.BringToFront()
        Me.Activate()
    End Sub

    Private Sub LoadVehicles()
        DataGridViewVehicles.Rows.Clear()
        Dim v As VehicleConfig = ReadVehicleFromXml(_xmlDoc)
        If v Is Nothing Then
            DataGridViewVehicles.Rows.Add("UNDEFINED", "")
        Else
            DataGridViewVehicles.Rows.Add(v.VehicleNumber, String.Join(",", v.Processors))
        End If
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' SAVE
    ' ─────────────────────────────────────────────────────────────
    Private Sub ButtonSave_Click(sender As Object, e As EventArgs) Handles ButtonSave.Click
        Try
            DataGridViewVehicles.EndEdit()
            Dim row As DataGridViewRow = DataGridViewVehicles.Rows(0)
            Dim number As String = If(row.Cells(COL_NUMBER).Value?.ToString(), String.Empty).Trim()
            If number.Length = 0 Then
                Throw New FormatException("Vehicle number must be non-empty.")
            End If
            Dim processorText As String = If(row.Cells(COL_PROCESSORS).Value?.ToString(), String.Empty)
            If Not String.Equals(number, "UNDEFINED", StringComparison.OrdinalIgnoreCase) Then
                ProcessorIdentity.ParseList(processorText.Split(","c), number)
            End If

            IncaConfiguration.MigrateLegacyVehicleLayout(_xmlDoc)
            Dim vehicleEl As XmlElement = TryCast(_xmlDoc.SelectSingleNode("/Configuration/Vehicle"), XmlElement)
            If vehicleEl Is Nothing Then
                vehicleEl = _xmlDoc.CreateElement("Vehicle")
                _xmlDoc.DocumentElement.AppendChild(vehicleEl)
            End If
            AppendTextElement(vehicleEl, "VehicleNumber", number)
            AppendTextElement(vehicleEl, "Processors", String.Join(",", processorText.Split(","c).Select(Function(token) token.Trim())))
            For Each obsoleteName As String In {"CameraPositions", "ZipMF4Files"}
                Dim obsolete As XmlNode = vehicleEl.SelectSingleNode(obsoleteName)
                If obsolete IsNot Nothing Then vehicleEl.RemoveChild(obsolete)
            Next

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Save failed: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AppendTextElement(parent As XmlElement, name As String, value As String)
        Dim el As XmlElement = TryCast(parent.SelectSingleNode(name), XmlElement)
        If el Is Nothing Then
            el = _xmlDoc.CreateElement(name)
            parent.AppendChild(el)
        End If
        el.InnerText = If(value, "")
    End Sub

    Private Sub ButtonCancel_Click(sender As Object, e As EventArgs) Handles ButtonCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub DataGridViewVehicles_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) _
        Handles DataGridViewVehicles.CellValueChanged
        _isDirty = True
    End Sub

End Class
