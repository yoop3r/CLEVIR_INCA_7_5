# .NET 10 Upgrade — Post-Completion Smoke Test

## Purpose

The `dotnet-version-upgrade` scenario is complete (8/8 tasks) and task 08 already validated a
clean build plus a live INCA COM activation smoke test. This document is a **second, deeper pass**
requested by the user before resuming any further modernization (WPF migration) work — it targets
every place the migration changed *runtime behavior*, not just compiled successfully, plus the one
area (installer) that has only ever been built, never actually installed and run.

This is a living checklist. Update the **Status** column as each item is exercised. Do not
recreate this file — edit it in place.

**Status legend**: ⬜ Not started · 🔄 In progress · ✅ Passed · ❌ Failed (see Notes) · ⏭️ Skipped (see Notes)

## Pre-requisites

- Baseline build is clean: full rebuild of `CLEVIR_INCA_7_5.vbproj` + `PcapEventBridge.csproj`
  via `MSBuild.exe /t:Rebuild /p:Platform=x64 /p:Configuration=Debug` → **0 errors, 0 warnings**
  (re-confirmed 2026-07-04 after fixing a `BC42314` regression introduced by the WPF pilot commit).
- Working tree is clean except for `InitForm.vb`, `Settings.Designer.vb`,
  `PcapEventBridge.csproj`, and `ExitAppFormWpf.xaml.vb` (all reviewed/kept, see
  `scenario-instructions.md` 2026-07-04 entries) — commit these alongside or before this pass.
- Hardware/environment needed depending on section: ETAS INCA7.5 installed
  (`C:\Program Files\ETAS\INCA7.5\`), a vehicle/bench connection for full INCA sessions (optional —
  COM activation alone is a valid partial test per task 08's precedent), LiDAR/OXTS hardware or a
  loopback/simulator if available, a USB flash drive, and access to a machine without the dev
  environment for the installer test (or at least a non-`bin\` folder to install into).

---

## 1. RCI2 / `incaRci2.dll` interop — never tested end-to-end until now

**Why it's here**: task 08 explicitly could not test this path because `incaRci2.dll` was missing
at the time. It was restored afterward (WiX installer fix pass). This is now the single biggest
piece of unverified interop surface from the whole migration — **and per the user, the one item
that determines whether the rest of the migration was worthwhile.**

**2026-07-04 pre-flight investigation (code/binary-level, no hardware needed)** — done ahead of
live testing to catch any architectural landmine before spending bench time:

- **`RCI2dotNet.dll`** (the managed wrapper, `lib\Interop\RCI2dotNet.dll`, referenced at
  `Version=7.3.0.21245`) is **pure IL, not mixed-mode C++/CLI** — confirmed by successfully
  reflection-loading it directly under a CoreCLR host (PowerShell 7.5.4 / .NET 9.0.10 runtime,
  same runtime family as .NET 10) via `Assembly.LoadFile` and enumerating all its types with
  `GetTypes()`. Mixed-mode assemblies throw `BadImageFormatException` immediately on CoreCLR —
  this one didn't. `ImageRuntimeVersion` reports `v4.0.30319` (built for .NET Framework), but that's
  just a metadata marker, not a compatibility blocker for pure-IL assemblies.
- Its `RCI2` class has exactly **one constructor** (`Sub New(path As String)`) and 8 public methods
  (`IncaGetRecords`, `IncaAddMeasureElement`, `IncaDeleteMeasureElement`, `IncaGetMeasureValue`,
  `IncaSetMeasureReadMode`, `IncaResetRecords`, `IncaGetLastError`, `IncaSetExperimentDispatch`) —
  all backed by nested delegate types (`D_IncaGetRecords`, etc.), the classic
  `Marshal.GetDelegateForFunctionPointer` / `GetProcAddress` pattern. This is **not COM interop** —
  no `COMReference`, no CoClass — it's a native-export P/Invoke-style binding, architecturally
  simpler and lower-risk than the `IncaCOM`/`Etas.Base.ComSupport` COM path already proven in task 08.
- **`incaRci2.dll`** (the real native/runtime dependency it wraps, repo root + `bin\x64\Debug\`,
  version `20.7503.0.210`) is **x64, PE32+**, and — surprisingly for a file with a "native-sounding"
  name — **carries a CLR header** (COR20 metadata RVA `0xFB000`, non-zero), meaning it's itself a
  mixed-mode or managed-with-native-exports binary. This raised the mixed-mode risk flag, so it
  was tested directly rather than assumed.
- **Constructed the real `RCI2` object against the real `incaRci2.dll`** (`New RCI2("...\incaRci2.dll")`)
  in an isolated CoreCLR process (PowerShell 7 background job, 15s timeout guard in case native init
  blocked/hung waiting on a device handshake) — **construction succeeded** with no exception and no
  timeout. This proves the exact code path at `GM_INCA_Comm.vb` line ~2347
  (`rci2 = New RCI2(Path.Combine(My.Application.Info.DirectoryPath, "incaRci2.dll"))`) is
  CoreCLR-safe at the load-and-construct level, fully outside the app and without a live INCA
  session running.
- **What this does NOT prove**: whether the 8 wrapped methods correctly marshal data to/from a
  *live* INCA session (real raster/signal registration, measurement values, records). Construction
  succeeding means the delegate-binding/native-load machinery works; it says nothing about
  protocol-level correctness once real INCA traffic flows. §1.2-§1.4 below are still required.

| # | Test | Expected result | Status | Notes |
|---|---|---|---|---|
| 1.0 | Pre-flight: reflection-load `RCI2dotNet.dll` under CoreCLR + construct real `RCI2` object against real `incaRci2.dll`, outside the app | Assembly loads (proves pure-IL, not mixed-mode), constructor succeeds with no exception/hang | ✅ | Done 2026-07-04 via isolated PowerShell 7 (CoreCLR) job with timeout guard. See investigation notes above. De-risks §1.2 significantly — the identical constructor call now has a proven-safe precedent. |
| 1.1 | Confirm `incaRci2.dll` present at `bin\x64\Debug\incaRci2.dll` after a fresh build | File exists (already confirmed on disk during investigation) | ✅ | Confirmed present 2026-07-04. |
| 1.2 | Launch the app, proceed through login far enough for `GM_INCA_Comm.vb` to construct `New RCI2(Path.Combine(My.Application.Info.DirectoryPath, "incaRci2.dll"))` (`GM_INCA_Comm.vb` line ~2347 area) | No exception; `rci2` is a live, non-null object | ⬜ | Same constructor call as §1.0, now inside the real app process/AppDomain rather than an isolated PowerShell job — still worth confirming no app-specific context (e.g. STA thread, working directory, other loaded assemblies) changes the outcome. Risk substantially lowered by §1.0. |
| 1.3 | Exercise a real RCI2-backed call — e.g. whatever CLEVIR screen calls `rci2.IncaGetMeasureValue(...)` / `rci2.IncaGetRecords(...)` (`GM_INCA_Comm.vb` lines ~1955, ~1995, ~2051) | Returns real measurement data matching what the pre-upgrade (net48) build would have returned | ⬜ | **This is the test that actually matters most** — needs a live INCA session with actual raster/signal config. §1.0 only proves the object constructs; it does not touch these delegate-bound methods at all. Nothing so far exercises real data marshaling across the native boundary. |
| 1.4 | Exercise `RCI2_CleanUp()` (`GM_INCA_Comm.vb` line 1876) — normal app shutdown path | Clean teardown, `rci2 = Nothing`, no exception, no orphaned INCA state | ⬜ | Pairs with the `MinimizeInca()`/process-handling fix kept in `InitForm.vb` — see §5. |
| 1.5 | Test the **absence** path too: temporarily rename `incaRci2.dll` and confirm the existing `File.Exists` guard in `GM_INCA_Comm.vb` degrades gracefully (`INIT_UNSUCCESSFUL`, no crash) | App handles missing DLL gracefully, as documented in task 08 | ⬜ | Regression-proof for the fallback path; restore the file afterward. |

---

## 2. Crypto removal — flash-drive file transfer now copies plaintext

**Why it's here**: task 07 removed `Encrypt()`/`Decrypt()` entirely. `TriggerEncryptAndCopy` now
calls `CopyFileToDDrive` directly. This changes the on-disk format of every file the app transfers
to a flash drive going forward — worth confirming the *receiving* side (whatever consumes these
files downstream) still accepts plaintext instead of `.encrypt`.

| # | Test | Expected result | Status | Notes |
|---|---|---|---|---|
| 2.1 | Insert a flash drive, trigger `HandleBackgroundEncryption()` path (normal `UsingFlashDrive` operation) | Files copy to the flash drive as plaintext (no `.encrypt` extension/content), matching existing `.log`-file precedent | ⬜ | `EncryptDecrypt.vb` / `GM_ResidentClient.vb`. |
| 2.2 | Confirm local source file is **not deleted** after copy (matches `.log` precedent, explicit decision from task 07) | Original file remains on the local machine after transfer | ⬜ | |
| 2.3 | Confirm the dead decrypt UI entries are actually gone from `VehicleStatDashboard.vb` at runtime (not just compiled out) | "Decrypt File...", "Decrypt and Delete Files on Q..." menu items are absent from the running app's menu | ⬜ | Visual confirmation — designer wiring was hand-edited across 2 files in task 07. |
| 2.4 | If any downstream tooling/process consumes the flash-drive files expecting `.encrypt` format, confirm it's been updated or the plaintext format is acceptable | No downstream breakage | ⬜ | This is outside the repo's control — flag to user if such a downstream consumer exists. |

---

## 3. LiDAR / OXTS thread shutdown — `Thread.Abort()` → cooperative cancellation

**Why it's here**: `Thread.Abort()` throws `PlatformNotSupportedException` on modern .NET, so task
04 replaced it with cooperative cancellation (`CancellationTokenSource` + `Thread.Join`). This is a
genuine behavioral change to how capture threads stop, not a mechanical port — worth confirming
capture start/stop is still prompt and clean with real (or simulated) hardware.

| # | Test | Expected result | Status | Notes |
|---|---|---|---|---|
| 3.1 | Start LiDAR capture, then stop it via the normal UI path | `LidarDevice.vb`'s `_captureThread.Join(5000)` returns promptly (thread exits cooperatively within 5s), no hang, no orphaned thread | ⬜ | `LidarDevice.vb` lines ~843, ~1035. |
| 3.2 | Start OXTS/NCOM capture, then stop it | `OxtsNcomCaptureDevice.vb`'s `_captureThread.Join(TimeSpan.FromSeconds(3))` completes cleanly | ⬜ | `OxtsNcomCaptureDevice.vb` line ~249. |
| 3.3 | Stop the OXTS listener thread | `OxtsNcomInterface.vb`'s `_listenerThread.Join(2000)` completes cleanly | ⬜ | `OxtsNcomInterface.vb` line ~260. |
| 3.4 | Exercise the app-level "killer thread" shutdown path (used during app exit) | `GM_ResidentClient.vb`'s `_MyKillerThread.Join(2000)` completes without hang | ⬜ | Line ~711 — ties into general app-exit testing (§6). |
| 3.5 | Force a **slow-to-respond** capture stop (if reproducible) — confirm the 3-5s timeout paths log/handle a timeout sensibly rather than silently hanging forever | Timeout path is handled gracefully, not a silent deadlock | ⬜ | Edge case — lower priority, only if hardware allows reproducing it. |
| 3.6 | Confirm `TimeMachineTimeSyncProvider.vb`'s `_workerThread.Join(1500)` shuts down cleanly if that sync feature is exercised | Clean shutdown | ⬜ | Lower priority if this feature isn't in active use. |

---

## 4. New `Dispose()` calls (CA2213 cleanup) — 12+ fields across multiple forms

**Why it's here**: task 04 added real `Dispose(disposing)` overrides that now actively dispose
objects that previously leaked until GC finalization. The risk isn't the disposal itself — it's
**use-after-dispose**: any code path that touches one of these fields *after* its owning form
starts closing could now throw `ObjectDisposedException` where it silently worked before (leaked
object, but still usable).

| # | Test | Expected result | Status | Notes |
|---|---|---|---|---|
| 4.1 | Close the main `GM_ResidentClient` form normally (not via Task Manager/force-kill) while `MyOxtsInterface`, `MyTdGraphicsContainer`, `MyMainTabControl`, `MyLogin`, `_MyUploadData`, `_MyRecordPlayback` are in their normal idle state | Clean close, no `ObjectDisposedException`, no hang | ⬜ | `GM_ResidentClient.designer.vb` — the largest single cluster (12 fields, 44 warning instances fixed). |
| 4.2 | Close the main form **while a recording is in progress** (`_recordingMonitorCts`) | Recording stops cleanly, then disposal proceeds without exception | ⬜ | Higher-risk timing case — cancellation + dispose racing against an active background loop. |
| 4.3 | Close the main form **while background tasks are running** (`_backgroundTasksCts`) | Background loop observes cancellation and exits before/without conflicting with disposal | ⬜ | `GM_ResidentClient.vb` lines ~551-583, ~9529, ~9563 — `Task.Delay` with the token is the key pattern to verify doesn't throw unexpectedly. |
| 4.4 | Open and close `GridDataClass`, `TD_TargetObjectsClass` dialogs (newly got their first-ever `Dispose` override) | Clean open/close cycle, repeatable without leaks or exceptions | ⬜ | These classes had **no** prior `Dispose` override at all — highest-novelty item in this section. |
| 4.5 | Open and close the login form multiple times in one session, then close the app | `LoginForm.designer.vb`'s extended disposal (runtime-created `_loginSubmitButton`) doesn't throw on repeated open/close | ⬜ | |
| 4.6 | Trigger a toast/status notification (`StatusNotifier.vb`'s `ToastForm`), let it fade normally, then separately close it early/manually before its timers fire | Both paths dispose `_lifetimeTimer`/`_fadeTimer` without double-dispose issues (`Timer.Dispose()` is idempotent per task 04's note, but worth confirming empirically) | ⬜ | |
| 4.7 | Trigger the init splash screen (`InitProgressSplash.vb`) during normal app startup | Splash closes cleanly, runtime-created `_label`/`_progress`/`_cancel` controls dispose without exception | ⬜ | |
| 4.8 | Exercise `LidarHealthDetailForm` open/close, including via its `FormClosing` path | `_refreshTimer` stops and disposes cleanly (extended override, not brand new) | ⬜ | |

---

## 5. WNet drive-mapping removal — UNC-only upload path now

**Why it's here**: `WNetAddConnection2`/`WNetCancelConnection2` and `MapDrive`/`UnMapDrive` were
deleted entirely (not just warning-fixed) per your explicit decision. The upload path now relies
solely on `Directory.Exists(NetworkDriveMapping)` against a UNC path, with no drive-letter mapping
fallback.

| # | Test | Expected result | Status | Notes |
|---|---|---|---|---|
| 5.1 | With normal network connectivity, confirm data upload finds the network share via UNC path directly | Upload proceeds, `Directory.Exists(NetworkDriveMapping)` resolves true | ⬜ | `CommonWirelessFunctions.vb` lines ~184, ~223, ~356, ~376, ~388. |
| 5.2 | With the network share unreachable, confirm the app reports "Could not find Data Upload Capability" cleanly instead of attempting (and failing at) a drive-letter mapping | Graceful degraded-mode message, no exception, no attempt to call removed WNet functions | ⬜ | This is the actual behavior-change case — confirm nothing regressed silently. |
| 5.3 | Confirm `UploadDataScreen.vb`'s `VerifyNetworkMapping()` reports the drive-unavailable condition directly (per task 04's change) | No auto-map attempt; direct, clear unavailability message | ⬜ | |
| 5.4 | If this environment/site previously relied on drive-letter mapping (rather than direct UNC access) for any reason, confirm with site IT/ops that UNC-only access is actually viable here | No environment-specific regression | ⬜ | This is a real question for your deployment environment, not just code — flag if uncertain. |

---

## 6. Installer — built but never actually installed

**Why it's here**: task 08 validated that `CLEVIR_Installer.wixproj` **builds** a fresh MSI
(`CLEVIR_7.5 Installation\bin\Debug\CLEVIR_7.5_Setup.msi`, confirmed present on disk,
2026-07-04 14:01). Nobody has actually **run** that MSI and confirmed the installed app launches.
This is the highest real-world-risk untested item — a clean build proves packaging *logic* is
correct, not that the installed bits actually run standalone (outside the dev `bin\` folder, with
whatever prerequisites a real target machine has or lacks).

| # | Test | Expected result | Status | Notes |
|---|---|---|---|---|
| 6.1 | Run `CLEVIR_7.5_Setup.msi` on a clean-ish machine or VM (or at minimum, a machine without the dev `bin\x64\Debug` folder in play) | Installer completes without error | ⬜ | Ideally a machine that mirrors a real deployment target (has ETAS INCA7.5 installed, does not have Visual Studio/dev tooling). |
| 6.2 | Launch the installed app from its installed location (not from the repo's `bin\` folder) | App starts, reaches `LoginForm`/main shell without missing-file errors | ⬜ | This is the real test of whether all `<Content Include>` items (including the 13 files restored from backup) actually get packaged, not just present in the dev output folder. |
| 6.3 | From the installed instance, confirm INCA COM interop still activates (repeat of task 08's smoke test, but from the installed path) | `Activator.CreateInstance`-equivalent real usage succeeds | ⬜ | Confirms the installed app resolves `IncaCOM.DLL` from the ETAS install tree correctly, same as the dev-folder test in task 08. |
| 6.4 | Confirm `HesaiWrapper.dll` is present and loadable from the installed location | No missing-native-DLL error | ⬜ | Called out in task 08 as only reliably copied when built through the solution — worth confirming the installer's own packaging step (not the build's post-build event) actually includes it. |
| 6.5 | Uninstall and reinstall once, to confirm no leftover-state issues | Clean uninstall/reinstall cycle | ⬜ | Lower priority — nice-to-have, not blocking. |

---

## 7. General regression pass (build/tooling changes, lower individual risk but broad surface)

| # | Test | Expected result | Status | Notes |
|---|---|---|---|---|
| 7.1 | Full normal user session: launch → login → typical vehicle test workflow → exit | No crashes, no unexpected dialogs, behavior matches pre-upgrade app from the user's own recollection | ⬜ | This is the broadest, most valuable single test — a real end-to-end session. |
| 7.2 | Exercise the exit flow via the new WPF `ExitAppFormWpf` dialog (all 4+ button options) | Each exit option behaves identically to the original WinForms `ExitAppForm` (already reviewed once, but worth confirming post-`BC42314`-fix rebuild) | ⬜ | Only WPF surface currently in the app. |
| 7.3 | Confirm binding-redirect trim (12→3 entries in `app.config`) causes no `FileLoadException` anywhere during a full session | No assembly-load errors | ⬜ | Task 06 confirmed this is inert on CoreCLR via build-time testing only; a live full-session run is the real proof. |
| 7.4 | Confirm `System.Speech`/`VoiceRecognitionClass.vb` voice recognition still works if that feature is in active use | Speech recognition functions as before | ⬜ | Flagged in assessment as a package-swap (compatible modern `System.Speech` package), not code-level change — but never explicitly smoke-tested. |
| 7.5 | Confirm HesaiWrapper native interop (`HesaiInterop.vb`) still functions if Hesai LiDAR hardware is available | Native interop calls succeed | ⬜ | `HesaiWrapper.vcxproj` itself was out of scope for the .NET retarget (native C++), but the managed-side P/Invoke wrapper (`NativeMethods` relocation in task 04) is worth a live check. |

---

## Summary tracking

| Section | Total items | Passed | Failed | Not started |
|---|---:|---:|---:|---:|
| 1. RCI2 interop | 6 | 2 | 0 | 4 |
| 2. Crypto removal / file transfer | 4 | 0 | 0 | 4 |
| 3. LiDAR/OXTS thread shutdown | 6 | 0 | 0 | 6 |
| 4. New Dispose() cleanup | 8 | 0 | 0 | 8 |
| 5. WNet removal | 4 | 0 | 0 | 4 |
| 6. Installer | 5 | 0 | 0 | 5 |
| 7. General regression | 5 | 0 | 0 | 5 |
| **Total** | **38** | **2** | **0** | **36** |

## How to use this document

- Work through sections in whatever order makes sense given hardware availability (e.g., if no
  LiDAR hardware is on hand today, skip to §1/§2/§6 which need less/different hardware).
- Mark ⏭️ Skipped with a reason if a test genuinely can't be run in this environment (e.g., no
  bench/vehicle connection) rather than leaving it ⬜ forever — that distinguishes "not yet tried"
  from "can't try here."
- Any ❌ Failed item should be recorded with enough detail (error message, repro steps) to open as
  a proper follow-up — this document is for tracking, not deep debugging.
- Once a section is fully ✅ or intentionally ⏭️, update the Summary tracking table.
