Imports System.Xml

''' <summary>
''' Dialog for viewing, adding, editing, and deleting vehicle configurations stored in
''' the &lt;Vehicles&gt; section of config.xml.  Receives the live XmlDocument from
''' ConfigurationEditorForm so changes are written back into the same document that
''' will be saved to disk on OK.
''' </summary>
Public Class VehicleEditorForm

    Private _xmlDoc As XmlDocument
    Private _isDirty As Boolean = False

    ' Column indices — must match the DataGridViewVehicles column order
    Private Const COL_NUMBER As Integer = 0
    Private Const COL_PROCESSORS As Integer = 1
    Private Const COL_CAMERAS As Integer = 2
    Private Const COL_ZIP As Integer = 3

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
        Dim vehicles As List(Of VehicleConfig) = ReadVehiclesFromXml(_xmlDoc)
        If vehicles Is Nothing Then Return

        For i As Integer = 0 To vehicles.Count - 1
            Dim v As VehicleConfig = vehicles(i)
            Dim rowIndex As Integer = DataGridViewVehicles.Rows.Add(
                v.VehicleNumber,
                String.Join(",", v.Processors),
                String.Join(",", v.CameraPositions),
                v.ZipMF4Files.ToString())
            DataGridViewVehicles.Rows(rowIndex).Tag = _xmlDoc.SelectNodes("/Configuration/Vehicles/Vehicle")(i)
        Next
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' ADD / REMOVE ROW
    ' ─────────────────────────────────────────────────────────────
    Private Sub ButtonAddVehicle_Click(sender As Object, e As EventArgs) Handles ButtonAddVehicle.Click
        DataGridViewVehicles.Rows.Add(
            "XXXXXXXX",
            "",
            "FRONT,REAR,NA,NA,NA,NA,NA,NA,NA",
            "True")
        _isDirty = True
        Dim newRow As Integer = DataGridViewVehicles.Rows.Count - 1
        DataGridViewVehicles.CurrentCell = DataGridViewVehicles.Rows(newRow).Cells(COL_NUMBER)
        DataGridViewVehicles.BeginEdit(True)
    End Sub

    Private Sub ButtonRemoveVehicle_Click(sender As Object, e As EventArgs) Handles ButtonRemoveVehicle.Click
        If DataGridViewVehicles.SelectedRows.Count = 0 Then Return
        Dim vehicleNum As String = DataGridViewVehicles.SelectedRows(0).Cells(COL_NUMBER).Value?.ToString()
        If MessageBox.Show($"Remove vehicle '{vehicleNum}'?", "Confirm",
                           MessageBoxButtons.YesNo, MessageBoxIcon.Warning) = DialogResult.Yes Then
            DataGridViewVehicles.Rows.Remove(DataGridViewVehicles.SelectedRows(0))
            _isDirty = True
        End If
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' SAVE
    ' ─────────────────────────────────────────────────────────────
    Private Sub ButtonSave_Click(sender As Object, e As EventArgs) Handles ButtonSave.Click
        Try
            Dim original As XmlNode = _xmlDoc.SelectSingleNode("/Configuration/Vehicles")
            Dim vehiclesNode As XmlNode = If(original Is Nothing, _xmlDoc.CreateElement("Vehicles"), original.CloneNode(True))
            For Each child As XmlNode In vehiclesNode.SelectNodes("Vehicle")
                vehiclesNode.RemoveChild(child)
            Next
            Dim numbers As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each row As DataGridViewRow In DataGridViewVehicles.Rows
                If row.IsNewRow Then Continue For
                Dim number As String = If(row.Cells(COL_NUMBER).Value?.ToString(), String.Empty).Trim()
                If number.Length = 0 OrElse Not numbers.Add(number) Then
                    Throw New FormatException("Vehicle numbers must be non-empty and unique: '" & number & "'.")
                End If
                Dim processorText As String = If(row.Cells(COL_PROCESSORS).Value?.ToString(), String.Empty)
                Dim processors As ProcessorIdentity() = ProcessorIdentity.ParseList(processorText.Split(","c), number)
                Dim source As XmlNode = TryCast(row.Tag, XmlNode)
                Dim vehicleEl As XmlElement = If(source Is Nothing, _xmlDoc.CreateElement("Vehicle"), CType(source.CloneNode(True), XmlElement))
                AppendTextElement(vehicleEl, "VehicleNumber", number)
                AppendTextElement(vehicleEl, "Processors", String.Join(",", processorText.Split(","c).Select(Function(token) token.Trim())))
                AppendTextElement(vehicleEl, "CameraPositions", CStr(row.Cells(COL_CAMERAS).Value))
                AppendTextElement(vehicleEl, "ZipMF4Files", CStr(row.Cells(COL_ZIP).Value))
                vehiclesNode.AppendChild(vehicleEl)
            Next
            If original Is Nothing Then
                _xmlDoc.DocumentElement.AppendChild(vehiclesNode)
            Else
                _xmlDoc.DocumentElement.ReplaceChild(vehiclesNode, original)
            End If

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
