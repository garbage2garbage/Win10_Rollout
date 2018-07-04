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
set pinstartlist=%usertemp%\pinstart.txt
set HKCU=%usertemp%\HKCU_Edge.reg
echo.
echo  Starting firstrun.cmd, this will not take long...
echo.
%appone% -unpinstart -all
%appone% -unpintaskbar -all
%appone% -regimport "%HKCU%"
%appone% -wallpaper "%wallpaperfolder%"
%appone% -removeappx "%appxlist%"
%appone% -weather
%appone% -pinstart "%pinstartlist%"
echo.
color 2f
rem if run from startup folder, delete firstrun.cmd
rem just leave appone.exe, pinstartlist.txt, appxlist.txt, HKCU_Edge.reg in users temp folder
if "%~dp0" == "%startfolder%\" (
    echo console window will close in 10 seconds...
    timeout /t 10
    (goto) 2>nul & del "%~f0"
)
