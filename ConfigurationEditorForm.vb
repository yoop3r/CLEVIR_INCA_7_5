Imports System.IO
Imports System.Text
Imports System.Xml

Public Class ConfigurationEditorForm
    Private _configData As New Dictionary(Of String, String)
    Private _isDirty As Boolean = False
    Private _xmlDoc As XmlDocument

    Private Sub ConfigurationEditorForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            InitializeUI()
            LoadConfiguration()

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

        ' Edit Vehicles Button
        Dim btnEditVehicles As New Button With {
            .Text = "Edit Vehicles",
            .Location = New Point(290, yPos),
            .Size = New Size(120, 30),
            .BackColor = Color.LightSalmon
        }
        AddHandler btnEditVehicles.Click, AddressOf ButtonEditVehicles_Click
        Me.Controls.Add(btnEditVehicles)

        ' Edit Cameras Button
        Dim btnEditCameras As New Button With {
            .Text = "Edit Cameras",
            .Location = New Point(430, yPos),
            .Size = New Size(120, 30),
            .BackColor = Color.LightGoldenrodYellow
        }
        AddHandler btnEditCameras.Click, AddressOf ButtonEditCameras_Click
        Me.Controls.Add(btnEditCameras)

        ' Signal Registration Mode Button
        Dim btnSignalReg As New Button With {
            .Name = "btnSignalReg",
            .Text = "Signal Reg Mode",
            .Location = New Point(560, yPos),
            .Size = New Size(140, 30),
            .BackColor = Color.LightYellow
        }
        AddHandler btnSignalReg.Click, AddressOf ButtonSignalRegMode_Click
        Me.Controls.Add(btnSignalReg)

        ' ✅ NEW: Generate Manifest Button
        ' Produces sidecar manifest JSON for each configured LiDAR by querying
        ' the device over the Hesai HTTP API, without needing a capture session.
        Dim btnGenerateManifest As New Button With {
            .Name = "btnGenerateManifest",
            .Text = "📋 Generate Manifest",
            .Location = New Point(710, yPos),
            .Size = New Size(160, 30),
            .BackColor = Color.LightCyan
        }
        AddHandler btnGenerateManifest.Click, AddressOf ButtonGenerateManifest_Click
        Me.Controls.Add(btnGenerateManifest)

        ' Search controls at bottom
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
                .Location = New Point(710, yPos),
                .Size = New Size(130, 30),
                .BackColor = Color.LightCoral
                }
        AddHandler btnReset.Click, Sub()
                                       If MessageBox.Show("Reset all values to defaults?", "Confirm",
                               MessageBoxButtons.YesNo) = DialogResult.Yes Then
                                           ' Reload config.xml
                                           LoadConfiguration()
                                       End If
                                   End Sub
        Me.Controls.Add(btnReset)

        ' ✅ NEW: Status label showing current file
        Dim lblCurrentFile As New Label With {
            .Text = "Editing: config.xml",
            .Location = New Point(590, yPos + 5),
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

    Private Sub ButtonSignalRegMode_Click(sender As Object, e As EventArgs)
        Try
            ' Build a lightweight selection dialog at runtime
            Dim dlg As New Form() With {
                .Text = "Signal Registration Mode",
                .Size = New Size(370, 240),
                .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedDialog,
                .MaximizeBox = False,
                .MinimizeBox = False,
                .TopMost = True
            }

            Dim lbl As New Label() With {
                .Text = "Select Signal Registration Mode:",
                .Location = New Point(12, 12),
                .Size = New Size(330, 20),
                .Font = New Font(dlg.Font, FontStyle.Bold)
            }

            Dim showNewFull As Boolean = Not (PATAC OrElse CurrentVehicleUsage = "VALIDATION")

            Dim rb1 As New RadioButton() With {.Text = "FULL", .Location = New Point(20, 38), .Size = New Size(310, 24), .Checked = (SignalRegistrationMode = "FULL")}
            Dim rb2 As New RadioButton() With {.Text = "DISPLAYS", .Location = New Point(20, 66), .Size = New Size(310, 24), .Checked = (SignalRegistrationMode = "DISPLAYS")}
            Dim rb3 As New RadioButton() With {.Text = "GO/NOGO", .Location = New Point(20, 94), .Size = New Size(310, 24), .Checked = (SignalRegistrationMode = "GO/NOGO")}
            Dim rb4 As New RadioButton() With {
                .Text = "Create New Experiment (Admin only)",
                .Location = New Point(20, 122),
                .Size = New Size(310, 24),
                .Checked = (SignalRegistrationMode = "NEW FULL"),
                .Enabled = ClevirAdministrator,
                .Visible = showNewFull
            }

            Dim btnOK As New Button() With {.Text = "OK", .DialogResult = DialogResult.OK, .Location = New Point(95, 165), .Size = New Size(80, 28)}
            Dim btnCancelDlg As New Button() With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Location = New Point(185, 165), .Size = New Size(80, 28)}

            dlg.Controls.AddRange(New Control() {lbl, rb1, rb2, rb3, rb4, btnOK, btnCancelDlg})
            dlg.AcceptButton = btnOK
            dlg.CancelButton = btnCancelDlg

            If dlg.ShowDialog(Me) <> DialogResult.OK Then Return

            Dim selectedMode As String = SignalRegistrationMode

            If rb1.Checked Then
                ' Warn non-admins about FULL registration performance impact
                If Not ClevirAdministrator Then
                    Dim warn = "FULL signal registration will take a LONG time. " &
                               "This is typically not necessary when running CLEVIR in a vehicle. " &
                               "Are you sure?"
                    If MsgBox(warn, CType(vbYesNo + vbQuestion, MsgBoxStyle), "FULL Signal Registration") <> vbYes Then
                        selectedMode = If(SaveSignalRegistrationMode = "FULL", "DISPLAYS", SaveSignalRegistrationMode)
                    Else
                        selectedMode = "FULL"
                    End If
                Else
                    selectedMode = "FULL"
                End If
            ElseIf rb2.Checked Then
                selectedMode = "DISPLAYS"
            ElseIf rb3.Checked Then
                selectedMode = "GO/NOGO"
            ElseIf rb4.Checked Then
                selectedMode = "NEW FULL"
            End If

            SignalRegistrationMode = selectedMode
            SaveSignalRegistrationMode = selectedMode

            ' Persist to in-memory XML document so ButtonSave will write it
            If _xmlDoc IsNot Nothing Then
                Dim node As XmlNode = FindNodeByPath(_xmlDoc.DocumentElement, "SignalRegistrationMode")
                If node IsNot Nothing Then
                    node.InnerText = selectedMode
                    ' Keep DataGridView in sync
                    For Each row As DataGridViewRow In DataGridViewParams.Rows
                        If row.Cells(0).Value?.ToString() = "SignalRegistrationMode" Then
                            row.Cells(1).Value = selectedMode
                            Exit For
                        End If
                    Next
                    _isDirty = True
                End If
            End If

            HandleUserMessageLogging("GMRC", $"ConfigEditor: Signal Registration Mode set to '{selectedMode}'")
            LabelStatus.Text = $"Signal Registration Mode: {selectedMode}"

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ButtonSignalRegMode_Click: {ex.Message}")
            MessageBox.Show($"Failed to set Signal Registration Mode: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                    LoadConfiguration()
                End If
            End Using

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"EditLidar failed: {ex.Message}")
            MessageBox.Show($"Failed to edit LiDAR devices: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ButtonEditVehicles_Click(sender As Object, e As EventArgs)
        Try
            If _xmlDoc Is Nothing Then
                MessageBox.Show("Please load a configuration file first.", "No Configuration",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            Using vehicleEditor As New VehicleEditorForm(_xmlDoc)
                vehicleEditor.StartPosition = FormStartPosition.CenterParent
                vehicleEditor.TopMost = True
                vehicleEditor.BringToFront()
                If vehicleEditor.ShowDialog() = DialogResult.OK Then
                    _isDirty = True
                    ' Refresh the parameter grid to reflect any structural changes
                    LoadConfiguration()
                End If
            End Using
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"EditVehicles failed: {ex.Message}")
            MessageBox.Show($"Failed to edit vehicles: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ButtonEditCameras_Click(sender As Object, e As EventArgs)
        Try
            If _xmlDoc Is Nothing Then
                MessageBox.Show("Please load a configuration file first.", "No Configuration",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            Using cameraEditor As New CameraEditorForm(_xmlDoc)
                cameraEditor.StartPosition = FormStartPosition.CenterParent
                cameraEditor.TopMost = True
                cameraEditor.BringToFront()
                If cameraEditor.ShowDialog() = DialogResult.OK Then
                    _isDirty = True
                    LoadConfiguration()
                End If
            End Using
        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"EditCameras failed: {ex.Message}")
            MessageBox.Show($"Failed to edit cameras: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Generates sidecar manifest JSON for each configured LiDAR device by
    ''' querying it live over the Hesai HTTP JSON API (pandar.cgi).
    '''
    ''' This is the on-demand counterpart to the manifest written automatically at
    ''' capture stop. It lets an operator verify device reachability and capture
    ''' the exact identity/config/calibration metadata that downstream ROS 2
    ''' conversion needs, without having to run a recording first.
    '''
    ''' Devices are read straight from the loaded config.xml rather than from live
    ''' LidarDevice instances, so this works before capture has ever been started.
    ''' </summary>
    Private Sub ButtonGenerateManifest_Click(sender As Object, e As EventArgs)
        Try
            HandleUserMessageLogging("GMRC", "Generate Manifest button pressed...")

            If _xmlDoc Is Nothing Then
                MessageBox.Show("Please load a configuration file first.", "No Configuration",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If _isDirty Then
                If MessageBox.Show(
                    "You have unsaved configuration changes. The manifest will be generated using the SAVED configuration." &
                    vbCrLf & vbCrLf & "Continue anyway?",
                    "Unsaved Changes", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                    Return
                End If
            End If

            ' ── Collect configured LiDAR devices ──────────────────────────
            Dim lidarNodes As XmlNodeList = _xmlDoc.SelectNodes("//LidarDevices/Lidar")
            If lidarNodes Is Nothing OrElse lidarNodes.Count = 0 Then
                MessageBox.Show("No LiDAR devices are configured in config.xml.", "No Devices",
                                MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Shared <HesaiConfig> sibling supplies the HTTP port and the
            ' calibration file fallbacks used when a live query is unavailable.
            Dim sharedHesaiNode As XmlNode = _xmlDoc.SelectSingleNode("//LidarDevices/HesaiConfig")

            ' ── Choose output folder ──────────────────────────────────────
            Dim outputDirectory As String
            Using folderDialog As New FolderBrowserDialog With {
                .Description = "Select a folder for the generated manifest files",
                .UseDescriptionForTitle = True
            }
                If folderDialog.ShowDialog(Me) <> DialogResult.OK Then Return
                outputDirectory = folderDialog.SelectedPath
            End Using

            ' ── Generate one manifest per device ──────────────────────────
            Dim results As New StringBuilder()
            Dim successCount As Integer = 0
            Dim skippedCount As Integer = 0

            Dim previousCursor As Cursor = Me.Cursor
            Me.Cursor = Cursors.WaitCursor
            Try
                For Each lidarNode As XmlNode In lidarNodes
                    ' Device identity and enablement are attributes on <Lidar>,
                    ' matching how GM_ResidentClient constructs devices.
                    Dim deviceId As String = lidarNode.Attributes("id")?.Value
                    Dim enabledAttr As String = lidarNode.Attributes("enabled")?.Value
                    Dim enabled As Boolean = True
                    If Not String.IsNullOrEmpty(enabledAttr) Then Boolean.TryParse(enabledAttr, enabled)

                    Dim ipAddress As String = lidarNode.SelectSingleNode("IpAddress")?.InnerText

                    If Not enabled Then
                        skippedCount += 1
                        results.AppendLine($"⏭️ {If(deviceId, "(unnamed)")}: disabled in config - skipped")
                        Continue For
                    End If

                    If String.IsNullOrWhiteSpace(ipAddress) Then
                        skippedCount += 1
                        results.AppendLine($"⚠️ {If(deviceId, "(unnamed)")}: no IP address configured - skipped")
                        Continue For
                    End If

                    If String.IsNullOrWhiteSpace(deviceId) Then deviceId = ipAddress

                    ' Per-device <HesaiConfig> overrides the shared sibling node.
                    Dim hesaiNode As XmlNode = If(lidarNode.SelectSingleNode("HesaiConfig"), sharedHesaiNode)

                    Dim httpPort As Integer = HesaiHttpApi.DefaultHttpPort
                    Dim parsedPort As Integer
                    If hesaiNode IsNot Nothing AndAlso
                       Integer.TryParse(hesaiNode.SelectSingleNode("HttpPort")?.InnerText, parsedPort) AndAlso
                       parsedPort > 0 Then
                        httpPort = parsedPort
                    End If

                    Dim manifestPath As String = GenerateManifestForDevice(
                        deviceId, ipAddress, httpPort, hesaiNode, lidarNode, outputDirectory)

                    If String.IsNullOrEmpty(manifestPath) Then
                        results.AppendLine($"❌ {deviceId} ({ipAddress}): unreachable - no manifest written")
                    Else
                        successCount += 1
                        results.AppendLine($"✅ {deviceId} ({ipAddress}): {Path.GetFileName(manifestPath)}")
                    End If
                Next
            Finally
                Me.Cursor = previousCursor
            End Try

            ' ── Report ────────────────────────────────────────────────────
            Dim attemptedCount As Integer = lidarNodes.Count - skippedCount
            Dim summary As String =
                $"Generated {successCount} of {attemptedCount} manifest(s) in:{vbCrLf}{outputDirectory}{vbCrLf}{vbCrLf}{results}"

            HandleUserMessageLogging("GMRC", $"Manifest generation complete: {successCount}/{attemptedCount} succeeded")

            MessageBox.Show(summary, "Generate Manifest", MessageBoxButtons.OK,
                            If(successCount = attemptedCount AndAlso successCount > 0,
                               MessageBoxIcon.Information, MessageBoxIcon.Warning))

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"GenerateManifest failed: {ex.Message}")
            MessageBox.Show($"Failed to generate manifest: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Builds and writes one on-demand manifest for a single configured device.
    ''' Live identity/config/status/angle-correction come from the HTTP API;
    ''' angle correction falls back to the configured file if the live query
    ''' fails, and firetimes are always file-sourced (no HTTP object exposes them).
    ''' </summary>
    ''' <returns>Path of the written manifest, or Nothing if the device was unreachable</returns>
    Private Function GenerateManifestForDevice(deviceId As String,
                                               ipAddress As String,
                                               httpPort As Integer,
                                               hesaiNode As XmlNode,
                                               lidarNode As XmlNode,
                                               outputDirectory As String) As String
        Try
            Dim metadata = HesaiHttpApi.GetMetadata(ipAddress, httpPort)
            If metadata Is Nothing Then Return Nothing

            Dim manifest As New PcapEventBridge.CaptureManifest With {
                .DeviceId = deviceId,
                .DeviceIpAddress = ipAddress,
                .GenerationTrigger = "OnDemand",
                .Device = metadata.Device,
                .Configuration = metadata.Configuration,
                .StatusAtCaptureStart = metadata.Status
            }

            If Not String.IsNullOrEmpty(metadata.AngleCorrectionCsv) Then
                manifest.AngleCorrection = PcapEventBridge.CalibrationBlob.FromHttp(metadata.AngleCorrectionCsv)
            End If

            ' ── Calibration file fallbacks ────────────────────────────────
            If hesaiNode IsNot Nothing Then
                Dim correctionPath As String = hesaiNode.SelectSingleNode("CorrectionFilePath")?.InnerText
                If manifest.AngleCorrection Is Nothing AndAlso
                   Not String.IsNullOrWhiteSpace(correctionPath) AndAlso File.Exists(correctionPath) Then
                    manifest.AngleCorrection = PcapEventBridge.CalibrationBlob.FromFile(
                        correctionPath, File.ReadAllBytes(correctionPath))
                End If

                Dim firetimesPath As String = hesaiNode.SelectSingleNode("FiretimesPath")?.InnerText
                If Not String.IsNullOrWhiteSpace(firetimesPath) AndAlso File.Exists(firetimesPath) Then
                    manifest.Firetimes = PcapEventBridge.CalibrationBlob.FromFile(
                        firetimesPath, File.ReadAllBytes(firetimesPath))
                End If
            End If

            ' ── Extrinsic / mount alignment, if configured for this device ──
            Dim extrinsicNode As XmlNode = lidarNode.SelectSingleNode("Extrinsic")
            If extrinsicNode IsNot Nothing Then
                manifest.Extrinsic = ParseExtrinsicNode(extrinsicNode)
            End If

            Dim safeDeviceId As String = deviceId
            For Each invalidChar In Path.GetInvalidFileNameChars()
                safeDeviceId = safeDeviceId.Replace(invalidChar, "_"c)
            Next

            Directory.CreateDirectory(outputDirectory)
            Dim manifestPath As String = Path.Combine(
                outputDirectory, $"{safeDeviceId}_{DateTime.Now:yyyyMMdd_HHmmss}.manifest.json")

            manifest.WriteToFile(manifestPath)
            Return manifestPath

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"GenerateManifestForDevice({deviceId}): {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Parses an &lt;Extrinsic&gt; config node into an ExtrinsicRecord, mirroring the
    ''' parsing performed by GM_ResidentClient when devices are constructed.
    ''' </summary>
    Private Function ParseExtrinsicNode(extrinsicNode As XmlNode) As PcapEventBridge.ExtrinsicRecord
        Dim record As New PcapEventBridge.ExtrinsicRecord With {
            .CalibrationId = extrinsicNode.SelectSingleNode("CalibrationId")?.InnerText,
            .Method = extrinsicNode.SelectSingleNode("Method")?.InnerText
        }

        ' IsCalibrated is an attribute on <Extrinsic>, not a child element.
        Dim isCalibrated As Boolean
        If Boolean.TryParse(extrinsicNode.Attributes("IsCalibrated")?.Value, isCalibrated) Then
            record.IsCalibrated = isCalibrated
        End If

        Dim datePerformed As DateTime
        If DateTime.TryParse(extrinsicNode.SelectSingleNode("DatePerformed")?.InnerText, datePerformed) Then
            record.DatePerformed = datePerformed
        End If

        Dim residual As Double
        If Double.TryParse(extrinsicNode.SelectSingleNode("ResidualError")?.InnerText, residual) Then
            record.ResidualError = residual
        End If

        Dim translation = ParseDoubleVector(extrinsicNode.SelectSingleNode("Translation")?.InnerText, 3)
        If translation IsNot Nothing Then record.TranslationMeters = translation

        Dim rotation = ParseDoubleVector(extrinsicNode.SelectSingleNode("RotationQuaternion")?.InnerText, 4)
        If rotation IsNot Nothing Then record.RotationQuaternion = rotation

        Return record
    End Function

    ''' <summary>
    ''' Parses a comma/space separated numeric vector of the expected length.
    ''' Returns Nothing if the text is absent or does not have exactly that many
    ''' parseable components, so the caller keeps its default.
    ''' </summary>
    Private Function ParseDoubleVector(text As String, expectedLength As Integer) As Double()
        If String.IsNullOrWhiteSpace(text) Then Return Nothing

        Dim parts = text.Split({","c, " "c, ";"c}, StringSplitOptions.RemoveEmptyEntries)
        If parts.Length <> expectedLength Then Return Nothing

        Dim values(expectedLength - 1) As Double
        For i As Integer = 0 To expectedLength - 1
            If Not Double.TryParse(parts(i).Trim(), Globalization.NumberStyles.Float,
                                   Globalization.CultureInfo.InvariantCulture, values(i)) Then
                Return Nothing
            End If
        Next

        Return values
    End Function

    Private Sub LoadConfiguration()
        Try
            _configData.Clear()
            DataGridViewParams.Rows.Clear()

            Dim configPath As String = Path.Combine(My.Application.Info.DirectoryPath, "config.xml")

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

            Case "Vehicles"
                ' ✅ Skip Vehicles entirely (handled by dedicated VehicleEditorForm button)
                Return True

            Case "CameraConfiguration"
                ' ✅ Skip CameraConfiguration entirely (handled by dedicated CameraEditorForm button)
                Return True

            Case "Compression", "OxtsConfiguration"
                ' ✅ DO NOT skip these - they should be expanded inline
                ' Return False to allow ProcessXmlNode to recurse and show sub-items
                Return False

            Case Else
                Return False
        End Select
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

            Case "OxtsEnabled", "WaitForLockOnStart", "LidarCaptureEnabled", "enabled",
                 "MuteVoiceRecordingMessages", "AudioToTextConversion", "AlternateRecordEnabled",
                 "EnableAltRecReStartAfterRecordStop", "OxtsCaptureEnabled", "EnableTimeSync",
                 "Enabled", "PtpAssumeLocked", "SaveCalSnapshotEnabled", "SkipSubscriptionOnCacheHit",
                 "CompressMF4", "CompressPCAP", "CompressASC", "CompressVSB", "DeleteAfterCompression",
                 "ZipMF4Files"
                If Not (value.Equals("True", StringComparison.OrdinalIgnoreCase) OrElse
                        value.Equals("False", StringComparison.OrdinalIgnoreCase)) Then
                    Return New ValidationResult(False, "Must be 'True' or 'False'")
                End If

            Case "APICommErrorMsgDelayTime"
                Dim delaySeconds As Integer
                If Not Integer.TryParse(value, delaySeconds) OrElse delaySeconds < 0 Then
                    Return New ValidationResult(False, "Must be a non-negative integer (seconds)")
                End If

            Case "MaxRetries", "RetryDelaySeconds", "FileLockTimeoutSeconds", "CompressionLevel"
                Dim numericValue As Integer
                If Not Integer.TryParse(value, numericValue) OrElse numericValue < 0 Then
                    Return New ValidationResult(False, "Must be a non-negative integer")
                End If

            Case "Provider"
                If Not (value.Equals("OXTS", StringComparison.OrdinalIgnoreCase) OrElse
                        value.Equals("TimeMachine", StringComparison.OrdinalIgnoreCase)) Then
                    Return New ValidationResult(False, "Must be 'OXTS' or 'TimeMachine'")
                End If

            Case "NcomPort", "DataPort", "ImuPort", "LidarDataPort", "LidarImuPort", "Port", "PtcPort", "HttpPort"
                Dim port As Integer
                If Not Integer.TryParse(value, port) OrElse port < 1 OrElse port > 65535 Then
                    Return New ValidationResult(False, "Must be between 1 and 65535")
                End If

            Case "PollMs"
                Dim pollMs As Integer
                If Not Integer.TryParse(value, pollMs) OrElse pollMs < 1 Then
                    Return New ValidationResult(False, "Must be a positive integer (milliseconds)")
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
            Case "AudioToTextConfiguration.PythonPath" : Return "Path to Python executable used for audio-to-text conversion"
            Case "AudioToTextConfiguration.ScriptName" : Return "Python script name to execute for audio-to-text conversion"
            Case "AudioToTextConfiguration.WorkingDirectory" : Return "Working directory for the audio-to-text Python script"
            Case "AudioToTextConfiguration.IntakeDir" : Return "Input directory containing audio files to convert"
            Case "AudioToTextConfiguration.ConfigPath" : Return "Path to the Driver_Log_Tools configuration spreadsheet"
            Case "AudioToTextConfiguration.ConfigSheetName" : Return "Sheet name within the configuration spreadsheet"
            Case "AudioToTextConfiguration.RunValue" : Return "Comma-separated run stages passed to the Python script (--RUN)"

            ' Alternate Recording
            Case "AlternateRecordEnabled" : Return "Enable CANalyzer/VehicleSpy recording (True/False)"
            Case "AlternateRecordConfig" : Return "Alternate recorder configuration name"

            ' Data Storage
            Case "BaseDataCollectionPath" : Return "Base directory for data collection"
            Case "NetworkDriveLetter" : Return "Network drive letter (e.g., Q:)"
            Case "NetworkDriveMapping" : Return "Network drive UNC path"
            Case "NetworkAdapterDescription" : Return "Preferred network adapter description filter (optional)"

            ' OXTS Configuration
            Case "OxtsConfiguration" : Return "OXTS GPS/INS device settings"
            Case "OxtsConfiguration.OxtsEnabled" : Return "Enable OXTS GPS/INS synchronization (True/False)"
            Case "OxtsConfiguration.NetworkAdapterGuid" : Return "Network adapter GUID for OXTS NCOM listener (Vlan40)"
            Case "OxtsConfiguration.NcomIpAddress" : Return "OXTS NCOM listener IP address (e.g., 10.5.55.200)"
            Case "OxtsConfiguration.NcomPort" : Return "OXTS NCOM UDP port (default: 3000)"
            Case "OxtsConfiguration.GpsLockTimeout" : Return "GPS lock wait timeout in milliseconds (default: 30000)"
            Case "OxtsConfiguration.WaitForLockOnStart" : Return "Wait for GPS lock before starting capture (True/False)"

            ' OXTS NCOM PCAP Capture Configuration
            Case "OxtsCaptureEnabled" : Return "Enable raw NCOM UDP packet capture to PCAP (True/False)"
            Case "OxtsCapture" : Return "OXTS NCOM PCAP capture settings"
            Case "OxtsCapture.NetworkAdapterGuid" : Return "Network adapter GUID for NCOM PCAP capture (Vlan40)"
            Case "OxtsCapture.IpAddress" : Return "OXTS device IP address for NCOM PCAP capture"
            Case "OxtsCapture.NcomPort" : Return "NCOM UDP port for PCAP capture (same as NCOM interface)"

            ' Time Sync Provider Configuration
            Case "TimeSyncConfiguration" : Return "Global time synchronization source selection"
            Case "TimeSyncConfiguration.EnableTimeSync" : Return "Enable shared time synchronization provider (True/False)"
            Case "TimeSyncConfiguration.Provider" : Return "Time sync source: OXTS or TimeMachine"
            Case "TimeMachineConfiguration" : Return "TimeMachine Locator API settings"
            Case "TimeMachineConfiguration.Enabled" : Return "Enable TimeMachine provider configuration (True/False)"
            Case "TimeMachineConfiguration.DeviceIp" : Return "TimeMachine device IP or broadcast"
            Case "TimeMachineConfiguration.Port" : Return "TimeMachine Locator UDP port (default: 7372)"
            Case "TimeMachineConfiguration.PollMs" : Return "Locator query poll interval in milliseconds"
            Case "TimeMachineConfiguration.PtpAssumeLocked" : Return "Treat TimeMachine as PTP-locked when responses are fresh (True/False)"

            ' LiDAR Configuration
            Case "LidarCaptureEnabled" : Return "Enable LiDAR data capture (True/False)"
            Case "LidarAdapterGuid" : Return "Network adapter GUID for LiDAR (legacy)"
            Case "LidarIpAddress" : Return "LiDAR device IP address (legacy)"
            Case "LidarDataPort" : Return "LiDAR data UDP port (legacy)"
            Case "LidarImuPort" : Return "LiDAR IMU UDP port (legacy)"

            ' Camera Configuration
            Case "CameraConfiguration" : Return "Camera device settings (edit via Edit Cameras button)"
            Case "CameraConfiguration.MaxCameras" : Return "Auto-calculated count of enabled cameras"
            Case "CameraConfiguration.InitialWaitTime" : Return "Seconds to wait before first camera ping on startup"
            Case "CameraConfiguration.PingTimeout" : Return "Camera reachability ping timeout in milliseconds"

            ' Compression
            Case "Compression" : Return "File compression settings"
            Case "Compression.CompressMF4" : Return "Compress MF4 files (True/False)"
            Case "Compression.CompressPCAP" : Return "Compress PCAP files (True/False)"
            Case "Compression.CompressASC" : Return "Compress ASC files (True/False)"
            Case "Compression.CompressVSB" : Return "Compress VSB files (True/False)"
            Case "Compression.DeleteAfterCompression" : Return "Delete original after compression (True/False)"
            Case "Compression.CompressionLevel" : Return "7-Zip compression level (1=Fastest, 9=Best)"
            Case "Compression.MaxRetries" : Return "Max retry attempts if a file is locked during compression"
            Case "Compression.RetryDelaySeconds" : Return "Delay between compression retry attempts (seconds)"
            Case "Compression.FileLockTimeoutSeconds" : Return "Total time to wait for a file lock to clear before giving up (seconds)"

            ' Hardware
            Case "MaxCameras" : Return "Maximum number of cameras supported"

            ' Vehicles
            Case "Vehicles" : Return "Fleet vehicle configurations (edit via Edit Vehicles button)"
            Case "VehicleNumber" : Return "Unique vehicle identifier (e.g. 6SME5384)"
            Case "Processors" : Return "Comma-separated processor list (e.g. ACP3_MCU,NA,NA,NA,NA,NA)"
            Case "CameraPositions" : Return "Comma-separated camera position names (e.g. FRONT,REAR,NA,...)"
            Case "CanMonitors" : Return "Comma-separated CAN monitor IDs (e.g. 886,886,886,886)"
            Case "DataUploadPath" : Return "Upload sub-path on network drive (e.g. ACP3\VehicleData\)"
            Case "CLEVIRFilesPath" : Return "Network path to CLEVIR signal/workspace files (e.g. Current\ACP3)"
            Case "ZipMF4Files" : Return "Zip MF4 files after recording (True/False)"
            Case "ConfigName" : Return "Workspace config name (e.g. ACP3_1P1C)"

            ' Global Application Settings
            Case "CurrentVehicleUsage" : Return "Current usage mode (e.g. VALIDATION)"
            Case "SelectedVehicleNumber" : Return "Currently active vehicle number from the Vehicles list"
            Case "SaveCalSnapshotEnabled" : Return "Save a calibration snapshot on session start (True/False)"
            Case "APICommErrorMsgDelayTime" : Return "Delay before showing API communication error messages (seconds)"
            Case "SkipSubscriptionOnCacheHit" : Return "Skip re-subscribing signals when cached subscription is valid (True/False)"

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

            BackupConfigFile()
            SaveConfiguration()

            MessageBox.Show($"Configuration saved to config.xml successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information)
            _isDirty = False

        Catch ex As Exception
            HandleUserMessageLogging("GMRC", $"ButtonSave_Click: {ex.Message}")
            MessageBox.Show($"Save failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SaveConfiguration()
        Dim configPath As String = Path.Combine(My.Application.Info.DirectoryPath, "config.xml")

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

    Private Sub BackupConfigFile()
        Try
            Dim configPath As String = Path.Combine(My.Application.Info.DirectoryPath, "config.xml")
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
