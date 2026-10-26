## [7.5.1] - 2024-12-22

### Added
- **Real-time LiDAR Health Monitoring**: Direct Hesai AT128 UDP packet parsing
  - Operational state display (High Resolution/Standard/Energy Saving @ XXX RPM)
  - Accurate packet loss detection via 32-bit UDP sequence analysis
  - Functional Safety alerts and fault code reporting
  - Color-coded integrity dashboard with live updates (2-second refresh)
  - 1% CPU overhead (samples every 100th packet)

### Technical Details
- `HesaiPacketInfo` structure for parsed packet data
- `ParseHesaiPacket()` with AT128-specific byte offsets
- Sequence gap detection with uint32 wrap-around handling
- No SDK dependency - pure VB.NET packet parsing
- No UDP port conflicts with PcapDotNet promiscuous capture

### Benefits
- Real-time visibility into LiDAR operational health
- Immediate detection of performance degradation
- Early warning of shutdown conditions
- Accurate data integrity metrics for quality assurance