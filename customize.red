;===========================================================================
;    user vars - put in config file instead
;===========================================================================
;optional vars
timezone: "Central Standard Time"
wallpaper-from: %wallpaper
wallpaper-to: %/c/users/public/pictures
links: [ ;pathname target (windows file format)
    {%userprofile%\desktop\All Apps} {shell:AppsFolder}
    ;{%userprofile%\desktop\Notepad} {notepad.exe}
]
power-ac-timeout: 0 ;current power profile (balanced) when on ac, timeout (0=off)
win32-apps-script: %Win32Apps/install.cmd
;newuser: {Owner}

;required vars - functions will assume these vars are set
reg-file: %customize.reg
create-link-script: %create-link.ps1
default-hiv: %/c/users/default/ntuser.dat
layout-xml: %/c/users/default/AppData/local/Microsoft/Windows/shell/DefaultLayouts.xml
startup-file: %firstrun.cmd ;create link in startup folder to this file
startup-files: [ %remove-apps.ps1 %weather.hiv %pintostart.ps1 %firstrun.reg ]
startup-files-to: %/c/users/default/appdata/local/temp
default-startup-folder: %"/c/users/default/appdata/roaming/microsoft/windows/start menu/programs/Startup"

;===========================================================================
;    script vars
;===========================================================================
required-files: reduce [ reg-file create-link-script ]
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
    any [ empty? last-err  append log rejoin [ "[ERROR]^/" last-err "^/^/" ] ] 
    any [ zero? ret  err-count: err-count + 1 ]
    zero? ret
]

;===========================================================================
;   common call/wait function for powershell with logging
;===========================================================================
call-powershell: func [ fil [file!] /arg args [string!] ][
    any [ arg  args: copy "" ] ;make args empty string if no arg
    call-wait rejoin [ 
        "powershell -executionpolicy bypass -file " to-local-file fil
        " " args ;will be harmless if no args
    ]
]

;===========================================================================
;   create link
;===========================================================================
create-link: func [ pathname [string!] target [string!] ][
    call-powershell/arg create-link-script rejoin [ pathname " " target ]
]

;===========================================================================
;    admin check
;===========================================================================
is-admin?: does [ call-wait {net file} ]
admin-error-view: [ 
    title "ERROR"
    size 400x100
    backdrop 150.0.0
    style t: text 400x50 150.0.0 center bold font-size 16 font-color white
    at 0x10 t "administrator rights needed"
    at 0x50 t "right click, run as administrator"
]
unless is-admin? [ view/flags admin-error-view 'no-min ] ;TODO add quit to block

;===========================================================================
;    required files check
;===========================================================================
have-all-files?: does [
    foreach fil required-files [ any [ exists? fil  return false ] ]
]
missing-files-view: [ 
    title "ERROR"
    size 400x100
    backdrop 150.0.0
    style t: text 400x50 150.0.0 center bold font-size 16 font-color white
    at 0x10 t "missing required file(s)"
]
unless have-all-files? [ view/flags missing-files-view 'no-min ] ;TODO add quit to block

;===========================================================================
;   view logs
;===========================================================================
log-view: does [
    view/no-wait/flags [ 
        title "LOG VIEW"
        backdrop 44.51.57
        
        button 90x20 font-size 12 bold "font size +" 
        on-down [ all [ a/font/size < 48  a/font/size: a/font/size + 2  a/size: a/size ] ]
        button 90x20 font-size 12 bold "font size -" 
        on-down [ all [ a/font/size > 10  a/font/size: a/font/size - 2  a/size: a/size ] ] return
        
        a: area 800x600
        19.24.30
        font-name "Consolas"
        font-size 16
        font-color 158.186.203
        bold on-resize [ a/size: event/window/size - 20x50 ]
        do [ a/text: log ]
    ] [resize no-min no-max ]
]

;===========================================================================
;   get serial number
;===========================================================================
get-serial-number: has [ tmp ][
    any [ call-wait "wmic bios get SerialNumber"  return false ]
    tmp: copy last-out
    tmp: split trim/lines tmp " "
    all [ equal? length? tmp 2  equal? tmp/1 "SerialNumber"  pc-serial-number: tmp/2 ]
    not none? pc-serial-number
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
backup-file: func [ fil [file!] newname [file!] ][
    call-wait rejoin [ "copy /y " to-local-file fil " " to-local-file newname ]
]
reg-update: does [
    any [ exists? default-hiv  backup-file default-hiv rejoin [ default-hiv ".original" ] ]
    any [ call-wait rejoin [ "reg load HKLM\1 " to-local-file default-hiv ]  return false ]
    (call-wait rejoin [ "reg import " to-local-file reg-file ]) and
        (call-wait "reg unload HKLM\1") ;both run regardless of return value
]


;===========================================================================
;    wallpapers, copy any jpg in this drive/folder
;===========================================================================
copy-wallpaper: does [
    all [
        value? 'wallpaper-from
        value? 'wallpaper-to
        return call-wait rejoin [ "copy /y " to-local-file wallpaper-from "\*.jpg " to-local-file wallpaper-to ]
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
    any [ ret  return false ]
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
;    create any needed links
;===========================================================================
create-links: has [ ret ][
    any [ ret: value? 'links  return false ]
    foreach [ p t ] links [
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
    (call-wait rejoin [ "powercfg /change monitor-timeout-ac " power-ac-timeout ]) and
    (call-wait rejoin [ "powercfg /change standby-timeout-ac " power-ac-timeout ])
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
    return false
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
        equal? "temp" username
        not empty? tmp: skip username 4 
        newuser: copy tmp
    ]
    all [
        value? newuser
        create-user/admin newuser
    ]
]
