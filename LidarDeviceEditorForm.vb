Imports System.Xml

Public Class LidarDeviceEditorForm
    Private _xmlDoc As XmlDocument
    Private _isDirty As Boolean = False

    Public Sub New(xmlDoc As XmlDocument)
        InitializeComponent()
        _xmlDoc = xmlDoc

        ' ✅ Ensure form appears on top
        Me.TopMost = True
        Me.ShowInTaskbar = False ' Don't show in taskbar (it's a dialog)
    End Sub
    Private Sub LidarDeviceEditorForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "LiDAR Device Configuration"
        Me.Size = New Size(700, 500)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog ' ✅ Make it a proper dialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        LoadLidarDevices()

        ' ✅ Ensure it's visible and on top
        Me.BringToFront()
        Me.Activate()
    End Sub

    Private Sub LoadLidarDevices()
        DataGridViewLidar.Rows.Clear()

        ' Find LidarDevices node
        Dim lidarDevicesNode As XmlNode = _xmlDoc.SelectSingleNode("//LidarDevices")
        If lidarDevicesNode Is Nothing Then Return

        ' Load each Lidar device
        For Each lidarNode As XmlNode In lidarDevicesNode.SelectNodes("Lidar")
            Dim id As String = lidarNode.Attributes("id")?.Value
            Dim enabled As String = lidarNode.Attributes("enabled")?.Value
            Dim adapterGuid As String = lidarNode.SelectSingleNode("AdapterGuid")?.InnerText
            Dim ipAddress As String = lidarNode.SelectSingleNode("IpAddress")?.InnerText
            Dim dataPort As String = lidarNode.SelectSingleNode("DataPort")?.InnerText
            Dim imuPort As String = lidarNode.SelectSingleNode("ImuPort")?.InnerText

            DataGridViewLidar.Rows.Add(id, enabled, adapterGuid, ipAddress, dataPort, imuPort)
        Next
    End Sub

    Private Sub ButtonSave_Click(sender As Object, e As EventArgs) Handles ButtonSave.Click
        Try
            ' Update XML document
            Dim lidarDevicesNode As XmlNode = _xmlDoc.SelectSingleNode("//LidarDevices")
            If lidarDevicesNode Is Nothing Then
                lidarDevicesNode = _xmlDoc.CreateElement("LidarDevices")
                _xmlDoc.DocumentElement.AppendChild(lidarDevicesNode)
            End If

            lidarDevicesNode.RemoveAll() ' Clear existing

            For Each row As DataGridViewRow In DataGridViewLidar.Rows
                If row.IsNewRow Then Continue For

                Dim lidarNode As XmlElement = _xmlDoc.CreateElement("Lidar")
                lidarNode.SetAttribute("id", row.Cells(0).Value?.ToString())
                lidarNode.SetAttribute("enabled", row.Cells(1).Value?.ToString())

                Dim adapterNode As XmlElement = _xmlDoc.CreateElement("AdapterGuid")
                adapterNode.InnerText = row.Cells(2).Value?.ToString()
                lidarNode.AppendChild(adapterNode)

                Dim ipNode As XmlElement = _xmlDoc.CreateElement("IpAddress")
                ipNode.InnerText = row.Cells(3).Value?.ToString()
                lidarNode.AppendChild(ipNode)

                Dim dataPortNode As XmlElement = _xmlDoc.CreateElement("DataPort")
                dataPortNode.InnerText = row.Cells(4).Value?.ToString()
                lidarNode.AppendChild(dataPortNode)

                Dim imuPortNode As XmlElement = _xmlDoc.CreateElement("ImuPort")
                imuPortNode.InnerText = row.Cells(5).Value?.ToString()
                lidarNode.AppendChild(imuPortNode)

                lidarDevicesNode.AppendChild(lidarNode)
            Next

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Save failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DataGridViewLidar_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridViewLidar.CellValueChanged
        _isDirty = True
    End Sub
End Class