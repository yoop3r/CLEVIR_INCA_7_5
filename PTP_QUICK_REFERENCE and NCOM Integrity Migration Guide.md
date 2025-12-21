# ?? PTP Configuration - Quick Reference Card

## ? Your System Status

```
? OXTS RT3000: PTP LOCKED (Master mode)
? Network: All on 10.5.55.x subnet
? Software: PandarView installed
? LiDAR: Ready for PTP configuration
```

---

## ?? 5-Minute Configuration (PandarView)

### 1. Connect
```
Open PandarView
? IP: 10.5.55.14
? Port: 9347
? Click "Connect"
```

### 2. Configure
```
Settings ? Time Sync ? PTP
? Enable: YES
? Mode: SLAVE
? Domain: 0
? Master IP: 10.5.55.200
? Profile: IEEE 1588-2008
```

### 3. Apply
```
Click "Apply" ? Click "Reboot"
Wait 60 seconds
```

### 4. Verify
```
PTP Status: Synchronized ?
Offset: < 1 µs ?
```

---

## ? Success Criteria

| Check | Target | Status |
|-------|--------|--------|
| **PandarView Connection** | Connected | ? |
| **PTP Status** | Synchronized | ? |
| **Offset from Master** | < 1 microsecond | ? |
| **Mean Path Delay** | < 100 microseconds | ? |
| **Clock Quality** | Good or Excellent | ? |
| **OXTS PTP Status** | LOCKED | ? |
| **CLEVIR Status** | Receiving NCOM | ? |

---

## ?? Detailed Guides

| Guide | Purpose | When to Use |
|-------|---------|-------------|
| **PandarView_PTP_Configuration_Guide.md** | Complete step-by-step with screenshots | Primary guide |
| **PTP_CONFIGURATION_QUICK_START.md** | Quick reference for all methods | Alternative methods |
| **PTP_INTEGRATION_COMPLETE_SUMMARY.md** | Full project documentation | Technical details |

---

## ?? Troubleshooting

### Cannot connect to LiDAR
```powershell
ping 10.5.55.14
```
- ? Reply: LiDAR is online
- ? Timeout: Check power and cable

### PTP stuck on "Listening"
1. Verify OXTS PTP: Check CLEVIR console
2. Check network: Try direct cable (no switch)
3. Restart both devices: OXTS first, then LiDAR

### High offset (> 10 µs)
1. Check network congestion
2. Use Cat6 cables
3. Minimize network traffic during operation

### Issue 1: "Integrity always 0%"

' Symptom: PacketIntegrityPercent = 0, TotalPacketsReceived = 0
' Cause: Listener not receiving data

' Debug steps:
1. Check firewall: Allow UDP 3000
2. Verify network adapter: Should be on 10.5.55.x subnet
3. Check OXTS config: UDP output enabled?
4. Use Wireshark: Capture UDP port 3000, look for 0xE7 sync bytes

### Issue 2: "Corruption suddenly jumps to 100%"

' Symptom: CorruptionPercent spikes from 0% to 100%
' Cause: Network cable disconnected or OXTS powered off
' Graceful handling:
    Private Sub MonitorCommunication()
        If (DateTime.UtcNow - _oxtsInterface.LastPacketTime).TotalSeconds > 5 Then
            ' No data for 5 seconds - device offline
            UpdateStatus("OXTS: NO COMMS", Color.Red)
        ElseIf _oxtsInterface.PacketCorruptionPercent > 50 Then
            ' Data arriving but corrupted - cable/network issue
            UpdateStatus("OXTS: CORRUPTED DATA", Color.Red)
        End If
    End Sub

### Issue 3: "Checksum failures but OXTS NAVdisplay looks fine"

 Possible causes:
 1. OXTS sending Structure-B packets (internal use, should be filtered)
    → Check: _stats.StructureBPackets count
    → Solution: Disable with command "-udp_ncomx_0"

 2. Multiple applications listening on same port (port sharing issues)
    → Check: netstat -an | findstr ":3000"
    → Solution: Ensure exclusive port use  
 3. Firmware version mismatch
    → Check: Status Channel 19 (software version)
    → Solution: Update to latest firmware

---
### Key Properties

' Integrity Metrics
oxts.PacketIntegrityPercent    ' 0-100%, goal: >99%
oxts.PacketCorruptionPercent   ' 0-100%, goal: <1%
oxts.TotalPacketsReceived      ' Absolute count
oxts.ValidPacketsReceived      ' Packets that passed checksums

### Key Methods

' Status Checks
oxts.IsGpsLocked              ' GPS solution valid
oxts.IsPtpSynchronized()      ' PTP clock synchronized
oxts.IsRealtime()             ' Data received within 2 second

' Monitoring
oxts.ReportPacketIntegrity()  ' Detailed diagnostics to log
oxts.ResetIntegrityStats()    ' Clear counters for fresh baseline

' Testing
oxts.TestOxtsIntegration()    ' Comprehensive system test

' Data Access
Dim pos = oxts.GetGpsPosition()  ' Returns OxtsPosition? (null if stale)
Dim pos2 = oxts.GetCurrentPosition() ' Returns OxtsPosition (never null, may be stale)

## ?? Quick Support

**Hesai**: Check PandarView help menu  
**OXTS**: support@oxts.com  
**CLEVIR**: `OxtsNcomInterface.TestOxtsIntegration()`

---

## ?? Ready to Start?

**Open**: `PandarView_PTP_Configuration_Guide.md`

**Time Required**: 5 minutes

**Difficulty**: Easy ?

**Success Rate**: 95%+ with PandarView

---

*Let's synchronize your system!* ????
