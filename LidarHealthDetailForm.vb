Option Strict On
Imports System.Windows.Forms
Imports System.Drawing

''' <summary>
''' Detail form showing per-device LiDAR health diagnostics
''' </summary>
Public Class LidarHealthDetailForm
    Private _devices As List(Of LidarDevice)
    Private _refreshTimer As Timer

    ''' <summary>
    ''' Constructor accepting list of LiDAR devices
    ''' </summary>
    Public Sub New(devices As List(Of LidarDevice))
        InitializeComponent()

        _devices = devices

        ' Initialize UI
        SetupDataGridView()
        SetupRefreshTimer()
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

        Catch ex As Exception
            MessageBox.Show($"Error setting up grid: {ex.Message}", "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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

        Public Sub New(device As LidarDevice)
            DeviceId = device.DeviceId
            PacketCount = device.PacketCount
            DroppedPackets = device.DroppedPackets
            TotalMB = device.TotalBytes / 1024.0 / 1024.0

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