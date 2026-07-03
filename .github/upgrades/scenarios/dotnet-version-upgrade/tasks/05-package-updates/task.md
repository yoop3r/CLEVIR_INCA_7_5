# 05-package-updates: Update, remove, and add package references for net10.0

With the project retargeted, reconcile all package references against `net10.0`. Upgrade the 9 packages currently pinned to preview/beta versions to their stable equivalents: `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `System.Diagnostics.DiagnosticSource`, `System.IO.Pipelines`, `System.Memory.Data`, `System.Text.Encoding.CodePages`, `System.Text.Encodings.Web`, `System.Text.Json` (all `11.0.0-preview.4/5.*` → `10.0.9`), and `Newtonsoft.Json` (`13.0.5-beta1` → `13.0.4`). Remove the 10 packages whose functionality is now provided by the `net10.0` shared framework: `System.Buffers`, `System.IO.Compression`, `System.IO.Compression.ZipFile`, `System.Memory`, `System.Numerics.Vectors`, `System.Security.Principal.Windows`, `System.Threading.Tasks.Extensions`, `System.ValueTuple`, `Microsoft.NETCore.Platforms`, `Microsoft.NETCore.Targets`. Also correct the stale `incacom` package reference, which declares `Version="19.0.0.0"` while the installed binary is `20.0.0.0` (not build-breaking today, but should be cleaned up here). Per the confirmed "Per-Project" package management option, keep all references as direct `PackageReference` entries — do not introduce Central Package Management.

Additionally, add the modern `System.Speech` NuGet package (resolves to `11.0.0-preview.5.26302.115` for `net10.0-windows`) to replace the framework-assembly reference that `VoiceRecognitionClass.vb` currently relies on for its 113 references (`SpeechSynthesizer`, `SpeechRecognitionEngine`, `Grammar`, `GrammarBuilder`, `Choices`, etc.). This is expected to be a package-reference swap only — the existing speech code should compile largely unchanged once the package and `net10.0-windows` are both in place.

**Done when**: all 9 flagged packages are updated to their stable `net10.0`-compatible versions, the 10 framework-provided packages are removed, the `incacom` reference version matches the actual binary (`20.0.0.0`), `System.Speech` is referenced as a NuGet package and `VoiceRecognitionClass.vb` compiles against it, and restore/build succeed with no package resolution warnings.

## Research Findings

### Version verification discrepancy (resolved)
`get_supported_package_version` returned `11.0.0-preview.5.26302.115` (a .NET 11 preview) for 8 of the 9 flagged packages (all except Newtonsoft.Json, which correctly returned `13.0.4`). This tool appears to return the absolute latest published version on the feed (including prereleases of the next major .NET version), not the latest *stable* version. Verified directly against the NuGet flat-container API (`api.nuget.org/v3-flatcontainer/{id}/index.json`): `10.0.9` is a valid, published, stable release for all 8 packages. Also checked `System.Speech`'s nuspec directly — `10.0.9` has an explicit `net10.0` dependency group (plus net8.0/net9.0/.NETStandard2.0), confirming it's valid for this project despite the task text quoting `11.0.0-preview.5.26302.115` for it (same stale-lookup artifact). **Decision: use `10.0.9` for all 8 flagged Microsoft.Extensions/System.* packages AND for the new `System.Speech` package reference**, for consistency with the "stable equivalent" goal and the project's `net10.0-windows` TFM. `Newtonsoft.Json` → `13.0.4` (both tools agreed).

### Files/locations affected
- `CLEVIR_INCA_7_5.vbproj` only — all changes are package/reference reconciliation in this single project file.

### System.Speech usage (via grep, active compile scope only — `_Archive\**` excluded via `<Compile Remove>`)
- `VoiceRecognitionClass.vb` — `Imports System.Speech.Recognition`, `Imports System.Speech.Synthesis` (113 refs per assessment)
- `GM_ResidentClient.vb` — `Imports System.Speech.Synthesis`
- `LidarDevice.vb` — `Imports System.Speech.Synthesis`
- `LMFR_Status_Screen_HC.vb` — `Imports System.Speech`, `Imports System.Speech.Recognition`, `Imports System.Speech.Synthesis`
- `Module1.vb` — `Imports System.Speech.Synthesis`
- `VehicleStatDashboard.vb` — `Imports System.Speech.Synthesis`

All these consume the same assembly reference, so swapping the old framework `<Reference Include="System.Speech">` (HintPath to .NETFramework v4.8 reference assemblies) for `<PackageReference Include="System.Speech" Version="10.0.9">` affects them uniformly — no code changes expected since the namespaces/types are unchanged.

### Package reconciliation plan
**Update (9):**
| Package | From | To |
|---|---|---|
| Microsoft.Extensions.DependencyInjection.Abstractions | 11.0.0-preview.4.26230.115 | 10.0.9 |
| Microsoft.Extensions.Logging.Abstractions | 11.0.0-preview.4.26230.115 | 10.0.9 |
| System.Diagnostics.DiagnosticSource | 11.0.0-preview.4.26230.115 | 10.0.9 |
| System.IO.Pipelines | 11.0.0-preview.4.26230.115 | 10.0.9 |
| System.Memory.Data | 11.0.0-preview.4.26230.115 | 10.0.9 |
| System.Text.Encoding.CodePages | 11.0.0-preview.4.26230.115 | 10.0.9 |
| System.Text.Encodings.Web | 11.0.0-preview.4.26230.115 | 10.0.9 |
| System.Text.Json | 11.0.0-preview.4.26230.115 | 10.0.9 |
| Newtonsoft.Json | 13.0.5-beta1 | 13.0.4 |

**Remove (10, framework-provided):** System.Buffers, System.IO.Compression (PackageReference only — the `<Reference>` HintPath entry to .NETFramework v4.8 stays untouched, out of scope), System.IO.Compression.ZipFile, System.Memory, System.Numerics.Vectors, System.Security.Principal.Windows, System.Threading.Tasks.Extensions, System.ValueTuple, Microsoft.NETCore.Platforms, Microsoft.NETCore.Targets.

**Add (1):** System.Speech PackageReference @ 10.0.9 (net10.0 dependency group confirmed via nuspec).

**Fix (1):** `<Reference Include="incacom, Version=19.0.0.0, ...">` → `Version=20.0.0.0` (confirmed actual resolved assembly identity is 20.0.0.0 via `get_project_dependencies`).

**Remove (1):** `<Reference Include="System.Speech">` framework HintPath entry (superseded by the new PackageReference).

**Untouched (out of scope, confirmed fine):** Azure.Core, NAudio* (all 6), PacketDotNet, SharpPcap, runtime.native.System.IO.Compression, SharpZipLib, System.ClientModel, all other `<Reference>` HintPath entries (Microsoft.CSharp, Microsoft.VisualBasic.Compatibility, System.Configuration, System.Management, System.Net.Http, System.Web.Extensions, System.Windows.Forms.DataVisualization, incacom's HintPath itself, Interop.Scripting, RCI2dotNet).

### Decomposition assessment
Single concern (package reference reconciliation), single project file, uniform mechanical pattern — no decomposition needed. Executing as-is.

### Validation / build-fix loop (post-execution)
The task's "Done when" bar requires **no package resolution warnings**, not just fixing the 10 explicitly-named removals. Two additional packages surfaced NU1510 "will not be pruned" warnings after the initial edit pass and were addressed:

1. **First restore build** (no `/restore` flag) failed with `BC30002`/`BC30590` — stale `project.assets.json` didn't yet know about the new `System.Speech` package. Fixed by adding `/restore` to the MSBuild invocation.
2. **Second build** (`/restore`, build_output_33.log): 0 errors, 17 warnings — 5 unexpected NU1510 warnings for `System.Diagnostics.DiagnosticSource`, `System.IO.Pipelines`, `System.Text.Encoding.CodePages`, `System.Text.Encodings.Web`, `System.Text.Json`. These are 5 of the 9 "update" packages that turned out to *also* be fully provided by the `net10.0` shared framework at the `10.0.9` stable version (unlike at the preview versions, where the version-mismatch apparently suppressed the prune-eligibility check). Removed all 5 — since the shared framework already provides them, updating vs. removing both satisfy "stable/net10-compatible", and removing eliminates the warning outright.
3. **Third build** (build_output_34.log): 0 errors, 7 warnings — 2 more NU1510 warnings surfaced for `System.Runtime.CompilerServices.Unsafe` and `System.Security.AccessControl`. Neither is in the task's named removal list, and neither is directly imported in any active source file (verified via grep — no matches). Removed both for the same reason: framework-provided, unused directly, and blocking the "no package resolution warnings" bar.
4. **Fourth build** (build_output_35.log): **0 errors, 3 warnings** — all 3 are the pre-existing `SYSLIB0021`/`SYSLIB0022` obsolete-crypto-API warnings in `EncryptDecrypt.vb`, explicitly called out as task 07's scope, not touched here.

**Final package set change vs. original task text:** in addition to the 9 update + 10 remove + 1 add + 1 fix specified in the task, 7 packages ended up removed instead of updated/left alone: the 5 update-candidates listed above (kept at no version — fully removed rather than updated, since `net10.0-windows` already provides them) plus `System.Runtime.CompilerServices.Unsafe` and `System.Security.AccessControl` (not originally named, but same category of framework-provided/prunable). Only `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Logging.Abstractions`, `System.Memory.Data`, and `Newtonsoft.Json` remain as actual version-updated `PackageReference` entries from the original 9; `System.Speech` was added as planned.

**Result:** restore/build succeeds with 0 errors and 0 package-resolution (NUxxxx) warnings — "Done when" criterion satisfied.


