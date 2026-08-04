; ============================================================================
;  XEON Dashboard — Inno Setup installer
;  Produces a friendly, no-administrator setup.exe that:
;    * installs the self-contained app (no .NET install needed by the user)
;    * silently installs the WebView2 Runtime if it's missing
;    * adds Start Menu and optional "start with Windows" shortcuts
;    * uninstalls cleanly
;
;  Build with Inno Setup 6 (https://jrsoftware.org/isdl.php):
;    - Publish the app first (see build.ps1), so ..\publish\ is populated.
;    - Optionally place MicrosoftEdgeWebView2Setup.exe next to this script.
;    - Compile: iscc XeonDashboard.iss   (or open in the Inno Setup IDE)
; ============================================================================

#define AppName "XEON Dashboard"
#define AppVersion "0.1.0"
#define AppPublisher "XEON Dashboard"
#define AppExeName "XeonDashboard.exe"

[Setup]
AppId={{7C4B2E90-3A1D-4E77-9C2E-2F8B1A6C4D30}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; No administrator rights required.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=Output
OutputBaseFilename=XEON-Dashboard-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "startupicon"; Description: "Start {#AppName} automatically when Windows signs in"; GroupDescription: "Startup:"
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
; The published, self-contained app folder. Populate ..\publish\ before compiling.
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
; Optional bundled WebView2 bootstrapper (used only if the runtime is missing).
Source: "MicrosoftEdgeWebView2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall skipifsourcedoesntexist

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: startupicon

[Run]
; Install WebView2 only if it's missing. Bootstrapper does a per-user install
; without elevation.
Filename: "{tmp}\MicrosoftEdgeWebView2Setup.exe"; Parameters: "/silent /install"; \
  Check: WebView2Missing and FileExistsInTmp; Flags: waituntilterminated
; Offer to launch after install.
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName} now"; \
  Flags: nowait postinstall skipifsilent

[Code]
function WebView2Missing: Boolean;
var
  pv: String;
begin
  // The Evergreen runtime records its version under this client GUID.
  // Check per-machine (both registry views) and per-user.
  Result := True;
  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', pv) and (pv <> '') then
    Result := False
  else if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', pv) and (pv <> '') then
    Result := False
  else if RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', pv) and (pv <> '') then
    Result := False;
end;

function FileExistsInTmp: Boolean;
begin
  Result := FileExists(ExpandConstant('{tmp}\MicrosoftEdgeWebView2Setup.exe'));
end;
