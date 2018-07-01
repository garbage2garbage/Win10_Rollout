@echo off
cd /d "%~dp0"
color 5f
SETLOCAL ENABLEDELAYEDEXPANSION
rem ===========================================================================
rem     firstrun.cmd
rem
rem     runs when new user created, located in
rem     %userprofile%\appdata\roaming\microsoft\windows\start menu\programs\startup
rem     deletes itself when done (if run from startup folder)
rem ===========================================================================

rem ===========================================================================
rem     set some vars
rem ===========================================================================
set startfolder=%userprofile%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
set usertemp=%userprofile%\appdata\local\temp
set wallpaperfolder=%systemdrive%\users\public\pictures
set appone=%usertemp%\appone.exe
set appxlist=%usertemp%\appxlist.txt
set pinstartlist=%usertemp%\pinstartlist.txt

cls
echo.
echo  Starting firstrun.cmd, this will not take long...
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

color 2f
rem if run from startup folder, delete firstrun.cmd
if "%~dp0" == "%startfolder%\" (
    echo DONE (console window will close in 10 seconds)
    timeout /t 10
    (goto) 2>nul & del "%~f0"
) else (
    echo DONE
    echo.
)











