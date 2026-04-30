; Inno Setup script for EZPos
[Setup]
AppName=EZPos
AppVersion=1.0
DefaultDirName={pf}\EZPos
DefaultGroupName=EZPos
OutputDir=.
OutputBaseFilename=SetupEZPos
Compression=lzma
SolidCompression=yes

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\EZPos"; Filename: "{app}\EZPos.exe"
Name: "{userdesktop}\EZPos"; Filename: "{app}\EZPos.exe"

[Run]
Filename: "{app}\EZPos.exe"; Description: "Jalankan EZPos"; Flags: nowait postinstall skipifsilent
