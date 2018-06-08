# create-link.ps1
# (simple version)

param(
    [Parameter(Position=0)] [string] $pathname,
    [Parameter(Position=1)] [string] $target
)

#no error message if fails
$erroractionpreference = "SilentlyContinue"

#check if both parameters provided
if(!$pathname -or !$target){ exit 1 }

$sh = New-Object -Com WScript.Shell
$sc = $sh.CreateShortcut("$pathname.lnk")
$sc.TargetPath = $target
$sc.Save()
if($error){ exit 1 }
exit 0


#all shortcut options
<#
Arguments 
Description 
FullName 
Hotkey 
IconLocation 
RelativePath 
TargetPath 
WindowStyle 
WorkingDirectory
#>


