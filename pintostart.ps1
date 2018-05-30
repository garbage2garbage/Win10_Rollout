#pintostart.ps1
#unpin all apps from the start menu
#pin apps to the start menu from a list provided in this script
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

#get app object by name
function Get-AppObj {
    param( [string]$appname )
    return $allappobj | ?{$_.Name -eq $appname}
}

#check if app is pinned (verbs will list the unpin action if pinned)
function Is-Pinned {
    param( [object]$app )
    if($app.Verbs() | ?{$_.Name -eq "Un&pin from Start"}){ return $true }
    return $false
}

#toggles pin/unpin, use Is-Pinned to check status before using
function Pin-Toggle {
    param( [object]$app )
    $app.InvokeVerb("pintostartscreen")
}

#pin app by name
function Pin-App {
    param( [string]$appname )
    if( ($a = Get-AppObj($appname)) -and (!(Is-Pinned($a))) ){ Pin-Toggle $a }
}

#unpin app by name
function Unpin-App {
    param( [string]$appname )
    if( ($a = Get-AppObj($appname)) -and (Is-Pinned($a)) ){ Pin-Toggle $a }
}

#get a list of all app names
function Get-AllAppNames {
    return $allappobj | %{$_.Name}
}

#unpin all apps
function Unpin-AllApps {
    foreach($i in Get-AllAppNames){ Unpin-App $i }
}

#pin apps from list
function Pin-Apps {
    param( [object]$applist)
    foreach($i in $applist){ Pin-App $i }
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
Unpin-AllApps

#pin what we want
pin-apps $mypinlist

