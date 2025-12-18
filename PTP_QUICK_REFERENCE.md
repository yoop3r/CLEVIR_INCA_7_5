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

---

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
