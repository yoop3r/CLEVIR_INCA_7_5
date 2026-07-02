# 01-prerequisites: Verify toolchain and .NET 10 SDK readiness

Before any project changes, confirm the local toolchain supports a `net10.0`/`net10.0-windows` target. Neither in-scope project uses multi-targeting today, but `CLEVIR_INCA_7_5.vbproj` will move from an old-style (`ToolsVersion="15.0"`) project format to SDK-style as part of this upgrade, so the installed SDK and any `global.json` must support both the SDK-style tooling and `net10.0` before work begins.

**Done when**: the .NET 10 SDK is confirmed installed and usable; any `global.json` present is compatible with `net10.0` (or updated accordingly); no blocking toolchain issues remain.

## Research Findings

- **SDK installation**: `validate_dotnet_sdk_installation(targetFramework='net10.0')` returned **"Compatible SDK found"**. `dotnet --list-sdks` confirms two SDKs present: `10.0.100-rc.1.25451.107` and `10.0.301` (stable, satisfies `net10.0`).
- **global.json**: Searched the full repo via `file_search`, `grep_search`, and a recursive filesystem scan (`Get-ChildItem -Recurse -Filter "global.json"`). No `global.json` file exists anywhere in the repo — the only textual hits for the string are inside this scenario's own planning docs (`plan.md`, this `task.md`), not an actual file. No SDK pinning is in effect, so there is nothing to reconcile.
- **Blocking issues**: None. The installed SDK supports both `net10.0` (for `PcapEventBridge.csproj`) and `net10.0-windows` (for `CLEVIR_INCA_7_5.vbproj`), and also supports the SDK-style project tooling the VB project will need after its conversion in task `03-sdk-style-conversion`.
- **Decomposition assessment**: Single concern, verification-only, no code changes — executed as-is per task instructions, no decomposition needed.

