; Inno Setup 6.3+ script for Floating Window Organizer.
; Build it with build\build-installer.ps1, which publishes the app first and passes
; AppVersion and PublishDir. Defaults below allow compiling it straight from the Inno Setup IDE.

#define AppName "Floating Window Organizer"
#define AppPublisher "dah007"
#define AppURL "https://github.com/dah007/floating-window-organizer"
#define AppExeName "FloatingWindowOrganizer.exe"

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif

[Setup]
; AppId identifies the app for upgrades and uninstall. Never change it.
AppId={{B1E966D6-A193-431F-9421-07D66A71E5B3}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}/issues
AppUpdatesURL={#AppURL}/releases
VersionInfoVersion={#AppVersion}

; Per-user install, no admin prompt. {autopf} resolves to %LOCALAPPDATA%\Programs.
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
DisableDirPage=auto

; Windows 11 22H2 or later, 64-bit.
MinVersion=10.0.22621
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

OutputDir=..\artifacts\installer
OutputBaseFilename=FloatingWindowOrganizer-Setup-{#AppVersion}
SetupIconFile=..\src\FloatingWindowOrganizer.App\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
WizardStyle=modern
Compression=lzma2/max
SolidCompression=yes

; Close a running copy before upgrading, and start it again afterwards.
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; Checked by default.
Name: "startup"; Description: "Start {#AppName} when I sign in to Windows"; GroupDescription: "Startup:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"

[Registry]
; Per-user "Run" entry; also listed (and switchable) in Settings > Apps > Startup.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "FloatingWindowOrganizer"; ValueData: """{app}\{#AppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; The widgets have no window to close from, so stop the app before removing its files.
Filename: "{sys}\taskkill.exe"; Parameters: "/im {#AppExeName} /f"; Flags: runhidden; RunOnceId: "StopApp"

; Widget settings in %APPDATA%\FloatingWindowOrganizer are kept on uninstall so a reinstall restores them.
