[Setup]
; Application information
AppId={{B8E88A4D-9F3E-4B5C-8A7D-2E1F6C9B4A8E}
AppName=RestWave
AppVersion={#GetEnv('APP_VERSION') != '' ? GetEnv('APP_VERSION') : '1.0.0'}
AppVerName=RestWave {#GetEnv('APP_VERSION') != '' ? GetEnv('APP_VERSION') : '1.0.0'}
AppPublisher=RestWave
AppPublisherURL=https://github.com/avi1989/real-rest-client
AppSupportURL=https://github.com/avi1989/real-rest-client/issues
AppUpdatesURL=https://github.com/avi1989/real-rest-client/releases
DefaultDirName={autopf}\RestWave
DefaultGroupName=RestWave
AllowNoIcons=yes
LicenseFile=LICENSE
OutputDir=installer
OutputBaseFilename=RestWave-Setup-{#GetEnv('APP_VERSION') != '' ? GetEnv('APP_VERSION') : '1.0.0'}
SetupIconFile=RestWave\Assets\logo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
Source: "publish\RestWave-windows-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\RestWave"; Filename: "{app}\RestWave.exe"
Name: "{group}\{cm:UninstallProgram,RestWave}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\RestWave"; Filename: "{app}\RestWave.exe"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\RestWave"; Filename: "{app}\RestWave.exe"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\RestWave.exe"; Description: "{cm:LaunchProgram,RestWave}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;

function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
  Result := 0;
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep=ssInstall) then
  begin
    if (IsUpgrade()) then
    begin
      UnInstallOldVersion();
    end;
  end;
end;