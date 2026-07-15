#ifndef MyAppVersion
  #define MyAppVersion "0.6.0"
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
AppComments=Быстрый Chromium-браузер с адаптивными вкладками, медиапанелью, AI и встроенной защитой.
DefaultDirName={localappdata}\Programs\Chroma Browser
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=auto
DisableWelcomePage=no
AllowNoIcons=yes
UsePreviousAppDir=yes
UsePreviousTasks=yes
UsePreviousLanguage=yes
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
WizardResizable=yes
WizardSizePercent=110
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
Name: "startmenu"; Description: "Создать ярлык в меню Пуск"; GroupDescription: "Ярлыки:"; Flags: checkedonce

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: startmenu
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
const
  ChromaWindow = $0015110F;
  ChromaPanel = $00231D1A;
  ChromaSurface = $00282018;
  ChromaText = $00FAF8F7;
  ChromaMuted = $00C1B6B0;
  ChromaAccent = $003D8AFF;

function WebView2RuntimeMissing: Boolean;
var
  Version: String;
begin
  Result :=
    (not RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F1E7E4A4-7C55-4F2F-A4A5-09A5E38A7AB7}', 'pv', Version)) and
    (not RegQueryStringValue(HKCU, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F1E7E4A4-7C55-4F2F-A4A5-09A5E38A7AB7}', 'pv', Version)) and
    (not DirExists(ExpandConstant('{pf32}\Microsoft\EdgeWebView\Application'))) and
    (not DirExists(ExpandConstant('{pf64}\Microsoft\EdgeWebView\Application'))) and
    (not DirExists(ExpandConstant('{localappdata}\Microsoft\EdgeWebView\Application')));
end;

procedure ApplyChromaInstallerTheme;
begin
  WizardForm.Color := ChromaWindow;
  WizardForm.MainPanel.Color := ChromaPanel;
  WizardForm.WelcomeLabel1.Font.Color := ChromaAccent;
  WizardForm.WelcomeLabel1.Font.Size := 22;
  WizardForm.WelcomeLabel1.Font.Style := [fsBold];
  WizardForm.WelcomeLabel2.Font.Color := ChromaMuted;
  WizardForm.PageNameLabel.Font.Color := ChromaAccent;
  WizardForm.PageNameLabel.Font.Style := [fsBold];
  WizardForm.PageDescriptionLabel.Font.Color := ChromaMuted;
  WizardForm.ReadyMemo.Color := ChromaSurface;
  WizardForm.ReadyMemo.Font.Color := ChromaText;
  WizardForm.FilenameLabel.Font.Color := ChromaMuted;
  WizardForm.StatusLabel.Font.Color := ChromaText;
  WizardForm.SelectDirLabel.Font.Color := ChromaText;
  WizardForm.SelectStartMenuFolderLabel.Font.Color := ChromaText;
  WizardForm.DirEdit.Color := ChromaSurface;
  WizardForm.DirEdit.Font.Color := ChromaText;
  WizardForm.GroupEdit.Color := ChromaSurface;
  WizardForm.GroupEdit.Font.Color := ChromaText;
  WizardForm.TasksList.Color := ChromaSurface;
  WizardForm.TasksList.Font.Color := ChromaText;
  WizardForm.ComponentsList.Color := ChromaSurface;
  WizardForm.ComponentsList.Font.Color := ChromaText;
end;

procedure InitializeWizard;
begin
  WizardForm.Caption := '{#MyAppName} {#MyAppVersion}';
  WizardForm.WelcomeLabel1.Caption := 'Chroma Browser {#MyAppVersion}';
  WizardForm.WelcomeLabel2.Caption :=
    'Графитово-оранжевый браузер с настраиваемой формой, жидким стеклом, медиапанелью и AI.' + #13#10 + #13#10 +
    'Установщик обновит существующую версию без удаления настроек, проверит WebView2 и создаст выбранные ярлыки.';
  ApplyChromaInstallerTheme;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  ApplyChromaInstallerTheme;
end;
