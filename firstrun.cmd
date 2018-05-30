@echo off
rem ===========================================================================
rem     firstrun.cmd
rem
rem     runs when new user created, located in
rem     %userprofile%\appdata\roaming\microsoft\windows\start menu\programs\startup
rem     deletes itself when done
rem
rem     if script run from somewhere else, no files deleted
rem     (if want to manually run script on current user)
rem ===========================================================================
cd /d "%~dp0"
set firstrun=0
set logout=0
if "%~dp0" == "%userprofile%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\" set firstrun=1
color 9f

rem ===========================================================================
rem     give user a choice to run this
rem ===========================================================================
echo ==========================================================================
echo    new Windows user startup script
echo ==========================================================================
echo    this script will cleanup the start menu, remove unneeded apps,
echo    change various settings, set wallpaper, etc, so you can have a
echo    pleasant Windows experience :)
echo.
echo    if you would rather skip this, and use all the default settings, then
echo    press N when asked to continue
echo.
choice /M "   continue? "
rem Y=1 N=2
if %errorlevel% == 2 goto :cleanup
cls
rem since Y, set to logout automatically when done
set logout=1



rem ===========================================================================
rem     set log file, can check results
rem ===========================================================================
set logfile="%userprofile%\firstrun.log"
time /t
echo.
echo Starting firstrun.cmd, this won' take long
echo you will logged out shortly, so just log back in at logon screen
echo.
rem ===========================================================================
rem     use call, so all output is logged with single command
rem ===========================================================================
call :main >> %logfile% 2>&1
echo DONE
echo.
time /t
echo.
echo can view log results in notepad
echo.
timeout /t 5 >NUL
start "" notepad %logfile%
echo.
color 2f
echo will be logging out so some changes can take effect...
timeout /t 30
rem ===========================================================================
rem delete other files, delete this file and logout if was firstrun
rem (goto) trick gets all following commands cached so
rem logoff command can still run when deleted
rem ===========================================================================
:cleanup
rem if run from startup folder, can delete and logoout
if %firstrun% == 1 (
    for %%f in (weather.hiv remove-apps.ps1 pintostart.ps1) do (
        attrib -H %%f >NUL 2>&1
        del %%f >NUL 2>&1
    )
    if %logout%==1 (
        (goto) 2>nul & del "%~f0" & shutdown /l
    ) else (
        (goto) 2>nul & del "%~f0"
    )
)
rem else was run from somewhere else, do not delete any files, just logout
shutdown /l
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
rem     registry changes
rem ===========================================================================
echo registry changes... >CON
echo [suggested apps]
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v "SystemPaneSuggestionsEnabled" /t REG_DWORD /d 0 /f
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v "SubscribedContent-338388Enabled" /t REG_DWORD /d 0 /f
echo.
echo [show tips and tricks]
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager" /v "SubscribedContent-338389Enabled" /t REG_DWORD /d 0 /f
echo.
echo [control panel large icons, classic view]
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel" /v "AllItemsIconView" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel" /v "StartupPage" /t REG_DWORD /d 1 /f
echo.
echo [desktop icons]
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{59031a47-3f72-44a7-89c5-5595fe6b30ee}" /t REG_DWORD /d 0 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{20D04FE0-3AEA-1069-A2D8-08002B30309D}" /t REG_DWORD /d 0 /f
rem control panel
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}" /t REG_DWORD /d 1 /f
rem network
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel" /v "{018D5C66-4533-4307-9B53-224DE2ED1FE6}" /t REG_DWORD /d 1 /f
echo.
echo [desktop auto-arrange icons]
reg add "HKCU\Software\Microsoft\Windows\Shell\Bags\1\Desktop" /v "FFlags" /t REG_DWORD /d 1075839525 /f
echo.
echo [ie connection settings (disable auto check connection script)]
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections" /v "DefaultConnectionSettings" /t REG_BINARY /d 4600000003000000010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 /f
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections" /v "SavedLegacySettings" /t REG_BINARY /d 4600000004000000010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 /f
echo.
echo [ie homepage]
reg add "HKCU\Software\Microsoft\Internet Explorer\Main" /v "Start Page" /t REG_SZ /d "http://google.com/" /f
echo.
echo [ie menu,command bar,status]
reg add "HKCU\Software\Microsoft\Internet Explorer\MINIE" /v "AlwaysShowMenus" /t REG_DWORD /d 1 /f
reg add "HKCU\Software\Microsoft\Internet Explorer\MINIE" /v "CommandBarEnabled" /t REG_DWORD /d 1 /f
reg add "HKCU\Software\Microsoft\Internet Explorer\MINIE" /v "ShowStatusBar" /t REG_DWORD /d 1 /f
echo.
echo [no hide file extensions]
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "HideFileExt" /t REG_DWORD /d 0 /f
echo.
echo [taskbar no combine]
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced" /v "TaskbarGlomLevel" /t REG_DWORD /d 1 /f
echo.
echo [people icon on taskbar]
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\People" /v "PeopleBand" /t REG_DWORD /d 0 /f
echo.
echo.
echo [Disable Edge Notifications]
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Microsoft.MicrosoftEdge_8wekyb3d8bbwe!MicrosoftEdge" /v Enabled /t REG_DWORD /d 0 /f
echo.
echo [Disable Cortana Notifications]
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Microsoft.Windows.Cortana_cw5n1h2txyewy!CortanaUI" /v Enabled /t REG_DWORD /d 0 /f
echo.
echo [Disable Store Notifications]
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Microsoft.WindowsStore_8wekyb3d8bbwe!App" /v Enabled /t REG_DWORD /d 0 /f
echo.
echo [Disable Suggested Notifications]
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.Suggested" /v Enabled /t REG_DWORD /d 0 /f
echo.
echo [Disable Windows Hello Notifications]
reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.HelloFace" /v Enabled /t REG_DWORD /d 0 /f
echo.
echo [Edge home button url, new tab use blank page]
reg add "HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\Main" /v "HomeButtonPage" /t REG_SZ /d "https://www.google.com/" /f
reg add "HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\Main" /v "HomeButtonEnabled" /t REG_DWORD /d 1 /f
reg add "HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\ServiceUI" /v "NewTabPageDisplayOption" /t REG_DWORD /d 2 /f
reg add "HKCU\Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage\microsoft.microsoftedge_8wekyb3d8bbwe\MicrosoftEdge\TabbedBrowsing" /v "NTPFirstRun" /t REG_DWORD /d 1 /f
echo.
echo [minimal taskband]
reg add HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband /v "Favorites" /t REG_BINARY /d ff /f
reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband /v "FavoritesChanges" /f
reg delete HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband /v "FavoritesVersion" /f
reg add HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband\AuxilliaryPins /v "MailPin" /t REG_DWORD /d 0 /f
rem cortana hidden
reg add HKCU\Software\Microsoft\Windows\CurrentVersion\Search /v "SearchboxTaskbarMode" /t REG_DWORD /d 0 /f
echo.

rem ===========================================================================
rem     other apps registry changes
rem ===========================================================================
echo [Foxit Reader settings]
reg add "HKCU\Software\Foxit Software\Foxit Reader 9.0\Preferences\General" /v "bShowStartPage" /t REG_SZ  /d "0" /f
reg add "HKCU\Software\Foxit Software\Foxit Reader 9.0\Preferences\General" /v "bShowAdvertisement" /t REG_SZ  /d "0" /f
reg add "HKCU\Software\Foxit Software\Foxit Reader 9.0\Preferences\General" /v "bShowFloatingPromotionPage" /t REG_SZ  /d "0" /f
reg add "HKCU\Software\Foxit Software\Foxit Reader 9.0\Preferences\General" /v "bCollectData" /t REG_SZ  /d "0" /f
reg add "HKCU\Software\Foxit Software\Foxit Reader 9.0\Preferences\Registration" /v "bShowRegisterDlg" /t REG_SZ  /d "0" /f
reg add "HKCU\Software\Foxit Software\Foxit Reader 9.0\Preferences\Others" /v "bCheckRegister" /t REG_SZ  /d "0" /f
echo.

rem ===========================================================================
rem     set wallpaper
rem ===========================================================================
echo setting wallpaper... >CON
echo [set wallpaper]
rem get number of jpgs, pick a random jpg
set n=0
for %%f in (c:\users\public\pictures\*.jpg) do set /A n+=1
if not %n% == 0 (
    setlocal enabledelayedexpansion
    set /a "rand=%random% * %n% / 32768 + 1"
    set n=1
    for /f "delims=*" %%1 in ('dir /a-d /b c:\users\public\pictures\*.jpg') do (
        set jpg=%%1
        set /a n+=1
        if !n! gtr !rand! goto getout
    )
    :getout
    reg add "HKCU\Control Panel\Desktop" /v "Wallpaper" /t REG_SZ /d "c:\users\public\pictures\%jpg%" /f
    echo %jpg%
) else ( echo pictures not found )
echo.

rem ===========================================================================
rem     remove windows apps
rem ===========================================================================
if exist remove-apps.ps1 (
    echo removing Windows apps... >CON
    echo [removing apps]
    attrib -H remove-apps.ps1
    powershell -executionpolicy bypass -file remove-apps.ps1
    echo.
)

rem ===========================================================================
rem     remove windows apps
rem ===========================================================================
if exist pintostart.ps1 (
    echo setting pinned apps... >CON
    echo [pinned apps]
    attrib -H pintostart.ps1
    powershell -executionpolicy bypass -file pintostart.ps1
    echo.
)

rem ===========================================================================
rem     wetaher app location
rem ===========================================================================
if exist weather.hiv (
    echo setting Weather app location... >CON
    echo [Weather app location]
    if exist "%userprofile%"\AppData\Local\Packages\microsoft.bingweather_8wekyb3d8bbwe (
        del /s /q "%userprofile%"\AppData\Local\Packages\microsoft.bingweather_8wekyb3d8bbwe\*
    )
    for %%d in (AC AppData LocalCache LocalState RoamingState Settings SystemAppData TempState) do (
        mkdir "%userprofile%\AppData\Local\Packages\microsoft.bingweather_8wekyb3d8bbwe\%%d"
    )
    attrib -H weather.hiv
    copy /y weather.hiv "%userprofile%"\AppData\Local\Packages\microsoft.bingweather_8wekyb3d8bbwe\Settings\settings.dat
    type NUL > "%userprofile%"\AppData\Local\Packages\microsoft.bingweather_8wekyb3d8bbwe\Settings\roaming.lock
    echo.
)

rem done
echo.
time /t
echo.







