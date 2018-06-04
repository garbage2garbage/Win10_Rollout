#apps starting with > will be removed, all others remain
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
$ProgressPreference=’SilentlyContinue’

write-host "Removing apps..."

foreach ($app in $win10apps.split("`n")){
    $app = $app.trim()
    if($app[0] -eq '>'){
        $app = $app.replace('>','')
        write-host -f yel $app
        Get-AppxPackage $app | Remove-AppxPackage -ErrorAction SilentlyContinue
    }
}

