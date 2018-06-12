Red [ needs: 'view ]
comment [
    things to do-
                            customize       firstrun
    set timezone            set
    wallpaper               copy            set
    links                   create          create
    power settings          set
    install apps            run
    new user                create
    remove apps                             run
    start menu tiles                        set
    clear taskbar                           set
    rename pc               set
    HKLM registry           set
    HKCU registry           set             set
    weather app location                    set
]

;===========================================================================
;    user vars - put in config file instead
;===========================================================================
;optional vars
timezone: "Central Standard Time" ;quoted (Red string)
wallpaper-from: %wallpaper ;relative to script folder, or absolute
wallpaper-to: %/c/users/public/pictures ;absolute
links: [ 
    ;pathname target (windows file format)
    ;env variables to expand put inside < >
    {<userprofile>\desktop\All Apps} {shell:AppsFolder}
    ;{<userprofile>\desktop\Notepad} {notepad.exe}
]
power-ac-timeout: 0 ;current power profile (balanced) when on ac, timeout (0=off)
win32-apps-script: %Win32Apps/install.cmd ;relative to script folder, or absolute
;newuser: {Owner} ;can set new user name to create

;apps starting with > will be REMOVED, all others remain
;(this list was generated in Win10 ver 1803, newer version will probably change)
;modify as needed (no quotes needed unless name has spaces)
win10apps-remove: [
     Microsoft.BingWeather
     Microsoft.DesktopAppInstaller
     Microsoft.GetHelp
     Microsoft.Getstarted
    >Microsoft.Messaging
    >Microsoft.Microsoft3DViewer
    >Microsoft.MicrosoftOfficeHub
     Microsoft.MicrosoftSolitaireCollection
     Microsoft.MicrosoftStickyNotes
    >Microsoft.MSPaint
    >Microsoft.Office.OneNote
    >Microsoft.OneConnect
    >Microsoft.People
    >Microsoft.Print3D
    >Microsoft.SkypeApp
    >Microsoft.StorePurchaseApp
    >Microsoft.Wallet
     Microsoft.WebMediaExtensions
     Microsoft.Windows.Photos
     Microsoft.WindowsAlarms
     Microsoft.WindowsCalculator
     Microsoft.WindowsCamera
    >microsoft.windowscommunicationsapps
    >Microsoft.WindowsFeedbackHub
     Microsoft.WindowsMaps
    >Microsoft.WindowsSoundRecorder
     Microsoft.WindowsStore
    >Microsoft.Xbox.TCUI
    >Microsoft.XboxApp
    >Microsoft.XboxGameOverlay
    >Microsoft.XboxGamingOverlay
    >Microsoft.XboxIdentityProvider
    >Microsoft.XboxSpeechToTextOverlay
    >Microsoft.ZuneMusic
    >Microsoft.ZuneVideo
]

;apps to pinto start menu
;(needs quotes for any with space in name- just quote all of them)
apps-to-pin: [
    "Microsoft Edge"
    "Calculator"
    "Settings"
    "File Explorer"
    "Task Manager"
    "Google Chrome"
    "Weather"
    "Control Panel"
    "Windows Defender Security Center"
]


;===========================================================================
;    required vars - functions will assume these vars are set
;===========================================================================
reg-file: %customize.reg
sys-drive: first get-env "systemdrive" ;normally C
default-hiv: rejoin [ %/ sys-drive %/users/default/ntuser.dat ]
layout-xml: rejoin [ %/ sys-drive %/users/default/AppData/local/Microsoft/Windows/shell/DefaultLayouts.xml ]
startup-file: %firstrun.cmd ;create link in startup folder to this file
startup-files: [ %remove-apps.ps1 %weather.hiv %pintostart.ps1 %firstrun.reg ]
startup-files-to: rejoin [ %/ sys-drive %/users/default/appdata/local/temp ]
default-startup-folder: rejoin [ %/ sys-drive %"/users/default/appdata/roaming/microsoft/windows/start menu/programs/Startup" ]
;need to specify 64bit version of powershell if this program (Red) is in 32bit mode
powershell-exe: either find get-env "programfiles" "x86" [ ;is ..(x86) if in 32bit mode on 64bit system 
    rejoin [ get-env "windir" "\Sysnative\WindowsPowerShell\v1.0\powershell.exe" ] ;specify 64bit
    ][ "powershell.exe" ] ;else no need to specify

;===========================================================================
;    script vars
;===========================================================================
required-files: reduce [ reg-file ]
    append required-files startup-files
    append required-files startup-file
username: get-env "username"
pc-name: get-env "computername"
pc-serial-number: none
last-out: ""
last-err: ""
log: ""
err-count: 0

;===========================================================================
;   theme colors
;===========================================================================
theme1: context [
    log-view: context [
        bg-major: 19.24.30
        bg-minor: 44.51.57
        fg-major: 158.186.203
    ]
    error-view: context [
        bg-major: 150.0.0
        fg-major: 255.255.255
    ]
]
theme: theme1

;===========================================================================
;    change to the script dir
;===========================================================================
all [ system/script/path change-dir system/script/path ]

;===========================================================================
;   common call/wait function with logging
;===========================================================================
call-wait: func [ cmd [string!] /local ret ][
    last-out: copy ""
    last-err: copy ""
    ret: call/wait/output/error cmd last-out last-err
    append log rejoin [ "[" now "][" cmd "][exit code " ret "]" newline ]
    trim/tail last-out
    trim/tail last-err
    any [ empty? last-out  append log rejoin [ "[OUTPUT]^/" last-out "^/^/" ] ]
    any [ empty? last-err  append log rejoin [ "[ERROR!]^/" last-err "^/^/" ] ] 
    any [ zero? ret  err-count: err-count + 1 ]
    zero? ret
]

;===========================================================================
;   call/wait function for powershell with script on command line
;   need to use 64bit version for 64bit windows, or pintostart will not
;     list pin/unpin status (since Red is 32bit for the moment, it ends up
;     getting redirected to the 32bit version of powershell- use 
;     %windir%\Sysnative for %windir%\System32 which will tell Windows
;     to not redirect)
;===========================================================================
call-powershell-cmd: func [ cmd [string!] ][
    call-wait rejoin [ 
        powershell-exe " -executionpolicy bypass -command " cmd
    ]
]

;===========================================================================
;   create link (provide command line script to powershell)
;===========================================================================
create-link-ps1: {
$sc=(New-Object -Com WScript.Shell).CreateShortcut('$PATHNAME.lnk')
$sc.TargetPath='$TARGET'; $sc.Save(); exit $error.count
}
create-link: func [ pathname [string!] target [string!] /local cmd ][
    replace cmd: copy create-link-ps1 "$PATHNAME" pathname
    replace cmd "$TARGET" target
    call-powershell-cmd cmd 
]

;===========================================================================
;   unpin all apps from start menu and taskbar, pin our list if defined
;   can also call with /list to list all pinable apps
;   (those already pinned start with >)
;   #menu strings- '&Pin to Start' 'Un&pin from Start' 'Unpin from tas&kbar'
;   explorer (File Explorer) requires a link in another location to get it
;   unpinned from the taskbar  
;===========================================================================
pintostart-ps1: {
$apps_topin=@()
$sh=new-object -com Shell.Application
$allappobj = $sh.NameSpace('shell:AppsFolder').Items()
if(!$apps_topin){
  foreach($app in $allappobj){
    $nam=$app.Name; $pinned=''
    if($app.Verbs()|?{$_.Name -eq 'Un&pin from Start'}){$pinned='>'} 
    ($pinned+$nam)|write-output 
  }
  exit 0
}
#unpin everything from start menu and taskbar
$allappobj|%{$_.Verbs()}|?{$_.Name -like 'Un*pin from *ta*'}|%{$_.DoIt()}
#file explorer - needs plan B
$sh.Namespace($env:ProgramData+'\Microsoft\Windows\Start Menu Places').Items()|
  ?{$_.Path -like '*File Explorer*'}|%{$_.Verbs()}|
  ?{$_.Name -eq 'Unpin from tas&kbar'}|%{$_.DoIt()}
#pin from list
$allappobj|?{$apps_topin.contains($_.Name)}|%{$_.Verbs()}|
  ?{$_.Name -eq '&Pin to Start'}|%{$_.DoIt()} 
}

pintostart: func [ /list /local tmp ret cmd ][
    cmd: copy pintostart-ps1
    all [ not list  not 'value apps-to-pin  return false ] 
    all [
        not list
        tmp: copy "$apps_topin = @(^/"
        foreach app apps-to-pin [ append tmp rejoin [ "'" app "'^/" ] ]
        append tmp ")"
        replace cmd "$apps_topin = @()" tmp
    ]
    any [ ret: call-powershell-cmd cmd  return false ]
    any [ list  return ret ]
    all [ empty? last-out  return false ]
    split copy last-out newline
]

;===========================================================================
;    remove-apps
;===========================================================================
list-apps: has [ ret ][
    all [ 
        ret: call-powershell-cmd "get-appxpackage | %{$_.Name}"
        ret: split copy last-out newline
        empty? ret
        ret: false
    ]
    ret ;false, or block of 1 or more apps
]
remove-apps: func [ /local ret ][
    any [ ret: value? win10apps-remove  return false ]
    foreach w win10apps-remove [
        w: to-string w
        all [ 
            equal? first w #">"
            w: skip w 1
            ret: ret and call-powershell-cmd rejoin [ 
                {$ProgressPreference='SilentlyContinue'; get-appxpackage } w { | remove-appxpackage} 
            ]
        ]
    ]
    ret ;false=any failed, true=all succeeded
]

;===========================================================================
;    admin check
;===========================================================================
is-admin?: does [ call-wait {net file} ]
admin-error-view: compose [ 
    title "ERROR"
    size 400x100
    backdrop (theme/error-view/bg-major)
    style t: text 400x50 (theme/error-view/bg-major) center bold font-size 16 font-color (theme/error-view/fg-major)
    at 0x10 t "administrator rights needed"
    at 0x50 t "right click, run as administrator"
]
;unless is-admin? [ view/flags admin-error-view 'no-min ] ;TODO add quit to block

;===========================================================================
;    required files check
;===========================================================================
have-all-files?: does [
    foreach fil required-files [ 
        any [ exists? fil  return false ]
    ]
]
missing-files-view: compose [ 
    title "ERROR"
    size 400x100
    backdrop (theme/error-view/bg-major)
    style t: text 400x50 (theme/error-view/bg-major) center 
        bold font-size 16
        font-color (theme/error-view/fg-major)
    at 0x10 t "required files missing"
]
;if not have-all-files? [ view/flags missing-files-view 'no-min ] ;TODO add quit to block

;===========================================================================
;   view logs
;===========================================================================
save-log: has [ nam ][
    all [
        nam: request-file/save/file to-file rejoin [ 
            get-env "userprofile" "\desktop\customize.txt" 
        ] ;not sure what write returns if error- just use try
        return not error? try [ write nam log ] 
    ]
    false
]
log-view: does [
    view/no-wait/flags compose [ 
        size 800x600
        title "LOG VIEW"
        backdrop (theme/log-view/bg-minor)
               
        style b: button 90x20 font-size 10 bold
        b "FONT SIZE" 
        on-down [ all [ a/font/size < 36  a/font/size: a/font/size + 2  a/size: a/size ] ]
        b font-size 8 "FONT SIZE" 
        on-down [ all [ a/font/size > 10  a/font/size: a/font/size - 2  a/size: a/size ] ]
        b "SAVE" [ save-log ] 
        return
        
        at 0x40
        a: area 800x560 (theme/log-view/bg-major)
            font-name "Consolas"
            font-size 16
            font-color (theme/log-view/fg-major)
        bold on-resize [ a/size: event/window/size - 0x40 ]
       
        do [ a/text: log ]
    ] [resize no-min no-max ]
]

;===========================================================================
;   get serial number
;===========================================================================
get-serial-number: has [ tmp ][
    any [ call-wait "wmic bios get SerialNumber"  return false ]
    tmp: split trim/lines copy last-out " "
    all [ 
        equal? length? tmp 2
        equal? tmp/1 "SerialNumber"
        not empty? tmp/2
        pc-serial-number: tmp/2 
    ]
    pc-serial-number
]
get-serial-number

;===========================================================================
;    set timezone
;===========================================================================
set-timezone: does [
    any [ value? 'timezone  return false ]
    call-wait rejoin [ {tzutil /s "} timezone {"} ]
]

;===========================================================================
;   registry changes HKLM\SOFTWARE and default user
;   default user changes here have to be done before firstrun.cmd runs
;   backup default user hiv if not already done
;===========================================================================
reg-update: has [ bak wbak whiv wreg ][
    bak: rejoin [ default-hiv ".original" ]
    wbak: to-local-file bak
    whiv: to-local-file default-hiv
    wreg: to-local-file reg-file
    any [ exists? bak  call-wait rejoin [ "copy /y " whiv " " wbak ] ]
    any [ call-wait rejoin [ "reg load HKLM\1 " whiv ]  return false ]
    and~ call-wait rejoin [ "reg import " wreg ]
         call-wait "reg unload HKLM\1" ;both run regardless of first return value
]


;===========================================================================
;    wallpapers, copy any jpg in this drive/folder
;===========================================================================
copy-wallpaper: does [
    all [
        value? 'wallpaper-from
        value? 'wallpaper-to
        return call-wait rejoin [ 
            "copy /y " to-local-file wallpaper-from "\*.jpg " to-local-file wallpaper-to 
        ]
    ]
    false
]


;===========================================================================
;    rename default user DefaultLayouts.xml for minimal start menu tiles
;    will end up with settings, store and edge
;===========================================================================
rename-layouts: does [
    any [ exists? layout-xml  return true ] ;already gone
    call-wait rejoin [ "ren " to-local-file layout-xml " *.bak" ]
]

;===========================================================================
;    copy files to default user temp folder
;    create link in startup folder to firstrun.cmd
;    user customization takes place in firstrun.cmd
;===========================================================================
copy-startup-files: has [ ret ][
    ret: true
    foreach fil startup-files [
        ret: ret and call-wait rejoin [ 
            "copy /y " to-local-file fil " " to-local-file startup-files-to 
        ]
    ] 
    unless ret [ return false ]
    ;TODO create startup folder link to firstrun.cmd
    ;     which will eventually get replaced by a red app
    any [ 
        exists? default-startup-folder
        call-wait rejoin [ "mkdir " to-local-file default-startup-folder ]
        return false
    ]
    create-link rejoin [ to-local-file default-startup-folder "\firstrun" ]
        rejoin [ to-local-file startup-files-to "\" to-local-file startup-file ]
]

;===========================================================================
;    create any needed links from list
;===========================================================================
create-links: has [ ret ][
    any [ ret: value? 'links   return false ]
    foreach [ p t ] links [
        ;replace env variables in links (only first one done)
        parse p [ thru "<" copy ev to ">" (replace p rejoin [ "<" ev ">" ] get-env ev) ]
        parse t [ thru "<" copy ev to ">" (replace t rejoin [ "<" ev ">" ] get-env ev) ]
        all [ p t  ret: ret and create-link p t ]
    ]
    ret
]

;===========================================================================
;    power profile, current config should be 'balanced', just change
;    plugged in settings to always stay on
;===========================================================================
set-power-profile: does [
    any [ value? 'power-ac-timeout  return false ]
    and~ call-wait rejoin [ "powercfg /change monitor-timeout-ac " power-ac-timeout ]
         call-wait rejoin [ "powercfg /change standby-timeout-ac " power-ac-timeout ]
]


;===========================================================================
;    install win32 apps
;===========================================================================
install-apps: does [
    all [ 
        value? 'win32-apps-script
        exists? win32-apps-script
        return call-wait win32-apps-script
    ]
    false
]

;===========================================================================
;   rename pc
;===========================================================================
set-pc-name: func [ newname [string!] ][
    call-wait rejoin [ 
        {WMIC ComputerSystem where Name="} pc-name 
        {" call Rename Name="} newname {"}
    ]
]

;===========================================================================
;    if this user is 'temp' or starts with 'temp', create a password of 1
;    to prevent autologin after restart (temp user no longer needed)
;===========================================================================
set-password: func [ user [string!] pw [string!] ][
    call-wait rejoin [ "net user " user " " pw ] 
]
create-user: func [ user [string!] /admin /local ret ][
    all [
        ret: call-wait rejoin [ "net user /add " user ]
        admin
        ret: call-wait rejoin [ "net localgroup administrators /add " user ]
    ]
    ret
]
is-temp-user?: does [ equal? "temp" copy/part username 4 ]
set-users: has [ tmp ][
    all [ is-temp-user?  set-password username 1 ]
    all [ 
        not value? 'newuser
        is-temp-user?
        not empty? tmp: skip username 4 
        newuser: copy tmp
    ]
    all [
        value? newuser
        return create-user/admin newuser
    ]
    false
]
