 @echo off
cd /d %~dp0
color 9f
rem ===========================================================================
rem     can pass 'all' to script to automatically do all installs
rem     else will ask for each, then install only requested apps
rem ===========================================================================
if "%1"=="all" (
    set zip=y
    set vlc=y
    set chrome=y
    set foxit=y
    set office=Y
) else (
    set /p zip=    "install 7Zip?         [Y,N] "
    set /p vlc=    "install VLC?          [Y,N] "
    set /p chrome= "install Chrome?       [Y,N] "
    set /p foxit=  "install Foxit Reader? [Y,N] "
    set /p office= "install LibreOffice?  [Y,N] "
)

rem ===========================================================================
rem     get file names so can update versions easily- assuming the install
rem     files are named similar enough
rem ===========================================================================
for /f %%n in ('dir /b 7z*.exe') do set zipnam=%%n
for /f %%n in ('dir /b vlc*.exe') do set vlcnam=%%n
for /f %%n in ('dir /b chrome*.exe') do set chromenam=%%n
for /f %%n in ('dir /b LibreOffice*.msi') do set officenam=%%n
for /f %%n in ('dir /b FoxitReader*.msi') do set foxitnam=%%n

rem ===========================================================================
rem     7Zip
rem ===========================================================================
if %zip% == y if exist 7z*.exe (
    <nul set /p nothing=installing 7Zip...
    %zipnam%  /S
    echo DONE
)

rem ===========================================================================
rem     VLC Media PLayer
rem     can use a vlc settings file from existing user if wanted
rem     also will delete some extra unneeded links in the start menu
rem ===========================================================================
if %vlc% == y if exist vlc-*.exe (
    <nul set /p nothing=installing VLC...
    %vlcnam% /L=1033 /S
    mkdir c:\users\default\appdata\roaming\vlc >NUL 2>&1
    copy /y vlc\*.* c:\users\default\appdata\roaming\vlc >NUL
    del "c:\programdata\microsoft\windows\start menu\programs\videolan\documentation.lnk" >NUL
    del "c:\programdata\microsoft\windows\start menu\programs\videolan\release notes.lnk" >NUL
    del "c:\programdata\microsoft\windows\start menu\programs\videolan\videolan website.lnk" >NUL
    echo DONE
)

rem ===========================================================================
rem     Chrome
rem     simple master_preferences file to set home page and startup page
rem ===========================================================================
if %chrome% == y if exist Chrome*.exe (
    <nul set /p nothing=installing Chrome...
    %chromenam% /silent /install
    copy /y chrome\master_preferences "c:\program files (x86)\google\chrome\application" >NUL
    echo DONE
)

rem ===========================================================================
rem     LibreOffice
rem ===========================================================================
if %office% == y if exist LibreOffice*.msi (
    <nul set /p nothing=installing LibreOffice...
    msiexec /qb /i %officenam% REGISTER_ALL_MSO_TYPES=1 REGISTER_DOC=1
    echo DONE
)

rem ===========================================================================
rem     Foxit Reader
rem     get msi version, then can use command line switches
rem     user registry settings handled in firstrun.cmd
rem     delete unneeded extra link in start menu
rem     Edge will still be set as default pdf viewer- have to manually change
rem     (Foxit user registry setting is set to not notify if default viewer)
rem ===========================================================================
if %foxit% == y if exist FoxitReader*.msi (
    <nul set /p nothing=installing Foxit Reader...
    msiexec /qb /i %foxitnam% ADDLOCAL=FX_PDFVIEWER,FX_SE MAKEDEFAULT="1" VIEW_IN_BROWSER="1" DESKTOP_SHORTCUT="1" STARTMENU_SHORTCUT="1" AUTO_UPDATE="0" CPDF_DISABLE="1" DISABLE_UNINSTALL_SURVEY="1"
    del "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Foxit Reader\Activate Plugins.lnk" >NUL
    echo DONE
)
