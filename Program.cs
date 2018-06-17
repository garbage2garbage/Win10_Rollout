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
using System.Net.NetworkInformation;

namespace ConsoleApp
{
    //public class App
    //{
    //    public string Name { get; set; }
    //    public string Path { get; set; }
    //    public string FullName { get; set; }
    //    public bool is_pinned { get; set; }
    //    public bool is_tbpinned { get; set; }
    //    public bool is_appx { get; set; }

    //    public override string ToString()
    //    {
    //        return "[" + (is_pinned ? "S" : " ") + (is_tbpinned ? "T" : " ") + (is_appx ? "W" : " ") + "] ";
    //    }
    //}

    class Program
    {
        //command line options
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

        //verb strings, all lowercase and '&' removed- 
        //(comparisons will convert verb to lowercase with & removed)
        public const string unpin_str = "unpin from start";
        public const string unpintb_str = "unpin from taskbar";
        public const string pin_str = "pin to start";

        //set wallpaper dll
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        static void Usage()
        {
             Console.WriteLine(
                NL +
                "   " + this_exe + "    v" + version + "    2018@curtvm" + NL +
                NL +
                "   options:        " + NL +
                NL +
                "   -listapps" + NL + 
                "      list all available apps with current pin status" + NL +
                "      to save a list to a file-" + NL +
                "      c:\\>" + this_exe + " -listapps > saved.txt" + NL +
                NL +
                "   -pinstart filename | appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "      pin apps to start menu from a -listapps saved file or" + NL +
                "      specify app(s) with one or more space separated app name(s)" + NL +
                "      app names with spaces need to be quoted" + NL +
                "      (can use '-unpinstart all' to first clear pinned apps)" + NL +
                NL +
                "   -unpinstart all | appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "      unpin all apps or specified apps from start menu tiles" + NL +
                NL +
                "   -unpintaskbar all | appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "      unpin all apps or specified apps from taskbar" + NL +
                NL + 
                "   -removeappx appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "      remove Windows store app(s) for the current user" + NL +
                "      use -listapps provided names, either friendly or full name" + NL +
                "      (full name must include at least the first underscore _ character)" + NL +
                NL +
                "   -wallpaper filename | foldername | Bing" + NL +
                "      set wallpaper to filename" + NL +
                "      set wallpaper to random jpg picture from foldername" + NL +
                "      set wallpaper to daily image from Bing.com" + NL +
                "      (picture saved in c:\\Users\\Public\\Pictures\\Bing)" + NL +
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

        static void FileError(string filnam, string msg) {
            Console.WriteLine("{0} : {1}", filnam, msg);
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            //script (exe) name
            this_exe = Environment.GetCommandLineArgs()[0].Split('\\').Last();

            //need at least one arg
            if (args.Length == 0)
            {
                Usage();
            }

            //store first cmd line option
            ArgOpt p = ArgOpt.NONE;

            //minumum number of args required (exluding the 1 required)
            var minargs = 0;

            //apps list (will all be lowercase)
            var appslist = new List<string>();
            //args list
            var argslist = new List<string>();
            //appx list (FullName)
            var appxlist = new List<string>();
            //output list
            var outlist = new List<string>();

            //get command line options
            //check first option- need valid option as first arg
            switch (args[0].ToLower())
            {
                case "-unpinstart":
                    p = ArgOpt.UNPINSTART;
                    minargs = 1;
                    break;
                case "-listapps":
                    p = ArgOpt.LISTAPPS;
                    break;
                case "-unpintaskbar":
                    p = ArgOpt.UNPINTASKBAR;
                    minargs = 1;
                    break;
                case "-pinstart":
                    p = ArgOpt.PINSTART;
                    minargs = 1;
                    break;
                case "-removeappx":
                    p = ArgOpt.REMOVEAPPX;
                    minargs = 1;
                    break;
                case "-wallpaper":
                    p = ArgOpt.WALLPAPER;
                    minargs = 1;
                    break;
                case "-timezone":
                    p = ArgOpt.TIMEZONE;
                    minargs = 1;
                    break;
                case "-hkcuimport":
                    p = ArgOpt.HKCUIMPORT;
                    minargs = 1;
                    break;
                case "-weather":
                    p = ArgOpt.WEATHER;
                    break;
                //invalid arg
                default: Usage(); break;
            }

            //check args length with what is needed at minumum
            //we already have 1 since we are here, so adjust
            if (args.Length < (minargs + 1))
            {
                Usage();
            }

            //get all other args into string list
            foreach (string str in args.Skip(1))
            {
                argslist.Add(str);
            }


            switch (p)
            {
                case ArgOpt.WALLPAPER:
                    SetWallpaper(argslist[0]);
                    break;
                case ArgOpt.TIMEZONE:
                    SetTimezone(argslist[0]);
                    break;
                case ArgOpt.HKCUIMPORT:
                    HkcuImport(argslist[0]);
                    break;
                case ArgOpt.WEATHER:
                    SetWeather(argslist.Count() > 0 ? argslist[0] : "");
                    break;
                case ArgOpt.UNPINSTART:
                    if (argslist.Count() == 1 && argslist[0].ToLower() == "all")
                    {
                        p = ArgOpt.UNPINALLSTART;
                        argslist.Remove("all");
                    }
                    else
                    {
                        appslist = argslist.Select(x => x.ToLower()).ToList();
                    }
                    break;
                case ArgOpt.UNPINTASKBAR:
                    if (argslist.Count() == 1 && argslist[0].ToLower() == "all")
                    {
                        p = ArgOpt.UNPINALLTASKBAR;
                        argslist.Remove("all");
                    }
                    else
                    {
                        appslist = argslist.Select(x => x.ToLower()).ToList();
                    }
                    break;
                case ArgOpt.PINSTART:
                    if (argslist.Count() == 1 && File.Exists(argslist[0]))
                    {
                        foreach (string line in System.IO.File.ReadAllLines(argslist[0]))
                        {
                            if (line.Length > 6 && line.StartsWith("[S"))
                            {
                                appslist.Add(line.Substring(6).ToLower().Trim());
                            };
                        }

                    }
                    else
                    {
                        appslist = argslist.Select(x => x.ToLower()).ToList();
                    }
                    break;
                case ArgOpt.REMOVEAPPX:
                    appslist = argslist.Select(x => x.ToLower()).ToList();
                    GetAppxList(ref appxlist);
                    break;
                case ArgOpt.LISTAPPS:
                    GetAppxList(ref appxlist);
                    break;
            }

            //get apps in appsfolder namespace
            Type t = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(t);
            var appobj = shell.NameSpace("shell:AppsFolder");

            //File Explorer needs plan B as will not unpin from AppsFolder namespace
            //check it here, unpin if needed and get pinned status for -list
            bool file_explorer_tbpinned = false;
            //only need to do for -unpintaskbar and -listapps
            if (p == ArgOpt.LISTAPPS || p == ArgOpt.UNPINTASKBAR || p == ArgOpt.UNPINALLTASKBAR)
            {
                file_explorer_tbpinned = IsFileExplorerTBPinned(
                    //bool option to 'doit' if in provided list and not -listapps
                    appslist.Contains("file explorer") && p != ArgOpt.LISTAPPS
                );
            }

            //get each app in appsfolder
            //get each menu option for app
            //if app needs the action done, do it
            foreach (var item in appobj.Items())
            {
                string Nam = item.Name;             //normal case as-is for console output
                string nam = Nam.ToLower();         //lowercase for compares
                string path = item.Path;
                bool is_winapp = path.Contains("_") &&
                                 path.Contains("!"); //is win10 store app
                bool is_in_apps = appslist.Contains(nam);//check using lowercase version
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
                if (nam == "file explorer")
                {
                    is_tb_pinned = file_explorer_tbpinned;
                }

                //-list format - [   ] app name  ( S, T, W )
                //just add to list, display later
                if (p == ArgOpt.LISTAPPS)
                {
                    char[] c = { '[',' ',' ',' ',']',' '};      //not pinned, regular app
                    if (is_pinned) {    c[1] = 'S'; }           //pinned start
                    if (is_tb_pinned) { c[2] = 'T'; }           //pinned taskbar
                    if (is_winapp) {    c[3] = 'W'; }           //win10 store app
                    string s = new string(c);
                    //add to outlist                    
                    outlist.Add(s + Nam);
                    //remove full name from appxlist
                    //so we can list remaining names from appxlist
                    //after normal output (freindly name takes precedence)
                    if (is_winapp)
                    {
                        foreach (var fullnam in appxlist)
                        {
                            if (item.Path.Split('_')[0] == fullnam.Split('_')[0])
                            {
                                appxlist.Remove(fullnam);
                                break;
                            }
                        }
                    }

                }
                //remove using friendly name
                if(p == ArgOpt.REMOVEAPPX && is_winapp && appslist.Contains(nam)){
                    if (RemoveAppx(ref appxlist, item.Path))
                    {
                        //success, remove from list
                        appslist.Remove(nam);
                    } 
                    //path is like-
                    //Microsoft.BingWeather_8wekyb3d8bbwe!App
                }
            }

            if (p == ArgOpt.LISTAPPS)
            {
                ListApps(ref outlist, ref appxlist);
            }
            //try reoving the remaining appslist, which are not in appsfolder
            if (p == ArgOpt.REMOVEAPPX)
            {
                foreach(var app in appslist)
                {
                    if (RemoveAppx(ref appxlist, app))
                    {
                        //success
                        appslist.Remove(app);
                    }
                }
                Environment.Exit(appslist.Count());
            }

        } //Main

        static void ListApps(ref List<string> apps, ref List<string> appx)
        {
            Console.WriteLine(
            NL +
            "    " + this_exe + " : apps list with status" + NL +
            "  ===============================================" + NL +
            "    [   ] = not pinned, regular app" + NL +
            "    [S  ] = Start menu pinned" + NL +
            "    [ T ] = Taskbar pinned" + NL +
            "    [  W] = Windows store app" + NL +
            "  ===============================================" + NL
            );

            foreach(var app in apps)
            {
                Console.WriteLine(app);
            }
            Console.WriteLine(
            NL +
            "  ===============================================" + NL +
            "    other Windows apps not in AppsFolder" + NL +
            "  ===============================================" + NL
            );
            foreach (var app in appx)
            {
                Console.WriteLine("[  W] " + app);
            }
            Console.WriteLine();
            Environment.Exit(0);
        }

        static bool IsFileExplorerTBPinned(bool unpin) {
            //get taskbar pinned status for -list
            //unpin if needed while we are here
            //get apps in appsfolder namespace
            Type t = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(t);
            //use another namespace where File Explorer is located- start menu places
            var feobj = shell.NameSpace(
                    Environment.GetEnvironmentVariable("ProgramData") +
                    "\\Microsoft\\Windows\\Start Menu Places"
                    );
            //find file explorer, check its verbs, unpin from taskbar if needed
            //set a bool to taskbar pin status for -list output
            foreach (var v in feobj.Items())
            {
                if (v.path.ToLower().Contains("file explorer"))
                {
                    foreach (var vv in v.Verbs())
                    {
                        if (vv.Name.ToLower().Replace("&","") == unpintb_str)
                        {
                            if (unpin)
                            {
                                vv.DoIt();
                            }
                            return true;
                        }
                    }
                    return false;
                }
            }
            return false;

        }

        static bool IsAdmin() {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);
        }

        static bool IsInternetUp()
        {
            try
            {
                Ping myPing = new Ping();
                String host = "8.8.8.8";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void GetAppxList(ref List<string> appxlist)
        {
            PackageManager packageManager = new Windows.Management.Deployment.PackageManager();
            IEnumerable<Package> packages =
                packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);

            foreach (Package package in packages)
            {
                appxlist.Add(package.Id.FullName);
            }
        }

        static bool RemoveAppx(ref List<string> appxlist, string nam)
        {
            //nam is like (for path from appsfolder.item)-
            // Microsoft.Windows.SecHealthUI_cw5n1h2txyewy!SecHealthUI
            //or can be partial name (name has to include first '_')
            // Microsoft.Windows.SecHealthUI_
            //or fullname
            // Microsoft.Windows.SecHealthUI_10.0.16299.402_neutral__cw5n1h2txyewy

            // this- ? if x86 or x64 in fullname will have to provide enough name
            // to specify, else only first one found is removed
            // Microsoft.NET.Native.Framework.1.7_1.7.25531.0_x86__8wekyb3d8bbwe
            // Microsoft.NET.Native.Framework.1.7_1.7.25531.0_x64__8wekyb3d8bbwe

            //PackageFullName   
            // : Microsoft.Windows.SecHealthUI_10.0.16299.402_neutral__cw5n1h2txyewy
            //(AppsFolder) Name
            // : Windows Defender Security Center
            //(AppsFolder) Path
            //Microsoft.Windows.SecHealthUI_cw5n1h2txyewy!SecHealthUI

            //has to at least include first _ in name, 
            //to prevent 'wildcard' type match (since using StartsWith to match)
            if (!nam.Contains("_"))
            {
                return false;
            }

            //split nam by '_' to compare
            //if ! in name (an appsfolder path name)
            if (nam.Contains("!"))
            {
                nam = nam.Split('_')[0];
            }

            //find it, do it
            foreach (var fullnam in appxlist)
            {
                if(fullnam.StartsWith(nam)) {
                    //remove from list (even if fails)
                    appxlist.Remove(fullnam);
                    return remove_package(fullnam);
                }
            }
            return false; //not found
        }

        static bool remove_package(string fullnam)
        {
            Console.Write(fullnam.Split('_')[0] + "...");
            PackageManager packageManager = new Windows.Management.Deployment.PackageManager();

            IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation =
                packageManager.RemovePackageAsync(fullnam);
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
            }
            else if (deploymentOperation.Status == AsyncStatus.Canceled)
            {
                Console.WriteLine("Removal canceled");
            }
            else if (deploymentOperation.Status == AsyncStatus.Completed)
            {
                Console.WriteLine("Removal succeeded");
                return true;
            }
            else
            {
                Console.WriteLine("Removal status unknown");
            }

            return false;
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
            if (fil.ToLower() == "bing")
            {
                bingpaper(ref fil);
            }
            if (!File.Exists(fil) && !(is_dir = Directory.Exists(fil))) {
                FileError(fil, "file or directory does not exist");
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

        static void bingpaper(ref string fil)
        {
            //no need to try if no internet
            if (!IsInternetUp())
            {
                Environment.Exit(1);
            }
            //get todays bing picture store in public pictures folder
            string dst = @"c:\users\public\pictures\Bing";
            if (!Directory.Exists(dst))
            {
                Directory.CreateDirectory(dst);
            }
            if (!Directory.Exists(dst))
            {
                FileError(dst, "cannot create directory");
            }

            System.Net.WebClient wc = new System.Net.WebClient();
            string bingurl = "http://www.bing.com";
            string binghttp = wc.DownloadString(bingurl);
            string[] lines = binghttp.Replace("\"", "\n").Replace("'", "\n").Replace("\\", "").Split('\n');
            string jpgurl = "";
            foreach (var line in lines)
            {
                if (line.StartsWith("/") && line.EndsWith(".jpg"))
                {
                    jpgurl = bingurl + line;
                    break;
                }
            }
            if (jpgurl == "")
            {
                Environment.Exit(1);
            }
            string jpgname = dst + "\\" +jpgurl.Split('/').Last();
            //may have already downloaded, check
            if (!File.Exists(jpgname))
            {
                wc.DownloadFile(jpgurl, jpgname);
            }
            if (File.Exists(jpgname))
            {
                fil = jpgname;
                return;
            }
            Environment.Exit(1);
        }

        static void HkcuImport(string fil) {
            if (!File.Exists(fil)) {
                FileError(fil, "file does not exist");
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
            string wxdir = up + @"\AppData\Local\Packages\microsoft.Bingweather_8wekyb3d8bbwe";
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








System.Net.WebClient wc = new System.Net.WebClient();
string bingurl = "http://www.bing.com";
string binghttp = wc.DownloadString(bingurl);
string[] lines = = binghttp.Replace("\"","\n").Replace("'","\n").Replace("\\","").Split('\n');
string jpgurl = "";
foreach (var line in lines) {
  if (line.StartsWith("/") && line.EndsWith(".jpg")) {
    jpgurl = bingurl + line;
    break;
  }
}
if(jpgurl == ""){ return 1; }

wc.DownloadFile(jpgurl, localname);






     */
