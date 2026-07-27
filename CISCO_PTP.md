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
