# ? FINAL CORRECT NCOM DECODER
## Based on Official OXTS NCOM Manual Rev 250811

---

## ?? Official Byte Layout (NCOM Structure-A, Table 6)

### Batch B: Position, Velocity and Orientation Output

| Byte Range | Field | Format | Scale | Units | Notes |
|------------|-------|--------|-------|-------|-------|
| **0** | Sync Byte | UByte | N/A | N/A | Always 0xE7 |
| **1-2** | Time | UShort | 0.001 | seconds | Milliseconds into GPS minute |
| **3-11** | Accelerations | 3×Word | 1×10?? | m/s² | Body frame accelerations (x,y,z) |
| **12-14** | Angular Rate X | Word | 1×10?? | rad/s | Roll rate (Wx) |
| **15-17** | Angular Rate Y | Word | 1×10?? | rad/s | Pitch rate (Wy) |
| **18-20** | Angular Rate Z | Word | 1×10?? | rad/s | Yaw rate (Wz) |
| **21** | Nav. Status | UByte | N/A | N/A | Structure identifier |
| **22** | Checksum 1 | UByte | N/A | N/A | Checksum of bytes 1-21 |
| **23-30** | **Latitude** | **Double** | **N/A** | **radians** | ? 64-bit IEEE 754 double |
| **31-38** | **Longitude** | **Double** | **N/A** | **radians** | ? 64-bit IEEE 754 double |
| **39-42** | **Altitude** | **Float** | **N/A** | **meters** | ? 32-bit IEEE 754 float |
| **43-45** | **North Velocity** | **Word** | **1×10??** | **m/s** | ? Signed 24-bit |
| **46-48** | **East Velocity** | **Word** | **1×10??** | **m/s** | ? Signed 24-bit |
| **49-51** | **Down Velocity** | **Word** | **1×10??** | **m/s** | ? Signed 24-bit |
| **52-54** | **Heading** | **Word** | **1×10??** | **radians** | ? Signed 24-bit, Range ±? |
| **55-57** | **Pitch** | **Word** | **1×10??** | **radians** | ? Signed 24-bit, Range ±? |
| **58-60** | **Roll** | **Word** | **1×10??** | **radians** | ? Signed 24-bit, Range ±? |
| **61** | Checksum 2 | UByte | N/A | N/A | Checksum of bytes 1-60 |
| **62** | Status Channel | UByte | N/A | N/A | Channel ID |
| **63-70** | Batch S | 8×UByte | Varies | Varies | Status information |
| **71** | Checksum 3 | UByte | N/A | N/A | Checksum of bytes 1-70 |

---

## ?? Critical Corrections from Previous Attempts

### ? What Was WRONG (All Previous Versions):

1. **Lat/Lon Format**: 
   - **WRONG**: 24-bit signed integers with scale 1×10?? degrees
   - **CORRECT**: **64-bit IEEE 754 doubles in radians**

2. **Altitude Format**:
   - **WRONG**: 24-bit signed integer with scale 0.001 meters
   - **CORRECT**: **32-bit IEEE 754 float in meters**

3. **Pitch/Roll Format**:
   - **WRONG**: 8-bit signed integers
   - **CORRECT**: **24-bit signed integers with scale 1×10?? radians**

4. **Byte Offsets**:
   - **WRONG**: Started Batch B at byte 7 (or 14, or 20...)
   - **CORRECT**: **Batch B starts at byte 23**

---

## ? Correct Implementation

### Key Functions

```c
// Cast 8 bytes (little-endian) to IEEE 754 double
static inline double cast_8_byte_LE_to_double(const uint8_t *b) {
    union {
        uint64_t i;
        double d;
    } u;
    u.i = ((uint64_t)b[0]) | ((uint64_t)b[1] << 8) | 
          ((uint64_t)b[2] << 16) | ((uint64_t)b[3] << 24) |
          ((uint64_t)b[4] << 32) | ((uint64_t)b[5] << 40) | 
          ((uint64_t)b[6] << 48) | ((uint64_t)b[7] << 56);
    return u.d;
}

// Cast 4 bytes (little-endian) to IEEE 754 float
static inline float cast_4_byte_LE_to_float(const uint8_t *b) {
    union {
        uint32_t i;
        float f;
    } u;
    u.i = ((uint32_t)b[0]) | ((uint32_t)b[1] << 8) | 
          ((uint32_t)b[2] << 16) | ((uint32_t)b[3] << 24);
    return u.f;
}
```

### Decoding Example

```c
// Latitude: bytes 23-30 (Double in radians)
double lat_rad = cast_8_byte_LE_to_double(&packet[23]);
output->latitude = lat_rad * RAD2DEG; // Convert to degrees

// Longitude: bytes 31-38 (Double in radians)
double lon_rad = cast_8_byte_LE_to_double(&packet[31]);
output->longitude = lon_rad * RAD2DEG;

// Altitude: bytes 39-42 (Float in meters)
output->altitude = cast_4_byte_LE_to_float(&packet[39]);

// North Velocity: bytes 43-45 (24-bit signed, scale 1×10??)
int32_t vn_raw = sign_extend_24(packet[43] | (packet[44] << 8) | (packet[45] << 16));
output->velocity_north = vn_raw * 1e-4;

// Heading: bytes 52-54 (24-bit signed, scale 1×10?? radians)
int32_t hdg_raw = sign_extend_24(packet[52] | (packet[53] << 8) | (packet[54] << 16));
output->heading = (hdg_raw * 1e-6) * RAD2DEG;
```

---

## ?? Expected Results

Based on ground truth from NAVDisplay:

| Field | Expected Value | Units |
|-------|----------------|-------|
| Latitude | ~42.964° | degrees |
| Longitude | ~-83.586° | degrees |
| Altitude | ~220.088 m | meters |
| Heading | ~0.334° | degrees |
| Pitch | ~0.410° | degrees |
| Roll | ~-0.175° | degrees |
| Velocity North | ~0.00 m/s | m/s |
| Velocity East | ~0.01 m/s | m/s |
| Velocity Down | ~0.00 m/s | m/s |
| Yaw Rate | ~-0.04 deg/s | deg/s |

---

## ?? Build Instructions

### x64 Build (Required for VB.NET AnyCPU/x64)

```cmd
cd /d "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\NCOMdecoder"
cl /O2 /W3 /LD /DNCOM_DECODE_DLL_EXPORT ncom_simple.c /link /OUT:NCOMdecoder.dll
```

### Copy to Application

```powershell
Copy-Item "NCOMdecoder.dll" -Destination "bin\x64\Debug\" -Force
```

### Verify Architecture

```powershell
dumpbin /headers NCOMdecoder.dll | Select-String "machine"
```

Should show: **`8664 machine (x64)`**

---

## ?? Reference

- **OXTS NCOM Manual**: Rev 250811
- **Table 6**: Batch B (position, velocity and orientation output) definition (Page 13)
- **Table 4**: Batch A (inertial output) definition (Page 10)
- **All multi-byte values**: Little-endian format (LSB first)
- **Signed integers**: Two's complement representation

---

## ?? Success Criteria

? Lat/Lon values match NAVDisplay to ±0.0001°  
? Altitude matches to ±1 meter  
? Heading/Pitch/Roll match to ±0.01°  
? Velocities match to ±0.01 m/s  
? Angular rates match to ±0.001 rad/s  

---

## ?? Debugging

If values still don't match:
1. Check that **Navigation Status (byte 21) ? 11** (must be Structure-A)
2. Verify **Sync Byte (byte 0) = 0xE7**
3. Check **byte order** (must be little-endian)
4. Confirm **VB.NET app is x64** (not x86/AnyCPU-Prefer32bit)
5. Use **wireshark** to capture and analyze raw UDP packets

---

*Generated: 2025-12-07*  
*Decoder: ncom_simple.c*  
*Manual: OXTS NCOM Rev 250811*
