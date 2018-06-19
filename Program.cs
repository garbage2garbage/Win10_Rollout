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
using iwr = IWshRuntimeLibrary;


namespace ConsoleApp
{
    public delegate void optdo(ref List<string> argslist);

    public class Options
    {
        public Options(string nam, optdo h) {
            Name = nam;
            Handler = h;
        }
        public string Name;
        public optdo Handler;
    }

    public class Apps
    {
        public Apps() { }
        public string FullName { get; set; }    //for appx removal, need full name
        public string Name { get; set; }        //display name from shell:AppsFolder
        public string Path { get; set; }        //path from shell:AppsFolder
        public dynamic Verbs { get; set; }      //verbs() pin/unpin/unpintb menu items
        const string unpintb_str = "Unpin from tas&kbar";
        const string unpin_str = "Un&pin from Start";
        const string pin_str = "&Pin to Start";
        public string ListPrint()
        {
            if (is_appx_hidden()) return "[  W] " + FullName;
            else
            {
                char[] c = {'[',' ',' ',' ',']',' '};
                if (is_pinned())    c[1] = 'S';
                if (is_tbpinned())  c[2] = 'T';
                if (is_appx())      c[3] = 'W';
                string s = new string(c);
                return s + Name;
            }

        }
        public bool is_appx()
        {
            return  FullName != null && FullName != "";
        }
        public bool is_appx_hidden()
        {
            return is_appx() && (Name == null || Name == "");
        }
        public bool is_tbpinned()
        {
            if (Verbs == null) return false;
            foreach (var v in Verbs) {
                if (v.Name == unpintb_str) return true;
            }
            return false;
        }
        public bool is_pinned()
        {
            if (Verbs == null) return false;
            foreach (var v in Verbs) {
                if (v.Name == unpin_str) return true;
            }
            return false;
        }
        public bool appx_name_match(ref List<string> argslist)
        {
            if (!is_appx()) { return false; }
            //argslist is all lowercase
            if (argslist.Contains(Name.ToLower())){ return true; }
            foreach(var nam in argslist) {
                if (nam.Contains("_") && FullName.ToLower().StartsWith(nam)) return true;
            }
            return false;
        }
        public bool unpin()
        {
            if (Verbs == null || !is_pinned()) return false;
            foreach (var v in Verbs) {
                if (v.Name == unpin_str) { v.DoIt(); return true; }
            }
            return false;
        }
        public bool unpintb()
        {
            if (Verbs == null || !is_tbpinned()) return false;
            foreach (var v in Verbs) {
                if (v.Name == unpintb_str) { v.DoIt(); return true; }
            }
            return false;
        }
        public bool pin()
        {
            if (Verbs == null || is_pinned()) return false;
            foreach (var v in Verbs) {
                if (v.Name == pin_str) { v.DoIt(); return true; }
            }
            return false;
        }
    }

    class Program
    {
        public const string version = "1803.00";
        public const string NL = "\n";
        public static string exe_name; //name of this script without path, set in main
        public static string sysdrive; //normally C:
        public static string bingfolder; //where to store bing wallpaper

        //set wallpaper dll
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        static void Usage()
        {
            Error(
                NL + "   to see a valid argument list-> " +
                exe_name + " -help" + NL
                );
        }

        static void Help(ref List<string> argslist)
        {
            string specific_str = null;
            bool specific = false;
            //argslist will be empty if plain -help
            //else argslist[0] will contain a valid option for specific help
            if (argslist.Count() > 0) {
                specific = true;
                specific_str = argslist[0];
            }                  //.........|.........|.........|.........|.........|.........|.........|.........|
            Console.WriteLine();
            Console.WriteLine($@"   {exe_name}   v{version}    2018@curtvm");
            Console.WriteLine();
            if (!specific) {
            Console.WriteLine($@"   options:     for specific help, use -option -help");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-listapps") {
            Console.WriteLine($@"   -listapps");
            }
            if (specific && specific_str == "-listapps") { 
            Console.WriteLine($@"       list all available apps with status");
            Console.WriteLine($@"       to save the apps list to a file-");
            Console.WriteLine($@"       {sysdrive}\>{exe_name} -listapps > saved.txt");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-pinstart") {
            Console.WriteLine($@"   -pinstart filename | appname1 [ appname2 ""app name 3"" ... ]");
            }
            if (specific && specific_str == "-pinstart") {
            Console.WriteLine($@"       pin apps to start menu from a -listapps saved file or");
            Console.WriteLine($@"       specify app(s) with one or more space separated app name(s)");
            Console.WriteLine($@"       app names with spaces need to be quoted");
            Console.WriteLine($@"       (can use '-unpinstart all' to first clear pinned apps)");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-unpinstart") {
            Console.WriteLine($@"   -unpinstart all | appname1 [ appname2 ""app name 3"" ... ]");
            }
            if (specific && specific_str == "-unpinstart") {
            Console.WriteLine($@"       unpin all apps or specified apps from start menu tiles");
            Console.WriteLine($@"       (placeholders/suggested apps in start menu will not be removed)");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-unpintaskbar") {
            Console.WriteLine($@"   -unpintaskbar all | appname1 [ appname2 ""app name 3"" ... ]");
            }
            if (specific && specific_str == "-unpintaskbar") {
            Console.WriteLine($@"       unpin all apps or specified apps from taskbar");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-removeappx") {
            Console.WriteLine($@"   -removeappx filename | appname1 [ appname2 ""app name 3"" ... ]");
            }
            if (specific && specific_str == "-removeappx") {
            Console.WriteLine($@"       remove Windows store app(s) for the current user from a");
            Console.WriteLine($@"       modified -listapps saved file or specify app(s) with one or more");
            Console.WriteLine($@"       space separated app name(s), names with spaces need to be quoted,");
            Console.WriteLine($@"       use either friendly name or full name (from -listapps)");
            Console.WriteLine($@"       (full name must include at least the first underscore _ character)");
            Console.WriteLine($@"       to modify a -listapps saved file, replace W marked app with X to remove");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-wallpaper") {
            Console.WriteLine($@"   -wallpaper filename | foldername | Bing"); 
            }
            if (specific && specific_str == "-wallpaper") {
            Console.WriteLine($@"       set wallpaper to filename");
            Console.WriteLine($@"       set wallpaper to random jpg picture from foldername");
            Console.WriteLine($@"       set wallpaper to daily image from Bing.com");
            Console.WriteLine($@"       (picture saved in " + bingfolder + ")");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-regimport") {
            Console.WriteLine($@"   -regimport [ -defaultuser ] filename");
            }
            if (specific && specific_str == "-regimport") {
            Console.WriteLine($@"       import a .reg type registry file");
            Console.WriteLine($@"       -defaultuser option will import a previously loaded/modified/saved");
            Console.WriteLine($@"       registry hive file (.reg) into the default user hive (ntuser.dat)");
            Console.WriteLine($@"       the hive will be loaded into the same key the reg file used, so");
            Console.WriteLine($@"       the reg file will not have to be modified");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-weather") {
            Console.WriteLine($@"   -weather [ settings.dat ]");
            }
            if (specific && specific_str == "-weather") {
            Console.WriteLine($@"       set Weather app to default location (51247)");
            Console.WriteLine($@"       or provide a Weather settings.dat file");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-timezone") {
            Console.WriteLine($@"   -timezone ""timezonestring""");
            }
            if (specific && specific_str == "-timezone") {
            Console.WriteLine($@"       set system timezone");
            Console.WriteLine($@"       {sysdrive}\>{exe_name} -timezone ""Central Standard Time""");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-shortcut") {
            Console.WriteLine($@"   -shortcut name target [ -arg arguments ][ -wd workingdir ]");
            }
            if (specific && specific_str == "-shortcut") {
            Console.WriteLine($@"       create shortcut, provide shortcut path and name, and target path");
            Console.WriteLine($@"       and name, -arg is for optional target arguments, and -wd for");
            Console.WriteLine($@"       optional working directory path");
            Console.WriteLine($@"       {sysdrive}\>{exe_name} -shortcut ""c:\users\me\desktop\Notepad"" ""c:\windows\notepad.exe""");
            Console.WriteLine();
            }
            if (!specific || specific && specific_str == "-hidelayoutxml") {
            Console.WriteLine($@"   -hidelayoutxml");
            }
            if (specific && specific_str == "-hidelayoutxml") {
            Console.WriteLine($@"       rename default user DefaultLayouts.xml start menu file");
            Console.WriteLine($@"       to get a minimal start menu layout for new users");
            Console.WriteLine($@"       (no placeholders or suggested apps, just Settings/Store/Edge)");
            Console.WriteLine();
            }
            if (!specific){ 
            Console.WriteLine();
            }
            Environment.Exit(0);
        }

        static void Error(string filnam, string msg) {
            Console.WriteLine("   {0} : {1}", filnam, msg);
            Environment.Exit(1);
        }

        static void Error(string msg)
        {
            Console.WriteLine(msg);
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            //script (exe) name
            exe_name = Environment.GetCommandLineArgs()[0].Split('\\').Last();
            //maybe overkill, but in case os installed on something other than C:
            sysdrive = Environment.GetEnvironmentVariable("systemdrive");
            if (sysdrive == null) sysdrive = "C:"; //just in case
            //bing picture folder -> public pictures \Bing
            bingfolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures);
            if (bingfolder == null) {
                bingfolder = sysdrive + @"\users\public\pictures\Bing";
            } else {
                bingfolder = bingfolder + "\\Bing";
            }

            //need at least one arg
            if (args.Length == 0) Usage();

            //args list (cmd line as-is)
            var argslist = new List<string>(args);

            //options list
            var optionslist = new List<Options>();
            optionslist.Add(new Options("-unpinstart", UnpinStart));
            optionslist.Add(new Options("-listapps", ListApps));
            optionslist.Add(new Options("-unpintaskbar", UnpinTaskbar));
            optionslist.Add(new Options("-pinstart", PinStart));
            optionslist.Add(new Options("-removeappx", RemoveAppx));
            optionslist.Add(new Options("-wallpaper", Wallpaper));
            optionslist.Add(new Options("-timezone", Timezone));
            optionslist.Add(new Options("-regimport", RegImport));
            optionslist.Add(new Options("-weather", Weather));
            optionslist.Add(new Options("-shortcut", Shortcut));
            optionslist.Add(new Options("-hidelayoutxml", HideLayoutXml));
            optionslist.Add(new Options("-help", Help));
            
            //check option against our list
            var opt = optionslist.Find(x => x.Name == argslist[0].ToLower());

            //if cannot find, show usage
            if (opt == null) Usage();

            //if -help, clear argslist and call function
            if (opt.Name == "-help") {
                argslist.Clear();
                opt.Handler(ref argslist);
            }

            //specific help -listapps -help
            if (argslist.Count() > 1 && argslist[1].ToLower() == "-help") {
                Help(ref argslist); //argslist[0] contains specific help option
            }

            //if not -help, remove first arg
            argslist.RemoveAt(0);

            //now call function
            opt.Handler(ref argslist);

            

            //get command line options
            //check first option- need valid option as first arg
            //although skipped in above list, we still have it in args[0]
            //switch (args[0].ToLower())
            //{
            //    case "-unpinstart":     UnpinStart(ref argslist); break;
            //    case "-listapps":       ListApps(ref argslist); break;
            //    case "-unpintaskbar":   UnpinTaskbar(ref argslist); break;
            //    case "-pinstart":       PinStart(ref argslist); break;
            //    case "-removeappx":     RemoveAppx(ref argslist); break;
            //    case "-wallpaper":      Wallpaper(ref argslist); break;
            //    case "-timezone":       Timezone(ref argslist); break;
            //    case "-regimport":      RegImport(ref argslist); break;
            //    case "-weather":        Weather(ref argslist); break;
            //    case "-shortcut":       Shortcut(ref argslist); break;
            //    case "-hidelayoutxml":  HideLayoutXml(ref argslist); break;
            //    case "-help":           Help(ref argslist); break;

            //    default:                Usage(); break;
            //}
        } //Main

        static void ListApps(ref List<string> argslist)
        {
            var myapps = new List<Apps>();
            GetAppsList(ref myapps, true); //true=get appx also
            Console.WriteLine(
            NL +
            "    " + exe_name + " : apps list with status" + NL +
            "  ===============================================" + NL +
            "    [   ] = not pinned, regular app" + NL +
            "    [S  ] = Start menu pinned" + NL +
            "    [ T ] = Taskbar pinned" + NL +
            "    [  W] = Windows store app" + NL +
            "  ===============================================" + NL
            );

            foreach (var app in myapps.OrderBy(m => m.Name))
            {
                //normal/visible apps first
                if (app.is_appx_hidden()) continue;
                Console.WriteLine(app.ListPrint());
            }
            Console.WriteLine(
            NL +
            "  ===============================================" + NL +
            "    other Windows apps not in AppsFolder" + NL +
            "  ===============================================" + NL
            );
            foreach (var app in myapps.OrderBy(m => m.FullName))
            {
                //hidden windows apps
                if (!app.is_appx_hidden()) continue;
                Console.WriteLine(app.ListPrint());
            }
            Console.WriteLine();
            Environment.Exit(0);
        }

        static dynamic fileExplorerVerbs() {
            //File Explorer needs alternate location for verbs()
            Type t = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(t);
            //use another namespace where File Explorer is located- start menu places
            var feobj = shell.NameSpace(
                    Environment.GetEnvironmentVariable("ProgramData") +
                    "\\Microsoft\\Windows\\Start Menu Places"
                    );
            //find file explorer, return its Verbs()
            foreach (var v in feobj.Items())
            {
                if (v.path.ToLower().Contains("file explorer"))
                {
                    return v.Verbs();
                }
            }
            return null;
        }

        static bool IsAdmin() {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
        }

        static bool IsInternetUp()
        {
            try
            {
                Ping p = new Ping();
                String host = "8.8.8.8";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = p.Send(host, timeout, buffer, pingOptions);
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        static void GetAppsList(ref List<Apps> myapps, bool getappx)
        {
            //if appx not needed- like unpinstart,pinstart,unpintaskbar
            //no need to get appx list

            //get Appx list first
            var appxlist = new List<string>();
            if (getappx){ getAppxList(ref appxlist); }

            //get apps in appsfolder namespace
            Type t = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(t);
            var appobj = shell.NameSpace("shell:AppsFolder");

            //cannot remove these from appxlist until finished
            //as some (1?) apps like calendar/mail refer to the same
            //appx- so keep a list instead
            var alreadydone = new List<string>();
            foreach (var item in appobj.Items())
            {
                var a = new Apps();
                a.Name = item.Name;
                a.Path = item.Path;
                a.Verbs = item.Verbs();
                //file explorer, need alternate location for verbs
                if (item.Name.ToLower() == "file explorer") {
                    var v = fileExplorerVerbs();
                    if (v != null)
                    {
                        a.Verbs = v;
                    }
                }               
                //check if win app (for -listapps and removeappx)
                if(getappx && item.Path.Contains("_") && item.Path.Contains("!"))
                {
                    var tmp = item.Path.Split('_')[0];
                    foreach (var n in appxlist)
                    {
                        if(n.StartsWith(tmp))
                        {
                            a.FullName = n;
                            alreadydone.Add(n);
                            break;
                        }
                    }
                }
                myapps.Add(a);
            }
            if (!getappx) return;

            //now add remaining appxlist
            foreach(var nam in appxlist)
            {
                if (alreadydone.Contains(nam)) continue;
                var a = new Apps();
                a.FullName = nam;
                myapps.Add(a);
            }
        }

        static void getAppxList(ref List<string> appxlist)
        {
            PackageManager packageManager = new Windows.Management.Deployment.PackageManager();
            IEnumerable<Package> packages =
                packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);

            foreach (Package package in packages)
            {
                appxlist.Add(package.Id.FullName);
            }
        }

        static void UnpinStart(ref List<string> argslist)
        {
            if (argslist.Count() == 0) Usage();
            bool doall = false;
            if (argslist.Count() == 1 && argslist[0].ToLower() == "all")
            {
                doall = true;
            }
            else
            {
                argslist = argslist.Select(x => x.ToLower()).ToList();
            }

            var myapps = new List<Apps>();
            GetAppsList(ref myapps, false); //false=appx not needed

            foreach (var app in myapps)
            {
                if (doall || argslist.Contains(app.Name.ToLower()))
                {
                    //just do it- Apps class will check if valid to do
                    app.unpin(); 
                }
            }
            Environment.Exit(0);
        }

        static void UnpinTaskbar(ref List<string> argslist)
        {
            if (argslist.Count() == 0) Usage();
            bool doall = false;
            if (argslist.Count() == 1 && argslist[0].ToLower() == "all")
            {
                doall = true;
            }
            else
            {
                argslist = argslist.Select(x => x.ToLower()).ToList();
            }
            var myapps = new List<Apps>();
            GetAppsList(ref myapps, false); //false=appx not needed

            foreach (var app in myapps)
            {
                if (doall || argslist.Contains(app.Name.ToLower()))
                {
                    //just do it- Apps class will check if valid
                    app.unpintb();
                }
            }
            Environment.Exit(0);
        }

        static void PinStart(ref List<string> argslist)
        {
            if (argslist.Count() == 0) Usage();
            string fil = argslist[0];
            if (argslist.Count() == 1 && File.Exists(fil))
            {
                argslist.Clear();
                foreach (string line in System.IO.File.ReadAllLines(fil))
                {
                    if (line.Length > 6 && line.StartsWith("[S"))
                    {
                        argslist.Add(line.Substring(6).ToLower().Trim());
                    };
                }
            }
            else
            {
                argslist = argslist.Select(x => x.ToLower()).ToList();
            }

            var myapps = new List<Apps>();
            GetAppsList(ref myapps, false); //false=appx not needed

            foreach (var app in myapps)
            {
                if (argslist.Contains(app.Name.ToLower()))
                {
                    //just do it- Apps class will check if valid
                    app.pin();
                }
            }
            Environment.Exit(0);
        }

        static void RemoveAppx(ref List<string> argslist)
        {
            if (argslist.Count() == 0) Usage();
            string fil = argslist[0];
            if (argslist.Count() == 1 && File.Exists(fil))
            {
                argslist.Clear();
                foreach (string line in System.IO.File.ReadAllLines(fil))
                {
                    if (line.Length > 6 && line.Substring(3).StartsWith("X] "))
                    {
                        argslist.Add(line.Substring(6).ToLower().Trim());
                    };
                }
            }
            else
            {
                //user provided
                argslist = argslist.Select(x => x.ToLower()).ToList();
            }

            var myapps = new List<Apps>();
            GetAppsList(ref myapps, true); //true=get appx also

            // provide fullname
            //  Microsoft.Windows.SecHealthUI_10.0.16299.402_neutral__cw5n1h2txyewy
            // or provide friendly name 
            //  Windows Defender Security Center
            // or provide partial fullname at least including first underscore
            //  Microsoft.Windows.SecHealthUI_

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
            int ret = argslist.Count();
            foreach(var app in myapps)
            {
                if (app.appx_name_match(ref argslist))
                {
                    if (remove_package(app.FullName))
                    {
                        ret--;
                    }
                }
            }
            Environment.Exit(ret);
        }

        static bool remove_package(string fullname)
        {
            Console.Write(fullname.Split('_')[0] + "...");
            PackageManager packageManager = new Windows.Management.Deployment.PackageManager();

            IAsyncOperationWithProgress<DeploymentResult, DeploymentProgress> deploymentOperation =
                packageManager.RemovePackageAsync(fullname);
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
                Console.WriteLine("error {0}", deploymentOperation.ErrorCode);
                //Console.WriteLine("Error text: {0}", deploymentResult.ErrorText);
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

        static void Timezone(ref List<string> argslist)
        {
            if (argslist.Count() == 0) Usage();
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "tzutil.exe",
                Arguments = "/s \"" + argslist[0] + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) { Environment.Exit(1); }

            process.WaitForExit();
            TimeZoneInfo.ClearCachedData();
            Environment.Exit(process.ExitCode);
        }

        static void Wallpaper(ref List<string> argslist) {
            if (argslist.Count() == 0) Usage();
            string fil = argslist[0];
            bool is_dir = false;
            if (fil.ToLower() == "bing")
            {
                bingpaper(ref fil);
            }
            if (!File.Exists(fil) && !(is_dir = Directory.Exists(fil))) {
                Error(fil, "file or directory does not exist");
            }
            //if dir, get random jpg in provided dir
            if (is_dir) {
                var rand = new Random();
                var files = Directory.GetFiles(fil, "*.jpg");
                if (files.Length == 0)
                {
                    Error(fil, "no jpg files found");
                }
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
            if (!Directory.Exists(bingfolder))
            {
                try { Directory.CreateDirectory(bingfolder); }
                catch (Exception) { Error(bingfolder, "cannot create directory"); }
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
            string jpgname = bingfolder + "\\" +jpgurl.Split('/').Last();
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

        static void RegImport(ref List<string> argslist) {
            if (argslist.Count() == 0) Usage();
            bool du = false;
            if (argslist[0].ToLower() == "-defaultuser") {
                du = true;
                argslist.RemoveAt(0);
                if (argslist.Count() == 0) Usage();
            }
            string fil = argslist[0];
            if (!File.Exists(fil)) Error(fil, "file does not exist");
            if (du) {
                if (!IsAdmin()) Error("command needs to be run as Administrator");
                regimport_du(fil); //failure there will exit
                Environment.Exit(0); //must have worked
            } else { //normal import
                bool ret = regdo("import " + fil);
                if (!ret && !IsAdmin()) { Error("command may need to be run as Administrator"); }
                Environment.Exit(ret?0:1);
            }
        }

        static void regimport_du(string fil)
        {
            //get reg key where reg file was previously loaded
            //[HKEY_LOCAL_MACHINE\1\Software\Microsoft\Windows\CurrentVersion\Run]
            //would be HKEY_LOCAL_MACHINE\1 -> HKLM\1
            string lkey = null;
            foreach (string line in System.IO.File.ReadAllLines(fil)) {
                //only get first one, assume all are the same
                if (line.StartsWith("[HKEY_LOCAL_MACHINE\\")) {
                    lkey = "HKLM";
                } else if (line.StartsWith("[HKEY_CURRENT_USER\\")) {
                    lkey = "HKU";
                }
                if (lkey == null) continue;
                string[] ss = line.Split('\\');
                if (ss.Count() > 2) lkey = lkey + "\\" + ss[1];
                else lkey = null;
                break;
            }
            if(lkey == null) Error("cannot decode reg file import location");

            string ntuserdat = sysdrive + @"\users\default\ntuser.dat";
            string ntuserbak = ntuserdat + ".original";
            //backup ntuser.dat -> ntuser.dat.original if not done already
            if (!File.Exists(ntuserbak)) {
                try {
                    File.Copy(ntuserdat, ntuserbak);
                }
                catch (Exception) {
                    Error("could not backup default user ntuser.dat");
                }
            }

            //check if key already exists (cannot load into existing key)
            if (regdo("query " + lkey)) {
                Error("reg file import location cannot be used");
            }

            //load ntuser.dat file to same location
            if (!regdo("load " + lkey + " " + ntuserdat)) {
                Error("Unable to load default user registry hive");
            }

            //import reg file
            bool ret = regdo("import " + fil);
            //unload ntuser.dat (if previous succeeded or not)
            ret &= regdo("unload " + lkey);
            //ret == true if both succeeded
            Environment.Exit(ret ? 0 : 1);
        }

        static bool regdo(string cmd)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = cmd,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null) { return false; }
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        static void Weather(ref List<string> argslist) {
            string fil = null;
            if (argslist.Count() > 0)
            {
                fil = argslist[0];
            }
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
            if (fil == null)
            {
                System.IO.File.WriteAllBytes(wxdir + @"\Settings\settings.dat", AppZero.Properties.Resources.settings_dat);
            }
            else {
                if (!File.Exists(fil)) { Error(fil,"file does not exist"); }
                File.Copy(fil, wxdir + @"\Settings\settings.dat");
            }
            Environment.Exit(0); //for now, should check above results
        }

        static void Shortcut(ref List<string> argslist)
        {
            if (argslist.Count() < 2) Usage();
            //name target [ -arg arguments ][ -wd workingdir ]
            string link = argslist[0] + ".lnk";
            try
            {
                //using iwr as 'File' name collision with IWshRuntimeLibrary
                //so- using iwr = IWshRuntimeLibrary; -at top of file
                var shell = new iwr.WshShell();
                var shortcut = shell.CreateShortcut(link) as iwr.IWshShortcut;
                shortcut.TargetPath = argslist[1];
                argslist.RemoveRange(0, 2);
                while (argslist.Count() > 1)
                {
                    if (argslist[0].ToLower() == "-arg")
                    {
                        //added to target as arguments
                        shortcut.Arguments = argslist[1];
                    }
                    else if (argslist[0].ToLower() == "-wd")
                    {
                        //Start In
                        shortcut.WorkingDirectory = argslist[1];
                    }
                    argslist.RemoveRange(0, 2);
                }
                shortcut.Save();
            } catch {
                Error("creating shortcut failed");
            }
            Environment.Exit(0);
        }

        static void HideLayoutXml(ref List<string> argslist)
        {
            if (!IsAdmin()) Error("command needs to be run as Administrator");
            string xml = sysdrive + @"\users\default\appdata\local\microsoft\windows\shell\DefaultLayouts.xml";
            string xmlbak = xml + ".bak";
            if (File.Exists(xml)) {
                try { File.Move(xml, xmlbak); } catch {
                    Error(xml.Split('\\').Last(), "Unable to rename");
                }
            }
            Environment.Exit(0);
        }
    }
}








