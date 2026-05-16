[Setup]
AppId={{D5E4316E-96F3-41DE-BDBA-FC4D98B0D31E}
AppName=ToggleDarkMode
AppVersion=1.0.0
AppPublisher=ToggleDarkMode
DefaultDirName={autopf}\ToggleDarkMode
DefaultGroupName=ToggleDarkMode
OutputBaseFilename=ToggleDarkMode-Setup
OutputDir=..\dist
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern
SetupIconFile=..\assets\favicon.ico
UninstallDisplayIcon={app}\ToggleDarkMode.exe
ShowLanguageDialog=no

[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "startup"; Description: "开机自动启动"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\build\ToggleDarkMode.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\scripts\build-and-run.ps1"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\assets\favicon.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\ToggleDarkMode"; Filename: "{app}\ToggleDarkMode.exe"; WorkingDir: "{app}"
Name: "{group}\Uninstall ToggleDarkMode"; Filename: "{uninstallexe}"
Name: "{autodesktop}\ToggleDarkMode"; Filename: "{app}\ToggleDarkMode.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ToggleDarkMode"; ValueData: """{app}\ToggleDarkMode.exe"""; Tasks: startup

[Run]
Filename: "{app}\ToggleDarkMode.exe"; Description: "{cm:LaunchProgram,ToggleDarkMode}"; Flags: nowait postinstall skipifsilent
