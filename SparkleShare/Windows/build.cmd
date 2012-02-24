@echo off

call %~dp0\..\..\data\plugins\build.cmd

set WinDirNet=%WinDir%\Microsoft.NET\Framework
set msbuild="%WinDirNet%\v3.5\msbuild.exe"
if not exist %msbuild% set msbuild="%WinDirNet%\v4.0.30319\msbuild.exe"
set wixBinDir=%WIX%\bin

%msbuild% /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" %~dp0\SparkleShare.sln

if "%1"=="installer" (
	if exist "%wixBinDir%" (
		"%wixBinDir%\heat.exe" dir "%git_install_root%." -cg msysGitComponentGroup -gg -scom -sreg -sfrag -srd -dr MSYSGIT_DIR -t addmedia.xlst -var wix.msysgitpath -o msysgit.wxs
		"%wixBinDir%\candle" "%~dp0\SparkleShare.wxs"
		"%wixBinDir%\candle" "msysgit.wxs
		"%wixBinDir%\light" -ext WixUIExtension Sparkleshare.wixobj msysgit.wixobj -dmsysgitpath=%git_install_root% -o SparkleShare.msi
		echo SparkleShare.msi created.

	) else (
		echo Not building installer ^(could not find wix, Windows Installer XML toolset^)
	    echo wix is available at http://wix.sourceforge.net/
	)
	
) else echo Not building installer, as it was not requested. ^(Issue "build.cmd installer" to build installer ^)

if "%1"=="portable" (
	echo Building PortableApp Application Installer...
	echo Copying latest release...
	
	set portableAppTemplateDir=%~dp0\..\..\SparkleSharePortable
	set iconsDir=%~dp0\..\..\data\icons
	set portableAppInfoDir=%portableAppTemplateDir%\App\AppInfo
	set binDir=%~dp0\..\..\bin
	rem replace the following variable with the path to your portable apps installer folder
	rem	ie f:\portableapps\portableappsPortableApps.comInstaller
	rem go to http://portableapps.com/apps/development/portableapps.com_installer
	rem and http://portableapps.com
	rem for more details on the platform and the installer.
	set portableAppInstallerDir=F:\PortableApps\PortableApps.comInstaller
	
	copy "%iconsDir%\folder-sparkleshare-16.png" "%portableAppInfoDir%\appicon_16.png"
	copy "%iconsDir%\folder-sparkleshare-32.png" "%portableAppInfoDir%\appicon_32.png"
	copy "%iconsDir%\sparkleshare.ico" "%portableAppInfoDir%\appicon.ico"
	xcopy "%binDir%" "%portableAppTemplateDir%\App\SparkleShare" /E /I /Y /Q
	
	echo Invoking installer...
	echo Preparing installer...
	echo "%portableAppInstallerDir%\PortableApps.comInstaller.exe"
	"%portableAppInstallerDir%\PortableApps.comInstaller.exe" %portableAppTemplateDir%
	echo PAF installer created.
) else echo Not building portableapps installer, as it was not requested. ^(Issue "build.cmd portable" to build portableapps installer ^)
