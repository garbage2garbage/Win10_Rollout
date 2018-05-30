#comment out (#) apps wanted to KEEP, all other will be removed
#(this list was generated in Win10 ver 1803, newer version will probably change)
#modify as needed
$apps = @"
            #Microsoft.BingWeather
            #Microsoft.DesktopAppInstaller
            #Microsoft.GetHelp
            #Microsoft.Getstarted
    Microsoft.Messaging
    Microsoft.Microsoft3DViewer
    Microsoft.MicrosoftOfficeHub
            #Microsoft.MicrosoftSolitaireCollection
            #Microsoft.MicrosoftStickyNotes
    Microsoft.MSPaint
    Microsoft.Office.OneNote
    Microsoft.OneConnect
    Microsoft.People
    Microsoft.Print3D
    Microsoft.SkypeApp
    Microsoft.StorePurchaseApp
    Microsoft.Wallet
            #Microsoft.WebMediaExtensions
            #Microsoft.Windows.Photos
            #Microsoft.WindowsAlarms
            #Microsoft.WindowsCalculator
            #Microsoft.WindowsCamera
    microsoft.windowscommunicationsapps
    Microsoft.WindowsFeedbackHub
            #Microsoft.WindowsMaps
    Microsoft.WindowsSoundRecorder
            #Microsoft.WindowsStore
    Microsoft.Xbox.TCUI
    Microsoft.XboxApp
    Microsoft.XboxGameOverlay
    Microsoft.XboxGamingOverlay
    Microsoft.XboxIdentityProvider
    Microsoft.XboxSpeechToTextOverlay
    Microsoft.ZuneMusic
    Microsoft.ZuneVideo
"@

write-host "Removing apps...`r`n"
foreach ($app in $apps.split("`n")){
    $app = (out-string -inputobject $app).trim()
    if($app[0] -eq "#"){ continue } #keep app
    write-host "removing $app`r`n"
    Get-AppxPackage $app | Remove-AppxPackage
}
