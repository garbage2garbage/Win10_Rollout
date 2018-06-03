<#

pintostart.ps1

unpin all apps from the start menu
(with a renamed DefaultLayouts.xml, will only be edge, settings, store)
pin apps from list provided in this script

taskbar- can unpin (mostly), but not pin

taskbar pinned apps will be done by reg setting in firstrun.reg as
explorer does not want to unpin for some reason

this will not remove any suggested apps which normally show up for
a new user when DefaultLayouts.xml is used- there may be a way but
I do not know how (without brute force in regsitry to all start menu)

#>

# get collection of all app objects
# (from shell:AppsFolder virtual apps folder)
$allappobj = (New-Object -Com Shell.Application).NameSpace('shell:::{4234d49b-0245-4df3-b780-3893943456e1}').Items()

# menu strings
$pin_str = "&Pin to Start"
$unpin_str = "Un&pin from Start"
$upintbar_str = "Unpin from tas&kbar"

# apps we want to pin
$mypinlist = @(
    "Microsoft Edge"
    "Calculator"
    "Settings"
    "File Explorer"
    "Task Manager"
    "Google Chrome"
    "Weather"
    "Control Panel"
    "Windows Defender Security Center"
)

# remove all apps
foreach($appobj in $allappobj){
    if($v = $appobj.Verbs() | where Name -eq $unpin_str){ $v.DoIt() } 
}

# pin apps we want
foreach($nam in $mypinlist){ 
    if($appobj = $allappobj | where Name -eq $nam){
        if($v = $appobj.Verbs() | where Name -eq $pin_str){ $v.DoIt() } 
    }
}

