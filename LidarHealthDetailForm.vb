Option Strict On
Imports System.Windows.Forms
Imports System.Drawing

''' <summary>
''' Detail form showing per-device LiDAR health diagnostics
''' </summary>
Public Class LidarHealthDetailForm
    Private _devices As List(Of LidarDevice)
    Private _refreshTimer As Timer
    Private _mainForm As GmResidentClient  ' ✅ ADD THIS - Reference to parent form

    ''' <summary>
    ''' Constructor accepting list of LiDAR devices
    ''' </summary>
    Public Sub New(devices As List(Of LidarDevice), Optional mainForm As GmResidentClient = Nothing)
        ' ✅ This loads all Designer controls (GroupBox_OxtsStatus, Panel_TestActions, buttons, etc.)
        InitializeComponent()

        _devices = devices
        _mainForm = mainForm

        ' ✅ Configure DataGridView (still done in code - easier than Designer)
        SetupDataGridView()

        ' ✅ Start auto-refresh timer
        SetupRefreshTimer()

        ' ✅ Load initial data
        LoadDeviceData()
    End Sub

    ''' <summary>
    ''' Configures the DataGridView for device health display
    ''' </summary>
    Private Sub SetupDataGridView()
        Try
            ' Configure DataGridView properties
            DataGridView1.AutoGenerateColumns = False
            DataGridView1.AllowUserToAddRows = False
            DataGridView1.AllowUserToDeleteRows = False
            DataGridView1.ReadOnly = True
            DataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            DataGridView1.MultiSelect = False
            DataGridView1.RowHeadersVisible = False
            DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

            ' Clear any existing columns
            DataGridView1.Columns.Clear()

            ' Add columns
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "DeviceId",
                .HeaderText = "Device ID",
                .DataPropertyName = "DeviceId",
                .FillWeight = 15
            })

            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "Status",
                .HeaderText = "Status",
                .DataPropertyName = "Status",
                .FillWeight = 15
            })

            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "PacketCount",
                .HeaderText = "Packets",
                .DataPropertyName = "PacketCount",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"},
                .FillWeight = 15
            })

            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "DroppedPackets",
                .HeaderText = "Dropped",
                .DataPropertyName = "DroppedPackets",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"},
                .FillWeight = 15
            })

            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "LossPercent",
                .HeaderText = "Loss %",
                .DataPropertyName = "LossPercent",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "F2"},
                .FillWeight = 12
            })

            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "LastPacket",
                .HeaderText = "Last Packet",
                .DataPropertyName = "LastPacket",
                .FillWeight = 20
            })

            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                .Name = "TotalBytes",
                .HeaderText = "Total MB",
                .DataPropertyName = "TotalMB",
                .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "F2"},
                .FillWeight = 13
            })
            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                                         .Name = "ChecksumErrors",
                                         .HeaderText = "Checksum Errors",
                                         .DataPropertyName = "ChecksumErrors",
                                         .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"},
                                         .FillWeight = 15
                                         })

            DataGridView1.Columns.Add(New DataGridViewTextBoxColumn With {
                                         .Name = "OutOfOrder",
                                         .HeaderText = "Out-of-Order",
                                         .DataPropertyName = "OutOfOrderPackets",
                                         .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N0"},
                                         .FillWeight = 15
                                         })
        Catch ex As Exception
            MessageBox.Show($"Error setting up grid: {ex.Message}", "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' ✅ NEW: Update OXTS status panel (called from RefreshTimer_Tick)
    ''' </summary>
    Private Sub UpdateOxtsStatus()
        If _mainForm Is Nothing OrElse _mainForm.MyOxtsInterface Is Nothing Then
            Return
        End If

        Dim oxtsIf = _mainForm.MyOxtsInterface

        ' Update PTP Status
        Dim lblPtp = TryCast(Me.Controls.Find("Label_PtpStatus", True).FirstOrDefault(), Label)
        If lblPtp IsNot Nothing Then
            lblPtp.Text = $"PTP: {oxtsIf.PtpStatus}"
            lblPtp.ForeColor = If(oxtsIf.IsPtpSynchronized(), Color.Green, Color.Red)
        End If

        ' Update GPS Lock
        Dim lblGps = TryCast(Me.Controls.Find("Label_GpsLock", True).FirstOrDefault(), Label)
        If lblGps IsNot Nothing Then
            Dim lockStatus = If(oxtsIf.IsGpsLocked, "LOCKED", "NO LOCK")
            lblGps.Text = $"GPS Lock: {lockStatus}"
            lblGps.ForeColor = If(oxtsIf.IsGpsLocked, Color.Green, Color.Red)
        End If

        ' Update Packet Loss (stub - implement GetNcomPacketLossPercent in OxtsNcomInterface)
        Dim lblLoss = TryCast(Me.Controls.Find("Label_PacketLoss", True).FirstOrDefault(), Label)
        If lblLoss IsNot Nothing Then
            ' TODO: Implement oxtsIf.GetNcomPacketLossPercent()
            lblLoss.Text = "Packet Loss: N/A"
            lblLoss.ForeColor = Color.Gray
        End If
    End Sub
    ''' <summary>
    ''' Sets up auto-refresh timer (updates every 2 seconds)
    ''' </summary>
    Private Sub SetupRefreshTimer()
        _refreshTimer = New Timer With {
            .Interval = 2000 ' 2 seconds
        }
        AddHandler _refreshTimer.Tick, AddressOf RefreshTimer_Tick
        _refreshTimer.Start()
    End Sub

    ''' <summary>
    ''' Loads device data into the grid
    ''' </summary>
    Private Sub LoadDeviceData()
        Try
            If _devices Is Nothing OrElse _devices.Count = 0 Then
                Label_Summary.Text = "No LiDAR devices configured"
                Return
            End If

            ' Build data rows
            Dim rows As New List(Of DeviceHealthRow)
            Dim healthyCount As Integer = 0
            Dim warningCount As Integer = 0
            Dim criticalCount As Integer = 0

            For Each device In _devices
                Dim row As New DeviceHealthRow(device)
                rows.Add(row)

                ' Count health statuses
                Select Case row.HealthStatus
                    Case DeviceHealthStatus.Healthy
                        healthyCount += 1
                    Case DeviceHealthStatus.Warning
                        warningCount += 1
                    Case DeviceHealthStatus.Critical
                        criticalCount += 1
                End Select
            Next

            ' Bind to grid
            DataGridView1.DataSource = Nothing
            DataGridView1.DataSource = rows

            ' Color-code rows
            For i As Integer = 0 To DataGridView1.Rows.Count - 1
                Dim row = DirectCast(rows(i), DeviceHealthRow)
                ColorCodeRow(DataGridView1.Rows(i), row.HealthStatus)
            Next

            For i As Integer = 0 To DataGridView1.Rows.Count - 1
                Dim checksumCell = DataGridView1.Rows(i).Cells("ChecksumErrors")
                If CLng(checksumCell.Value) > 100 Then
                    checksumCell.Style.BackColor = Color.Red
                End If
            Next

            ' Update summary label
            Label_Summary.Text = $"Total: {_devices.Count} | Healthy: {healthyCount} | Warning: {warningCount} | Critical: {criticalCount}"

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

    ''' <summary>
    ''' Timer tick handler for auto-refresh
    ''' </summary>
    Private Sub RefreshTimer_Tick(sender As Object, e As EventArgs)
        LoadDeviceData()
        UpdateOxtsStatus()  ' ✅ Add this
    End Sub

    ''' <summary>
    ''' Manual refresh button handler
    ''' </summary>
    Private Sub Button_Refresh_Click(sender As Object, e As EventArgs) Handles Button_Refresh.Click
        LoadDeviceData()
    End Sub

    ''' <summary>
    ''' Close button handler
    ''' </summary>
    Private Sub Button_Close_Click(sender As Object, e As EventArgs) Handles Button_Close.Click
        Me.Close()
    End Sub

    ''' <summary>
    ''' Form closing handler - cleanup timer
    ''' </summary>
    Private Sub LidarHealthDetailForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If _refreshTimer IsNot Nothing Then
            _refreshTimer.Stop()
            _refreshTimer.Dispose()
            _refreshTimer = Nothing
        End If
    End Sub

    ''' <summary>
    ''' Export to CSV button handler
    ''' </summary>
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

    ''' <summary>
    ''' Exports grid data to CSV file
    ''' </summary>
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
                    values.Add($"""{cell.Value}""")
                Next
                writer.WriteLine(String.Join(",", values))
            Next
        End Using
    End Sub

    ''' <summary>
    ''' ✅ FIXED: Test OXTS connection using parent form reference
    ''' </summary>
    Private Sub TestOxtsConnection_Click(sender As Object, e As EventArgs)
        If _mainForm Is Nothing Then
            StatusNotifier.Warn("Main form reference not available", "Test")
            Return
        End If

        If _mainForm.MyOxtsInterface Is Nothing Then
            StatusNotifier.Warn("OXTS not initialized", "Test")
            Return
        End If

        _mainForm.MyOxtsInterface.TestOxtsIntegration()
        LoadDeviceData()  ' Refresh grid immediately
        StatusNotifier.Toast("OXTS test complete - check diagnostics", "Test")
    End Sub

    ''' <summary>
    ''' ✅ FIXED: Test LiDAR capture using device list
    ''' </summary>
    Private Sub TestLidarCapture_Click(sender As Object, e As EventArgs)
        If _devices Is Nothing OrElse _devices.Count = 0 Then
            StatusNotifier.Warn("No LiDAR devices configured", "Test")
            Return
        End If

        For Each lidar In _devices
            lidar.TestLidarOxtsIntegration()
        Next

        LoadDeviceData()  ' Refresh grid
        StatusNotifier.Toast($"LiDAR test complete for {_devices.Count} device(s)", "Test")
    End Sub

    ''' <summary>
    ''' ✅ FIXED: Full integration test
    ''' </summary>
    Private Sub TestOxtsLidarIntegration_Click(sender As Object, e As EventArgs)
        HandleUserMessageLogging("GMRC", "=== FULL INTEGRATION TEST ===")

        ' Test OXTS
        If _mainForm IsNot Nothing AndAlso _mainForm.MyOxtsInterface IsNot Nothing Then
            _mainForm.MyOxtsInterface.TestOxtsIntegration()
        Else
            HandleUserMessageLogging("GMRC", "⚠️ OXTS not initialized")
        End If

        ' Test LiDAR
        If _devices IsNot Nothing AndAlso _devices.Count > 0 Then
            For Each lidar In _devices
                lidar.TestLidarOxtsIntegration()
            Next
        Else
            HandleUserMessageLogging("GMRC", "⚠️ No LiDAR devices")
        End If

        HandleUserMessageLogging("GMRC", "=== INTEGRATION TEST COMPLETE ===")
        LoadDeviceData()  ' Refresh grid
        UpdateOxtsStatus()  ' Refresh OXTS panel
        StatusNotifier.Toast("Integration test complete - check logs", "Test")
    End Sub

    ''' <summary>
    ''' ✅ FIXED: Inject test marker
    ''' </summary>
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
    ''' Helper enum for device health status
    ''' </summary>
    Private Enum DeviceHealthStatus
        Healthy
        Warning
        Critical
    End Enum

    ''' <summary>
    ''' Data row class for binding to DataGridView
    ''' </summary>
    Private Class DeviceHealthRow
        Public Property DeviceId As String
        Public Property Status As String
        Public Property PacketCount As Long
        Public Property DroppedPackets As Long
        Public Property LossPercent As Double
        Public Property LastPacket As String
        Public Property TotalMB As Double
        Public Property HealthStatus As DeviceHealthStatus
        Public Property ChecksumErrors As Long
        Public Property OutOfOrderPackets As Long

        Public Sub New(device As LidarDevice)
            DeviceId = device.DeviceId
            PacketCount = device.PacketCount
            DroppedPackets = device.DroppedPackets
            TotalMB = device.TotalBytes / 1024.0 / 1024.0
            ChecksumErrors = device.ChecksumErrors  ' ← Now populated from SDK
            OutOfOrderPackets = device.OutOfOrderPackets

            ' Enhanced health logic using real error counts
            If ChecksumErrors > 100 OrElse OutOfOrderPackets > 50 Then
                HealthStatus = DeviceHealthStatus.Critical
                Status = "DATA CORRUPT"
            ElseIf LossPercent >= 20.0 Then
                HealthStatus = DeviceHealthStatus.Critical
                Status = "CRITICAL"
            End If

            ' Calculate loss percentage
            Dim totalPackets As Long = PacketCount + DroppedPackets
            If totalPackets > 0 Then
                LossPercent = (CDbl(DroppedPackets) / CDbl(totalPackets)) * 100.0
            Else
                LossPercent = 0.0
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

            ' ================================================================
            ' Determine health status (synchronized with OnVehicleScreen logic)
            ' ================================================================

            ' **NEW: Check if device has NEVER received packets (comms never established)**
            If Not device.LastPacketTimestamp.HasValue AndAlso device.PacketCount = 0 Then
                HealthStatus = DeviceHealthStatus.Critical
                Status = "NO COMMS"  ' Clear indicator of initialization failure

                ' Check if device stopped (no packets in last 5 seconds)
            ElseIf device.LastPacketTimestamp.HasValue AndAlso (DateTime.Now - device.LastPacketTimestamp.Value).TotalSeconds > 5 Then
                HealthStatus = DeviceHealthStatus.Critical
                Status = "STOPPED"

                ' Check for critical packet loss (>20%)
            ElseIf LossPercent >= 20.0 Then
                HealthStatus = DeviceHealthStatus.Critical
                Status = "CRITICAL"

                ' Check for warning packet loss (5-20%)
            ElseIf LossPercent >= 5.0 Then
                HealthStatus = DeviceHealthStatus.Warning
                Status = "WARNING"

                ' Everything looks good
            Else
                HealthStatus = DeviceHealthStatus.Healthy
                Status = "Healthy"
            End If
        End Sub
    End Class
End Class