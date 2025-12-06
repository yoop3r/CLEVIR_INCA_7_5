Imports System.IO
Imports System.Xml

Public Class ConfigurationEditorForm
    Private _currentDrvNumber As Integer = 0
    Private _configData As New Dictionary(Of String, String)
    Private _isDirty As Boolean = False

    Private Sub ConfigurationEditorForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            InitializeUI()
            LoadConfiguration(0) ' Load DRV00 by default

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ConfigEditor Load: {ex.Message}")
            MessageBox.Show($"Failed to load configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub InitializeUI()
        ' Set form properties
        Me.Text = "CLEVIR Configuration Editor"
        Me.Size = New Size(800, 600)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' ✅ UPDATED: Populate DRV dropdown with DRVR00-DRVR05
        ComboBoxDRV.Items.Add("config.xml")
        For i As Integer = 0 To 5
            ComboBoxDRV.Items.Add($"DRV{i:D2}.xml")
        Next
        ComboBoxDRV.SelectedIndex = 0 ' Select config.xml by default
        ' Add OXTS Quick Toggle Button
        Dim btnToggleOxts As New Button With {
                .Text = "Toggle OXTS",
                .Location = New Point(650, 10),
                .Size = New Size(120, 30),
                .BackColor = Color.LightGreen
                }
        AddHandler btnToggleOxts.Click, AddressOf ButtonToggleOxts_Click
        Me.Controls.Add(btnToggleOxts)
    End Sub

    Private Sub ButtonToggleOxts_Click(sender As Object, e As EventArgs)
        Try
            ' Find OxtsEnabled row
            For i As Integer = 0 To DataGridViewParams.Rows.Count - 1
                Dim paramName As String = DataGridViewParams.Rows(i).Cells(0).Value?.ToString()
                If paramName = "OxtsEnabled" Then
                    Dim currentValue As String = DataGridViewParams.Rows(i).Cells(1).Value?.ToString()
                    Dim newValue As String = If(currentValue = "True", "False", "True")

                    DataGridViewParams.Rows(i).Cells(1).Value = newValue
                    _isDirty = True

                    MessageBox.Show($"OXTS set to: {newValue}", "Toggle OXTS",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
            Next

            MessageBox.Show("OxtsEnabled parameter not found in configuration.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ToggleOxts failed: {ex.Message}")
        End Try
    End Sub
    Private Sub LoadConfiguration(drvNumber As Integer)
        Try
            _currentDrvNumber = drvNumber
            _configData.Clear()
            DataGridViewParams.Rows.Clear()

            ' Build config file path
            Dim configPath As String = GetDRVConfigPath(drvNumber)

            ' ✅ Log which file we're loading
            HandleUserMessageLogging("GMRC", $"ConfigEditor: Loading {configPath}")

            If Not File.Exists(configPath) Then
                MessageBox.Show($"Configuration file not found: {configPath}" & vbCrLf & vbCrLf &
                               "Expected location: " & My.Application.Info.DirectoryPath,
                               "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                LabelStatus.Text = $"ERROR: File not found - {Path.GetFileName(configPath)}"
                Return
            End If

            ' Load XML
            Dim doc As New XmlDocument()
            doc.Load(configPath)

            ' Extract parameters
            For Each node As XmlNode In doc.DocumentElement.ChildNodes
                If node.NodeType = XmlNodeType.Element Then
                    _configData(node.Name) = node.InnerText

                    ' Add to grid
                    Dim rowIndex As Integer = DataGridViewParams.Rows.Add(
                        node.Name,
                        node.InnerText,
                        GetParameterDescription(node.Name)
                    )

                    ' Color-code validation
                    ValidateAndColorRow(rowIndex, node.Name, node.InnerText)
                End If
            Next

            Dim fileName As String = Path.GetFileName(configPath)
            LabelStatus.Text = $"Loaded: {fileName} - {_configData.Count} parameters"
            _isDirty = False

            HandleUserMessageLogging("GMRC", $"ConfigEditor: Successfully loaded {_configData.Count} parameters from {fileName}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"LoadConfiguration: {ex.Message}")
            MessageBox.Show($"Failed to load configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' ✅ FIXED: Returns correct path for config.xml or DRVRXX.xml files
    ''' -1 = config.xml (main configuration)
    ''' 0-5 = DRVR00.xml through DRVR05.xml
    ''' </summary>
    Private Function GetDRVConfigPath(drvNumber As Integer) As String
        Dim appDir As String = My.Application.Info.DirectoryPath

        If drvNumber = -1 Then
            ' Main config file
            Return Path.Combine(appDir, "config.xml")
        Else
            ' DRVR00.xml through DRVR05.xml
            Return Path.Combine(appDir, $"DRVR{drvNumber:D2}.xml")
        End If
    End Function

    Private Sub ValidateAndColorRow(rowIndex As Integer, paramName As String, value As String)
        Dim validationResult = ValidateParameter(paramName, value)

        If Not validationResult.IsValid Then
            DataGridViewParams.Rows(rowIndex).DefaultCellStyle.BackColor = Color.LightPink
            DataGridViewParams.Rows(rowIndex).Cells(2).Value = $"❌ {validationResult.Message}"
        Else
            DataGridViewParams.Rows(rowIndex).DefaultCellStyle.BackColor = Color.White
        End If
    End Sub

    Private Function ValidateParameter(paramName As String, value As String) As ValidationResult
        ' Basic validation rules
        Select Case paramName
            Case "INCADatabase", "INCAWorkspace"
                If Not String.IsNullOrWhiteSpace(value) AndAlso Not Directory.Exists(value) Then
                    Return New ValidationResult(False, "Directory does not exist")
                End If

            Case "RecordFileDurationMinutes"
                Dim minutes As Integer
                If Not Integer.TryParse(value, minutes) OrElse (minutes < 1 AndAlso minutes <> -1) Then
                    Return New ValidationResult(False, "Must be -1 (unlimited) or 1-60")
                End If

            Case "LidarIpAddress"
                If Not String.IsNullOrWhiteSpace(value) Then
                    Dim ip As System.Net.IPAddress = Nothing
                    If Not System.Net.IPAddress.TryParse(value, ip) Then
                        Return New ValidationResult(False, "Invalid IP address")
                    End If
                End If
                ' ════════════════════════════════════════════════════════════
                ' OXTS Validation
                ' ════════════════════════════════════════════════════════════
            Case "OxtsEnabled", "OxtsWaitForLockOnStart"
                If Not (value.Equals("True", StringComparison.OrdinalIgnoreCase) OrElse
                        value.Equals("False", StringComparison.OrdinalIgnoreCase)) Then
                    Return New ValidationResult(False, "Must be 'True' or 'False'")
                End If

            Case "OxtsNcomIpAddress"
                If Not String.IsNullOrWhiteSpace(value) Then
                    Dim ip As System.Net.IPAddress = Nothing
                    If Not System.Net.IPAddress.TryParse(value, ip) Then
                        Return New ValidationResult(False, "Invalid IP address format")
                    End If
                End If

            Case "OxtsNcomPort"
                Dim port As Integer
                If Not Integer.TryParse(value, port) OrElse port < 1 OrElse port > 65535 Then
                    Return New ValidationResult(False, "Must be between 1 and 65535")
                End If

            Case "OxtsGpsLockTimeout"
                Dim timeout As Integer
                If Not Integer.TryParse(value, timeout) OrElse timeout < 1000 OrElse timeout > 120000 Then
                    Return New ValidationResult(False, "Must be between 1000 and 120000 ms")
                End If
        End Select

        Return New ValidationResult(True, "Valid")
    End Function

    Private Function GetParameterDescription(paramName As String) As String
        ' Return friendly descriptions
        Select Case paramName
            Case "INCADatabase" : Return "INCA database directory path"
            Case "INCAWorkspace" : Return "INCA workspace name"
            Case "INCAExperiment" : Return "INCA experiment file name"
            Case "INCAVariableFile" : Return "Signal list file path (.csv or .xlsx)"
            Case "RecordFileDurationMinutes" : Return "Max recording duration (-1 = unlimited)"
            Case "LidarCaptureEnabled" : Return "Enable/disable LiDAR capture (True/False)"
            Case "LidarIpAddress" : Return "IP address of LiDAR device"
            Case "NetworkDriveMapping" : Return "Network drive mapping path"
                ' ════════════════════════════════════════════════════════════
                ' OXTS GPS/INS Parameters
                ' ════════════════════════════════════════════════════════════
            Case "OxtsEnabled"
                Return "Enable OXTS GPS/INS synchronization (True/False)"
            Case "OxtsNcomIpAddress"
                Return "OXTS NCOM listener IP address (e.g., 192.168.10.30)"
            Case "OxtsNcomPort"
                Return "OXTS NCOM UDP port (default: 3000)"
            Case "OxtsGpsLockTimeout"
                Return "GPS lock wait timeout in milliseconds (default: 30000)"
            Case "OxtsWaitForLockOnStart"
                Return "Wait for GPS lock before starting capture (True/False)"

            Case Else
                Return ""
        End Select
    End Function

    Private Sub ButtonSave_Click(sender As Object, e As EventArgs) Handles ButtonSave.Click
        Try
            If Not _isDirty Then
                MessageBox.Show("No changes to save.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Validate all parameters before saving
            For i As Integer = 0 To DataGridViewParams.Rows.Count - 1
                Dim paramName As String = DataGridViewParams.Rows(i).Cells(0).Value?.ToString()
                Dim value As String = DataGridViewParams.Rows(i).Cells(1).Value?.ToString()

                Dim result = ValidateParameter(paramName, value)
                If Not result.IsValid Then
                    MessageBox.Show($"Validation failed for '{paramName}': {result.Message}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Return
                End If
            Next

            ' Backup before saving
            BackupConfigFile(_currentDrvNumber)

            ' Save to XML
            SaveConfiguration(_currentDrvNumber)

            Dim fileName As String = If(_currentDrvNumber = -1, "config.xml", $"DRV{_currentDrvNumber:D2}.xml")
            MessageBox.Show($"Configuration saved to {fileName} successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            _isDirty = False
            Me.DialogResult = DialogResult.OK

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ButtonSave_Click: {ex.Message}")
            MessageBox.Show($"Save failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SaveConfiguration(drvNumber As Integer)
        Dim configPath As String = GetDRVConfigPath(drvNumber)

        ' Build new XML
        Dim doc As New XmlDocument()
        Dim root As XmlElement = doc.CreateElement("Configuration")
        doc.AppendChild(root)

        ' Add each parameter
        For i As Integer = 0 To DataGridViewParams.Rows.Count - 1
            Dim paramName As String = DataGridViewParams.Rows(i).Cells(0).Value?.ToString()
            Dim value As String = DataGridViewParams.Rows(i).Cells(1).Value?.ToString()

            If Not String.IsNullOrEmpty(paramName) Then
                Dim elem As XmlElement = doc.CreateElement(paramName)
                elem.InnerText = If(value, String.Empty) ' Handle null values
                root.AppendChild(elem)
            End If
        Next

        ' Save with formatting
        Using writer As New XmlTextWriter(configPath, System.Text.Encoding.UTF8)
            writer.Formatting = Formatting.Indented
            writer.Indentation = 2
            doc.Save(writer)
        End Using

        HandleUserMessageLogging("GMRC", $"Configuration saved to {configPath}")
    End Sub

    Private Sub BackupConfigFile(drvNumber As Integer)
        Try
            Dim configPath As String = GetDRVConfigPath(drvNumber)
            If File.Exists(configPath) Then
                Dim timestamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")
                Dim backupPath As String = configPath.Replace(".xml", $"_backup_{timestamp}.xml")
                File.Copy(configPath, backupPath)
                HandleUserMessageLogging("GMRC", $"Backup created: {backupPath}")
            End If
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"Backup failed: {ex.Message}")
        End Try
    End Sub

    Private Sub ButtonApplyToAll_Click(sender As Object, e As EventArgs) Handles ButtonApplyToAll.Click
        Try
            If MessageBox.Show("Apply current configuration to ALL DRV files (DRV00-DRV05 and config.xml)?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
                Return
            End If

            ' Backup and save config.xml
            BackupConfigFile(-1)
            SaveConfiguration(-1)

            ' Backup and save DRVR00-DRVR05
            For drvNum As Integer = 0 To 5
                BackupConfigFile(drvNum)
                SaveConfiguration(drvNum)
            Next

            MessageBox.Show("Configuration applied to all files successfully!" & vbCrLf &
                           "Files updated: config.xml, DRV00.xml through DRV05.xml",
                           "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            _isDirty = False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ApplyToAll failed: {ex.Message}")
            MessageBox.Show($"Apply to all failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ComboBoxDRV_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxDRV.SelectedIndexChanged
        If _isDirty Then
            If MessageBox.Show("You have unsaved changes. Discard them?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                ' Revert selection
                ComboBoxDRV.SelectedIndex = If(_currentDrvNumber = -1, 0, _currentDrvNumber + 1)
                Return
            End If
        End If

        ' ✅ UPDATED: Map dropdown index to file number
        ' Index 0 = config.xml (drvrNumber = -1)
        ' Index 1-6 = DRVR00-DRVR05 (drvrNumber = 0-5)
        Dim selectedIndex As Integer = ComboBoxDRV.SelectedIndex
        If selectedIndex = 0 Then
            LoadConfiguration(-1) ' config.xml
        Else
            LoadConfiguration(selectedIndex - 1) ' DRVR00-DRVR05
        End If
    End Sub

    Private Sub DataGridViewParams_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridViewParams.CellValueChanged
        If e.RowIndex >= 0 AndAlso e.ColumnIndex = 1 Then ' Value column changed
            _isDirty = True

            Dim paramName As String = DataGridViewParams.Rows(e.RowIndex).Cells(0).Value?.ToString()
            Dim newValue As String = DataGridViewParams.Rows(e.RowIndex).Cells(1).Value?.ToString()

            ValidateAndColorRow(e.RowIndex, paramName, newValue)
        End If
    End Sub

    ' Helper class for validation results
    Private Class ValidationResult
        Public Property IsValid As Boolean
        Public Property Message As String

        Public Sub New(isValid As Boolean, message As String)
            Me.IsValid = isValid
            Me.Message = message
        End Sub
    End Class
End Class