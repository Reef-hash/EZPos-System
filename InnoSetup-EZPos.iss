; ============================================================
;  EZPos Inno Setup Script
;  Target runtime : .NET 6.0 Desktop Runtime (x64)
;  Before building: dotnet publish -c Release -r win-x64
;                   --self-contained false -o publish
; ============================================================

#define AppName    "EZPos"
#define AppVersion "1.0.0"
#define AppPublisher "EZPos"
#define AppURL     "https://github.com"
#define AppExe     "EZPos.exe"

; .NET 6 Desktop Runtime version required
#define DotNetVersion "6.0"
#define DotNetMajor   6

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
; Icon shown in Add/Remove Programs and installer window
SetupIconFile=Resources\Icons\app.ico
UninstallDisplayIcon={app}\{#AppExe}
; Output
OutputDir=installer
OutputBaseFilename=EZPos-Setup-v{#AppVersion}
; Compression
Compression=lzma2/ultra64
SolidCompression=yes
; Require Windows 7 or later (matches net6.0-windows7.0)
MinVersion=6.1
; Require 64-bit Windows
ArchitecturesRequired=x64
ArchitecturesInstallIn64BitMode=x64
; Privileges
PrivilegesRequired=admin
; Wizard appearance
WizardStyle=modern
WizardSizePercent=120

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; ============================================================
;  Files
; ============================================================
[Files]
; App binaries (build with: dotnet publish -c Release -r win-x64 --self-contained false -o publish)
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Bundle the .NET 6 Desktop Runtime offline installer so the
; setup works without internet on the target machine.
; Download from: https://dotnet.microsoft.com/en-us/download/dotnet/6.0
; File: windowsdesktop-runtime-6.0.xx-win-x64.exe  (~55 MB)
; Place it next to this .iss file before building the installer.
Source: "windowsdesktop-runtime-6.0*-win-x64.exe"; \
  DestDir: "{tmp}"; \
  Flags: deleteafterinstall; \
  Check: DotNetRuntimeMissing

; ============================================================
;  Shortcuts
; ============================================================
[Icons]
Name: "{group}\{#AppName}";            Filename: "{app}\{#AppExe}"; IconFilename: "{app}\{#AppExe}"
Name: "{group}\Uninstall {#AppName}";  Filename: "{uninstallexe}"
Name: "{userdesktop}\{#AppName}";      Filename: "{app}\{#AppExe}"; IconFilename: "{app}\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

; ============================================================
;  Run after install
; ============================================================
[Run]
; Install .NET 6 Desktop Runtime silently if missing
Filename: "{tmp}\windowsdesktop-runtime-6.0*-win-x64.exe"; \
  Parameters: "/install /quiet /norestart"; \
  StatusMsg: "Installing .NET 6 Desktop Runtime..."; \
  Check: DotNetRuntimeMissing; \
  Flags: waituntilterminated

; Launch app
Filename: "{app}\{#AppExe}"; \
  Description: "Launch {#AppName}"; \
  Flags: nowait postinstall skipifsilent

; ============================================================
;  Pascal Script — .NET runtime detection
; ============================================================
[Code]

// Returns true when .NET 6 Desktop Runtime (x64) is NOT installed.
// Checked by reading the shared registry key that the Desktop Runtime writes.
function DotNetRuntimeMissing: Boolean;
var
  key:     String;
  version: String;
begin
  Result := True;

  // .NET 6+ Desktop Runtime writes its version under:
  // HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost
  // OR under sharedfx\Microsoft.WindowsDesktop.App
  key := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  if RegQueryStringValue(HKLM, key, '{#DotNetVersion}', version) then
  begin
    Result := False;
    Exit;
  end;

  // Fallback: check for any 6.x key under the sharedfx path
  if RegKeyExists(HKLM, key) then
  begin
    // Key exists means at least one version is registered; trust it for major version check
    Result := False;
    Exit;
  end;

  // Also try the simpler dotnet host path used by some SDK/runtime combos
  key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full';
  // That key is for .NET Framework; not useful here — keep Result = True so runtime installs
end;

// Show a friendly message when .NET needs to be installed
procedure InitializeWizard();
begin
  // Nothing extra needed; status message in [Run] section handles feedback
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if DotNetRuntimeMissing then
  begin
    MsgBox(
      '.NET 6 Desktop Runtime was not detected on this machine.' + #13#10 +
      'It will be installed automatically before {#AppName} is set up.' + #13#10#13#10 +
      'An internet connection is NOT required — the runtime is bundled in this installer.',
      mbInformation, MB_OK);
  end;
end;
