# 08-final-validation: Full solution build, test, and INCA interop smoke test

With both tiers upgraded, validate the solution as a whole. Build `CLEVIR_INCA_7_5.sln` end-to-end and confirm both in-scope projects (`PcapEventBridge.csproj`, `CLEVIR_INCA_7_5.vbproj`) target `net10.0`/`net10.0-windows` cleanly with zero errors and zero warnings. Because the ETAS INCA COM interop assemblies (`IncaCOM.dll`/`incacom`, `RCI2dotNet.dll`, `Interop.Scripting.dll`, `Etas.Base.ComSupport.dll`) are a critical, hard-to-mechanically-verify dependency, go beyond a metadata/build-level check: run a runtime smoke test that actually exercises INCA COM interop calls from the retargeted application, not just confirms the assemblies load. Confirm `CLEVIR_Installer.wixproj` (out of scope for retargeting itself) still packages the upgraded output correctly — no dedicated task is expected here since it is already SDK-style and untouched by the retargeting, but flag it for a follow-up if deployment assumptions (self-contained vs framework-dependent) need to change as a result of the `net10.0` move. Document any remaining deferred recommendations (e.g., binding redirect items intentionally retained, the `MarshalByRefObject` investigation outcome) in one place for the user.

**Done when**: the full solution builds with zero errors and zero warnings across both in-scope projects, existing automated tests (if any) pass, a runtime smoke test exercising INCA COM interop succeeds, and the installer's packaging is confirmed unaffected (or a follow-up is explicitly noted if not).

## Research Findings

### Attempt History
Two prior delegation attempts on this task did not produce usable work: attempt 1 was canceled
before any research/build started (see `progress-details.md` for that attempt's record); attempt 2
returned a truncated fragment with no `task.md` enrichment and no new `progress-details.md` (verified
on disk — `task.md` was still this stub, `git status` showed only bookkeeping changes). Per the
bounded-retry rule, this attempt proceeds **inline** rather than re-delegating a third time.

### Projects in Solution (`get_projects_in_solution`)
- `HesaiWrapper\HesaiWrapper\HesaiWrapper.vcxproj` — native C++ Hesai lidar wrapper. **Out of scope**
  for retargeting (not a .NET project); observe build status only.
- `PcapEventBridge\PcapEventBridge.csproj` — **in scope**. SDK-style, `net10.0`, retargeted in task 02.
- `..\CLEVIR_7.5 Installation\CLEVIR_Installer.wixproj` — WiX installer, SDK-style, untouched by
  retargeting. **Out of scope** for retargeting; observe/report status only per task wording.
- `CLEVIR_INCA_7_5.vbproj` — **in scope**. SDK-style (task 03), `net10.0-windows` (task 04),
  packages reconciled (task 05), binding redirects reviewed (task 06), crypto removed (task 07).

### Build Tool Decisions (from `scenario-instructions.md`, already validated across tasks 02-07)
- `PcapEventBridge.csproj`: `dotnet build` (SDK-style, net10.0-only, no COM/resx/designer resources).
- `CLEVIR_INCA_7_5.vbproj`: full-framework `MSBuild.exe` — NOT `dotnet build` — required because the
  project retains `COMReference` items (`ResolveComReference` unsupported on .NET Core MSBuild →
  `MSB4803`). Confirmed path: `C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe`.
  Re-confirmed post-task-04 net10.0-windows retarget across build_output_28-41.log.
- Solution-wide build (`.sln`) must also go through `MSBuild.exe` for the same COM-reference reason
  (`dotnet build` cannot resolve `HesaiWrapper.vcxproj` or `CLEVIR_INCA_7_5.vbproj`'s COM refs anyway).

### No Test Projects Exist
`discover_test_projects` was run across all 4 solution projects in an earlier task session and
returned none. The solution has zero test projects. "Existing automated tests (if any) pass" in
Done-When resolves to: no automated tests exist to run. Writing new tests is out of scope for a
validation task.

### `incaRci2.dll` — Confirmed Missing, Pre-Existing Gap, Not a Regression
Directly verified: `C:\DEV\CLEVIR\CLEVIR_INCA_7_5\bin\x64\Debug\incaRci2.dll` does not exist.
`GM_INCA_Comm.vb` (~lines 3173-3178) explicitly checks `File.Exists(dllPath)` for this DLL at INCA
init time and returns `INIT_UNSUCCESSFUL` with a clear error message if absent — this is existing,
correct error-handling behavior, not something the .NET 10 retarget broke. Never tracked in git
history; not present in `lib\Interop\` (which has `incacom.dll`, `Interop.Scripting.dll`,
`RCI2dotNet.dll` but not this one) or in the installed ETAS INCA7.5 SDK tree
(`C:\Program Files\ETAS\INCA7.5\cebra`, searched recursively). User is sourcing it from a backup
separately (raised in this same session). **Runtime smoke test scope adjustment**: test what IS
loadable today (`incacom.dll`, `RCI2dotNet.dll`, `Interop.Scripting.dll`) and document the
RCI2-dependent path as blocked pending backup restore — this is a legitimate "flag for user" outcome,
not a task failure.

### WiX Installer — Separately Diagnosed This Session (Not Yet Fixed)
User reported `WIX0103` missing-file errors in `CLEVIR_7.5 Installation\Product.wxs`. Investigated
and answered directly in chat already:
- Most originally-reported missing files (exe, NAudio DLLs, `incacom.dll`, `RCI2dotNet.dll`) already
  exist in `bin\x64\Debug` — that error log was stale, predating the task 07 rebuild.
- Genuinely missing (backup-restore items, never tracked in git): `incaRci2.dll` (above) plus 13
  config/reference files: `CLEVIR.ini`, `config.xml`, `AudioTotextConfig.xml`, `adminPCs.txt`,
  `Availability.txt`, `CANalyzerStartDelayTimeMsec.txt`, `CLEVIR_FeatureAccessForPATAC.txt`,
  `ClearCodesDelayTimes.txt`, `debug.txt`, `ReadMe.txt`, `TopDownViewConfig30.txt`,
  `VehicleConfig.txt`, `VALIDATION_DataDictionary.csv`. None have `<Content Include>` entries in
  `CLEVIR_INCA_7_5.vbproj` — even once restored, they need project wiring (not yet done, pending
  user confirmation of restore location).
- Two wiring gaps identified but **not yet fixed** (pending — separate from this task):
  `GM_ResidentClient_256px.ico` has `<Content Include>` but is missing `<CopyToOutputDirectory>`;
  `HesaiWrapper.dll` builds fine to repo-root `x64\Debug` and its `PostBuildEvent` only copies to
  `bin\x64\Debug` when built through the solution (`$(SolutionDir)` must resolve).
- **Decision for this task**: attempt the installer build as part of full-solution validation: if it
  fails on these already-known missing-file/wiring issues, do not attempt to fix them here (separate,
  pending user's backup files) — confirm the failure mode matches what's documented above (i.e., no
  NEW regression introduced by the .NET 10 retarget itself) and record it as an explicit deferred
  follow-up, exactly as the task's own wording anticipates.
- Do NOT modify `Product.wxs`, `CLEVIR_Installer.wixproj`, or `HesaiWrapper.vcxproj` in this task.

### Scope Clarification for "Zero Errors and Zero Warnings"
Applies specifically to the two **in-scope retargeted** projects (`PcapEventBridge.csproj`,
`CLEVIR_INCA_7_5.vbproj`). The native Hesai wrapper and WiX installer are pre-existing/out-of-scope
projects to observe, not to warning-sweep.

### Decomposition Assessment
Single concern (final validation gate), known scope (2 in-scope projects + 2 observe-only projects),
no internal decision points, no cross-task dependencies remaining. Executing as atomic — no
decomposition needed.

