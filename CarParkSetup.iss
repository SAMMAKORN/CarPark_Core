[Setup]
AppName=CarParkPSDC
AppVersion=1.0
AppPublisher=PSDC
DefaultDirName={autopf}\CarParkPSDC
DefaultGroupName=CarParkPSDC
OutputBaseFilename=CarParkPSDCSetup
OutputDir=installer
Compression=lzma2
SolidCompression=yes
SetupIconFile=bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\appicon.ico
WizardStyle=modern

[Files]
Source: "bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\CarParkPSDC"; Filename: "{app}\CarPark.exe"; IconFilename: "{app}\appicon.ico"
Name: "{commondesktop}\CarParkPSDC"; Filename: "{app}\CarPark.exe"; IconFilename: "{app}\appicon.ico"

[Run]
Filename: "{app}\CarPark.exe"; Description: "Launch CarParkPSDC"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
