# Forward plan: config.xml as the definitive INCA configuration source

Date: 2026-10-14
Status: Steps 1-5 implemented; automated verification passes. Step 6 awaits user-run INCA/VehicleSpy smoke testing.
Scope: INCA workspace selection, config-driven processor identity, and explicit dictionary/VehicleSpy setup resources. This document does not authorize WPF migration or restore retired import/flash workflows.

## 1. Requirements carried forward from this chat

- `config.xml` is authoritative. Do not fall back to `VehicleConfigurationsNF.csv`, hardcoded processor lists, or inferred workspace names.
- Read the configured workspace/template name from the text of `/Configuration/INCAWorkspace`, for example `<INCAWorkspace>184_MY26_ACP3_1P</INCAWorkspace>`.
- Interpret `TemplateName` in the request as that element's value, not a new nested `<TemplateName>` element. Preserve the existing flat XML schema unless a separate template and target workspace are explicitly requested.
- Populate `INCAWorkspaceTemplateName` from that configured value if the compatibility global remains necessary. Keep `INCAWorkspace`, the name passed to INCA, consistent with it; never generate either name from processor count, camera count, BlueBox ID, or a variant prefix.
- Processor identity comes from the selected vehicle's `/Configuration/Vehicles/Vehicle/Processors` comma-separated list. The current schema uses plural `<Processors>`, not singular `<Processor>`.
- Active processor names follow `Variant_Controller`: ACP2/ACP3/FPM are current variant examples; MCU/SOC are controller-type examples. Neither list is an allowlist. A new syntactically valid variant or controller must not require a source-code change.
- Preserve FPM as FPM. The earlier FPM-to-ACP3 alias was an interim implementation, not the desired model. The earlier discussion used "family" for MCU; do not confuse controller type with variant.
- ACP4, HC, and LC are deprecated. Do not add new handling for them or silently reinterpret their names. Verify remaining callers before removing legacy code.
- `CanMonitors`, `DataUploadPath`, `CLEVIRFilesPath`, `ConfigName`, and BlueBox-derived naming are retired inputs. Do not reintroduce them.
- `ImportSoftwareAndCalsFunctions.vb` is retired and currently deleted in the working tree. Retain only genuinely required migrated helpers/globals; do not restore the module.
- The user previously requested that multi-processor A2L/enumeration changes remain deferred. Record their implications, but keep them outside this identity migration.
- The .NET 10 runtime smoke test was completed according to the user. Do not repeat the entire modernization baseline or start any WPF form migration as part of this work.

## 2. Initial evidence and gaps (before implementation)

This table records the starting point, not the current implementation. Completed changes and the runtime checkpoint are recorded in section 5. Locations refer to the original inspection; use symbol names if line numbers move.

| Surface | Current state | Gap to close |
|---|---|---|
| `config.xml:7,207-215` | Flat `INCAWorkspace` value and per-vehicle `Processors`; current example is `ACP3_MCU,NA,NA,NA,NA,NA`. | Preserve this schema and configured workspace name. |
| `Module1.vb:207` | `INCAWorkspaceTemplateName` is declared; the source search found no current assignment or consumer. | Decide whether to retain it as a compatibility value or remove it after migration. Assigning it alone does not wire runtime behavior. |
| `GM_ResidentClient.ReadConfiguration` | Reads `INCAWorkspace` from its supplied XML file. Driver-specific configuration is also loaded through this reader. | Establish explicit precedence so a stale driver file cannot override authoritative INCA identity from `config.xml`. |
| `Module1.ReadVehicleConfigsFile` | Reads vehicles from `config.xml`; checks that `INCAWorkspace` exists after a vehicle match. Does not assign the workspace from that node. | Validate and apply the same configuration snapshot; avoid validating one file while using a workspace global loaded from another. |
| `InitForm.GetProjectNameFromProcessor` | Contains `ProcessorProjectAliases`, including FPM-to-ACP3 mappings. | Use shared generic identity parsing instead of a separate alias table. |
| `Module1.ApplyVehicleConfig` / `DetermineProjectName` | First processor is checked against named variants; active processors exclude tokens containing `NA`. | Accept new variants generically; skip only the exact `NA` sentinel, not names containing those letters. |
| `Module1.ResolveProcessorFamily` / `GetPrimaryDeviceName` | FPM-to-ACP3 mapping and hardcoded default-name behavior remain. | Separate exact device identity, variant, controller type, and any genuinely required behavior settings. |
| `GM_ResidentClient.GetDeviceStatus` | Fixed processor list plus the primary configured processor. | Monitor every configured active processor, including secondary MCU/SOC devices. |
| `GM_INCA_Comm.IsKnownProcessorDevice` | Substring matching; an empty configured list matches every device. | Define consistent device matching and explicit invalid/unconfigured behavior rather than permissive fallback. |
| `GM_INCA_Comm.ProcessDevicesAfterOpen` | Flash selection still contains fixed names and primary-processor logic. | Use configured membership where only identity is needed; preserve separately verified device-specific behavior. |
| `ConfigurationEditorForm.ValidateParameter` | Treats `INCAWorkspace` as a filesystem directory. | Validate a required INCA object name, not `Directory.Exists`; INCA lookup validates its existence later. |
| `GM_ResidentClient.WriteConfiguration` | Reconstructs XML and writes the workspace global; does not serialize `Vehicles` or `SelectedVehicleNumber`. | Protect authoritative fleet/selection data from loss if this writer targets the master configuration. |
| `VehicleEditorForm.ButtonSave_Click` | Reconstructs vehicle entries; Add Vehicle seeds `ACP3_MCU`. | Validate generic identities before mutation and avoid silently discarding future/unknown vehicle fields. |

The working tree already contained substantial unrelated edits before this migration. Those edits have been preserved; do not revert them during testing or follow-up.

## 3. Step-wise implementation

### Step 1 - Lock the configuration contract and precedence

- [x] Trace startup, vehicle selection, configuration reload, driver-file generation/loading, and every master-config save entry point.
- [x] Make `config.xml` authoritative for database, workspace, experiment, and selected-vehicle processor identity. Inventory intentional runtime changes such as temporary experiment selection; do not persist them over the configured baseline accidentally.
- [x] Document which non-identity driver settings may still override master defaults. Driver files are derived snapshots, not an alternative source of INCA identity.
- [x] Use the existing flat workspace element. Do not introduce a processor-type registry or a second workspace/template setting without a concrete runtime need.
- [x] Specify legacy bare tokens such as `FPM` and `XETK1` explicitly: require conversion to the actual INCA device name where possible, or obtain approval for a temporary XML-defined translation. Never guess a controller suffix or silently alias FPM to ACP3.
- [x] Specify mixed-variant display/project behavior. Preserve full identities for every processor; any use of the first active processor for a single display/project label must be explicit and must not limit device handling.

Exit: the source-of-truth, precedence, legacy-token policy, and single-label behavior are settled before changing runtime code.

### Step 2 - Introduce one shared processor parser

- [x] Reuse the existing `VehicleConfig.Processors` input; add only the smallest shared parser/value representation needed by its callers.
- [x] Trim each token; ignore exact `NA` case-insensitively. Define empty-slot compatibility explicitly, and reject a selected vehicle with no active processors before INCA initialization.
- [x] Parse non-empty variant and controller components generically, with a documented separator/character rule. Do not validate against ACP2/ACP3/FPM or MCU/SOC lists.
- [x] Preserve the configured device token for INCA calls; use case-insensitive comparison for membership. Reject duplicate configured identities with a useful message.
- [x] Use exact device-name membership by default. If INCA adds decorations, verify actual names and specify narrowly bounded matching; do not use arbitrary substring matches.
- [x] Report invalid input through `HandleUserMessageLogging` and the existing UI notification path, naming the vehicle, XML element, and offending token.
- [x] Replace the separate InitForm alias logic and `DetermineProjectName` allowlist with this shared interpretation. Inspect every `ProjectName`/`GProjectAbbreviation` consumer before changing its value.

Exit: an unseen value such as `NEXTGEN_MCU` or `NEXTGEN_SOC` parses and displays correctly without adding a code branch, and FPM remains FPM.

### Step 3 - Wire workspace validation and application together

- [x] Read `/Configuration/INCAWorkspace` from the authoritative XML snapshot and require a non-whitespace value.
- [x] Validate workspace and selected-vehicle processors before publishing the new identity globals, so failed reloads cannot leave mixed old/new state.
- [x] Set `INCAWorkspace` to the trimmed configured value; retain `INCAWorkspaceTemplateName` as the requested compatibility value initialized from exactly the same setting.
- [x] Check both INCA initialization entry points, configuration verification, login/status display, and reload/vehicle-selection paths for consistent use of the configured workspace.
- [x] Preserve the existing ability to select a vehicle when the selection is `UNDEFINED` or unmatched. This must not permit INCA measurement/recording with stale identity state.
- [x] Use an actionable missing-element error. Do not synthesize a replacement workspace.
- [x] Preserve the existing INCA database lookup for workspace existence; actual hardware smoke verification remains pending.

Exit: the exact configured workspace reaches INCA; changing the processor variant cannot rewrite the workspace name.

### Step 4 - Migrate all identity-based runtime consumers

- [x] Build `ActiveProcessorNames` and `NumControllers` once from validated active tokens; derive any primary identity from that same result.
- [x] Update device status, conformity filtering, and identity-based flash selection to handle all configured processors, not a fixed list or only the primary device.
- [x] Keep opt-in `SsDFIC_FA` debug injection restricted to the configured primary device and the existing signal-name check, not every processor.
- [x] Remove active VehicleSpy/ARXML resource selection and the unused flashing-workspace allowlist. Make dictionary/setup selection and NEW FULL experiment-template selection explicit.
- [x] Do not assume that an FPM identity implies ACP3 resources, or that a controller name alone specifies a flashing strategy.
- [x] Remove unused aliases and template/suffix globals; retain the specifically requested workspace-template compatibility value. Do not modernize inactive updater code or redesign hardware-specific flash algorithms in this migration.

Exit: adding a variant requires configuration/resources, not an identity allowlist edit; unrelated devices do not become processors.

### Step 5 - Make editing and persistence round-trip safely

- [x] Separate INCA workspace-name validation from filesystem-directory validation in `ConfigurationEditorForm` and update relevant descriptions.
- [x] Apply shared processor validation in the vehicle editor before replacing XML nodes. Remove the ACP3-specific Add Vehicle default in favor of explicit entry.
- [x] Preserve `Vehicles`, `SelectedVehicleNumber`, workspace identity, unrelated sections, and unknown elements across master/driver save paths. Verify UI-specific round-trips during the user smoke test.
- [x] Ensure driver snapshot generation/loading cannot overwrite master identity or reintroduce CSV/deprecated inputs.
- [x] Keep retirement of `CanMonitors`, upload paths, BlueBox naming, and `ConfigName` consistent across models, editors, serialization, and comments.
- [x] Retain `ZipMF4Files` and existing DEVELOPMENT/VALIDATION precedence. Correct its XML read/round-trip without consolidating compression policy; policy consolidation remains a separate decision.

Exit: save -> reload -> restart preserves authoritative identity and all unrelated configuration values.

### Step 6 - Verify the intended behavior and update this plan

Use existing repository tooling. Do not introduce a persistent test project solely to verify this migration. Exercise deterministic parsing/configuration checks separately from hardware-dependent smoke checks.

| Case | Required outcome |
|---|---|
| Existing ACP3 baseline | Configured workspace, experiment, device filtering, recording, and unrelated settings remain unchanged. |
| FPM_MCU | Device identity remains `FPM_MCU`; displayed variant is FPM, not ACP3. |
| NEXTGEN_MCU and NEXTGEN_SOC | Both accepted without source edits; controller types remain distinct. |
| Two active processors | Both counted and monitored; a secondary-device failure is surfaced through the existing failure path. |
| Exact NA sentinel | `NA` padding is excluded; a valid variant containing `NA`, such as `NANO_MCU`, is retained. |
| Matching boundaries | `ACP3_MCU` does not match `ACP3_MCU2`; case/whitespace handling follows the defined contract. |
| Bad selected-vehicle config | Empty/all-NA, duplicate, or malformed identities produce actionable errors; no permissive match-all fallback. |
| Missing/blank workspace | Clear error; no generated default and no INCA start using stale globals. |
| Workspace/variant independence | Changing processors does not alter the configured workspace string; actual INCA device mismatch is reported separately. |
| Conflicting driver snapshot | Master `config.xml` identity wins; intentional non-identity driver settings still behave as specified. |
| Save and restart | All active identities, selected vehicle, workspace, unrelated sections, and unknown nodes survive each save path. |
| Unselected vehicle | Vehicle selection remains available; DRIVE/INCA recording cannot proceed with stale processor state. |

- [x] Run isolated deterministic checks against production parser/resource/persistence code and build `CLEVIR_INCA_7_5.vbproj`.
- [ ] Perform a targeted INCA smoke check with actual configured device names: initialization, device status, measurement/record start-stop, and restart persistence. Record unavailable hardware checks as pending, not passed.
- [x] Update examples/descriptions and this file with completed steps, validation evidence, and the next pending action.

## 4. Explicit follow-up, not part of this migration

Multi-processor enumeration handling needs a separate scoped change: associate each A2L with its configured device, isolate conversion/measurement keys by device, and validate cache freshness against the relevant input set. The earlier blanket claim that A2L parsing was unrelated to caching was incomplete: `CreateNewEnumerationFile` does parse A2L content for enumeration output. Do not change enumeration formats, cache keys, or invalidate existing caches implicitly in this identity migration; document any stale-cache prerequisites for smoke checks.

## 5. Progress and resume point

### Step 1 investigation (before implementation) - 2026-10-14

- Confirmed startup loads the master through `InitForm` -> `ReadConfigFile` -> `ReadConfiguration`, then applies the selected vehicle through `ReadVehicleConfigsFile` before initializing the INCA interface.
- Confirmed vehicle selection persists `SelectedVehicleNumber`, changes `VehicleNumber`, and reapplies vehicle configuration. Editor reload paths currently call `ReadConfigFile` without necessarily reapplying processor identity; implementation must reconcile both paths and invalidate cached configuration verification.
- `ReadUserConfigFile` and `WriteUserConfigFile` remain defined, but the VB source search found no call sites. Treat these as compatibility APIs, not evidence that driver overrides are active today. Preserve non-identity compatibility while preventing these APIs from overriding master INCA identity.
- `WriteConfigFile` delegates to the reconstructing writer; its visible InitForm invocation is commented out. The active configuration editor updates the existing XML document. Protect both APIs against future data loss rather than assuming the unused writer is harmless.
- `ProjectName` affects resources, not only display: it selects `<Variant>_DataDictionary.csv`, and legacy VehicleSpy/ARXML branches inspect it. A first-processor project label therefore requires an explicit policy for mixed variants; identity parsing must not silently imply shared resource compatibility.
- User confirmed strict `Variant_Controller` tokens and requested explicit VehicleSpy setup and dictionary resource selection. The decisions and implementation are recorded below.

### Confirmed contract and implemented behavior

- Active tokens must contain exactly one underscore separating non-empty variant and controller components. Each component starts with an ASCII letter/digit and may contain letters, digits, or hyphens. Empty slots are errors; use exact `NA` padding. Matching ignores case; configured device spelling is retained.
- The first active variant supplies the existing project label (and existing versioned enumeration naming). Every active processor is counted, filtered, and monitored independently. FPM is no longer translated to ACP3.
- `<DataDictionary>` explicitly selects the baseline GUI dictionary regardless of processor mix or DEVELOPMENT/VALIDATION mode. Relative paths resolve from the application directory. The annotation editor may still select a temporary in-session override; edit the XML setting to make a selection persistent.
- `<VehicleSpySetup>` explicitly selects the user-configured capture setup when `<AlternateRecordConfig>` is VehicleSpy. No ARXML/A2L/variant mapping selects it. Loading is wired into both launch and first-time/restart capture paths.
- The sample config makes the old resources explicit: `VALIDATION_DataDictionary.csv` and `VehicleSpy\IntrepidTest.vs3`. Both exist in the Debug output, and the deployed config contains the new fields. Confirm the latter is the intended real capture setup before the runtime test.
- A concrete additional resource dependency was found in NEW FULL registration: it previously invented `<Variant>_CAN_Template_Exp`. Optional `<INCAExperimentTemplate>` now supplies that existing INCA experiment template explicitly. The sample leaves it blank; normal DISPLAYS registration is unchanged. NEW FULL reports an actionable error until a template is configured. This is separate from the flat workspace name.
- Master identity covers database, workspace, experiment/template, signal list, ETAS user path, selected vehicle/processors, dictionary, recorder type, and VehicleSpy setup. Compatibility driver readers may still supply non-identity recording/storage/compression/hardware settings, but master identity wins. No active calls to the compatibility driver read/write APIs were found.
- Runtime saves merge into existing XML, preserve unmodeled root/nested/vehicle metadata, and copy identity from the master rather than transient runtime globals. Vehicle-editor rows retain their original nodes; failed validation does not replace the fleet. Returning from the vehicle editor no longer discards edits by reloading disk.
- INCA start/restart and DRIVE are blocked without valid active processors. Exact matching rejects suffix/substring collisions. A configured processor missing from the actual experiment is an explicit error, including secondary controllers.
- The device-status path no longer overwrites the computed fault result with a legacy constant; existing ignore/debug overrides remain explicit. The new behavior requires runtime verification.
- Loading while measurement/recording is active is rejected. After INCA initialization, changing session identity/resources requires restart instead of silently switching the live session. The main-form Change Vehicle action saves a validated next-session selection while retaining the current session identity until restart.

### Verification evidence

- Final Visual Studio project build: successful. Whitespace check on affected tracked files: passed.
- Isolated validation harness: **79 deterministic checks passed** against production `IncaConfiguration.vb`. Coverage includes generic parsing, FPM identity, mixed controllers, NA boundaries, exact device membership and collision rejection, invalid/duplicate inputs, paths, missing required fields, master precedence during saves, metadata preservation, actual on-disk XML round-trip, and restart-required session comparisons.
- Harness artifacts are retained outside the repository in this session's `files\inca-validation` directory; no persistent repository test project or test dependency was added. Visual Studio Test Explorer reported no tests in the production project.
- Production config and deployed Debug config have been checked. Hardware-dependent INCA/VehicleSpy behavior and interactive editor behavior have not been marked passed.

### User runtime test checkpoint - next action

1. Back up the runtime `bin\x64\Debug\config.xml` before editing. Confirm `<DataDictionary>` and `<VehicleSpySetup>` identify the intended files; do not substitute a dummy capture setup for a real recording test.
2. Start with the existing ACP3_MCU baseline and DISPLAYS registration. Confirm workspace `184_MY26_ACP3_1P`, experiment selection, device status, and the expected annotation tabs/buttons.
3. Start and stop a short INCA recording and, when enabled, VehicleSpy capture. Confirm expected files are produced and annotation triggers behave normally. No flashing operation is part of this test.
4. Open Edit Vehicles, make a controlled change, save the vehicle dialog, save the master editor, close, then reopen/restart. Confirm the vehicle change survives and other sections/metadata remain intact. Restore the baseline when finished.
5. With measurement stopped, try reloading changed workspace/processor/dictionary/setup identity after INCA is initialized: expect a restart-required notification, not an immediate session switch. During measurement, expect reload rejection.
6. On a matching real multi-controller workspace, verify all configured controllers appear and a secondary-device fault is reported. Do not expect a fabricated NEXTGEN device to exist in a real INCA experiment; unknown-variant acceptance was checked deterministically.
7. Report the first failed step and its exact message/log context. Mark this checkpoint complete only after actual results are supplied. Leave multi-processor enumeration/cache-format changes deferred.

### Runtime feedback: configuration error acknowledgement (initial fix; superseded by recovery flow below)

- User reported the missing experiment warning flashed and the application exited without acknowledgement.
- Validation failures now use an explicitly owned, topmost modal error dialog, including during hidden auto-drive startup. The error remains until acknowledged; initialization is blocked and the validation cache stays invalid.
- Removed forced application shutdown from the validation exception path and the redundant generic failure prompt in DRIVE. Auto-drive returns to the existing interactive InitForm fallback for configuration editing and retry.
- Verification: project build and whitespace checks passed; all 79 configuration checks still pass. A session-only Windows UI harness invoked the compiled production failure reporter with the reported missing-experiment text and the public validation entry point with no active processors, in separate processes. Both dialogs remained blocking until the harness clicked OK, had a topmost owner, returned False, and left the process alive; the failure reporter also cleared the cache. No INCA connection or configuration edits were used for these isolated checks.
- The initial acknowledgement-only test is superseded by the user-approved recovery flow below.

### Approved follow-up: hidden-startup recovery

- Keep InitForm hidden, including on configuration/connection failure. Its conversion to a module remains separate; no WPF/form migration is performed here.
- `InitializationRecovery.vb` centralizes recoverable stage execution and persistent, topmost Edit Configuration / Retry / Exit prompts. It reuses the existing dialog/editor forms. Non-configuration stages (operating-mode file and INCA-interface creation) offer Retry / Exit only.
- Saving and closing the editor retries the failed stage using a fresh configuration load. Closing without saving returns to the same error, without retry or continuation. Repeated failures stay in recovery. Exit uses the caller's existing shutdown path; user-requested storage-drive cancellation remains an abort, not an editor prompt.
- Startup load, required-tool paths, vehicle setup, INCA connection/preflight, and pre-initialization LoginForm config reload use the shared flow. Missing dictionaries and VehicleSpy setups are detected during preflight, before entering the main session. The startup vehicle backup convention remains intact.
- Preflight reloads master XML, reconnects to the configured database, then calls authoritative `VerifyConfigFiles` for workspace, experiment, signal list, and enumeration setup. Failure reporting can be captured by recovery without flashing an additional generic warning.
- Removed the interactive InitForm fallback and its connection-launch Show call. Unsupported/unavailable INCA versions return a connection failure instead of terminating CLEVIR. Unexpected exceptions outside safely retryable stages are acknowledged and stopped, not blindly replayed against partial initialization.
- LoginForm no longer substitutes basic non-empty-field checks for full INCA preflight. Saved-but-unvalidated changes block LOGIN. Existing initialized-session identity/resource guards still require restart rather than hot-switching an active session.
- Automated checks: successful build, whitespace checks, existing 79 configuration checks, and 11 recovery/error-propagation checks. Recovery checks cover success, Retry, Exit, captured exceptions, non-config stages, explicit startup abort, captured load/validation errors, cancelled editor, simulated saved-editor result, and repeated invalid retries. UI tests invoke the compiled production recovery module with controlled stage results; editor tests use a disposable session-only XML copy, never the live configuration. Actual editor save/reload and INCA connection are still manual integration checks.
- Next runtime test: start with the missing experiment, leave the error visible, select Edit Configuration, cancel once (expect the same error), then save a valid experiment and close (expect revalidation and normal LoginForm progression, never InitForm). Also test external correction plus Retry, Exit, repeated invalid edits, and LoginForm config changes. After INCA initialization, confirm identity/resource edits still request restart.
