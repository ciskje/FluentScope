@echo off
rem Start FluentScope in Debug configuration (builds first if needed).
cd /d "%~dp0"
dotnet run --project FluentScope.csproj
pause
