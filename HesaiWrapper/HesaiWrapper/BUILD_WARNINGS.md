# HesaiWrapper — Build Warnings Reference

## Context

HesaiWrapper wraps the **Hesai LiDAR SDK 2.0** (`C:\DEV\HesaiLidar_SDK_2.0-master\`),
a third-party vendor library supplied by Hesai Technology Co., Ltd.

The Visual Studio build produces a small number of compiler warnings on every build.
**These warnings are in the vendor SDK source — not in the CLEVIR wrapper code.**
They are documented here so they are understood and not mistakenly acted upon.

---

## Warnings

### C4267 — `size_t` narrowing to smaller integer types

| # | File | Line | Detail |
|---|------|------|--------|
| 1 | `libhesai\PtcParser\src\ptc_1_0_parser.cc` | 47 | `size_t` → `uint32_t` (argument) |
| 2 | `libhesai\PtcParser\src\ptc_1_0_parser.cc` | 77 | `size_t` → `int` (initializing) |
| 3 | `libhesai\PtcParser\src\ptc_1_0_parser.cc` | 93 | `size_t` → `int` (argument) |
| 4 | `libhesai\PtcParser\src\ptc_1_0_parser.cc` | 95 | `size_t` → `uint32_t` (argument) |
| 5 | `libhesai\PtcParser\src\ptc_2_0_parser.cc` | 38 | `size_t` → `uint32_t` (argument) |
| 6 | `libhesai\SerialClient\src\serial_client.cc` | 50  | `size_t` → `uint32_t` (initializing) |
| 7 | `libhesai\SerialClient\src\serial_client.cc` | 85  | `size_t` → `int` (initializing) |
| 8 | `libhesai\SerialClient\src\serial_client.cc` | 130 | `size_t` → `uint16_t` (argument) |
| 9 | `libhesai\SerialClient\src\serial_client.cc` | 312 | `size_t` → `uint16_t` (argument) |
| 10 | `libhesai\SerialClient\src\serial_client.cc` | 419 | `size_t` → `uint16_t` (argument) |
| 11 | `libhesai\SerialClient\src\serial_client.cc` | 509 | `size_t` → `int` (initializing) |

**Why it happens:** `size_t` is 64-bit on x64 Windows. The Hesai SDK was written
assuming 32-bit sizes and assigns `std::vector::size()` return values directly into
`uint32_t`, `uint16_t`, or `int` fields without an explicit cast.

**Risk:** Theoretical data truncation if a buffer ever exceeded ~4 GB (`uint32_t`)
or ~65 KB (`uint16_t`). In practice, LiDAR packet payloads are measured in hundreds
of bytes, so the upper bits are always zero and no truncation occurs at runtime.

---

### C4244 — `uint16_t` assigned to `uint8_t`

| # | File | Line | Detail |
|---|------|------|--------|
| 1 | `libhesai\lidar_types.h` | 199 | `const uint16_t` → `uint8_t` |
| 2 | `libhesai\lidar_types.h` | 215 | `const uint16_t` → `uint8_t` |

**Why it happens:** Two fields in the `LidarDecodedPacket` structure are declared as
`uint8_t` but are assigned from `uint16_t` source values. Hesai uses the same header
across multiple sensor models; on the sensors used by CLEVIR the values assigned to
these fields are always within 0–255.

**Risk:** If a future firmware update or different sensor model sends a value > 255
for these fields, the upper byte would be silently dropped. Worth noting in any
hardware compatibility review when upgrading to a new Hesai sensor variant.

---

## Why these have not been fixed

These warnings exist **entirely within `C:\DEV\HesaiLidar_SDK_2.0-master\`**, which
is Hesai vendor source code outside the CLEVIR repository.

Patching the SDK source directly would:
1. Create a privately maintained fork of a vendor library.
2. Require re-applying patches every time Hesai releases an SDK update.
3. Risk introducing subtle behavioural changes into code that Hesai has tested
   against their own hardware.

The correct fix, if ever required, is to raise the issue with Hesai or wait for an
upstream SDK release that addresses them.

---

## What to do if new warnings appear

1. Check whether the warning is in `C:\DEV\HesaiLidar_SDK_2.0-master\` (vendor —
   document here, do not fix) or in `HesaiWrapper\HesaiWrapper\` (CLEVIR code —
   fix it).
2. If it is a new vendor warning not listed above, add it to this file with the same
   format and a brief risk assessment.
3. If the Hesai SDK is updated, re-run a full rebuild and verify this file still
   reflects the actual warning list.
