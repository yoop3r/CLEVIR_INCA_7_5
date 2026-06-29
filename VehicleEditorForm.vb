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
    Private Const COL_CAN As Integer = 3
    Private Const COL_UPLOAD As Integer = 4
    Private Const COL_CLEVIR As Integer = 5
    Private Const COL_ZIP As Integer = 6
    Private Const COL_CONFIG As Integer = 7

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

        For Each v As VehicleConfig In vehicles
            DataGridViewVehicles.Rows.Add(
                v.VehicleNumber,
                String.Join(",", v.Processors),
                String.Join(",", v.CameraPositions),
                String.Join(",", v.CanMonitors),
                v.DataUploadPath,
                v.CLEVIRFilesPath,
                v.ZipMF4Files.ToString(),
                v.ConfigName)
        Next
    End Sub

    ' ─────────────────────────────────────────────────────────────
    ' ADD / REMOVE ROW
    ' ─────────────────────────────────────────────────────────────
    Private Sub ButtonAddVehicle_Click(sender As Object, e As EventArgs) Handles ButtonAddVehicle.Click
        DataGridViewVehicles.Rows.Add(
            "XXXXXXXX",
            "ACP3_MCU,NA,NA,NA,NA,NA",
            "FRONT,REAR,NA,NA,NA,NA,NA,NA,NA",
            "886,886,886,886",
            "ACP3\VehicleData\",
            "Current\ACP3",
            "True",
            "ACP3_1P1C")
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
            ' Locate or create the <Vehicles> node
            Dim vehiclesNode As XmlNode = _xmlDoc.SelectSingleNode("//Vehicles")
            If vehiclesNode Is Nothing Then
                vehiclesNode = _xmlDoc.CreateElement("Vehicles")
                _xmlDoc.DocumentElement.AppendChild(vehiclesNode)
            End If
            vehiclesNode.RemoveAll()

            For Each row As DataGridViewRow In DataGridViewVehicles.Rows
                If row.IsNewRow Then Continue For

                Dim vehicleEl As XmlElement = _xmlDoc.CreateElement("Vehicle")
                AppendTextElement(vehicleEl, "VehicleNumber", CStr(row.Cells(COL_NUMBER).Value))
                AppendTextElement(vehicleEl, "Processors", CStr(row.Cells(COL_PROCESSORS).Value))
                AppendTextElement(vehicleEl, "CameraPositions", CStr(row.Cells(COL_CAMERAS).Value))
                AppendTextElement(vehicleEl, "CanMonitors", CStr(row.Cells(COL_CAN).Value))
                AppendTextElement(vehicleEl, "DataUploadPath", CStr(row.Cells(COL_UPLOAD).Value))
                AppendTextElement(vehicleEl, "CLEVIRFilesPath", CStr(row.Cells(COL_CLEVIR).Value))
                AppendTextElement(vehicleEl, "ZipMF4Files", CStr(row.Cells(COL_ZIP).Value))
                AppendTextElement(vehicleEl, "ConfigName", CStr(row.Cells(COL_CONFIG).Value))
                vehiclesNode.AppendChild(vehicleEl)
            Next

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Save failed: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AppendTextElement(parent As XmlElement, name As String, value As String)
        Dim el As XmlElement = _xmlDoc.CreateElement(name)
        el.InnerText = If(value, "")
        parent.AppendChild(el)
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
