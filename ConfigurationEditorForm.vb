Imports System.IO
Imports System.Text
Imports System.Xml

Public Class ConfigurationEditorForm
    Private _currentDrvNumber As Integer = -1 ' ✅ Default to config.xml
    Private _configData As New Dictionary(Of String, String)
    Private _isDirty As Boolean = False
    Private _xmlDoc As XmlDocument

    Private Sub ConfigurationEditorForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            InitializeUI()
            LoadConfiguration(-1) ' ✅ Always start with config.xml

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ConfigEditor Load: {ex.Message}")
            MessageBox.Show($"Failed to load configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub InitializeUI()
        ' Set form properties
        Me.Text = "CLEVIR Configuration Editor - Master Configuration"
        Me.Size = New Size(1000, 700)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MaximizeBox = True
        Me.MinimizeBox = True

        ' Configure DataGridView
        DataGridViewParams.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        DataGridViewParams.AllowUserToAddRows = False
        DataGridViewParams.AllowUserToDeleteRows = False
        DataGridViewParams.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        DataGridViewParams.MultiSelect = False
        DataGridViewParams.ReadOnly = False
        DataGridViewParams.Columns(0).ReadOnly = True
        DataGridViewParams.Columns(1).ReadOnly = False
        DataGridViewParams.Columns(2).ReadOnly = True

        ' ✅ REMOVED: ComboBox for switching files (not needed anymore)
        ' User always edits config.xml, then generates driver files

        ' ═══════════════════════════════════════════════════════════════
        ' ✅ NEW: Add action buttons at the top
        ' ═══════════════════════════════════════════════════════════════
        Dim yPos As Integer = 10

        ' Toggle OXTS Button
        Dim btnToggleOxts As New Button With {
            .Text = "Toggle OXTS",
            .Location = New Point(10, yPos),
            .Size = New Size(120, 30),
            .BackColor = Color.LightGreen
        }
        AddHandler btnToggleOxts.Click, AddressOf ButtonToggleOxts_Click
        Me.Controls.Add(btnToggleOxts)

        ' Edit LiDAR Devices Button
        Dim btnEditLidar As New Button With {
            .Text = "Edit LiDAR Devices",
            .Location = New Point(140, yPos),
            .Size = New Size(140, 30),
            .BackColor = Color.LightBlue
        }
        AddHandler btnEditLidar.Click, AddressOf ButtonEditLidar_Click
        Me.Controls.Add(btnEditLidar)

        ' ✅ NEW: Generate Driver Files Button
        Dim btnGenerateDrivers As New Button With {
            .Text = "Generate Driver Files...",
            .Location = New Point(290, yPos),
            .Size = New Size(160, 30),
            .BackColor = Color.LightGoldenrodYellow,
            .Font = New Font(Me.Font, FontStyle.Bold)
        }
        AddHandler btnGenerateDrivers.Click, AddressOf ButtonGenerateDrivers_Click
        Me.Controls.Add(btnGenerateDrivers)

        ' ═══════════════════════════════════════════════════════════════
        ' Search controls at bottom
        ' ═══════════════════════════════════════════════════════════════
        Dim lblSearch As New Label With {
        .Text = "Search:",
        .Location = New Point(200, Me.ClientSize.Height - 44),
        .Size = New Size(50, 20),
        .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left,
        .TextAlign = ContentAlignment.MiddleLeft
    }
        Me.Controls.Add(lblSearch)

        Dim txtSearch As New TextBox With {
        .Name = "txtSearch",
        .Size = New Size(200, 35),
        .Location = New Point(250, Me.ClientSize.Height - 44),
        .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left,
        .Text = "Search parameters...",
        .ForeColor = SystemColors.GrayText
    }

        ' Placeholder behavior
        AddHandler txtSearch.GotFocus, Sub(sender, e)
                                           If txtSearch.Text = "Search parameters..." Then
                                               txtSearch.Text = ""
                                               txtSearch.ForeColor = SystemColors.WindowText
                                           End If
                                       End Sub

        AddHandler txtSearch.LostFocus, Sub(sender, e)
                                            If String.IsNullOrWhiteSpace(txtSearch.Text) Then
                                                txtSearch.Text = "Search parameters..."
                                                txtSearch.ForeColor = SystemColors.GrayText
                                            End If
                                        End Sub

        ' Search functionality
        AddHandler txtSearch.TextChanged, Sub()
                                              If txtSearch.ForeColor = SystemColors.WindowText Then
                                                  For Each row As DataGridViewRow In DataGridViewParams.Rows
                                                      If row.IsNewRow Then Continue For
                                                      Dim paramName As String = row.Cells(0).Value?.ToString().ToLower()
                                                      Dim description As String = row.Cells(2).Value?.ToString().ToLower()
                                                      Dim searchText As String = txtSearch.Text.ToLower()
                                                      row.Visible = CType((String.IsNullOrEmpty(searchText) OrElse
                                                                           paramName?.Contains(searchText) OrElse
                                                                           description?.Contains(searchText)), Boolean)
                                                  Next
                                              End If
                                          End Sub

        Me.Controls.Add(txtSearch)

        ' Reset to Defaults Button
        Dim btnReset As New Button With {
                .Text = "Reset to Defaults",
                .Location = New Point(460, yPos),
                .Size = New Size(130, 30),
                .BackColor = Color.LightCoral
                }
        AddHandler btnReset.Click, Sub()
                                       If MessageBox.Show("Reset all values to defaults?", "Confirm",
                               MessageBoxButtons.YesNo) = DialogResult.Yes Then
                                           ' Load default config.xml from resources or template
                                           LoadConfiguration(-1)
                                       End If
                                   End Sub
        Me.Controls.Add(btnReset)

        ' ✅ NEW: Status label showing current file
        Dim lblCurrentFile As New Label With {
            .Text = "Editing: config.xml",
            .Location = New Point(460, yPos + 5),
            .Size = New Size(400, 20),
            .Font = New Font(Me.Font, FontStyle.Bold),
            .ForeColor = Color.DarkBlue
        }
        Me.Controls.Add(lblCurrentFile)
    End Sub

    ' Export to JSON for easy sharing
    Private Sub ExportToJson()
        Dim json As New StringBuilder()
        json.AppendLine("{")
        For i As Integer = 0 To DataGridViewParams.Rows.Count - 1
            Dim key = DataGridViewParams.Rows(i).Cells(0).Value
            Dim val = DataGridViewParams.Rows(i).Cells(1).Value
            json.AppendLine($"  ""{key}"": ""{val}"",")
        Next
        json.AppendLine("}")
        File.WriteAllText("config_export.json", json.ToString())
    End Sub

    ''' <summary>
    ''' ✅ NEW: Generate driver-specific configuration files
    ''' </summary>
    Private Sub ButtonGenerateDrivers_Click(sender As Object, e As EventArgs)
        Try
            ' Check if config.xml has unsaved changes
            If _isDirty Then
                Dim result = MessageBox.Show(
                    "You have unsaved changes to config.xml." & vbCrLf & vbCrLf &
                    "Save changes before generating driver files?",
                    "Save Changes?",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question)

                Select Case result
                    Case DialogResult.Yes
                        ' Save config.xml first
                        BackupConfigFile(-1)
                        SaveConfiguration(-1)
                    Case DialogResult.Cancel
                        Return
                    Case DialogResult.No
                        ' Continue without saving
                End Select
            End If

            ' ═══════════════════════════════════════════════════════════════
            ' ✅ Show dialog to ask how many driver files to generate
            ' ═══════════════════════════════════════════════════════════════
            Using inputForm As New Form()
                inputForm.Text = "Generate Driver Configuration Files"
                inputForm.Size = New Size(400, 200)
                inputForm.StartPosition = FormStartPosition.CenterParent
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog
                inputForm.MaximizeBox = False
                inputForm.MinimizeBox = False

                Dim lblPrompt As New Label With {
                    .Text = "How many driver configuration files to generate?" & vbCrLf &
                            "(e.g., 5 will create DRVR00.xml through DRVR04.xml)",
                    .Location = New Point(20, 20),
                    .Size = New Size(350, 40),
                    .AutoSize = False
                }

                Dim numericUpDown As New NumericUpDown With {
                    .Location = New Point(20, 70),
                    .Size = New Size(100, 25),
                    .Minimum = 1,
                    .Maximum = 20,
                    .Value = 6 ' Default to 6 (DRVR00-DRVR05)
                }

                Dim btnOK As New Button With {
                    .Text = "Generate",
                    .Location = New Point(200, 120),
                    .DialogResult = DialogResult.OK
                }

                Dim btnCancel As New Button With {
                    .Text = "Cancel",
                    .Location = New Point(280, 120),
                    .DialogResult = DialogResult.Cancel
                }

                inputForm.Controls.AddRange({lblPrompt, numericUpDown, btnOK, btnCancel})
                inputForm.AcceptButton = btnOK
                inputForm.CancelButton = btnCancel

                If inputForm.ShowDialog() = DialogResult.OK Then
                    Dim numDrivers As Integer = CInt(numericUpDown.Value)
                    GenerateDriverConfigFiles(numDrivers)
                End If
            End Using

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"GenerateDrivers failed: {ex.Message}")
            MessageBox.Show($"Failed to generate driver files: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' ✅ Generate N driver configuration files from config.xml
    ''' </summary>
    Private Sub GenerateDriverConfigFiles(numDrivers As Integer)
        Try
            Dim appDir As String = My.Application.Info.DirectoryPath
            Dim configPath As String = Path.Combine(appDir, "config.xml")

            If Not File.Exists(configPath) Then
                MessageBox.Show("config.xml not found. Cannot generate driver files.",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Load master config.xml
            Dim masterDoc As New XmlDocument()
            masterDoc.Load(configPath)

            Dim generatedFiles As New List(Of String)

            ' Generate DRVR00.xml through DRVR(N-1).xml
            For i As Integer = 0 To numDrivers - 1
                Dim driverFileName As String = $"DRVR{i:D2}.xml"
                Dim driverPath As String = Path.Combine(appDir, driverFileName)

                ' Backup existing file if it exists
                If File.Exists(driverPath) Then
                    Dim timestamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")
                    Dim backupPath As String = driverPath.Replace(".xml", $"_backup_{timestamp}.xml")
                    File.Copy(driverPath, backupPath, True)
                    HandleUserMessageLogging("GMRC", $"Backed up existing {driverFileName} to {Path.GetFileName(backupPath)}")
                End If

                ' Clone the XML document
                Dim driverDoc As New XmlDocument()
                driverDoc.LoadXml(masterDoc.OuterXml)

                ' ✅ Optional: Customize driver-specific settings here
                ' Example: Set a driver-specific ID or comment
                Dim comment As XmlComment = driverDoc.CreateComment($" Driver {i:D2} Configuration - Generated from config.xml on {DateTime.Now:yyyy-MM-dd HH:mm:ss} ")
                driverDoc.DocumentElement.PrependChild(comment)

                ' Save with formatting
                Using writer As New XmlTextWriter(driverPath, System.Text.Encoding.UTF8)
                    writer.Formatting = Formatting.Indented
                    writer.Indentation = 1
                    writer.IndentChar = ControlChars.Tab
                    driverDoc.Save(writer)
                End Using

                generatedFiles.Add(driverFileName)
                HandleUserMessageLogging("GMRC", $"Generated {driverFileName}")
            Next

            ' Show success message
            MessageBox.Show(
                $"Successfully generated {numDrivers} driver configuration file(s):" & vbCrLf & vbCrLf &
                String.Join(vbCrLf, generatedFiles),
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"GenerateDriverConfigFiles: {ex.Message}")
            Throw
        End Try
    End Sub

    Private Sub ButtonToggleOxts_Click(sender As Object, e As EventArgs)
        Try
            For i As Integer = 0 To DataGridViewParams.Rows.Count - 1
                Dim paramName As String = DataGridViewParams.Rows(i).Cells(0).Value?.ToString()
                If paramName = "OxtsConfiguration.OxtsEnabled" Then
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

    ''' <summary>
    ''' ✅ FIXED: Always edit config.xml, not driver-specific files
    ''' </summary>
    Private Sub ButtonEditLidar_Click(sender As Object, e As EventArgs)
        Try
            ' ✅ Always edit the master config.xml document
            Using lidarEditor As New LidarDeviceEditorForm(_xmlDoc)
                ' ✅ Ensure dialog appears on top
                lidarEditor.StartPosition = FormStartPosition.CenterParent
                lidarEditor.TopMost = True
                lidarEditor.BringToFront()
                If lidarEditor.ShowDialog() = DialogResult.OK Then
                    _isDirty = True
                    ' Refresh display of config.xml
                    LoadConfiguration(-1)
                End If
            End Using

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"EditLidar failed: {ex.Message}")
            MessageBox.Show($"Failed to edit LiDAR devices: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadConfiguration(drvNumber As Integer)
        Try
            _currentDrvNumber = drvNumber
            _configData.Clear()
            DataGridViewParams.Rows.Clear()

            Dim configPath As String = GetDRVConfigPath(drvNumber)

            HandleUserMessageLogging("GMRC", $"ConfigEditor: Loading {configPath}")

            If Not File.Exists(configPath) Then
                MessageBox.Show($"Configuration file not found: {configPath}" & vbCrLf & vbCrLf &
                               "Expected location: " & My.Application.Info.DirectoryPath,
                               "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                LabelStatus.Text = $"ERROR: File not found - {Path.GetFileName(configPath)}"
                Return
            End If

            _xmlDoc = New XmlDocument()
            _xmlDoc.Load(configPath)

            ProcessXmlNode(_xmlDoc.DocumentElement, "")

            Dim fileName As String = Path.GetFileName(configPath)
            LabelStatus.Text = $"Loaded: {fileName} - {DataGridViewParams.Rows.Count} parameters"
            _isDirty = False

            HandleUserMessageLogging("GMRC", $"ConfigEditor: Successfully loaded {DataGridViewParams.Rows.Count} parameters from {fileName}")

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"LoadConfiguration: {ex.Message}")
            MessageBox.Show($"Failed to load configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' ✅ Recursively process XML nodes with hierarchy prefix
    ''' </summary>
    Private Sub ProcessXmlNode(node As XmlNode, parentPath As String)
        For Each childNode As XmlNode In node.ChildNodes
            ' ✅ Skip comments and other non-element nodes
            If childNode.NodeType <> XmlNodeType.Element Then Continue For

            Dim fullPath As String = If(String.IsNullOrEmpty(parentPath),
                                    childNode.Name,
                                    $"{parentPath}.{childNode.Name}")

            ' ✅ Check if this node has child ELEMENTS (not just text)
            Dim hasChildElements As Boolean = False
            For Each child As XmlNode In childNode.ChildNodes
                If child.NodeType = XmlNodeType.Element Then
                    hasChildElements = True
                    Exit For
                End If
            Next

            ' ════════════════════════════════════════════════════════════
            ' ✅ Handle nodes with child elements
            ' ════════════════════════════════════════════════════════════
            If hasChildElements Then
                ' Check if we should skip rendering this node entirely
                If ShouldSkipNode(childNode) Then
                    ' Skip both the header and children (e.g., LidarDevices)
                    Continue For
                Else
                    ' Show as expandable section header
                    Dim rowIndex As Integer = DataGridViewParams.Rows.Add(
                    fullPath,
                    "[Complex Element - See Sub-items]",
                    GetParameterDescription(fullPath)
                )
                    DataGridViewParams.Rows(rowIndex).DefaultCellStyle.BackColor = Color.LightGray
                    DataGridViewParams.Rows(rowIndex).Cells(1).ReadOnly = True

                    ' ✅ Recurse into children to show sub-items
                    ProcessXmlNode(childNode, fullPath)
                End If
            Else
                ' ════════════════════════════════════════════════════════════
                ' ✅ Handle simple text value nodes
                ' ════════════════════════════════════════════════════════════
                Dim value As String = childNode.InnerText

                Dim rowIndex As Integer = DataGridViewParams.Rows.Add(
                fullPath,
                value,
                GetParameterDescription(fullPath)
            )

                ' Validate and color-code
                ValidateAndColorRow(rowIndex, fullPath, value)
            End If
        Next
    End Sub
    Private Function ShouldSkipNode(node As XmlNode) As Boolean
        Select Case node.Name
            Case "LidarDevices"
                ' ✅ Skip LidarDevices entirely (handled by dedicated editor button)
                Return True

            Case "Compression", "OxtsConfiguration"
                ' ✅ DO NOT skip these - they should be expanded inline
                ' Return False to allow ProcessXmlNode to recurse and show sub-items
                Return False

            Case Else
                Return False
        End Select
    End Function

    Private Function GetDRVConfigPath(drvNumber As Integer) As String
        Dim appDir As String = My.Application.Info.DirectoryPath

        If drvNumber = -1 Then
            Return Path.Combine(appDir, "config.xml")
        Else
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
        ' ✅ Extract leaf name for validation (e.g., "OxtsConfiguration.OxtsEnabled" → "OxtsEnabled")
        Dim leafName As String = If(paramName.Contains("."), paramName.Split("."c).Last(), paramName)

        Select Case leafName
            Case "INCADatabase", "INCAWorkspace"
                If Not String.IsNullOrWhiteSpace(value) AndAlso Not Directory.Exists(value) Then
                    Return New ValidationResult(False, "Directory does not exist")
                End If

            Case "RecordFileDurationMinutes"
                Dim minutes As Integer
                If Not Integer.TryParse(value, minutes) OrElse (minutes < 1 AndAlso minutes <> -1) Then
                    Return New ValidationResult(False, "Must be -1 (unlimited) or 1-60")
                End If

            Case "IpAddress", "NcomIpAddress", "LidarIpAddress"
                If Not String.IsNullOrWhiteSpace(value) Then
                    Dim ip As System.Net.IPAddress = Nothing
                    If Not System.Net.IPAddress.TryParse(value, ip) Then
                        Return New ValidationResult(False, "Invalid IP address")
                    End If
                End If

            Case "OxtsEnabled", "WaitForLockOnStart", "LidarCaptureEnabled", "enabled"
                If Not (value.Equals("True", StringComparison.OrdinalIgnoreCase) OrElse
                        value.Equals("False", StringComparison.OrdinalIgnoreCase)) Then
                    Return New ValidationResult(False, "Must be 'True' or 'False'")
                End If

            Case "NcomPort", "DataPort", "ImuPort", "LidarDataPort", "LidarImuPort"
                Dim port As Integer
                If Not Integer.TryParse(value, port) OrElse port < 1 OrElse port > 65535 Then
                    Return New ValidationResult(False, "Must be between 1 and 65535")
                End If

            Case "GpsLockTimeout", "OxtsGpsLockTimeout"
                Dim timeout As Integer
                If Not Integer.TryParse(value, timeout) OrElse timeout < 1000 OrElse timeout > 120000 Then
                    Return New ValidationResult(False, "Must be between 1000 and 120000 ms")
                End If
        End Select

        Return New ValidationResult(True, "Valid")
    End Function

    Private Function GetParameterDescription(paramName As String) As String
        Select Case paramName
            ' INCA Configuration
            Case "INCADatabase" : Return "INCA database directory path"
            Case "INCAWorkspace" : Return "INCA workspace name"
            Case "INCAExperiment" : Return "INCA experiment file name"
            Case "INCAVariableFile" : Return "Signal list file path (.csv or .xlsx)"
            Case "SignalRegistrationMode" : Return "Signal registration mode (DISPLAYS/ALL)"

            ' Recording
            Case "RecordWAVTime" : Return "Audio recording duration (seconds)"
            Case "RecordFileDurationMinutes" : Return "Max recording duration (-1 = unlimited)"
            Case "MuteVoiceRecordingMessages" : Return "Suppress voice recording notifications (True/False)"
            Case "AudioToTextConversion" : Return "Enable speech-to-text conversion (True/False)"

            ' Alternate Recording
            Case "AlternateRecordEnabled" : Return "Enable CANalyzer/VehicleSpy recording (True/False)"
            Case "AlternateRecordConfig" : Return "Alternate recorder configuration name"

            ' Data Storage
            Case "BaseDataCollectionPath" : Return "Base directory for data collection"
            Case "NetworkDriveLetter" : Return "Network drive letter (e.g., Q:)"
            Case "NetworkDriveMapping" : Return "Network drive UNC path"

            ' OXTS Configuration
            Case "OxtsConfiguration" : Return "OXTS GPS/INS device settings"
            Case "OxtsConfiguration.OxtsEnabled" : Return "Enable OXTS GPS/INS synchronization (True/False)"
            Case "OxtsConfiguration.NcomIpAddress" : Return "OXTS NCOM listener IP address (e.g., 10.5.55.200)"
            Case "OxtsConfiguration.NcomPort" : Return "OXTS NCOM UDP port (default: 3000)"
            Case "OxtsConfiguration.GpsLockTimeout" : Return "GPS lock wait timeout in milliseconds (default: 30000)"
            Case "OxtsConfiguration.WaitForLockOnStart" : Return "Wait for GPS lock before starting capture (True/False)"

            ' LiDAR Configuration
            Case "LidarCaptureEnabled" : Return "Enable LiDAR data capture (True/False)"
            Case "LidarAdapterGuid" : Return "Network adapter GUID for LiDAR (legacy)"
            Case "LidarIpAddress" : Return "LiDAR device IP address (legacy)"
            Case "LidarDataPort" : Return "LiDAR data UDP port (legacy)"
            Case "LidarImuPort" : Return "LiDAR IMU UDP port (legacy)"

            ' Compression
            Case "Compression" : Return "File compression settings"
            Case "Compression.CompressMF4" : Return "Compress MF4 files (True/False)"
            Case "Compression.CompressPCAP" : Return "Compress PCAP files (True/False)"
            Case "Compression.CompressASC" : Return "Compress ASC files (True/False)"
            Case "Compression.CompressVSB" : Return "Compress VSB files (True/False)"
            Case "Compression.DeleteAfterCompression" : Return "Delete original after compression (True/False)"
            Case "Compression.CompressionLevel" : Return "7-Zip compression level (1=Fastest, 9=Best)"

            ' Hardware
            Case "MaxCameras" : Return "Maximum number of cameras supported"

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

            ' Validate all parameters
            For i As Integer = 0 To DataGridViewParams.Rows.Count - 1
                Dim paramName As String = DataGridViewParams.Rows(i).Cells(0).Value?.ToString()
                Dim value As String = DataGridViewParams.Rows(i).Cells(1).Value?.ToString()

                If value = "[Complex Element - See Sub-items]" Then Continue For

                Dim result = ValidateParameter(paramName, value)
                If Not result.IsValid Then
                    MessageBox.Show($"Validation failed for '{paramName}': {result.Message}",
                                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Return
                End If
            Next

            BackupConfigFile(-1) ' Always save config.xml
            SaveConfiguration(-1)

            MessageBox.Show($"Configuration saved to config.xml successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            _isDirty = False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ButtonSave_Click: {ex.Message}")
            MessageBox.Show($"Save failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SaveConfiguration(drvNumber As Integer)
        Dim configPath As String = GetDRVConfigPath(drvNumber)

        ' ✅ Update existing XML document with edited values
        For i As Integer = 0 To DataGridViewParams.Rows.Count - 1
            Dim paramName As String = DataGridViewParams.Rows(i).Cells(0).Value?.ToString()
            Dim value As String = DataGridViewParams.Rows(i).Cells(1).Value?.ToString()

            ' Skip complex elements
            If value = "[Complex Element - See Sub-items]" Then Continue For

            ' Navigate to node using dot notation
            Dim node As XmlNode = FindNodeByPath(_xmlDoc.DocumentElement, paramName)
            If node IsNot Nothing Then
                node.InnerText = If(value, String.Empty)
            End If
        Next

        ' Save with formatting
        Using writer As New XmlTextWriter(configPath, System.Text.Encoding.UTF8)
            writer.Formatting = Formatting.Indented
            writer.Indentation = 1
            writer.IndentChar = ControlChars.Tab
            _xmlDoc.Save(writer)
        End Using

        HandleUserMessageLogging("GMRC", $"Configuration saved to {configPath}")
    End Sub

    ''' <summary>
    ''' ✅ Find XML node by hierarchical path (e.g., "OxtsConfiguration.OxtsEnabled")
    ''' </summary>
    Private Function FindNodeByPath(root As XmlNode, path As String) As XmlNode
        Dim parts() As String = path.Split("."c)
        Dim currentNode As XmlNode = root

        For Each part As String In parts
            Dim found As Boolean = False
            For Each child As XmlNode In currentNode.ChildNodes
                If child.NodeType = XmlNodeType.Element AndAlso child.Name = part Then
                    currentNode = child
                    found = True
                    Exit For
                End If
            Next

            If Not found Then Return Nothing
        Next

        Return currentNode
    End Function

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

    Private Sub ComboBoxDRV_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxDRV.SelectedIndexChanged
        If _isDirty Then
            If MessageBox.Show("You have unsaved changes. Discard them?", "Confirm",
                               MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                ComboBoxDRV.SelectedIndex = If(_currentDrvNumber = -1, 0, _currentDrvNumber + 1)
                Return
            End If
        End If

        Dim selectedIndex As Integer = ComboBoxDRV.SelectedIndex
        If selectedIndex = 0 Then
            LoadConfiguration(-1) ' config.xml
        Else
            LoadConfiguration(selectedIndex - 1) ' DRVR00-DRVR05
        End If
    End Sub

    Private Sub DataGridViewParams_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridViewParams.CellValueChanged
        If e.RowIndex >= 0 AndAlso e.ColumnIndex = 1 Then
            _isDirty = True

            Dim paramName As String = DataGridViewParams.Rows(e.RowIndex).Cells(0).Value?.ToString()
            Dim newValue As String = DataGridViewParams.Rows(e.RowIndex).Cells(1).Value?.ToString()

            ValidateAndColorRow(e.RowIndex, paramName, newValue)
        End If
    End Sub

    Private Class ValidationResult
        Public Property IsValid As Boolean
        Public Property Message As String

        Public Sub New(isValid As Boolean, message As String)
            Me.IsValid = isValid
            Me.Message = message
        End Sub
    End Class
End Class