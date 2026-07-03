# 07-crypto-removal: Progress Details

## Decomposition Decision
Executed as a single pass — no decomposition. Rationale: solution-wide call-site analysis confirmed all symbols being removed have zero references outside the 3 files touched (all other hits live in `_Archive\**`, which is `<Compile Remove>`d from the project). The `EncryptDecrypt.vb` crypto removal (sub-concern A) and `VehicleStatDashboard.vb`/`.Designer.vb` UI removal (sub-concern B) are tightly coupled — the UI handlers are the only external callers of `Decrypt()`/`StrFileToDecrypt` — so splitting them would add coordination overhead without reducing risk. The `MarshalByRefObject` investigation (sub-concern C) was read-only and folded in at negligible cost.

## Files Modified
- `EncryptDecrypt.vb` — removed all crypto primitives and state; `TriggerEncryptAndCopy` simplified to a direct plaintext `CopyFileToDDrive` call.
- `VehicleStatDashboard.vb` — removed 4 dead handlers: `DecryptFileToolStripMenuItem_Click`, `FindEncryptedFilesToolStripMenuItem_Click`, `FindAndDecryptToolStripMenuItem_Click`, `ToolStripMenuItem1_Click`.
- `VehicleStatDashboard.Designer.vb` — removed the corresponding 7 control declarations/instantiations, trimmed the `DropDownItems.AddRange` arrays (2 entry points), removed property-setup blocks, and removed 7 `Friend WithEvents` declarations.
- `.github\upgrades\scenarios\dotnet-version-upgrade\tasks\07-crypto-removal\task.md` — enriched with detailed research findings (decomposition rationale, per-sub-concern line references, `MarshalByRefObject` investigation conclusion).

## Changes Summary

### A. Crypto primitive removal (`EncryptDecrypt.vb`)
Removed: `CreateKey`, `CreateIv`, `CryptoAction` enum, `EncryptOrDecryptFile`, `HandleEncryptFileName`, `HandleDecryptFileName`, `Encrypt`, `Decrypt`, plus module-level state (`Password`, `_txtDestinationDecryptText`/`_txtDestinationEncryptText`, `StrFileToDecrypt`, `_strOutputEncrypt`/`_strOutputDecrypt`, `FsInput`/`FsOutput`). Removed unused `Imports System.Security` / `Imports System.Security.Cryptography`.

`TriggerEncryptAndCopy` no longer calls `Encrypt()`; it now unconditionally copies the plaintext file via `CopyFileToDDrive(subfolderName, savefilename, allFiles)` — matching the `.log`-file precedent that already existed in the same function. The now-unused `filenamewithpath` parameter was dropped (4 call sites in `EncryptFilesInDirectory` updated). `CopyFileToDDrive` (`Module1.vb`) was confirmed to never delete the source file, consistent with the "do not delete local original" requirement.

### B. Dead decrypt UI removal (`VehicleStatDashboard.vb` / `.Designer.vb`)
Removed the "Decrypt File...", "Decrypt and Delete Files on Q..." (with its All/CSAV2/Low Content/High Content/Decrypt-and-Delete submenu), and their Click handlers. Also removed `ToolStripMenuItem1_Click` (the "All" checkbox helper) since it becomes dead once its only consumer is gone. Designer file kept in sync: instantiation lines, `DropDownItems.AddRange` arrays, property blocks, and `Friend WithEvents` declarations all updated together to avoid dangling references.

`GM_ResidentClient.vb`'s `HandleBackgroundEncryption()` required no changes — confirmed its call to `EncryptFilesInDirectory(...)` is unchanged in signature and behavior; it now simply results in plaintext `.` files being transferred (no more `.encrypt` output).

### C. `MarshalByRefObject` investigation (`GM_INCA_Comm.vb`)
**No code change** — investigation-only per task scope. Evidence gathered:
- The only known remoting-related member (lease override) was already removed in task `04-winforms-retarget`.
- Solution-wide search for `RemotingServices`, `CreateInstanceAndUnwrap`, `AppDomain.CreateInstance`, `GetLifetimeService`, `CreateObjRef`, `System.Runtime.Remoting` usage in the compiled codebase returns zero hits (only stray non-project files and `_Archive\**`).
- `GM_INCA_CommClass` is instantiated with a plain in-process constructor (`InitForm.vb:969`) — no AppDomain/Remoting boundary crossing anywhere in the live codebase.

**Recommendation**: safe to remove `Inherits MarshalByRefObject` in a future, explicitly-approved change. Not applied now, per task's explicit "review/decision item, not a prescribed code change" framing.

## Build Result
Authoritative full rebuild via Visual Studio MSBuild (`/t:Rebuild`, x64, Debug):
- Log: `build_output_41.log`
- Result: **Build succeeded — 0 Warning(s), 0 Error(s)**
- Confirmed the 3 SYSLIB0021 (x2) / SYSLIB0022 (x1) warnings present in `build_output_40.log` are now fully eliminated (code deleted, not modernized).

## Test Result
No test projects exist in this solution (confirmed across all 4 solution projects in prior tasks) — no tests to run.

## Done-When Criteria Verification
1. ✅ No remaining `RijndaelManaged`/`SHA512Managed`/`SYSLIB0021`/`SYSLIB0022` references anywhere in live code (verified via solution-wide grep; only archive/doc hits remain).
2. ✅ `TriggerEncryptAndCopy` copies files directly without encrypting.
3. ✅ Dead decrypt UI entry points removed from `VehicleStatDashboard.vb`/`.Designer.vb` with no dangling references (verified — no BC30451/BC30456-style undefined-reference errors in rebuild).
4. ✅ Project builds with zero errors/warnings (`build_output_41.log`).
5. ✅ `MarshalByRefObject` investigation outcome documented (see task.md Research Findings and section C above) — recommendation to keep as-is for now, flagged as safe-to-remove-later.

## Issues Encountered
None. The build succeeded on the first rebuild attempt after edits.
