; FluentScope setup script (Inno Setup 6). Build it with setup.bat.
; Per-machine install, administrator rights required.
; The version is read from the published exe (auto-incremented at build time
; in FluentScope.csproj), so no manual sync is needed.

#define AppSource "..\publish\app\*"
#define AppExe "..\publish\app\FluentScope.exe"
; Full file version is X.Y.Z.0 (revision always 0: <Version> in the csproj
; is X.Y.Z). Setup name carries only X.Y.Z.
#define AppVersionFull GetVersionNumbersString(AppExe)
#define _P1 Pos(".", AppVersionFull)
#define _V1 Copy(AppVersionFull, 1, _P1 - 1)
#define _R1 Copy(AppVersionFull, _P1 + 1, Len(AppVersionFull))
#define _P2 Pos(".", _R1)
#define _V2 Copy(_R1, 1, _P2 - 1)
#define _R2 Copy(_R1, _P2 + 1, Len(_R1))
#define _P3 Pos(".", _R2)
#if _P3 > 0
  #define _V3 Copy(_R2, 1, _P3 - 1)
#else
  #define _V3 _R2
#endif
#define AppVersion _V1 + "." + _V2 + "." + _V3

[Setup]
AppId={{6E6CBE46-A941-464A-8398-56089B203891}}
AppName=FluentScope
AppVersion={#AppVersion}
AppPublisher=Francesco Ferrara
AppPublisherURL=https://www.elenafrancesco.org
DefaultDirName={commonpf}\FluentScope
DefaultGroupName=FluentScope
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=.
OutputBaseFilename=FluentScope-Setup-{#AppVersion}-win-x64
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion={#AppVersion}
VersionInfoProductName=FluentScope
VersionInfoCompany=Francesco Ferrara
VersionInfoDescription=FluentScope setup
SetupIconFile=..\icon.ico
UninstallDisplayIcon={app}\FluentScope.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#AppSource}"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs; Excludes: "*.pdb"

[Icons]
Name: "{group}\FluentScope"; Filename: "{app}\FluentScope.exe"; Comment: "Shows USB device speeds"
Name: "{autodesktop}\FluentScope"; Filename: "{app}\FluentScope.exe"; Tasks: desktopicon; Comment: "Shows USB device speeds"

[Run]
Filename: "{app}\FluentScope.exe"; Description: "Launch FluentScope"; Flags: nowait postinstall skipifsilent

; The app is published self-contained (win-x64), so no .NET runtime is needed on the target machine.
