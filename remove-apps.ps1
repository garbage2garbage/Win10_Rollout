# remove-apps.ps1

#apps starting with > will be REMOVED, all others remain
#(this list was generated in Win10 ver 1803, newer version will probably change)
#modify as needed
$win10apps = @"
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
"@

#disable remove-appxpackage progress info
$ProgressPreference="SilentlyContinue"

write-host "Removing apps..."

#list is big string so we don't have to quote each app in an array
#but we then need to split up our list and trim 
foreach ($nam in $win10apps.split("`n").trim()){
    if($nam[0] -eq '>'){
        $nam = $nam.replace('>','')
        write-host -f yel $nam
        Get-AppxPackage $nam | Remove-AppxPackage -ErrorAction SilentlyContinue
    }
}

