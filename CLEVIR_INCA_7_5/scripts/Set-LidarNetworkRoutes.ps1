<#
.SYNOPSIS
	Configures/repairs the persistent Windows route to the TM2000B subnet (192.168.10.0/24) via
	the LiDAR VLAN20 gateway on this PC, and removes known-bad phantom routes.

.DESCRIPTION
	This script exists because manually typing `route add`/`route delete` has repeatedly
	caused two distinct problems on both the DEV and bench PCs in this project:

	  1. A stale persistent route to a destination subnet pointing at an OLD Vlan20 gateway
		 address, left behind after the switch's Vlan20 SVI was renumbered.
	  2. A phantom persistent DEFAULT route (0.0.0.0/0) accidentally pointing at the
		 Vlan20 gateway, hijacking the PC's real default gateway (typically Wi-Fi).

	Both silently cause traffic to the TM2000B (192.168.10.20) to be routed out the wrong
	interface (typically Wi-Fi) instead of the LiDAR NIC, even though the switch itself routes
	the subnet correctly. Re-run this script any time the switch's Vlan20 SVI address changes,
	or any time `Find-NetRoute -RemoteIPAddress <device>` shows the wrong interface/next hop.

	NOTE: OXTS is intentionally NOT handled by this script. Per the end users' final decision,
	OXTS Sync Omni, Hunter, and Intrepid GigaStar live together on their own dedicated, isolated
	`Vlan40` (`10.5.2.0/24`) as a private CAN/RTK data-extraction network for ETAS -- separate
	from the LiDAR NIC/VLAN20 handled by this script. LiDAR and OXTS are confirmed not to need
	to interoperate over the network, so no OXTS-related route/address configuration is needed
	on a PC that only has a LiDAR NIC on VLAN20. A PC that needs live OXTS/NCOM data must have a
	NIC physically/logically on Vlan40 itself, since NCOM is UDP broadcast traffic that does not
	cross VLAN boundaries even when a valid route exists. See docs/TM2000B_Network_Setup.md for
	full context and the addendum IP table.

.PARAMETER LidarGatewayIp
	The current Vlan20 SVI address on the switch (the LiDAR NIC's default gateway).
	Defaults to the address documented in TM2000B_Network_Setup.md as of the LiDAR
	alignment-tool subnet renumbering to 100.64.1.0/24.

.PARAMETER TmSubnet
	The TM2000B's subnet (Vlan30). Defaults to 192.168.10.0.

.PARAMETER TmSubnetMask
	Subnet mask for the TM2000B subnet. Defaults to 255.255.255.0.

.PARAMETER TmDeviceIp
	The TM2000B's own IP address, used only for the final reachability test.
	Defaults to 192.168.10.20.

.EXAMPLE
	.\Set-LidarNetworkRoutes.ps1

	Repairs routing to the TM subnet using the documented defaults. Run this after any switch
	Vlan20/Vlan30 SVI change, or whenever the TM2000B becomes unreachable.

.EXAMPLE
	.\Set-LidarNetworkRoutes.ps1 -LidarGatewayIp 100.64.1.177

	Explicitly specify the current Vlan20 gateway (useful if the switch is renumbered again
	and this script's default hasn't been updated yet).
#>
[CmdletBinding()]
param(
	[string]$LidarGatewayIp = "100.64.1.177",
	[string]$TmSubnet = "192.168.10.0",
	[string]$TmSubnetMask = "255.255.255.0",
	[string]$TmDeviceIp = "192.168.10.20"
)

$ErrorActionPreference = "Stop"

function Write-Section($text) {
	Write-Host ""
	Write-Host "== $text ==" -ForegroundColor Cyan
}

# This script must run elevated: `route -p add`/`route delete` for persistent routes
# silently fail ("requires elevation") in a non-admin shell, which can leave stale
# duplicate routes in place and mask real failures. Fail loudly instead of continuing.
$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
	Write-Host "ERROR: This script must be run from an elevated (Run as Administrator) PowerShell session." -ForegroundColor Red
	Write-Host "Route changes require admin rights; re-launch PowerShell as Administrator and re-run this script." -ForegroundColor Red
	exit 1
}

function Repair-PersistentRoute {
	param(
		[string]$SubnetLabel,
		[string]$DestinationSubnet,
		[string]$DestinationSubnetMask,
		[string]$DestinationDeviceIp,
		[string]$GatewayIp
	)

	Write-Section "[$SubnetLabel] Current route state (before changes)"
	route print -4 | Select-String "$DestinationSubnet|0\.0\.0\.0"

	Write-Section "[$SubnetLabel] Removing stale/phantom routes"

	# Remove ANY existing persistent/active route(s) to the destination subnet, regardless of
	# gateway. route delete only removes one matching entry per call, so loop a few times to
	# be safe. NOTE: route.exe writes its "not found" message to stderr; with
	# $ErrorActionPreference = "Stop" that would otherwise be treated as a terminating error
	# via 2>&1, so it is suppressed locally around each native route call.
	$previousErrorActionPreference = $ErrorActionPreference
	$ErrorActionPreference = "Continue"
	for ($i = 0; $i -lt 5; $i++) {
		$result = route delete $DestinationSubnet 2>&1
		if ($result -match "element not found|not found") { break }
		Write-Host "  Removed a route to $DestinationSubnet"
	}

	# Remove a phantom persistent default route pointing at the LiDAR gateway, if present.
	# (This is the "0.0.0.0/0 via <LiDAR gateway>" mistake seen repeatedly in this project.)
	$defaultRouteResult = route delete 0.0.0.0 mask 0.0.0.0 $GatewayIp 2>&1
	if ($defaultRouteResult -notmatch "element not found|not found") {
		Write-Host "  Removed a phantom default route via $GatewayIp" -ForegroundColor Yellow
	}
	$ErrorActionPreference = $previousErrorActionPreference

	Write-Section "[$SubnetLabel] Adding correct persistent route"
	$previousErrorActionPreference = $ErrorActionPreference
	$ErrorActionPreference = "Continue"
	route -p add $DestinationSubnet mask $DestinationSubnetMask $GatewayIp metric 1
	$ErrorActionPreference = $previousErrorActionPreference
	if ($LASTEXITCODE -ne 0) {
		Write-Host "WARNING: 'route add' returned a non-zero exit code (commonly 'requires elevation' if not run as Administrator, or the route already existed). Verifying actual state below rather than trusting this alone." -ForegroundColor Yellow
	}

	Write-Section "[$SubnetLabel] Resulting route state"
	route print -4 | Select-String "$DestinationSubnet|0\.0\.0\.0"

	Write-Section "[$SubnetLabel] Route selection check"
	$netRoute = Find-NetRoute -RemoteIPAddress $DestinationDeviceIp -ErrorAction SilentlyContinue |
		Select-Object InterfaceAlias, IPAddress, NextHop
	$netRoute | Format-Table -AutoSize

	$usesLidarInterface = $netRoute | Where-Object { $_.InterfaceAlias -like "*LiDAR*" }
	if (-not $usesLidarInterface) {
		Write-Host "WARNING: Traffic to $DestinationDeviceIp is NOT routed via the LiDAR interface. Check the LiDAR NIC's own IP/gateway configuration and re-run this script." -ForegroundColor Red
	}

	Write-Section "[$SubnetLabel] Reachability test"
	Clear-DnsClientCache | Out-Null
	arp -d $GatewayIp 2>$null
	arp -d $DestinationDeviceIp 2>$null

	Write-Host "Pinging $DestinationDeviceIp..."
	$devicePing = Test-Connection -ComputerName $DestinationDeviceIp -Count 4 -ErrorAction SilentlyContinue

	# Re-check the route selection AFTER the ping, since a transient/racing route state during
	# the script's own changes can let a ping briefly succeed even when the persistent route is
	# actually wrong (this was observed on the bench PC: the script reported ping SUCCESS while
	# Find-NetRoute showed Wi-Fi, and an immediate manual ping afterward failed 100%). Never
	# report overall success unless BOTH the route selection and the ping agree.
	$netRouteAfterPing = Find-NetRoute -RemoteIPAddress $DestinationDeviceIp -ErrorAction SilentlyContinue |
		Select-Object InterfaceAlias, IPAddress, NextHop
	$usesLidarInterfaceAfterPing = $netRouteAfterPing | Where-Object { $_.InterfaceAlias -like "*LiDAR*" }

	if ($devicePing -and $devicePing.Count -gt 0 -and $usesLidarInterfaceAfterPing) {
		Write-Host "SUCCESS: $($devicePing.Count) of 4 replies received from $DestinationDeviceIp via the LiDAR interface." -ForegroundColor Green
	} elseif ($devicePing -and $devicePing.Count -gt 0 -and -not $usesLidarInterfaceAfterPing) {
		Write-Host "INCONSISTENT RESULT: $($devicePing.Count) of 4 ping replies were received from $DestinationDeviceIp, but the route selection is NOT using the LiDAR interface ($($netRouteAfterPing.InterfaceAlias -join ', ')). This ping result cannot be trusted — do not treat this as a fix. Re-run 'Find-NetRoute -RemoteIPAddress $DestinationDeviceIp' and a fresh 'ping $DestinationDeviceIp' manually to confirm actual current state; the persistent route may not have applied correctly to the LiDAR NIC (check that the LiDAR NIC has a valid $($GatewayIp.Substring(0, $GatewayIp.LastIndexOf('.'))).0/24 address assigned)." -ForegroundColor Red
	} else {
		Write-Host "FAILURE: No replies from $DestinationDeviceIp. Check switch-side SVI and port state (see docs/TM2000B_Network_Setup.md)." -ForegroundColor Red
	}
}

Repair-PersistentRoute -SubnetLabel "TM2000B (Vlan30)" -DestinationSubnet $TmSubnet `
	-DestinationSubnetMask $TmSubnetMask -DestinationDeviceIp $TmDeviceIp -GatewayIp $LidarGatewayIp

Write-Section "OXTS (out of scope for this script)"
Write-Host "OXTS/Hunter/Intrepid GigaStar now live on their own dedicated, isolated Vlan40"
Write-Host "(10.5.2.0/24), separate from the LiDAR NIC/VLAN20 handled by this script."
Write-Host "No route or address configuration is needed on this PC for OXTS."
Write-Host "See docs/TM2000B_Network_Setup.md for the current OXTS/Vlan40 network design."
