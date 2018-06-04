# create-shortcut.ps1
# (simple version)

param(
    [Parameter(Position=0)] [string] $pathname,
    [Parameter(Position=1)] [string] $target
)
$erroractionpreference = "SilentlyContinue"
if(!$pathname -or !$target){ exit 1 }
$sh = New-Object -Com WScript.Shell
$sc = $sh.CreateShortcut("$pathname.lnk")
$sc.TargetPath = $target
$sc.Save()
if($error){ exit 1 }
exit 0


#unused for our use
<#
$sc.Arguments="-args"
$sc.WorkingDirectory = "c:\windows"
$sc.WindowStyle = 1
$sc.Hotkey = "CTRL+SHIFT+F"
$sc.IconLocation = "notepad.exe, 0"
$sc.Description = "Shortcut Description";
#>


