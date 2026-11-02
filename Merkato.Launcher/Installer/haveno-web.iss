[Setup]
AppId={{EA9A2026-618D-42C8-8A13-6F428EDFCD9A}
AppName=Haveno Web
AppVersion=0.0.1
WizardStyle=modern dynamic
DefaultDirName={autopf}\haveno-web
DefaultGroupName=haveno-web
UninstallDisplayIcon={app}\Merkato.Launcher.exe
Compression=lzma2
SolidCompression=yes
OutputDir=Output
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputBaseFilename=haveno-web-windows-x86_64-installer
SetupIconFile=..\Assets\haveno.ico

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\..\publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\Haveno Web"; Filename: "{app}\Merkato.Launcher.exe"
Name: "{autodesktop}\Haveno Web"; Filename: "{app}\Merkato.Launcher.exe"; Tasks: "desktopicon"
