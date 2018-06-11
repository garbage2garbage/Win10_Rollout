# pintostart.ps1

# apps we want pinned to start menu- change as needed
$apps_topin = @(
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

<#

1> unpin all apps from the start menu and taskbar
(with a renamed DefaultLayouts.xml, will only be edge, settings, store in start menu)

2> pin apps to start menu from list provided in this script

taskbar- can unpin, but not pin (File Explorer needs alternate method to unpin) 

this will not remove any suggested apps which normally show up for
a new user when DefaultLayouts.xml is used- there may be a way but
I do not know how (without brute force in registry to all start menu)

#>

# create com object
$sh = new-object -com Shell.Application

# get collection of all app objects
$allappobj = $sh.NameSpace('shell:AppsFolder').Items()

# to get a list of app names, can run this command after above command
#$allappobj | %{$_.Name}

# menu strings
$pin_str = "&Pin to Start"
$unpin_str = "Un&pin from Start"
$unpintb_str = "Unpin from tas&kbar"

# remove all apps from start menu and taskbar
$allappobj | %{$_.Verbs()} | ?{($_.Name -eq $unpin_str -or $_.Name -eq $unpintb_str)} | %{$_.DoIt()}

# pin apps we want to start menu
$allappobj | ?{$apps_topin.contains($_.Name)} | %{$_.Verbs()} | ?{$_.Name -eq $pin_str} | %{$_.DoIt()} 
  
# need alternate method for File Explorer taskbar
# the lnk in the following folder works ok
$sh.Namespace($env:ProgramData+'\Microsoft\Windows\Start Menu Places').Items() |
  ?{$_.Path -like '*File Explorer*'} | %{$_.Verbs()} | ?{$_.Name -eq $unpintb_str} | %{$_.DoIt()}




