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
set layoutxmlfolder=%LOCALAPPDATA%\Microsoft\Windows\Shell
set layoutxml=%usertemp%\DefaultLayouts.xml
echo.
echo  ---new user setup---
echo  starting firstrun.cmd, this will not take long...
echo.
%appone% -weather
%appone% -regimport "%HKCU%"
%appone% -wallpaper "%wallpaperfolder%"
echo.
%appone% -removeappx "%appxlist%"
echo.
rem Win10 1903- cannot access start menu
rem Plan B - use a layout xml file for this user to set icons
rem resetstartmenu will cause it to be loaded
copy /y %layoutxml% %layoutxmlfolder% %silent%
%appone% -norecentapps
%appone% -unpintaskbar -all
%appone% -resetstartmenu
echo.
color 2f
rem if run from startup folder, delete firstrun.cmd
rem just leave appone.exe, appxlist.txt, HKCU_Edge.reg in users temp folder
if "%~dp0" == "%startfolder%\" (
    echo console window will close in 10 seconds...
    timeout /t 10
    (goto) 2>nul & del "%~f0"
)
