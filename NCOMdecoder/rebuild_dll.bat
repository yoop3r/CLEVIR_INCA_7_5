@echo off
REM Rebuild NCOMdecoder.dll with corrected byte offsets
REM Run this script from Developer Command Prompt for VS

echo ========================================
echo Rebuilding NCOMdecoder.dll...
echo ========================================

REM Compile the C code
cl /O2 /W3 /LD /DNCOM_DECODE_DLL_EXPORT ncom_simple.c /link /OUT:NCOMdecoder.dll

if errorlevel 1 (
    echo ERROR: Compilation failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo Build successful!
echo ========================================
echo.
echo DLL created: NCOMdecoder.dll
echo.
echo NEXT STEPS:
echo 1. Close your CLEVIR application
echo 2. Copy NCOMdecoder.dll to bin\x64\Debug\
echo 3. Restart your application
echo.
pause
