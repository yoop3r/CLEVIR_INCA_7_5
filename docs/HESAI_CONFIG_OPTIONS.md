# ?? Finding Hesai LiDAR Configuration Interface

## Current Situation

**PandarView 2** you're using is a **playback/visualization tool**, not a configuration tool. It's showing an old PCAP recording from 2023.

---

## ? Confirmed LiDAR Settings

From your PandarView connection dialog:
```
LiDAR IP:           10.5.55.14
UDP Data Port:      2311 (point cloud stream)
PTC Config Port:    9347 ? (CONFIRMED!)
Fault Port:         2368
Multicast IP:       239.192.20.10
```

**PTC Port 9347 is confirmed** - this is the configuration/command port!

---

## ? Option 1: Web Interface (FASTEST - Try This NOW!)

The LiDAR's actual IP should have a web interface:

### Open Browser:
```
http://10.5.55.14
```

**Try this immediately!** Most Hesai LiDARs have a built-in web UI.

### If Web Interface Loads:

1. **Login** with default credentials:
   - Username: `admin`
   - Password: `admin` or `hesai`

2. **Navigate to PTP Settings**:
   ```
   Network ? Time Synchronization ? PTP
   ```

3. **Configure**:
   - Enable PTP: ? **Yes**
   - Mode: **Slave**
   - Domain: **0**
   - Master IP: **10.5.55.200**
   - Profile: **IEEE 1588-2008**

4. **Save and Reboot**

**Done in 2 minutes!** ?

---

## ? Option 2: Test PTC Port (If No Web UI)

If web interface doesn't exist, we'll use PTC protocol.

### Run Connectivity Test:
```powershell
cd C:\DEV\CLEVIR\CLEVIR_INCA_7_5
.\Test-HesaiPTC.ps1
```

This will:
- ? Test network connection
- ? Verify PTC port 9347 is open
- ? Attempt to open web interface
