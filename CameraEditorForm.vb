Imports System.Xml

Public Class CameraEditorForm
    Private _xmlDoc As XmlDocument

    Public Sub New(xmlDoc As XmlDocument)
        InitializeComponent()
        _xmlDoc = xmlDoc
        Me.TopMost = True
        Me.ShowInTaskbar = False
    End Sub

    Private Sub CameraEditorForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadCameras()
        Me.BringToFront()
        Me.Activate()
    End Sub

    Private Sub LoadCameras()
        DataGridViewCameras.Rows.Clear()

        Dim camerasNode As XmlNode = _xmlDoc.SelectSingleNode("//CameraConfiguration/Cameras")
        If camerasNode Is Nothing Then Return

        For Each camNode As XmlNode In camerasNode.SelectNodes("Camera")
            Dim position As String = camNode.Attributes("position")?.Value
            Dim ipAddress As String = camNode.Attributes("ipAddress")?.Value
            Dim enabled As String = If(camNode.Attributes("enabled")?.Value, "true").ToLower()
            DataGridViewCameras.Rows.Add(position, ipAddress, enabled)
        Next
    End Sub

    Private Sub ButtonSave_Click(sender As Object, e As EventArgs) Handles ButtonSave.Click
        Try
            ' Locate or create CameraConfiguration and Cameras nodes
            Dim cfgNode As XmlNode = _xmlDoc.SelectSingleNode("//CameraConfiguration")
            If cfgNode Is Nothing Then
                cfgNode = _xmlDoc.CreateElement("CameraConfiguration")
                _xmlDoc.DocumentElement.AppendChild(cfgNode)
            End If

            Dim camerasNode As XmlNode = cfgNode.SelectSingleNode("Cameras")
            If camerasNode Is Nothing Then
                camerasNode = _xmlDoc.CreateElement("Cameras")
                cfgNode.AppendChild(camerasNode)
            Else
                camerasNode.RemoveAll()
            End If

            ' Count enabled cameras for MaxCameras
            Dim enabledCount As Integer = 0

            For Each row As DataGridViewRow In DataGridViewCameras.Rows
                If row.IsNewRow Then Continue For

                Dim position As String = row.Cells(0).Value?.ToString()?.Trim()
                Dim ipAddress As String = row.Cells(1).Value?.ToString()?.Trim()
                Dim enabledStr As String = If(row.Cells(2).Value?.ToString(), "false").ToLower()

                If String.IsNullOrWhiteSpace(position) OrElse String.IsNullOrWhiteSpace(ipAddress) Then
                    Continue For
                End If

                Dim camElem As XmlElement = _xmlDoc.CreateElement("Camera")
                camElem.SetAttribute("position", position)
                camElem.SetAttribute("ipAddress", ipAddress)
                camElem.SetAttribute("enabled", enabledStr)
                camerasNode.AppendChild(camElem)

                If enabledStr = "true" Then enabledCount += 1
            Next

            ' Update MaxCameras to the actual count of enabled cameras
            Dim maxCamsNode As XmlNode = cfgNode.SelectSingleNode("MaxCameras")
            If maxCamsNode Is Nothing Then
                maxCamsNode = _xmlDoc.CreateElement("MaxCameras")
                cfgNode.InsertBefore(maxCamsNode, camerasNode)
            End If
            maxCamsNode.InnerText = enabledCount.ToString()

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Save failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DataGridViewCameras_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridViewCameras.CellValueChanged
        ' No-op: changes accumulate until Save is clicked
    End Sub
End Class
