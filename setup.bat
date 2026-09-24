@echo off
rem Build the USBScope installer: self-contained single file + Inno Setup.
setlocal
cd /d "%~dp0"
dotnet publish USBScope.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\app
if errorlevel 1 goto :error
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
  echo Inno Setup 6 not found. Install it from https://jrsoftware.org/isdl.php then run setup.bat again.
  goto :error
)
cd installer
"%ISCC%" USBScope.iss
if errorlevel 1 goto :error
cd ..
echo.
echo Installer ready: %~dp0installer\USBScope-Setup.exe
pause
exit /b 0
:error
echo BUILD FAILED
pause
exit /b 1
