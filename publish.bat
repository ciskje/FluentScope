@echo off
rem Build FluentScope in Release and publish the executable to .\publish\
cd /d "%~dp0"
dotnet publish FluentScope.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish
echo.
echo Published to: %~dp0publish\FluentScope.exe
pause
