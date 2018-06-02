#pintostart.ps1
#
#unpin all apps from the start menu
#pin apps to the start menu from a list provided in this script
#can also unpin apps from taskbar (not 100% working)
#
#script could be made more general with command line options
#but am only interested in cleaning up and setting the start menu
#tiles for a new user (where DefaultLayouts.xml is renamed for the
#default user so the start menu tile is already minimal)
#
#this will not remove any suggested apps which normally show up for
#a new user when DefaultLayouts.xml is used- there may be a way but
#I do not know how


#get collection of all app objects
#(from shell:AppsFolder virtual apps folder)
#(keep in global var, so new-object only needs to be run one time for this script)
$allappobj = (New-Object -Com Shell.Application).NameSpace('shell:::{4234d49b-0245-4df3-b780-3893943456e1}').Items()



#get app object by name (returns null if app not found)
function Get-AppObj {
    param( [string] $appname )
    return $allappobj | ?{$_.Name -eq $appname}
}

#taskbar- can unpin, but not pin (unpin not always working with File Explorer)
#check if app is taskbar pinned (returns true/false)
function Is-TaskbarPinned {
    param( [object] $appobj )
    return !!($appobj.Verbs() | ?{$_.Name -eq "Unpin from tas&kbar"})
}

#unpin taskbar app by name
function Unpin-TaskbarApp {
    param( [string] $appname )
    if(!($appobj = Get-AppObj($appname))){ return }
    if( Is-TaskbarPinned($appobj) ){ $appobj.InvokeVerb("taskbarunpin") }
}

#check if app is pinned (returns true/false)
function Is-Pinned {
    param( [object] $appobj )
    return !!($appobj.Verbs() | ?{$_.Name -eq "Un&pin from Start"})
}

#toggles pin (pin->unpin or unpin->pin)
#use Is-Pinned to check status before using
function Pin-Toggle {
    param( [object] $appobj )
    $appobj.InvokeVerb("pintostartscreen")
}

#pin/unpin app by name
# >Pin-App "calculator" -unpin
# >Pin-App "calculator"
function Pin-App {
    param( [string] $appname, [switch] $unpin=$false )
    if(!($appobj = Get-AppObj($appname))){ return }
    if((Is-Pinned($appobj)) -eq $unpin){ Pin-Toggle $appobj }
}

#unpin all apps (to clear out all apps before pinning wanted apps)
function Unpin-AllApps {
    foreach($appobj in $allappobj){
        if(Is-Pinned($appobj)){ Pin-Toggle $appobj }
    }
}

#pin apps from a list
function Pin-Apps {
    param( [object] $applist )
    foreach($nam in $applist){ Pin-App $nam }
}



#apps we want to pin
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



#unpin all apps
#(with a renamed DefaultLayouts.xml, will only be edge, settings, store)
#taskbar pinned apps will be done by reg setting in firstrun.reg as
#explorer remains pinned when using Unpin-TaskbarApp for some reason
Unpin-AllApps

#pin what we want
Pin-Apps $mypinlist

