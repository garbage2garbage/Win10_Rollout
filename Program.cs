using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;


namespace ConsoleApp
{
    class Program
    {
        enum PinArg { NONE, UNPIN, UNPINTASKBAR, UNPINALL, PIN, LIST };
        public const string NL = "\r\n";
        public static string this_exe; //name of this script without path, set in main
        public const string version = "1.00";

        static void usage()
        {
            Console.WriteLine(
                NL +
                "   options:        " + this_exe + "  v" + version + "          2018@curtvm" + NL +
                NL +
                "   -list           list all available apps with current pin status" + NL +
                NL +
                "                   to save a list to a file-" + NL +
                "                   c:\\>" + this_exe + " -list > saved.txt" + NL +
                NL +
                "   -pin            appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "                     * one or more space separated app name(s)" + NL +
                "                     * app names with spaces need to be quoted" + NL +
                "                   filename  (-list format file)" + NL +
                NL +
                "   -unpin          appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "   -unpintaskbar   appname1 [ appname2 \"app name 3\" ... ]" + NL +
                NL +
                "   -unpinall       unpin all apps from the start menu and taskbar" + NL +
                "                     * placeholders/suggested apps will not be removed" + NL
                );

            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            //script (exe) name
            this_exe = Environment.GetCommandLineArgs()[0].Split('\\').Last();

            //cmd line option
            PinArg p = PinArg.NONE;

            //apps list (create using list, convert to string[] when done)
            var appslist = new List<string>();

            //all lowercase and '&' removed- comparisons will be the same
            const string unpin_str = "unpin from start";
            const string unpintb_str = "unpin from taskbar";
            const string pin_str = "pin to start";

            //get command line options
            //check first option- need valid option as first arg
            if (args.Length == 0) {
                usage();
            } else {
                switch (args[0].ToLower()) {
                    case "-unpin": p = PinArg.UNPIN; break;
                    case "-list": p = PinArg.LIST; break;
                    case "-unpinall": p = PinArg.UNPINALL; break;
                    case "-unpintaskbar": p = PinArg.UNPINTASKBAR; break;
                    case "-pin": p = PinArg.PIN; break;
                    //invalid arg
                    default: usage(); break;
                }
            }

            //if provided a -list formatted file to -pin option, use that
            if (p == PinArg.PIN && args.Length == 2 && File.Exists(args[1])) {
                foreach (string line in System.IO.File.ReadAllLines(args[1])) {
                    if (line.Length > 4 && line.StartsWith("[S")) {
                        appslist.Add(line.Substring(5).ToLower().Trim());
                    };
                }
            }
            //get command line app name options if option requires it
            else if (p == PinArg.UNPIN || p == PinArg.UNPINTASKBAR || p == PinArg.PIN) {
                foreach (string str in args.Skip(1)) {
                    appslist.Add(str.ToLower().Trim());
                }
            }

            //convert list to array
            string[] apps = appslist.ToArray();

            //check if apps list ok with option provided
            //(-unpinall and -list so not need app list)
            if (apps.Length == 0 && p != PinArg.UNPINALL && p != PinArg.LIST) {
                usage();
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
            foreach (var v in feobj.Items()) {
                if (v.path.ToString().ToLower().Contains("file explorer")) {
                    foreach (var vv in v.Verbs()) {
                        if (vv.Name.ToString() == unpintb_str) {
                            file_explorer_tbpinned = true;
                            if (apps.Contains("file explorer") && p == PinArg.UNPINTASKBAR || p == PinArg.UNPINALL) {
                                vv.DoIt();
                            }
                        }
                    }
                }
            }

            //output header for -list
            //(make sure header doesn't match list/restore format-
            // simply make first char a space, then will be ok)
            if (p == PinArg.LIST) {
                Console.WriteLine(
                NL +
                "  " + this_exe + " : apps list with status" + NL +
                "===============================================" + NL +
                "  [  ] = not pinned" + NL +
                "  [S ] = Start menu pinned" + NL +
                "  [ T] = Taskbar pinned" + NL +
                "  [ST] = Both start menu and taskbar pinned" + NL +
                "===============================================" + NL
                );
            }

            //get each app in appsfolder
            //get each menu option for app
            //if app needs the action done, do it
            //if need only list
            foreach (var item in appobj.Items()) {
                string Nam = item.Name.ToString();  //normal case as-is for console output
                string nam = Nam.ToLower();         //lowercase for compares
                bool is_in_apps = apps.Contains(nam);//check using lowercase version
                bool is_pinned = false;
                bool is_tb_pinned = false;

                foreach (var v in item.Verbs()) {
                    //remove '&', compare using lowercase
                    switch (v.Name.ToString().ToLower().Replace("&", "")) {
                        case unpin_str:
                            is_pinned = true; //for -list ouput
                            if (p == PinArg.UNPIN && is_in_apps || p == PinArg.UNPINALL) {
                                v.DoIt();
                            }
                            break;
                        case pin_str:
                            if (p == PinArg.PIN && is_in_apps) {
                                v.DoIt();
                            }
                            break;
                        case unpintb_str:
                            is_tb_pinned = true; //for -list ouput
                            if (p == PinArg.UNPINTASKBAR && is_in_apps || p == PinArg.UNPINALL) {
                                v.DoIt();
                            }
                            break;
                    }
                }

                //for file explorer- use previously checked taskbar info instead
                if (nam == "file explorer" && file_explorer_tbpinned) {
                    is_tb_pinned = true;
                }

                //-list format - [?] app name  ( ? = space, B, S, or T )
                if (p == PinArg.LIST) {
                    string s = "[  ] ";                              //not pinned
                    if (is_tb_pinned && is_pinned) { s = "[ST] "; }  //pinned start and taskbar
                    else if (is_pinned) { s = "[S ] "; }             //pinned start
                    else if (is_tb_pinned) { s = "[ T] "; }          //pinned taskbar
                    Console.WriteLine(s + Nam);
                }
            }
            if (p == PinArg.LIST) { Console.WriteLine(); }
        }
    }
}


/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace ConsoleApp1
{
    class RemApp
    {
        static void Main(string[] args)
        {

            //get apps in appsfolder namespace
            Type t = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(t);
            var appobj = shell.NameSpace("shell:AppsFolder");
            var maxl = 0;
            foreach (var item in appobj.Items())
            {
                if (item.Path.Contains("!") && (item.Name.Trim().Length > maxl)) {
                    maxl = item.Name.Trim().Length;
                }
            }
            foreach (var item in appobj.Items())
            {
                if (item.Path.Contains("!"))
                {
                    Console.WriteLine("{0,-" + maxl + "}    {1}", item.Name, item.Path.ToString().Split('_')[0]);
                }
                else {
                    Console.WriteLine(item.Name);
                }
            }



            //Console.WriteLine(WindowsIdentity.GetCurrent().Name);

                PackageManager packageManager = new Windows.Management.Deployment.PackageManager();
            IEnumerable<Package> packages =
                packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);


            foreach (Package package in packages)
            {
                Console.WriteLine(package.Id.Name);
            }

            if (args.Length == 1) {
                foreach (Package package in packages)
                {
                    if (package.Id.Name == args[0]) {
                        remove_package(package);
                    }
                }

            }

            Console.ReadKey();
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


-list
-restore
-pin
-unpin
-unpintaskbar
-unpinall

-unwinapp

-wallpaper file | folder (random)





S = Start Menu pinned
T = Taskbar pinned
W = Windows Store App

[S  ] SageTV 9
[   ] SageTV 9 Service Control
[   ] Manual
[S  ] sPlan 7.0
[   ] sPlan 7.0 - Viewer
[S W] Weather
[  W] Mixed Reality Viewer
[  W] Voice Recorder
[  W] Alarms & Clock
[S W] Calculator




public void SetSystemTimeZone(string timeZoneId)
{
    var process = Process.Start(new ProcessStartInfo
    {
        FileName = "tzutil.exe",
        Arguments = "/s \"" + timeZoneId + "\"",
        UseShellExecute = false,
        CreateNoWindow = true
    });

    if (process != null)
    {
        process.WaitForExit();
        TimeZoneInfo.ClearCachedData();
    }
}

     */

