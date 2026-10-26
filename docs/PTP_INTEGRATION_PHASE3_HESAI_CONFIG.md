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
- **Offset from Master**: Should be < 1 ?s (microsecond)
- **Mean Path Delay**: Should be < 100 ?s
- **Offset Std Dev**: Should be < 10 ?s

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

Console.WriteLine($"Time Offset: {offset:F2} ?s")
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
Time Offset: 0.23 ?s
Mean Path Delay: 45 ?s
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
- Positional accuracy: ?2 cm (RTK + LiDAR accuracy)
- Temporal accuracy: ?1 ?s (PTP synchronization)
- No drift over time
- Stable synchronization across reboots

---

## PTP Profile Notes - TM2000B / Cisco C9300L Timing Chain

**Final verified configuration for this switch (`FMVSS127_switch`,
C9300L-48T-4X, IOS-XE 17.6.4)**:

- **Both Hesai LiDARs: `Profile = IEEE1588`** (Default Profile). This is
  required on this switch — setting a LiDAR's own profile to `802.1AS` causes
  it to go `Frozen`, then `Free Run` after a reboot; it never (re-)acquires
  sync through this switch.
- **TM2000B: `Profile = IEEE1588`** (`Packet Output = IPv4 UDP`, `Delay
  Mechanism = End to End`, `Transmission Method = Multicast`). **Do not set
  the TM2000B to `802.1AS`.** An earlier finding claimed `802.1AS` gave the
  tightest lock; this was **disproven** — see the "ES886 masquerade" incident
  in `CISCO_PTP.md`. In `802.1AS` mode the TM2000B transmits native Layer-2
  gPTP frames this switch's `udp-ipv4`-only PTP engine cannot receive at all,
  so the switch silently locks to whatever else is reachable over
  `udp-ipv4` instead — in this environment, the **ETAS ES886**, which
  free-runs to master when no other master is heard. The earlier "excellent
  lock" was the LiDARs/switch locking to the ES886, not the TM2000B.
- The LiDAR's own profile does **not** need to match the TM2000B's profile —
  the switch's boundary clock normalizes/re-originates PTP for the
  LiDAR-facing ports regardless of the TM2000B's upstream profile (when it
  can actually receive it).

**Do not use `Configure-HesaiPTP-8021AS.ps1` against these LiDARs on this
switch.** Use the existing `Configure-HesaiPTP.ps1` (IEEE1588) script for
both LiDARs; only the TM2000B's own profile setting should be `802.1AS`.

### Why a LiDAR set to 802.1AS cannot sync through this switch

This switch's PTP transport is hard-locked to `udp-ipv4` — confirmed via
`ptp transport ?`, which only offers `ipv4` as an option (no pure-L2/Ethernet
transport exists on this platform/software combination). There is also no
`ptp profile 802.1as` global command; the closest related feature,
`ptp dot1as extend property <WORD>`, requires a specific property name that
was not identified via CLI help and was **not applied** to the live switch to
avoid risking an unverified change.

`ptp mode boundary` terminates and re-originates PTP messages on every port
in the switch's own profile (Default Profile / IEEE 1588-2008 over
`udp-ipv4`), regardless of what is attached on either side. A LiDAR
configured for IEEE1588 accepts this normalized traffic without issue — this
is why LiDAR=IEEE1588 locks regardless of the TM2000B's profile. A LiDAR
configured for genuine 802.1AS expects raw L2 Ethernet gPTP frames (multicast
MAC `01:80:C2:00:00:0E`, ethertype `0x88F7`, no IP/UDP header) — traffic this
switch's boundary clock cannot produce, since it has no native L2/802.1AS
transport. This is why a LiDAR set to 802.1AS goes Frozen then Free Run: it
never receives anything it recognizes as valid 802.1AS traffic.

Achieving genuine end-to-end 802.1AS (a LiDAR itself reporting locked under
the 802.1AS profile) would require either a switch with true L2-native gPTP
boundary/transparent clock support, or removing this switch from the timing
path entirely for that LiDAR — both larger changes, out of scope unless
specifically required later.

### Switch-side prerequisites (recap; see `CISCO_PTP.md` for full detail)

1. Confirm the correct physical interface names first via `show switch` and
   `show interfaces status | include LIDAR|TIMEMACHINE` — do not assume
   interface numbering matches device port labels (a phantom stack-member-1
   interface can silently accept config with zero real effect on this
   platform).
2. Apply `ptp enable` to each real LiDAR port and the TM2000B-facing port;
   verify with `show ptp port <interface>` (this command does not show up
   via `show run interface`).
3. Confirm PTP domain number matches across LiDAR, switch, and TM2000B
   (CLEVIR default: `0`).

### Expected Output (final working configuration)

```
PTP Status: Locked
PTP Profile: IEEE 1588-2008
Master IP/Identity: TM2000B grandmaster clock identity (via switch boundary clock)
Sync Status: Synchronized
Time Offset: single-digit nanoseconds when TM2000B Profile = 802.1AS
			 (looser, double/triple-digit ns, when TM2000B Profile = IEEE1588)
```

### If a LiDAR shows "Frozen" or "Free Run"

- First confirm `ptp enable` is actually applied to the correct, real
  physical interface — verify via `show switch` and `show interfaces status`
  before assuming a profile issue.
- Confirm the LiDAR's own `Profile` is set to `IEEE1588`, not `802.1AS` — on
  this switch, `802.1AS` on the LiDAR itself will not sync.
- Re-check `show ptp clock` / `show ptp port <interface>` on the C9300L for
  port state (`MASTER`/`SLAVE`/`FAULTY`) and `show ptp parent` for the
  grandmaster identity (should match the TM2000B, not the switch's own
  identity).
- If the grandmaster identity is already correct (TM2000B, `Steps Removed:
  1`+) but the LiDARs are still Free Run, check the global delay mechanism:
  `show ptp port <interface>` → `Delay Mechanism` must be `End to End`
  (`ptp mode boundary delay-req`), not `Peer to Peer` (`pdelay-req`). This has
  been observed to drift on its own and causes the TM2000B-facing port to
  hang at `Port state: UNCALIBRATED` with `Peer mean path delay(ns): 0`. Fix
  with `no ptp mode` then `ptp mode boundary delay-req` (delay mechanism
  cannot be changed while `ptp mode` is already configured).
- Always run `copy running-config startup-config` after any working PTP fix
  — an unsaved config fully reverts on the next switch reboot/power-cycle,
  which has caused both `ptp enable` loss and delay-mechanism drift to recur
  in this environment. See `CISCO_PTP.md` "Incident" section for the full
  writeup.

---
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
