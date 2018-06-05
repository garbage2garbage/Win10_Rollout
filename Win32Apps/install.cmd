@echo off
cd /d %~dp0
color 5f
SETLOCAL ENABLEDELAYEDEXPANSION
set silent=^>NUL 2^>^&1
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
rem     can pass 'all' to script to automatically do all installs
rem ===========================================================================
if "%1"=="all" goto :install

rem ===========================================================================
rem     else will ask for each (available), then install only requested apps
rem ===========================================================================
if defined zipnam    set /p a=" install 7Zip?         [Y,N] " && if !a! == n set zipnam=
if defined vlcnam    set /p a=" install VLC?          [Y,N] " && if !a! == n set vlcnam=
if defined chromenam set /p a=" install Chrome?       [Y,N] " && if !a! == n set chromenam=
if defined foxitnam  set /p a=" install Foxit Reader? [Y,N] " && if !a! == n set foxitnam=
if defined officenam set /p a=" install LibreOffice?  [Y,N] " && if !a! == n set officenam=

:install
rem ===========================================================================
rem     https://www.7-zip.org/download.html
rem
rem     7Zip
rem ===========================================================================
if defined zipnam (
    <nul set /p nothing=installing 7Zip...
    %zipnam%  /S
    echo DONE
)

rem ===========================================================================
rem     https://www.videolan.org/vlc/index.html
rem
rem     VLC Media PLayer
rem     can use a vlc settings file from existing user if wanted
rem     also will delete some extra unneeded links in the start menu
rem ===========================================================================
if defined vlcnam (
    <nul set /p nothing=installing VLC...
    %vlcnam% /L=1033 /S
    mkdir c:\users\default\appdata\roaming\vlc %silent%
    if exist vlc copy /y vlc\*.* c:\users\default\appdata\roaming\vlc %silent%
    del "c:\programdata\microsoft\windows\start menu\programs\videolan\documentation.lnk" %silent%
    del "c:\programdata\microsoft\windows\start menu\programs\videolan\release notes.lnk" %silent%L
    del "c:\programdata\microsoft\windows\start menu\programs\videolan\videolan website.lnk" %silent%
    echo DONE
)

rem ===========================================================================
rem     https://www.google.com/intl/en/chrome/browser/desktop/index.html?standalone=1
rem
rem     Chrome
rem     simple master_preferences file to set home page and startup page
rem ===========================================================================
if defined chromenam (
    <nul set /p nothing=installing Chrome...
    %chromenam% /silent /install
    if exist chrome\master_preferences copy /y chrome\master_preferences "c:\program files (x86)\google\chrome\application" %silent%
    echo DONE
)

rem ===========================================================================
rem     https://www.libreoffice.org/download/download/
rem
rem     LibreOffice
rem ===========================================================================
if defined officenam (
    <nul set /p nothing=installing LibreOffice...
    msiexec /qb /i %officenam% REGISTER_ALL_MSO_TYPES=1 REGISTER_DOC=1
    echo DONE
)

rem ===========================================================================
rem     https://www.foxitsoftware.com/pdf-reader/enterprise-register.php
rem
rem     Foxit Reader
rem     get msi version, then can use command line switches
rem     user registry settings handled in firstrun.cmd
rem     delete vlc start menu links not needed
rem     Edge will still be set as default pdf viewer- have to manually change
rem     (firstrun.reg user registry setting disables notify if default viewer)
rem ===========================================================================
if defined foxitnam (
    <nul set /p nothing=installing Foxit Reader...
    msiexec /qb /i %foxitnam% ADDLOCAL=FX_PDFVIEWER,FX_SE MAKEDEFAULT="1" VIEW_IN_BROWSER="1" DESKTOP_SHORTCUT="1" STARTMENU_SHORTCUT="1" AUTO_UPDATE="0" CPDF_DISABLE="1" DISABLE_UNINSTALL_SURVEY="1"
    del "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Foxit Reader\Activate Plugins.lnk" %silent%
    echo DONE
)
