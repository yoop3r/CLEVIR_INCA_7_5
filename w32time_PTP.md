1. Configure w32time to use the TM2000B as its only time source
Run in an elevated PowerShell (pwsh.exe as Administrator):

	w32tm /config /manualpeerlist:"10.5.55.10,0x8" /syncfromflags:manual /reliable:YES /update

Notes on that command:
	•	10.5.55.10,0x8 — the 0x8 flag tells w32time to use SpecialPollInterval (forces a fixed, tighter polling interval instead of the default adaptive 64s–1024s range) combined with marking it as a valid time source. If you 
	omit the flag Windows uses default 0x1 (client mode), which is fine too but polls less aggressively.
	•	/reliable:YES — only matters if this machine also acts as a time source for others on the domain/workgroup; harmless to set regardless.
	•	/syncfromflags:manual — tells w32time to only use the manual peer list (the TM2000B), not domain hierarchy or other sources.

2. Tighten the polling interval (registry) for better discipline
	
	By default Windows polls every 64s–1024s (VMICTimeProvider/NtpClient), which is too coarse for a good GPS clock. Tighten it:

	Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\Config' -Name 'MaxPollInterval' -Value 6 -Type DWord
	Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\Config' -Name 'MinPollInterval' -Value 4 -Type DWord
	Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\Config' -Name 'UpdateInterval' -Value 100 -Type DWord

	•	MinPollInterval/MaxPollInterval are log2(seconds) — 4 = 16s, 6 = 64s. This forces w32time to poll every 16–64 seconds instead of backing off to 1024s once "stable," which keeps the clock discipline loop much tighter for jitter-sensitive work.
	•	UpdateInterval controls how aggressively the local clock rate is adjusted per sample (lower = more responsive, but can be noisier — 100 is a reasonable middle ground down from the 300000 default... actually the default is already low; leave this one alone unless you see clock chasing/instability).

3. Disable VMICTimeProvider if this is a VM

	If the machine capturing LiDAR data is a VM (Hyper-V), the hypervisor's own time sync (VMIC) will fight with your NTP source and dominate:
	w32tm /config /syncfromflags:manual /reliable:YES
	Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\TimeProviders\VMICTimeProvider' -Name 'Enabled' -Value 0
	(Skip this if running on bare metal.)

4. Restart the service and force a resync
	Stop-Service w32time
	Start-Service w32time
	w32tm /resync /force

5. Verify sync status and accuracy

w32tm /query /status
w32tm /query /source
w32tm /stripchart /computer:10.5.55.10 /samples:10 /dataonly

	/stripchart will show you the actual offset in real time against the TM2000B — this is the number to watch. Expect low-single-digit milliseconds at best via NTP over Ethernet (Windows NTP client itself has resolution/scheduling limits around 1-15ms even with a perfect source), which is still a solid improvement over unsynced free-run drift, but won't reach PTP's sub-microsecond territory — consistent with what I mentioned earlier about Windows lacking a real PTP stack.

Relevant to your jitter thresholds
	
	Since the TM2000B's "PTP Config" tab exists too, if there's ever a NIC in that capture host with true PTP hardware timestamp support (Intel I210/I225 etc.), it would be worth pointing that NIC's driver at the TM2000B's PTP service directly (grandmaster) instead of/alongside NTP — that's the path to actually tightening your 45 ms PHASE_DRIFT threshold in LidarPcapCapture.vb significantly, since PTP hardware timestamping bypasses the OS scheduler jitter that NTP-over-software-clock can't avoid.
	
	
	
	PS C:\Windows\System32> Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\TimeProviders\PtpClient' -ErrorAction SilentlyContinue

Enabled                     : 1
InputProvider               : 1
DllName                     : C:\WINDOWS\System32\ptpprov.dll
PtpMasters                  : 10.5.55.10
DelayPollInterval           : 16000
AnnounceInterval            : 4000
EnableMulticastTx           : 1
UseE2ECorrection            : 1
HardwareTimestampingIfIndex : 21
MulticastRxEnabled          : 1
MulticastIfIndex            : 21
AllowedMasters              : 10.5.55.10
PSPath                      : Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\
                              W32Time\TimeProviders\PtpClient
PSParentPath                : Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\
                              W32Time\TimeProviders
PSChildName                 : PtpClient
PSDrive                     : HKLM
PSProvider                  : Microsoft.PowerShell.Core\Registry

PS C:\Windows\System32> Get-WinEvent -LogName 'Microsoft-Windows-Time-Service/Operational' -MaxEvents 50 | Where-Object {$_.Message -match "Ptp|1588|ptpprov"} | Format-Table TimeCreated, Id, Message -AutoSize

TimeCreated            Id Message
-----------            -- -------
7/21/2026 3:52:42 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 3:47:38 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 3:42:34 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 3:39:12 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 3:39:12 PM  260 W32time Service periodic configuration and status message…
7/21/2026 3:34:08 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 3:29:01 PM  257 W32time service has started at 2026-07-21T19:29:01.929Z (UTC), System Tick Count 8134875.…
7/21/2026 3:21:45 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 2:22:28 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 2:05:24 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 1:48:20 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 1:31:16 PM  263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 1:31:16 PM  260 W32time Service periodic configuration and status message…
7/21/2026 1:13:56 PM  257 W32time service has started at 2026-07-21T17:13:56.707Z (UTC), System Tick Count 19312.…
7/21/2026 1:09:30 PM  257 W32time service has started at 2026-07-21T17:09:30.945Z (UTC), System Tick Count 63750.…
7/21/2026 12:03:30 PM 263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 11:46:14 AM 263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 11:29:10 AM 263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 11:12:06 AM 263 W32time Service configuration parameters have been updated. This may impact the fine-grained…
7/21/2026 11:12:06 AM 260 W32time Service periodic configuration and status message…


PS C:\Windows\System32> Get-NetAdapter | Select-Object Name, InterfaceIndex, InterfaceDescription, Status

Name    InterfaceIndex InterfaceDescription                               Status
----    -------------- --------------------                               ------
Wi-Fi 2             15 Realtek 8832BU Wireless LAN WiFi 6 USB NIC         Up
ETAS                14 Intel(R) Ethernet Connection X722 for 10GBASE-T #2 Up
LiDAR                5 Intel(R) Ethernet Connection X722 for 10GBASE-T    Up

PS C:\Windows\System32> Get-WinEvent -ListLog "*Time*" | Select-Object LogName, RecordCount

LogName                                                     RecordCount
-------                                                     -----------
Microsoft-Windows-Time-Service/Operational                          829
Microsoft-Windows-Time-Service-PTP-Provider/PTP-Operational          47
Microsoft-Windows-PerceptionRuntime/Operational                       0
Microsoft-Windows-DateTimeControlPanel/Operational                    0
Microsoft-Windows-AppModel-Runtime/Admin                           1587

PS C:\Windows\System32> wevtutil sl "Microsoft-Windows-Time-Service-PTP/Operational" /e:true
Failed to read configuration for log Microsoft-Windows-Time-Service-PTP/Operational.
The specified channel could not be found.

PS C:\Windows\System32> wevtutil sl "Microsoft-Windows-Time-Service-PTP/Operational" /e:true
Failed to read configuration for log Microsoft-Windows-Time-Service-PTP/Operational.
The specified channel could not be found.
PS C:\Windows\System32> w32tm /query /status /verbose
Leap Indicator: 0(no warning)
Stratum: 1 (primary reference - syncd by radio clock)
Precision: -23 (119.209ns per tick)
Root Delay: 0.0000000s
Root Dispersion: 10.0000000s
ReferenceId: 0x4C4F434C (source name:  "LOCL")
Last Successful Sync Time: 7/21/2026 3:42:18 PM
Source: Local CMOS Clock
Poll Interval: 4 (16s)

Phase Offset: 0.0000000s
ClockRate: 0.0156250s
State Machine: 0 (Unset)
Time Source Flags: 0 (None)
Server Role: 576 (Reliable Time Service)
Last Sync Error: 1 (The computer did not resync because no time data was available.)
Time since Last Good Sync Time: 841.5511544s

Troubleshooting NTP Source Issues
If you intended to use NTP but are stuck on the Local CMOS Clock, the following steps may resolve the issue:

Check Firewall: Ensure UDP port 123 is allowed outbound in the Windows Firewall and any infrastructure firewalls. 
Reset Configuration: If the configuration is corrupted, reset the Windows Time service:

w32tm /unregister
w32tm /register
net stop w32time
net start w32time
w32tm /config /manualpeerlist:"10.5.55.10,0x8" /syncfromflags:manual /reliable:yes /update
w32tm /resync

Adjust Poll Intervals: Recent Windows updates may have altered poll intervals, causing sync failures. You can reset them to standard values:

Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\Config' -Name UpdateInterval -Value 0x64
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\Config' -Name MinPollInterval -Value 0x6
Set-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\Config' -Name MaxPollInterval -Value 0xa   

Domain Controller Specifics: On a Primary Domain Controller (PDC), the time source is typically the PDC Emulator in the domain. Ensure the PDC is correctly syncing with an external NTP server, as other domain members will sync from it. 



Windows does not support PTP (Precision Time Protocol) via the standard w32tm command-line tool, which is NTP-only. However, native PTP support exists in the OS kernel for specific versions, and third-party software is available for broader compatibility.

Native Windows PTP Support (Windows Server 2019 / Windows 10 v1809+)
Newer versions of Windows include a native PTP provider (ptpprov.dll). If this file exists in %windir%\system32\, you can enable PTP without external software. 

Verify Support: Check for C:\Windows\System32\ptpprov.dll. 

Enable via Registry:

Navigate to HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\W32Time\TimeProviders.
Create a new key named PtpClient.
Inside PtpClient, create a DWORD value named Enabled and set it to 1.
Create a DWORD value named DllName and set the data to ptpprov.dll.

Restart Service: Restart the W32Time service.

Hardware Requirements: The network interface card (NIC) must support NDIS packet timestamping.  Native Windows PTP strictly requires PTPv2 over UDP (ports 319/320) in End-to-End delay mode; it does not support Layer 2 (raw Ethernet) or Peer-to-Peer delay mechanisms.

Third-Party PTP Solutions
If your Windows version lacks native support or your hardware requires features like Layer 2 transport or Peer-to-Peer delay, you must use third-party software. These applications install their own services to synchronize the system clock. 

PTPSync: An open-source service specifically designed for Windows. It is ideal for environments using standard NICs without hardware timestamping, achieving microsecond accuracy via software timestamping. It includes a manager application for configuring network interface GUIDs. 

Domain Time II (Greyware): A commercial solution supporting PTPv2 (Unicast, Multicast, Hybrid) with fallback to NTP. It offers robust management tools and supports various profiles including Telecom (G.8275.1) and Power (IEEE C37.238). 

Meinberg PTP Client: A professional client supporting both software and hardware timestamping (with specific Oregano NICs on Windows). It supports a wide range of profiles (Default, Enterprise, Telecom, AVB/TSN) and works on Windows 7 through Server 2016/2019. 


This error is typically a Windows Installer permission or temporary directory issue rather than an actual lack of hard drive space.

1. Run as Administrator
Standard non-admin accounts often get blocked when the installer attempts to write to administrative system directories:

Right-click the .msi file.

Select Run as administrator (or open Command Prompt as Administrator, navigate to the download folder, and type msiexec /i RustDesk.msi).