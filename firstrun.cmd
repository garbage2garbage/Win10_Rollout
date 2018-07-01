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
set wallpaperfolder=c:\users\public\pictures
set appone=%usertemp%\appone.exe
set appxlist=%usertemp%\appxlist.txt
set pinstartlist=%usertemp%\pinstartlist.txt
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
echo setting wallpaper...
echo.
%appone% -wallpaper "%wallpaperfolder%"
echo.

rem ===========================================================================
rem     set start menu pinned apps
rem ===========================================================================
echo start menu tiles...
echo.
%appone% -unpinstart -all
%appone% -unpintaskbar -all
%appone% -pinstart "%pinstartlist%"
echo.

rem ===========================================================================
rem     remove windows apps
rem ===========================================================================
echo removing apps...
echo.
%appone% -removeappx "%appxlist%"
echo.

rem ===========================================================================
rem     weather app location
rem ===========================================================================
echo Weather app location...
echo.
%appone% -weather
echo.


echo.
color 2f
echo  will be logging out so some changes can take effect...
timeout /t 30
rem ===========================================================================
rem copy this file to temp IF was firstrun (keep temp files around if needed)
rem delete this file from startup folder
rem (goto) trick gets all following commands cached so
rem logoff command can still run when deleted
rem ===========================================================================
:cleanup
rem if run from startup folder
if %firstrun% == 1 (
    copy /y "%~f0" "%usertemp%" %silent%
    if %logout% == 1 (
        (goto) 2>nul & del "%~f0" & shutdown /l
    ) else (
        (goto) 2>nul & del "%~f0"
    )
)
rem else was run from somewhere else, do not delete any files, just logout
if %logout% == 1 shutdown /l










