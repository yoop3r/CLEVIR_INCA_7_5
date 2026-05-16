# Monitor-LidarCapture.ps1
# Monitors CLEVIR LiDAR capture performance

param(
    [int]$DurationSeconds = 300,  # 5 minutes
    [string]$OutputCsv = "lidar_performance.csv"
)

Write-Host "=== LiDAR Capture Performance Monitor ===" -ForegroundColor Cyan
Write-Host "Duration: $DurationSeconds seconds"
Write-Host "Output: $OutputCsv"
Write-Host ""

# Find CLEVIR process
$processName = "CLEVIR_INCA_7_5"
$process = Get-Process -Name $processName -ErrorAction SilentlyContinue

if (-not $process) {
    Write-Host "ERROR: $processName process not found!" -ForegroundColor Red
    exit 1
}

Write-Host "Found process: $($process.ProcessName) (PID: $($process.Id))" -ForegroundColor Green
Write-Host "Starting monitoring..." -ForegroundColor Yellow
Write-Host ""

# CSV header
"Timestamp,CPU_Percent,Memory_MB,Threads,Handles" | Out-File -FilePath $OutputCsv -Encoding UTF8

$startTime = Get-Date
$sampleCount = 0

while ((Get-Date).Subtract($startTime).TotalSeconds -lt $DurationSeconds) {
    try {
        # Get fresh process object
        $process = Get-Process -Id $process.Id -ErrorAction Stop
        
        $cpuPercent = $process.CPU
        $memoryMB = [Math]::Round($process.WorkingSet64 / 1MB, 2)
        $threads = $process.Threads.Count
        $handles = $process.HandleCount
        
        # Calculate CPU percentage (average over last second)
        $cpuSnapshot = $process.TotalProcessorTime
        Start-Sleep -Milliseconds 1000
        $process = Get-Process -Id $process.Id -ErrorAction Stop
        $cpuDelta = ($process.TotalProcessorTime - $cpuSnapshot).TotalMilliseconds
        $cpuPercentNow = [Math]::Round(($cpuDelta / 1000) * 100, 2)
        
        # Log to CSV
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        "$timestamp,$cpuPercentNow,$memoryMB,$threads,$handles" | Out-File -FilePath $OutputCsv -Append -Encoding UTF8
        
        # Console output (every 10 samples)
        $sampleCount++
        if ($sampleCount % 10 -eq 0) {
            Write-Host ("[{0}] CPU: {1}% | Memory: {2} MB | Threads: {3}" -f $timestamp, $cpuPercentNow, $memoryMB, $threads)
        }
        
    } catch {
        Write-Host "Process terminated" -ForegroundColor Yellow
        break
    }
}

Write-Host ""
Write-Host "=== Monitoring Complete ===" -ForegroundColor Green
Write-Host "Data saved to: $OutputCsv"

# Calculate summary statistics
$data = Import-Csv $OutputCsv
$avgCpu = ($data | Measure-Object -Property CPU_Percent -Average).Average
$maxCpu = ($data | Measure-Object -Property CPU_Percent -Maximum).Maximum
$avgMem = ($data | Measure-Object -Property Memory_MB -Average).Average
$maxMem = ($data | Measure-Object -Property Memory_MB -Maximum).Maximum

Write-Host ""
Write-Host "=== Summary Statistics ===" -ForegroundColor Cyan
Write-Host "CPU Average: $([Math]::Round($avgCpu, 2))%"
Write-Host "CPU Peak: $([Math]::Round($maxCpu, 2))%"
Write-Host "Memory Average: $([Math]::Round($avgMem, 2)) MB"
Write-Host "Memory Peak: $([Math]::Round($maxMem, 2)) MB"
Write-Host "Sample Count: $sampleCount"
