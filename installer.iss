#define AppName "ClipHat"
#define AppVersion "1.2.0"
[Setup]
AppId={{E1A90BA8-26D8-47D3-85A6-7CC8C8652B4F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=GuruHat
DefaultDirName={autopf}\ClipHat
DefaultGroupName=ClipHat
OutputDir=dist
OutputBaseFilename=ClipHat-1.2.0-Windows-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=assets\ClipHat.ico
UninstallDisplayIcon={app}\ClipHat.exe
CloseApplications=force
RestartApplications=no
[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\ClipHat"; Filename: "{app}\ClipHat.exe"
Name: "{autodesktop}\ClipHat"; Filename: "{app}\ClipHat.exe"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
[Run]
Filename: "{app}\ClipHat.exe"; Description: "Launch ClipHat"; Parameters: "--show-history"; Flags: nowait postinstall skipifsilent
