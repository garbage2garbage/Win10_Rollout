@echo off
cd /d "%~dp0"
color 5f
rem ===========================================================================
rem     firstrun.cmd
rem     runs when new user created, located in
rem     %userprofile%\appdata\roaming\microsoft\windows\start menu\programs\startup
rem     deletes itself when done (if run from startup folder)
rem ===========================================================================
set startfolder=%userprofile%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
set usertemp=%userprofile%\appdata\local\temp
set wallpaperfolder=%systemdrive%\users\public\pictures
set appone=%usertemp%\appone.exe
set appxlist=%usertemp%\removeappx.txt
set HKCU=%usertemp%\HKCU_Edge.reg
echo.
echo  ---new user setup---
echo  starting firstrun.cmd, this will not take long...
echo.
rem reset start menu first, will cleanup and use minimal apps
rem should already be minimal, but explorer seems fragile with the start menu
%appone% -resetstartmenu
rem now unpin all
%appone% -unpinstart -all
%appone% -unpintaskbar -all
%appone% -weather
%appone% -regimport "%HKCU%"
%appone% -wallpaper "%wallpaperfolder%"
echo.
%appone% -removeappx "%appxlist%"
echo.
rem do again
%appone% -resetstartmenu
%appone% -unpinstart "Microsoft Store"
%appone% -pinstart "Microsoft Edge" Calculator Settings "File Explorer" "Task Manager" "Google Chrome" Weather "Control Panel" "Windows Defender Security Center"
echo.
color 2f
rem if run from startup folder, delete firstrun.cmd
rem just leave appone.exe, appxlist.txt, HKCU_Edge.reg in users temp folder
if "%~dp0" == "%startfolder%\" (
    echo console window will close in 10 seconds...
    timeout /t 10
    (goto) 2>nul & del "%~f0"
)
