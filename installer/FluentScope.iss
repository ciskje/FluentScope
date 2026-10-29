; FluentScope setup script (Inno Setup 6). Build it with setup.bat.
; Per-machine install, administrator rights required.
; The version is read from the published exe (auto-incremented at build time
; in FluentScope.csproj), so no manual sync is needed.

#define AppSource "..\publish\app\*"
#define AppExe "..\publish\app\FluentScope.exe"
#define AppVersion GetVersionNumbersString(AppExe)

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
OutputBaseFilename=FluentScope-Setup
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
