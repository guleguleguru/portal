; Inno Setup script for Desktop Portal 0.1.0-beta.
; Build release files first:
;   powershell -ExecutionPolicy Bypass -File ..\scripts\publish-release.ps1

[Setup]
AppId={{C672A1DD-5046-4A24-899B-7B76FD04E9E4}
AppName=Desktop Portal
AppVersion=0.1.0-beta
AppPublisher=Desktop Portal Contributors
AppPublisherURL=https://example.invalid/desktop-portal
AppSupportURL=https://example.invalid/desktop-portal
AppUpdatesURL=https://example.invalid/desktop-portal
DefaultDirName={autopf}\Desktop Portal
DefaultGroupName=Desktop Portal
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=DesktopPortal-0.1.0-beta-win-x64-setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\DesktopPortal.exe
SetupIconFile=..\DesktopPortal\Assets\DesktopPortal.ico

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\artifacts\DesktopPortal-0.1.0-beta-win-x64\DesktopPortal.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Desktop Portal"; Filename: "{app}\DesktopPortal.exe"
Name: "{autodesktop}\Desktop Portal"; Filename: "{app}\DesktopPortal.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\DesktopPortal.exe"; Description: "Launch Desktop Portal"; Flags: nowait postinstall skipifsilent
