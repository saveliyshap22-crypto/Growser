#ifndef MyAppVersion
  #define MyAppVersion "0.5.0"
#endif

#define MyAppName "Chroma Browser"
#define MyAppPublisher "Chroma Browser contributors"
#define MyAppExeName "ChromaBrowser.exe"
#define MyAppURL "https://github.com/saveliyshap22-crypto/Growser"

[Setup]
AppId={{D38B9387-77C7-44F6-9C66-625766E647A8}
AppName={#MyAppName}
AppVerName={#MyAppName} {#MyAppVersion}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases/latest
AppComments=Быстрый Chromium-браузер с адаптивными вкладками, AI-панелью и встроенной защитой.
DefaultDirName={localappdata}\Programs\Chroma Browser
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=auto
DisableWelcomePage=no
AllowNoIcons=yes
UsePreviousAppDir=yes
UsePreviousTasks=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\artifacts\installer
OutputBaseFilename=ChromaBrowser-Setup-{#MyAppVersion}-x64
SetupIconFile=..\src\Chroma.Browser\Resources\Chroma.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName} {#MyAppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
WizardResizable=no
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
CloseApplications=yes
RestartApplications=no
ChangesAssociations=yes
SetupLogging=yes
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Chroma Browser Installer
VersionInfoProductName={#MyAppName}
VersionInfoCopyright=Open-source Chroma Browser contributors

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать значок на рабочем столе"; GroupDescription: "Ярлыки:"; Flags: unchecked
Name: "defaultbrowser"; Description: "После установки открыть выбор браузера по умолчанию"; GroupDescription: "Windows:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\ChromaBrowser"; ValueType: string; ValueName: ""; ValueData: "Chroma Browser"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\ChromaBrowser\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCU; Subkey: "Software\Clients\StartMenuInternet\ChromaBrowser\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"""
Root: HKCU; Subkey: "Software\ChromaBrowser\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "Chroma Browser"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\ChromaBrowser\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "Быстрый браузер на основе Chromium"
Root: HKCU; Subkey: "Software\ChromaBrowser\Capabilities\URLAssociations"; ValueType: string; ValueName: "http"; ValueData: "ChromaBrowserURL"
Root: HKCU; Subkey: "Software\ChromaBrowser\Capabilities\URLAssociations"; ValueType: string; ValueName: "https"; ValueData: "ChromaBrowserURL"
Root: HKCU; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "Chroma Browser"; ValueData: "Software\ChromaBrowser\Capabilities"; Flags: uninsdeletevalue
Root: HKCU; Subkey: "Software\Classes\ChromaBrowserURL"; ValueType: string; ValueName: ""; ValueData: "Chroma Browser URL"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Classes\ChromaBrowserURL"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\ChromaBrowserURL\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKCU; Subkey: "Software\Classes\ChromaBrowserURL\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Установка Chromium WebView2 Runtime…"; Flags: waituntilterminated skipifdoesntexist; Check: WebView2RuntimeMissing
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить Chroma Browser"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent
Filename: "ms-settings:defaultapps"; Description: "Выбрать Chroma Browser по умолчанию"; Flags: shellexec postinstall skipifsilent unchecked; Tasks: defaultbrowser

[Code]
function WebView2RuntimeMissing: Boolean;
begin
  Result :=
    (not DirExists(ExpandConstant('{pf32}\Microsoft\EdgeWebView\Application'))) and
    (not DirExists(ExpandConstant('{localappdata}\Microsoft\EdgeWebView\Application')));
end;

procedure InitializeWizard;
begin
  WizardForm.Caption := '{#MyAppName} {#MyAppVersion}';
  WizardForm.WelcomeLabel1.Caption := 'Установка Chroma Browser';
  WizardForm.WelcomeLabel2.Caption :=
    'Установщик обновит существующую версию без удаления настроек, проверит Chromium WebView2 и создаст выбранные ярлыки.';
end;
