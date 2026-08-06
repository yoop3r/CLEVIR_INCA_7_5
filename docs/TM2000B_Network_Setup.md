# TM2000B / Cisco Catalyst Network Setup Checklist

This checklist documents the network configuration required for any PC (DEV, bench, or
production) that needs to reach the TM2000B TimeMachine over the Cisco Catalyst
C9300L-48T-4X switch, and to resolve the "PTP: No TimeMachine response" symptom in the
LiDAR Health Detail form.

## Topology

```
PC (LiDAR NIC) --- VLAN 20 (100.64.1.0/24) --- Catalyst switch (routed, ip routing enabled)
													  |
												VLAN 30 (192.168.10.0/24)
													  |
												 TM2000B (192.168.10.20)

OXTS RT, Hunter, Intrepid GigaStar --- VLAN 40 (10.5.2.0/24) --- Catalyst switch
(isolated CAN/RTK data-extraction network, separate from LiDAR/VLAN20; ETAS consumes
 CAN/RTK data from this VLAN)
```

- Switch SVIs: `Vlan20 = 100.64.1.177/24`, `Vlan30 = 192.168.10.254/24`, `Vlan40 = 10.5.2.1/24`
- TM2000B: `192.168.10.20`, gateway `192.168.10.254`
- Vlan20 was renumbered from `100.64.20.0/24` to `100.64.1.0/24` to satisfy a hard
  requirement of the LiDAR alignment tool, which is used broadly across the organization and
  expects LiDAR 1/2 on `100.64.1.2/24` and `100.64.1.3/24`. See "Revision history" in the
  addendum below.
- **OXTS's final network home is its own dedicated/isolated `Vlan40` (`10.5.2.0/24`)**, shared
  with Hunter Sync Omni and Intrepid GigaStar, forming a self-contained CAN/RTK data-extraction
  network consumed by ETAS. This VLAN does **not** need to share a broadcast domain with the
  LiDAR NIC/VLAN20 — end users confirmed OXTS and LiDAR do not need to interoperate over the
  network. See "Revision history" below for the investigation that led here: OXTS was first
  placed on Vlan40 (routed), then temporarily moved into VLAN20's broadcast domain out of
  caution about the RT3000 v3's gateway handling, then moved back to a dedicated Vlan40 once
  a controlled test proved the RT3000 handles ARP and routing correctly — the real reason NCOM
  didn't cross VLANs is that **NCOM is UDP broadcast traffic, which never crosses a routed VLAN
  boundary by design**, regardless of whether the RT3000's gateway is configured correctly.
  This is a fundamental property of broadcast traffic, not an OXTS defect.
- TM Locator Data Service: UDP port `7372`, query bytes `0xA1 0x04 0xB2`, 80-byte response

## PTP timing chain — TM2000B → Cisco C9300L → Hesai LiDAR

The TM2000B, C9300L, and Hesai Pandar128 LiDARs all support both standard IEEE
1588-2008 PTP and IEEE 802.1AS (gPTP). **The Hesai "Frozen" status observed
during initial bring-up was root-caused to a missing `ptp enable` on the
physical LiDAR/TM2000B-facing switch interfaces** — see `CISCO_PTP.md` for the
full investigation and verified fix. Once `ptp enable` was applied to the
correct interfaces (`GigabitEthernet2/0/14`, `GigabitEthernet2/0/16`,
`GigabitEthernet2/0/26` on this switch), further testing established the
**final working configuration**:

- **Both Hesai LiDARs must have their own `Profile` set to `IEEE1588`.**
  Setting a LiDAR's own profile to `802.1AS` causes it to go `Frozen`, then
  `Free Run` after a reboot — it never (re-)acquires sync through this
  switch, because the switch's boundary clock re-originates PTP on every port
  as Default Profile over `udp-ipv4` (confirmed hard-locked via `ptp transport
  ?`, no pure-L2/Ethernet transport exists at the CLI level on this platform),
  and a genuinely 802.1AS-only LiDAR client does not recognize that traffic
  as valid gPTP (802.1AS requires raw L2 Ethernet frames, no IP/UDP header).
- **The TM2000B's own `Profile` must be set to `IEEE1588`** (`Packet Output =
  IPv4 UDP`, `Delay Mechanism = End to End`, `Transmission Method =
  Multicast`). **Do not set the TM2000B to `802.1AS`** — an earlier revision
  of this doc incorrectly claimed 802.1AS gave the tightest lock; this was
  disproven. In `802.1AS` mode the TM2000B transmits native Layer-2 gPTP
  frames the switch's `udp-ipv4`-only PTP engine cannot receive at all, so the
  switch silently stops hearing the TM2000B and BMCA elects whatever else is
  reachable over `udp-ipv4` instead — in this environment that turned out to
  be the **ETAS ES886**, whose own PTP clock free-runs to master when no
  other master is heard. The earlier "excellent lock" under 802.1AS was
  actually the LiDARs/switch locking to the ES886, not the TM2000B. See
  `CISCO_PTP.md` "ES886 masquerade" incident for the full investigation.
- The LiDAR's own profile setting does **not** need to match the TM2000B's —
  the switch normalizes/re-originates PTP for the LiDAR-facing ports
  regardless of the TM2000B's upstream profile (when it can receive it).

Before troubleshooting "Frozen"/"Free Run"/lock-quality issues on this switch,
confirm:
1. `show switch` to identify the actual active stack member number — this
   switch is a single-member unit at member `2`; do not assume `1/0/x`
   interface naming even if a phantom `1/0/x` interface appears to accept
   configuration commands without error.
2. `show interfaces status | include LIDAR|TIMEMACHINE` to find the correct
   physical ports before applying any PTP config.
3. `ptp enable` is applied per-interface on every LiDAR port and the
   TM2000B-facing port (it does not appear in `show run interface` output on
   this platform — verify with `show ptp port <interface>` instead).
4. PTP domain number matches across LiDAR, switch, and TM2000B (CLEVIR
   default: domain `0`).
5. Both LiDARs' own `Profile` setting is `IEEE1588`, and the TM2000B's
   `Profile` is **`IEEE1588`** (not `802.1AS` — see above). See `CISCO_PTP.md`
   for the full explanation of why 802.1AS cannot sync through this switch
   for either the LiDAR or the TM2000B.
6. Global delay mechanism is `ptp mode boundary delay-req` (End-to-End), not
   `pdelay-req` (Peer-to-Peer) — this has been observed to drift on its own
   and causes ports to hang at `Port state: UNCALIBRATED` with `Peer mean
   path delay(ns): 0` even when the TM2000B is correctly elected grandmaster.
   **The TM2000B also has its own independent delay-mechanism setting**
   (separate from its 802.1AS/IEEE1588 profile selection) and must be set to
   **End-to-End** to match the switch (confirmed correct at time of writing).
7. **Confirm `show ptp parent` → Grandmaster Clock Identity matches the
   TM2000B's known identity (`0xC:AE:7D:FF:FE:25:19:F6`)** — not the switch's
   own identity (`0x90:EB:50:FF:FE:46:DF:80`, self-elected) and not the ETAS
   ES886 (`0x0:60:34:FF:FE:1D:C3:47`, masquerading master). Only the
   TM2000B's identity means the switch is genuinely disciplined to it.
8. **Run `copy running-config startup-config` immediately after any verified
   PTP fix** — an unsaved config fully reverts on the next switch
   reboot/power-cycle, which has caused this exact issue (both `ptp enable`
   loss and delay-mechanism drift) to recur. See `CISCO_PTP.md` "Incident"
   section for the full recurrence writeup.



## Switch-side prerequisites (already configured on the shared switch; verify only)

1. `ip routing` must be enabled globally.
   ```
   show running-config | include ip routing
   ```
2. Vlan20 and Vlan30 SVIs must be `up/up` with the correct primary (non-secondary) addresses.
   ```
   show ip interface brief
   show running-config interface Vlan20
   show running-config interface Vlan30
   ```
3. `192.168.10.0/24` and `100.64.1.0/24` must both appear as directly connected (`C`) routes.
   ```
   show ip route
   ```
4. The TM2000B's physical port must be a member of VLAN 30 and show `connected`.
   ```
   show vlan brief
   show interfaces status
   ```

> **Recurring failure mode — check this first if the TM becomes unreachable after ANY
> switch-side Vlan20/Vlan30 SVI address change.** This exact issue has recurred multiple
> times in this project (on both DEV and bench PCs): a PC's **persistent** Windows route
> table silently keeps a stale gateway address (e.g. `100.64.20.254`) after the switch SVI is
> renumbered (e.g. to `100.64.1.177`), and/or a phantom persistent `0.0.0.0/0` default route
> gets created pointing at the LiDAR gateway, hijacking the real default gateway. Both leave
> `ping` and app traffic to the TM silently routed out the wrong interface (typically Wi-Fi)
> instead of the LiDAR NIC.
>
> **Use [`scripts/Set-LidarNetworkRoutes.ps1`](../scripts/Set-LidarNetworkRoutes.ps1) instead
> of typing `route` commands by hand.** It idempotently removes any stale/phantom routes and
> re-adds the single correct persistent route, then verifies reachability:
> ```powershell
> cd C:\DEV\CLEVIR\CLEVIR_INCA_7_5\scripts
> .\Set-LidarNetworkRoutes.ps1
> ```
> Pass `-LidarGatewayIp <new SVI address>` if the switch is renumbered again and the script's
> built-in default hasn't been updated to match yet. Re-run this any time
> `Find-NetRoute -RemoteIPAddress 192.168.10.20` shows the wrong interface/next hop, or any
> time the TM becomes unreachable after a switch change.

## Per-PC setup (required on every new PC connected to the LiDAR VLAN)

1. **Assign a unique static IP on the LiDAR NIC**, on the `100.64.1.0/24` subnet.
   - Do not reuse `100.64.1.2`/`100.64.1.3`, which are reserved for LiDAR 1/2 themselves, or
	 `100.64.1.177`, which is the Vlan20 SVI gateway.
   - Do **not** leave a secondary/duplicate address assigned from prior troubleshooting
	 (check with `Get-NetIPAddress -InterfaceAlias "LiDAR"`), including any leftover
	 `100.64.20.x` addresses from before this subnet was renumbered.

2. **Add the persistent route to the TM subnet via the Vlan20 SVI gateway.** Use
   [`scripts/Set-LidarNetworkRoutes.ps1`](../scripts/Set-LidarNetworkRoutes.ps1) (recommended
   — see callout above) rather than typing this by hand. If you need to do it manually:

   ```powershell
   route -p add 192.168.10.0 mask 255.255.255.0 100.64.1.177 metric 1
   ```

3. **Check for stray/duplicate persistent routes before and after adding the route.**
   This was the actual root cause of TM outages on both PCs multiple times in this
   investigation — not the switch.
   [`scripts/Set-LidarNetworkRoutes.ps1`](../scripts/Set-LidarNetworkRoutes.ps1) does this
   automatically; if checking manually:

   ```powershell
   route print -4 | Select-String "192.168.10|0.0.0.0"
   ```

   Watch for:
   - A persistent `0.0.0.0 0.0.0.0 100.64.1.177 Default` entry — this incorrectly overrides
	 the real default gateway and must be removed:
	 ```powershell
	 route delete 0.0.0.0 mask 0.0.0.0 100.64.1.177
	 ```
   - Duplicate `192.168.10.0/24` entries bound to a different interface (e.g. Wi-Fi) with a
	 worse metric — remove all matching entries and re-add only the correct one:
	 ```powershell
	 route delete 192.168.10.0
	 route -p add 192.168.10.0 mask 255.255.255.0 100.64.1.177 metric 1
	 ```
   - Any leftover route still pointing at an old gateway from before a prior subnet
	 renumbering (e.g. `100.64.20.254`) — remove it the same way (`route delete 192.168.10.0`,
	 repeated until `route print -4` no longer lists it).

4. **Verify routing selects the LiDAR NIC**, not Wi-Fi/Ethernet:
   ```powershell
   Find-NetRoute -RemoteIPAddress 192.168.10.20 | Select-Object InterfaceAlias,IPAddress,NextHop
   ```
   Expected: `InterfaceAlias = LiDAR`, `NextHop = 100.64.1.177`.

5. **Confirm reachability in stages** (clear ARP cache first to avoid stale entries):
   ```powershell
   arp -d 100.64.1.177
   arp -d 192.168.10.1
   arp -d 192.168.10.20
   ping 100.64.1.177 -n 4  # Vlan20 SVI (local subnet)
   ping 192.168.10.1 -n 4  # Vlan30 SVI (routed hop)
   ping 192.168.10.20 -n 4 # TM2000B itself
   ```
   All three should return 4/4 replies with sub-millisecond RTT (single-hop, same LAN).

6. **Confirm the TM Locator Data Service (UDP 7372) responds** — `Test-NetConnection -Port`
   will NOT work here since the service is UDP-only. Use a raw UDP probe instead:
   ```powershell
   $client = New-Object System.Net.Sockets.UdpClient
   $client.Client.ReceiveTimeout = 3000
   $endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Parse('192.168.10.20'), 7372)
   $query = [byte[]](0xA1,0x04,0xB2)
   $client.Send($query, $query.Length, $endpoint) | Out-Null
   try {
	   $remote = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)
	   $resp = $client.Receive([ref]$remote)
	   "Received $($resp.Length) bytes from $remote"
   } catch {
	   "No response: $($_.Exception.Message)"
   } finally {
	   $client.Close()
   }
   ```
   Expected: `Received 80 bytes from 192.168.10.20:7372`.

7. **OXTS is no longer on the LiDAR NIC's VLAN and does not need PC-side action on this VLAN20
   checklist.** Per the end users' final decision, OXTS Sync Omni, Hunter, and Intrepid
   GigaStar now live together on their own dedicated, isolated `Vlan40` (`10.5.2.0/24`) as a
   private CAN/RTK data-extraction network consumed by ETAS. This VLAN intentionally does not
   share a broadcast domain with the LiDAR NIC/VLAN20, so **no OXTS reachability or NCOM setup
   is expected or required from a VLAN20 PC**. A PC needing to reach OXTS/NCOM directly (e.g.
   for a dedicated capture PC) must have a NIC physically/logically on `Vlan40` — routing
   alone is not sufficient, because NCOM is UDP broadcast traffic that does not cross VLAN
   boundaries even when a valid route exists (confirmed: the switch can ARP-resolve and route
   to OXTS across VLANs, but NCOM broadcast frames still do not reach a VLAN20-only PC).

8. **Verify the app's runtime config, not just the repo config.**
   `GM_ResidentClient.ReadUserConfigFile()` copies the root `config.xml` into a per-user /
   output-directory config on first run, and reads that copy thereafter. If the TM's IP or
   port ever changes, update both:
   - Repo root `config.xml`
   - The actual runtime copy (e.g. `bin\x64\Debug\config.xml`, or the per-user config path
	 under `My.Application.Info.DirectoryPath`)

   If in doubt, delete the stale per-user copy so it's regenerated from the current root
   `config.xml` on next launch.

8. **Restart the application** so `TimeMachineTimeSyncProvider` re-initializes and
   `LidarHealthDetailForm` reflects the live status. Confirm the header reads:
   ```
   PTP: LOCKED (TimeMachine source)   Source: TimeMachine   NTP: Stable | Sync=0
   ```

## LiDAR multicast (IGMP snooping) prerequisites on Vlan20

**Confirmed root cause of "LiDAR shows NO COMMS / 0 packets" (2026-07-29):** the Hesai LiDAR
units stream point-cloud data as **multicast** UDP (`config.xml`
`<HesaiConfig><MulticastIpAddress>239.192.20.10</MulticastIpAddress>`, port `2368`), not
unicast. This is separate from each LiDAR's own **Control IP** (its unicast management
address on `100.64.1.0/24`, used for control-plane only) — do not confuse the two when
troubleshooting.

The app does **not** perform a socket-level IGMP join for this stream — it captures raw
frames directly off the NIC via a BPF filter (see `GM_ResidentClient.log`: `NIC opened, BPF:
udp and greater 100 and (src host ... or src host ...)`), and the LiDARs themselves do not
appear to generate IGMP membership reports that this switch's IGMP snooping recognizes as a
valid join for `239.192.20.10`. With **IGMP snooping enabled** on Vlan20 (the switch default),
this resulted in the switch having **zero entries** for `239.192.20.10` in
`show ip igmp snooping groups vlan 20` at all times — even with the app running and both
LiDARs transmitting — and the switch pruned the multicast stream from **every** port,
including the port the capturing PC itself was connected to. This is why Wireshark showed
zero `udp.port==2368` traffic and the LiDAR Health Detail form showed "NO COMMS" / 0 packets
for both LiDARs, even though basic unicast connectivity (Control IP web pages) worked fine.

1. **Check current IGMP snooping / group state on Vlan20:**
   ```
   show ip igmp snooping vlan 20
   show ip igmp snooping querier vlan 20
   show ip igmp snooping groups vlan 20
   ```
   - `show ip igmp snooping querier vlan 20` returning nothing means there is no active
     querier on the VLAN.
   - `show ip igmp snooping groups vlan 20` returning an empty table (or missing
     `239.192.20.10` specifically) while the app/LiDARs are running means the switch has
     never registered a join for the LiDAR multicast stream and **will prune it from every
     port**, including the capturing PC's own port.

2. **Fix: statically register the multicast group on the LiDAR NIC's switch port.** An IGMP
   querier alone did *not* resolve this (the LiDARs still never registered a recognized
   join). The switch **does** support static IGMP snooping group registration, but **only for
   a single interface per command** — `interface range` is rejected with `% Invalid input
   detected`:
   ```
   ! WRONG (rejected by this switch/IOS version):
   ip igmp snooping vlan 20 static 239.192.20.10 interface range Gi2/0/13 - 24

   ! CORRECT — one interface per command:
   configure terminal
   ip igmp snooping vlan 20 static 239.192.20.10 interface GigabitEthernet2/0/18
   end
   show ip igmp snooping groups vlan 20
   ```
   **Always confirm the actual current port before registering** — see the LiDAR 1/2
   addendum entry above. As of 2026-07-29 the DEV/bench PC's LiDAR NIC lands on `Gi2/0/18`,
   not the previously-assumed `Gi2/0/20`; registering the group on the wrong port silently
   fails (the group entry appears fine in the table, but no port list membership exists for
   the port the PC is actually on, so packets are still pruned there). If the LiDAR NIC's
   port ever changes, remove the stale registration and add a new one for the correct port:
   ```
   configure terminal
   no ip igmp snooping vlan 20 static 239.192.20.10 interface GigabitEthernet2/0/<old-port>
   ip igmp snooping vlan 20 static 239.192.20.10 interface GigabitEthernet2/0/<new-port>
   end
   ```
   This is the preferred fix over disabling snooping entirely, since it keeps IGMP snooping's
   pruning behavior active for all other multicast traffic on Vlan20 and only special-cases
   the one group/port that needs it. (Disabling snooping outright — `no ip igmp snooping vlan
   20` — was tested as a temporary fallback and does work, floods all multicast on the VLAN,
   and is a reasonable fallback if the static per-interface syntax is ever unavailable, but is
   no longer the recommended approach now that the correct static-registration syntax is
   known.)

   **Critical: always `write memory` after applying this.** This static registration lives in
   `running-config` only until saved; a switch reload (intentional or accidental) will wipe it
   from `startup-config` if it was never saved, exactly as happened on 2026-07-29 when the
   switch was inadvertently restarted and the registration had to be re-applied from scratch.
   ```
   write memory
   show startup-config | include ip igmp snooping vlan 20 static
   ```
   Confirm the command appears in `startup-config` output before considering this fix durable.

   Verify by running the app with both LiDARs connected and re-checking the LiDAR Health
   Detail form — expect `Status = Capturing`, `Integrity % ≈ 99.9+`, and `Last Packet` updating
   continuously (0s ago).

## Known non-issues (ruled out during this investigation)

- Cisco PTP (IEEE 1588) 319/320 traffic was present and unaffected by the switch; it is
  unrelated to TM Locator UDP 7372 reachability.
- No ACLs on the switch block VLAN 20 ↔ VLAN 30 traffic (only a management-plane ACL for
  web/DNS exists, unrelated to inter-VLAN routing).
- A persistent MAC-flap notification on VLAN 20 (`0600.0000.01d4` between `Gi2/0/14` and
  `Gi2/0/16`) was root-caused to both Pandar128E3X LiDAR units sharing the same vendor-fixed
  secondary `Customer MAC Address` (`06:00:00:00:01:D4`), confirmed via each LiDAR's own web
  UI. This is a fixed, duplicated vendor identifier rather than a media-converter or L2-loop
  issue, is likely benign/cosmetic, and does not affect TM reachability or LiDAR multicast
  delivery. Optionally suppress the repeated log entries with
  `no mac address-table notification mac-move` if the logging noise is undesirable.

## Addendum: Device IP Assignments

Values below are the physical/required addresses as documented in `config.xml` and this
checklist. Cells marked "*(per-PC)*" are intentionally left blank because they are assigned
uniquely per machine (see step 1 of "Per-PC setup" above) rather than being a fixed value.

> **Last verified against switch config after the LiDAR alignment-tool subnet renumbering.**
> The LiDAR alignment tool used broadly across the organization requires LiDAR 1/2 on
> `100.64.1.2/24` and `100.64.1.3/24`; Vlan20 was therefore renumbered from `100.64.20.0/24`
> to `100.64.1.0/24`, and OXTS was moved off Vlan20 onto its own dedicated `Vlan40`
> (`10.5.2.0/24`) to avoid an address-space conflict with the alignment-tool's required
> LiDAR addressing — see "Revision history" at the end of this section before relying on
> cached values elsewhere (scripts, TM web config, per-PC persistent routes, LiDAR/OXTS
> device web UIs, etc.).

| Device    | IP Address                    | Subnet Mask     | Gateway            |
|-----------|-------------------------------|-----------------|--------------------|
| LiDAR NIC (also Hesai `HostIpAddress`) | 100.64.1.8 | 255.255.255.0  | 100.64.1.177 (Vlan20 SVI) |
| ETAS NIC  | *(per-PC, on 192.168.40.0/24)* | 255.255.255.0  | 192.168.40.254 (Vlan10 SVI) |
| LiDAR 1   | 100.64.1.2                    | 255.255.255.0   | 100.64.1.177 (Vlan20 SVI) |
| LiDAR 2   | 100.64.1.3                    | 255.255.255.0   | 100.64.1.177 (Vlan20 SVI) |
| TM2000B   | 192.168.10.20                 | 255.255.255.0   | 192.168.10.254 (Vlan30 SVI) |
| OXTS      | 10.5.2.30                     | 255.255.255.0   | 10.5.2.1 (Vlan40 SVI) — isolated private VLAN, not reachable from VLAN20 |

Notes:
- **LiDAR NIC / Hesai `HostIpAddress`**: on the `100.64.1.0/24` VLAN 20 subnet used both to
  reach the TM2000B and as the Hesai SDK's host-side bind address for LiDAR control/data
  communication. Verified in code: `GM_ResidentClient.vb` reads `<HesaiConfig><HostIpAddress>`
  into `HesaiInterop`'s config struct, which passes it through unchanged to
  `HesaiWrapper.cpp`'s `param.input_param.host_ip_address` — an empty/missing value falls
  back to `"0.0.0.0"` (bind to any interface).

  **Standard for this project: set `HostIpAddress` explicitly to the PC's own LiDAR NIC
  address** (not a separate fixed device, and not left as `0.0.0.0`/blank), even though every
  PC uses the same VLAN20/`100.64.1.0/24` topology. This is a deliberate choice given this
  project's multi-homed PCs (LiDAR NIC + ETAS NIC on different VLANs): letting the socket
  bind to `0.0.0.0` leaves multicast group-join interface selection and outbound control
  traffic source-interface selection up to OS routing-table state at that moment, which this
  investigation showed can be unreliable (stray persistent routes, Wi-Fi vs. LiDAR NIC
  ambiguity — see the per-PC routing issues resolved earlier in this document). Explicit
  binding removes that ambiguity.

  The repo `config.xml` is checked in with `100.64.1.8` as the default `HostIpAddress`,
  matching this PC's LiDAR NIC address — this is now the single formal build for the
  foreseeable future, so no per-PC override is required. If an additional PC is ever added
  later, its `config.xml` (or per-user runtime copy) `HostIpAddress` must be set to match its
  own LiDAR NIC address, and it must use a unique host address on this subnet (see
  "Per-PC setup" step 1); do not reuse `100.64.1.2`/`100.64.1.3` (reserved for LiDAR 1/2) or
  `100.64.1.177` (Vlan20 SVI gateway). Renumbered from `100.64.20.0/24` on the LiDAR
  alignment-tool renumbering date (see revision history).
- **ETAS NIC**: dedicated adapter used for ETAS/INCA XCP communication. VLAN 10 (Cisco name
  `VLAN0010`, ETAS ports `Gi2/0/1`-`Gi2/0/11`) now carries `192.168.40.0/24` with SVI
  `192.168.40.254` — **this replaces the previously documented `10.0.10.0/24` / `10.0.10.1`**.
  No fixed host address is documented in `config.xml`; assign a unique per-PC address on
  `192.168.40.0/24` per the ETAS/INCA experiment hardware configuration.
- **LiDAR 1 / LiDAR 2**: Hesai LiDAR unit addresses, from `config.xml` `<LidarDevices>`
  (`Lidar id="1"` = FRONT, `Lidar id="2"` = REAR). **Required to be `100.64.1.2`/`100.64.1.3`
  by the LiDAR alignment tool**, which is used broadly across the organization and expects
  these exact addresses; renumbered from `100.64.20.14`/`100.64.20.15`. Remain on Vlan20 so
  the switch can distribute gPTP/802.1AS timing to them over the same L2 broadcast domain as
  the LiDAR NIC, without requiring a boundary-clock hop across VLANs. **The formal build PC's
  LiDAR NIC physically connects to switch port `Gi2/0/18`** (confirmed via
  `show mac address-table vlan 20`). **Always verify the actual current port** with
  `show mac address-table vlan 20` (look up the PC's LiDAR NIC MAC address, shown by
  `Get-NetAdapter -Name "*LiDAR*"` on the PC) before adding/moving any static IGMP
  registration — port assignments can shift between test sessions if cabling changes.
- **TM2000B**: from `config.xml` `<TimeMachineConfiguration><DeviceIp>`, reachable via VLAN 30
  (`192.168.10.0/24`). **The Vlan30 SVI address changed from `192.168.10.1` to
  `192.168.10.254`.** The TM2000B's own web configuration (System Settings > Gateway) must be
  updated to `192.168.10.254` to match, or it will lose its route out once the old `.1`
  address is removed/renumbered on the switch. Verify this on the TM before relying on
  reachability again.
- **OXTS**: from `config.xml` `<OxtsConfiguration><NcomIpAddress>` /
  `<OxtsCapture><IpAddress>`, address `10.5.2.30`. **Final design (per end-user decision):**
  OXTS Sync Omni, Hunter, and Intrepid GigaStar live together on a dedicated, isolated `Vlan40`
  (`10.5.2.0/24`, SVI `10.5.2.1/24`, OXTS's switch port `Gi2/0/38` an access port on VLAN40) as
  a private CAN/RTK data-extraction network for ETAS. It intentionally does **not** share a
  broadcast domain with the LiDAR NIC/VLAN20 — the end users confirmed LiDAR and OXTS do not
  need to interoperate. The switch can still ARP-resolve and route to OXTS across VLANs (this
  was verified), but that does not matter for NCOM/NAVdisplay use from VLAN20, because NCOM is
  UDP broadcast traffic and broadcast traffic never crosses a routed VLAN boundary — this,
  not a gateway/ARP problem, is why NAVdisplay on a VLAN20 PC cannot see NCOM once OXTS is
  isolated on Vlan40. A PC that needs live OXTS/NCOM data must have a NIC on Vlan40 itself.

  Earlier revisions of this document (see history below) explored keeping OXTS on the same
  VLAN20 broadcast domain as the LiDAR NIC via a secondary subnet, out of caution about the
  OXTS RT3000 v3's gateway handling. That workaround is now obsolete and has been superseded
  of the NIC's own IP subnet membership — the switch-side Vlan20 secondary subnet is what
  matters, not anything on the PC. Do not add a `10.5.2.x` address to any PC's LiDAR NIC.

  **Important: OXTS does not respond to ARP or ICMP (ping) at all**, confirmed by clearing and
  re-checking switch port counters on OXTS's port (`Gi2/0/38`) during a ping test -- inbound
  unicast frames from OXTS remained at zero throughout, even though the switch had already
  learned OXTS's MAC/IP by snooping its NCOM UDP broadcast traffic. **Update:** a later, more
  controlled test on the dedicated `Vlan40` design showed OXTS *does* answer ARP (`show arp
  vlan 40` learned its IP/MAC, and `show ip route` showed a valid directly-connected route) --
  the earlier zero-unicast-counter observation is now believed to have been an artifact of that
  particular test rather than a general property of the device. **The real reason a VLAN20 PC
  cannot see live OXTS/NCOM data when OXTS is on Vlan40 is that NCOM is UDP broadcast traffic,
  which never crosses a routed VLAN boundary**, regardless of whether ARP/routing succeeds.
  Validate OXTS reachability via NCOM traffic (OXTS NAVdisplay live data and command send/ack,
  or the app's `OxtsNcomCaptureDevice`) from a PC that is actually on the same VLAN as OXTS.

### Revision history

| Date       | Change                                                                 |
|------------|------------------------------------------------------------------------|
| (latest)   | **RESOLVED — Hesai LiDAR "Frozen"/"Free Run" PTP status.** Two findings, in order: (1) `ptp enable` was never applied to the real, physically-cabled LiDAR/TM2000B interfaces — all earlier config attempts targeted a phantom interface `GigabitEthernet1/0/20` which does not physically exist (`show switch` revealed stack member `1` has MAC `0000.0000.0000` and state `Provisioned`/not present; switch is a single active unit at member `2`). Real ports: `GigabitEthernet2/0/14` (LiDAR #1), `GigabitEthernet2/0/16` (LiDAR #2), `GigabitEthernet2/0/18` (PC LiDAR NIC), `GigabitEthernet2/0/26` (TM2000B). After applying `ptp enable` to `Gi2/0/14`, `Gi2/0/16`, and `Gi2/0/26`, both LiDARs locked **when each LiDAR's own `Profile` was `IEEE1588`**. (2) Setting a LiDAR's own `Profile` to `802.1AS` produced `Frozen`, then `Free Run` after a LiDAR reboot — root cause: this switch's PTP transport is hard-locked to `udp-ipv4` (confirmed via `ptp transport ?`, no pure-L2/Ethernet transport option exists), and `ptp mode boundary` terminates/re-originates PTP on every port as Default Profile regardless of what's attached, so a genuinely 802.1AS-only LiDAR client never receives traffic it recognizes as valid gPTP. **Final working configuration: both Hesai LiDARs set to `Profile = IEEE1588`; TM2000B set to `Profile = 802.1AS`** (gives tightest lock, single-digit ns offset on both LiDARs; TM2000B set to `IEEE1588` also works but with looser double/triple-digit ns offset). The LiDAR's own profile does not need to match the TM2000B's. See `CISCO_PTP.md` for the full investigation and verified commands. `Configure-HesaiPTP-8021AS.ps1` is retained in the repo for reference but should **not** be used against these LiDARs on this switch — use the existing `Configure-HesaiPTP.ps1` (IEEE1588) script for both LiDARs instead. |
| (later)    | **RECURRENCE AND SECOND FIX — both LiDARs found in Free Run again, independent of TM2000B profile.** Live diagnosis found two compounding problems, both from the switch config never having been saved: (1) `ptp enable` had been lost again on `Gi2/0/14` and `Gi2/0/16` (switch reboot/power-cycle reverted to the old `startup-config`); `Gi2/0/26` still had it, so the switch could still elect the TM2000B as grandmaster (`Steps Removed: 1`) but the LiDAR-facing ports were dark to PTP — fixed by re-applying `ptp enable` and this time saving with `copy running-config startup-config`. (2) The switch's global delay mechanism had drifted from `ptp mode boundary delay-req` (End-to-End, the original working baseline) to `pdelay-req` (Peer-to-Peer) — symptom was `Gi2/0/26` stuck at `Port state: UNCALIBRATED` with `Peer mean path delay(ns): 0` even after the grandmaster was correctly the TM2000B; fixed with `no ptp mode` then `ptp mode boundary delay-req`, then saved. After both fixes, both LiDARs achieved **Locked** status. See `CISCO_PTP.md` "Incident" section for the full writeup. **Lesson: always `copy running-config startup-config` immediately after any verified PTP fix on this switch.** |
| (correction) | **CORRECTED — "TM2000B=802.1AS gives tightest lock" was a false correlation ("ES886 masquerade").** Re-testing (switch=`pdelay-req`+TM2000B=802.1AS/Peer-to-Peer, matching the original "best lock" recipe) showed `show ptp parent` grandmaster identity was `0x0:60:34:FF:FE:1D:C3:47` — **not** the TM2000B (`0xC:AE:7D:FF:FE:25:19:F6`) — identified as the **ETAS ES886**, which free-runs to master when no other master is heard over `udp-ipv4`. Because the switch's PTP transport is hard-locked to `udp-ipv4` and the TM2000B in `802.1AS` mode transmits only native Layer-2 gPTP frames (confirmed via its UI: `PTP Destination MAC 01:1B:19:00:00:00`, `P2P Destination MAC 01:80:C2:00:00:0E`, no IP header), the switch never actually heard the TM2000B in any earlier 802.1AS test — it was locking to the ES886 instead, which explained the "excellent" but unpredictable lock quality. Reverting the switch to `ptp mode boundary delay-req` (End-to-End) with **TM2000B = IEEE1588 (`Packet Output = IPv4 UDP`, `Delay Mechanism = End to End`, `Transmission Method = Multicast`)** produced `show ptp parent` grandmaster = the TM2000B's real identity, and both LiDARs Locked. **TM2000B = IEEE1588 is now the confirmed, permanent setting; TM2000B = 802.1AS must not be used with this switch.** |
| (latest)   | **Final decision: OXTS moved back to a dedicated, isolated `Vlan40` (`10.5.2.0/24`) as its own private CAN/RTK network**,
| (latest)   | **Corrected the same-VLAN20 OXTS design
| (latest)   | **Moved OXTS off the routed `Vlan40` design onto a secondary `10.5.2.0/24` subnet on `Vlan20`**, placing it in the same L2 broadcast domain as the LiDAR NIC instead of relying on inter-VLAN routing. This was adopted after the OXTS RT3000 v3's ability to correctly use a gateway for return traffic could not be confirmed (local OXTS rep had no guidance on RT-side gateway configuration). Switch changes: `Vlan40` shut down/unassigned; `Vlan20` SVI gained a secondary address `10.5.2.1/24` alongside its existing primary `100.64.1.177/24`; OXTS physically moved to port `Gi2/0/38`, reconfigured as a VLAN20 access port (previously VLAN40). PC-side: LiDAR NIC given a secondary `10.5.2.x/24` address (e.g. DEV `10.5.2.8`) alongside its existing primary `100.64.1.x/24` address; no route/gateway configured for the `10.5.2.0/24` subnet since it is now a directly-connected secondary on the same NIC. `scripts/Set-LidarNetworkRoutes.ps1` updated to remove the now-stale OXTS routed-gateway repair logic (`OxtsSubnet`/`OxtsSubnetMask`/`OxtsDeviceIp` params and the corresponding `Repair-PersistentRoute` call removed) and replaced with a simple secondary-IP presence check. **Key troubleshooting finding**: OXTS does not respond to ARP or ICMP (ping) on this interface at all — confirmed via switch port counters on `Gi2/0/38` showing zero inbound unicast frames from OXTS during a ping test, both before and after this VLAN change — so `ping`/ARP must not be used to validate OXTS reachability; use NCOM traffic (OXTS NAVdisplay live data and command send/ack, or the app's `OxtsNcomCaptureDevice`) instead. This ping/ARP behavior was observed identically on both the prior routed `Vlan40` design and the current same-VLAN20 design, so it is considered a property of the OXTS device itself rather than a symptom of either topology. **Note: the PC-side secondary IP mentioned here was later found unnecessary — see the entry above.** |
| (latest)   | **Renumbered the LiDAR alignment subnet from `100.64.20.0/24` to `100.64.1.0/24`**
| (latest)   | **Corrected the Vlan20 gateway from `100.64.1.254` to `100.64.1.177`.** The initial renumbering above used `.254` to match the project's existing Vlan30/Vlan40 `.254`/`.1` gateway convention, but the LiDAR alignment tool has a **hard requirement** that the Vlan20 gateway be exactly `100.64.1.177`. This overrides the `.254` convention for Vlan20 specifically (Vlan30 and Vlan40 SVIs are unaffected and remain `.254`/`.1` respectively). Switch SVI `Vlan20` changed again from `100.64.1.254` to `100.64.1.177`; `scripts/Set-LidarNetworkRoutes.ps1` default `$LidarGatewayIp` and all gateway references in this document updated to `100.64.1.177` accordingly. Any PC route already configured for `.254` must be re-run through `Set-LidarNetworkRoutes.ps1` (or have its persistent route manually corrected) to pick up `.177`. |
| (initial)  | Vlan30 SVI `192.168.10.1`; Vlan10 SVI `10.0.10.1` (`10.0.10.0/24`).     |
| 2026-07-29 | Vlan30 SVI changed to `192.168.10.254`; Vlan10 SVI changed to `192.168.40.254` (`192.168.40.0/24`, replacing `10.0.10.0/24`). Any per-PC persistent static routes added under the "Per-PC setup" section that reference the old `195.0.0.254`/gateway pairing for `192.168.10.0/24` remain valid (Vlan20 SVI and route next-hop `195.0.0.254` did not change), but the TM2000B's own gateway setting and any ETAS-side host configuration must be updated to match the new SVI addresses above. |
| 2026-07-29 | LiDAR 1 (`10.5.55.14` -> `195.0.0.14`), LiDAR 2 (`10.5.55.15` -> `195.0.0.15`), OXTS (`10.5.55.200` -> `195.0.0.200`), and Hesai host NIC (`10.5.55.20` -> `195.0.0.20`) renumbered from the flat `10.5.55.0/24` subnet onto `195.0.0.0/24` (Vlan20), to place them in the same L2 broadcast domain as the LiDAR NIC (physically on switch port `Gi2/0/20`) in preparation for gPTP/802.1AS timing distribution from the TM2000B via the switch. `config.xml` updated accordingly (`<OxtsConfiguration>`, `<OxtsCapture>`, `<LidarIpAddress>`, `<LidarDevices><Lidar>`, `<HesaiConfig><HostIpAddress>`). Physical devices and PC-side NIC configuration must be updated to match before connectivity is restored; switch-side Vlan20 port assignments must also be verified for the ports these devices are cabled to. |
| 2026-07-29 | Vlan20 subnet renumbered from `195.0.0.0/24` (real public address space, at risk of proxy/WPAD auto-detection misclassification on Windows) to `100.64.20.0/24` (RFC 6598 Shared Address Space). Switch SVI `195.0.0.254` -> `100.64.20.254`; LiDAR NIC per-PC addresses (e.g. DEV `195.0.0.8` -> `100.64.20.8`, bench `195.0.0.9` -> `100.64.20.9`); LiDAR 1 `195.0.0.14` -> `100.64.20.14`; LiDAR 2 `195.0.0.15` -> `100.64.20.15`; OXTS `195.0.0.200` -> `100.64.20.200`; Hesai host NIC `195.0.0.20` -> `100.64.20.20`. `config.xml` updated accordingly. All per-PC persistent static routes and NIC gateway settings referencing the old `195.0.0.254` gateway must be updated to `100.64.20.254`; watch for leftover `195.0.0.x` addresses/routes during cleanup (see "Per-PC setup" steps 1 and 3). Note: `100.64.0.0/10` is reserved for carrier-grade NAT, not general LAN use, and is not universally guaranteed to bypass proxy/WPAD logic on every OS/security stack — if proxy issues persist, fall back to an RFC 1918 range (e.g. `10.0.20.0/24`, matching the switch's legacy numbering scheme). |
| 2026-07-29 | **Clarification, not a value change**: the "Hesai host NIC" row/notes above previously implied `HostIpAddress` was a separate fixed device address. Confirmed with the team that `<HesaiConfig><HostIpAddress>` is intended to be **the same address as the PC's own LiDAR NIC** (the adapter physically cabled to Vlan20), not a distinct device. The checked-in `config.xml` value (`100.64.20.20`) is only correct for the one PC assigned that address; every other PC must set its own `config.xml` (or per-user runtime copy) `HostIpAddress` to match its own LiDAR NIC's address. Also confirmed switch port mapping: TM2000B is on port `Gi2/0/26` (Vlan30), and OXTS was moved from Vlan40 onto Vlan20 alongside LiDAR 1/2. |
| 2026-07-29 | Corrected `config.xml` `<HesaiConfig><HostIpAddress>` from the placeholder `100.64.20.20` to `100.64.20.8` (DEV PC's actual LiDAR NIC address), per confirmed DEV/bench addressing (DEV = `.8`, Test Bench = `.9`). Bench PC must set its own runtime config copy to `100.64.20.9`. Also confirmed and fixed a recurrence of the stale-persistent-route issue on the DEV PC after the Vlan20 SVI renumbering: a phantom `0.0.0.0/0 via 100.64.20.254` route and a stale `192.168.10.0/24 via 195.0.0.254` route were both present, blocking TM reachability until removed and replaced with `192.168.10.0/24 via 100.64.20.254`. See the new callout at the top of "Per-PC setup" flagging this as a recurring failure mode to check after any switch SVI change. |
| 2026-07-29 | The same phantom `0.0.0.0/0 via 100.64.20.254` persistent route recurred independently on the bench PC (no `192.168.10.0/24` route was present at all there, unlike DEV). This confirms manually-typed `route -p add`/`route delete` commands are error-prone and this class of mistake will keep recurring after every switch renumbering. Added [`scripts/Set-LidarNetworkRoutes.ps1`](../scripts/Set-LidarNetworkRoutes.ps1), an idempotent script that removes any stale/phantom routes to the TM subnet and the LiDAR gateway, re-adds the single correct persistent route, and verifies reachability. This is now the recommended method for configuring/repairing per-PC routing instead of manual `route` commands; "Per-PC setup" steps 2-3 updated to reference it. |
| 2026-07-29 | **Root-caused and fixed the "LiDAR NO COMMS / 0 packets" issue** on DEV: both LiDARs were reachable via unicast (Control IP web pages) but produced zero multicast UDP traffic (`239.192.20.10:2368`) even with the app running and both LiDARs physically transmitting; the switch's `show ip igmp snooping groups vlan 20` never showed the group registered, meaning IGMP snooping pruned the stream from every port on Vlan20, including the capturing PC's own port. Confirmed via log inspection that the app captures LiDAR data via raw NIC/BPF capture (not a socket-level IGMP join), so no join was ever registered by the host side either. Initial attempt at a static group registration (`ip igmp snooping vlan 20 static 239.192.20.10 interface range Gi2/0/13 - 24`) failed with `% Invalid input detected`; this switch/IOS version rejects `interface range` in this command, not static registration itself. As a temporary fallback, disabled IGMP snooping entirely for Vlan20 (`no ip igmp snooping vlan 20`), which immediately restored LiDAR streaming, confirming the diagnosis. |
| 2026-07-29 | **Corrected the multicast fix to a targeted static registration** instead of disabling snooping: re-enabled `ip igmp snooping vlan 20`, then registered the group on a single interface (`ip igmp snooping vlan 20 static 239.192.20.10 interface GigabitEthernet2/0/20` — no `range` keyword). This initially appeared to work, but LiDAR traffic was lost again shortly after; `show mac address-table vlan 20` revealed the DEV PC's LiDAR NIC (MAC `5081.40fa.7ec1`) was actually learned on **`Gi2/0/18`**, not `Gi2/0/20` (which was `notconnect`) — the static registration had been applied to the wrong/stale port. Moved the registration to `Gi2/0/18` (`no ip igmp snooping vlan 20 static 239.192.20.10 interface GigabitEthernet2/0/20` then `... interface GigabitEthernet2/0/18`), after which both LiDARs immediately resumed `Capturing` at 100% integrity. Updated the "LiDAR multicast" section and the LiDAR 1/2 addendum entry to document `Gi2/0/18` as the confirmed current port and to recommend always verifying the actual port via `show mac address-table vlan 20` before registering/moving a static IGMP entry, since port assignments can shift between sessions. |
| 2026-07-29 | While validating the above, `Find-NetRoute -RemoteIPAddress 192.168.10.20` on DEV was found resolving via Wi-Fi again (the persistent-route problem recurring, independent of the LiDAR/IGMP issue). Running [`scripts/Set-LidarNetworkRoutes.ps1`](../scripts/Set-LidarNetworkRoutes.ps1) from a **non-elevated** PowerShell session produced a `route add ... requires elevation` error on the final add step, yet still resulted in a working route (a stale duplicate `192.168.10.0/24 via 100.64.20.254` entry happened to survive the delete loop) — this was luck, not a guaranteed outcome. Added an explicit administrator-privilege check at the top of the script that fails fast with a clear error message if not run elevated, instead of silently completing a partial/lucky fix. **The script must always be run from an elevated (Run as Administrator) PowerShell session.** |
| 2026-07-29 | The switch was inadvertently restarted (uptime reset to ~34 min), and the static IGMP registration (`ip igmp snooping vlan 20 static 239.192.20.10 interface GigabitEthernet2/0/18`) had to be re-applied — confirmed it was present in `running-config` but **not** in `startup-config`, meaning it would not have survived the restart if it hadn't already been present beforehand. Ran `write memory` and confirmed via `show startup-config \| include ip igmp snooping vlan 20 static` that the entry is now persisted and will survive future reloads. Updated the "LiDAR multicast" section above with a `write memory` step as a mandatory part of applying this fix. **Also discovered a separate, unresolved issue during this investigation**: `show logging \| include LINK\|FLAP` revealed a continuous MAC flap (`%SW_MATM-4-MACFLAP_NOTIF`) for host `0600.0000.01d4` in Vlan20, alternating between `Gi2/0/14` and `Gi2/0/16` roughly every 15 seconds, non-stop, since the restart. This is suspected to be the actual root cause of broader intermittent comms loss observed on both LiDAR (Vlan20) and ETAS (Vlan10) during the same session — sustained flapping can degrade switch-wide forwarding/CPU performance and destabilize IGMP snooping's port tracking, independent of any config correctness. **This is a priority open item**: identify what is physically connected to `Gi2/0/14` and `Gi2/0/16` (previously suspected to involve an Innomaker 1000Base-T1-TX media converter) and resolve the apparent L2 loop or redundant-link condition causing the flap. |
| 2026-07-29 | **Root-caused the MAC flap** flagged above: checked each Pandar128E3X LiDAR's own web UI "Device Info" page. Both units have unique real hardware MAC addresses (FRONT `EC:9F:0D:01:2F:CF`, REAR `EC:9F:0D:01:30:FE`), but **both report an identical secondary "Customer MAC Address": `06:00:00:00:01:D4`** — exactly matching the flapping MAC seen alternating between `Gi2/0/14` (FRONT) and `Gi2/0/16` (REAR). This is a fixed/vendor-assigned identifier baked into the Pandar128E3X firmware, not a media-converter issue as originally suspected (no converter is involved in causing this), and not a real L2 loop or cabling fault — it is expected switch behavior given that both units transmit an identical secondary MAC identity. No known way to change this "Customer MAC" on the units. Documented as likely benign in the "Open issues" section, with a recommendation to verify actual LiDAR data-stream health (not just the flap warning) before treating this as a real problem, and an optional `no mac address-table notification mac-move` step to silence the log noise if desired. |

## Open issues (not yet resolved)

- **Continuous MAC flap on Vlan20 — ROOT CAUSE CONFIRMED (2026-07-29), not an L2 loop.**
  `0600.0000.01d4` was flapping between `Gi2/0/14` and `Gi2/0/16` (LiDAR 1/FRONT and LiDAR
  2/REAR ports) roughly every 15 seconds. Checked each Pandar128E3X's own web UI "Device Info"
  page: each LiDAR has a unique, real hardware **MAC Address** (FRONT `EC:9F:0D:01:2F:CF`,
  REAR `EC:9F:0D:01:30:FE`), but **both units report an identical secondary "Customer MAC
  Address": `06:00:00:00:01:D4`** — this exactly matches the flapping MAC. This is a
  vendor-assigned/fixed identifier baked into the Pandar128E3X firmware (note the
  locally-administered-bit pattern, `06` as the first octet), not a per-unit unique address,
  and not related to any media converter as originally suspected. If this "Customer MAC"
  identity is transmitted on the wire by each unit (e.g., via an internal virtual
  interface/protocol frame bridged onto the physical port), the switch will legitimately see
  the same MAC arrive from two different ports and log it as flapping — this is expected,
  cosmetic switch behavior given how the LiDARs are built, **not** a real L2 loop or
  cabling/config fault, and there does not appear to be a way to change this "Customer MAC" on
  the units themselves.
  - **Confirm actual LiDAR data traffic is unaffected**: this flap is on a MAC that is
    separate from the real UDP data stream (multicast `239.192.20.10:2368`), which is what
    actually matters for the app. Verify via the LiDAR Health Detail form (`Status =
    Capturing`, 0 corrupted/dropped, `Integrity %` sustained near 100) over several minutes
    with both LiDARs live before concluding this flap is impacting real traffic.
  - **Optional: suppress the log noise** (does not change switching behavior, just silences
    the repeated log lines) if the constant `%SW_MATM-4-MACFLAP_NOTIF` messages are
    undesirable:
    ```
    configure terminal
    no mac address-table notification mac-move
    end
    write memory
    ```
  - If comms instability (LiDAR and/or ETAS) recurs even with this flap confirmed benign and
    LiDAR traffic verified healthy, look elsewhere for the cause (switch CPU load from an
    unrelated source, STP topology change, a different physical/link issue) rather than
    continuing to attribute it to this MAC flap.

