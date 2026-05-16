# LiDAR SharpPcap Migration - Test Checklist

## ✅ Pre-Migration Baseline (PcapDotNet)
- [ ] Capture 5-minute LiDAR recording
- [ ] Note CPU usage: ________%
- [ ] Note packet drop rate: ________ pkts/sec
- [ ] Note memory usage: ________ MB
- [ ] Verify PCAP file opens in Wireshark: Yes / No
- [ ] Verify event markers are present: Yes / No

## ✅ Post-Migration Tests (SharpPcap)

### Basic Functionality
- [ ] Application starts without errors
- [ ] LiDAR device detected successfully
- [ ] Capture starts (green toast notification)
- [ ] PCAP file created in correct directory
- [ ] Event markers injected (START, STOP, TEST)
- [ ] Capture stops cleanly

### PCAP File Validation
- [ ] Open PCAP in Wireshark (no errors)
- [ ] Verify packet count matches UI counter
- [ ] Check for marker packets (filter: `udp.port == 65000`)
- [ ] Verify marker payload contains GPS timestamp
- [ ] Check sidecar .lidar_events.txt file exists
- [ ] Verify sidecar file has correct frame numbers

### Performance Metrics
- [ ] CPU usage during capture: ________% (target: <20% reduction)
- [ ] Packet drop rate: ________ pkts (target: 0)
- [ ] Memory usage: ________ MB (should be similar)
- [ ] Capture thread priority: AboveNormal (verify in Process Explorer)

### Multi-Device Testing (if applicable)
- [ ] Start capture on LiDAR1
- [ ] Start capture on LiDAR2 (simultaneously)
- [ ] Both devices capture without interference
- [ ] No UDP port conflicts
- [ ] Both PCAP files valid

### OXTS Integration
- [ ] OXTS time sync active during capture
- [ ] Event markers use GPS timestamp (not system time)
- [ ] Verify marker timestamp format: `GPS:yyyy-MM-dd HH:mm:ss.fff`

### Hesai SDK Integration
- [ ] Hesai SDK registers in validation-only mode
- [ ] No UDP port conflicts with SharpPcap
- [ ] Checksum error counter updates
- [ ] Out-of-order packet counter updates
- [ ] SDK stats displayed in Health Detail form

### Edge Cases
- [ ] Stop/Start capture rapidly (3 times in 10 seconds)
- [ ] Inject 100 markers rapidly (stress test)
- [ ] Unplug/replug network cable during capture
- [ ] System suspend/resume during capture
- [ ] Capture duration: 30+ minutes (long-term stability)

### Rollback Test
- [ ] Revert to PcapDotNet branch
- [ ] Verify old code still works
- [ ] Re-migrate to SharpPcap branch
- [ ] Verify migration succeeds again

## 📊 Performance Comparison

| Metric              | PcapDotNet (Before) | SharpPcap (After) | Improvement |
|---------------------|---------------------|-------------------|-------------|
| CPU Usage (%)       |                     |                   |             |
| Packet Drops (pkts) |                     |                   |             |
| Memory (MB)         |                     |                   |             |
| Capture Latency (ms)|                     |                   |             |

## 🐛 Issues Found
1. _______________________________________________________________
2. _______________________________________________________________
3. _______________________________________________________________

## ✅ Sign-Off
- [ ] All tests passed
- [ ] Performance improved or equal to baseline
- [ ] No regressions identified
- [ ] Ready for production deployment

**Tested By:** __________________  
**Date:** __________________  
**Build Version:** __________________
