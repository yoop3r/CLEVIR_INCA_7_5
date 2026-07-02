1. Open x64 Native Tools Command Prompt
Search for "x64 Native Tools Command Prompt for VS 2022" (or your VS version) in the Start menu.
2. Compile the Byte Finder Test Tool (x64)

cd /d "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\NCOMdecoder"
cl /O2 /W3 byte_finder.c /Fe:byte_finder.exe
.\byte_finder.exe

3. Compile the Main NCOM Decoder DLL (x64)

cd /d "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\NCOMdecoder"
cl /O2 /W3 /LD /DNCOM_DECODE_DLL_EXPORT ncom_simple.c /link /OUT:NCOMdecoder.dll

4. Copy the x64 DLL to Your Application, using Powershell

Copy-Item "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\NCOMdecoder\NCOMdecoder.dll" -Destination "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\bin\x64\Debug\" -Force
		 
5. ?? Verify Architecture
		 
dumpbin /headers "C:\DEV\CLEVIR\CLEVIR_INCA_7_5\NCOMdecoder\NCOMdecoder.dll" | Select-String "machine"
		 
Should show: 8664 machine (x64)