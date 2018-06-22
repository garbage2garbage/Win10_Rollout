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

namespace ConsoleApp
{
    public delegate void optdo(ref List<string> argslist);

    public class Options
    {
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
                return String.Format("[{0}{1}{2}] {3}",
                    is_pinned() ? "S" : " ",
                    is_tbpinned() ? "T" : " ",
                    is_appx() ? "W" : " ",
                    Name);
            }

        }
        public bool is_appx()
        {
            return FullName != null;
        }
        public bool is_appx_hidden()
        {
            return is_appx() && Name == null;
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
        public bool appx_name_match(ref List<string> argslist)
        {
            if (!is_appx()) { return false; }
            //argslist is all lowercase
            if (argslist.Contains(Name.ToLower())) { return true; }
            foreach (var nam in argslist)
            {
                if (nam.Contains("_") && FullName.ToLower().StartsWith(nam)) return true;
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

    class Program
    {
        public const string version = "1803.00";
        public const string NL = "\n";
        public static string exe_name; //name of this script without path, set in main
        public static string sysdrive; //normally C:
        public static string bingfolder; //where to store bing wallpaper
        public static List<Options> optionslist;
        public static bool script_is_running = false; //if script running, no exits
        public static int error_count = 0;

        //set wallpaper dll
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        static void Exit(int exitcode)
        {
            if (script_is_running)
            {
                error_count += exitcode;
                return; //back to script
            }
            Environment.Exit(exitcode);
        }

        static void Help(ref List<string> argslist)
        {
            if (script_is_running)
            {
                error_count++; //inc error count
                return; //and ignore for scripts
            }
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
                Console.WriteLine($@"   options:     for specific help, use -optionname -help");
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
                Console.WriteLine($@"    to save the apps list to a file-");
                Console.WriteLine($@"    c:\>{exe_name} -listapps > saved.txt");
                Console.WriteLine();
            }
            if (!specific || specific_str == "-pinstart")
            {
                Console.WriteLine($@"   -pinstart filename | appname1 [ appname2 ""app name 3"" ... ]");
            }
            if (specific_str == "-pinstart")
            {
                Console.WriteLine(AppZero.Properties.Resources.pinstart_help);
            }
            if (!specific || specific_str == "-unpinstart")
            {
                Console.WriteLine($@"   -unpinstart all | appname1 [ appname2 ""app name 3"" ... ]");
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
                Console.WriteLine($@"   -unpintaskbar all | appname1 [ appname2 ""app name 3"" ... ]");
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
                Console.WriteLine(AppZero.Properties.Resources.removeappx_help);
            }
            if (!specific || specific_str == "-wallpaper")
            {
                Console.WriteLine($@"   -wallpaper filename | foldername | Bing");
            }
            if (specific_str == "-wallpaper")
            {
                Console.WriteLine(AppZero.Properties.Resources.wallpaper_help.Replace("{bingfolder}",bingfolder));
            }
            if (!specific || specific_str == "-regimport")
            {
                Console.WriteLine($@"   -regimport [ -defaultuser ] filename");
            }
            if (specific_str == "-regimport")
            {
                Console.WriteLine(AppZero.Properties.Resources.regimport_help.Replace("{exe_name}",exe_name));
            }
            if (!specific || specific_str == "-weather")
            {
                Console.WriteLine($@"   -weather [ settings.dat ]");
            }
            if (specific_str == "-weather")
            {
                Console.WriteLine();
                Console.WriteLine($@"    set Weather app to default location (51247)");
                Console.WriteLine($@"    or provide a Weather settings.dat file");
                Console.WriteLine();
            }
            if (!specific || specific_str == "-timezone")
            {
                Console.WriteLine($@"   -timezone ""timezonestring""");
            }
            if (specific_str == "-timezone")
            {
                Console.WriteLine();
                Console.WriteLine($@"    set system timezone");
                Console.WriteLine($@"    {sysdrive}\>{exe_name} -timezone ""Central Standard Time""");
                Console.WriteLine();
            }
            if (!specific || specific_str == "-shortcut")
            {
                Console.WriteLine($@"   -shortcut name target [ -arg arguments ][ -wd workingdir ]");
            }
            if (specific_str == "-shortcut")
            {
                Console.WriteLine(AppZero.Properties.Resources.shortcut_help.Replace("{exe_name}",exe_name));
            }
            if (!specific || specific_str == "-hidelayoutxml")
            {
                Console.WriteLine($@"   -hidelayoutxml");
            }
            if (specific_str == "-hidelayoutxml")
            {
                Console.WriteLine(AppZero.Properties.Resources.hidelayoutxml_help);
            }
            if (!specific || specific_str == "-createuser")
            {
                Console.WriteLine($@"   -createuser [ -admin ] username [ password ]");
            }
            if (specific_str == "-createuser")
            {
                Console.WriteLine(AppZero.Properties.Resources.createuser_help);
            } 
            if (!specific || specific_str == "-renamepc")
            {
                Console.WriteLine($@"   -renamepc newname [ -s# ]");
            }
            if (specific_str == "-renamepc")
            {
                Console.WriteLine(AppZero.Properties.Resources.renamepc_help);
            } 
            if (!specific || specific_str == "-scriptfile")
            {
                Console.WriteLine($@"   -scriptfile filename");
            }
            if (specific_str == "-scriptfile")
            {
                Console.WriteLine(AppZero.Properties.Resources.scriptfile_help.Replace("{exe_name}",exe_name));
            }            
            if (!specific)
            {
                Console.WriteLine();
            }
            Environment.Exit(0);
        }

        static void Error(string filnam, string msg)
        {
            Console.WriteLine();
            Console.WriteLine("   {0} : {1}", filnam, msg);
            Console.WriteLine();
            if (script_is_running)
            {
                error_count++;
                return;
            }
            Exit(1);
        }

        static void Error(string msg)
        {
            Console.WriteLine();
            Console.WriteLine("   " + msg);
            Console.WriteLine();
            if (script_is_running)
            {
                error_count++;
                return;
            }
            Exit(1);
        }

        static void ErrorAdmin(){
            Error("need to run this command as Administrator");
        }

        //maximize/minimize window
        [DllImport("user32.dll")]
        static extern bool ShowWindow(System.IntPtr hWnd, int cmdShow);
        static void Maximize()
        {
            Process p = Process.GetCurrentProcess();
            ShowWindow(p.MainWindowHandle, 3); //SW_MAXIMIZE = 3
        }
        static void Minimize()
        {
            Process p = Process.GetCurrentProcess();
            ShowWindow(p.MainWindowHandle, 6); //SW_MINIMIZE = 6
        }

        static void Main(string[] args)
        {
            //script (exe) name
            exe_name = Environment.GetCommandLineArgs()[0].Split('\\').Last();
            //in case os installed on something other than C: (slim chance)
            sysdrive = Environment.GetEnvironmentVariable("systemdrive");
            if (sysdrive == null) sysdrive = "C:"; //just in case
            //bing picture folder -> public pictures \Bing
            bingfolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures);
            if (bingfolder == null)
            {
                bingfolder = sysdrive + @"\users\public\pictures\Bing";
            }
            else
            {
                bingfolder = bingfolder + "\\Bing";
            }

            //args list (cmd line as-is)
            var argslist = new List<string>(args);

            if (argslist.Count() == 0) Help(ref argslist);

            //options list- name, function name
            optionslist = new List<Options>();
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
            optionslist.Add(new Options("-scriptfile", ScriptFile));
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

        } //Main

        static void ListApps(ref List<string> argslist)
        {
            var myapps = new List<Apps>();
            getAppsList(ref myapps, true); //true=get appx also
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
            Exit(0);
        }

        static dynamic fileExplorerVerbs()
        {
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

        static void getAppsList(ref List<Apps> myapps, bool getappx)
        {
            //if appx not needed- like unpinstart,pinstart,unpintaskbar
            //no need to get appx list

            //get Appx list first
            var appxlist = new List<string>();
            if (getappx) { getAppxList(ref appxlist); }

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
                if (item.Name.ToLower() == "file explorer")
                {
                    var v = fileExplorerVerbs();
                    if (v != null)
                    {
                        a.Verbs = v;
                    }
                }
                //check if win app (for -listapps and removeappx)
                if (getappx && item.Path.Contains("_") && item.Path.Contains("!"))
                {
                    var tmp = item.Path.Split('_')[0];
                    foreach (var n in appxlist)
                    {
                        if (n.StartsWith(tmp))
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
            foreach (var nam in appxlist)
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
            if (argslist.Count() == 1)
            {
                Help(ref argslist);
                return; //if script
            }
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
            getAppsList(ref myapps, false); //false=appx not needed

            foreach (var app in myapps)
            {
                if (doall || argslist.Contains(app.Name.ToLower()))
                {
                    //just do it- Apps class will check if valid to do
                    app.unpin();
                }
            }
            Exit(0);
        }

        static void UnpinTaskbar(ref List<string> argslist)
        {
            if (argslist.Count() == 1)
            {
                Help(ref argslist);
                return; //if script
            }
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
            getAppsList(ref myapps, false); //false=appx not needed

            foreach (var app in myapps)
            {
                if (doall || argslist.Contains(app.Name.ToLower()))
                {
                    //just do it- Apps class will check if valid
                    app.unpintb();
                }
            }
            Exit(0);
        }

        static void PinStart(ref List<string> argslist)
        {
            if (argslist.Count() == 1)
            {
                Help(ref argslist);
                return; //if script
            }
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
            getAppsList(ref myapps, false); //false=appx not needed

            foreach (var app in myapps)
            {
                if (argslist.Contains(app.Name.ToLower()))
                {
                    //just do it- Apps class will check if valid
                    app.pin();
                }
            }
            Exit(0);
        }

        static void RemoveAppx(ref List<string> argslist)
        {
            if (argslist.Count() == 1)
            {
                Help(ref argslist);
                return; //if script
            }
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
            getAppsList(ref myapps, true); //true=get appx also

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
            foreach (var app in myapps)
            {
                if (app.appx_name_match(ref argslist))
                {
                    if (remove_package(app.FullName))
                    {
                        ret--;
                    }
                }
            }
            Exit(ret);
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
            if (argslist.Count() == 1)
            {
                Help(ref argslist);
                return; //if script
            }
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "tzutil.exe",
                Arguments = "/s \"" + argslist[0] + "\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
            {
                Exit(1);
                return; //if script
            }

            process.WaitForExit();
            TimeZoneInfo.ClearCachedData();
            Exit(process.ExitCode);
        }

        static void Wallpaper(ref List<string> argslist)
        {
            if (argslist.Count() == 1)
            {
                Help(ref argslist);
                return; //if script
            }
            string fil = argslist[1];
            bool is_dir = false;
            if (fil.ToLower() == "bing")
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
                    return; //if script
                }
                fil = files[rand.Next(files.Length)];
            }
            //0=fail, non-zero=success   
            if (0 == SystemParametersInfo(20, 0, Path.GetFullPath(fil), 2))
            {
                Exit(1);
                return; //if script
            }
            Exit(0);
        }

        static void bingpaper(ref string fil)
        {
            //no need to try if no internet
            if (!isInternetUp())
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
            string jpgname = bingfolder + "\\" + jpgurl.Split('/').Last();
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

        static void RegImport(ref List<string> argslist)
        {
            if (argslist.Count() == 1)
            {
                Help(ref argslist);
                return; //if script
            }
            bool du = false;
            string fil;
            if (argslist[1].ToLower() == "-defaultuser")
            {
                du = true;
                if (argslist.Count() == 2)
                {
                    Help(ref argslist);
                    return; //if script
                }
                fil = argslist[2];
            } else fil = argslist[1];

            if (!File.Exists(fil))
            {
                Error(fil, "file does not exist");
                return; //if script
            }
            if (du)
            {
                if (!isAdmin())
                {
                    ErrorAdmin();
                    return; //if script
                }
                regimport_du(fil); //failure there will exit
                Exit(0); //must have worked
                return; //if script
            }
            else
            { //normal import
                bool ret = regdo("import " + fil);
                if (!ret && !isAdmin())
                {
                    ErrorAdmin();
                    return; //if script
                }
                Exit(ret ? 0 : 1);
            }
        }

        static void regimport_du(string fil)
        {
            //get reg key where reg file was previously loaded
            //[HKEY_LOCAL_MACHINE\1\Software\Microsoft\Windows\CurrentVersion\Run]
            //would be HKEY_LOCAL_MACHINE\1 -> HKLM\1
            string lkey = null;
            foreach (string line in System.IO.File.ReadAllLines(fil))
            {
                //only get first one, assume all are the same
                if (line.StartsWith("[HKEY_LOCAL_MACHINE\\"))
                {
                    lkey = "HKLM";
                }
                else if (line.StartsWith("[HKEY_CURRENT_USER\\"))
                {
                    lkey = "HKU";
                }
                if (lkey == null) continue;
                string[] ss = line.Split('\\');
                if (ss.Count() > 2) lkey = lkey + "\\" + ss[1];
                else lkey = null;
                break;
            }
            if (lkey == null) Error("cannot decode reg file import location");

            string ntuserdat = sysdrive + @"\users\default\ntuser.dat";
            string ntuserbak = ntuserdat + ".original";
            //backup ntuser.dat -> ntuser.dat.original if not done already
            if (!File.Exists(ntuserbak))
            {
                try
                {
                    File.Copy(ntuserdat, ntuserbak);
                }
                catch (Exception)
                {
                    Error("could not backup default user ntuser.dat");
                }
            }

            //check if key already exists (cannot load into existing key)
            if (regdo("query " + lkey))
            {
                Error("reg file import location cannot be used");
            }

            //load ntuser.dat file to same location
            if (!regdo("load " + lkey + " " + ntuserdat))
            {
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

        static void Weather(ref List<string> argslist)
        {
            string fil = null;
            if (argslist.Count() > 1)
            {
                fil = argslist[1];
            }
            string up = Environment.GetEnvironmentVariable("userprofile");
            if (up == null)
            {
                Exit(1);
                return; //if script
            }
            //weather app folder
            string wxdir = up + @"\AppData\Local\Packages\microsoft.Bingweather_8wekyb3d8bbwe";
            //folders to create
            string[] fol = { "AC", "AppData", "LocalCache", "LocalState", "RoamingState", "Settings", "SystemAppData", "TempState" };
            //delete everything
            try { Directory.Delete(wxdir, recursive: true); }
            catch (IOException)
            {
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
            else
            {
                if (!File.Exists(fil))
                {
                    //just provide internal file data instead
                    System.IO.File.WriteAllBytes(wxdir + @"\Settings\settings.dat", AppZero.Properties.Resources.settings_dat);
                    //but still show error
                    Error(fil, "file does not exist");
                    return; //if script
                }
                File.Copy(fil, wxdir + @"\Settings\settings.dat");
            }
            Exit(0); //for now, should check above results
        }

        static void Shortcut(ref List<string> argslist)
        {
            if (argslist.Count() < 3)
            {
                Help(ref argslist);
                return; //if script
            }
            //name target [ -arg arguments ][ -wd workingdir ]
            string link = argslist[1] + ".lnk";
            try
            {
                //using iwr as 'File' name collision with IWshRuntimeLibrary
                //so- using iwr = IWshRuntimeLibrary; -at top of file
                var shell = new iwr.WshShell();
                var shortcut = shell.CreateShortcut(link) as iwr.IWshShortcut;
                shortcut.TargetPath = argslist[2];
                argslist.RemoveRange(0, 3);
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
            }
            catch
            {
                Error("creating shortcut failed");
                return; //if script
            }
            Exit(0);
        }

        static void HideLayoutXml(ref List<string> argslist)
        {
            if (!isAdmin())
            {
                ErrorAdmin();
                return; //if script
            }
            string xml = sysdrive + @"\users\default\appdata\local\microsoft\windows\shell\DefaultLayouts.xml";
            string xmlbak = xml + ".bak";
            if (File.Exists(xml))
            {
                try { File.Move(xml, xmlbak); }
                catch
                {
                    Error(xml.Split('\\').Last(), "Unable to rename");
                    return; //if script
                }
            }
            Exit(0);
        }

        static void ScriptFile(ref List<string> argslist)
        {
            if (argslist.Count() == 1) Help(ref argslist);
            string fil = argslist[1];
            if (!File.Exists(fil)) { 
                Error(fil, "script file not found");
            }

            FileInfo filsiz = new FileInfo(fil);
            if (filsiz.Length > 0xFFFF)
            {
                Error("file too large (>64k, so probably not a script file)");
                return; //not needed here, but do anyway
            };

            //add script-only options
            optionslist.Add(new Options("-delstartuplnk", DelStartupLnk));
            //other additional options will be handled here

            Console.WriteLine();
            Console.WriteLine($@"   {exe_name}   v{version}    2018@curtvm");
            Console.WriteLine();
            Console.WriteLine($@"   script : {fil.Split('\\').Last()}");
            Console.WriteLine();

            //prevent exits, help
            script_is_running = true;

            //each line separated into list of strings
            //just like cmdline arguments
            var scriptargs = new List<string>();

            //number of lines executed
            int cmd_count = 0;

            string[] lines = null;
            try{
                lines = File.ReadAllLines(fil);
            } catch(Exception){ 
                Error(fil, "cannot read file");
                return;
            }
            if(lines.Length == 0){ 
                Error(fil, "file is empty");
                return; //not needed, script not started so Error exits
            }

            for(int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0) continue;

                scriptargs.Clear();

                //split line into args
                string[] args = cmdlineArray(lines[i]);
                //check if somehow no results on non-empty line
                if (args.Length == 0) continue;
                //string[] -> scriptargs list
                scriptargs.AddRange(args);

                //maximize/minimize window
                if(scriptargs[0].ToLower() == "-maximize"){
                    Maximize();
                    continue;
                }
                if(scriptargs[0].ToLower() == "-minimize"){
                    Minimize();
                    continue;
                }
                //exit if not admin
                if(scriptargs[0].ToLower() == "-needadmin"){ 
                    if(!isAdmin()){ 
                        ErrorAdmin();
                        break;
                    }
                }

                //check -writefile filename
                if(scriptargs[0].ToLower() == "-writefile" && scriptargs.Count() == 2){
                    //replace any <stuff> with environment variables
                    if(!getEnvVar(ref scriptargs)){ 
                        continue; //if error the embedd lines will be skipped
                        //along with the -writefile end marker
                    }
                    writeFile(ref lines, scriptargs[1], ref i);
                    //i = second -writefile line, now will ++ in for loop
                    //and back to normal
                    continue;                    
                }
                //check -message
                if(scriptargs[0].ToLower() == "-message"){ 
                    Message(ref lines, ref i);
                    //i = second -message line, now will ++ in for loop
                    //and back to normal
                    continue;                    
                }

                //check -continue?
                if(scriptargs[0].ToLower() == "-continue?"){
                    Console.Write("   ");
                    string ans = Console.ReadLine();
                    if(ans.ToLower().StartsWith("y")) continue;
                    break;
                }

                //now check regular commands

                //check cmdline option against our optionslist
                var opt = optionslist.Find(x => x.Name == scriptargs[0].ToLower());

                //if cannot find any, skip to next line
                if (opt == null) continue;

                //ignore -scriptfile
                if (opt.Name == "-scriptfile") continue;

                //skip specific help, like ->  -listapps -help
                if (scriptargs.Count() > 1 && scriptargs[1].ToLower() == "-help")
                {
                    continue;
                }

                //inc cmd count
                cmd_count++;

                //replace any <stuff> with environment variables
                if(!getEnvVar(ref scriptargs)){ 
                    continue; //do not run if failed
                }

                //now call function
                opt.Handler(ref scriptargs);
            }

            //info
            Console.WriteLine();
            Console.WriteLine($@"   script : {fil.Split('\\').Last()} : {cmd_count} commands : {error_count} error{(error_count == 1 ? "" : "s")}");
            Console.WriteLine();

            //turn off script mode,let exit exit
            script_is_running = false;

            //done, will return number of errors
            Exit(error_count);
        }

        static string[] cmdlineArray(string cmdline)
        {
            //simple version - double quotes only
            //no nested quotes
            var lc = new List<char>();
            bool inquote = false;
            foreach (var ch in cmdline)
            {
                if (ch == '"')
                {
                    inquote = !inquote; //toggle
                    continue; //quotes not kept
                }
                //save non-quoted spaces as \n
                if (!inquote && ch == ' ') lc.Add('\n');
                else lc.Add(ch); //add to our list of chars
            }
            //list of chars to string, then split by \n, discard empties
            return string.Concat(lc)
                .Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        static void DelStartupLnk(ref List<string> argslist)
        {
            //option is exclusive to scriptfile
            int ret = 1;
            //delete any links to this exe in this userprofile's startup folder
            var sdir = Environment.GetEnvironmentVariable("userprofile");
            if (sdir == null) return;
            //"C:\\Users\\User1"
            sdir = sdir + @"\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup";
            foreach (var f in Directory.EnumerateFiles(sdir)
                .Where(n => n.ToLower().EndsWith("lnk"))) 
            {
                if (File.ReadAllText(f).Contains(exe_name)){
                    File.Delete(f);
                    ret = 0; //exit code 0, found at least one
                }
            }
            Exit(ret); //even though will not exit, it will record the exit code
        }

        static void writeFile(ref string[] lines, string fil, ref int idx)
        {
            //scripts only
            //lines[idx] is line with -writefile filename
            //so advance 1
            idx++;
            var idx_start = idx;
            for( ; idx < lines.Length; idx++)
            { 
                if(lines[idx].ToLower().StartsWith("-writefile"))
                {
                    if(idx - idx_start > 0)
                    { 
                        var w = new List<string>(lines).GetRange(idx_start, idx).ToArray();
                        try { 
                            File.WriteAllLines(fil, w); 
                        } catch(Exception) { 
                            Error(fil, "could not write to file"); 
                        }
                    } else { 
                        Error("no data to write");    
                    }
                    break;
                }  
            }
        }

        static int Message(ref string[] lines, ref int idx)
        {
            //scripts only
            //lines[idx] is line with -message
            //so advance 1
            idx++;
            var idx_start = idx;
            for( ; idx < lines.Length; idx++)
            { 
                if(lines[idx].ToLower().StartsWith("-message"))
                {
                    return idx;
                }
                Console.WriteLine(lines[idx]);
            }
            return idx;
        }

        static bool getEnvVar(ref List<string> scriptargs)
        {
            if(scriptargs.Count() == 1) return true; //nothing to do
            for(var i = 1;i < scriptargs.Count();i++)
            {
                Match m = Regex.Match(scriptargs[i],"<.*>");
                while(m.Success)
                {
                    var str = m.ToString();
                    var e = Environment.GetEnvironmentVariable(
                        str.Replace("<","").Replace(">","")
                        );
                    m = m.NextMatch();
                    if(e == null){
                        Error("bad environment variable substitution");
                        return false; //any failure bad
                    }
                    scriptargs[i] = scriptargs[i].Replace(str,e);
                }
            }
            return true;
        }

        static void CreateUser(ref List<string> argslist)
        {
            //-createuser [ -admin ] username [ password ]
            if(argslist.Count() < 2) { 
                Help(ref argslist);
                return;
            }             
            bool admingroup = false;
            if(argslist.Count() > 1 && argslist[1].ToLower() == "-admin"){ 
                admingroup = true;
                argslist.RemoveAt(1);
            }
            if(argslist.Count() == 1){
                Help(ref argslist);
                return;
            }
            if(!isAdmin())
            {
                ErrorAdmin();
                return;
            }
            //now 2 or more args
            string cmd = "user /add " + argslist[1];
            if(argslist.Count() > 2){
                cmd = cmd + " " + argslist[2];
            }
            if(!netdo(cmd)){
                Error("add user failed");
                return; 
            }
            if(!admingroup) return;
            if(!netdo("localgroup administrators /add " + argslist[1])){
                Error("add to administrators group failed");
            }
        }

        static bool netdo(string cmd)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "net.exe",
                Arguments = cmd,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process == null) { return false; }
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        static string getSerialNumber()
        { 
            Process p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "wmic.exe";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Arguments = "bios get SerialNumber";
            p.Start();
            if(p == null) return null;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            if(output != null && output.StartsWith("SerialNumber")){ 
                return output.Split(new char[]{'\r','\n',' '}, 
                    System.StringSplitOptions.RemoveEmptyEntries)[1];
            }
            return null;
        }

        static void RenamePC(ref List<string> argslist)
        { 
            if(argslist.Count() < 2){ 
                Help(ref argslist);
                return;
            }
            string newname = argslist[1];
            if(argslist.Count() > 2 && argslist[2].ToLower() == "-s#"){ 
                string s = getSerialNumber();
                if(s == null)
                {
                    Error("could not get pc serial number");
                    return;
                }
                newname = newname + s;
            }

            //-renamepc newname [ -s# ]
            string oldname = Environment.GetEnvironmentVariable("computername");
            if(oldname == null){ 
                Error("could not get current computer name");
                return;
            }
            if(!isAdmin()){ 
                ErrorAdmin();
                return;
            }
            //WMIC ComputerSystem where Name="%computername%" call Rename Name="%newpcname%"            
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "wmic.exe",
                Arguments = "ComputerSystem where Name=" + oldname +
                    " call Rename Name=" + newname,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (p == null) {
                Error("failed to rename pc");
                return;
            }
            p.WaitForExit();
            if(p.ExitCode != 0){ 
                Error("failed to rename pc");    
            }
        }
    }
}


/*
            Maximize();
 
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(exe_name);

            Console.ReadKey();

[DllImport("user32.dll", SetLastError = true)]
static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

public static bool WindowsLogOff() {
  return ExitWindowsEx(0, 0); // (4,0) force logout

('wmic bios get SerialNumber /format:csv') 
 WMIC ComputerSystem where Name="%computername%" call Rename Name="%newpcname%"

}
     
     */