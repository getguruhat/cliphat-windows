#define AppName "ClipHat"
#define AppVersion "1.5.0"
[Setup]
AppId={{E1A90BA8-26D8-47D3-85A6-7CC8C8652B4F}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
VersionInfoVersion={#AppVersion}
VersionInfoProductVersion={#AppVersion}
AppPublisher=GuruHat
AppPublisherURL=https://guruhat.com/apps/cliphat/
DefaultDirName={autopf}\ClipHat
DefaultGroupName=ClipHat
OutputDir=dist
OutputBaseFilename=ClipHat-{#AppVersion}-Windows-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=assets\ClipHatSetup.ico
WizardSmallImageFile=assets\SetupWizardSmall-55.bmp,assets\SetupWizardSmall-110.bmp
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
[Code]
// ClipHat needs .NET Framework 4.8 (release 528040). It's part of Windows 10 1903 and later
// and Windows 11, so this only stops PCs on older Windows 10 builds that never installed it.
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
  ErrorCode: Integer;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    if MsgBox('ClipHat needs Microsoft .NET Framework 4.8, which isn''t installed on this PC.' + #13#10#13#10 +
              'Open the Microsoft download page now?', mbError, MB_YESNO) = IDYES then
      ShellExecAsOriginalUser('open', 'https://dotnet.microsoft.com/download/dotnet-framework/net48', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;
