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
using iwr = IWshRuntimeLibrary; //using alias to prevent File name clash
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace ConsoleApp
{
    public delegate void optdo(ref List<string> argslist);

    public class Options
    {
        //option name and function
        public Options(string nam, optdo h)
        {
            Name = nam;
            Handler = h;
        }
        public string Name;
        public optdo Handler;
    }

    public class Apps
    {
        public Apps() { }
        public string AppxName { get; set; }
        public string Name { get; set; }        //display name from shell:AppsFolder
        public string Path { get; set; }        //path from shell:AppsFolder
        public dynamic Verbs { get; set; }      //verbs() pin/unpin/unpintb menu items
        const string unpintb_str = "Unpin from tas&kbar";
        const string unpin_str = "Un&pin from Start";
        const string pin_str = "&Pin to Start";
        public void ListPrint()
        {
            Console.WriteLine( String.Format("  [{0}{1}{2}] {3}{4}",
                    is_pinned() ? "S" : " ",
                    is_tbpinned() ? "T" : " ",
                    is_appx() ? "W" : " ",
                    Name,
                    is_appx() ? " <" + AppxName + ">" : "")
                    );
        }
        public bool is_appx()
        {
            return AppxName != null;
        }
        public bool is_tbpinned()
        {
            if (Verbs == null) return false;
            foreach (var v in Verbs)
            {
                if (v.Name == unpintb_str) return true;
            }
            return false;
        }
        public bool is_pinned()
        {
            if (Verbs == null) return false;
            foreach (var v in Verbs)
            {
                if (v.Name == unpin_str) return true;
            }
            return false;
        }
        public bool unpin()
        {
            if (!is_pinned()) return false;
            foreach (var v in Verbs)
            {
                if (v.Name == unpin_str)
                {
                    v.DoIt();
                    return true;
                }
            }
            return false;
        }
        public bool unpintb()
        {
            if (!is_tbpinned()) return false;
            foreach (var v in Verbs)
            {
                if (v.Name == unpintb_str)
                {
                    v.DoIt();
                    return true;
                }
            }
            return false;
        }
        public bool pin()
        {
            if (is_pinned()) return false;
            foreach (var v in Verbs)
            {
                if (v.Name == pin_str)
                {
                    v.DoIt();
                    return true;
                }
            }
            return false;
        }
    }

    public class Appx
    {
        public Appx() { }
        public string PackageFullName { get; set; }
        public string Name { get; set; }
        public string InstalledLocation { get; set; }
        public bool is_system()
        { 
            return InstalledLocation.ToLower().StartsWith(Program.windir.ToLower());
        }
        public void ListPrint()
        {
            Console.WriteLine("  " + PackageFullName);
        }
    }

    class Program
    {
        //global vars
        public const string version = "1803.00"; //tested with win10 1803
        public const string NL = "\n";
        public static string exe_name;      //name of this script without path, set in main
        public static string sysdrive;      //normally C:
        public static string windir;        //normally C:\WINDOWS
        public static string bingfolder;    //where to store bing wallpaper
        public static List<Options> optionslist; //each option, handler function 
        public static string userprofile;   //userprofile env variable

        //set wallpaper dll
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);


        static void Main(string[] args)
        {
            //script (exe) name
            exe_name = Environment.GetCommandLineArgs()[0].Split('\\').Last();
            //in case os installed on something other than C: (slim chance)
            sysdrive = Environment.GetEnvironmentVariable("systemdrive");
            if (sysdrive == null) sysdrive = "C:"; //just in case
            //in case os installed on something other than C: (slim chance)
            windir = Environment.GetEnvironmentVariable("windir");
            if(windir == null) windir = @"C:\Windows";
            //bing picture folder -> public pictures \Bing
            bingfolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures);
            if (bingfolder == null)
            {
                bingfolder = $@"{sysdrive}\users\public\pictures\Bing";
            }
            else
            {
                bingfolder = $@"{bingfolder}\Bing";
            }
            //current user's profile folder
            //I assume it will always get something, functions that use can
            //check if null
            userprofile = Environment.GetEnvironmentVariable("userprofile");

            //args list (cmd line as-is)
            var argslist = new List<string>(args);

            if (argslist.Count() == 0)
            {
                Help(ref argslist);
            }

            //options list- name, function name
            optionslist = new List<Options>();
            optionslist.Add(new Options("-unpinstart", UnpinStart));
            optionslist.Add(new Options("-listapps", ListApps));
            optionslist.Add(new Options("-unpintaskbar", UnpinTaskbar));
            optionslist.Add(new Options("-pinstart", PinStart));
            optionslist.Add(new Options("-removeappx", RemoveAppx));
            optionslist.Add(new Options("-wallpaper", Wallpaper));
            optionslist.Add(new Options("-shortcut", Shortcut));
            optionslist.Add(new Options("-createuser", CreateUser));
            optionslist.Add(new Options("-renamepc", RenamePC));

            //check cmdline option against our optionslist
            var opt = optionslist.Find(x => x.Name == argslist[0].ToLower());

            //if cannot find any, show help
            if (opt == null)
            {
                argslist.Clear();
                Help(ref argslist);
            }

            //check if specific help, like ->  -listapps -help
            if (argslist.Count() > 1 && argslist[1].ToLower() == "-help")
            {
                Help(ref argslist); //argslist[0] contains specific help option
            }

            //now call function
            opt.Handler(ref argslist);

            //should not get here, so exit 1
            Exit(1);

        }


        //help, error, exit functions

        static void Exit(int exitcode)
        {
            Environment.Exit(exitcode);
        }

        static void Help(ref List<string> argslist)
        {
            //not pretty (:
            string specific_str = "";
            bool specific = false;
            if (argslist.Count() > 0)
            {
                specific = true;
                specific_str = argslist[0];
            }
            Console.WriteLine();
            Console.WriteLine($@"   {exe_name}   v{version}    2018@curtvm");
            Console.WriteLine();
            if (!specific)
            {
                Console.WriteLine($@"   for specific help, use -optionname -help");
                Console.WriteLine();
                Console.WriteLine($@"   options:");
                Console.WriteLine();
            }
            if (!specific || specific_str == "-listapps")
            {
                Console.WriteLine($@"   -listapps");
            }
            if (specific_str == "-listapps")
            {
                Console.WriteLine();
                Console.WriteLine($@"    list all available apps with status");
                Console.WriteLine($@"    and list installed Windows store apps");
                Console.WriteLine();
            }
            if (!specific || specific_str == "-pinstart")
            {
                Console.WriteLine($@"   -pinstart filename | appname1 [ appname2 ""app name 3"" ... ]");
            }
            if (specific_str == "-pinstart")
            {
                Console.WriteLine(AppOne.Properties.Resources.pinstart_help);
            }
            if (!specific || specific_str == "-unpinstart")
            {
                Console.WriteLine($@"   -unpinstart -all | appname1 [ appname2 ""app name 3"" ... ]");
            }
            if (specific_str == "-unpinstart")
            {
                Console.WriteLine();
                Console.WriteLine($@"    unpin all apps or specified apps from start menu tiles");
                Console.WriteLine($@"    (placeholders/suggested apps in start menu will not be removed)");
                Console.WriteLine();
            }
            if (!specific || specific_str == "-unpintaskbar")
            {
                Console.WriteLine($@"   -unpintaskbar -all | appname1 [ appname2 ""app name 3"" ... ]");
            }
            if (specific_str == "-unpintaskbar")
            {
                Console.WriteLine();
                Console.WriteLine($@"    unpin all apps or specified apps from taskbar");
                Console.WriteLine();
            }
            if (!specific || specific_str == "-removeappx")
            {
                Console.WriteLine($@"   -removeappx filename | appname1 [ appname2 ""app name 3"" ... ]");
            }
            if (specific_str == "-removeappx")
            {
                Console.WriteLine(AppOne.Properties.Resources.removeappx_help);
            }
            if (!specific || specific_str == "-wallpaper")
            {
                Console.WriteLine($@"   -wallpaper filename | foldername | Bing.com");
            }
            if (specific_str == "-wallpaper")
            {
                Console.WriteLine(AppOne.Properties.Resources.wallpaper_help.Replace("{bingfolder}", bingfolder));
            }
            if (!specific || specific_str == "-shortcut")
            {
                Console.WriteLine($@"   -shortcut name target [ -arg arguments ][ -wd workingdir ]");
            }
            if (specific_str == "-shortcut")
            {
                Console.WriteLine(AppOne.Properties.Resources.shortcut_help.Replace("{exe_name}", exe_name));
            }
            if (!specific || specific_str == "-createuser")
            {
                Console.WriteLine($@"   -createuser [ -admin ] username [ password ]");
            }
            if (specific_str == "-createuser")
            {
                Console.WriteLine(AppOne.Properties.Resources.createuser_help);
            }
            if (!specific || specific_str == "-renamepc")
            {
                Console.WriteLine($@"   -renamepc newname [ description ] ([s#] [date] [rand])");
            }
            if (specific_str == "-renamepc")
            {
                Console.WriteLine(AppOne.Properties.Resources.renamepc_help);
            }
            if (!specific)
            {
                Console.WriteLine();
            }
            Exit(1);
        }

        static void Error(string filnam, string msg)
        {
            Console.WriteLine();
            Console.WriteLine("   {0} : {1}", filnam, msg);
            Console.WriteLine();
            Exit(1);
        }

        static void Error(string msg)
        {
            Console.WriteLine();
            Console.WriteLine("   " + msg);
            Console.WriteLine();
            Exit(1);
        }

        static void ErrorAdmin()
        {
            Error("need to run this command as Administrator");
        }


        //option functions

        static void ListApps(ref List<string> argslist)
        {
            //-listapps
            var myapps = new List<Apps>();
            getAppsList(ref myapps);
            Console.WriteLine();
            Console.WriteLine($@"    {exe_name} : apps list with status");
            Console.WriteLine($@"  ===============================================");
            Console.WriteLine($@"    [   ] = not pinned, regular app");
            Console.WriteLine($@"    [S  ] = Start menu pinned");
            Console.WriteLine($@"    [ T ] = Taskbar pinned");
            Console.WriteLine($@"    [  W] = Windows store app <appxname>");
            Console.WriteLine($@"  ===============================================");
            Console.WriteLine();

            foreach (var app in myapps.OrderBy(m => m.Name))
            {
                app.ListPrint();
            }
            Console.WriteLine();


            Console.WriteLine(@"  ===============================================");
            Console.WriteLine(@"    installed Windows store apps");
            Console.WriteLine(@"  ===============================================");
            Console.WriteLine();

            var myappx = new List<Appx>();
            getAppxList(ref myappx);
            foreach (var app in myappx.OrderBy(m => m.PackageFullName))
            {
                if(!app.is_system()) app.ListPrint();
            }
            Console.WriteLine();


            Console.WriteLine(@"  ===============================================");
            Console.WriteLine(@"    installed Windows system apps");
            Console.WriteLine(@"  ===============================================");
            Console.WriteLine();
            foreach (var app in myappx.OrderBy(m => m.PackageFullName))
            {
                if (app.is_system()) app.ListPrint();
            }
            Console.WriteLine();
            Exit(0);
        }

        static void UnpinStart(ref List<string> argslist)
        {
            //-unpinstart -all | list ...
            bool doall = argslist.RemoveAll(x => x.ToLower() == "-all") > 0;
            if (!doall && argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
            }
            argslist = argslist.Select(x => x.ToLower()).ToList();
            var myapps = new List<Apps>();
            getAppsList(ref myapps);
            foreach (var app in myapps)
            {
                if (doall || argslist.Contains(app.Name.ToLower()))
                {
                    if(app.unpin()) argslist.Remove(app.Name.ToLower());
                }
            }
            Exit(argslist.Count());
        }

        static void UnpinTaskbar(ref List<string> argslist)
        {
            //-unpintaskbar -all | list ...
            bool doall = argslist.RemoveAll(x => x.ToLower() == "-all") > 0;
            if (!doall && argslist.Count() < 2)
            {
                Help(ref argslist);
                return; //if script
            }
            argslist = argslist.Select(x => x.ToLower()).ToList();
            var myapps = new List<Apps>();
            getAppsList(ref myapps);
            foreach (var app in myapps)
            {
                if (doall || argslist.Contains(app.Name.ToLower()))
                {
                    if(app.unpintb()) argslist.Remove(app.Name.ToLower());
                }
            }
            Exit(argslist.Count());
        }

        static void PinStart(ref List<string> argslist)
        {
            //-pinstart filename | list ...
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return; //if script
            }
            argslist.RemoveAt(0);
            string fil = argslist[0];
            if (argslist.Count() == 1 && File.Exists(fil))
            {
                argslist.Clear();
                foreach (string line in File.ReadAllLines(fil))
                {
                    if (line.Length > 0)
                    {
                        argslist.Add(line.Trim());
                    };
                }
            }
            //all to lowercase
            argslist = argslist.Select(x => x.ToLower()).ToList();

            var myapps = new List<Apps>();
            getAppsList(ref myapps);
            foreach (var app in myapps)
            {
                if (argslist.Contains(app.Name.ToLower()))
                {
                    if(app.pin()) argslist.Remove(app.Name.ToLower());
                }
            }
            Exit(argslist.Count());
        }

        static void RemoveAppx(ref List<string> argslist)
        {
            //-removeappx filename | list...
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return; //if script
            }
            argslist.RemoveAt(0);
            string fil = argslist[0];
            if (argslist.Count() == 1 && File.Exists(fil))
            {
                argslist.Clear();
                foreach (string line in System.IO.File.ReadAllLines(fil))
                {
                    if (line.Length > 0)
                    {
                        argslist.Add(line.Trim());
                    };
                }
            }
            //all to lowercase, fullname to Name if needed
            //(if fullname provided, will convert to short name)
            argslist = argslist.Select(x => x.Split('_')[0].ToLower()).ToList();

            var myappx = new List<Appx>();
            getAppxList(ref myappx);
            foreach (var app in myappx)
            {
                if (argslist.Contains(app.Name.ToLower()))
                {
                    if (removePackage(app.Name, app.PackageFullName))
                    {
                        argslist.Remove(app.Name.ToLower());
                    }
                }
            }
            Exit(argslist.Count()); //number of apps in list not uninstalled
        }

        static void Wallpaper(ref List<string> argslist)
        {
            //-wallpaper filename | foldername | bing
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
            }
            string fil = argslist[1];
            bool is_dir = false;
            if (fil.ToLower() == "bing.com")
            {
                bingpaper(ref fil);
            }
            if (!File.Exists(fil) && !(is_dir = Directory.Exists(fil)))
            {
                Error(fil, "file or directory does not exist");
                return; //if script
            }
            //if dir, get random jpg in provided dir
            if (is_dir)
            {
                var rand = new Random();
                var files = Directory.GetFiles(fil, "*.jpg");
                if (files.Length == 0)
                {
                    Error(fil, "no jpg files found");
                    return;
                }
                fil = files[rand.Next(files.Length)];
            }
            //0=fail, non-zero=success 
            Exit(0 == SystemParametersInfo(20, 0, Path.GetFullPath(fil), 3) ? 1 : 0);
        }

        static void Shortcut(ref List<string> argslist)
        {
            //-shortcut name target [ -arg arguments ][ -wd workingdir ]
            //check for -arg and -wd
            string arg = null;
            string wd = null;
            int ai = argslist.IndexOf("-arg");
            if (ai > 0)
            {
                arg = argslist.ElementAtOrDefault(ai + 1);
                if (arg == null)
                {
                    Help(ref argslist);
                    return;
                }
                argslist.RemoveRange(ai, 2);
            }
            int wi = argslist.IndexOf("-wd");
            if (wi > 0)
            {
                wd = argslist.ElementAtOrDefault(wi + 1);
                if (wd == null)
                {
                    Help(ref argslist);
                    return;
                }
                argslist.RemoveRange(wi, 2);
            }
            //-shortcut name target
            if (argslist.Count() < 3)
            {
                Help(ref argslist);
                return;
            }
            try
            {
                //using iwr as 'File' name collision with IWshRuntimeLibrary
                //so- using iwr = IWshRuntimeLibrary; -at top of file
                var shell = new iwr.WshShell();
                string link = argslist[1] + ".lnk";
                var shortcut = shell.CreateShortcut(link) as iwr.IWshShortcut;
                shortcut.TargetPath = argslist[2];
                if (arg != null) shortcut.Arguments = arg;
                if (wd != null) shortcut.WorkingDirectory = wd;
                shortcut.Save();
            }
            catch
            {
                Error("creating shortcut failed");
                return;
            }
        }

        static void CreateUser(ref List<string> argslist)
        {
            //-createuser [ -admin ] username [ password ]
            bool admin = argslist.RemoveAll(x => x.ToLower() == "-admin") > 0;
            //-createuser username [ password ]
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
            }
            if (!isAdmin())
            {
                ErrorAdmin();
                return;
            }
            //now has 2 or more args
            string username = argslist[1];
            string password = argslist.Count() > 2 ? " " + argslist[2] : null;
            string args = $@"user /add {username}{password}";

            if (!processDo("net.exe", args))
            {
                Error("add user failed");
                return;
            }
            if (admin && !processDo("net.exe",
                $@"localgroup administrators /add {username}"))
            {
                Error("add to administrators group failed");
                return;
            }
        }

        static void RenamePC(ref List<string> argslist)
        {
            //-renamepc newname [ description ] (<s#> <date> <rand>)
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
            }
            if (!isAdmin())
            {
                ErrorAdmin();
                return;
            }
            //replace [s#] [date] [rand]
            string sernum = getSerialNumber();
            string date = DateTime.Now.ToLocalTime().ToString("MMddyyyy");
            string rand = ((UInt64)DateTime.Now.ToBinary()).ToString();
            rand = rand.Substring(rand.Length - 8); //last 8 digits
            for (var i = 1; i < argslist.Count(); i++)
            {
                if (argslist[i].Contains("[s#]") && sernum == null)
                {
                    Error("could not get pc serial number");
                    return;
                }
                argslist[i] = argslist[i]
                    .Replace("[s#]", sernum)
                    .Replace("[date]", date)
                    .Replace("[rand]", rand);
            }
            string newname = argslist[1];
            string desc = argslist.Count() > 2 ? argslist[2] : null;
            string oldname = Environment.GetEnvironmentVariable("computername");
            if (oldname == null)
            {
                Error("could not get current computer name");
                return;
            }
            //quote names to be safe
            if (!processDo("wmic.exe",
                 $@"ComputerSystem where Name=""{oldname}"" call Rename Name=""{newname}"""))
            {
                Error("failed to rename pc");
                return;
            }
            if (desc != null && !processDo("wmic.exe", $@"os set description=""{desc}"""))
            {
                Error("could not set pc description");
                return;
            }
        }


        //helper functions

        static bool processDo(string cmd, string args, ref string output)
        {
            output = null;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.FileName = cmd;
            startInfo.Arguments = args;
            startInfo.RedirectStandardOutput = true;

            //Console.WriteLine(cmd + " " + args);
            try
            {
                using (Process p = Process.Start(startInfo))
                {
                    output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
        static bool processDo(string cmd, string args)
        {
            //processDo without needing output
            string dummy = null;
            return processDo(cmd, args, ref dummy);
        }

        static string getSerialNumber()
        {
            string output = null;
            processDo("wmic.exe", "bios get SerialNumber", ref output);
            if (output != null && output.StartsWith("SerialNumber"))
            {
                var arr = output.Split(new char[] { '\r', '\n', ' ' },
                    System.StringSplitOptions.RemoveEmptyEntries);
                if (arr.Length == 2)
                {
                    return arr[1];
                }
            }
            return null;
        }


        static void bingpaper(ref string fil)
        {
            fil = null; //caller can now test
            //no need to try if no internet
            if (!isInternetUp())
            {
                Error("no internet connection to bing.com");
                return;
            }
            //get todays bing picture store in public pictures folder
            if (!Directory.Exists(bingfolder))
            {
                try
                {
                    Directory.CreateDirectory(bingfolder);
                }
                catch (Exception)
                {
                    Error(bingfolder, "cannot create directory");
                    return;
                }
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
                return;
            }
            string jpgname = $@"{bingfolder}\{jpgurl.Split('/').Last()}";
            //may have already downloaded, check
            if (!File.Exists(jpgname))
            {
                wc.DownloadFile(jpgurl, jpgname);
            }
            if (File.Exists(jpgname))
            {
                fil = jpgname;
            }
        }

        static bool removePackage(string nam, string fullnam)
        {
            Console.Write(nam + "...");
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
                //DeploymentResult deploymentResult = deploymentOperation.GetResults();
                //Console.WriteLine("error {0}", deploymentOperation.ErrorCode);
                //Console.WriteLine("Error text: {0}", deploymentResult.ErrorText);
                Console.WriteLine("failed");
            }
            else if (deploymentOperation.Status == AsyncStatus.Canceled)
            {
                Console.WriteLine("canceled");
            }
            else if (deploymentOperation.Status == AsyncStatus.Completed)
            {
                Console.WriteLine("removed");
                return true;
            }
            else
            {
                Console.WriteLine("unknown status");
            }

            return false;
        }

        static bool isAdmin()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
        }

        static bool isInternetUp()
        {
            try
            {
                Ping p = new Ping();
                String host = "8.8.8.8";
                int timeout = 2000;
                PingReply reply = p.Send(host, timeout);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void getAppsList(ref List<Apps> myapps)
        {
            //get apps in appsfolder namespace
            Type t = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(t);
            var appobj = shell.NameSpace("shell:AppsFolder");

            foreach (var item in appobj.Items())
            {
                var a = new Apps();
                a.Name = item.Name;
                a.Path = item.Path;
                a.Verbs = item.Verbs();
                //file explorer, need alternate location for verbs
                if (item.Name.ToLower() == "file explorer")
                {
                    var v = fileExplorerVerbs();
                    if (v != null) a.Verbs = v;
                }
                //check if appx
                if (item.Path.Contains("_") && item.Path.Contains("!"))
                {
                    a.AppxName = item.Path.Split('_')[0];
                }
                myapps.Add(a);
            }
        }

        static void getAppxList(ref List<Appx> myappx)
        {
            PackageManager packageManager = new Windows.Management.Deployment.PackageManager();
            IEnumerable<Package> packages =
                packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);

            foreach (Package package in packages)
            {
                var a = new Appx();
                a.Name = package.Id.Name;
                a.PackageFullName = package.Id.FullName;
                a.InstalledLocation = package.InstalledLocation.Path;
                myappx.Add(a);
            }
        }

        static dynamic fileExplorerVerbs()
        {
            //File Explorer needs alternate location for verbs()
            Type t = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(t);
            //use another namespace where File Explorer is located- start menu places
            var feobj = shell.NameSpace(
                    Environment.GetEnvironmentVariable("ProgramData") +
                    @"\Microsoft\Windows\Start Menu Places"
                    );
            //find file explorer, return its Verbs()
            foreach (var v in feobj.Items())
            {
                if (v.path.ToLower().Contains("file explorer")) return v.Verbs();
            }
            return null;
        }


    }
}
