The PTP (Precision Time Protocol, IEEE 1588) packet structure on the Cisco Catalyst C9300L-48T-4X (which supports PTP/TSN features including gPTP/IEEE 802.1AS profiles) follows the standard IEEE 1588-2008/2019 definition. Cisco does not use a proprietary packet format for PTP messages or status parameters—it implements the standard messages with hardware timestamping support on supported ports.Standard PTP Packet Structure (Common Header + Status-Relevant Fields)All PTP messages share a common header (defined in IEEE 1588). Key fields that provide or indicate PTP status parameters include:
•	Common PTP Header (first ~34 bytes):
•	transportSpecific (4 bits)
•	messageType (4 bits) — e.g., 0x0=Sync, 0x1=Delay_Req, 0x2=Follow_Up, 0x3=Delay_Resp, 0xB=Announce, etc.
•	versionPTP (4 bits) — typically 2 for PTPv2
•	messageLength (16 bits)
•	domainNumber (8 bits)
•	reserved fields
•	flagField (16 bits) — Primary status flags:
•	twoStepFlag
•	alternateMasterFlag
•	unicastFlag
•	Profile-specific flags
•	leap61, leap59, currentUtcOffsetValid, ptpTimescale, timeTraceable, frequencyTraceable (these directly convey time/status quality)
•	correctionField (64 bits) — Used by transparent clocks for residence time (important for accuracy/status in TSN)
•	sourcePortIdentity (80 bits: 64-bit clockIdentity + 16-bit portNumber)
•	sequenceId (16 bits)
•	controlField (legacy, 8 bits)
•	logMessageInterval (8 bits)
•	Announce Message (key for conveying clock status parameters):
•	originTimestamp
•	currentUtcOffset
•	grandmasterPriority1 / grandmasterPriority2
•	grandmasterClockQuality (includes clockClass, clockAccuracy, offsetScaledLogVariance)
•	grandmasterIdentity
•	stepsRemoved
•	timeSource
These fields carry the core status parameters (clock quality, traceability, priorities, offset info, etc.) used by the Best Master Clock Algorithm (BMCA) and for synchronization status.Other relevant messages:
•	Sync / Follow_Up / Delay_Req / Delay_Resp / Pdelay_* (for timing and delay measurement; event messages are hardware-timestamped on Cisco).
•	Management messages (can query status/datasets; forwarded with hop-count handling on boundary clocks).
•	Signaling messages (generally dropped or limited on Cisco Catalyst 9300 series).
Cisco-specific notes for C9300L-48T-4X:
•	Supports PTPv2 (IEEE 1588-2008), two-step clocks only (one-step dropped).
•	Supports Default profile, gPTP (IEEE 802.1AS for TSN), G.8275.1, and AES67 profiles (mutually exclusive).
•	Multicast L2 Ethernet or L3 IPv4/UDP transport.
•	Hardware timestamping on supported ports (typically uplinks + applicable downlinks; exact port limits vary by exact 9300L variant—verify with show ptp).
•	Transparent Clock (E2E or P2P) or Boundary Clock modes.
•	PTP packets are forwarded by default even without PTP enabled (transparent passthrough).
How to View/Monitor PTP Status Parameters on the SwitchUse these commands (most relevant for status):
•	show ptp clock — Overall clock status, type (BC/TC/OC), offset, mean path delay, quality.
•	show ptp port [interface] — Per-port state (Master/Slave/Passive/etc.), intervals, delay mechanism.
•	show ptp brief — Quick port/domain/state overview.
•	show ptp parent — Parent/grandmaster details and status.
•	show platform software fed switch active ptp domain 0 — Low-level servo/clock sync details.
For remote/SNMP access to status parameters, use the CISCO-PTP-MIB (OID 1.3.6.1.4.1.9.9.760):
•	cPtpClockCurrentDSTable — offsetFromMaster, meanPathDelay, stepsRemoved.
•	cPtpClockParentDSTable — Parent/grandmaster identity, priorities, quality, observed offset.
•	Port dataset tables (cPtpClockPortDS*) — Port state, identity, intervals, stats (packets in/out, errors).
•	Other tables for system/domain info and statistics.
This MIB exposes the standard IEEE 1588 datasets (currentDS, parentDS, timePropertiesDS, portDS) plus Cisco-specific extensions.Official References
•	Cisco Catalyst 9300 PTP Configuration Guide (covers messages, modes, verification, supported profiles): Layer 2 Configuration Guide – Configuring Precision Time Protocol (PTP) 
•	Cisco PTP Support FAQ (profiles, limitations, packet handling): Support for Precision Time Protocol on Cisco Catalyst Switches 
•	Troubleshooting PTP on Catalyst 9000: Search Cisco for "Troubleshoot Precision Time Protocol on Catalyst 9000 Series Switches" (document ID 221062).
•	IEEE Standard: IEEE Std 1588-2008 or 1588-2019 (Clause on message formats and datasets) — this is the definitive source for the exact byte-level packet structure.
•	CISCO-PTP-MIB: Available on sites like mibs.observium.org or via Cisco MIB downloads.
For packet captures, use Wireshark (it has an excellent PTP dissector) on a SPAN/mirror port. The structure is fully standard-compliant.

The TM2000B uses UDP port 7372 for its Locator Data Service, a protocol allowing network applications to extract status and location information from the device.  To query this service, a client must send a 3-byte hexadecimal message (0xA1 0x04 0xB2) to the device's IP address on port 7372.  The device responds with an 80-byte packet containing the firmware version, GPS lock status (0=No Lock, 1=2D, 2=3D), NTP sync count, current UTC time, latitude/longitude, and the server name

---

## Resolved: Hesai LiDAR "Frozen" Status — Root Cause and Fix (verified on live switch)

### Actual root cause (NOT a profile/transport incompatibility)

The initial hypothesis was that `802.1AS` (gPTP) and IEEE 1588 Default Profile
are wire-incompatible and that the switch needed a special gPTP profile
configured. **This was investigated live on the switch (`FMVSS127_switch`,
C9300L-48T-4X, IOS-XE 17.6.4) and found to be incorrect.** The real cause was
much simpler:

- **PTP was never enabled (`ptp enable`) on any of the physical, real
  LiDAR/TM2000B-facing interfaces.** All prior configuration attempts had
  targeted a phantom/incorrect interface (`GigabitEthernet1/0/20`), which does
  not physically exist — `show switch` revealed stack member `1` has MAC
  `0000.0000.0000` and state `Provisioned` (not physically present); the
  switch is a single active unit, member `2`.
- The real, physically-cabled interfaces are:
  - `GigabitEthernet2/0/14` — LiDAR #1
  - `GigabitEthernet2/0/16` — LiDAR #2
  - `GigabitEthernet2/0/18` — PC LiDAR NIC uplink
  - `GigabitEthernet2/0/26` — TM2000B TimeMachine (VLAN30)
- None of these ports had `ptp enable` applied, so the switch was not
  participating in PTP on any of them at all — regardless of global
  `ptp mode`/`ptp transport`/priority settings, no Sync/Announce/Delay traffic
  was being relayed or boundary-clocked on those ports. This alone produces
  the "Frozen" LiDAR status: the LiDAR previously locked (e.g. during bench
  testing on a different network) but received no valid PTP traffic on this
  switch, so it held its last known state instead of reverting to free-run.

### The fix that was applied and verified

```
configure terminal
interface GigabitEthernet2/0/14
 ptp enable
exit
interface GigabitEthernet2/0/16
 ptp enable
exit
interface GigabitEthernet2/0/26
 ptp enable
exit
```

After applying `ptp enable`, both LiDARs achieved PTP lock **as long as each
LiDAR's own profile was set to IEEE1588**. Setting a LiDAR's own profile to
`802.1AS` produced **Frozen**, then **Free Run** after a LiDAR reboot — the
LiDAR never (re-)acquired sync in that mode, regardless of the TM2000B's
profile setting.

### Final working configuration (verified) — CORRECTED

**Earlier revision of this doc incorrectly recommended `TM2000B = 802.1AS`
as giving the "tightest lock." This has been disproven and is corrected
below — see the "ES886 masquerade" incident for the full story.**

- **Both Hesai LiDARs: `Profile = IEEE1588` (Default Profile)** — this is
  required; setting a LiDAR itself to `802.1AS` prevents it from syncing
  through this switch at all (Free Run).
- **TM2000B: `Profile = IEEE1588` (`Packet Output = IPv4 UDP`), `Delay
  Mechanism = End to End`, `Transmission Method = Multicast`.** This is now
  the confirmed, correct, permanent setting — verified via `show ptp parent`
  showing the TM2000B's real clock identity (`0xC:AE:7D:FF:FE:25:19:F6`,
  Class 6, Within 1us) as grandmaster, with both LiDARs reporting `Locked`.
- **Do not set the TM2000B to `802.1AS`.** This switch's PTP transport is
  hard-locked to `udp-ipv4` and cannot receive the TM2000B's native Layer-2
  gPTP frames under any circumstance — see below for what actually happens
  if you do.
- The LiDAR's own profile setting does **not** need to match the TM2000B's —
  the switch's boundary clock normalizes/re-originates PTP for the LiDAR side
  regardless of what the TM2000B transmits upstream (when it can receive it).

### Why a LiDAR (or the TM2000B) set to 802.1AS cannot sync through this switch

The C9300L's PTP transport is **hard-locked to `udp-ipv4`** — confirmed via
`ptp transport ?`, which only offers `ipv4` as an option (no pure-L2/Ethernet
transport exists on this platform/software combination). There is also no
`ptp profile 802.1as` global command; the closest related feature,
`ptp dot1as extend property <WORD>`, requires a specific property name that
was not identified via CLI help and was **not applied** to the live switch to
avoid risking an unverified change.

The TM2000B's own web UI confirms that in `802.1AS (gPTP)` mode it transmits
genuine Layer-2 Ethernet multicast frames — `PTP Destination MAC:
01:1B:19:00:00:00`, `P2P Destination MAC: 01:80:C2:00:00:0E`, no IP/UDP
header at all. A switch whose PTP engine only understands `udp-ipv4` **cannot
parse these frames as PTP messages under any configuration** — it doesn't
see an announce, sync, or delay message from the TM2000B at all in this mode.
This applies equally whether it's a LiDAR or the TM2000B transmitting 802.1AS.

### Incident: the "ES886 masquerade" — why 802.1AS looked like it was working

Earlier testing believed `TM2000B = 802.1AS` produced the tightest lock
(single-digit ns). **This was a false correlation.** Live diagnosis (see the
Peer-to-Peer re-test below) revealed that whenever the TM2000B is set to
`802.1AS`, the switch never hears it at all (per the transport limitation
above) — instead, `show ptp parent` showed a **completely different
grandmaster clock identity** (`0x0:60:34:FF:FE:1D:C3:47`), which was
identified as the **ETAS ES886**. The ES886 has its own PTP clock that goes
SLAVE when a master is present on the network, and **free-runs to MASTER
itself when no master is heard** — exactly what happens on `udp-ipv4` when
the TM2000B is transmitting only L2 802.1AS frames the switch can't see.

So the "excellent lock" seen under `TM2000B = 802.1AS` was actually both
LiDARs and the switch locking to the **ES886's free-running clock**, not to
the TM2000B at all. This explains why lock quality seemed to vary
unpredictably between sessions — it depended on whether the ES886 happened
to be reachable/free-running as a masquerading master at the time, not on
any property of the TM2000B's 802.1AS configuration.

**Confirming test performed:**
1. Switch set to `ptp mode boundary pdelay-req` (Peer-to-Peer), TM2000B set
   to `802.1AS` + Peer-to-Peer (matching the original "best lock" recipe).
2. `show ptp parent` → Grandmaster Clock Identity = `0x0:60:34:FF:FE:1D:C3:47`
   (the ES886, Class 248, Accuracy Within 25us, Priority 128/128 — default
   Cisco/ES886 free-run values, **not** the TM2000B's real identity/quality).
   All three PTP ports (`Gi2/0/14`, `Gi2/0/16`, `Gi2/0/26`) showed
   `Peer mean path delay(ns): 0` — no real peer-delay exchange ever
   completed with anything, consistent with the switch only hearing the
   ES886 over `udp-ipv4` and never the TM2000B.
3. Switch reverted to `ptp mode boundary delay-req` (End-to-End), TM2000B set
   to `IEEE1588` + End-to-End + Multicast → `show ptp parent` → Grandmaster
   Clock Identity = `0xC:AE:7D:FF:FE:25:19:F6` (the **real** TM2000B, Class 6,
   Within 1us) — confirmed correct. Both LiDARs reported **Locked**.

**Conclusion:** if the TM2000B is set to `802.1AS` again, expect the switch
to stop hearing it entirely and fall back to whatever `udp-ipv4` clock is
reachable (the ES886, if present and free-running to master, or the switch
itself self-electing as grandmaster if nothing else is heard). This is
deterministic given the switch's fixed `udp-ipv4`-only transport — it is not
expected to vary run to run. **`TM2000B = IEEE1588` is therefore the
permanent, confirmed setting**, not merely one of two viable options.

### User-confirmed behavior: ETAS ES886 SLAVE/MASTER flip and lock-fidelity paradox

Directly observing the ETAS ES886's own PTP status while toggling the
TM2000B's profile confirmed the mechanism above exactly:

- **TM2000B = IEEE1588 (`IPv4 UDP`):** the ES886 sees the TM2000B as a better
  master reachable over `udp-ipv4` and correctly transitions its own PTP mode
  to **SLAVE**, deferring to the TM2000B (BMCA working as intended).
- **TM2000B = `802.1AS`:** the ES886 no longer hears the TM2000B (same
  Layer-2/`udp-ipv4` transport mismatch described above applies to the ES886
  as well as the switch) and its PTP mode flips to **MASTER** — i.e. the
  ES886 self-elects, exactly matching the switch's `show ptp parent` showing
  the ES886's identity (`0x0:60:34:FF:FE:1D:C3:47`) as grandmaster in that
  mode. This is direct, independent confirmation of the "ES886 masquerade"
  finding above.

**Interesting but non-actionable observation:** Hesai LiDAR lock *fidelity*
(offset/jitter) was subjectively/numerically **tighter when the ES886 was
acting as MASTER** than when the TM2000B was the master. This is plausible
for reasons unrelated to which clock is "better":
- The ES886 may be topologically/electrically closer to the switch (fewer
  hops, less path asymmetry between request/response legs), which reduces
  measured PTP offset/jitter regardless of the master's own absolute
  accuracy.
- The ES886's PTP servo/hardware timestamping implementation may simply have
  different (tighter-looking) jitter characteristics than the TM2000B's, even
  though the TM2000B is the vehicle's authoritative GPS-disciplined time
  source.
- **Lock fidelity (offset/jitter to whichever clock is elected master) is a
  different property than lock correctness (GPS/UTC traceability).** The
  ES886 free-running as master provides a very stable *local* reference with
  no external correction — over time it will drift with no ground truth,
  whereas the TM2000B is the only GPS-disciplined, UTC-traceable time source
  in this system.

**Decision:** despite the ES886-as-master lock looking tighter, the TM2000B
must remain the intended, actual grandmaster (`TM2000B = IEEE1588`) so that
system time stays GPS/UTC-traceable. Accepting the ES886 as an accidental
master (via TM2000B=802.1AS) would trade this correctness for cosmetically
tighter-looking LiDAR offset numbers, and is not an acceptable tradeoff for
this system.

### Current switch-wide PTP configuration (as verified on live switch)

```
ptp transport ipv4 udp
ptp mode boundary delay-req
ptp priority1 100
ptp priority2 100
```

**Important:** the delay mechanism must be `delay-req` (End-to-End). This
switch was found on a later date with `ptp mode boundary pdelay-req`
(Peer-to-Peer) instead, which broke lock on both LiDARs even with `ptp enable`
present and the TM2000B correctly elected as grandmaster — see the incident
below. To change the delay mechanism, PTP must first be disabled globally:

```
configure terminal
no ptp mode
ptp mode boundary delay-req
end
copy running-config startup-config
```

(`ptp mode boundary delay-req` is rejected outright if `ptp mode` is already
configured with a different delay mechanism — Cisco requires `no ptp mode`
first.)

### Incident: recurrence after config was not saved + delay mechanism drift

On a later date, both LiDARs were found in **Free Run** again, with the
TM2000B set to `IEEE1588` and, separately, to `802.1AS` — neither profile
change affected the outcome, which was the first clue this was not a
profile/transport issue this time. Live diagnosis found **two** compounding
problems:

1. **`ptp enable` was missing again on `Gi2/0/14` and `Gi2/0/16`.** The fix
   from the original investigation had apparently never been saved with
   `copy running-config startup-config`, and the switch had been
   rebooted/power-cycled at some point since, reverting to the old
   `startup-config` without `ptp enable` on those two ports. `Gi2/0/26`
   (TM2000B-facing) still had `ptp enable`, so the switch could still see and
   elect the TM2000B as grandmaster (`show ptp clock` → `Steps Removed: 1`,
   `show ptp parent` → grandmaster identity matched the TM2000B), but the
   LiDAR-facing ports were dark to PTP.
   - **Fix:** re-applied `ptp enable` under `interface GigabitEthernet2/0/14`
     and `interface GigabitEthernet2/0/16`, then `copy running-config
     startup-config` to persist it this time.
2. **The switch's global delay mechanism had changed from `delay-req`
   (End-to-End) to `pdelay-req` (Peer-to-Peer)** — `show ptp port
   GigabitEthernet2/0/14/16/26` all showed `Delay Mechanism: Peer to Peer`
   where the original working baseline (`C9300L Show PTP Port.txt`) showed
   `End to End`. Even after fixing (1), `Gi2/0/26` remained stuck in
   `Port state: UNCALIBRATED` (BMCA had correctly selected the TM2000B as
   master, but the Peer-delay measurement — `Peer mean path delay(ns): 0`
   on every port — never completed) and both LiDARs stayed Free Run.
   - **Fix:**
     ```
     configure terminal
     no ptp mode
     ptp mode boundary delay-req
     end
     copy running-config startup-config
     ```
     After this, `show ptp port GigabitEthernet2/0/26` moved off
     `UNCALIBRATED` and **both LiDARs achieved Locked status**.

**Lesson:** always run `copy running-config startup-config` immediately after
any verified-working PTP change on this switch — an unsaved config is fully
reverted by any reboot/power-cycle, silently reproducing symptoms that look
like a fresh profile/transport problem. Also treat `ptp mode boundary
delay-req` vs `pdelay-req` as a distinct, switch-wide variable to check
separately from per-interface `ptp enable` and from the LiDAR/TM2000B PTP
profile settings — all three must be correct simultaneously for lock.

**Note:** the TM2000B itself has its own independent delay-mechanism setting
(Peer-to-Peer or End-to-End), separate from its PTP profile (802.1AS vs.
IEEE1588). This must match the switch's `ptp mode boundary delay-req`/
`pdelay-req` setting — confirmed at time of writing that the TM2000B is set
to **End-to-End**, matching the switch's `delay-req` mode. If the TM2000B's
delay mechanism is ever changed independently (e.g. to Peer-to-Peer) without
also changing the switch's `ptp mode`, expect the same `UNCALIBRATED`/no-lock
symptoms described above to recur, even with `ptp enable` correctly applied.

Per-interface `ptp enable` was applied to `Gi2/0/14`, `Gi2/0/16`, and
`Gi2/0/26` (note: `ptp enable` does **not** appear in `show run interface`
output on this platform — it is tracked in a separate internal PTP port-state
table; verify with `show ptp port <interface>` instead of `show run
interface`).

### Verification commands used

```
show ptp clock
```
Shows overall clock config; `Steps Removed` should read `1` (or higher) once
the switch actually hears the TM2000B on `Gi2/0/26`, rather than `0` (switch
self-elected as grandmaster).

```
show ptp port
show ptp port GigabitEthernet2/0/14
```
Per-port state (`MASTER`/`SLAVE`/`FAULTY`/`UNCALIBRATED`) and delay mechanism.
Use `show switch` first if interface numbering is in doubt — this is a
single-member stack with switch/member number `2`; do not assume `1/0/x`
naming.

```
show ptp parent
```
Confirm grandmaster clock identity — if it still shows the switch's own
identity (`0x90:EB:50:FF:FE:46:DF:80`) instead of the TM2000B's, the switch has
not heard the TM2000B and is self-electing as grandmaster (check `ptp enable`
on the TM2000B-facing port and confirm the TM2000B is actually transmitting).

### Checklist for re-verifying or extending this configuration

1. Confirm interface identity first: `show switch` (identify the active
   member number) and `show interfaces status | include LIDAR|TIMEMACHINE` to
   find the correct physical ports before applying any PTP config — do not
   assume interface numbering matches device port labels.
2. `ptp enable` must be applied per-interface on every LiDAR port and the
   TM2000B-facing port; it will not show in `show run interface` — verify with
   `show ptp port <interface>` instead.
3. `show ptp clock` → `Steps Removed` should be `1`+ (synced to TM2000B), not
   `0` (self-elected grandmaster).
4. `show ptp parent` → `Grandmaster Clock Identity` should match the TM2000B,
   not the switch's own clock identity.
5. **Set both Hesai LiDARs' own `Profile` to `IEEE1588`** — do not set a
   LiDAR's own profile to `802.1AS` on this switch; it will not sync (Frozen,
   then Free Run after reboot).
6. **Set the TM2000B's `Profile` to `IEEE1588` (`Packet Output = IPv4 UDP`),
   `Delay Mechanism = End to End`, `Transmission Method = Multicast`.** Do
   **not** set the TM2000B to `802.1AS` — the switch cannot receive its native
   L2 gPTP frames, and BMCA will instead lock to whatever else is reachable
   over `udp-ipv4` (confirmed to be the ETAS ES886 in this environment, which
   free-runs to master when no other master is heard) — see the "ES886
   masquerade" incident above for the full writeup and confirming test.
7. **Confirm the global delay mechanism is `ptp mode boundary delay-req`
   (End-to-End), not `pdelay-req` (Peer-to-Peer)** — check `show ptp port
   <interface>` → `Delay Mechanism`. If it shows `Peer to Peer`, a port stuck
   at `Port state: UNCALIBRATED` with `Peer mean path delay(ns): 0` even
   though the grandmaster is correctly the TM2000B is the tell-tale symptom.
   Fix with `no ptp mode` then `ptp mode boundary delay-req` (the mode cannot
   be changed directly while PTP is already configured).
7a. **The TM2000B has its own independent delay-mechanism setting**
    (Peer-to-Peer or End-to-End), separate from its PTP profile (802.1AS vs.
    IEEE1588 selection). It must be set to **End-to-End** to match the
    switch's `delay-req` mode above — confirmed correct at time of writing. If
    this is ever changed on the TM2000B without also updating `ptp mode` on
    the switch, expect the same `UNCALIBRATED`/no-lock symptoms to recur.
8. **Always run `copy running-config startup-config` immediately** after any
   verified-working PTP change — an unsaved config reverts completely on the
   next reboot/power-cycle, and the two symptoms above (`ptp enable` missing,
   delay mechanism reverted) have both recurred this way.
9. **Verify `show ptp parent` → Grandmaster Clock Identity matches the
   TM2000B's known identity (`0xC:AE:7D:FF:FE:25:19:F6`), not the switch's own
   identity (`0x90:EB:50:FF:FE:46:DF:80`) and not the ETAS ES886
   (`0x0:60:34:FF:FE:1D:C3:47`).** All three identities have been observed as
   the reported "grandmaster" at different points — only the TM2000B's is
   correct; if you see either of the other two, the switch is not actually
   getting time from the TM2000B.
10. Genuine end-to-end 802.1AS (any device reporting locked under the
    802.1AS profile through this switch) is **not achievable** on this switch
    model/software — confirmed via the transport limitation and the ES886
    masquerade test above. Would require a switch with true L2-native gPTP
    boundary/transparent clock support, or removing this switch from the
    timing path entirely.
