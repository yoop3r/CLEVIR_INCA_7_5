<#
.SYNOPSIS
    Test script for OXTS NCOM SharpPcap migration validation

.DESCRIPTION
    Validates OXTS PCAP capture functionality after SharpPcap migration:
    - Verifies PCAP file creation
    - Checks packet counts
    - Validates marker injection
    - Analyzes PCAP with tshark (if available)

.PARAMETER PcapPath
    Path to the OXTS PCAP file to analyze

.EXAMPLE
    .\Test-OxtsCapture.ps1 -PcapPath "C:\Data\Recording_01_OXTS.pcap"
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$PcapPath,
    
    [Parameter(Mandatory=$false)]
    [string]$TsharkPath = "C:\Program Files\Wireshark\tshark.exe"
)

Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  OXTS NCOM SharpPcap Migration - Validation Script" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Function to format file size
function Format-FileSize {
    param([long]$Size)
    if ($Size -lt 1KB) { return "$Size B" }
    if ($Size -lt 1MB) { return "{0:N2} KB" -f ($Size / 1KB) }
    if ($Size -lt 1GB) { return "{0:N2} MB" -f ($Size / 1MB) }
    return "{0:N2} GB" -f ($Size / 1GB)
}

# Step 1: Check if PCAP path provided
if (-not $PcapPath) {
    Write-Host "⚠️  No PCAP path specified. Searching for latest OXTS capture..." -ForegroundColor Yellow
    
    $dataPath = "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\Data"
    if (Test-Path $dataPath) {
        $latestPcap = Get-ChildItem -Path $dataPath -Filter "*_OXTS.pcap" -Recurse | 
                      Sort-Object LastWriteTime -Descending | 
                      Select-Object -First 1
        
        if ($latestPcap) {
            $PcapPath = $latestPcap.FullName
            Write-Host "✅ Found: $($latestPcap.Name)" -ForegroundColor Green
        } else {
            Write-Host "❌ No OXTS PCAP files found in $dataPath" -ForegroundColor Red
            Write-Host ""
            Write-Host "Usage: .\Test-OxtsCapture.ps1 -PcapPath <path_to_pcap>" -ForegroundColor Yellow
            exit 1
        }
    }
}

# Step 2: Validate PCAP file exists
if (-not (Test-Path $PcapPath)) {
    Write-Host "❌ PCAP file not found: $PcapPath" -ForegroundColor Red
    exit 1
}

$pcapFile = Get-Item $PcapPath
Write-Host ""
Write-Host "───────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host "  File Information" -ForegroundColor White
Write-Host "───────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host "File:         $($pcapFile.Name)" -ForegroundColor Cyan
Write-Host "Path:         $($pcapFile.DirectoryName)" -ForegroundColor Cyan
Write-Host "Size:         $(Format-FileSize $pcapFile.Length)" -ForegroundColor Cyan
Write-Host "Created:      $($pcapFile.CreationTime)" -ForegroundColor Cyan
Write-Host "Last Write:   $($pcapFile.LastWriteTime)" -ForegroundColor Cyan
Write-Host ""

# Step 3: Check for sidecar event log
$eventLogPath = $PcapPath -replace '\.pcap$', '.oxts_events.txt'
if (Test-Path $eventLogPath) {
    Write-Host "✅ Sidecar event log found: $([System.IO.Path]::GetFileName($eventLogPath))" -ForegroundColor Green
    
    $eventLog = Get-Item $eventLogPath
    Write-Host "   Size: $(Format-FileSize $eventLog.Length)" -ForegroundColor Gray
    
    # Show first few lines
    Write-Host ""
    Write-Host "   Preview (first 10 lines):" -ForegroundColor Gray
    Get-Content $eventLogPath -TotalCount 10 | ForEach-Object {
        Write-Host "   $_" -ForegroundColor DarkGray
    }
    Write-Host ""
} else {
    Write-Host "⚠️  Sidecar event log not found" -ForegroundColor Yellow
    Write-Host ""
}

# Step 4: Analyze with tshark if available
if (Test-Path $TsharkPath) {
    Write-Host "───────────────────────────────────────────────────────────────" -ForegroundColor Gray
    Write-Host "  PCAP Analysis (tshark)" -ForegroundColor White
    Write-Host "───────────────────────────────────────────────────────────────" -ForegroundColor Gray
    
    # Get packet count
    Write-Host "Analyzing packets..." -ForegroundColor Cyan
    $packetCount = & $TsharkPath -r $PcapPath -T fields -e frame.number | Measure-Object -Line
    Write-Host "Total Packets: $($packetCount.Lines)" -ForegroundColor Green
    
    # Get OXTS NCOM packets (UDP port 3000)
    Write-Host ""
    Write-Host "Filtering OXTS NCOM packets (UDP port 3000)..." -ForegroundColor Cyan
    $ncomPackets = & $TsharkPath -r $PcapPath -Y "udp.port == 3000" -T fields -e frame.number | Measure-Object -Line
    Write-Host "NCOM Packets: $($ncomPackets.Lines)" -ForegroundColor Green
    
    # Get event marker packets (UDP port 65002)
    Write-Host ""
    Write-Host "Filtering event markers (UDP port 65002)..." -ForegroundColor Cyan
    $markerPackets = & $TsharkPath -r $PcapPath -Y "udp.port == 65002" -T fields -e frame.number | Measure-Object -Line
    Write-Host "Marker Packets: $($markerPackets.Lines)" -ForegroundColor Green
    
    # Show first NCOM packet details
    if ($ncomPackets.Lines -gt 0) {
        Write-Host ""
        Write-Host "First NCOM Packet Details:" -ForegroundColor Cyan
        & $TsharkPath -r $PcapPath -Y "udp.port == 3000" -c 1 -V | Select-Object -First 30
    }
    
    # Show marker packet payloads
    if ($markerPackets.Lines -gt 0) {
        Write-Host ""
        Write-Host "Event Marker Payloads:" -ForegroundColor Cyan
        & $TsharkPath -r $PcapPath -Y "udp.port == 65002" -T fields -e data.text | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    
} else {
    Write-Host "⚠️  tshark not found at: $TsharkPath" -ForegroundColor Yellow
    Write-Host "   Install Wireshark for detailed packet analysis" -ForegroundColor Gray
    Write-Host ""
}

# Step 5: Validation Summary
Write-Host "───────────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host "  Validation Summary" -ForegroundColor White
Write-Host "───────────────────────────────────────────────────────────────" -ForegroundColor Gray

$checks = @()

# Check 1: File exists and has data
if ($pcapFile.Length -gt 0) {
    $checks += [PSCustomObject]@{
        Check = "PCAP file created"
        Status = "✅ PASS"
        Color = "Green"
    }
} else {
    $checks += [PSCustomObject]@{
        Check = "PCAP file created"
        Status = "❌ FAIL (empty file)"
        Color = "Red"
    }
}

# Check 2: Sidecar log exists
if (Test-Path $eventLogPath) {
    $checks += [PSCustomObject]@{
        Check = "Event log created"
        Status = "✅ PASS"
        Color = "Green"
    }
} else {
    $checks += [PSCustomObject]@{
        Check = "Event log created"
        Status = "⚠️  WARN (missing)"
        Color = "Yellow"
    }
}

# Check 3: NCOM packets captured (if tshark available)
if (Test-Path $TsharkPath) {
    if ($ncomPackets.Lines -gt 0) {
        $checks += [PSCustomObject]@{
            Check = "NCOM packets captured"
            Status = "✅ PASS ($($ncomPackets.Lines) packets)"
            Color = "Green"
        }
    } else {
        $checks += [PSCustomObject]@{
            Check = "NCOM packets captured"
            Status = "❌ FAIL (no packets)"
            Color = "Red"
        }
    }
    
    # Check 4: Marker packets injected
    if ($markerPackets.Lines -gt 0) {
        $checks += [PSCustomObject]@{
            Check = "Event markers injected"
            Status = "✅ PASS ($($markerPackets.Lines) markers)"
            Color = "Green"
        }
    } else {
        $checks += [PSCustomObject]@{
            Check = "Event markers injected"
            Status = "⚠️  WARN (no markers)"
            Color = "Yellow"
        }
    }
}

# Print summary table
$checks | ForEach-Object {
    Write-Host "  $($_.Check.PadRight(30)) $($_.Status)" -ForegroundColor $_.Color
}

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan

# Step 6: Next Steps
$passCount = ($checks | Where-Object { $_.Status -like "*PASS*" }).Count
$totalChecks = $checks.Count

if ($passCount -eq $totalChecks) {
    Write-Host "🎉 All checks passed! Migration validated successfully." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Test with real OXTS NCOM data stream" -ForegroundColor White
    Write-Host "  2. Verify packet integrity in Wireshark" -ForegroundColor White
    Write-Host "  3. Run performance benchmarks (CPU usage)" -ForegroundColor White
    Write-Host "  4. Remove PcapDotNet NuGet packages" -ForegroundColor White
    Write-Host "  5. Reinstall Npcap WITHOUT WinPcap compatibility" -ForegroundColor White
} else {
    Write-Host "⚠️  Some checks failed. Review errors above." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Cyan
    Write-Host "  1. Check OXTS device IP: 10.5.55.200" -ForegroundColor White
    Write-Host "  2. Verify network adapter GUID in config" -ForegroundColor White
    Write-Host "  3. Check Npcap driver loaded: Get-NetAdapter" -ForegroundColor White
    Write-Host "  4. Review application logs for errors" -ForegroundColor White
}

Write-Host ""
