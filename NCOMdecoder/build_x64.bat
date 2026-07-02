@echo off
echo ========================================
echo Building NCOMdecoder.dll (x64)
echo ========================================

cd /d "%~dp0"

REM Try to find and run vcvars64.bat
set "VSPATH=C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
if not exist "%VSPATH%" set "VSPATH=C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvars64.bat"
if not exist "%VSPATH%" set "VSPATH=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat"
if not exist "%VSPATH%" set "VSPATH=C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\VC\Auxiliary\Build\vcvars64.bat"

if exist "%VSPATH%" (
    echo Found Visual Studio at: %VSPATH%
    call "%VSPATH%"
) else (
    echo ? Could not find vcvars64.bat
    echo Please run this from "x64 Native Tools Command Prompt for VS 2022"
    pause
    exit /b 1
)

REM Clean old files
if exist NCOMdecoder.dll del NCOMdecoder.dll
if exist ncom_simple.obj del ncom_simple.obj

echo.
echo Compiling ncom_simple.c as 64-bit...
cl /c /O2 /W3 /DNCOM_DECODE_DLL_EXPORT ncom_simple.c
if errorlevel 1 (
    echo ? Compilation failed!
    pause
    exit /b 1
)

echo.
echo Linking as 64-bit DLL...
link /DLL /OUT:NCOMdecoder.dll ncom_simple.obj
if errorlevel 1 (
    echo ? Linking failed!
    pause
    exit /b 1
)

REM Verify architecture
echo.
echo Checking DLL architecture:
dumpbin /headers NCOMdecoder.dll | findstr machine

REM Copy to output directory
if exist ..\bin\x64\Debug\ (
    copy /Y NCOMdecoder.dll ..\bin\x64\Debug\
    echo.
    echo ? DLL copied to bin\x64\Debug\
) else (
    echo ?? Output directory not found, DLL remains in NCOMdecoder folder
)

echo.
echo ? Build complete!
pause
