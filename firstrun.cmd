@echo off
cd /d "%~dp0"
color 5f
SETLOCAL ENABLEDELAYEDEXPANSION
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
set appone=%usertemp%\appone.exe
set appxlist=%usertemp%\appxlist.txt
set pinstartlist=%usertemp%\pinstartlist.txt
set bingwxhiv=%usertemp%\weather.hiv
set bingwxfolder=%userprofile%\AppData\Local\Packages\microsoft.bingweather_8wekyb3d8bbwe
if "%~dp0" == "%startfolder%\" set firstrun=1
set silent=^>NUL 2^>^&1

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
echo  Starting firstrun.cmd, this will not take long
echo  you will be logged out shortly, so just log back in at logon screen
echo.

rem ===========================================================================
rem     set wallpaper
rem ===========================================================================
<nul set /p nothing=setting wallpaper...
%appone% -wallpaper %wallpaperfolder%
if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
echo.

rem ===========================================================================
rem     registry changes
rem ===========================================================================
if exist "%regfile%" (
    <nul set /p nothing=registry changes...
    reg import "%regfile%" %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

rem ===========================================================================
rem     set start menu pinned apps
rem ===========================================================================
<nul set /p nothing=setting start menu pinned apps...
%appone% -unpinstart -all
%appone% -unpintaskbar -all
for /f %%a in ('type %pinstartlist%') do %appone% -pinstart %%a
if %errorlevel% == 0 ( echo OK ) else ( echo FAILED )
echo.

rem ===========================================================================
rem     remove windows apps
rem ===========================================================================
<nul set /p nothing=removing apps...
for /f %%a in ('type %appxlist%') do %appone% -removeappx %%a
echo.

rem ===========================================================================
rem     weather app location
rem ===========================================================================
if exist "%bingwxhiv%" (
    <nul set /p nothing=setting Weather app location...
    if exist "%bingwxfolder%" del /s /q "%bingwxfolder%\*" %silent%
    for %%d in (AC AppData LocalCache LocalState RoamingState Settings SystemAppData TempState) do (
        mkdir "%bingwxfolder%\%%d" %silent%
    )
    copy /y "%bingwxhiv%" "%bingwxfolder%\Settings\settings.dat" %silent%
    if !errorlevel! == 0 ( echo OK ) else ( echo FAILED )
    echo.
)

echo.
color 2f
echo  will be logging out so some changes can take effect...
timeout /t 30
rem ===========================================================================
rem delete this file, helper files, and logout IF was firstrun
rem (goto) trick gets all following commands cached so
rem logoff command can still run when deleted
rem ===========================================================================
:cleanup
rem if run from startup folder, can delete files and logoout
if %firstrun% == 1 (
    del /Q "%usertemp%\*.*" %silent%
    if %logout% == 1 (
        (goto) 2>nul & del "%~f0" & shutdown /l
    ) else (
        (goto) 2>nul & del "%~f0"
    )
)
rem else was run from somewhere else, do not delete any files, just logout
if %logout% == 1 shutdown /l










