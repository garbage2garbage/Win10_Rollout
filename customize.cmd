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
set newpcname=PC-[s#]
set newpcdescription=
set newuser=User1
set powerprofile_ac=0
rem --our files--
set firstrunscript=firstrun.cmd
set helperfiles=appone.exe removeappx.txt pinstart.txt HKCU_Edge.reg
set regfileHKLM=HKLM.reg
set regfileHKCU=HKCU.reg
rem --our folders--
set wallpaperfrom=Wallpaper
set win32appsfrom=Win32Apps
rem --files--
set onedrivelnk=%systemdrive%\users\default\appdata\roaming\microsoft\windows\start menu\programs\OneDrive.lnk
rem --folders--
set wallpaperto=%systemdrive%\users\public\pictures
set defaultstartfolder=%systemdrive%\users\default\appdata\roaming\microsoft\windows\start menu\programs\Startup
set defaulttemp=%systemdrive%\users\default\appdata\local\temp
set defaultdesktop=%systemdrive%\users\default\Desktop
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
rem     registry changes HKLM\SOFTWARE and HKCU for default user
rem     backup default user hiv if not already done
rem     if already a backup, restore original before making changes
rem     (any changes here will be done to original registry)
rem ===========================================================================
if exist %regfileHKCU% (
    echo HKCU default user registry changes...
    echo.
    if exist %systemdrive%\users\default\ntuser.dat.bak (
        copy /y %systemdrive%\users\default\ntuser.dat.bak %systemdrive%\users\default\ntuser.dat %silent%
    )
    appone.exe -regimport "%regfileHKCU%" -defaultuser
    echo.
)
if exist "%regfileHKLM%" (
    echo HKLM registry changes...
    appone.exe -regimport "%regfileHKLM%"
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
echo DefaultLayouts.xml...
appone.exe -layoutxml -hide
echo.

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
rem     (delete all files in these folders before copying the files)
rem ===========================================================================
if exist "%firstrunscript%" (
    set fail=0
    <nul set /p nothing=copying firstrun files to default user...
    if not exist "%defaultstartfolder%" (mkdir "%defaultstartfolder%" %silent% || set fail=1)
    del /q "%defaultstartfolder%\*.*" %silent%
    copy /y "%firstrunscript%" "%defaultstartfolder%" %silent% || set fail=1
    rem copy any other needed files to temp folder
    del /q "%defaulttemp%\*.*" %silent%
    for %%f in (%helperfiles%) do (
        copy /y %%f "%defaulttemp%" %silent% || set fail=1
    )
    if !fail! == 0 ( echo OK ) else ( echo FAILED )
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
    pushd "%~dp0"
    call %win32appsfrom%\install.cmd
    echo.
    color 5f
    popd
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
    echo adding new user %newuser%...
    appone.exe -createuser -admin "%newuser%"
    echo.
)

rem ===========================================================================
rem     new computer name
rem ===========================================================================
echo.
if defined newpcname (
    echo  current computer name: %computername%
    if defined newpcdescription (
        appone.exe -renamepc "%newpcname%" "%newpcdescription%"
    ) else (
        appone.exe -renamepc "%newpcname%"
    )
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
