[Setup]
AppId={{B1E4A5D5-BCB1-4CA5-A6C8-727BB6889344}
AppName=Gestion Magasin Cosmétiques
AppVersion=1.0.0
DefaultDirName={autopf}\GestionMagasin
DefaultGroupName=Gestion Magasin
OutputDir=..\artifacts\installer
OutputBaseFilename=GestionMagasin-Setup-x64
ArchitecturesInstallIn64BitMode=x64
ArchitecturesAllowed=x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest

[Files]
Source: "..\artifacts\publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{autoprograms}\Gestion Magasin"; Filename: "{app}\GestionMagasin.WinForms.exe"
Name: "{autodesktop}\Gestion Magasin"; Filename: "{app}\GestionMagasin.WinForms.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Créer un raccourci sur le bureau"; Flags: unchecked

[Run]
Filename: "{app}\GestionMagasin.WinForms.exe"; Description: "Lancer Gestion Magasin"; Flags: postinstall nowait skipifsilent
