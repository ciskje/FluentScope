@echo off
rem Build the FluentScope installer: self-contained single file + Inno Setup.
setlocal
cd /d "%~dp0"
dotnet publish FluentScope.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish\app
if errorlevel 1 goto :error
set "ISCC=%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles(x86)%\Inno Setup 7\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%ProgramFiles%\Inno Setup 7\ISCC.exe"
if not exist "%ISCC%" set "ISCC=%LOCALAPPDATA%\Programs\Inno Setup 7\ISCC.exe"
if not exist "%ISCC%" (
  echo Inno Setup 6 or later not found. Install it from https://jrsoftware.org/isdl.php then run setup.bat again.
  goto :error
)
cd installer
"%ISCC%" FluentScope.iss
if errorlevel 1 goto :error
cd ..
echo.
echo Installer ready: %~dp0installer\FluentScope-Setup-<version>-win-x64.exe
pause
exit /b 0
:error
echo BUILD FAILED
pause
exit /b 1
