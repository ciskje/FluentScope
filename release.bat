@echo off
rem Build the installer and publish it as a GitHub Release (or update.bat).
rem Usage: release.bat [tag]  (default tag: v<exe FileVersion>)
setlocal
cd /d "%~dp0"

call setup.bat < nul
if errorlevel 1 exit /b 1

for /f "usebackq delims=" %%V in (`powershell -NoProfile -Command "[System.Diagnostics.FileVersionInfo]::GetVersionInfo('publish\app\USBScope.exe').FileVersion"`) do set "VER=%%V"
if "%~1" neq "" set "VER=%~1"
set "TAG=v%VER: =%"

gh release view %TAG% > nul 2>&1
if errorlevel 1 (
  gh release create %TAG% installer\USBScope-Setup.exe --title "USBScope %TAG%" --notes "Self-contained Windows x64 installer (no .NET runtime needed). Requires administrator rights."
) else (
  echo Release %TAG% exists, uploading asset again...
  gh release upload %TAG% installer\USBScope-Setup.exe --clobber
)
