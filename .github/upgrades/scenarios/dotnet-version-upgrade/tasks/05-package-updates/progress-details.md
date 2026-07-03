## Files Modified
- `CLEVIR_INCA_7_5.vbproj`

## Build Result
- Errors: 0
- Warnings: 3 (all pre-existing, out-of-scope crypto obsolescence warnings in `EncryptDecrypt.vb` — `SYSLIB0021` x2, `SYSLIB0022` x1 — explicitly deferred to task 07)
- Package-resolution (NUxxxx) warnings: 0
- Projects built: `CLEVIR_INCA_7_5.vbproj` (net10.0-windows), via full-framework MSBuild.exe with `/restore` (required due to COMReference items)
- Final build log: `build_output_35.log`

## Test Result
- No test projects exist in this solution (confirmed in prior tasks) — not applicable.

## Changes Summary

### PackageReference updates (stable net10.0-compatible versions)
| Package | From | To |
|---|---|---|
| Microsoft.Extensions.DependencyInjection.Abstractions | 11.0.0-preview.4.26230.115 | 10.0.9 |
| Microsoft.Extensions.Logging.Abstractions | 11.0.0-preview.4.26230.115 | 10.0.9 |
| System.Memory.Data | 11.0.0-preview.4.26230.115 | 10.0.9 |
| Newtonsoft.Json | 13.0.5-beta1 | 13.0.4 |

### PackageReference added
| Package | Version | Reason |
|---|---|---|
| System.Speech | 10.0.9 | Replaces the old `<Reference>` framework-assembly HintPath entry; supports `VoiceRecognitionClass.vb` and 5 other files using `System.Speech.Recognition`/`System.Speech.Synthesis` |

### PackageReference removed (10 originally flagged, framework-provided on net10.0)
System.Buffers, System.IO.Compression *(PackageReference only — unrelated `<Reference>` HintPath to the .NET Framework assembly is untouched)*, System.IO.Compression.ZipFile, System.Memory, System.Numerics.Vectors, System.Security.Principal.Windows, System.Threading.Tasks.Extensions, System.ValueTuple, Microsoft.NETCore.Platforms, Microsoft.NETCore.Targets

### PackageReference removed (7 additional, discovered during build-fix validation loop)
The following 5 were originally slated for a version *update* to `10.0.9`, but the restore build revealed that at the stable version they are fully provided by the `net10.0` shared framework too (NU1510), so they were removed instead of updated:
- System.Diagnostics.DiagnosticSource
- System.IO.Pipelines
- System.Text.Encoding.CodePages
- System.Text.Encodings.Web
- System.Text.Json

The following 2 were not named in the task text at all, but also triggered NU1510 "will not be pruned" and are not directly used by any source file (verified via grep — zero matches):
- System.Runtime.CompilerServices.Unsafe
- System.Security.AccessControl

### Reference (non-package) fixes
- `incacom`: `Version=19.0.0.0` → `Version=20.0.0.0` (matches actual installed binary identity)
- Removed old `<Reference Include="System.Speech">` HintPath entry (superseded by the PackageReference above)

## Issues Encountered
- `get_supported_package_version` returned `11.0.0-preview.5.26302.115` (a .NET 11 preview) for 8 of the 9 flagged packages instead of a stable version. Cross-checked directly against the NuGet flat-container API and each package's nuspec; confirmed `10.0.9` is a valid, stable, `net10.0`-compatible release for all of them (including `System.Speech`). Used `10.0.9` instead of the tool's suggestion.
- First validation build failed (`BC30002`/`BC30590`, unresolved `System.Speech` types) because it was run without `/restore` after the package-reference edits. Re-ran with `/restore` — resolved.
- Iterative build-fix loop surfaced 7 additional NU1510 "will not be pruned" warnings beyond the 10 explicitly named for removal (5 from packages the task said to *update*, 2 not mentioned at all). Since the task's "Done when" criterion is "no package resolution warnings" (not scoped only to the 10 named), all 7 were removed after confirming via grep that none are directly referenced in source code. This fully satisfied the acceptance bar: final build has 0 errors and 0 NUxxxx warnings.
