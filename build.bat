@echo off
REM Builds Dolftasker.2.exe using the C# compiler that ships with Windows (.NET Framework 4).
REM No SDK or extra downloads required.

setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist "%CSC%" (
    echo Could not find csc.exe ^(.NET Framework 4^).
    exit /b 1
)

if not exist "%~dp0bin" mkdir "%~dp0bin"

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ ^
    /out:"%~dp0bin\Dolftasker.2.exe" ^
    /win32manifest:"%~dp0src\app.manifest" ^
    /reference:System.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.Windows.Forms.dll ^
    "%~dp0src\MacroRecorder.cs" "%~dp0src\DolfwaUi.cs"

if errorlevel 1 (
    echo Build FAILED.
    exit /b 1
)

echo Build OK -^> bin\Dolftasker.2.exe
endlocal
