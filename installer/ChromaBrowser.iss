#ifndef MyAppVersion
  #define MyAppVersion "0.3.0"
#endif

#define MyAppName "Chroma Browser"
#define MyAppPublisher "Chroma Browser contributors"
#define MyAppExeName "ChromaBrowser.exe"

[Setup]
AppId={{D38B9387-77C7-44F6-9C66-625766E647A8}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\Chroma Browser
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\artifacts\installer
OutputBaseFilename=ChromaBrowser-Setup-{#MyAppVersion}-x64
SetupIconFile=..\src\Chroma.Browser\Resources\Chroma.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Chroma Browser Installer
VersionInfoProductName={#MyAppName}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать значок на рабочем столе"; GroupDescription: "Дополнительные значки:"; Flags: unchecked

[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

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
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Проверка Chromium WebView2 Runtime…"; Flags: waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить Chroma Browser"; Flags: nowait postinstall skipifsilent
