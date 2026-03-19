# CLEVIR 7.5 Installer Modernization Plan

## Table of Contents
1. [Executive Summary](#executive-summary)
2. [Migration Strategy](#migration-strategy)
3. [Detailed Dependency Analysis](#detailed-dependency-analysis)
4. [Project-by-Project Plans](#project-by-project-plans)
5. [Risk Management](#risk-management)
6. [Testing & Validation Strategy](#testing--validation-strategy)
7. [Complexity & Effort Assessment](#complexity--effort-assessment)
8. [Source Control Strategy](#source-control-strategy)
9. [Success Criteria](#success-criteria)

---

## Executive Summary

### Scenario Description
Migration of legacy Visual Studio Deployment Project (`.vdproj`) to modern WiX Toolset v4 installer for the CLEVIR INCA 7.5 application.

### Scope
**Current State:**
- Legacy `.vdproj` installer (not loaded in modern Visual Studio)
- Installs to `C:\CLEVIR_INCA_7_5`
- Packages main executable, dependencies, configuration files, and data files
- Creates desktop and start menu shortcuts
- Requires .NET Framework 4.8

**Target State:**
- Modern WiX Toolset v4 installer project
- Maintainable XML-based configuration
- Compatible with Visual Studio 2022 and command-line builds
- Identical installation behavior and file layout

### Discovered Metrics
- **Files to Package:** ~60 files (executables, DLLs, configs, data files)
- **Shortcuts:** 3 (2 desktop + 1 start menu)
- **Prerequisites:** .NET Framework 4.8 (x86 and x64)
- **Registry Entries:** Minimal (manufacturer entries in HKLM and HKCU)
- **Custom Actions:** None (simplifies migration)
- **UI Dialogs:** Standard 5-screen installer flow

### Complexity Assessment
**Classification:** Medium Complexity

**Rationale:**
- ✅ **Simplified by:** No custom actions, standard UI, single MSI output
- ⚠️ **Moderate because:** ~60 files to map, multiple file types, registry configuration
- 🔧 **Manageable:** WiX has direct equivalents for all .vdproj features used

### Critical Issues
None identified. The existing .vdproj uses standard features that map cleanly to WiX.

### Recommended Approach
**Incremental replacement** with parallel validation:
1. Create WiX project alongside existing .vdproj
2. Build and test WiX installer independently
3. Validate feature parity with side-by-side testing
4. Replace .vdproj once WiX installer validated
5. Remove legacy .vdproj from solution

### Expected Iterations
- **Phase 1:** Foundation (Iterations 2.1-2.3) - WiX project setup and structure
- **Phase 2:** Core Migration (Iteration 3.1) - File mappings and components
- **Phase 3:** Features & UI (Iteration 3.2) - Shortcuts, registry, dialogs
- **Phase 4:** Validation (Iteration 3.3) - Testing strategy and success criteria

**Total:** 6 iterations to complete plan

---

## Migration Strategy

### Approach Selection: Incremental Replacement

**Justification:**
- **Low Risk:** Keep existing .vdproj functional until WiX validated
- **Parallel Development:** Build and test WiX independently
- **Easy Rollback:** Can revert to .vdproj if issues arise
- **Side-by-Side Comparison:** Install both MSIs to verify feature parity

### Why Not "Big Bang" Replacement?
- Installer is critical for deployment; cannot afford downtime
- Need validation period to ensure all features work correctly
- Legacy .vdproj still works (just not loaded in modern VS)

### Dependency-Based Ordering

**Sequential phases (no parallelization possible):**

1. **Phase 1: Environment Setup**
   - Install WiX Toolset
   - Configure development environment
   - **Dependencies:** None
   - **Duration:** 1 session

2. **Phase 2: WiX Project Creation**
   - Create `.wixproj` file
   - Define product metadata (name, version, manufacturer, etc.)
   - Set up basic directory structure
   - **Dependencies:** Phase 1 complete
   - **Duration:** 1 session

3. **Phase 3: File Component Mapping**
   - Map all 60+ files to WiX `<Component>` elements
   - Organize into logical `<ComponentGroup>` sections
   - Define installation directory structure
   - **Dependencies:** Phase 2 complete
   - **Duration:** 2-3 sessions (largest effort)

4. **Phase 4: Features & UI Configuration**
   - Add shortcuts (desktop + start menu)
   - Configure registry entries
   - Set up UI dialog sequence
   - Add .NET Framework prerequisite
   - **Dependencies:** Phase 3 complete
   - **Duration:** 1-2 sessions

5. **Phase 5: Build & Testing**
   - Configure Debug/Release builds
   - Build MSI and test installation
   - Compare with .vdproj-generated MSI
   - **Dependencies:** Phase 4 complete
   - **Duration:** 1-2 sessions

6. **Phase 6: Integration & Cleanup**
   - Add WiX project to solution
   - Update build scripts (if any)
   - Remove .vdproj from solution
   - Update documentation
   - **Dependencies:** Phase 5 validated
   - **Duration:** 1 session

### Execution Approach: Sequential
Phases must be completed in order due to dependencies. No parallelization possible for a single-person migration.

### Rollback Strategy
- Keep .vdproj in solution until WiX validated (do not delete)
- Tag git commit before removing .vdproj
- Document rollback procedure in README

### Success Metrics
- WiX MSI installs successfully
- All files deployed to correct locations
- Shortcuts created and functional
- Registry entries match .vdproj
- .NET Framework prerequisite enforced
- Uninstall removes all components cleanly

---

## Detailed Dependency Analysis

### Installer Dependencies
The WiX installer has minimal external dependencies:

**Build-Time Dependencies:**
- WiX Toolset v4.x (or v3.x if preferred)
- .NET SDK (for WiX build tools)
- Visual Studio 2022 (optional, for GUI editing)

**Install-Time Dependencies:**
- .NET Framework 4.8 (prerequisite for main application)
- Windows Installer 3.1+ (included in Windows Vista+)

### File Dependency Graph

The installer packages files from the build output directory:

```
Build Output Directory
└── bin\x64\Debug\InstallFiles\GM\
    ├── CLEVIR_INCA_7_5.exe (main application)
    ├── AutoUpdater.exe (updater utility)
    ├── HesaiWrapper.dll (C++ wrapper - from HesaiWrapper project)
    ├── Dependencies (third-party DLLs)
    │   ├── PcapDotNet.*.dll
    │   ├── NAudio.*.dll
    │   ├── IncaCOM.dll (ETAS integration)
    │   ├── RCI2dotNet.dll
    │   └── System.*.dll (runtime assemblies)
    └── Configuration Files
        ├── CLEVIR.ini
        ├── config.xml
        ├── DRVR*.xml (6 driver configs)
        ├── *.txt (configuration data)
        └── *.csv (lookup tables)
```

### Migration Phase Grouping

**Phase 1: Foundation Setup**
- Install WiX Toolset
- Create new WiX project (.wixproj)
- Configure basic product information

**Phase 2: Core File Installation**
- Map all 60+ files from .vdproj to WiX Components
- Define installation directory structure
- Configure file deployment

**Phase 3: Features & UI**
- Add shortcuts (desktop + start menu)
- Configure registry entries
- Customize UI dialog flow
- Add .NET Framework 4.8 prerequisite

**Phase 4: Build & Validation**
- Configure build outputs (Debug/Release)
- Test installer functionality
- Validate against original .vdproj behavior

### Critical Path
1. **WiX Toolset Installation** → Blocks all subsequent work
2. **Project Creation & Structure** → Blocks file mapping
3. **File Component Mapping** → Blocks feature addition
4. **Build Configuration** → Blocks testing

### No Circular Dependencies
The installer project depends on build outputs but does not affect the main projects. Migration can proceed independently.

---

## Project-by-Project Plans

### WiX Installer Project

**Current State:**
- **Project Type:** Visual Studio Deployment Project (.vdproj)
- **Location:** `C:\DEV\CLEVIR\CLEVIR_7.5 Installation\CLEVIR_7.5 Installation.vdproj`
- **Status:** Not loaded (legacy format unsupported in modern VS)
- **Output:** `CLEVIR_7.5 Installation.msi`
- **Install Location:** `C:\CLEVIR_INCA_7_5`
- **Configurations:** Debug and Release

**Target State:**
- **Project Type:** WiX Toolset Project (.wixproj)
- **Location:** `C:\DEV\CLEVIR\CLEVIR_7.5 Installation\CLEVIR_Installer.wixproj`
- **Status:** Fully integrated with VS 2022 solution
- **Output:** `CLEVIR_7.5_Setup.msi`
- **Install Location:** `C:\CLEVIR_INCA_7_5` (unchanged)
- **Configurations:** Debug and Release (matching .vdproj)

---

### Migration Steps

#### 1. Prerequisites

**Install WiX Toolset:**
- Download and install WiX Toolset v4.x from https://wixtoolset.org/
- OR install WiX v3.14 (more stable, widely documented)
- Install WiX Visual Studio Extension (optional, for GUI editing)

**Verify Installation:**
```powershell
wix --version  # For WiX v4
# OR
candle.exe /?  # For WiX v3
```

**Required Skills:**
- Basic XML syntax knowledge
- Understanding of Windows Installer concepts (components, features)
- Familiarity with MSBuild (for project file editing)

---

#### 2. Create WiX Project

**Option A: Command Line (Recommended)**
```powershell
cd "C:\DEV\CLEVIR\CLEVIR_7.5 Installation"
dotnet new wix -n CLEVIR_Installer  # Creates .wixproj and Product.wxs
```

**Option B: Visual Studio**
1. Right-click solution → Add → New Project
2. Search "WiX" → Select "Setup Project for WiX v4"
3. Name: `CLEVIR_Installer`
4. Location: `C:\DEV\CLEVIR\CLEVIR_7.5 Installation\`

**Initial Project Structure:**
```
CLEVIR_7.5 Installation/
├── CLEVIR_Installer.wixproj
└── Product.wxs (main WiX source file)
```

---

#### 3. Configure Product Metadata

**Edit `Product.wxs`** - Define basic product information:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs">
  <Product Id="*" 
           Name="CLEVIR_7.5 Installation" 
           Language="1033" 
           Version="7.5.1.0" 
           Manufacturer="Client Solutions" 
           UpgradeCode="23105CF9-22DC-4835-BA8D-D3432A8FAF09">

    <Package InstallerVersion="500" 
             Compressed="yes" 
             InstallScope="perUser" 
             Description="CLEVIR INCA 7.5 Installation"
             Comments="CLEVIR INCA 7.5 Installation" />

    <MajorUpgrade DowngradeErrorMessage="A newer version of [ProductName] is already installed." />

    <MediaTemplate EmbedCab="yes" />

    <!-- Features and Components defined below -->

  </Product>
</Wix>
```

**Key Mapping from .vdproj:**
- `ProductCode`: Use `*` (auto-generated per build)
- `UpgradeCode`: **Copy exact value** from .vdproj: `{23105CF9-22DC-4835-BA8D-D3432A8FAF09}`
- `ProductVersion`: `7.5.1.0` (from .vdproj "ProductVersion")
- `Manufacturer`: `Client Solutions` (from .vdproj)
- `InstallScope`: `perUser` (matches .vdproj `InstallAllUsers = FALSE`)

---

#### 4. Define Installation Directory Structure

**Add Directory Structure** to `Product.wxs`:

```xml
<Fragment>
  <Directory Id="TARGETDIR" Name="SourceDir">
    <!-- Program Files structure (not used, but required) -->
    <Directory Id="ProgramFilesFolder">
      <Directory Id="ManufacturerFolder" Name="Client Solutions" />
    </Directory>

    <!-- Custom install location: C:\CLEVIR_INCA_7_5 -->
    <Directory Id="CLEVIR_ROOT" Name="CLEVIR_INCA_7_5">
      <Directory Id="INSTALLDIR" />
    </Directory>

    <!-- Desktop shortcuts -->
    <Directory Id="DesktopFolder" Name="Desktop" />

    <!-- Start Menu shortcuts -->
    <Directory Id="ProgramMenuFolder">
      <Directory Id="ApplicationProgramsFolder" Name="CLEVIR_7.5 Installation" />
    </Directory>
  </Directory>
</Fragment>
```

**Note:** The `CLEVIR_ROOT` directory ID maps to `C:\` drive, with `CLEVIR_INCA_7_5` subdirectory.

---

#### 5. Map Files to Components

**File Mapping Strategy:**

Group files logically into ComponentGroups:
1. **Main Application** - CLEVIR_INCA_7_5.exe + direct dependencies
2. **Utilities** - AutoUpdater.exe
3. **Third-Party DLLs** - PcapDotNet, NAudio, ETAS libraries
4. **System Dependencies** - System.*.dll assemblies
5. **Configuration Files** - XML, INI, TXT files
6. **Data Files** - CSV lookup tables
7. **Binaries** - 7z executables, HesaiWrapper.dll

**Component Group Example (Main Application):**

```xml
<Fragment>
  <ComponentGroup Id="MainApplicationComponents" Directory="INSTALLDIR">

    <!-- Main Executable -->
    <Component Id="CMP_CLEVIR_INCA_EXE" Guid="*">
      <File Id="FILE_CLEVIR_INCA_EXE" 
            Source="$(var.ProjectDir)..\CLEVIR_INCA_7_5\bin\$(var.Configuration)\InstallFiles\GM\CLEVIR_INCA_7_5.exe" 
            KeyPath="yes" />
    </Component>

    <!-- AutoUpdater -->
    <Component Id="CMP_AutoUpdater" Guid="*">
      <File Id="FILE_AutoUpdater" 
            Source="$(var.ProjectDir)..\CLEVIR_INCA_7_5\bin\$(var.Configuration)\InstallFiles\GM\AutoUpdater.exe" 
            KeyPath="yes" />
    </Component>

    <!-- Add remaining components... -->

  </ComponentGroup>
</Fragment>
```

**Complete File Mapping** (all 60+ files):

Create separate ComponentGroups for organization:

**A. Third-Party Dependencies:**
```xml
<ComponentGroup Id="ThirdPartyLibraries" Directory="INSTALLDIR">
  <Component Id="CMP_PcapDotNet_Core" Guid="*">
    <File Source="...\PcapDotNet.Core.dll" KeyPath="yes" />
  </Component>
  <Component Id="CMP_PcapDotNet_Packets" Guid="*">
    <File Source="...\PcapDotNet.Packets.dll" KeyPath="yes" />
  </Component>
  <Component Id="CMP_PcapDotNet_Base" Guid="*">
    <File Source="...\PcapDotNet.Base.dll" KeyPath="yes" />
  </Component>
  <Component Id="CMP_NAudio_Core" Guid="*">
    <File Source="...\NAudio.Core.dll" KeyPath="yes" />
  </Component>
  <Component Id="CMP_NAudio_WinMM" Guid="*">
    <File Source="...\NAudio.WinMM.dll" KeyPath="yes" />
  </Component>
  <Component Id="CMP_IncaCOM" Guid="*">
    <File Source="...\incacom.dll" KeyPath="yes" />
  </Component>
  <Component Id="CMP_IncaRci2" Guid="*">
    <File Source="...\incaRci2.dll" KeyPath="yes" />
  </Component>
  <Component Id="CMP_RCI2dotNet" Guid="*">
    <File Source="...\RCI2dotNet.dll" KeyPath="yes" />
  </Component>
  <Component Id="CMP_EtasBaseComSupport" Guid="*">
    <File Source="...\Etas.Base.ComSupport.dll" KeyPath="yes" />
  </Component>
  <!-- Add remaining DLLs... -->
</ComponentGroup>
```

**B. System Assemblies:**
```xml
<ComponentGroup Id="SystemAssemblies" Directory="INSTALLDIR">
  <Component Id="CMP_SystemIOCompression" Guid="*">
    <File Source="...\System.IO.Compression.dll" KeyPath="yes" />
  </Component>
  <Component Id="CMP_SystemIOCompressionFileSystem" Guid="*">
    <File Source="...\System.IO.Compression.FileSystem.dll" KeyPath="yes" />
  </Component>
  <!-- Add remaining System.*.dll files... -->
</ComponentGroup>
```

**C. Configuration Files:**
```xml
<ComponentGroup Id="ConfigurationFiles" Directory="INSTALLDIR">
  <Component Id="CMP_CLEVIR_INI" Guid="*">
    <File Source="...\CLEVIR.ini" KeyPath="yes" />
  </Component>
  <Component Id="CMP_ConfigXML" Guid="*">
    <File Source="...\config.xml" KeyPath="yes" />
  </Component>
  <Component Id="CMP_AudioToTextConfig" Guid="*">
    <File Source="...\AudioTotextConfig.xml" KeyPath="yes" />
  </Component>
  <Component Id="CMP_ONVIFConfig" Guid="*">
    <File Source="...\ONVIFSystemConfigurationDialog.exe.config" KeyPath="yes" />
  </Component>
  <!-- Driver XML files -->
  <Component Id="CMP_DRVR00" Guid="*">
    <File Source="...\DRVR00.xml" KeyPath="yes" />
  </Component>
  <Component Id="CMP_DRVR01" Guid="*">
    <File Source="...\DRVR01.xml" KeyPath="yes" />
  </Component>
  <Component Id="CMP_DRVR02" Guid="*">
    <File Source="...\DRVR02.xml" KeyPath="yes" />
  </Component>
  <Component Id="CMP_DRVR03" Guid="*">
    <File Source="...\DRVR03.xml" KeyPath="yes" />
  </Component>
  <Component Id="CMP_DRVR04" Guid="*">
    <File Source="...\DRVR04.xml" KeyPath="yes" />
  </Component>
  <Component Id="CMP_DRVR05" Guid="*">
    <File Source="...\DRVR05.xml" KeyPath="yes" />
  </Component>
  <!-- TXT configuration files -->
  <Component Id="CMP_AdminPCs" Guid="*">
    <File Source="...\adminPCs.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_Availability" Guid="*">
    <File Source="...\Availability.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_CameraIPAddresses" Guid="*">
    <File Source="...\CameraIPAddresses.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_CANalyzerStartDelay" Guid="*">
    <File Source="...\CANalyzerStartDelayTimeMsec.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_CLEVIRFeatureAccess" Guid="*">
    <File Source="...\CLEVIR_FeatureAccessForPATAC.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_ClearCodesDelay" Guid="*">
    <File Source="...\ClearCodesDelayTimes.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_Debug" Guid="*">
    <File Source="...\debug.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_ReadMe" Guid="*">
    <File Source="...\ReadMe.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_TopDownViewConfig" Guid="*">
    <File Source="...\TopDownViewConfig30.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_UserIDList" Guid="*">
    <File Source="...\UserIDList.txt" KeyPath="yes" />
  </Component>
  <Component Id="CMP_VehicleConfig" Guid="*">
    <File Source="...\VehicleConfig.txt" KeyPath="yes" />
  </Component>
</ComponentGroup>
```

**D. Data Files:**
```xml
<ComponentGroup Id="DataFiles" Directory="INSTALLDIR">
  <Component Id="CMP_ValidationDataDictionary" Guid="*">
    <File Source="...\VALIDATION_DataDictionary.csv" KeyPath="yes" />
  </Component>
  <Component Id="CMP_VehicleConfigurationsNF" Guid="*">
    <File Source="...\VehicleConfigurationsNF.csv" KeyPath="yes" />
  </Component>
  <Component Id="CMP_VehiclePTPLookup" Guid="*">
    <File Source="...\VehiclePTPLookup.csv" KeyPath="yes" />
  </Component>
</ComponentGroup>
```

**E. Utility Binaries:**
```xml
<ComponentGroup Id="UtilityBinaries" Directory="INSTALLDIR">
  <Component Id="CMP_7z2501" Guid="*">
    <File Source="...\7z2501.exe" KeyPath="yes" />
  </Component>
  <Component Id="CMP_7z2501_x64" Guid="*">
    <File Source="...\7z2501-x64.exe" KeyPath="yes" />
  </Component>
  <Component Id="CMP_HesaiWrapper" Guid="*">
    <File Source="...\HesaiWrapper\HesaiWrapper\x64\$(var.Configuration)\HesaiWrapper.dll" KeyPath="yes" />
  </Component>
</ComponentGroup>
```

**F. Icon File:**
```xml
<ComponentGroup Id="IconFiles" Directory="INSTALLDIR">
  <Component Id="CMP_AppIcon" Guid="*">
    <File Id="FILE_AppIcon" Source="...\GM_ResidentClient_256px.ico" KeyPath="yes" />
  </Component>
</ComponentGroup>
```

---

#### 6. Define Feature

**Add Feature** referencing all ComponentGroups:

```xml
<Feature Id="MainFeature" Title="CLEVIR INCA 7.5" Level="1">
  <ComponentGroupRef Id="MainApplicationComponents" />
  <ComponentGroupRef Id="ThirdPartyLibraries" />
  <ComponentGroupRef Id="SystemAssemblies" />
  <ComponentGroupRef Id="ConfigurationFiles" />
  <ComponentGroupRef Id="DataFiles" />
  <ComponentGroupRef Id="UtilityBinaries" />
  <ComponentGroupRef Id="IconFiles" />
  <ComponentGroupRef Id="ShortcutComponents" />
  <ComponentGroupRef Id="RegistryComponents" />
</Feature>
```

---

#### 7. Add Shortcuts

**Desktop Shortcuts** (2):

```xml
<Fragment>
  <ComponentGroup Id="ShortcutComponents">

    <!-- Desktop Shortcut -->
    <Component Id="CMP_DesktopShortcut" Guid="*" Directory="DesktopFolder">
      <Shortcut Id="DesktopShortcut"
                Name="CLEVIR_INCA_7_5"
                Description="CLEVIR_INCA_7_5"
                Target="[INSTALLDIR]CLEVIR_INCA_7_5.exe"
                WorkingDirectory="INSTALLDIR"
                Icon="AppIcon.ico" />
      <RemoveFolder Id="RemoveDesktopFolder" Directory="DesktopFolder" On="uninstall" />
      <RegistryValue Root="HKCU" Key="Software\ClientSolutions\CLEVIR" 
                     Name="DesktopShortcut" Type="integer" Value="1" KeyPath="yes" />
    </Component>

    <!-- Start Menu Shortcut -->
    <Component Id="CMP_StartMenuShortcut" Guid="*" Directory="ApplicationProgramsFolder">
      <Shortcut Id="StartMenuShortcut"
                Name="CLEVIR_INCA_7_5"
                Description="CLEVIR_INCA_7-4"
                Target="[INSTALLDIR]CLEVIR_INCA_7_5.exe"
                WorkingDirectory="INSTALLDIR"
                Icon="AppIcon.ico" />
      <RemoveFolder Id="RemoveApplicationProgramsFolder" On="uninstall" />
      <RegistryValue Root="HKCU" Key="Software\ClientSolutions\CLEVIR" 
                     Name="StartMenuShortcut" Type="integer" Value="1" KeyPath="yes" />
    </Component>

  </ComponentGroup>
</Fragment>

<!-- Define Icon -->
<Icon Id="AppIcon.ico" SourceFile="$(var.ProjectDir)..\CLEVIR_INCA_7_5\bin\$(var.Configuration)\InstallFiles\GM\GM_ResidentClient_256px.ico" />
```

---

#### 8. Configure Registry Entries

**Registry Entries** (minimal, from .vdproj):

```xml
<Fragment>
  <ComponentGroup Id="RegistryComponents" Directory="INSTALLDIR">

    <!-- HKLM Registry -->
    <Component Id="CMP_Registry_HKLM" Guid="*">
      <RegistryKey Root="HKLM" Key="Software\Client Solutions">
        <RegistryValue Type="string" Value="" KeyPath="yes" />
      </RegistryKey>
    </Component>

    <!-- HKCU Registry -->
    <Component Id="CMP_Registry_HKCU" Guid="*">
      <RegistryKey Root="HKCU" Key="Software\Client Solutions">
        <RegistryValue Type="string" Value="" KeyPath="yes" />
      </RegistryKey>
    </Component>

  </ComponentGroup>
</Fragment>
```

---

#### 9. Add .NET Framework 4.8 Prerequisite

**WiX v4 Approach** (using BootstrapperApplicationRef):

Create a Bundle project (optional, for prerequisite checking):

**Option A: Simple Launch Condition** (no bundle):
```xml
<Condition Message="This application requires .NET Framework 4.8. Please install the .NET Framework then run this installer again.">
  <![CDATA[Installed OR NETFRAMEWORK48]]>
</Condition>

<PropertyRef Id="NETFRAMEWORK48" />
```

**Option B: Burn Bundle** (advanced, recommended):

Create separate Bundle project:
```xml
<Bundle Name="CLEVIR_7.5 Installation" Version="7.5.1.0" Manufacturer="Client Solutions" UpgradeCode="...">
  <BootstrapperApplicationRef Id="WixStandardBootstrapperApplication.RtfLicense">
    <bal:WixStandardBootstrapperApplication LicenseFile="License.rtf" />
  </BootstrapperApplicationRef>

  <Chain>
    <!-- .NET Framework 4.8 Prerequisite -->
    <PackageGroupRef Id="NetFx48Web" />

    <!-- Main MSI -->
    <MsiPackage SourceFile="$(var.CLEVIR_Installer.TargetPath)" />
  </Chain>
</Bundle>
```

**Recommendation:** Start with Option A (simple condition), add Bundle later if needed.

---

#### 10. Configure UI Dialog Sequence

**Add WixUI Dialog Set:**

```xml
<UIRef Id="WixUI_InstallDir" />
<Property Id="WIXUI_INSTALLDIR" Value="INSTALLDIR" />

<!-- Customize welcome dialog -->
<WixVariable Id="WixUILicenseRtf" Value="License.rtf" />
<WixVariable Id="WixUIBannerBmp" Value="Banner.bmp" />
<WixVariable Id="WixUIDialogBmp" Value="Dialog.bmp" />
```

This provides the standard 5-dialog flow:
1. Welcome
2. License Agreement
3. Installation Folder
4. Confirm Installation
5. Progress/Finished

---

#### 11. Configure Build Outputs

**Edit `.wixproj`** to configure Debug/Release:

```xml
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|x64' ">
  <OutputPath>bin\Debug\</OutputPath>
  <DefineConstants>Configuration=Debug</DefineConstants>
</PropertyGroup>

<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|x64' ">
  <OutputPath>bin\Release\</OutputPath>
  <DefineConstants>Configuration=Release</DefineConstants>
</PropertyGroup>
```

---

#### 12. Build and Test

**Build MSI:**
```powershell
cd "C:\DEV\CLEVIR\CLEVIR_7.5 Installation"
msbuild CLEVIR_Installer.wixproj /p:Configuration=Debug /p:Platform=x64
```

**Test Installation:**
1. Locate MSI: `bin\Debug\CLEVIR_7.5_Setup.msi`
2. Run installer (as administrator if needed)
3. Verify:
   - Files installed to `C:\CLEVIR_INCA_7_5`
   - Desktop shortcut created
   - Start menu shortcut created
   - Application launches successfully
4. Test uninstall:
   - Uninstall via "Add/Remove Programs"
   - Verify all files removed
   - Verify shortcuts removed

---

### Expected Breaking Changes

**None** - WiX can replicate all .vdproj functionality without breaking changes.

**Potential Differences:**
- MSI filename changes from `CLEVIR_7.5 Installation.msi` to `CLEVIR_7.5_Setup.msi` (configurable)
- Build output location changes (but can be configured to match)
- Internal MSI structure differs (not visible to end users)

---

### Code Modifications

**No application code changes required.**

**Project file changes:**
- Add `CLEVIR_Installer.wixproj` to solution
- Update build scripts (if any) to reference new project
- Remove `CLEVIR_7.5 Installation.vdproj` from solution (after validation)

---

### Testing Strategy

**Phase 1: Fresh Install Testing**
1. Clean VM or test machine (no previous CLEVIR installation)
2. Run WiX-generated MSI
3. Verify all files installed
4. Verify shortcuts functional
5. Verify application launches
6. Verify application functionality (smoke test)

**Phase 2: Upgrade Testing**
1. Install old .vdproj MSI (if available)
2. Run WiX MSI as upgrade
3. Verify files upgraded
4. Verify settings preserved (if applicable)

**Phase 3: Uninstall Testing**
1. Install WiX MSI
2. Uninstall via Control Panel
3. Verify all files removed
4. Verify shortcuts removed
5. Verify registry entries removed
6. Verify no orphaned files/folders

**Phase 4: Side-by-Side Comparison**
1. Compare file layouts between .vdproj and WiX installs
2. Compare registry entries
3. Compare shortcut properties
4. Document any differences

---

### Validation Checklist

- [ ] WiX Toolset installed
- [ ] `.wixproj` file created
- [ ] Product metadata configured (name, version, manufacturer, UpgradeCode)
- [ ] All 60+ files mapped to Components
- [ ] ComponentGroups organized logically
- [ ] Feature references all ComponentGroups
- [ ] Desktop shortcuts created (2)
- [ ] Start menu shortcut created (1)
- [ ] Registry entries configured
- [ ] .NET Framework 4.8 prerequisite added
- [ ] UI dialog sequence configured
- [ ] Debug/Release configurations set
- [ ] MSI builds successfully
- [ ] Fresh install test passed
- [ ] Uninstall test passed
- [ ] Application launches after install
- [ ] All files deployed to correct locations
- [ ] Side-by-side comparison with .vdproj completed
- [ ] WiX project added to solution
- [ ] Build scripts updated (if needed)
- [ ] .vdproj removed from solution

---

## Risk Management

## Risk Management

### High-Risk Changes

| Risk Area | Risk Level | Description | Mitigation Strategy |
|-----------|-----------|-------------|---------------------|
| File Mapping Errors | Medium | Missing or incorrectly mapped files could cause runtime failures | Use checklist; compare file lists between .vdproj and WiX; test thoroughly |
| Path Configuration | Medium | Incorrect install paths could break application | Match .vdproj exactly (`C:\CLEVIR_INCA_7_5`); validate during testing |
| Dependency Missing | Medium | Missing DLL could prevent application launch | Map all dependencies from .vdproj; test application launch |
| UpgradeCode Change | High | Changing UpgradeCode breaks upgrade path | **Copy exact UpgradeCode** from .vdproj: `{23105CF9-22DC-4835-BA8D-D3432A8FAF09}` |
| Registry Issues | Low | Missing/incorrect registry entries rarely critical for this app | Registry entries are minimal; validate against .vdproj |
| Shortcut Errors | Low | Broken shortcuts inconvenience users but not critical | Test shortcuts point to correct target; validate icons |
| Build Integration | Medium | WiX build failing in CI/CD pipeline | Test build locally first; update build scripts incrementally |

### Security Vulnerabilities

**None identified** - This is an installer migration, not a security upgrade.

**Note:** Installer itself doesn't introduce new attack vectors. Application security unchanged.

### Contingency Plans

#### **Scenario 1: WiX Build Failures**

**Symptoms:** MSI fails to build; candle.exe or light.exe errors

**Root Causes:**
- Missing file references
- Syntax errors in .wxs files
- Missing WiX extensions

**Resolution:**
1. Review build error messages carefully
2. Validate XML syntax in .wxs files
3. Check file paths are correct (use variables like `$(var.Configuration)`)
4. Consult WiX documentation for error codes

**Rollback:** Continue using .vdproj until WiX build fixed

---

#### **Scenario 2: Missing Files After Installation**

**Symptoms:** Application crashes on launch; "DLL not found" errors

**Root Causes:**
- File not mapped in WiX Components
- File path incorrect in Source attribute
- Build configuration mismatch (Debug vs Release)

**Resolution:**
1. Compare installed file list with .vdproj-generated MSI
2. Add missing files to appropriate ComponentGroup
3. Rebuild and test

**Rollback:** Uninstall WiX MSI, reinstall using .vdproj MSI

---

#### **Scenario 3: Upgrade Breaks Existing Installations**

**Symptoms:** Users report upgrade failures or lost settings

**Root Causes:**
- UpgradeCode changed (breaks upgrade detection)
- Component IDs changed (breaks per-component upgrade)

**Resolution:**
1. **Never change UpgradeCode** - use exact value from .vdproj
2. Use `Guid="*"` for Component IDs (auto-generated, safe)
3. Test upgrade scenario before deployment

**Rollback:** Users uninstall new version, reinstall old version

---

#### **Scenario 4: Performance Issues During Build**

**Symptoms:** WiX build takes too long; large MSI file size

**Root Causes:**
- Unnecessary files packaged
- Debug symbols included in Release build

**Resolution:**
1. Review file list; remove unnecessary files
2. Ensure Release build uses Release binaries (not Debug)
3. Use `EmbedCab="yes"` for smaller MSI distribution

**Rollback:** N/A - performance issue, not critical

---

#### **Scenario 5: Users Report "Installation Failed"**

**Symptoms:** MSI install wizard shows generic error

**Root Causes:**
- Missing .NET Framework 4.8 prerequisite
- Insufficient permissions
- Disk space issues

**Resolution:**
1. Improve prerequisite checking (add clear error message)
2. Document administrator rights requirement (if needed)
3. Add disk space check

**Rollback:** Use .vdproj MSI temporarily while fixing

---

## Testing & Validation Strategy

### Phase-by-Phase Testing Requirements

#### **Phase 1: Build Validation**
**Goal:** Verify WiX project builds successfully

**Tests:**
1. Build Debug configuration → MSI generated
2. Build Release configuration → MSI generated
3. Verify MSI file size reasonable (~10-50MB expected)
4. Inspect MSI with Orca tool (optional) → verify file table

**Success Criteria:**
- ✅ MSI builds without errors
- ✅ MSI file created in `bin\Debug\` and `bin\Release\`

---

#### **Phase 2: Fresh Install Validation**
**Goal:** Verify installer works on clean system

**Environment:** Clean Windows 10/11 VM or test machine (no previous CLEVIR installation)

**Tests:**
1. **Run installer** (double-click MSI)
2. **Verify UI** - all dialogs display correctly
3. **Complete installation** - select default options
4. **Verify files deployed**:
   ```powershell
   Get-ChildItem "C:\CLEVIR_INCA_7_5" -Recurse | Measure-Object
   # Should show ~60 files
   ```
5. **Verify shortcuts created**:
   - Desktop: `CLEVIR_INCA_7_5.lnk` exists
   - Start Menu: `C:\ProgramData\Microsoft\Windows\Start Menu\Programs\CLEVIR_7.5 Installation\CLEVIR_INCA_7_5.lnk` exists
6. **Verify application launches**:
   - Double-click desktop shortcut
   - Application starts without errors
7. **Smoke test application** - perform basic operations

**Success Criteria:**
- ✅ Installer completes without errors
- ✅ All files present in `C:\CLEVIR_INCA_7_5`
- ✅ Shortcuts functional
- ✅ Application launches successfully

---

#### **Phase 3: Uninstall Validation**
**Goal:** Verify complete cleanup on uninstall

**Tests:**
1. **Uninstall via Control Panel**:
   - Settings → Apps → CLEVIR_7.5 Installation → Uninstall
2. **Verify folder removed**:
   ```powershell
   Test-Path "C:\CLEVIR_INCA_7_5"  # Should return False
   ```
3. **Verify shortcuts removed**:
   - Desktop shortcut gone
   - Start menu shortcut gone
4. **Verify registry cleaned up**:
   ```powershell
   Test-Path "HKLM:\Software\Client Solutions"  # May remain if empty
   Test-Path "HKCU:\Software\Client Solutions"  # May remain if empty
   ```

**Success Criteria:**
- ✅ Uninstall completes without errors
- ✅ Installation folder removed
- ✅ Shortcuts removed
- ✅ No orphaned files/folders

---

#### **Phase 4: Upgrade Validation** (if applicable)
**Goal:** Verify upgrade from previous version works

**Prerequisites:** Old CLEVIR installation present

**Tests:**
1. **Install old version** (using .vdproj MSI if available)
2. **Run WiX MSI** (should detect existing installation)
3. **Verify upgrade prompt** - "Upgrade existing installation?"
4. **Complete upgrade**
5. **Verify files updated** - check version numbers
6. **Verify application still works**

**Success Criteria:**
- ✅ Upgrade detected automatically
- ✅ Files updated successfully
- ✅ Application functionality preserved

**Note:** This phase optional if no upgrade scenario exists.

---

#### **Phase 5: Comparison Testing**
**Goal:** Verify WiX MSI matches .vdproj behavior

**Tests:**
1. **Install .vdproj MSI** on test machine #1
2. **Install WiX MSI** on test machine #2
3. **Compare file listings**:
   ```powershell
   # Machine 1
   Get-ChildItem "C:\CLEVIR_INCA_7_5" -Recurse | Select Name, Length | Export-Csv vdproj_files.csv

   # Machine 2
   Get-ChildItem "C:\CLEVIR_INCA_7_5" -Recurse | Select Name, Length | Export-Csv wix_files.csv

   # Compare
   Compare-Object (Import-Csv vdproj_files.csv) (Import-Csv wix_files.csv) -Property Name
   ```
4. **Compare shortcuts** - verify properties identical
5. **Compare registry entries**:
   ```powershell
   # Export registry keys from both machines and compare
   ```

**Success Criteria:**
- ✅ File lists identical (or document differences)
- ✅ Shortcuts have same properties
- ✅ Registry entries match

---

### Smoke Tests

**After each installation, perform these quick checks:**

1. **Application Launch Test**
   - Double-click desktop shortcut
   - Application window appears within 10 seconds
   - No error dialogs displayed

2. **Basic Functionality Test**
   - Open configuration file (CLEVIR.ini)
   - Verify application reads configuration correctly
   - Perform one core application function (e.g., connect to device, load data)

3. **File Integrity Test**
   - Spot-check 5-10 random files exist and have correct sizes
   - Verify no "file not found" errors in application logs

**Duration:** ~5 minutes per smoke test

---

### Comprehensive Validation

**Before declaring migration complete, perform full validation:**

#### **Validation Checklist:**

**Installation:**
- [ ] MSI installs on Windows 10
- [ ] MSI installs on Windows 11
- [ ] Install to default location works
- [ ] Install to custom location works (if customizable)
- [ ] Silent install works (`msiexec /i CLEVIR_7.5_Setup.msi /qn`)
- [ ] Repair install works (Control Panel → Repair)

**Files:**
- [ ] All 60+ files present after install
- [ ] File sizes match expected values
- [ ] No extra/unexpected files deployed
- [ ] HesaiWrapper.dll (C++ DLL) loads correctly
- [ ] CLEVIR_INCA_7_5.exe launches
- [ ] AutoUpdater.exe launches

**Shortcuts:**
- [ ] Desktop shortcut #1 created
- [ ] Desktop shortcut #2 created (if applicable)
- [ ] Start menu shortcut created
- [ ] Shortcuts point to correct executable
- [ ] Shortcut icons display correctly
- [ ] Shortcuts work after reboot

**Registry:**
- [ ] HKLM\Software\Client Solutions key created
- [ ] HKCU\Software\Client Solutions key created
- [ ] No unexpected registry entries

**Prerequisites:**
- [ ] .NET Framework 4.8 check works (error if missing)
- [ ] Installer blocks if .NET missing

**Uninstall:**
- [ ] Uninstall via Control Panel works
- [ ] All files removed
- [ ] Shortcuts removed
- [ ] Registry entries cleaned up
- [ ] No orphaned folders

**Upgrade:**
- [ ] Upgrade from previous version works (if applicable)
- [ ] Settings preserved after upgrade

**Edge Cases:**
- [ ] Install works with non-ASCII username
- [ ] Install works with limited disk space warning
- [ ] Install works on non-C: drive (if configurable)
- [ ] Multiple simultaneous installs blocked correctly

**Documentation:**
- [ ] Installation instructions updated
- [ ] Build instructions updated
- [ ] Known issues documented (if any)

---

### Test Environments

**Required Test Machines:**

1. **Windows 10 (64-bit)** - Clean VM
2. **Windows 11 (64-bit)** - Clean VM
3. **Developer Machine** - With Visual Studio 2022, for build testing

**Optional:**
- Windows Server 2019/2022 (if deployed to servers)

---

### Automated Testing (Future Enhancement)

**Not required for initial migration, but consider later:**

- PowerShell test scripts for file/registry validation
- Pester tests for installation verification
- CI/CD pipeline integration (automated build + test)

---

## Complexity & Effort Assessment

### Relative Complexity

**Overall Project Complexity:** Medium

### Phase-by-Phase Breakdown

| Phase | Complexity | Dependencies | Risk | Notes |
|-------|-----------|--------------|------|-------|
| 1. Environment Setup | Low | None | Low | Standard WiX installation |
| 2. Project Creation | Low | Phase 1 | Low | Template-based |
| 3. File Mapping | **High** | Phase 2 | **Medium** | 60+ files, requires careful mapping and organization |
| 4. Features & UI | Medium | Phase 3 | Medium | Standard features, some customization |
| 5. Build & Testing | Medium | Phase 4 | **Medium** | Multiple test scenarios, comparison with .vdproj |
| 6. Integration | Low | Phase 5 validated | Low | Add to solution, update docs |

### Resource Requirements

**Skills Needed:**
- **XML/WiX syntax** - Medium proficiency required for editing .wxs files
- **Windows Installer knowledge** - Understanding of components, features, upgrades
- **MSBuild** - Basic knowledge for project file configuration
- **Testing** - Systematic approach to validation

**Tools Needed:**
- WiX Toolset v4.x (or v3.14)
- Visual Studio 2022 (optional, for GUI editing)
- Orca MSI editor (optional, for inspection)
- Clean Windows VMs (for testing)

**Estimated Sessions:**
- Session 1: Environment setup + project creation (1-2 hours)
- Session 2: File mapping components A-C (2-3 hours)
- Session 3: File mapping components D-F + features (2-3 hours)
- Session 4: UI configuration + build (1-2 hours)
- Session 5: Testing + validation (2-4 hours)
- Session 6: Integration + documentation (1 hour)

**Total Effort:** 9-15 hours (distributed across multiple sessions)

### Parallel Execution Opportunities

**None** - This is a single-project migration with sequential dependencies. All work must be done serially.

**However:**
- Testing can overlap with documentation updates
- Build configuration can be refined while testing progresses

---

## Source Control Strategy

### Branching Strategy

**Recommended: Feature Branch Workflow**

```
master (current stable branch)
  ↓
  └── feature/wix-installer-migration (new branch)
        ↓
        └── [Work on WiX project]
        ↓
        └── [Test & validate]
        ↓
        └── Merge to master (after validation)
```

**Branch Creation:**
```bash
git checkout master
git pull origin master
git checkout -b feature/wix-installer-migration
```

**Branch Purpose:**
- Isolate WiX migration work from main development
- Allow parallel work on other features (if needed)
- Enable easy rollback if issues discovered

---

### Commit Strategy

**Commit Frequency:** After each logical milestone

**Recommended Commit Sequence:**

1. **Initial Setup**
   ```bash
   git add "CLEVIR_7.5 Installation/CLEVIR_Installer.wixproj"
   git add "CLEVIR_7.5 Installation/Product.wxs"
   git commit -m "chore: Create WiX installer project skeleton"
   ```

2. **Product Configuration**
   ```bash
   git add "CLEVIR_7.5 Installation/Product.wxs"
   git commit -m "feat(installer): Configure product metadata and directory structure"
   ```

3. **File Mapping - Main Application**
   ```bash
   git add "CLEVIR_7.5 Installation/Product.wxs"
   git commit -m "feat(installer): Add main application components (exe + dependencies)"
   ```

4. **File Mapping - Libraries**
   ```bash
   git add "CLEVIR_7.5 Installation/Product.wxs"
   git commit -m "feat(installer): Add third-party library components"
   ```

5. **File Mapping - Configuration Files**
   ```bash
   git add "CLEVIR_7.5 Installation/Product.wxs"
   git commit -m "feat(installer): Add configuration file components"
   ```

6. **File Mapping - Data Files**
   ```bash
   git add "CLEVIR_7.5 Installation/Product.wxs"
   git commit -m "feat(installer): Add data file components"
   ```

7. **Features & Shortcuts**
   ```bash
   git add "CLEVIR_7.5 Installation/Product.wxs"
   git commit -m "feat(installer): Add desktop and start menu shortcuts"
   ```

8. **Registry Configuration**
   ```bash
   git add "CLEVIR_7.5 Installation/Product.wxs"
   git commit -m "feat(installer): Configure registry entries"
   ```

9. **UI Customization**
   ```bash
   git add "CLEVIR_7.5 Installation/Product.wxs"
   git commit -m "feat(installer): Configure UI dialog sequence"
   ```

10. **Build Configuration**
    ```bash
    git add "CLEVIR_7.5 Installation/CLEVIR_Installer.wixproj"
    git commit -m "feat(installer): Configure Debug/Release build outputs"
    ```

11. **Prerequisites**
    ```bash
    git add "CLEVIR_7.5 Installation/Product.wxs"
    git commit -m "feat(installer): Add .NET Framework 4.8 prerequisite check"
    ```

12. **Solution Integration**
    ```bash
    git add "CLEVIR_INCA_7_5.sln"
    git commit -m "feat(installer): Add WiX project to solution"
    ```

13. **Remove Legacy Installer** (after validation)
    ```bash
    git rm "CLEVIR_7.5 Installation/CLEVIR_7.5 Installation.vdproj"
    git commit -m "chore: Remove legacy .vdproj installer (replaced by WiX)"
    ```

14. **Documentation**
    ```bash
    git add README.md
    git add "CLEVIR_7.5 Installation/README.md"
    git commit -m "docs: Update installation and build documentation for WiX"
    ```

**Commit Message Format:**
- Use conventional commits: `type(scope): description`
- Types: `feat`, `fix`, `chore`, `docs`, `test`
- Keep messages concise but descriptive

---

### Review and Merge Process

**Pre-Merge Checklist:**

- [ ] All validation tests passed (see Success Criteria below)
- [ ] WiX MSI tested on Windows 10 and Windows 11
- [ ] Side-by-side comparison with .vdproj completed
- [ ] Documentation updated
- [ ] Build succeeds in clean environment
- [ ] No merge conflicts with master

**Pull Request (if using PR workflow):**

1. **Create PR** from `feature/wix-installer-migration` to `master`
2. **PR Description** should include:
   - Summary of changes
   - Testing performed
   - Known issues (if any)
   - Comparison results with .vdproj
3. **Request review** from team lead or senior developer
4. **Address feedback**
5. **Merge** after approval

**Merge Command:**
```bash
git checkout master
git merge feature/wix-installer-migration --no-ff
git push origin master
```

**Tag Release** (optional):
```bash
git tag -a v7.5.1-wix -m "WiX installer implementation"
git push origin v7.5.1-wix
```

---

### Rollback Procedure

**If WiX migration causes issues after merge:**

**Option 1: Revert Merge Commit**
```bash
git revert -m 1 <merge_commit_hash>
git push origin master
```

**Option 2: Restore .vdproj**
```bash
git checkout <commit_before_removal> -- "CLEVIR_7.5 Installation/CLEVIR_7.5 Installation.vdproj"
git commit -m "chore: Restore .vdproj installer temporarily"
```

**Option 3: Branch Rollback**
```bash
# If merge not yet pushed
git reset --hard HEAD~1
```

---

### File Exclusions

**Update `.gitignore`** to exclude WiX build artifacts:

```gitignore
# WiX build outputs
*.wixobj
*.wixpdb
*.wixlibpdb
bin/
obj/

# MSI build outputs
*.msi
*.wixpdb
*.cab

# Existing .vdproj outputs (already excluded)
*.msi
Debug/
Release/
```

---

## Success Criteria

### Technical Success Criteria

**Build & Deployment:**
- ✅ WiX project builds successfully in Visual Studio 2022
- ✅ MSI generated for both Debug and Release configurations
- ✅ MSI file size reasonable (~10-50MB)
- ✅ Build succeeds on clean machine (no dependencies on developer environment)

**Installation:**
- ✅ MSI installs successfully on Windows 10 (64-bit)
- ✅ MSI installs successfully on Windows 11 (64-bit)
- ✅ All 60+ files deployed to `C:\CLEVIR_INCA_7_5`
- ✅ Files have correct permissions (read/execute)
- ✅ No errors in Windows Event Log during installation

**Application Functionality:**
- ✅ CLEVIR_INCA_7_5.exe launches successfully after install
- ✅ Application passes smoke test (basic functionality works)
- ✅ No "DLL not found" or "file missing" errors
- ✅ HesaiWrapper.dll (C++ component) loads correctly
- ✅ Configuration files (CLEVIR.ini, config.xml) read successfully

**Shortcuts:**
- ✅ Desktop shortcut created
- ✅ Start menu shortcut created under "CLEVIR_7.5 Installation"
- ✅ Shortcuts point to correct executable path
- ✅ Shortcut icons display correctly
- ✅ Shortcuts functional after reboot

**Registry:**
- ✅ HKLM\Software\Client Solutions key created (if required)
- ✅ HKCU\Software\Client Solutions key created (if required)
- ✅ Registry entries match .vdproj behavior

**Prerequisites:**
- ✅ Installer checks for .NET Framework 4.8
- ✅ Clear error message if prerequisite missing
- ✅ Installer blocked if .NET Framework 4.8 not installed

**Uninstall:**
- ✅ Uninstall via Control Panel completes successfully
- ✅ All files removed from `C:\CLEVIR_INCA_7_5`
- ✅ Shortcuts removed (desktop + start menu)
- ✅ Registry entries cleaned up (or marked for cleanup)
- ✅ No orphaned files or folders

**Upgrade:**
- ✅ Upgrade detection works (if previous version installed)
- ✅ Upgrade completes successfully
- ✅ Application functions correctly after upgrade

**Silent Install:**
- ✅ Silent install works: `msiexec /i CLEVIR_7.5_Setup.msi /qn`
- ✅ Silent uninstall works: `msiexec /x {ProductCode} /qn`

---

### Quality Criteria

**Code Quality:**
- ✅ WiX XML well-formatted and readable
- ✅ Components organized into logical ComponentGroups
- ✅ No hardcoded paths (use variables like `$(var.Configuration)`)
- ✅ Comments added for complex sections
- ✅ Component IDs follow consistent naming convention

**Maintainability:**
- ✅ Product.wxs file modular (fragments for different sections)
- ✅ Easy to add/remove files in future
- ✅ Build configuration clear and documented
- ✅ No "magic numbers" or unexplained values

**Documentation:**
- ✅ README.md updated with WiX build instructions
- ✅ Installation instructions updated
- ✅ Developer setup guide includes WiX Toolset installation
- ✅ Known issues documented (if any)
- ✅ Troubleshooting section added

**Parity with .vdproj:**
- ✅ File layout matches .vdproj-generated MSI
- ✅ Shortcut properties match
- ✅ Registry entries match
- ✅ Installation path identical (`C:\CLEVIR_INCA_7_5`)
- ✅ UI dialog flow similar (Welcome → Folder → Confirm → Progress → Finished)

---

### Process Criteria

**Source Control:**
- ✅ All WiX files committed to git
- ✅ Commit messages clear and descriptive
- ✅ Feature branch merged to master after validation
- ✅ .vdproj removed from solution (after WiX validated)

**Testing:**
- ✅ All tests in "Testing & Validation Strategy" section passed
- ✅ Side-by-side comparison with .vdproj completed
- ✅ Test results documented

**Team Alignment:**
- ✅ Team aware of migration timeline
- ✅ Stakeholders approve WiX migration
- ✅ Deployment process updated (if needed)

---

### Acceptance Criteria

**To consider the migration COMPLETE and SUCCESSFUL, all of the following must be true:**

1. ✅ **WiX MSI builds successfully** in Visual Studio and command line
2. ✅ **All Technical Success Criteria** (above) met
3. ✅ **Fresh install test passed** on Windows 10 and Windows 11
4. ✅ **Uninstall test passed** - no orphaned files
5. ✅ **Application launches and functions** correctly after install
6. ✅ **Side-by-side comparison** shows no critical differences from .vdproj
7. ✅ **Documentation updated** with WiX build instructions
8. ✅ **Source control** - all changes committed and merged
9. ✅ **Team approval** - stakeholders sign off on migration
10. ✅ **.vdproj removed** from solution (keeping in git history for reference)

**If any criteria not met:**
- Document issue in Known Issues section
- Create remediation plan
- Optionally defer non-critical items to future iterations

---

### Post-Migration Validation

**One week after deployment:**
- ✅ Monitor user reports for installation issues
- ✅ Verify no increase in installation failure rate
- ✅ Confirm no unexpected behavior in production

**One month after deployment:**
- ✅ Review telemetry/logs for installer success rate
- ✅ Gather user feedback on installation experience
- ✅ Document lessons learned for future installer updates

---

*Migration plan complete. Ready for execution phase.*

---

**Plan Version:** 1.0  
**Last Updated:** 2025  
**Status:** Ready for Review
