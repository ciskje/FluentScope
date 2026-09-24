@echo off
rem Start USBScope in Debug configuration (builds first if needed).
cd /d "%~dp0"
dotnet run --project USBScope.csproj
pause
