# 03-sdk-style-conversion: Convert CLEVIR_INCA_7_5.vbproj to SDK-style format

`CLEVIR_INCA_7_5.vbproj` is currently an old-style VB.NET project (`ToolsVersion="15.0"`, non-SDK format, `ClassicWinForms` project kind). Per the confirmed Bottom-Up strategy and .NET Framework migration rules, SDK-style conversion is a structural change that must happen as its own task, staying on the current `net48` target framework — the TFM upgrade to `net10.0-windows` happens separately afterward so structural changes and API-surface changes aren't conflated into the same build-fix cycle. The project already uses `PackageReference` for all NuGet dependencies (no `packages.config` present), so this task is scoped purely to the project file format itself (implicit item includes, simplified `.vbproj` XML, removal of legacy MSBuild import boilerplate) while preserving every existing `Compile`/`EmbeddedResource`/form-designer file association the WinForms designer relies on across all 196 files.

**Done when**: `CLEVIR_INCA_7_5.vbproj` is SDK-style, still targets `net48`, and builds successfully with no missing-file or designer-association regressions.

