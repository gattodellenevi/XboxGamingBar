; Inno Setup Script for Couch Game Bar Widget & Helper
; Dynamic Version and Wildcard Package Resolution

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#ifndef BuildConfig
  #define BuildConfig "Release"
#endif

#define MyAppName "Couch Game Bar"
#define MyAppPublisher "Gattodellenevi"
#define MyAppExeName "CouchGamingBarHelper.exe"

[Setup]
AppId={{D1A39F45-8B2E-4C3D-9A1F-8E2B4C5D6E7F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\CouchGameBar
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=CouchGameBarSetup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Helper Binaries (Release build with elevated process manifest)
Source: "Samples\XboxGamingBarHelper\bin\{#BuildConfig}\net8.0-windows10.0.22000.0\*"; DestDir: "{app}\Helper"; Flags: ignoreversion recursesubdirs createallsubdirs

; UWP Package, Security Certificate, and Dependencies (Visual Studio Publish AppPackages Output)
Source: "Samples\XboxGamingBarPackage\AppPackages\*.msix"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist
Source: "Samples\XboxGamingBarPackage\AppPackages\*.appx"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist
Source: "Samples\XboxGamingBarPackage\AppPackages\*.cer"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs skipifsourcedoesntexist
Source: "Samples\XboxGamingBarPackage\AppPackages\Dependencies\x64\*.appx"; DestDir: "{app}\Dependencies"; Flags: ignoreversion skipifsourcedoesntexist

[Run]
; 1. Install Security Certificate into TrustedPeople store (required for sideloading self-signed UWP packages)
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Get-ChildItem -Path '{app}\*.cer' -ErrorAction SilentlyContinue | ForEach-Object {{ certutil -addstore 'TrustedPeople' $_.FullName }}"""; Flags: runhidden; StatusMsg: "Installing security certificate..."

; 2. Register UWP AppX Package dynamically via PowerShell (selects the newest/latest built package in {app})
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""$pkg = Get-ChildItem -Path '{app}\*.msix','{app}\*.appx' -Exclude 'Microsoft.*' -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1 -ExpandProperty FullName; $deps = Get-ChildItem -Path '{app}\Dependencies\*.appx' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName; if ($pkg) {{ if ($deps) {{ Add-AppxPackage -Path $pkg -DependencyPath $deps -ForceApplicationShutdown }} else {{ Add-AppxPackage -Path $pkg -ForceApplicationShutdown }} }}"""; Flags: runhidden; StatusMsg: "Registering Xbox Gaming Bar Widget..."

; 3. Create Elevated Task Scheduler task to run Helper at Logon (0 UAC prompts)
Filename: "schtasks.exe"; Parameters: "/Create /TN ""CouchGamingBarHelper"" /TR """"{app}\Helper\{#MyAppExeName}"""" /SC ONLOGON /RL HIGHEST /F"; Flags: runhidden; StatusMsg: "Configuring startup background service..."

; 4. Launch Helper task immediately
Filename: "schtasks.exe"; Parameters: "/Run /TN ""CouchGamingBarHelper"""; Flags: runhidden; StatusMsg: "Starting background service..."

[UninstallRun]
; 1. Remove Task Scheduler task silently
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""CouchGamingBarHelper"" /F"; Flags: runhidden; StatusMsg: "Removing startup task..."

; 2. Unregister UWP AppX Package silently
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -Command ""Get-AppxPackage *CouchGameBar* -ErrorAction SilentlyContinue | Remove-AppxPackage"""; Flags: runhidden; StatusMsg: "Unregistering Xbox Gaming Bar Widget..."

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
