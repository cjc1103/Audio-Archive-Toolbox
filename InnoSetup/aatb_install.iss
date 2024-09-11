; Audio Archive Toolbox
; Installation script for InnoSetup
;
; Operating system: Windows 10 (x64)
; Required components to be installed
;   .Net 8.0 (x64) or greater
;   Apple Application Support
;   Sound Exchange (sox)
; Other required components do not require installaion
;   and are in the "Executables" subdirectory
; Ver. 2024-09-11
; Compiled by Chris Cantwell

; script to modify Windows environment path
#include "environment.iss"
#define AATBName "Audio Archive Toolbox"
#define AATBVer "6.1.4"
#define AATBDistDir "e:\source\Audio Archive Toolbox\bin\x64\Release\net8.0-windows10.0.17763.0
#define AATBSetupOutputDir "e:\source"
#define DotNETDistFile = "dotnet-runtime-8.0.8-win-x64.exe"
#define AppleSupportDistFile = "AppleApplicationSupport64.msi"
#define SOXDistFile = "sox-14.4.2-win32.exe"
#define SOXProgramDir = "sox-14-4-2"

[Setup]
; Windows 10, 11 x64 architechure
; will install programs in c:\Program Files
MinVersion=10.0
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
WizardStyle=classic
AppName={#AATBName}
AppVersion={#AATBVer}
AppVerName={#AATBName} {#AATBVer}
AppComments=Utility for compressing and archiving audio wave files
DefaultDirName={autopf}\{#AATBName}
OutputDir={#AATBSetupOutputDir}
OutputBaseFilename={#AATBName} {#AATBVer} Setup
Compression=lzma2/max
ChangesEnvironment=yes
AppPublisher=Riverbend Music Productions
AppReadmeFile={app}\readme.txt
AppSupportURL=https://sourceforge.net/projects/aatb/

[Messages]
BeveledLabel=Audio Archive Toolbox

[Types]
Name: "full"; Description: "Full installation"
Name: "custom"; Description: "Custom installation"; Flags: iscustom

[Components]
; fixed base program
Name: "aatb"; Description: "Audio Archive Toolbox program and utilities"; Types: full custom; Flags: fixed
; additional support programs, selectable by end user
Name: "aatb\dotnet"; Description: "Microsoft .NET (x64) Framework"; Types: full custom
Name: "aatb\apple"; Description: "Apple Application Support"; Types: full custom
Name: "aatb\sox"; Description: "SoX Audio utility"; Types: full custom

[Files]
; copy files to app directory
Source: "{#AATBDistDir}\*.*"; DestDir: "{app}"
Source: "Utilities\*.*"; DestDir: "{app}"
; copy support programs for postinstall
Source: "Microsoft .NET\{#DotNETDistFile}"; DestDir: "{app}\Microsoft .NET"; Components: "aatb\dotnet"
Source: "Apple\{#AppleSupportDistFile}"; DestDir: "{app}\Apple"; Components: "aatb\apple"
Source: "Sound Exchange\{#SOXDistFile}"; DestDir: "{app}\Sound Exchange"; Components: "aatb\sox"

[Run]
Filename: "{app}\Microsoft .NET\{#DotNETDistFile}"; \
  Description: "Install .NET 8.0 Runtime"; \
  StatusMsg: "Installing .NET 8.0 Runtime.."; \
  Components: "aatb\dotnet"; \
  Flags: postinstall runascurrentuser waituntilterminated
Filename: "{app}\Apple\{#AppleSupportDistFile}"; \
  Description: "Install Apple Application Support"; \
  StatusMsg: "Installing Apple Application Support.."; \
  Components: "aatb\apple"; \
  Flags: postinstall runascurrentuser shellexec waituntilterminated
Filename: "{app}\Sound Exchange\{#SOXDistFile}"; \
  Description: "Install Sound Exchange"; \
  StatusMsg: "Installing Sound Exchange.."; \
  Components: "aatb\sox"; \
  Flags: postinstall runascurrentuser waituntilterminated
  
[UninstallDelete]
Type: files; Name: "{app}\Microsoft .NET\{#DotNETDistFile}"
Type: files; Name: "{app}\Apple\{#AppleSupportDistFile}"
Type: files; Name: "{app}\Sound Exchange\{#SOXDistFile}"

; The following code modifies the environment path for all users
; requires environment.iss
[Code]

procedure CurStepChanged(CurStep: TSetupStep);
begin
	if CurStep = ssPostInstall 
	then begin
		EnvAddPath('C:\Program Files\Audio Archive Toolbox');
		EnvAddPath('C:\Program Files (x86)\{#SOXProgramDir}');
	end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
	if CurUninstallStep = usPostUninstall
	then begin
		EnvRemovePath('C:\Program Files\Audio Archive Toolbox');
		EnvRemovePath('C:\Program Files (x86)\{#SOXProgramDir}');
	end;
end;
