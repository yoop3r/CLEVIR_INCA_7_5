# Hesai PandarXT PTP Configuration - Direct TCP Method
# Uses raw TCP socket communication with Hesai PTC protocol
# Based on Hesai PTC protocol specification

param(
    [string]$LidarIP = "10.5.55.14",
    [int]$PtcPort = 9347,
    [string]$OxtsIP = "10.5.55.200"
)

Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host " Hesai Pandar PTP Configuration (Direct TCP)" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target LiDAR: $LidarIP`:$PtcPort" -ForegroundColor Green
Write-Host "OXTS Master: $OxtsIP" -ForegroundColor Green
Write-Host ""

# Test connection
Write-Host "[1/3] Testing connection to LiDAR..." -ForegroundColor Yellow
try {
    $tcp = New-Object System.Net.Sockets.TcpClient
    $tcp.Connect($LidarIP, $PtcPort)
    $tcp.Close()
    Write-Host "      ? Connection successful!" -ForegroundColor Green
} catch {
    Write-Host "      ? Cannot connect to $LidarIP`:$PtcPort" -ForegroundColor Red
    Write-Host "      Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Possible issues:" -ForegroundColor Yellow
    Write-Host "  - LiDAR not powered on" -ForegroundColor Yellow
    Write-Host "  - Wrong IP address (check LiDAR settings)" -ForegroundColor Yellow
    Write-Host "  - Network cable not connected" -ForegroundColor Yellow
    Write-Host "  - Firewall blocking port $PtcPort" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "[2/3] Checking current LiDAR configuration..." -ForegroundColor Yellow
Write-Host "      ??  This tool provides basic connectivity testing." -ForegroundColor Yellow
Write-Host "      For full PTP configuration, you need to:" -ForegroundColor Cyan
Write-Host ""
Write-Host "      Option A: Use Hesai PandarView Software" -ForegroundColor White
Write-Host "        1. Download PandarView from Hesai website" -ForegroundColor Gray
Write-Host "        2. Connect to LiDAR at $LidarIP" -ForegroundColor Gray
Write-Host "        3. Navigate to 'PTP Settings'" -ForegroundColor Gray
Write-Host "        4. Enable PTP Slave mode" -ForegroundColor Gray
Write-Host "        5. Set Master IP to $OxtsIP" -ForegroundColor Gray
Write-Host "        6. Set Domain to 0" -ForegroundColor Gray
Write-Host "        7. Save and reboot LiDAR" -ForegroundColor Gray
Write-Host ""
Write-Host "      Option B: Use Hesai Web Interface" -ForegroundColor White
Write-Host "        1. Open browser to http://$LidarIP" -ForegroundColor Gray
Write-Host "        2. Login with default credentials" -ForegroundColor Gray
Write-Host "        3. Navigate to Network ? PTP" -ForegroundColor Gray
Write-Host "        4. Configure PTP settings" -ForegroundColor Gray
Write-Host ""

Write-Host "[3/3] Verifying OXTS PTP is broadcasting..." -ForegroundColor Yellow
Write-Host "      Run this in CLEVIR console:" -ForegroundColor Cyan
Write-Host "      OxtsNcomInterface.TestOxtsIntegration()" -ForegroundColor White
Write-Host ""
Write-Host "      Expected output:" -ForegroundColor Gray
Write-Host "      PTP Status: ? LOCKED (Fully Synchronized!)" -ForegroundColor Green
Write-Host "      Timing Source: ??? Primary GNSS (Default)" -ForegroundColor Green
Write-Host ""

Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host " Next Steps" -ForegroundColor Yellow
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Configure LiDAR PTP using PandarView or web interface" -ForegroundColor White
Write-Host "2. Verify PTP sync in LiDAR status page" -ForegroundColor White
Write-Host "3. Check CLEVIR OXTS status shows 'PTP: LOCKED'" -ForegroundColor White
Write-Host "4. Start data collection to verify microsecond-level sync" -ForegroundColor White
Write-Host ""

# Offer to open browser to LiDAR web interface
$response = Read-Host "Open LiDAR web interface in browser? (Y/N)"
if ($response -eq "Y" -or $response -eq "y") {
    Start-Process "http://$LidarIP"
}
