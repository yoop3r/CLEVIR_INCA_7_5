# .NET Version Upgrade Plan

## Overview

**Target**: CLEVIR INCA 7.5 — a VB.NET WinForms desktop application (`CLEVIR_INCA_7_5.vbproj`) and its supporting C# packet-capture bridge library (`PcapEventBridge.csproj`), moving from .NET Framework 4.8 to .NET 10 (`net10.0-windows` for the WinForms app, `net10.0` for the library).
**Scope**: 4 total projects in the solution; 2 are in scope for .NET runtime retargeting. `CLEVIR_INCA_7_5.vbproj` is a large old-style (non-SDK) WinForms app — 94,921 LOC across 196 files (118 with flagged incidents), ~53,910+ estimated LOC impact, almost entirely mechanical WinForms/GDI+ API surface changes. `PcapEventBridge.csproj` is a small, already-SDK-style leaf library (63 LOC, 1 file, zero flagged issues). `HesaiWrapper.vcxproj` (native C++, no TFM) and `CLEVIR_Installer.wixproj` (WiX installer, already SDK-style, native project kind) are **not** part of .NET runtime retargeting and have no tasks below.

### Selected Strategy

**Bottom-Up (Dependency-First)** — Upgrade from leaf nodes to root applications, tier by tier.

**Rationale**: The 2 in-scope projects form a 2-tier dependency graph that crosses the .NET Framework → modern .NET boundary (`net48` → `net10.0`/`net10.0-windows`). Per the .NET Framework migration planning rules, any Framework solution with 2+ projects requires Bottom-Up (non-negotiable) — the framework gap means the WinForms app and its library dependency have genuinely different upgrade mechanics (SDK-style conversion, WinForms/GDI+ resolution, binding redirects apply only to the app; the library is a simple in-place retarget) and each tier must be validated independently.

### Dependency Graph

```
Tier 2: [CLEVIR_INCA_7_5.vbproj]   (net48 → net10.0-windows, ClassicWinForms → SDK-style)
			 ↓
Tier 1: [PcapEventBridge.csproj]   (net48 → net10.0, already SDK-style)
```

Out of scope for retargeting (native/non-.NET project kinds): `HesaiWrapper.vcxproj` (native C++), `CLEVIR_Installer.wixproj` (WiX installer — depends on both tiers plus `HesaiWrapper.vcxproj`, but is not itself a .NET runtime target).

### Tier Summary

**Tier 1 — PcapEventBridge.csproj**
- Projects included: `PcapEventBridge.csproj` only
- Dependencies on previous tiers: none (leaf node)
- Completion criteria: builds standalone on `net10.0` with zero errors/warnings

**Tier 2 — CLEVIR_INCA_7_5.vbproj**
- Projects included: `CLEVIR_INCA_7_5.vbproj` only
- Dependencies on previous tiers: Tier 1 (`PcapEventBridge.csproj`) must be retargeted and validated first
- Completion criteria: SDK-style, targets `net10.0-windows`, builds with zero errors/warnings, packages/binding redirects reconciled, obsolete APIs replaced, INCA COM interop runtime smoke test passes

> **Note on assessment false positive**: the "Windows Forms Legacy Controls" bucket (1,431 issues, 2.7%) flagged in the assessment is a confirmed miscategorization — direct source inspection found zero real usages of actually-removed controls (`StatusBar`, `ToolBar`, classic `MainMenu`/`ContextMenu`); the flagged hits are `MenuStrip`, `Form.MainMenuStrip`, and `DataGridView`/`DataGridViewCell*`, all current, fully-supported WinForms controls. No control-replacement work is required for this bucket (see task `04-winforms-retarget`).

## Tasks

### 01-prerequisites: Verify toolchain and .NET 10 SDK readiness

Before any project changes, confirm the local toolchain supports a `net10.0`/`net10.0-windows` target. Neither in-scope project uses multi-targeting today, but `CLEVIR_INCA_7_5.vbproj` will move from an old-style (`ToolsVersion="15.0"`) project format to SDK-style as part of this upgrade, so the installed SDK and any `global.json` must support both the SDK-style tooling and `net10.0` before work begins.

**Done when**: the .NET 10 SDK is confirmed installed and usable; any `global.json` present is compatible with `net10.0` (or updated accordingly); no blocking toolchain issues remain.

---

### 02-pcapeventbridge-retarget: Retarget PcapEventBridge.csproj to net10.0

`PcapEventBridge.csproj` is the sole Tier 1 project — a small, already SDK-style C# class library (63 LOC, 1 file: `PcapEventBridge.cs`) with zero package or API issues flagged in the assessment (difficulty: Low). It has no in-solution project dependencies, so it can be retargeted independently and validated with a standalone build before its only consumer, `CLEVIR_INCA_7_5.vbproj`, is touched. Since it is already SDK-style, this is a straightforward `<TargetFramework>` change from `net48` to `net10.0` plus a sanity check of its shared packages (`SharpPcap` 6.3.1, `PacketDotNet` 1.4.9-pre53 — both reported ✅ Compatible and also used by the Tier 2 project).

**Done when**: `PcapEventBridge.csproj` targets `net10.0`, builds standalone with zero errors and zero warnings, and its package references resolve cleanly on the new TFM.

---

### 03-sdk-style-conversion: Convert CLEVIR_INCA_7_5.vbproj to SDK-style format

`CLEVIR_INCA_7_5.vbproj` is currently an old-style VB.NET project (`ToolsVersion="15.0"`, non-SDK format, `ClassicWinForms` project kind). Per the confirmed Bottom-Up strategy and .NET Framework migration rules, SDK-style conversion is a structural change that must happen as its own task, staying on the current `net48` target framework — the TFM upgrade to `net10.0-windows` happens separately afterward so structural changes and API-surface changes aren't conflated into the same build-fix cycle. The project already uses `PackageReference` for all NuGet dependencies (no `packages.config` present), so this task is scoped purely to the project file format itself (implicit item includes, simplified `.vbproj` XML, removal of legacy MSBuild import boilerplate) while preserving every existing `Compile`/`EmbeddedResource`/form-designer file association the WinForms designer relies on across all 196 files.

**Done when**: `CLEVIR_INCA_7_5.vbproj` is SDK-style, still targets `net48`, and builds successfully with no missing-file or designer-association regressions.

---

### 04-winforms-retarget: Retarget CLEVIR_INCA_7_5.vbproj to net10.0-windows and resolve WinForms/GDI+ API surface

This is the core TFM upgrade for the WinForms desktop application: retarget from `net48` to `net10.0-windows` with `UseWindowsForms` enabled. The assessment reports 53,910 API issues on this project (45,607 binary-incompatible + 8,300 source-incompatible + 3 behavioral-change), but ~98% are mechanical — Windows Forms (44,958 refs, 83.4%: `Label`, `Button`, `ListBox`, `Control` properties, etc.) and GDI+/`System.Drawing` (7,604 refs, 14.1%: `Font`, `FontStyle`, `ContentAlignment`, `GraphicsUnit`) references that resolve automatically once the project targets `net10.0-windows` with Windows Desktop support — not manual per-call fixes. The "Windows Forms Legacy Controls" bucket (1,431 issues) is a confirmed false positive (see plan Overview) and needs no control-replacement work.

One genuine compile-breaking item will surface during this retarget: `GM_INCA_Comm.vb` (~lines 1246-1259) has a dead `System.Runtime.Remoting` lease override (`InitializeLifetimeService` using `ILease`) that the original developer's own comment marks "no longer used" — `System.Runtime.Remoting` has no equivalent in modern .NET, so this override must be **deleted**, not ported, to get a clean build. Before deleting, check the `.vbproj`'s `Compile Include` entries to confirm which duplicate copies (`GM_INCA_Comm_06.16.vb`, `GM_INCA_Comm_backup.vb`, and the `_Archive` copies) are actually compiled into the project — only `GM_INCA_Comm.vb` is confirmed in scope today; leave any file not referenced by the project untouched. Do **not** remove the class's `MarshalByRefObject` inheritance itself (line 317) as part of this task — that is a separate investigation item tracked in `07-obsolete-crypto-cleanup`. If the build surfaces Windows-native API gaps not covered by the WinForms/GDI+ retarget alone, add the Microsoft Windows Compatibility Pack (`Microsoft.Windows.Compatibility`) per the confirmed "Windows Native APIs" option.

**Done when**: `CLEVIR_INCA_7_5.vbproj` targets `net10.0-windows` with `UseWindowsForms` enabled, the dead Remoting lease override is removed from `GM_INCA_Comm.vb`, the project builds with zero errors, and no genuine (non-false-positive) WinForms/GDI+ API incompatibilities remain.

---

### 05-package-updates: Update, remove, and add package references for net10.0

With the project retargeted, reconcile all package references against `net10.0`. Upgrade the 9 packages currently pinned to preview/beta versions to their stable equivalents: `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `System.Diagnostics.DiagnosticSource`, `System.IO.Pipelines`, `System.Memory.Data`, `System.Text.Encoding.CodePages`, `System.Text.Encodings.Web`, `System.Text.Json` (all `11.0.0-preview.4/5.*` → `10.0.9`), and `Newtonsoft.Json` (`13.0.5-beta1` → `13.0.4`). Remove the 10 packages whose functionality is now provided by the `net10.0` shared framework: `System.Buffers`, `System.IO.Compression`, `System.IO.Compression.ZipFile`, `System.Memory`, `System.Numerics.Vectors`, `System.Security.Principal.Windows`, `System.Threading.Tasks.Extensions`, `System.ValueTuple`, `Microsoft.NETCore.Platforms`, `Microsoft.NETCore.Targets`. Also correct the stale `incacom` package reference, which declares `Version="19.0.0.0"` while the installed binary is `20.0.0.0` (not build-breaking today, but should be cleaned up here). Per the confirmed "Per-Project" package management option, keep all references as direct `PackageReference` entries — do not introduce Central Package Management.

Additionally, add the modern `System.Speech` NuGet package (resolves to `11.0.0-preview.5.26302.115` for `net10.0-windows`) to replace the framework-assembly reference that `VoiceRecognitionClass.vb` currently relies on for its 113 references (`SpeechSynthesizer`, `SpeechRecognitionEngine`, `Grammar`, `GrammarBuilder`, `Choices`, etc.). This is expected to be a package-reference swap only — the existing speech code should compile largely unchanged once the package and `net10.0-windows` are both in place.

**Done when**: all 9 flagged packages are updated to their stable `net10.0`-compatible versions, the 10 framework-provided packages are removed, the `incacom` reference version matches the actual binary (`20.0.0.0`), `System.Speech` is referenced as a NuGet package and `VoiceRecognitionClass.vb` compiles against it, and restore/build succeed with no package resolution warnings.

---

### 06-binding-redirect-review: Document and reconcile app.config assembly binding redirects

`app.config` currently has 12 hand-authored `<assemblyBinding>` redirects (with developer comments such as "✅ FIXED: Match actual installed package versions"), and the assessment flags 15 issues against them: 7 mandatory MSB3836 conflicts where a manual redirect targets a version older than what the updated packages/framework now provide (`System.Runtime.CompilerServices.Unsafe`, `System.Buffers`, `System.Memory`, `System.Threading.Tasks.Extensions`, `Azure.Core`, `System.Numerics.Vectors`, `System.Security.AccessControl`), 8 potential forced-downgrade redirects for largely the same set, and 1 missing redirect (`System.IO.Compression` — manual redirect covers `v4.2.0.0` but the package provides `v4.3.0`). Per the confirmed "Document and Review Before Removing" option, produce a short reconciliation record — for each of the 12 existing redirects, note its original purpose (from the dev comments and the package/assembly it targets) before deciding whether it should be removed (its target package was removed in `05-package-updates`, or `net10.0`'s shared framework now supplies it), corrected to match the resolved version, or explicitly retained. Since most flagged conflicts involve packages that are either removed entirely or now framework-provided, expect this task to shrink the binding redirect section rather than patch every version number.

**Done when**: every flagged binding redirect issue (7 mandatory + 8 potential + 1 missing) is resolved or explicitly documented as intentionally retained with a stated reason, `app.config` reflects the reconciled set, and the project builds without MSB3836 warnings.

---

### 07-crypto-removal: Remove file encryption/decryption feature entirely

**Scope change (2026-07-03, explicit user decision)**: originally scoped as an obsolete-API modernization (`RijndaelManaged`→`Aes.Create()`, `SHA512Managed`→`SHA512.Create()`) but is now **full removal** of the encrypt/decrypt feature — the user confirmed encryption is no longer needed, and explicitly accepted that the existing `.encrypt` file archive (including files on shared network locations, some dated 2019/2020 — see `VehicleStatDashboard.vb`'s "Find and Decrypt" tool) becomes permanently unreadable/abandoned as a result. No bulk-decrypt migration pass was requested.

Remove the crypto primitives from `EncryptDecrypt.vb`: `CreateKey`, `CreateIv`, `EncryptOrDecryptFile`, `Encrypt`, `Decrypt`, `HandleEncryptFileName`, `HandleDecryptFileName`, the `CryptoAction` enum, and the module-level crypto state (`Password` const, `_txtDestination*Text`, `_strOutput*`, `StrFileToDecrypt`, `FsInput`/`FsOutput`). Keep `EncryptFilesInDirectory` and `TriggerEncryptAndCopy` — these drive the flash-drive file-transfer workflow (directory walk, file-in-use checks, calling `Module1.vb`'s `CopyFileToDDrive`), which is a distinct feature from encryption and was not asked to be removed. `TriggerEncryptAndCopy` must stop calling `Encrypt(filenamewithpath)` and instead copy the plaintext file directly via `CopyFileToDDrive` — matching the existing precedent already in the same function for `.log` files (copied unencrypted today, never deleted locally afterward). Do not delete the local original file after copy for these files, consistent with that precedent.

Remove the now-dead decrypt UI entry points in `VehicleStatDashboard.vb` / `VehicleStatDashboard.Designer.vb`: `DecryptFileToolStripMenuItem`, `FindEncryptedFilesToolStripMenuItem` (parent menu, "Decrypt and Delete Files on Q..."), `FindAndDecryptToolStripMenuItem` (child, "Decrypt and Delete") — their Click handlers (`DecryptFileToolStripMenuItem_Click`, `FindEncryptedFilesToolStripMenuItem_Click`, `FindAndDecryptToolStripMenuItem_Click`) and Designer control declarations/wiring. `GM_ResidentClient.vb`'s `HandleBackgroundEncryption()` (calls `EncryptFilesInDirectory` every 10s while `UsingFlashDrive`) stays as-is — it will simply stop producing `.encrypt` output once `TriggerEncryptAndCopy` no longer encrypts.

This is a larger blast-radius change than the original scope (multiple files including a WinForms Designer file) — assess at execution time whether decomposition is warranted (e.g., crypto-primitive removal vs. UI-entry-point removal as separate sub-steps).

Separately, `GM_INCA_CommClass` (in `GM_INCA_Comm.vb`, line 317) inherits `MarshalByRefObject`. This compiles fine on `net10.0` and needs no change by default — its only known remoting-related use (the lease override) was already removed in `04-winforms-retarget` as dead code. Investigate whether the `MarshalByRefObject` base class itself still serves any purpose elsewhere in the codebase or is a vestige of the old Remoting design; do not remove the inheritance unless investigation confirms it is safe. This is a review/decision item, not a prescribed code change.

**Done when**: all crypto primitives are removed from `EncryptDecrypt.vb` with no remaining `RijndaelManaged`/`SHA512Managed`/`SYSLIB0021`/`SYSLIB0022` references, `TriggerEncryptAndCopy` copies files directly without encrypting, the dead decrypt UI entry points are removed from `VehicleStatDashboard.vb`/`.Designer.vb` with no dangling references, the project builds with zero errors/warnings, and the `MarshalByRefObject` investigation outcome (keep or remove) is documented even if no code change results from it.

---

### 08-final-validation: Full solution build, test, and INCA interop smoke test

With both tiers upgraded, validate the solution as a whole. Build `CLEVIR_INCA_7_5.sln` end-to-end and confirm both in-scope projects (`PcapEventBridge.csproj`, `CLEVIR_INCA_7_5.vbproj`) target `net10.0`/`net10.0-windows` cleanly with zero errors and zero warnings. Because the ETAS INCA COM interop assemblies (`IncaCOM.dll`/`incacom`, `RCI2dotNet.dll`, `Interop.Scripting.dll`, `Etas.Base.ComSupport.dll`) are a critical, hard-to-mechanically-verify dependency, go beyond a metadata/build-level check: run a runtime smoke test that actually exercises INCA COM interop calls from the retargeted application, not just confirms the assemblies load. Confirm `CLEVIR_Installer.wixproj` (out of scope for retargeting itself) still packages the upgraded output correctly — no dedicated task is expected here since it is already SDK-style and untouched by the retargeting, but flag it for a follow-up if deployment assumptions (self-contained vs framework-dependent) need to change as a result of the `net10.0` move. Document any remaining deferred recommendations (e.g., binding redirect items intentionally retained, the `MarshalByRefObject` investigation outcome) in one place for the user.

**Done when**: the full solution builds with zero errors and zero warnings across both in-scope projects, existing automated tests (if any) pass, a runtime smoke test exercising INCA COM interop succeeds, and the installer's packaging is confirmed unaffected (or a follow-up is explicitly noted if not).
