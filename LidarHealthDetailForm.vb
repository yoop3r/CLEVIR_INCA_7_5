Option Strict On
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Threading.Tasks

''' <summary>
''' Detail form showing per-device LiDAR health diagnostics with integrity-first monitoring.
''' Prioritizes data corruption detection over packet loss metrics per OXTS/Hesai specifications.
''' </summary>
Public Class LidarHealthDetailForm
    Private _devices As List(Of LidarDevice)
    Private _refreshTimer As Timer
    Private _mainForm As GmResidentClient

    ''' <summary>
    ''' Constructor accepting list of LiDAR devices
    ''' </summary>
    Public Sub New(devices As List(Of LidarDevice), Optional mainForm As GmResidentClient = Nothing)
        InitializeComponent()

        _devices = devices
        _mainForm = mainForm

        SetupDataGridView()
        SetupRefreshTimer()
        LoadDeviceData()
    End Sub

    ''' <summary>
    ''' Configures the DataGridView for device health display
    ''' </summary>
    Private Sub SetupDataGridView()
        Try
            DataGridView1.AutoGenerateColumns = False
            DataGridView1.AllowUserToAddRows = False
            DataGridView1.AllowUserToDeleteRows = False
            DataGridView1.ReadOnly = True
            DataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            DataGridView1.MultiSelect = False
            DataGridView1.RowHeadersVisible = False
            DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

            DataGridView1.Columns.Clear()

            ' Device identification
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "DeviceId",
                .HeaderText = "Device ID",
                .DataPropertyName = "DeviceId",
                .FillWeight = 12
            })

            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "Status",
                .HeaderText = "Status",
                .DataPropertyName = "Status",
                .FillWeight = 12
            })

            ' Packet counts
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "PacketCount",
                .HeaderText = "Packets",
                .DataPropertyName = "PacketCount",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"},
                .FillWeight = 12
            })

            ' ✅ NEW: Integrity metrics (replacing "Dropped" and "Loss %")
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "IntegrityPercent",
                .HeaderText = "Integrity %",
                .DataPropertyName = "IntegrityPercent",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "F2"},
                .FillWeight = 12
            })

            ' ✅ NEW: Corruption count (critical errors)
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "CorruptedPackets",
                .HeaderText = "Corrupted",
                .DataPropertyName = "CorruptedPackets",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"},
                .FillWeight = 12
            })

            ' Checksum errors (Hesai SDK specific)
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "ChecksumErrors",
                .HeaderText = "Checksum Err",
                .DataPropertyName = "ChecksumErrors",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"},
                .FillWeight = 12
            })

            ' Out-of-order packets
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "OutOfOrder",
                .HeaderText = "Out-of-Order",
                .DataPropertyName = "OutOfOrderPackets",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"},
                .FillWeight = 12
            })

            ' Timing info
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "LastPacket",
                .HeaderText = "Last Packet",
                .DataPropertyName = "LastPacket",
                .FillWeight = 13
            })

            ' ✅ NEW: Operational state column
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "OperationalInfo",
                .HeaderText = "Operational Info",
                .DataPropertyName = "OperationalInfo",
                .FillWeight = 20
            })

            ' Data volume
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "TotalBytes",
                .HeaderText = "Total MB",
                .DataPropertyName = "TotalMB",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "F2"},
                .FillWeight = 11
            })

        Catch ex As Exception
            MessageBox.Show($"Error setting up grid: {ex.Message}", "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' ✅ REFACTORED: Updates OXTS status panel with integrity metrics
    ''' </summary>
    Private Sub UpdateOxtsStatus()
        If _mainForm Is Nothing OrElse _mainForm.MyOxtsInterface Is Nothing Then
            Return
        End If

        Dim oxtsIf = _mainForm.MyOxtsInterface

        ' Update PTP Status
        Dim lblPtp = TryCast(Me.Controls.Find("Label_PtpStatus", True).FirstOrDefault(), Label)
        If lblPtp IsNot Nothing Then
            lblPtp.Text = $"PTP: {OxtsStatusChannelDecoder.GetPtpStatusDescription(oxtsIf.PtpStatus)}"
            lblPtp.ForeColor = If(oxtsIf.IsPtpSynchronized(), Color.Green, Color.Red)
        End If

        ' Update GPS Lock
        Dim lblGps = TryCast(Me.Controls.Find("Label_GpsLock", True).FirstOrDefault(), Label)
        If lblGps IsNot Nothing Then
            Dim lockStatus = If(oxtsIf.IsGpsLocked, "LOCKED", "NO LOCK")
            lblGps.Text = $"GPS: {lockStatus}"
            lblGps.ForeColor = If(oxtsIf.IsGpsLocked, Color.Green, Color.Red)
        End If

        ' ✅ REPLACED: Packet Loss → Data Integrity
        Dim lblIntegrity = TryCast(Me.Controls.Find("Label_PacketLoss", True).FirstOrDefault(), Label)
        If lblIntegrity IsNot Nothing Then
            Dim integrityPercent = oxtsIf.PacketIntegrityPercent
            Dim corruptionPercent = oxtsIf.PacketCorruptionPercent

            lblIntegrity.Text = $"Integrity: {integrityPercent:F1}% (Corrupt: {corruptionPercent:F2}%)"

            ' Color code based on corruption severity
            If corruptionPercent >= 5.0 Then
                lblIntegrity.ForeColor = Color.Red      ' Critical corruption
            ElseIf corruptionPercent >= 1.0 Then
                lblIntegrity.ForeColor = Color.Orange   ' Warning
            ElseIf integrityPercent >= 99.0 Then
                lblIntegrity.ForeColor = Color.Green    ' Excellent
            Else
                lblIntegrity.ForeColor = Color.DarkGreen ' Good
            End If
        End If

        ' ✅ NEW: Show packet counts
        Dim lblPackets = TryCast(Me.Controls.Find("Label_PacketCount", True).FirstOrDefault(), Label)
        If lblPackets IsNot Nothing Then
            lblPackets.Text = $"NCOM: {oxtsIf.ValidPacketsReceived:N0} valid / {oxtsIf.TotalPacketsReceived:N0} total"
            lblPackets.ForeColor = Color.Black
        End If
    End Sub

    ''' <summary>
    ''' Sets up auto-refresh timer (updates every 2 seconds)
    ''' </summary>
    Private Sub SetupRefreshTimer()
        _refreshTimer = New Timer With {.Interval = 2000}
        AddHandler _refreshTimer.Tick, AddressOf RefreshTimer_Tick
        _refreshTimer.Start()
    End Sub

    ''' <summary>
    ''' ✅ REFACTORED: Loads device data with integrity-focused metrics
    ''' </summary>
    Private Sub LoadDeviceData()
        Try
            If _devices Is Nothing OrElse _devices.Count = 0 Then
                Label_Summary.Text = "No LiDAR devices configured"
                Return
            End If

            Dim rows As New List(Of DeviceHealthRow)
            Dim healthyCount As Integer = 0
            Dim warningCount As Integer = 0
            Dim criticalCount As Integer = 0

            For Each device In _devices
                Dim row As New DeviceHealthRow(device)
                rows.Add(row)

                Select Case row.HealthStatus
                    Case DeviceHealthStatus.Healthy : healthyCount += 1
                    Case DeviceHealthStatus.Warning : warningCount += 1
                    Case DeviceHealthStatus.Critical : criticalCount += 1
                End Select
            Next

            DataGridView1.DataSource = Nothing
            DataGridView1.DataSource = rows

            ' Color-code rows by health status
            For i As Integer = 0 To DataGridView1.Rows.Count - 1
                Dim row = DirectCast(rows(i), DeviceHealthRow)
                ColorCodeRow(DataGridView1.Rows(i), row.HealthStatus)
            Next

            ' ✅ NEW: Highlight corruption issues in red
            For i As Integer = 0 To DataGridView1.Rows.Count - 1
                Dim checksumCell = DataGridView1.Rows(i).Cells("ChecksumErrors")
                Dim integrityCell = DataGridView1.Rows(i).Cells("IntegrityPercent")

                ' Red highlight if checksum errors exceed threshold
                If CLng(checksumCell.Value) > 100 Then
                    checksumCell.Style.BackColor = Color.Red
                    checksumCell.Style.ForeColor = Color.White
                End If

                ' Red highlight if integrity below 95%
                Dim integrity = CDbl(integrityCell.Value)
                If integrity < 95.0 Then
                    integrityCell.Style.BackColor = Color.Orange
                    integrityCell.Style.ForeColor = Color.Black
                End If
                If integrity < 90.0 Then
                    integrityCell.Style.BackColor = Color.Red
                    integrityCell.Style.ForeColor = Color.White
                End If
            Next

            Label_Summary.Text = $"Total: {_devices.Count} | ✅ Healthy: {healthyCount} | ⚠️ Warning: {warningCount} | ❌ Critical: {criticalCount}"

        Catch ex As Exception
            MessageBox.Show($"Error loading device data: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Colors a grid row based on health status
    ''' </summary>
    Private Sub ColorCodeRow(row As DataGridViewRow, status As DeviceHealthStatus)
        Select Case status
            Case DeviceHealthStatus.Healthy
                row.DefaultCellStyle.BackColor = Color.LightGreen
                row.DefaultCellStyle.ForeColor = Color.Black
            Case DeviceHealthStatus.Warning
                row.DefaultCellStyle.BackColor = Color.Orange
                row.DefaultCellStyle.ForeColor = Color.Black
            Case DeviceHealthStatus.Critical
                row.DefaultCellStyle.BackColor = Color.LightCoral
                row.DefaultCellStyle.ForeColor = Color.White
        End Select
    End Sub

    Private Sub RefreshTimer_Tick(sender As Object, e As EventArgs)
        LoadDeviceData()
        UpdateOxtsStatus()
    End Sub

    Private Sub Button_Refresh_Click(sender As Object, e As EventArgs) Handles Button_Refresh.Click
        LoadDeviceData()
    End Sub

    Private Sub Button_Close_Click(sender As Object, e As EventArgs) Handles Button_Close.Click
        Me.Close()
    End Sub

    Private Sub LidarHealthDetailForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If _refreshTimer IsNot Nothing Then
            _refreshTimer.Stop()
            _refreshTimer.Dispose()
            _refreshTimer = Nothing
        End If
    End Sub

    Private Sub Button_Export_Click(sender As Object, e As EventArgs) Handles Button_Export.Click
        Try
            Dim saveDialog As New SaveFileDialog With {
                .Filter = "CSV Files (*.csv)|*.csv",
                .FileName = $"LidarHealth_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                .Title = "Export LiDAR Health Report"
            }

            If saveDialog.ShowDialog() = DialogResult.OK Then
                ExportToCsv(saveDialog.FileName)
                MessageBox.Show($"Health report exported to:{vbCrLf}{saveDialog.FileName}", "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ExportToCsv(filename As String)
        Using writer As New IO.StreamWriter(filename)
            ' Write header
            Dim headers As New List(Of String)
            For Each col As DataGridViewColumn In DataGridView1.Columns
                headers.Add(col.HeaderText)
            Next
            writer.WriteLine(String.Join(",", headers))

            ' Write data rows
            For Each row As DataGridViewRow In DataGridView1.Rows
                Dim values As New List(Of String)
                For Each cell As DataGridViewCell In row.Cells
                    values.Add($"""")
                Next
                writer.WriteLine(String.Join(",", values))
            Next
        End Using
    End Sub

    Private Sub TestOxtsConnection_Click(sender As Object, e As EventArgs) Handles Button_TestOxts.Click
        If _mainForm Is Nothing Then
            StatusNotifier.Warn("Main form reference not available", "Test")
            Return
        End If

        If _mainForm.MyOxtsInterface Is Nothing Then
            StatusNotifier.Warn("OXTS not initialized", "Test")
            Return
        End If

        _mainForm.MyOxtsInterface.TestOxtsIntegration()
        LoadDeviceData()
        StatusNotifier.Toast("OXTS test complete - check diagnostics", "Test")
    End Sub

    Private Sub TestLidarCapture_Click(sender As Object, e As EventArgs) Handles Button_TestLidar.Click
        If _devices Is Nothing OrElse _devices.Count = 0 Then
            StatusNotifier.Warn("No LiDAR devices configured", "Test")
            Return
        End If

        For Each lidar In _devices
            lidar.TestLidarOxtsIntegration()
        Next

        LoadDeviceData()
        StatusNotifier.Toast($"LiDAR test complete for {_devices.Count} device(s)", "Test")
    End Sub

    Private Sub TestOxtsLidarIntegration_Click(sender As Object, e As EventArgs) Handles Button_TestIntegration.Click
        HandleUserMessageLogging("GMRC", "=== FULL INTEGRATION TEST ===")

        If _mainForm IsNot Nothing AndAlso _mainForm.MyOxtsInterface IsNot Nothing Then
            _mainForm.MyOxtsInterface.TestOxtsIntegration()
        Else
            HandleUserMessageLogging("GMRC", "⚠️ OXTS not initialized")
        End If

        If _devices IsNot Nothing AndAlso _devices.Count > 0 Then
            For Each lidar In _devices
                lidar.TestLidarOxtsIntegration()
            Next
        Else
            HandleUserMessageLogging("GMRC", "⚠️ No LiDAR devices")
        End If

        HandleUserMessageLogging("GMRC", "=== INTEGRATION TEST COMPLETE ===")
        LoadDeviceData()
        UpdateOxtsStatus()
        StatusNotifier.Toast("Integration test complete - check logs", "Test")
    End Sub

    Private Sub InjectTestMarker_Click(sender As Object, e As EventArgs)
        If _devices Is Nothing OrElse _devices.Count = 0 Then
            StatusNotifier.Warn("No LiDAR devices capturing", "Test")
            Return
        End If

        Dim injected As Integer = 0
        For Each lidar In _devices
            If lidar.IsCapturing Then
                lidar.InjectTestMarkerWithOxtsData()
                injected += 1
            End If
        Next

        If injected > 0 Then
            StatusNotifier.Toast($"Injected test marker into {injected} LiDAR(s)", "Test")
        Else
            StatusNotifier.Warn("No active LiDAR captures", "Test")
        End If
    End Sub

    ''' <summary>
    ''' ✅ REPLACED: Diagnose packet loss → Report data integrity
    ''' </summary>
    Private Sub Button_DiagnoseOxts_Click(sender As Object, e As EventArgs) Handles Button_DiagnoseOxts.Click
        If _mainForm Is Nothing OrElse _mainForm.MyOxtsInterface Is Nothing Then
            StatusNotifier.Warn("OXTS not initialized", "Diagnose")
            Return
        End If

        ' Generate comprehensive integrity report
        _mainForm.MyOxtsInterface.ReportPacketIntegrity()

        ' Refresh UI
        UpdateOxtsStatus()
        LoadDeviceData()

        StatusNotifier.Toast("OXTS integrity report generated - check diagnostics window", "Diagnose")
    End Sub

    ''' <summary>
    ''' ✅ NEW: Reset OXTS integrity statistics
    ''' </summary>
    Private Sub Button_ResetOxtsStats_Click(sender As Object, e As EventArgs) Handles Button_ResetOxtsStats.Click
        If _mainForm Is Nothing OrElse _mainForm.MyOxtsInterface Is Nothing Then
            StatusNotifier.Warn("OXTS not initialized", "Reset")
            Return
        End If

        _mainForm.MyOxtsInterface.ResetIntegrityStats()

        ' Refresh after 5 seconds to show new stats
        Task.Run(Async Function()
                     Await Task.Delay(5000)
                     Me.Invoke(Sub()
                                   UpdateOxtsStatus()
                                   LoadDeviceData()
                               End Sub)
                 End Function)

        StatusNotifier.Toast("OXTS integrity counters reset. Check again in 5 seconds.", "Reset")
    End Sub

    Private Enum DeviceHealthStatus
        Healthy
        Warning
        Critical
    End Enum

    ''' <summary>
    ''' ✅ REFACTORED: Data row using Hesai packet parser for accurate health monitoring
    ''' Parses UDP sequence numbers directly from Hesai AT128 packets
    ''' </summary>
    Private Class DeviceHealthRow
        Public Property DeviceId As String
        Public Property Status As String
        Public Property PacketCount As Long
        Public Property IntegrityPercent As Double
        Public Property CorruptedPackets As Long
        Public Property ChecksumErrors As Long
        Public Property OutOfOrderPackets As Long
        Public Property LastPacket As String
        Public Property TotalMB As Double
        Public Property HealthStatus As DeviceHealthStatus
        Public Property OperationalInfo As String  ' ✅ NEW: Operational state + RPM

        Public Sub New(device As LidarDevice)
            DeviceId = device.DeviceId

            ' Get basic counters
            PacketCount = device.PacketCount
            ChecksumErrors = 0  ' Not tracked by parser yet
            OutOfOrderPackets = device.OutOfOrderPackets

            ' ✅ NEW: Use DroppedPackets from Hesai sequence gap detection
            Dim droppedPackets As Long = device.DroppedPackets
            CorruptedPackets = droppedPackets + OutOfOrderPackets

            ' ✅ Calculate integrity from actual sequence gaps
            Dim totalExpected As Long = PacketCount + droppedPackets
            If totalExpected > 0 Then
                IntegrityPercent = ((totalExpected - droppedPackets) / CDbl(totalExpected)) * 100.0
            Else
                IntegrityPercent = 100.0
            End If

            TotalMB = device.TotalBytes / 1024.0 / 1024.0

            ' ✅ NEW: Show Hesai operational state and motor speed
            Dim hesaiInfo As HesaiPacketInfo = device.LastHesaiInfo
            If hesaiInfo.IsValid Then
                Dim opState As String = GetOperationalStateString(hesaiInfo.OperationalState)
                Dim returnMode As String = GetReturnModeString(hesaiInfo.ReturnMode)
                OperationalInfo = $"{opState} @ {hesaiInfo.MotorSpeed} RPM | {returnMode}"

                ' ✅ Show functional safety alerts if present
                If hesaiInfo.HasFunctionalSafety AndAlso hesaiInfo.LidarState > 1 Then
                    Dim fsState As String = GetLidarStateString(hesaiInfo.LidarState)
                    OperationalInfo &= $" | FS: {fsState}"

                    If hesaiInfo.FaultCode > 0 Then
                        OperationalInfo &= $" (Fault: 0x{hesaiInfo.FaultCode:X4})"
                    End If
                End If
            Else
                OperationalInfo = "Parsing..."
            End If

            ' Format last packet time
            If device.LastPacketTimestamp.HasValue Then
                Dim elapsed = DateTime.Now - device.LastPacketTimestamp.Value
                If elapsed.TotalSeconds < 60 Then
                    LastPacket = $"{elapsed.TotalSeconds:F0}s ago"
                ElseIf elapsed.TotalMinutes < 60 Then
                    LastPacket = $"{elapsed.TotalMinutes:F0}m ago"
                Else
                    LastPacket = device.LastPacketTimestamp.Value.ToString("HH:mm:ss")
                End If
            Else
                LastPacket = "Never"
            End If

            ' ✅ REFACTORED: Health determination with integrity thresholds
            If Not device.LastPacketTimestamp.HasValue AndAlso device.PacketCount = 0 Then
                HealthStatus = DeviceHealthStatus.Critical
                Status = "NO COMMS"

            ElseIf device.LastPacketTimestamp.HasValue AndAlso (DateTime.Now - device.LastPacketTimestamp.Value).TotalSeconds > 5 Then
                HealthStatus = DeviceHealthStatus.Critical
                Status = "STOPPED"

            ElseIf hesaiInfo.IsValid AndAlso hesaiInfo.OperationalState = 1 Then
                ' Operational State = 1 = Shutdown
                HealthStatus = DeviceHealthStatus.Critical
                Status = "SHUTDOWN"

            ElseIf hesaiInfo.HasFunctionalSafety AndAlso hesaiInfo.LidarState >= 5 Then
                ' LidarState: 5=Pre-Shutdown, 6=Shutdown
                HealthStatus = DeviceHealthStatus.Critical
                Status = "FS: PRE-SHUTDOWN"

            ElseIf IntegrityPercent < 90.0 Then
                ' Critical packet loss (>10%)
                HealthStatus = DeviceHealthStatus.Critical
                Status = "DATA LOSS"

            ElseIf hesaiInfo.HasFunctionalSafety AndAlso hesaiInfo.LidarState >= 3 Then
                ' LidarState: 3=Pre-Perf Degradation, 4=Perf Degradation
                HealthStatus = DeviceHealthStatus.Warning
                Status = "FS: DEGRADED"

            ElseIf IntegrityPercent < 95.0 Then
                ' Warning on moderate packet loss (5-10%)
                HealthStatus = DeviceHealthStatus.Warning
                Status = "INTEGRITY LOW"

            ElseIf droppedPackets > 1000 Then
                ' Warning on high absolute loss
                HealthStatus = DeviceHealthStatus.Warning
                Status = "PACKET LOSS"

            ElseIf device.IsCapturing AndAlso device.PacketCount > 0 Then
                HealthStatus = DeviceHealthStatus.Healthy
                Status = "Capturing"

            ElseIf device.PacketCount > 0 Then
                HealthStatus = DeviceHealthStatus.Healthy
                Status = "Healthy"

            Else
                HealthStatus = DeviceHealthStatus.Warning
                Status = "Idle"
            End If
        End Sub

        ''' <summary>
        ''' ✅ NEW: Decode Hesai operational state byte
        ''' </summary>
        Private Shared Function GetOperationalStateString(state As Byte) As String
            Select Case state
                Case 0 : Return "High Resolution"
                Case 1 : Return "Shutdown"
                Case 2 : Return "Standard"
                Case 3 : Return "Energy Saving"
                Case Else : Return $"Unknown ({state})"
            End Select
        End Function

        ''' <summary>
        ''' ✅ NEW: Decode Hesai return mode byte
        ''' </summary>
        Private Shared Function GetReturnModeString(mode As Byte) As String
            Select Case mode
                Case &H33 : Return "First"
                Case &H37 : Return "Strongest"
                Case &H38 : Return "Last"
                Case &H39 : Return "Dual(Last+Strong)"
                Case &H3B : Return "Dual(Last+First)"
                Case &H3C : Return "Dual(First+Strong)"
                Case Else : Return $"Mode 0x{mode:X2}"
            End Select
        End Function

        ''' <summary>
        ''' ✅ NEW: Decode Hesai functional safety LiDAR state
        ''' </summary>
        Private Shared Function GetLidarStateString(state As Byte) As String
            Select Case state
                Case 0 : Return "Init"
                Case 1 : Return "Normal"
                Case 2 : Return "Warning"
                Case 3 : Return "Pre-Perf Degradation"
                Case 4 : Return "Perf Degradation"
                Case 5 : Return "Pre-Shutdown"
                Case 6 : Return "Shutdown"
                Case Else : Return $"Unknown ({state})"
            End Select
        End Function
    End Class
End Class