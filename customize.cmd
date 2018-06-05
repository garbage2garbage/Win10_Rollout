@echo off
cd /d %~dp0
color 5f
SETLOCAL ENABLEDELAYEDEXPANSION
rem ===========================================================================
rem     customize.cmd
rem     run this with a new temp user right after Windows OOBE
rem ===========================================================================

rem ===========================================================================
rem     vars
rem ===========================================================================
rem --options--
set timezone=Central Standard Time
set newname=
set newuser=
set powerprofile_ac=0
rem --our files--
set firstrunscript=firstrun.cmd
set helperfiles=remove-apps.ps1 weather.hiv pintostart.ps1 firstrun.reg
set createlink=create-link.ps1
set regfile=customize.reg
rem --our folders--
set wallpaperfrom=Wallpaper
set win32appsfrom=Win32Apps
rem --files--
set defaultlayoutfile=c:\users\default\appdata\local\microsoft\windows\shell\DefaultLayouts.xml
set onedrivelnk=c:\users\default\appdata\roaming\microsoft\windows\start menu\programs\OneDrive.lnk
set defaulthiv=c:\users\default\ntuser.dat
rem --folders--
set wallpaperto=c:\users\public\pictures
set defaultstartfolder=c:\users\default\appdata\roaming\microsoft\windows\start menu\programs\Startup
set defaulttemp=c:\users\default\appdata\local\temp
set defaultdesktop=c:\users\default\Desktop
rem --misc--
set silent=^>NUL 2^>^&1

rem ===========================================================================
rem     test if running as admin
rem ===========================================================================
net file %silent%
if not %errorlevel% == 0 (
    color 4f
    echo  This script needs to run as administrator
    echo.
    echo  right click this script file, run as administrator
    echo.
    timeout /t 15
    goto :eof
)
echo.
echo  === Starting customize.cmd ===
echo.

rem ===========================================================================
rem     set timezone
rem ===========================================================================
if defined timezone (
    <nul set /p nothing=setting time zone to %timezone%...
    tzutil /s "%timezone%" %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     registry changes HKLM\SOFTWARE and default user
rem     default user changes here have to be done before firstrun.cmd runs
rem     backup default user hiv if not already done
rem ===========================================================================
if exist "%regfile%" (
    if not exist "%defaulthiv%.original" (
        <nul set /p nothing=backing up default user ntuser.dat...
        copy /y "%defaulthiv%" "%defaulthiv%.original" %silent%
        if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
        echo.
    )
    <nul set /p nothing=HKLM and default user registry changes...
    reg load HKLM\1 "%defaulthiv%" %silent% && reg import "%regfile%" %silent% && reg unload HKLM\1 %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     wallpapers, copy any jpg in this drive/folder
rem ===========================================================================
if exist "%wallpaperfrom%" (
    <nul set /p nothing=copying wallpapers...
    copy /y "%wallpaperfrom%\*.jpg" "%wallpaperto%" %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     rename default user DefaultLayouts.xml for minimal start menu tiles
rem     will end up with settings, store and edge
rem ===========================================================================
if exist "%defaultlayoutfile%" (
    <nul set /p nothing=renaming DefaultLayouts.xml...
    ren "%defaultlayoutfile%" *.bak %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem OneDrive setup in Run key already removed by customize.cmd, but link remains
rem ===========================================================================
if exist "%onedrivelnk%" (
    <nul set /p nothing=deleting OneDrive start menu link...
    del "%onedrivelnk%" %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     copy files to default user startup folder
rem     any new users will run firstrun.cmd from startup folder
rem
rem     user customization takes place in firstrun.cmd
rem     any other needed files will go into default user temp folder
rem ===========================================================================
if exist "%firstrunscript%" (
    set fail=0
    <nul set /p nothing=copying firstrun files to default user...
    if not exist "%defaultstartfolder%" (mkdir "%defaultstartfolder%" %silent% || set fail=1)
    copy /y "%firstrunscript%" "%defaultstartfolder%" %silent% || set fail=1
    rem copy any other needed files to temp folder
    for %%f in (%helperfiles%) do (
        copy /y %%f "%defaulttemp%" %silent% || set fail=1
    )
    if !fail! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     create any needed dekstop links to default user desktop
rem ===========================================================================
if exist "%create-link%" (
    <nul set /p nothing=creating desktop links...
    powershell -executionpolicy bypass -file "%create-link%" "%defaultdesktop%\All Apps" shell:AppsFolder
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     power profile, current config should be 'balanced', just change
rem     plugged in settings to always stay on
rem ===========================================================================
if defined powerprofile_ac (
    <nul set /p nothing=setting current ac power profile to %powerprofile_ac%...
    powercfg /change monitor-timeout-ac %powerprofile_ac% %silent% && powercfg /change standby-timeout-ac %powerprofile_ac% %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     install win32 apps
rem ===========================================================================
if exist "%win32appsfrom%\install.cmd" (
    echo.
    call %win32appsfrom%\install.cmd
    echo.
    color 5f
)

rem ===========================================================================
rem     if this user is 'temp' or starts with 'temp', create a password of 1
rem     to prevent autologin after restart (we don't need to login ever again)
rem ===========================================================================
if %username:~0,4% == temp (
    <nul set /p nothing=setting %username% password to 1 to prevent auto logon at next boot
    net user %username% 1 %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     if newuser not set (top of script) and this user is 'temp'
rem     ask to create a new user
rem ===========================================================================
if not defined newuser if %username% == temp (
    set /p newuser=" [add new user]  name: "
    echo.
)

rem ===========================================================================
rem     if newuser not set (top of script) and this user starts with 'temp'
rem     create a new user using name after 'temp' (like tempOwner -> Owner)
rem ===========================================================================
if not defined newuser if %username:~0,4% == temp (
    set newuser=%username:~4%
    echo.
)

rem ===========================================================================
rem     create new user if defined
rem ===========================================================================
if defined newuser (
    <nul set /p nothing=adding new user %newuser%
    net user /add %newuser% %silent% && net localgroup administrators /add %newuser% %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     get serial number (for a suggested new name)
rem ===========================================================================
if not defined newname (
    set sernum=" "
    rem sernum will normally be set twice, last time set is serial number
    for /f "skip=1 tokens=2 delims=," %%a in ('wmic bios get SerialNumber /format:csv') DO set sernum=%%a
    rem (the above seems to end up with one space char if fails)
    if not !sernum! == " " set newname=PC-!sernum!
)

rem ===========================================================================
rem     check if want a new computer name
rem ===========================================================================
echo  current computer name: %computername%
echo.
if defined newname (
    echo  [ press ENTER for new name of %newname% ]
) else (
    echo  [ press ENTER to keep %computername% ]
)
set /p newname="  new computer name: "
if defined newname (
    WMIC ComputerSystem where Name="%computername%" call Rename Name="%newname%" >NUL
)
echo.

rem ===========================================================================
rem     DONE
rem ===========================================================================
color 2f
echo.
echo  === reboot for changes to take effect ===
echo.
if %username:~0,4% == temp (
echo  remember to delete this temp user when logged into the new user
echo.
)
timeout /t 30
























