; Audio Archive Toolbox
; Installation script for InnoSetup
;
; Operating system: Windows 10 (x64)
; Required components to be installed from Support subdirectory
;   .Net 10.0 (x64) or greater
;   Apple Application Support
;   Sound Exchange (sox)
; Other required components do not require installaion
;   and are copied from the "Tools" subdirectory to the Program directory
; Ver. 2026-05-29
; Compiled by Chris Cantwell

; script to modify Windows environment path
#include "environment.iss"
#define AATBName "Audio Archive Toolbox"
#define AATBVer "6.2.4"
#define SourceDir "d:\source"
#define AATBRootDir "d:\source\Audio Archive Toolbox"
#define AATBDistDir "\bin\x64\Release\net10.0-windows10.0.22000.0"
#define ToolsSubDir "\.innosetup\Tools"
#define SupportSubDir "\.innosetup\Support"
#define DotNETDistFile = "dotnet-runtime-10.0.8-win-x64.exe"
#define AppleSupportDistFile = "AppleApplicationSupport64.msi"
#define SOXDistFile = "sox-14.4.2-win32.exe"
#define SOXProgramDir = "sox-14-4-2"

[Setup]
; Windows 10/11, x64 architechure
; will install programs under {autopf} program directory (default c:\Program Files)
AppName={#AATBName}
AppVersion={#AATBVer}
AppVerName={#AATBName} {#AATBVer}
AppComments=Utility for compressing and archiving audio wave files
MinVersion=10.0
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
WizardStyle=modern
DefaultDirName={autopf}\Audio Archive Toolbox
OutputDir={#SourceDir}
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
Name: "aatb"; Description: "Audio Archive Toolbox program"; Types: full; Flags: fixed
Name: "aatb_support"; Description: "Required support utilities and programs"; Types: full custom
; additional support programs, selectable by end user
Name: "aatb_support\dotnet"; Description: "Microsoft .NET (x64) Framework"; Types: full custom
Name: "aatb_support\apple"; Description: "Apple Application Support"; Types: full custom
Name: "aatb_support\sox"; Description: "SoX Audio utility"; Types: full custom

[Files]
; copy executable to app directory
Source: "{#AATBRootDir}{#AATBDistDir}\*.*"; DestDir: "{app}"; Flags: ignoreversion
; copy files to app directory. Overwrite existing files for reinstallation
Source: "{#AATBRootDir}{#ToolsSubDir}\*.*"; DestDir: "{app}"; Flags: ignoreversion
; copy support programs for postinstall. Do not overwrite for reinstallation
Source: "{#AATBRootDir}{#SupportSubDir}\Microsoft .NET\{#DotNETDistFile}"; DestDir: "{app}\Microsoft .NET"; Components: "aatb_support\dotnet"
Source: "{#AATBRootDir}{#SupportSubDir}\Apple\{#AppleSupportDistFile}"; DestDir: "{app}\Apple"; Components: "aatb_support\apple"
Source: "{#AATBRootDir}{#SupportSubDir}\Sound Exchange\{#SOXDistFile}"; DestDir: "{app}\Sound Exchange"; Components: "aatb_support\sox"

[Run]
Filename: "{app}\Microsoft .NET\{#DotNETDistFile}"; \
  Description: "Install .NET 10.0 Runtime"; \
  StatusMsg: "Installing .NET 10.0 Runtime.."; \
  Components: "aatb_support\dotnet"; \
  Flags: postinstall runascurrentuser waituntilterminated
Filename: "{app}\Apple\{#AppleSupportDistFile}"; \
  Description: "Install Apple Application Support"; \
  StatusMsg: "Installing Apple Application Support.."; \
  Components: "aatb_support\apple"; \
  Flags: postinstall runascurrentuser shellexec waituntilterminated
Filename: "{app}\Sound Exchange\{#SOXDistFile}"; \
  Description: "Install Sound Exchange"; \
  StatusMsg: "Installing Sound Exchange.."; \
  Components: "aatb_support\sox"; \
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
