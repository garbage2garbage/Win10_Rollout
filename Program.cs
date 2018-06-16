using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

using System.Security.Principal;
using System.Threading;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Deployment;
using System.Diagnostics;

namespace ConsoleApp
{
    class Program
    {
        enum ArgOpt {
            NONE,
            UNPINSTART,
            UNPINTASKBAR,
            UNPINALLSTART,
            UNPINALLTASKBAR,
            PINSTART,
            LISTAPPS,
            REMOVEAPPX,
            WALLPAPER,
            TIMEZONE,
            HKCUIMPORT,
            WEATHER
        };
        public const string NL = "\n";
        public static string this_exe; //name of this script without path, set in main
        public const string version = "1.00";

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        static void Usage()
        {
             Console.WriteLine(
                "   " + this_exe + "    v" + version + "    2018@curtvm" + NL +
                NL +
                "   options:        " + NL +
                NL +
                "   -listapps" + NL + 
                "      list all available apps with current pin status" + NL +
                "      to save a list to a file-" + NL +
                "      c:\\>" + this_exe + " -listapps > saved.txt" + NL +
                NL +
                "   -pinstart appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "      one or more space separated app name(s)" + NL +
                "      app names with spaces need to be quoted" + NL +
                NL +
                "   -pinstart filename" + NL + 
                "      provide a -listapps saved file" + NL +
                "      will not remove any current pinned apps," + NL +
                "      so is typically preceded by command -unpinstart all" + NL +
                NL +
                "   -unpinstart appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "      unpin app(s) from start menu tiles" + NL +
                NL +
                "   -unpinstart all" + NL +
                "      unpin all apps from start menu tiles" + NL +
                NL +
                "   -unpintaskbar appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "       unpin app from taskbar" + NL +
                NL +
                "   -unpintaskbar all" + NL +
                "      unpin all apps from taskbar" + NL +
                NL + 
                "   -removeappx appname [ appname2 \"app name 3\" ... ]" + NL +
                "      remove Windows store app for current user" + NL +
                "      use friendly name for visible apps, full name for others" + NL +
                NL +
                "   -wallpaper filename" + NL +
                "      set wallpaper to filename" + NL +
                NL +
                "   -wallpaper foldername" + NL +
                "      set wallpaper to random jpg picture from foldername" + NL +
                NL +
                "   -HKCUimport filename" + NL +
                "       import a registry file for the current user" + NL +
                NL +
                "   -Weather [ settings.dat ]" + NL +
                "       set Weather app to default location (51247)" + NL +
                "       or provide a Weather settings.dat file" + NL +
                NL +
                "   -timezone \"timezonestring\"" + NL +
                "       set system timezone" + NL +
                "       c:\\" + this_exe + " -timezone \"Central Standard Time\"" + NL +
                NL +
                "   Notes" + NL +
                "      placeholders/suggested apps in start menu will not be removed" + NL +
                "      there is currently no way to pin apps to the taskbar" + NL
                );

            Environment.Exit(1);
        }

        static void FileError(string filnam) {
            Console.WriteLine("{0} : file does not exist", filnam);
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            //script (exe) name
            this_exe = Environment.GetCommandLineArgs()[0].Split('\\').Last();

            //cmd line option
            ArgOpt p = ArgOpt.NONE;

            //apps list (create using list, convert to string[] when done)
            var appslist = new List<string>();

            //all lowercase and '&' removed- comparisons will be the same
            const string unpin_str = "unpin from start";
            const string unpintb_str = "unpin from taskbar";
            const string pin_str = "pin to start";

            //get command line options
            //check first option- need valid option as first arg
            if (args.Length == 0)
            {
                Usage();
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "-unpinstart": p = ArgOpt.UNPINSTART; break;
                    case "-listapps": p = ArgOpt.LISTAPPS; break;
                    case "-unpintaskbar": p = ArgOpt.UNPINTASKBAR; break;
                    case "-pinstart": p = ArgOpt.PINSTART; break;
                    case "-removeappx": p = ArgOpt.REMOVEAPPX; break;
                    case "-wallpaper": p = ArgOpt.WALLPAPER; break;
                    case "-timezone": p = ArgOpt.TIMEZONE; break;
                    case "-hkcuimport": p = ArgOpt.HKCUIMPORT; break;
                    case "-weather": p = ArgOpt.WEATHER; break;
                    //invalid arg
                    default: Usage(); break;
                }
            }

            //check -wallpaper
            if (p == ArgOpt.WALLPAPER){
                if (args.Length < 2) { Usage(); }
                SetWallpaper(args[1]);
            }

            //check -timezone
            if (p == ArgOpt.TIMEZONE) {
                if (args.Length < 2) { Usage(); }
                SetTimezone(args[1]);
            }

            //check -HKCUimport
            if (p == ArgOpt.HKCUIMPORT) {
                if (args.Length < 2) { Usage(); }
                HkcuImport(args[1]);
            }

            //check -Weather
            if (p == ArgOpt.WEATHER)
            {
                string fil = (args.Length > 1) ? args[1] : "";
                SetWeather(fil);
            }

            //check for 'all' in second arg for -unpinstart and -unpintaskbar
            if (args.Length == 2 && args[1].ToLower() == "all")
            {
                    if (p == ArgOpt.UNPINSTART) { p = ArgOpt.UNPINALLSTART; }
                    else if (p == ArgOpt.UNPINTASKBAR) { p = ArgOpt.UNPINALLTASKBAR; }
            }
            //if provided a -list formatted file to -pin option, use that
            else if (p == ArgOpt.PINSTART && args.Length == 2 && File.Exists(args[1]))
            {
                foreach (string line in System.IO.File.ReadAllLines(args[1]))
                {
                    if (line.Length > 4 && line.StartsWith("[S"))
                    {
                        appslist.Add(line.Substring(6).ToLower().Trim());
                    };
                }
            }
            //get command line app name options if option requires it
            else if (p == ArgOpt.UNPINSTART ||
                        p == ArgOpt.UNPINTASKBAR ||
                        p == ArgOpt.PINSTART ||
                        p == ArgOpt.REMOVEAPPX)
            {
                foreach (string str in args.Skip(1))
                {
                    appslist.Add(str.ToLower().Trim());
                }
            }

            //convert list to array
            string[] apps = appslist.ToArray();

            //check if apps list ok with option provided
            if (apps.Length == 0 && 
                    p != ArgOpt.LISTAPPS &&
                    p != ArgOpt.UNPINALLTASKBAR &&
                    p != ArgOpt.UNPINALLSTART)
            {
                Usage();
            }

            //get apps in appsfolder namespace
            Type t = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(t);
            var appobj = shell.NameSpace("shell:AppsFolder");

            //File Explorer needs plan B as will not unpin from AppsFolder namespace
            //use another namespace where File Explorer is located- start menu places
            bool file_explorer_tbpinned = false;
            var feobj = shell.NameSpace(
                    Environment.GetEnvironmentVariable("ProgramData") +
                    "\\Microsoft\\Windows\\Start Menu Places"
                    );
            //find file explorer, check its verbs, unpin from taskbar if needed
            //set a bool to taskbar pin status for -list output
            foreach (var v in feobj.Items())
            {
                if (v.path.ToString().ToLower().Contains("file explorer"))
                {
                    foreach (var vv in v.Verbs())
                    {
                        if (vv.Name.ToString() == unpintb_str)
                        {
                            file_explorer_tbpinned = true;
                            if (apps.Contains("file explorer") && (p == ArgOpt.UNPINTASKBAR || p == ArgOpt.UNPINALLTASKBAR))
                            {
                                vv.DoIt();
                            }
                        }
                    }
                }
            }

            //output header for -listapps
            if (p == ArgOpt.LISTAPPS)
            {
                Console.WriteLine(
                NL +
                "  " + this_exe + " : apps list with status" + NL +
                "===============================================" + NL +
                "  [   ] = not pinned, regular app" + NL +
                "  [S  ] = Start menu pinned" + NL +
                "  [ T ] = Taskbar pinned" + NL +
                "  [  W] = Windows store app" + NL +
                "===============================================" + NL
                );
            }

            //get each app in appsfolder
            //get each menu option for app
            //if app needs the action done, do it
            foreach (var item in appobj.Items())
            {
                string Nam = item.Name.ToString();  //normal case as-is for console output
                string nam = Nam.ToLower();         //lowercase for compares
                bool is_winapp = item.Path.Contains("!"); //is win10 store app
                bool is_in_apps = apps.Contains(nam);//check using lowercase version
                bool is_pinned = false;
                bool is_tb_pinned = false;

                foreach (var v in item.Verbs())
                {
                    //remove '&', compare using lowercase
                    switch (v.Name.ToString().ToLower().Replace("&", ""))
                    {
                        case unpin_str:
                            is_pinned = true; //for -list ouput
                            if (p == ArgOpt.UNPINALLSTART || p == ArgOpt.UNPINSTART && is_in_apps)
                            {
                                v.DoIt();
                            }
                            break;
                        case pin_str:
                            if (p == ArgOpt.PINSTART && is_in_apps)
                            {
                                v.DoIt();
                            }
                            break;
                        case unpintb_str:
                            is_tb_pinned = true; //for -list ouput
                            if (p == ArgOpt.UNPINALLTASKBAR || p == ArgOpt.UNPINTASKBAR && is_in_apps)
                            {
                                v.DoIt();
                            }
                            break;
                    }
                }

                //for file explorer- use previously checked taskbar info instead
                if (nam == "file explorer" && file_explorer_tbpinned)
                {
                    is_tb_pinned = true;
                }

                //-list format - [   ] app name  ( S, T, W )
                if (p == ArgOpt.LISTAPPS)
                {
                    char[] c = { '[',' ',' ',' ',']'};          //not pinned, regular app
                    if (is_pinned) {    c[1] = 'S'; }           //pinned start
                    if (is_tb_pinned) { c[2] = 'T'; }           //pinned taskbar
                    if (is_winapp) {    c[3] = 'W'; }           //win10 store app
                    string s = new string(c);
                    Console.WriteLine(s + Nam);
                }
                if(p == ArgOpt.REMOVEAPPX && is_winapp && apps.Contains(nam)){
                    RemoveAppx(item.Path); 
                        //path is like-
                        //Microsoft.BingWeather_8wekyb3d8bbwe!App

                }
            }
            if (p == ArgOpt.LISTAPPS) { Console.WriteLine(); }


        }

        static bool IsAdmin() {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);
        }

        static int RemoveAppx(string path)
        {
            //path is like-
            //Microsoft.BingWeather_8wekyb3d8bbwe!App
            //package.Id.Name is like-
            //Microsoft.BingWeather
            //so split path by '_' to get same as package.Id.Name to compare
            //then use package.Id.FullName to uninstall

            if (!path.Contains("_")) { return 1; }
            string nam = path.Split('_')[0];

            PackageManager packageManager = new Windows.Management.Deployment.PackageManager();
            IEnumerable<Package> packages =
                packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);


            foreach (Package package in packages)
            {
                if(package.Id.Name == nam) {
                    return remove_package(package);
                }
            }

            return 1; //not found
        }

        static int remove_package(Windows.ApplicationModel.Package package)
        {
            var returnValue = 0;
            Console.Write(package.Id.Name + "...");
            PackageManager packageManager = new Windows.Management.Deployment.PackageManager();

            IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation =
                packageManager.RemovePackageAsync(package.Id.FullName);
            // This event is signaled when the operation completes
            ManualResetEvent opCompletedEvent = new ManualResetEvent(false);

            // Define the delegate using a statement lambda
            deploymentOperation.Completed = (depProgress, status) => { opCompletedEvent.Set(); };

            // Wait until the operation completes
            opCompletedEvent.WaitOne();

            // Check the status of the operation
            if (deploymentOperation.Status == AsyncStatus.Error)
            {
                DeploymentResult deploymentResult = deploymentOperation.GetResults();
                Console.WriteLine("Error code: {0}", deploymentOperation.ErrorCode);
                Console.WriteLine("Error text: {0}", deploymentResult.ErrorText);
                returnValue = 1;
            }
            else if (deploymentOperation.Status == AsyncStatus.Canceled)
            {
                Console.WriteLine("Removal canceled");
            }
            else if (deploymentOperation.Status == AsyncStatus.Completed)
            {
                Console.WriteLine("Removal succeeded");
            }
            else
            {
                returnValue = 1;
                Console.WriteLine("Removal status unknown");
            }

            return returnValue;
        }

        static void SetTimezone(string tz)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "tzutil.exe",
                Arguments = "/s \"" + tz + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) { Environment.Exit(1); }

            process.WaitForExit();
            TimeZoneInfo.ClearCachedData();
            Environment.Exit(process.ExitCode);
        }

        static void SetWallpaper(string fil) {
            bool is_dir = false;
            if (!File.Exists(fil) && !(is_dir = Directory.Exists(fil))) {
                FileError(fil);
            }
            //if dir, get random jpg in provided dir
            if (is_dir) {
                var rand = new Random();
                var files = Directory.GetFiles(fil, "*.jpg");
                fil = files[rand.Next(files.Length)];
            }
            //0=fail, non-zero=success   
            if (0 == SystemParametersInfo(20, 0, Path.GetFullPath(fil), 2)) {
                Environment.Exit(1);
            }
            Environment.Exit(0);
        }

        static void HkcuImport(string fil) {
            if (!File.Exists(fil)) {
                FileError(fil);
            }
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = "/import " + fil,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null) { Environment.Exit(1); }

            process.WaitForExit();
            Environment.Exit(process.ExitCode);
        }

        static void SetWeather(string fil) {
            string up = Environment.GetEnvironmentVariable("userprofile");
            if(up == null) { Environment.Exit(1); }
            //weather app folder
            string wxdir = up + @"\AppData\Local\Packages\microsoft.bingweather_8wekyb3d8bbwe";
            //folders to create
            string[] fol = { "AC", "AppData", "LocalCache", "LocalState", "RoamingState", "Settings", "SystemAppData", "TempState" };
            //delete everything
            try { Directory.Delete(wxdir, recursive: true); }
            catch (IOException) {
                //try again- AC dir seems to sometimes take 2 tries
                try { Directory.Delete(wxdir, recursive: true); }
                //ignore if happens again, just try to copy file
                catch { }
            }
            //create all folders
            foreach (string d in fol)
            {
                Directory.CreateDirectory(wxdir + @"\" + d);
            }
            //if filename not provided, use resource data instead
            if (fil == "")
            {
                System.IO.File.WriteAllBytes(wxdir + @"\Settings\settings.dat", AppZero.Properties.Resources.settings_dat);
            }
            else {
                File.Copy(fil, wxdir + @"\Settings\settings.dat");
            }
            Environment.Exit(0); //for now, should check above results
        }
    }
}








/*
 
     shell.NameSpace("shell:AppsFolder").Items()
    Path: Microsoft.BingWeather_8wekyb3d8bbwe!App
    Name: Weather                             
    
    
Windows.ApplicationModel.Package
    Id.Name: Microsoft.BingWeather
    Id.FamilyName: Microsoft.BingWeather_8wekyb3d8bbwe
    
    
shell.items() .Path.Split('_')[0] = package.Id.Name 















     */
