# ?? Phase 3: Hesai Pandar128E3X PTP Configuration

## ? Prerequisites Met

```
? Network: All devices on 10.5.55.x subnet
? OXTS: PTP Status = LOCKED (Master)
? OXTS IP: 10.5.55.200
? LiDAR IP: 10.5.55.14
? PC IP: 10.5.55.201
? ptc_tool: Located and ready
```

---

## ??? Step 1: Extract Current LiDAR Configuration

### Open PowerShell and navigate to ptc_tool directory:

```powershell
cd "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\HesaiLidar_SDK_2.0-master\tool_ptc\out\build\x64-Debug\Debug"
```

### ?? IMPORTANT: ptc_tool Syntax

The `ptc_tool` requires **two arguments**: `<device_ip> <ptc_port>`

```
Usage: ptc_tool.exe <device_ip_address> <ptc_port>
Default PTC Port: 9347
```

### Get full LiDAR configuration:

```powershell
.\ptc_tool.exe 10.5.55.14 9347 --get-config > C:\DEV\CLEVIR\hesai_config_before.txt
```

### Check current PTP status:

```powershell
.\ptc_tool.exe 10.5.55.14 9347 --get-ptp-status
```

**Expected Output (before configuration):**
```
PTP Status: Disabled
PTP Mode: 0 (Disabled)
PTP Profile: Default
Master IP: Not configured
Sync Status: No synchronization
```

### View current PTP configuration:

```powershell
.\ptc_tool.exe 10.5.55.14 9347 --get-ptp-config
```

---

## ?? Step 2: Configure LiDAR as PTP Slave

### Enable PTP and set OXTS as master:

```powershell
# Enable PTP slave mode
.\ptc_tool.exe 10.5.55.14 9347 --set-ptp-enable 1

# Set PTP domain (usually 0, but check OXTS configuration)
.\ptc_tool.exe 10.5.55.14 9347 --set-ptp-domain 0

# Set PTP profile to IEEE 1588-2008 (PTPv2)
.\ptc_tool.exe 10.5.55.14 9347 --set-ptp-profile 0

# Optional: Set expected master IP (not always required for multicast)
.\ptc_tool.exe 10.5.55.14 9347 --set-ptp-master-ip 10.5.55.200
```

### Save configuration to LiDAR flash:

```powershell
.\ptc_tool.exe 10.5.55.14 9347 --save-config
```

### Reboot LiDAR to apply changes:

```powershell
.\ptc_tool.exe 10.5.55.14 9347 --reboot
```

**? Wait 30-60 seconds for LiDAR to reboot**

---

## ?? Step 3: Verify PTP Synchronization

### Check PTP status after reboot:

```powershell
.\ptc_tool.exe 10.5.55.14 9347 --get-ptp-status
```

**Expected Output (after configuration):**
```
PTP Status: Locked ?
PTP Mode: 2 (Slave)
PTP Profile: IEEE 1588-2008
Master IP: 10.5.55.200
Sync Status: Synchronized
Time Offset: < 1 microsecond
Sync Quality: Excellent
```

### Get detailed PTP synchronization metrics:

```powershell
.\ptc_tool.exe 10.5.55.14 9347 --get-ptp-offset
```

**Look for:**
- **Offset from Master**: Should be < 1 탎 (microsecond)
- **Mean Path Delay**: Should be < 100 탎
- **Offset Std Dev**: Should be < 10 탎

---

## ?? Step 4: Monitor PTP Performance

### Continuous monitoring (run in separate PowerShell window):

```powershell
while ($true) {
    Clear-Host
    Write-Host "=== Hesai Pandar128E3X PTP Status ===" -ForegroundColor Cyan
    Write-Host "Time: $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Green
    Write-Host ""
    
    .\ptc_tool.exe --device-ip 10.5.55.14 --get-ptp-status
    
    Write-Host ""
    Write-Host "--- Press Ctrl+C to stop monitoring ---" -ForegroundColor Yellow
    
    Start-Sleep -Seconds 2
}
```

---

## ?? Step 5: Verify Time Synchronization with OXTS

### In CLEVIR application, check both sources:

```visualbasic
' Test in Immediate Window (Debug mode)
? OxtsNcomInterface.TestOxtsIntegration()

' Check PTP sync quality
? $"OXTS PTP: {OxtsNcomInterface.IsPtpSynchronized()} - Quality: {OxtsNcomInterface.GetPtpSyncQuality()}%"

' Compare timestamps
Dim oxtsTime = OxtsNcomInterface.GetSynchronizedTimestamp()
Dim lidarTime = ' Extract from LiDAR point cloud timestamp
Dim offset = (oxtsTime - lidarTime).TotalMicroseconds

Console.WriteLine($"Time Offset: {offset:F2} 탎")
```

**Target Performance:**
- ? Time offset < 1 microsecond
- ? Sync quality > 95%
- ? No sync lost messages

---

## ?? Troubleshooting

### Issue 1: "PTP Status: No Sync Packets Received"

**Cause**: PTP multicast not reaching LiDAR

**Solution**:
1. Check network switch supports multicast (IGMP)
2. Verify no firewall blocking UDP ports 319, 320
3. Use Wireshark to confirm PTP packets on network:
   ```
   Filter: ptp
   Expected: Sync, Follow_Up, Announce messages from 10.5.55.200
   ```

### Issue 2: "PTP Offset > 10 microseconds"

**Cause**: Network congestion or switch delay

**Solution**:
1. Use a **managed switch** with PTP support (IEEE 1588 boundary clock)
2. Enable QoS (Quality of Service) for PTP traffic priority
3. Reduce network traffic on switch during operation

### Issue 3: "Master IP shows 0.0.0.0"

**Cause**: LiDAR receiving multicast but not identifying master

**Solution**:
1. Explicitly set master IP:
   ```powershell
   .\ptc_tool.exe --device-ip 10.5.55.14 --set-ptp-master-ip 10.5.55.200
   ```
2. Verify OXTS PTP domain matches LiDAR (usually domain 0)

### Issue 4: "ptc_tool command not found"

**Cause**: Not in correct directory or path issue

**Solution**:
```powershell
# Use full path
& "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\HesaiLidar_SDK_2.0-master\tool_ptc\out\build\x64-Debug\Debug\ptc_tool.exe" --device-ip 10.5.55.14 --get-ptp-status
```

---

## ?? ptc_tool Quick Reference

### Configuration Commands
```powershell
# Get all configuration
--get-config

# PTP Enable/Disable
--set-ptp-enable [0|1]

# PTP Domain (0-127)
--set-ptp-domain [0-127]

# PTP Profile
--set-ptp-profile [0|1|2]
  0 = IEEE 1588-2008 (Default, recommended)
  1 = IEEE 1588-2019
  2 = IEEE 802.1AS (gPTP)

# PTP Master IP
--set-ptp-master-ip [IP_ADDRESS]

# Network Settings
--set-ip [NEW_IP]
--set-netmask [NETMASK]
--set-gateway [GATEWAY]

# Save & Reboot
--save-config
--reboot
```

### Status/Query Commands
```powershell
# PTP Status
--get-ptp-status
--get-ptp-config
--get-ptp-offset

# Device Info
--get-device-info
--get-firmware-version

# Network Info
--get-network-config
```

---

## ? Success Criteria

After completing Phase 3, you should see:

### OXTS Status
```
PTP Status: ? LOCKED (Master)
Timing Source: ??? Primary GNSS
Slaves Detected: 1 (Hesai LiDAR)
Sync Quality: 100%
```

### Hesai LiDAR Status
```
PTP Status: ? LOCKED (Slave)
Master IP: 10.5.55.200
Time Offset: 0.23 탎
Mean Path Delay: 45 탎
Sync Quality: Excellent
```

### Point Cloud Timestamps
```
OXTS GPS Time:    2025-12-07 17:30:45.123456789 UTC
LiDAR Timestamp:  2025-12-07 17:30:45.123457012 UTC
Offset:           0.223 microseconds ?

?? Perfect synchronization for georeferenced LiDAR!
```

---

## ?? Final Validation

### Test with Real Data Collection

1. **Start data collection** in CLEVIR
2. **Capture 10 seconds** of synchronized data
3. **Export point cloud** with timestamps
4. **Compare OXTS and LiDAR timestamps** for same physical feature
5. **Verify spatial accuracy** matches expected RTK precision

**Expected Results:**
- Positional accuracy: �2 cm (RTK + LiDAR accuracy)
- Temporal accuracy: �1 탎 (PTP synchronization)
- No drift over time
- Stable synchronization across reboots

---

## ?? Reference Documents

- **Hesai Pandar128E3X Manual**: PTP configuration section
- **OXTS NCOM Manual**: Rev 250811, Status Channel 23 (PTP)
- **IEEE 1588-2008**: Precision Time Protocol specification
- **Your Files**:
  - `PTP_INTEGRATION_PHASE1.md` - Monitoring implementation
  - `OFFICIAL_NCOM_DECODER.md` - NCOM packet structure
  - `OxtsStatusChannelDecoder.vb` - PTP status decoder

---

## ?? Next Steps After Phase 3

1. **Integrate PTP status in CLEVIR UI**
   - Add PTP sync indicator to main form
   - Show real-time sync quality gauge
   - Alert on sync loss

2. **Log PTP Performance**
   - Record sync quality over time
   - Track offset drift
   - Generate sync quality reports

3. **Automated Validation**
   - Compare LiDAR/OXTS timestamps on startup
   - Alert if offset exceeds threshold
   - Auto-reconfigure if sync lost

---

*Last Updated: 2025-12-07*  
*Status: Ready for Phase 3 Implementation* ??
