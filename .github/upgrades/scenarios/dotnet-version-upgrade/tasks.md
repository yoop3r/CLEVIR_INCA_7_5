# .NET Version Upgrade Progress

## Overview

Upgrading CLEVIR INCA 7.5 (VB.NET WinForms desktop app + supporting C# library) from .NET Framework 4.8 to .NET 10, using a Bottom-Up strategy: the leaf `PcapEventBridge.csproj` library is retargeted and validated first, then `CLEVIR_INCA_7_5.vbproj` is converted to SDK-style, retargeted to `net10.0-windows`, and reconciled (packages, binding redirects, obsolete APIs) before final solution-wide validation.
**Progress**: 2/8 tasks complete <progress value="25" max="100"></progress> 25%

## Tasks
- ✅ 01-prerequisites: Verify toolchain and .NET 10 SDK readiness ([Content](tasks/01-prerequisites/task.md), [Progress](tasks/01-prerequisites/progress-details.md))
- ✅ 02-pcapeventbridge-retarget: Retarget PcapEventBridge.csproj to net10.0 ([Content](tasks/02-pcapeventbridge-retarget/task.md), [Progress](tasks/02-pcapeventbridge-retarget/progress-details.md))
- 🔲 03-sdk-style-conversion: Convert CLEVIR_INCA_7_5.vbproj to SDK-style format
- 🔲 04-winforms-retarget: Retarget CLEVIR_INCA_7_5.vbproj to net10.0-windows and resolve WinForms/GDI+ API surface
- 🔲 05-package-updates: Update, remove, and add package references for net10.0
- 🔲 06-binding-redirect-review: Document and reconcile app.config assembly binding redirects
- 🔲 07-crypto-removal: Remove file encryption/decryption feature entirely (scope confirmed with user 2026-07-03 — full removal, not modernization; see scenario-instructions.md Key Decisions Log)
- 🔲 08-final-validation: Full solution build, test, and INCA interop smoke test

## Recent Activity
- 2026-07-03: Regenerated this file to fix a duplication/corruption issue (duplicate progress lines, duplicate 01-prerequisites entry).
- 2026-07-03: Task 07 rescoped from "obsolete-crypto-cleanup" (modernize APIs) to "crypto-removal" (delete encrypt/decrypt feature entirely), per explicit user confirmation after surfacing that the decrypt path also covers a shared network archive of `.encrypt` files (`Q:\` paths, dating to 2019/2020) that will become permanently unreadable/abandoned. `plan.md` and `scenario-instructions.md` updated accordingly.
