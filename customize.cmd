@echo off
rem ===========================================================================
rem     customize.cmd
rem ===========================================================================
color 9f




rem ===========================================================================
rem     test if running as admin 
rem ===========================================================================
net file 1>nul 2>nul
if not %errorlevel%==0 (
    color 4f
    echo This needs to run as administrator
    echo.
    echo right click this batch file, run as administrator
    echo.
    timeout /t 15
    goto :eof
)

rem ===========================================================================
rem     change to this script drive/path
rem ===========================================================================
cd /d %~dp0

rem ===========================================================================
rem     set log file, can check if script acting right
rem ===========================================================================
set logfile=%windir%\temp\customize.log

rem ===========================================================================
rem     use call, so all output is logged with single command
rem ===========================================================================
time /t
echo.
echo Starting customize.cmd, logging to %logfile%...
call :main > %logfile% 2>&1
echo.
time /t
echo.

rem ===========================================================================
rem     set a password on temp user so does not auto logon after next reboot
rem     create the new user
rem     skip this if user does not contain 'temp' 
rem     (if script run later and logged into a needed user)
rem ===========================================================================
if %username:~0,4% == temp (
    echo setting %username% password to 1 to prevent auto logon at next boot
    net user %username% 1
    rem if username is not temp, assume the rest is actual username wanted
    rem if is temp, ask for user name
    if %username% == temp (
        set /p newuser=" [Add new user]  name: "    
    ) else ( set newuser=%username:~4% )
    echo.
)
set newuser >NUL 2>&1
if %errorlevel% == 0 (
    echo adding new user %newuser%
    net user /add %newuser%
    net localgroup administrators /add %newuser%
    echo.
)

rem ===========================================================================
rem     install win32 apps
rem ===========================================================================
if exist Win32Apps\install.cmd ( 
    echo running Win32Apps\install.cmd...
    call Win32Apps\install.cmd
)

rem ===========================================================================
rem     show log file in notepad
rem ===========================================================================
echo opening logfile in notepad...
notepad %logfile%
echo.
color 2f
echo reboot for changes to take effect
echo.
echo if you want a different computer name, you can do it now before reboot
echo.
timeout /t 10
goto :eof
rem END



rem ===========================================================================
rem ===========================================================================
rem     main script function called from above
rem ===========================================================================
rem ===========================================================================
:main
date /t
time /t
echo.
rem ===========================================================================
rem     set timezone
rem ===========================================================================
echo setting time zone to Central... >CON
echo [set timezone]
tzutil /s "Central Standard Time"
echo.

rem ===========================================================================
rem     remove Windows provisioned apps
rem     use powershell script
rem     (just leave them in, remove for each new user via firstrun.cmd)
rem ===========================================================================
rem if exist remove-apps.ps1 ( 
rem    echo removing provisioned apps... >CON
rem    echo [remove provisioned apps via powershell script]
rem    powershell -executionpolicy bypass -file remove-apps.ps1 -provisioned
rem    echo.
rem )

rem ===========================================================================
rem     registry changes HKLM\SOFTWARE
rem ===========================================================================
echo HKLM registry changes... >CON
echo [HKLM registry]
echo [advertising, suggested apps]
reg add HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\AdvertisingInfo /v Enabled /t REG_DWORD /d 0 /f
reg add HKLM\SOFTWARE\Policies\Microsoft\Windows\CloudContent /v DisableWindowsConsumerFeatures /t REG_DWORD /d 1 /f
echo.
echo [disable first logon animation, use  preparing Windows... instead]
reg add HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System /v "EnableFirstLogonAnimation" /t REG_DWORD /d 0 /f
echo.
rem echo [hide recently added apps - policy for all users]
rem reg add HKLM\SOFTWARE\Policies\Microsoft\Windows\Explorer /v HideRecentlyAddedApps /t REG_DWORD /d 1 /f
rem echo.

rem ===========================================================================
rem     registry changes for default user
rem     apply any here that will be too late for the firstrun.cmd
rem     (backup of original ntuser.dat is made so can get back to original)
rem ===========================================================================
if not exist c:\users\default\ntuser.dat.original (
    echo backing up default user ntuser.dat... >CON
    echo [backup default user registry]
    copy /y c:\users\default\ntuser.dat c:\users\default\ntuser.dat.original
    echo.
)
echo default user registry changes.. >CON
echo [modify default user registry]
reg load HKLM\1 c:\users\default\ntuser.dat
echo.
echo [prevent OneDrive from installing for new users]
reg delete HKLM\1\Software\Microsoft\Windows\CurrentVersion\Run /v OneDriveSetup /f
echo.
echo [prevent welcome page shown in edge]
reg add "HKLM\1\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v "SubscribedContent-310093Enabled" /t REG_DWORD /d 0 /f
echo.
echo [default console font size to something readable]
reg add HKLM\1\Console /v "FontSize" /t REG_DWORD /d 1572864 /f
reg unload HKLM\1
echo.

rem ===========================================================================
rem     wallpapers, copy any jpg in this drive/folder
rem ===========================================================================
if exist Wallpaper (
    echo copying wallpapers... >CON
    echo [copy jpg wallpapers]
    copy /y Wallpaper\*.jpg c:\users\public\pictures
    echo.
)

rem ===========================================================================
rem     rename default user DefaultLayouts.xml for minimal start menu tiles
rem     will end up with settings, store and edge
rem ===========================================================================
echo renaming DefaultLayouts.xml... >CON
echo [rename DefaultLayouts.xml for default user]
ren c:\users\default\appdata\local\microsoft\windows\shell\DefaultLayouts.xml DefaultLayouts.xml.bak
echo.

rem ===========================================================================
rem OneDrive setup in Run key already removed by customize.cmd, but link remains
rem ===========================================================================
echo deleting OneDrive start menu link... >CON
echo [delete OneDrive start menu link]
del "c:\users\default\appdata\roaming\microsoft\windows\start menu\programs\OneDrive.lnk"
echo.


rem ===========================================================================
rem     copy files to default user startup folder
rem     any new users will run firstrun.cmd from startup folder
rem     
rem     user customization takes place in firstrun.cmd
rem ===========================================================================
echo copying startup files to default user... >CON
echo [copy files to default user startup folder]
set sf="c:\users\default\appdata\roaming\microsoft\windows\start menu\programs\Startup"
if not exist %sf% mkdir %sf%
copy /y firstrun.cmd %sf%
rem copy any other needed files here, hide so they don't try to run
rem if already there, remove hidden attrib so can replace
for %%f in (remove-apps.ps1 weather.hiv pintostart.ps1) do (
    if exist %%f (
        if exist %sf%\%%f attrib -H %sf%\%%f
        copy /y %%f %sf%
        rem hide so it doesn't open in notepad on startup, hidden will not run
        attrib +H %sf%\%%f
    )
)
echo.

rem ===========================================================================
rem     copy any additional dekstop links to default user desktop
rem ===========================================================================
if exist Desktop (
    echo copying desktop links... >CON
    echo [copy app links to default user desktop]
    copy /y Desktop\*.lnk c:\users\default\Desktop
    echo.
)


rem ===========================================================================
rem     computer name - using serial number
rem ===========================================================================
echo [setting computer name]
rem using wmic to avoid using powershell
for /f "skip=1 tokens=2 delims=," %%a in ('wmic bios get SerialNumber /format:csv') DO set sernum=%%a
for /f "skip=1 tokens=2 delims=," %%a in ('wmic computersystem get name /format:csv') DO set oldname=%%a
rem if both are set to something, rename (the above seems to end up with one space char if fails)
if not %oldname% == " " if not %sernum% == " " (
    echo setting computer name from %oldname% to PC-%sernum%... >CON
    echo old name: %oldname%   new name: PC-%sernum%
    WMIC ComputerSystem where Name="%oldname%" call Rename Name="PC-%sernum%"
) else ( echo cannot get serial number, name not changed )
echo.

rem ===========================================================================
rem     power profile, current config should be 'balanced', just change
rem     plugged in settings to always stay on
rem ===========================================================================
echo setting power profile to always on when on ac... >CON
echo [setting current power profile - monitor and standby timeout for ac to 0 - off]
powercfg /change monitor-timeout-ac 0
powercfg /change standby-timeout-ac 0
echo.


rem done, log time
date /t
time /t
echo.
echo.


