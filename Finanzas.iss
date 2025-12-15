[Setup]
AppId={{B1B4D941-1AA7-4F4B-9E2E-7E1E7F8A7A10}}
AppName=Finanzas
AppVersion=1.0.0
DefaultDirName={autopf}\Finanzas
DefaultGroupName=Finanzas
DisableDirPage=yes
OutputDir=dist
OutputBaseFilename=Finanzas-Setup
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x86 x64
ArchitecturesInstallIn64BitMode=x64
UninstallDisplayIcon={app}\Finanzas.exe
SetupIconFile=assets\finanzas.ico

[Tasks]
Name: "desktopicon"; Description: "Crear acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
Source: "dist\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs; Check: Is64BitInstallMode
Source: "dist\win-x86\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs; Check: not Is64BitInstallMode

[Icons]
Name: "{group}\Finanzas"; Filename: "{app}\Finanzas.exe"
Name: "{commondesktop}\Finanzas"; Filename: "{app}\Finanzas.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Finanzas.exe"; Description: "Ejecutar Finanzas"; Flags: nowait postinstall skipifsilent
