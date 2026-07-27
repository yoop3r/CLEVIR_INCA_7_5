The difference is clear from the dependency analysis. Here's a precise breakdown:
Why Debug ≠ Release Outside the Dev Machine
					Debug DLL									Release DLL
Size				2,190,336 bytes								429,568 bytes
CRT deps			MSVCP140D.dll, 								MSVCP140.dll,
					VCRUNTIME140D.dll, 							VCRUNTIME140.dll,
					ucrtbased.dll	  							api-ms-win-crt-*.dll
Redistributable?	❌ No — only installed with Visual Studio	✅ Yes — ships with the VC++ Redistributable

The Debug build links against the Debug CRT (*D.dll), which is not redistributable and only exists on machines with Visual Studio installed. That's why it fails outside the dev environment.

What to Deploy
Use the Release|x64 build:
C:\DEV\CLEVIR\CLEVIR_INCA_7_5\bin\x64\Release\HesaiWrapper.dll

Also deploy these alongside it (or install the prerequisite):

Option A — Install the VC++ Redistributable on the target machine
Download and run the Visual C++ 2015–2022 Redistributable (x64) on the target:
•	vc_redist.x64.exe from Microsoft's download page - https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170
This installs MSVCP140.dll, VCRUNTIME140.dll, VCRUNTIME140_1.dll, and the api-ms-win-crt-*.dll forwarder DLLs.

Option B — Bundle the CRT DLLs locally (side-by-side)
Copy these files from C:\Windows\System32 (or your VS install's Redist\x64) next to HesaiWrapper.dll:
•	MSVCP140.dll
•	VCRUNTIME140.dll
•	VCRUNTIME140_1.dll
The api-ms-win-crt-*.dll files are Windows API sets — they are always present on Windows 10/11. No action needed for those.

Build tip
To build the Release x64 from the command line without opening Visual Studio:
msbuild "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\HesaiWrapper\HesaiWrapper\HesaiWrapper.vcxproj" /p:Configuration=Release /p:Platform=x64
The post-build xcopy in the project will automatically drop the output into bin\x64\Release\.


Update to build process

Someone has been rebuilding only the Debug configuration of HesaiWrapper.vcxproj directly (e.g., via IDE with Debug selected), so the Release native DLL has silently drifted out of date relative to the current source.
Most effective way to produce a correct, current Release DLL:
1.	Always build via the solution, in Release, not the vcxproj standalone in isolation — that's what guarantees the .sln's config mapping applies and keeps the native DLL in lockstep with whatever else changed. From the IDE: set the Solution Configuration dropdown to Release, then Build Solution (not just the C++ project).
2.	Or, from the command line (matches what HESAI_COMPILE.MD already documents, but building through the .sln instead of the .vcxproj directly is safer since it also validates the config mapping):

msbuild "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\CLEVIR_INCA_7_5.sln" /p:Configuration=Release /p:Platform=x64 /t:HesaiWrapper /v:minimal

This rebuilds just the native project but through the solution's Release|x64 mapping, and its PostBuildEvent drops the fresh DLL into bin\x64\Release\ automatically.
3.	Then build the VB app itself in Release so CopyHesaiWrapperNative picks up that same fresh Release DLL into the app's own output folder:

msbuild "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\CLEVIR_INCA_7_5.sln" /p:Configuration=Release /p:Platform=x64

VS 2026 is installed under version folder "18". Let's use that msbuild.
Build succeeded (only pre-existing warnings, no errors). Let's verify the fresh Release DLL landed correctly in both locations.
The Release build completed successfully:

- **MSBuild path found**: VS 2026 (internally versioned "18") lives at `C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\amd64\MSBuild.exe` — the earlier `2026` path guess was wrong.
- **Build result**: `HesaiWrapper.vcxproj` compiled clean (only pre-existing `C4244` double→float warnings from the Hesai SDK, no errors).
- **Output verified**: Both `x64\Release\HesaiWrapper.dll` and the solution's `bin\x64\Release\HesaiWrapper.dll` are now freshly built and in sync (same size, same timestamp: 417,280 bytes, just built).

You now have a current Release|x64 `HesaiWrapper.dll` at `C:\DEV\CLEVIR\CLEVIR_INCA_7_5\bin\x64\Release\HesaiWrapper.dll` — that's the one to reference/ship when running outside Visual Studio. The `CopyHesaiWrapperNative` target in `CLEVIR_INCA_7_5.vbproj` will also pick up this same Release DLL automatically the next time you build the VB app with `Configuration=Release`.

The Release build completed successfully:
•	MSBuild path found: VS 2026 (internally versioned "18") lives at C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\amd64\MSBuild.exe — the earlier 2026 path guess was wrong.
•	Build result: HesaiWrapper.vcxproj compiled clean (only pre-existing C4244 double→float warnings from the Hesai SDK, no errors).
•	Output verified: Both x64\Release\HesaiWrapper.dll and the solution's bin\x64\Release\HesaiWrapper.dll are now freshly built and in sync (same size, same timestamp: 417,280 bytes, just built).
You now have a current Release|x64 HesaiWrapper.dll at C:\DEV\CLEVIR\CLEVIR_INCA_7_5\bin\x64\Release\HesaiWrapper.dll — that's the one to reference/ship when running outside Visual Studio. The CopyHesaiWrapperNative target in CLEVIR_INCA_7_5.vbproj will also pick up this same Release DLL automatically the next time you build the VB app with Configuration=Release.