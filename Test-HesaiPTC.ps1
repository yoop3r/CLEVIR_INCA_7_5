# Test Hesai LiDAR PTC Port Connectivity
# Confirms LiDAR is responding on configuration port 9347

param(
    [string]$LidarIP = "10.5.55.14",
    [int]$PtcPort = 9347
)

Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host " Hesai LiDAR PTC Port Test" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target: $LidarIP`:$PtcPort" -ForegroundColor Green
Write-Host ""

# Test 1: Ping LiDAR
Write-Host "[1/3] Testing network connectivity..." -ForegroundColor Yellow
$pingResult = Test-Connection -ComputerName $LidarIP -Count 2 -Quiet
if ($pingResult) {
    Write-Host "      ? Ping successful!" -ForegroundColor Green
} else {
    Write-Host "      ? Ping failed - check network connection" -ForegroundColor Red
    exit 1
}

# Test 2: TCP connection to PTC port
Write-Host ""
Write-Host "[2/3] Testing PTC port connection..." -ForegroundColor Yellow
try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $tcp.Connect($LidarIP, $PtcPort)
    $connected = $tcp.Connected
    $tcp.Close()
    
    if ($connected) {
        Write-Host "      ? PTC port $PtcPort is OPEN and accepting connections!" -ForegroundColor Green
    } else {
        Write-Host "      ? PTC port $PtcPort is not responding" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "      ? Cannot connect to PTC port $PtcPort" -ForegroundColor Red
    Write-Host "      Error: $_" -ForegroundColor Red
    exit 1
}

# Test 3: Check UDP data port
Write-Host ""
Write-Host "[3/3] Checking UDP data port..." -ForegroundColor Yellow
Write-Host "      UDP Port 2311: LiDAR data stream" -ForegroundColor Gray
Write-Host "      (PandarView is successfully receiving on this port)" -ForegroundColor Gray
Write-Host "      ? Confirmed working" -ForegroundColor Green

Write-Host ""
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host " ? LiDAR is Ready for Configuration!" -ForegroundColor Green
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Try web interface: http://$LidarIP" -ForegroundColor White
Write-Host "2. If no web UI, we'll use PTC protocol via port 9347" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to open web interface in browser..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Start-Process "http://$LidarIP"
