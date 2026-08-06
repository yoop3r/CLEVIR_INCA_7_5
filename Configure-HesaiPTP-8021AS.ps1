# Hesai Pandar128E3X gPTP (IEEE 802.1AS) Configuration Script
# Configures LiDAR to sync with the TM2000B TimeMachine grandmaster via the
# Cisco C9300L TSN boundary clock, using the IEEE 802.1AS (gPTP) profile.
#
# NOTE: 802.1AS requires every hop in the path (TM2000B -> Cisco C9300L -> LiDAR)
# to be running the SAME profile. If the LiDAR shows "Frozen" instead of "Locked",
# the most common cause is the switch (or TM2000B) still running the Default
# IEEE 1588-2008 profile instead of gPTP. See CISCO_PTP.md for the switch-side
# configuration required before running this script.

param(
	[string]$LidarIP = "10.5.55.14",
	[string]$Tm2000bIP = "192.168.10.20",
	[int]$PtpDomain = 0,
	[int]$PtcPort = 9347,
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
Write-Host " Hesai Pandar128E3X gPTP (IEEE 802.1AS) Configuration Tool" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????????????????????" -ForegroundColor Cyan
Write-Host ""
Write-Host "Configuration:" -ForegroundColor Green
Write-Host "  LiDAR IP:        $LidarIP"
Write-Host "  PTC Port:        $PtcPort"
Write-Host "  TM2000B IP:      $Tm2000bIP  (grandmaster, via Cisco C9300L boundary clock)"
Write-Host "  PTP Domain:      $PtpDomain"
Write-Host "  PTP Profile:     IEEE 802.1AS (gPTP)"
Write-Host "  Network Transport: L2 (Ethernet, required by 802.1AS)"
Write-Host ""
Write-Host "??  Prerequisite: Cisco C9300L must be configured for the gPTP profile" -ForegroundColor Yellow
Write-Host "   on the VLAN20 LiDAR-facing ports, and the TM2000B must be set to" -ForegroundColor Yellow
Write-Host "   802.1AS mode. See CISCO_PTP.md before running this script." -ForegroundColor Yellow
Write-Host ""

# Function to execute ptc_tool command
function Invoke-PtcTool {
	param([string]$Arguments)

	# Always include LidarIP and PtcPort
	$fullArgs = "$LidarIP $PtcPort $Arguments"
	$output = & $ptcTool $fullArgs 2>&1
	return $output
}

# Step 1: Backup current configuration
if (-not $SkipBackup) {
	Write-Host "[1/6] Backing up current configuration..." -ForegroundColor Yellow
	$backupFile = "C:\DEV\CLEVIR\hesai_config_backup_8021as_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"

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

# Step 3: Configure PTP for 802.1AS
Write-Host "[3/6] Configuring gPTP (802.1AS) settings..." -ForegroundColor Yellow

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

Write-Host "      Setting PTP profile to IEEE 802.1AS (gPTP)..." -NoNewline
$result = Invoke-PtcTool "--set-ptp-profile 2"
if ($LASTEXITCODE -eq 0) {
	Write-Host " ?" -ForegroundColor Green
} else {
	Write-Host " ? Failed" -ForegroundColor Red
	Write-Host "      Error: $result" -ForegroundColor Red
}

# 802.1AS mandates the peer-to-peer (P2P) delay mechanism and Layer 2 transport.
# Some firmwares expose these as separate switches; attempt to set them and warn
# (rather than fail hard) if the option does not exist on this firmware version.
Write-Host "      Setting PTP transport to L2 (required by 802.1AS)..." -NoNewline
$result = Invoke-PtcTool "--set-ptp-transport L2"
if ($LASTEXITCODE -eq 0) {
	Write-Host " ?" -ForegroundColor Green
} else {
	Write-Host " ??  Not supported by this firmware / already fixed to L2 for 802.1AS" -ForegroundColor Yellow
}

Write-Host "      Setting PTP delay mechanism to P2P (required by 802.1AS)..." -NoNewline
$result = Invoke-PtcTool "--set-ptp-delay-mechanism P2P"
if ($LASTEXITCODE -eq 0) {
	Write-Host " ?" -ForegroundColor Green
} else {
	Write-Host " ??  Not supported by this firmware / already fixed to P2P for 802.1AS" -ForegroundColor Yellow
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

# Step 6: Verify gPTP synchronization
Write-Host "[6/6] Verifying gPTP (802.1AS) synchronization..." -ForegroundColor Yellow
Write-Host "      Checking PTP status (may take 10-15 seconds to lock)..." -ForegroundColor Gray

for ($i = 1; $i -le 15; $i++) {
	$ptpStatus = Invoke-PtcTool "--get-ptp-status"

	if ($ptpStatus -match "Locked" -or $ptpStatus -match "Synchronized") {
		Write-Host ""
		Write-Host "      ? PTP LOCKED! gPTP synchronization successful!" -ForegroundColor Green
		Write-Host ""
		Write-Host "      Final PTP Status:" -ForegroundColor Cyan
		$ptpStatus | ForEach-Object { Write-Host "        $_" -ForegroundColor White }

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

	if ($ptpStatus -match "Frozen") {
		Write-Host ""
		Write-Host "      ??  PTP status: FROZEN - LiDAR previously locked but is no" -ForegroundColor Yellow
		Write-Host "         longer receiving valid 802.1AS Sync/Announce traffic." -ForegroundColor Yellow
		Write-Host "         This almost always means an upstream hop (Cisco C9300L" -ForegroundColor Yellow
		Write-Host "         or TM2000B) is not actually running the gPTP profile." -ForegroundColor Yellow
		Write-Host "         See CISCO_PTP.md for switch-side verification commands" -ForegroundColor Yellow
		Write-Host "         ('show ptp clock', 'show ptp port')." -ForegroundColor Yellow
	}

	Write-Host "      Attempt $i/15: Waiting for gPTP lock..." -ForegroundColor Gray
	Start-Sleep -Seconds 2
}

Write-Host ""
Write-Host "      ??  PTP not locked after 30 seconds" -ForegroundColor Yellow
Write-Host ""
Write-Host "      Current PTP Status:" -ForegroundColor Cyan
$ptpStatus | ForEach-Object { Write-Host "        $_" -ForegroundColor White }
Write-Host ""
Write-Host "Possible issues (802.1AS-specific):" -ForegroundColor Yellow
Write-Host "  1. Cisco C9300L is running Default Profile (IEEE1588) instead of gPTP" -ForegroundColor Yellow
Write-Host "     on the VLAN20 LiDAR-facing port(s) - verify with 'show ptp port'" -ForegroundColor Yellow
Write-Host "  2. TM2000B is not actually set to 802.1AS mode" -ForegroundColor Yellow
Write-Host "  3. 802.1AS requires Peer-to-Peer (P2P) delay mechanism; Default Profile" -ForegroundColor Yellow
Write-Host "     E2E delay requests are ignored by 802.1AS-only devices" -ForegroundColor Yellow
Write-Host "  4. PTP domain mismatch between LiDAR, switch, and TM2000B (domain $PtpDomain used here)" -ForegroundColor Yellow
Write-Host "  5. LiDAR firmware may not support 802.1AS transport/delay-mechanism overrides" -ForegroundColor Yellow
Write-Host "     used in this script; consult PandarView for manual verification" -ForegroundColor Yellow
Write-Host ""
Write-Host "Try:" -ForegroundColor Cyan
Write-Host "  - Run 'show ptp clock' and 'show ptp port' on the C9300L to confirm gPTP profile" -ForegroundColor Cyan
Write-Host "  - Use Wireshark to verify Pdelay_Req/Pdelay_Resp (not Delay_Req/Delay_Resp) on the wire" -ForegroundColor Cyan
Write-Host "  - Check TM2000B web UI PTP profile setting matches 802.1AS" -ForegroundColor Cyan
Write-Host ""
exit 1
