@echo off
cd /d "%~dp0"
color 5f
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

rem ===========================================================================
rem     set some vars
rem ===========================================================================
set firstrun=0
set logout=0
set startfolder=%userprofile%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
set usertemp=%userprofile%\appdata\local\temp
set regfile=%usertemp%\firstrun.reg
set wallpaperfolder=c:\users\public\pictures
set removeappsfile=%usertemp%\remove-apps.ps1
set pinstartfile=%usertemp%\pintostart.ps1
set bingwxhiv=%usertemp%\weather.hiv
set bingwxfolder=%userprofile%\AppData\Local\Packages\microsoft.bingweather_8wekyb3d8bbwe
if "%~dp0" == "%startfolder%\" set firstrun=1

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
echo.
echo  Starting firstrun.cmd, this won' take long
echo  you will logged out shortly, so just log back in at logon screen
echo.

rem ===========================================================================
rem     remove windows apps
rem ===========================================================================
if exist "%removeappsfile%" (
    <nul set /p nothing=removing Windows apps...
    powershell -executionpolicy bypass -file "%removeappsfile%"
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     registry changes
rem ===========================================================================
if exist "%regfile%" (
    <nul set /p nothing=registry changes...
    reg import "%regfile%"
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     set wallpaper
rem ===========================================================================
<nul set /p nothing=setting wallpaper...
rem get number of jpgs, pick a random jpg
set n=0
for %%f in (%wallpaperfolder%\*.jpg) do set /A n+=1
if not %n% == 0 (
    setlocal enabledelayedexpansion
    set /a "rand=%random% * %n% / 32768 + 1"
    set n=1
    for /f "delims=*" %%1 in ('dir /a-d /b %wallpaperfolder%\*.jpg') do (
        set jpg=%%1
        set /a n+=1
        if !n! gtr !rand! goto getout
    )
    :getout
    reg add "HKCU\Control Panel\Desktop" /v "Wallpaper" /t REG_SZ /d "%wallpaperfolder%\%jpg%" /f
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
) else ( echo pictures not found )
echo.

rem ===========================================================================
rem     set start menu pinned apps
rem ===========================================================================
if exist "%pinstartfile%" (
    <nul set /p nothing=setting start menu pinned apps...
    powershell -executionpolicy bypass -file "%pinstartfile%"
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     wetaher app location
rem ===========================================================================
if exist "%bingwxhiv%" (
    <nul set /p nothing=setting Weather app location...
    if exist "%bingwxfolder%" del /s /q "%bingwxfolder%\*"
    for %%d in (AC AppData LocalCache LocalState RoamingState Settings SystemAppData TempState) do (
        mkdir "%bingwxfolder%\%%d" >NUL
    )
    copy /y "%bingwxhiv%" "%bingwxfolder%\Settings\settings.dat" >NUL
    type NUL > "%bingwxfolder%\Settings\roaming.lock"
    if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

echo.
color 2f
echo  will be logging out so some changes can take effect...
timeout /t 30
rem ===========================================================================
rem delete this file and logout if was firstrun
rem (goto) trick gets all following commands cached so
rem logoff command can still run when deleted
rem ===========================================================================
:cleanup
del /Q "%usertemp%\*.*" >NUL 2>&1
rem if run from startup folder, can delete and logoout
if %firstrun% == 1 (
    if %logout% == 1 (
        (goto) 2>nul & del "%~f0" & shutdown /l
    ) else (
        (goto) 2>nul & del "%~f0"
    )
)
rem else was run from somewhere else, do not delete any files, just logout
if %logout% == 1 shutdown /l










