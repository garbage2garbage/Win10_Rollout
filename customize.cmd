@echo off
cd /d %~dp0
color 5f
rem ===========================================================================
rem     customize.cmd
rem     run this with a new temp user right after Windows OOBE
rem ===========================================================================

rem ===========================================================================
rem     set some vars
rem ===========================================================================
set firstrunscript=firstrun.cmd
set helperfiles=remove-apps.ps1 weather.hiv pintostart.ps1 firstrun.reg
set createlink=create-link.ps1
set regfile=customize.reg
set wallpaperto=c:\users\public\pictures
set wallpaperfrom=Wallpaper
set win32appsfrom=Win32Apps
set defaultlayoutfile=c:\users\default\appdata\local\microsoft\windows\shell\DefaultLayouts.xml
set onedrivelnk=c:\users\default\appdata\roaming\microsoft\windows\start menu\programs\OneDrive.lnk
set defaultstartfolder=c:\users\default\appdata\roaming\microsoft\windows\start menu\programs\Startup
set defaulttemp=c:\users\default\appdata\local\temp
set defaultdesktop=c:\users\default\Desktop
set defaulthiv=c:\users\default\ntuser.dat
set timezone=Central Standard Time
set newname=0
set sernum=0

rem ===========================================================================
rem     test if running as admin
rem ===========================================================================
net file 1>NUL 2>&1
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
<nul set /p nothing=setting time zone to %timezone%...
tzutil /s "%timezone%" >NUL 2>&1
if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
echo.

rem ===========================================================================
rem     registry changes HKLM\SOFTWARE and default user
rem     default user changes here have to be done before firstrun.cmd runs
rem     backup default user hiv if not already done
rem ===========================================================================
if exist "%regfile%" (
    if not exist "%defaulthiv%.original" (
        <nul set /p nothing=backing up default user ntuser.dat...
        copy /y "%defaulthiv%" "%defaulthiv%.original" >NUL 2>&1
        if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
        echo.
    )
    <nul set /p nothing=HKLM and default user registry changes...
    reg load HKLM\1 "%defaulthiv%" >NUL 2>&1
    reg import "%regfile%" >NUL 2>&1
    reg unload HKLM\1 >NUL 2>&1
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     wallpapers, copy any jpg in this drive/folder
rem ===========================================================================
if exist "%wallpaperfrom%" (
    <nul set /p nothing=copying wallpapers...
    copy /y "%wallpaperfrom%\*.jpg" "%wallpaperto%" >NUL 2>&1
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     rename default user DefaultLayouts.xml for minimal start menu tiles
rem     will end up with settings, store and edge
rem ===========================================================================
if exist "%defaultlayoutfile%" (
    <nul set /p nothing=renaming DefaultLayouts.xml...
    ren "%defaultlayoutfile%" *.bak >NUL 2>&1
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem OneDrive setup in Run key already removed by customize.cmd, but link remains
rem ===========================================================================
if exist "%onedrivelnk%" (
    <nul set /p nothing=deleting OneDrive start menu link...
    del "%onedrivelnk%" >NUL 2>&1
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
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
    <nul set /p nothing=copying firstrun files to default user...
    if not exist "%defaultstartfolder%" mkdir "%defaultstartfolder%" >NUL 2>&1
    copy /y "%firstrunscript%" "%defaultstartfolder%" >NUL 2>&1
    rem copy any other needed files to temp folder
    for %%f in (%helperfiles%) do (
        copy /y %%f "%defaulttemp%" >NUL 2>&1
    )
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     create any needed dekstop links to default user desktop
rem ===========================================================================
if exist "%create-link%" (
    <nul set /p nothing=creating desktop links...
    powershell -executionpolicy bypass -file "%create-link%" "%defaultdesktop%\All Apps" shell:AppsFolder
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     power profile, current config should be 'balanced', just change
rem     plugged in settings to always stay on
rem ===========================================================================
<nul set /p nothing=setting power profile to always on when on ac...
powercfg /change monitor-timeout-ac 0 >NUL 2>&1
powercfg /change standby-timeout-ac 0 >NUL 2>&1
if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
echo.

rem ===========================================================================
rem     install win32 apps
rem ===========================================================================
if exist "%win32appsfrom%\install.cmd" (
    echo running %win32appsfrom%\install.cmd...
    echo.
    call %win32appsfrom%\install.cmd
    echo.
    color 5f
)

rem ===========================================================================
rem     set a password on temp user so does not auto logon after next reboot
rem     create the new user IF this user name starts with 'temp'
rem     (if user name starts with temp, assume this is a temp user that is
rem      only used to run this script)
rem     if user name is 'temp', ask for a new user name to create
rem     if user name is 'temp<something>', create a new user '<something>'
rem ===========================================================================
if %username:~0,4% == temp (
    <nul set /p nothing=setting %username% password to 1 to prevent auto logon at next boot
    net user %username% 1 >NUL
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
    rem if username is not temp, assume the rest is actual username wanted
    rem if is temp, ask for user name
    if %username% == temp (
        set /p newuser=" [add new user]  name: "
    ) else ( set newuser=%username:~4% )
    echo.
)
rem check if newuser var is now set
set newuser >NUL 2>&1
if %errorlevel% == 0 (
    <nul set /p nothing=adding new user %newuser%
    net user /add %newuser% >NUL
    net localgroup administrators /add %newuser% >NUL
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     check if want a new computer name
rem ===========================================================================
rem get serial number
for /f "skip=1 tokens=2 delims=," %%a in ('wmic bios get SerialNumber /format:csv') DO set sernum=%%a
rem (the above seems to end up with one space char if fails)
if %sernum% == " " ( set sernum=0 ) else ( set newname=PC-%sernum% )
echo.
echo current computer name: %computername%
echo.
if %sernum% == 0 (
    echo  [ press ENTER to keep %computername% ]
) else (
    echo  [ press ENTER for new name of %newname% ]
)
set /p newname="  new computer name: "
if not %newname% == 0 (
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
























