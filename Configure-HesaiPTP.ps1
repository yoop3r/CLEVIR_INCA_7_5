# Hesai Pandar128E3X PTP Configuration Script
# Configures LiDAR to sync with OXTS RT3000 via PTP (IEEE 1588)

param(
    [string]$LidarIP = "10.5.55.14",
    [string]$OxtsIP = "10.5.55.200",
    [int]$PtpDomain = 0,
    [int]$PtcPort = 9347,      # ? FIXED: Added PTC port parameter (default: 9347)
    [switch]$SkipBackup,
    [switch]$AutoReboot
)

# Path to ptc_tool
$ptcTool = "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\HesaiLidar_SDK_2.0-master\tool_ptc\out\build\x64-Debug\Debug\ptc_tool.exe"

# Check if ptc_tool exists
if (-not (Test-Path $ptcTool)) {
    Write-Host "? ERROR: ptc_tool.exe not found at:" -ForegroundColor Red
    Write-Host "   $ptcTool" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Please build the Hesai SDK first or update the path in this script." -ForegroundColor Yellow
    exit 1
}

Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host " Hesai Pandar128E3X PTP Configuration Tool" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Green
Write-Host "  LiDAR IP:      $LidarIP"
Write-Host "  PTC Port:      $PtcPort"
Write-Host "  OXTS IP:       $OxtsIP"
Write-Host "  PTP Domain:    $PtpDomain"
Write-Host ""

# Function to execute ptc_tool command
function Invoke-PtcTool {
    param([string]$Arguments)
    
    # ? FIXED: Always include LidarIP and PtcPort
    $fullArgs = "$LidarIP $PtcPort $Arguments"
    $output = & $ptcTool $fullArgs 2>&1
    return $output
}

# Step 1: Backup current configuration
if (-not $SkipBackup) {
    Write-Host "[1/6] Backing up current configuration..." -ForegroundColor Yellow
    $backupFile = "C:\DEV\CLEVIR\hesai_config_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    
    $configOutput = Invoke-PtcTool "--get-config"
    $configOutput | Out-File -FilePath $backupFile -Encoding UTF8
    
    Write-Host "      ? Backup saved to: $backupFile" -ForegroundColor Green
    Write-Host ""
}

# Step 2: Get current PTP status
Write-Host "[2/6] Checking current PTP status..." -ForegroundColor Yellow
$ptpStatus = Invoke-PtcTool "--get-ptp-status"
Write-Host "      Current Status:" -ForegroundColor Gray
$ptpStatus | ForEach-Object { Write-Host "        $_" -ForegroundColor Gray }
Write-Host ""

# Step 3: Configure PTP
Write-Host "[3/6] Configuring PTP settings..." -ForegroundColor Yellow

Write-Host "      Enabling PTP..." -NoNewline
$result = Invoke-PtcTool "--set-ptp-enable 1"
if ($LASTEXITCODE -eq 0) {
    Write-Host " ?" -ForegroundColor Green
} else {
    Write-Host " ? Failed" -ForegroundColor Red
    Write-Host "      Error: $result" -ForegroundColor Red
}

Write-Host "      Setting PTP domain to $PtpDomain..." -NoNewline
$result = Invoke-PtcTool "--set-ptp-domain $PtpDomain"
if ($LASTEXITCODE -eq 0) {
    Write-Host " ?" -ForegroundColor Green
} else {
    Write-Host " ? Failed" -ForegroundColor Red
}

Write-Host "      Setting PTP profile to IEEE 1588-2008..." -NoNewline
$result = Invoke-PtcTool "--set-ptp-profile 0"
if ($LASTEXITCODE -eq 0) {
    Write-Host " ?" -ForegroundColor Green
} else {
    Write-Host " ? Failed" -ForegroundColor Red
}

Write-Host "      Setting PTP master IP to $OxtsIP..." -NoNewline
$result = Invoke-PtcTool "--set-ptp-master-ip $OxtsIP"
if ($LASTEXITCODE -eq 0) {
    Write-Host " ?" -ForegroundColor Green
} else {
    Write-Host " ? Failed" -ForegroundColor Red
}
Write-Host ""

# Step 4: Save configuration
Write-Host "[4/6] Saving configuration to LiDAR flash..." -ForegroundColor Yellow
$result = Invoke-PtcTool "--save-config"
if ($LASTEXITCODE -eq 0) {
    Write-Host "      ? Configuration saved" -ForegroundColor Green
} else {
    Write-Host "      ? Failed to save configuration" -ForegroundColor Red
    Write-Host "      Error: $result" -ForegroundColor Red
}
Write-Host ""

# Step 5: Reboot LiDAR
Write-Host "[5/6] Rebooting LiDAR..." -ForegroundColor Yellow
if ($AutoReboot) {
    $result = Invoke-PtcTool "--reboot"
    Write-Host "      ? LiDAR rebooting... (waiting 45 seconds)" -ForegroundColor Yellow
    Start-Sleep -Seconds 45
    Write-Host "      ? Reboot complete" -ForegroundColor Green
} else {
    Write-Host "      ??  Manual reboot required!" -ForegroundColor Yellow
    Write-Host "      Run: ptc_tool $LidarIP $PtcPort --reboot" -ForegroundColor Cyan
    Write-Host ""
    $response = Read-Host "      Reboot now? (Y/N)"
    if ($response -eq "Y" -or $response -eq "y") {
        $result = Invoke-PtcTool "--reboot"
        Write-Host "      ? LiDAR rebooting... (waiting 45 seconds)" -ForegroundColor Yellow
        Start-Sleep -Seconds 45
        Write-Host "      ? Reboot complete" -ForegroundColor Green
    } else {
        Write-Host "      ??  Skipping reboot. Changes will not take effect until reboot!" -ForegroundColor Red
        exit 0
    }
}
Write-Host ""

# Step 6: Verify PTP synchronization
Write-Host "[6/6] Verifying PTP synchronization..." -ForegroundColor Yellow
Write-Host "      Checking PTP status (may take 10-15 seconds to lock)..." -ForegroundColor Gray

for ($i = 1; $i -le 15; $i++) {
    $ptpStatus = Invoke-PtcTool "--get-ptp-status"
    
    if ($ptpStatus -match "Locked" -or $ptpStatus -match "Synchronized") {
        Write-Host ""
        Write-Host "      ? PTP LOCKED! Synchronization successful!" -ForegroundColor Green
        Write-Host ""
        Write-Host "      Final PTP Status:" -ForegroundColor Cyan
        $ptpStatus | ForEach-Object { Write-Host "        $_" -ForegroundColor White }
        
        # Get offset metrics
        Write-Host ""
        Write-Host "      PTP Offset Metrics:" -ForegroundColor Cyan
        $offsetMetrics = Invoke-PtcTool "--get-ptp-offset"
        $offsetMetrics | ForEach-Object { Write-Host "        $_" -ForegroundColor White }
        
        Write-Host ""
        Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Green
        Write-Host " ? Configuration Complete!" -ForegroundColor Green
        Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Green
        exit 0
    }
    
    Write-Host "      Attempt $i/15: Waiting for PTP lock..." -ForegroundColor Gray
    Start-Sleep -Seconds 2
}

Write-Host ""
Write-Host "      ??  PTP not locked after 30 seconds" -ForegroundColor Yellow
Write-Host ""
Write-Host "      Current PTP Status:" -ForegroundColor Cyan
$ptpStatus | ForEach-Object { Write-Host "        $_" -ForegroundColor White }
Write-Host ""
Write-Host "Possible issues:" -ForegroundColor Yellow
Write-Host "  1. Network switch doesn't support multicast/IGMP" -ForegroundColor Yellow
Write-Host "  2. Firewall blocking UDP ports 319, 320" -ForegroundColor Yellow
Write-Host "  3. OXTS PTP not enabled or different domain" -ForegroundColor Yellow
Write-Host "  4. Network congestion or excessive delay" -ForegroundColor Yellow
Write-Host ""
Write-Host "Try:" -ForegroundColor Cyan
Write-Host "  - Wait another 30-60 seconds for sync to establish" -ForegroundColor Cyan
Write-Host "  - Use Wireshark to verify PTP packets on network" -ForegroundColor Cyan
Write-Host "  - Check OXTS PTP status in CLEVIR application" -ForegroundColor Cyan
Write-Host ""
exit 1
