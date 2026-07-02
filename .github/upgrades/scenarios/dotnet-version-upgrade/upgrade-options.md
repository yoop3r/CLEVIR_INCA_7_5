# Upgrade Options — CLEVIR_INCA_7_5

Assessment: 2 in-scope projects — `CLEVIR_INCA_7_5.vbproj` (VB.NET, WinForms, old-style, net48 → net10.0-windows) and `PcapEventBridge.csproj` (C#, SDK-style, net48 → net10.0) — with heavy WinForms/GDI+ usage, ETAS INCA COM interop, hand-authored assembly binding redirects, and no EF6/third-party logging/third-party DI container in use.

## Strategy

### Upgrade Strategy
Both in-scope projects currently target .NET Framework (net48) — crossing the Framework→modern .NET boundary with 2 projects fixes the strategy to Bottom-Up per the framework-migration override rule.

| Value | Description |
|-------|-------------|
| **Bottom-Up** (selected) | Upgrade the leaf-node library (`PcapEventBridge.csproj`) first and validate it builds standalone on net10.0, then upgrade `CLEVIR_INCA_7_5.vbproj` which depends on it. Fixed for Framework→modern migrations with 2+ projects — no alternative shown. |

## Project Structure

### Project Approach
`PcapEventBridge.csproj` is a class library with no `System.Web` dependency, and its only in-solution consumer (`CLEVIR_INCA_7_5.vbproj`) is migrating in the same upgrade — no .NET Framework dependents remain once the scenario completes.

| Value | Description |
|-------|-------------|
| **In-place** (selected) | Retarget `PcapEventBridge.csproj` directly from net48 to net10.0. Clean migration since its only consumer migrates in the same effort. |
| Multi-targeting | Add net10.0 alongside net48 (`net48;net10.0`) so the library keeps serving a Framework consumer during a longer transition — not needed since both projects migrate together. |

### Package Management
`CLEVIR_INCA_7_5.vbproj` is non-SDK-style (`ToolsVersion="15.0"`) and both projects cross the .NET Framework → modern .NET boundary — CPM adds migration friction during this transition rather than easing it.

| Value | Description |
|-------|-------------|
| **Per-Project (defer CPM to post-migration)** (selected) | Each project keeps its own package versions during the active migration. A deferred CPM recommendation is added to the final cleanup phase once both projects are SDK-style and settled on net10.0. |
| Central Package Management (CPM) | Create `Directory.Packages.props` now and centralize versions — better suited once all projects are already SDK-style and stable on a single TFM. |

## Compatibility

### Unsupported API Handling
Beyond the mechanical WinForms/GDI+ noise, the assessment surfaced genuine breaking API items: dead `System.Runtime.Remoting` lease code in `GM_INCA_Comm.vb` (no modern equivalent, already flagged as unused by the developer) and obsolete `RijndaelManaged`/`SHA512Managed` crypto calls in `EncryptDecrypt.vb` — a small count confined to one project, well below the deferral threshold.

| Value | Description |
|-------|-------------|
| **Fix Inline** (selected) | Resolve every flagged API change within the same task — delete the dead Remoting lease code and modernize crypto calls to `Aes.Create()`/`SHA512.Create()`. No deferred stubs or follow-up subtasks. |
| Defer Complex Changes | Stub complex API replacements to keep the project compiling and create follow-up resolution subtasks — reserved for >5 complex changes spread across multiple projects, not the case here. |

### Windows Native APIs
44,958 WinForms + 7,604 GDI+/`System.Drawing` references spread across 118 files, plus ETAS INCA COM interop assemblies (`IncaCOM.dll`, `RCI2dotNet.dll`, `Interop.Scripting.dll`, `Etas.Base.ComSupport.dll`) — pervasive Windows API usage with no cross-platform requirement for this hardware-bound automotive/LiDAR desktop application.

| Value | Description |
|-------|-------------|
| **Windows Compatibility Pack** (selected) | Add `Microsoft.Windows.Compatibility` to cover any remaining Windows-only APIs beyond core WinForms/GDI+. The app stays Windows-only, matching its hardware-bound nature. |
| No Compatibility Pack | Surface Windows API build errors immediately, forcing cross-platform alternatives — not warranted for a Windows-only hardware integration desktop app. |

## Modernization

### Assembly Binding Redirects
`app.config` contains 12 `assemblyBinding` redirects with hand-authored developer comments (e.g. `✅ FIXED: Match actual installed package versions`, `✅ NEW: Add binding for Newtonsoft.Json`), and the assessment separately flags 7 mandatory MSB3836 manual-vs-auto-generated conflicts — the volume (>10) and hand-authored nature warrant review rather than blind removal.

| Value | Description |
|-------|-------------|
| **Document and Review Before Removing** (selected) | Generate a report of all 12 redirects and their purposes before removal, since several were manually reconciled to fix real version conflicts. |
| Remove Binding Redirects | Remove all redirects immediately, relying on .NET's different assembly resolution — risks resurfacing the conflicts these redirects were manually added to fix. |
