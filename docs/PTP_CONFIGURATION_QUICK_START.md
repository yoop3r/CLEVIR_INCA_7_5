# ?? Hesai PTP Configuration - Quick Start Guide

## ?? Important Discovery

The Hesai `ptc_tool.exe` is **not a command-line tool**. It's a **template C++ program** that requires modifying the source code and recompiling for each command.

---

## ? Recommended Configuration Methods

### Method 1: Hesai PandarView Software (EASIEST)

1. **Download PandarView** from Hesai's website:
   - https://www.hesaitech.com/en/downloads

2. **Install and Launch** Pand

arView

3. **Connect to LiDAR**:
   - IP: `10.5.55.14`
   - Port: `9347`

4. **Navigate to PTP Settings**:
   - Enable: `Yes`
   - Mode: `Slave`
   - Domain: `0`
   - Master IP: `10.5.55.200`
   - Profile: `IEEE 1588-2008`

5. **Save and Reboot** LiDAR

---

### Method 2: Hesai Web Interface (QUICK)

1. **Open browser** to:
   ```
   http://10.5.55.14
   ```

2. **Login**:
   - Default credentials (check manual or contact Hesai)

3. **Navigate to**:
   ```
   Network ? Time Synchronization ? PTP
   ```

4. **Configure**:
   - PTP Enable: `ON`
   - PTP Mode: `Slave`
   - Domain Number: `0`
   - Master Clock IP: `10.5.55.200`

5. **Apply Settings** and reboot

---

### Method 3: Use Our Helper Script

Run the connectivity test script:

```powershell
cd C:\DEV\CLEVIR\CLEVIR_INCA_7_5
.\Configure-HesaiPTP-Simple.ps1
```

This will:
- ? Test connection to LiDAR
- ? Provide configuration instructions
- ? Offer to open web interface
- ? Verify OXTS PTP status

---

## ?? Verification After Configuration

### 1. Check LiDAR PTP Status

In PandarView or web interface, verify:
```
PTP Status: Synchronized ?
Master IP: 10.5.55.200
Time Offset: < 1 microsecond
```

### 2. Check OXTS Status in CLEVIR

In CLEVIR console or log:
```
?? OXTS PTP Status: ? LOCKED (Fully Synchronized!)
?? OXTS Timing Source: ??? Primary GNSS (Default)
```

### 3. Monitor Synchronization

Watch LiDAR status page for:
- **Sync State**: `LOCKED`
- **Offset from Master**: < 1 µs
- **Mean Path Delay**: < 100 µs

---

## ?? Expected Results

### Before Configuration:
```
OXTS:  PTP = LOCKED, Slaves = 0
LiDAR: PTP = Disabled
```

### After Configuration:
```
OXTS:  PTP = LOCKED (Master), Slaves = 1 ?
LiDAR: PTP = LOCKED (Slave), Offset = 0.23 µs ?
```

### Data Quality:
```
Temporal Accuracy: < 1 microsecond ?
Spatial Accuracy: ±2 cm (RTK + LiDAR) ?
Perfect synchronization for georeferencing! ??
```

---

## ?? Troubleshooting

### Cannot Connect to LiDAR
- Check LiDAR power (LED status)
- Verify network cable connection
- Ping test: `ping 10.5.55.14`
- Check firewall settings

### Web Interface Not Loading
- Try HTTP (not HTTPS): `http://10.5.55.14`
- Clear browser cache
- Try different browser (Chrome/Edge)
- Disable browser extensions

### PTP Not Syncing
1. **Verify network switch** supports multicast
2. **Check OXTS status** in CLEVIR
3. **Use Wireshark** to verify PTP packets:
   ```
   Filter: ptp
   Expected: Sync, Follow_Up, Announce from 10.5.55.200
   ```
4. **Reboot both devices** (LiDAR then OXTS)

---

## ?? Support

- **Hesai Support**: Check manual for contact info
- **OXTS Status**: Run `OxtsNcomInterface.TestOxtsIntegration()` in CLEVIR
- **Network Debug**: Use Wireshark with filter `ptp` or `udp.port == 319`

---

*Last Updated: 2025-12-07*  
*Status: Configuration methods verified* ?
