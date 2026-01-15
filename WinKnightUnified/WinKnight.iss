; WinKnight Installer Script for Inno Setup
; Requires Inno Setup 6.0+ (https://jrsoftware.org/isinfo.php)

#define MyAppName "WinKnight"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "WinKnight Project"
#define MyAppURL "https://github.com/WinKnight"
#define MyAppExeName "WinKnightUI.exe"

[Setup]
; Application info
AppId={{F5A7D3E2-8B9C-4E1F-A2D6-7C3E8F9A1B4D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

; Installation directories
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes

; Output configuration
OutputDir=..\Installer\Output
OutputBaseFilename=WinKnight_Setup_v{#MyAppVersion}
SetupIconFile=WinKnightUI\Assets\favicon.ico
Compression=lzma2/ultra64
SolidCompression=yes

; Modern UI settings
WizardStyle=modern
WizardSizePercent=110

; Privileges
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog

; Windows version requirements
MinVersion=10.0

; Uninstaller
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}

; License and info
LicenseFile=LICENSE.txt
InfoBeforeFile=README.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode
Name: "startupicon"; Description: "Run WinKnight on Windows startup"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
; Main application files (Release build)
Source: "WinKnightUI\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Assets
Source: "WinKnightUI\Assets\favicon.ico"; DestDir: "{app}"; Flags: ignoreversion
; Helper executables
Source: "CacheCleaner\bin\Release\net8.0-windows\*"; DestDir: "{app}\Tools\CacheCleaner"; Flags: ignoreversion recursesubdirs
Source: "WindowsUpdateManager\bin\Release\net8.0-windows\*"; DestDir: "{app}\Tools\WindowsUpdateManager"; Flags: ignoreversion recursesubdirs
Source: "SelfHeal\bin\Release\net8.0-windows\*"; DestDir: "{app}\Tools\SelfHeal"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\favicon.ico"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\favicon.ico"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Registry]
; Add to startup if selected
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "WinKnight"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon
; App path for easier command line access
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\App Paths\{#MyAppExeName}"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName}"; Flags: uninsdeletekey

[Run]
; Option to run after install
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[UninstallDelete]
; Clean up logs on uninstall
Type: filesandordirs; Name: "{localappdata}\WinKnight"
Type: filesandordirs; Name: "{commonappdata}\WinKnight"

[Code]
// Check for .NET 8 runtime
function IsDotNet8Installed: Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  // For simplicity, we assume .NET is installed if dotnet command works
  // In production, you'd parse the output to check for specific version
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  // You could add .NET runtime check here and prompt user to install it
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Post-installation tasks could go here
  end;
end;

[Messages]
WelcomeLabel2=This will install [name/ver] on your computer.%n%nWinKnight is a powerful Windows system utility that monitors your system health, manages startup programs, and provides quick system maintenance tools.%n%nIt is recommended that you close all other applications before continuing.

[CustomMessages]
english.LaunchingApp=Launching WinKnight...
