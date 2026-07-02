# NCOM Decoder Fix - Byte Offset Correction

## Issue Found
The original `ncom_simple.c` had **overlapping byte reads** for angular rates and heading:

### Before (INCORRECT):
```c
// Yaw rate (Wz): bytes 36-37
int16_t wz_raw = (int16_t)(packet[36] | (packet[37] << 8));

// Heading: bytes 37-38  ? WRONG! Byte 37 used twice!
uint16_t heading_raw = packet[37] | (packet[38] << 8);
```

**Problem**: Byte 37 was being read as both:
- High byte of Yaw Rate (Wz)
- Low byte of Heading

This caused **incorrect heading values** because it was shifted by 1 byte.

---

## Fix Applied
According to the **OXTS NCOM Batch S specification**, the correct byte offsets are:

### After (CORRECTED):
```c
// Yaw rate (Wz): bytes 36-37
int16_t wz_raw = (int16_t)(packet[36] | (packet[37] << 8));

// Heading: bytes 39-40  ? FIXED!
uint16_t heading_raw = packet[39] | (packet[40] << 8);
```

---

## NCOM Batch S Byte Layout (Relevant Fields)

| Byte Offset | Field | Size | Scale Factor | Units |
|-------------|-------|------|--------------|-------|
| 16-18 | Velocity North | 3 bytes | 0.0001 | m/s |
| 19-21 | Velocity East | 3 bytes | 0.0001 | m/s |
| 22-24 | Velocity Down | 3 bytes | 0.0001 | m/s |
| 27 | Pitch | 1 byte | 0.01 | radian |
| 28 | Roll | 1 byte | 0.01 | radian |
| **32-33** | **Wx (Roll Rate)** | **2 bytes** | **0.00001** | **rad/s** |
| **34-35** | **Wy (Pitch Rate)** | **2 bytes** | **0.00001** | **rad/s** |
| **36-37** | **Wz (Yaw Rate)** | **2 bytes** | **0.00001** | **rad/s** |
| **39-40** | **Heading** | **2 bytes** | **0.0001** | **radian** |

---

## How to Apply the Fix

1. **Close CLEVIR application** (to release NCOMdecoder.dll)
2. **Run rebuild script**:
   ```
   cd NCOMdecoder
   rebuild_dll.bat
   ```
3. **Copy the new DLL**:
   ```
   copy NCOMdecoder.dll ..\bin\x64\Debug\NCOMdecoder.dll
   ```
4. **Restart CLEVIR**

---

## Expected Results After Fix

? **Heading values should now be correct**
? **Angular rates (Wx, Wy, Wz) should remain correct**
? **Velocities (North, East, Down) should remain correct**

---

## Testing

After applying the fix, verify:

1. **Console Output** shows realistic heading values (0-360°)
2. **Log files** contain accurate GPS start markers with correct heading
3. **Angular rates** show non-zero values when vehicle is rotating

---

## References

- OXTS RT3000 NCOM Manual (Batch S Packet Structure)
- `ncom_simple.c` (line 104: heading byte offset corrected from 37-38 to 39-40)
