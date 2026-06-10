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