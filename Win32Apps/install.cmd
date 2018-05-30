@echo off
cd /d %~dp0
color 9f
rem ===========================================================================
rem  choose which app(s) wanted
rem ===========================================================================
set /p zip=    "install 7Zip?         [Y,N] "
set /p vlc=    "install VLC?          [Y,N] "
set /p chrome= "install Chrome?       [Y,N] "
set /p foxit=  "install Foxit Reader? [Y,N] "
set /p office= "install LibreOffice?  [Y,N] "

rem ===========================================================================
rem  set app name- can update apps without the need to change this script
rem ===========================================================================
for /f %%n in ('dir /b 7z*.exe') do set zipnam=%%n
for /f %%n in ('dir /b vlc-*.exe') do set vlcnam=%%n
for /f %%n in ('dir /b chrome*.exe') do set chromenam=%%n
for /f %%n in ('dir /b LibreOffice*.msi') do set officenam=%%n
for /f %%n in ('dir /b FoxitReader*.exe') do set foxitnam=%%n

rem ===========================================================================
rem  7Zip
rem ===========================================================================
if %zip% == y if exist 7z*.exe (
    <nul set /p nothing=installing 7Zip...
    %zipnam%  /S
    echo DONE
)

rem ===========================================================================
rem  VLC Media PLayer
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
rem  Chrome (master preferences set as needed)
rem ===========================================================================
if %chrome% == y if exist Chrome*.exe (
    <nul set /p nothing=installing Chrome...
    %chromenam% /silent /install
    copy /y chrome\master_preferences "c:\program files (x86)\google\chrome\application" >NUL
    echo DONE
)

rem ===========================================================================
rem  LibreOffice
rem ===========================================================================
if %office% == y if exist LibreOffice*.msi (
    <nul set /p nothing=LibreOffice...
    msiexec /qb /i %officenam% REGISTER_ALL_MSO_TYPES=1  REGISTER_DOC=1
    echo DONE
)

rem ===========================================================================
rem  Foxit Reader (run manually, cannot find command line options)
rem               (choose options carefully- disconnect internet to skip
rem                having to choose some add-ons they want to push)
rem ===========================================================================
if %foxit% == y if exist FoxitReader*.exe (
    <nul set /p nothing=Foxit Reader manual install...
    %foxitnam%
    echo DONE
)
