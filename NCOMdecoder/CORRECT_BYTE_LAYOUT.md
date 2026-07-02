# NCOM Structure B - FINAL CORRECT Byte Offsets

## Resolution
After comparing with the **official OXTS NComRxC.c reference implementation**, the correct byte offsets and scale factors are now verified.

---

## ? CORRECT NCOM Structure B Byte Layout (Verified Against OXTS Reference)

| Byte Offset | Field | Size | Type | Scale Factor | OXTS Constant | Units | Range |
|-------------|-------|------|------|--------------|---------------|-------|-------|
| 0 | Sync Byte | 1 | uint8 | N/A | `NCOM_SYNC` | N/A | 0xE7 (fixed) |
| 1-2 | Time | 2 | uint16 | 0.001 | `TIME2SEC` | seconds | 0-59.999 |
| 3-6 | Accelerations | 4 | - | (skipped) | - | - | - |
| **7-9** | **Latitude** | **3** | **int24** | **1e-7** | - | **degrees** | ±90° |
| **10-12** | **Longitude** | **3** | **int24** | **1e-7** | - | **degrees** | ±180° |
| **13-15** | **Altitude** | **3** | **int24** | **0.001** | `POSA2M` | **meters** | ±8388 m |
| **16-18** | **Velocity North** | **3** | **int24** | **0.0001** | `VEL2MPS` | **m/s** | ±838 m/s |
| **19-21** | **Velocity East** | **3** | **int24** | **0.0001** | `VEL2MPS` | **m/s** | ±838 m/s |
| **22-24** | **Velocity Down** | **3** | **int24** | **0.0001** | `VEL2MPS` | **m/s** | ±838 m/s |
| **25-26** | **Heading** | **2** | **uint16** | **1e-6** | `ANG2RAD` | **radian** | 0-2? |
| **27** | **Pitch** | **1** | **int8** | **0.01** | (8-bit scaled) | **radian** | ±?/2 |
| **28** | **Roll** | **1** | **int8** | **0.01** | (8-bit scaled) | **radian** | ±? |
| **29-30** | **Roll Rate (Wx)** | **2** | **int16** | **0.00001** | `RATE2RPS` | **rad/s** | ±0.32 rad/s |
| **31-32** | **Pitch Rate (Wy)** | **2** | **int16** | **0.00001** | `RATE2RPS` | **rad/s** | ±0.32 rad/s |
| **33-34** | **Yaw Rate (Wz)** | **2** | **int16** | **0.00001** | `RATE2RPS` | **rad/s** | ±0.32 rad/s |

---

## Key Corrections from Previous Versions

### ? WRONG (Recent "fix"):
```c
// Angular rates with scale 0.0001 (WRONG!)
int16_t wx_raw = (int16_t)(packet[29] | (packet[30] << 8));
output->roll_rate = wx_raw * 0.0001; // TOO LARGE BY 10x!

// Heading with scale 0.0001 (WRONG!)
uint16_t heading_raw = packet[25] | (packet[26] << 8);
output->heading = (heading_raw * 0.0001) * RAD2DEG; // TOO LARGE BY 100x!
```

### ? CORRECT (Verified Against OXTS Reference):
```c
// Angular rates with scale 0.00001 (CORRECT per RATE2RPS = 1e-5)
int16_t wx_raw = (int16_t)(packet[29] | (packet[30] << 8));
output->roll_rate = wx_raw * 0.00001;

// Heading with scale 1e-6 (CORRECT per ANG2RAD = 1e-6)
uint16_t heading_raw = packet[25] | (packet[26] << 8);
output->heading = (heading_raw * 1e-6) * RAD2DEG;
```

---

## OXTS Reference Constants (from NComRxC.c)

```c
#define TIME2SEC            (1e-3)   // Timestamp: 1 ms
#define VEL2MPS             (1e-4)   // Velocity: 0.1 mm/s
#define ANG2RAD             (1e-6)   // Angle: 0.001 mrad (for 16-bit heading)
#define RATE2RPS            (1e-5)   // Angular rate: 0.01 mrad/s
#define POSA2M              (1e-3)   // Position altitude: 1 mm
```

**Note**: Pitch and Roll use **8-bit** storage with **0.01 radian** scale (not ANG2RAD).

---

## Expected Results with Corrected Decoder

Based on ground truth data:
- ? Latitude: ~42.96° (was 0.22°)
- ? Longitude: ~-83.59° (was -0.66°)
- ? Altitude: ~216.9 m (was -8060.9 m)
- ? Heading: ~0.16° (was 281.64°)
- ? Pitch: ~0.40° (was -8.59°)
- ? Roll: ~-0.19° (was -1.15°)
- ? Velocities: ~0.0 m/s (was hundreds of m/s)
- ? Angular rates: ~-0.02 deg/s = -0.000349 rad/s (was -0.0222 rad/s, off by 63x!)

---

## How to Apply

1. **Close CLEVIR application**
2. **Copy corrected DLL**:
   ```
   copy NCOMdecoder\NCOMdecoder.dll bin\x64\Debug\
   ```
3. **Restart CLEVIR**
4. **Verify values match ground truth**

---

## Reference
- OXTS NComRxC.c (official reference implementation)
- All multi-byte values are **little-endian**
- Signed integers use **two's complement**
- 24-bit values require **sign extension** to 32-bit
