[Setup]
AppName=CarParkPSDC
AppVersion=1.0
AppPublisher=PSDC
DefaultDirName={localappdata}\CarParkPSDC
DefaultGroupName=CarParkPSDC
OutputBaseFilename=CarParkPSDCSetup
OutputDir=installer
Compression=lzma2
SolidCompression=yes
SetupIconFile=bin\Release\net10.0-windows10.0.19041.0\win-x64\appicon.ico
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion skipifsourcedoesntexist

[Icons]
Name: "{group}\CarParkPSDC"; Filename: "{app}\CarPark.exe"; IconFilename: "{app}\appicon.ico"; WorkingDir: "{app}"
Name: "{commondesktop}\CarParkPSDC"; Filename: "{app}\CarPark.exe"; IconFilename: "{app}\appicon.ico"; WorkingDir: "{app}"

[Run]
Filename: "{app}\CarPark.exe"; Description: "Launch CarParkPSDC"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
