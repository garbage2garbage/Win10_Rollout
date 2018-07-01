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
            Console.WriteLine(String.Format("  [{0}{1}{2}] {3}{4}",
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
            if (windir == null) windir = @"C:\Windows";
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
            optionslist.Add(new Options("-weather", Weather));
            optionslist.Add(new Options("-layoutxml", LayoutXml));
            optionslist.Add(new Options("-regimport", RegImport));

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
            string specific_str = null;
            //empty argslist = general help (short help for all options)
            //else specific help wanted with -help 
            // or error- correct option with incorrect suboptions- so give specifi help
            if (argslist.Count() > 0)
            {
                specific_str = argslist[0];
            }
            Console.WriteLine();
            Console.WriteLine($@"    {exe_name}   v{version}    2018@curtvm");
            Console.WriteLine();
            if (specific_str == null)
            {
                Console.WriteLine(AppOne.Properties.Resources.help);
                Exit(1);
            }
            foreach (var lin in AppOne.Properties.Resources.help.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
            {
                if(lin.Trim().StartsWith(argslist[0])) {
                    Console.WriteLine(lin);
                    Console.WriteLine();
                    break;
                }
            }           
            if (specific_str == "-listapps")
            {
                Console.WriteLine(AppOne.Properties.Resources.listapps_help);
            }
            if (specific_str == "-pinstart")
            {
                Console.WriteLine(AppOne.Properties.Resources.pinstart_help);
            }
            if (specific_str == "-unpinstart")
            {
                Console.WriteLine();
                Console.WriteLine($@"    unpin all apps or specified apps from start menu tiles");
                Console.WriteLine($@"    (placeholders/suggested apps in start menu will not be removed)");
                Console.WriteLine();
            }
            if (specific_str == "-unpintaskbar")
            {
                Console.WriteLine();
                Console.WriteLine($@"    unpin all apps or specified apps from taskbar");
                Console.WriteLine();
            }
            if (specific_str == "-removeappx")
            {
                Console.WriteLine(AppOne.Properties.Resources.removeappx_help);
            }
            if (specific_str == "-wallpaper")
            {
                Console.WriteLine(AppOne.Properties.Resources.wallpaper_help.Replace("{bingfolder}", bingfolder));
            }
            if (specific_str == "-shortcut")
            {
                Console.WriteLine(AppOne.Properties.Resources.shortcut_help.Replace("{exe_name}", exe_name));
            }
            if (specific_str == "-createuser")
            {
                Console.WriteLine(AppOne.Properties.Resources.createuser_help);
            }
            if (specific_str == "-renamepc")
            {
                Console.WriteLine(AppOne.Properties.Resources.renamepc_help);
            }
            if (specific_str == "-weather")
            {
                Console.WriteLine(AppOne.Properties.Resources.weather_help);
            }
            if (specific_str == "-layoutxml")
            {
                Console.WriteLine(AppOne.Properties.Resources.layoutxml_help);
            }
            if (specific_str == "-regimport")
            {
                Console.WriteLine(AppOne.Properties.Resources.regimport_help);
            }
            Exit(1);
        }

        static void Error(string filnam, string msg)
        {
            Console.WriteLine("   {0} : {1}", filnam, msg);
            Exit(1);
        }

        static void Error(string msg)
        {
            Console.WriteLine("   " + msg);
            Exit(1);
        }

        static void ErrorAdmin()
        {
            Error("need to run this command as Administrator");
        }


        //option functions

        static void ListApps(ref List<string> argslist)
        {
            //-listapps [ -savepinned ]
            var myapps = new List<Apps>();
            if (argslist.RemoveAll(x => x.ToLower() == "-savepinned") > 0) 
            {
                getAppsList(ref myapps);
                foreach(var app in myapps)
                { 
                    if(app.is_pinned()) Console.WriteLine(app.Name);
                }
                Exit(0);
            }

            Console.WriteLine();
            Console.WriteLine($@"    {exe_name} : apps list with status");
            Console.WriteLine($@"  ===============================================");
            Console.WriteLine($@"    [   ] = not pinned, regular app");
            Console.WriteLine($@"    [S  ] = Start menu pinned");
            Console.WriteLine($@"    [ T ] = Taskbar pinned");
            Console.WriteLine($@"    [  W] = Windows store app <appxname>");
            Console.WriteLine($@"  ===============================================");
            Console.WriteLine();
            getAppsList(ref myapps);

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
                if (!app.is_system()) app.ListPrint();
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
            if (argslist.RemoveAll(x => x.ToLower() == "-all") > 0)
            {
                var ma = new List<Apps>();
                getAppsList(ref ma);
                Console.Write("Unpin all start tiles...");
                foreach (var app in ma)
                {
                    if (app.unpin()) Console.Write("x");
                }
                Console.WriteLine(".DONE");
                Exit(0);
            }
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
            }
            argslist.RemoveAt(0);
            int maxlen = 0;
            foreach (var a in argslist)
            {
                if (a.Length > maxlen) maxlen = a.Length;
            }
            var myapps = new List<Apps>();
            getAppsList(ref myapps);
            int failed = 0;
            foreach (var app in argslist)
            {
                var idx = myapps.FindIndex(x => x.Name.ToLower() == app.ToLower());
                if (idx == -1)
                {
                    Console.WriteLine(@"{0}...not found", app.PadRight(maxlen, '.'));
                    failed++;
                }
                else
                {
                    if (myapps[idx].unpin()) Console.WriteLine(@"{0}...UNPINNED", app.PadRight(maxlen, '.'));
                    else Console.WriteLine(@"{0}...unchanged", app.PadRight(maxlen, '.'));
                }
            }
            Exit(failed);
        }

        static void UnpinTaskbar(ref List<string> argslist)
        {
            //-unpintaskbar -all | list ...
            if (argslist.RemoveAll(x => x.ToLower() == "-all") > 0)
            {
                var ma = new List<Apps>();
                getAppsList(ref ma);
                Console.Write("Unpin all apps on taskbar...");
                foreach (var app in ma)
                {
                    if (app.unpintb()) Console.Write("x");
                }
                Console.WriteLine(".DONE");
                Exit(0);
            }

            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
            }
            argslist.RemoveAt(0);
            int maxlen = 0;
            foreach (var a in argslist)
            {
                if (a.Length > maxlen) maxlen = a.Length;
            }
            var myapps = new List<Apps>();
            getAppsList(ref myapps);
            int failed = 0;
            foreach (var app in argslist)
            {
                var idx = myapps.FindIndex(x => x.Name.ToLower() == app.ToLower());
                if (idx == -1)
                {
                    Console.WriteLine(@"{0}...not found", app.PadRight(maxlen, '.'));
                    failed++;
                }
                else
                {
                    if (myapps[idx].unpintb()) Console.WriteLine(@"{0}...TBUNPINNED", app.PadRight(maxlen, '.'));
                    else Console.WriteLine(@"{0}...unchanged", app.PadRight(maxlen, '.'));
                }
            }
            Exit(failed);
        }

        static void PinStart(ref List<string> argslist)
        {
            //-pinstart filename | list ...
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
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
            int maxlen = 0;
            foreach (var a in argslist)
            {
                if (a.Length > maxlen) maxlen = a.Length;
            }
            var myapps = new List<Apps>();
            getAppsList(ref myapps);
            int failed = 0;
            foreach (var app in argslist)
            {
                var idx = myapps.FindIndex(x => x.Name.ToLower() == app.ToLower());
                if (idx == -1)
                {
                    Console.WriteLine(@"{0}...not found", app.PadRight(maxlen, '.'));
                    failed++;
                }
                else
                {
                    if (myapps[idx].pin()) Console.WriteLine(@"{0}...PINNED", app.PadRight(maxlen, '.'));
                    else Console.WriteLine(@"{0}...unchanged", app.PadRight(maxlen, '.'));
                }
            }
            Exit(failed);
        }

        static void RemoveAppx(ref List<string> argslist)
        {
            //-removeappx filename | list...
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
            }
            argslist.RemoveAt(0);
            string fil = argslist[0];
            if (argslist.Count() == 1 && File.Exists(fil))
            {
                argslist.Clear();
                foreach (string line in System.IO.File.ReadAllLines(fil))
                {
                    //no empty lines, no names with spaces
                    if (line.Length > 0 && !line.Trim().Contains(" "))
                    {
                        argslist.Add(line.Trim());
                    };
                }
            }
            int maxlen = 0;
            foreach (var a in argslist)
            {
                if (a.Length > maxlen) maxlen = a.Length;
            }
            var myappx = new List<Appx>();
            getAppxList(ref myappx);
            int failed = 0;
            foreach (var app in argslist)
            {
                //long name (if provided) to short name (compares are to short names)
                string ap = app.Split('_')[0];
                var idx = myappx.FindIndex(x => x.Name.ToLower() == ap.ToLower());
                //if (argslist.Contains(app.Name.ToLower()))
                if (idx == -1)
                {
                    Console.WriteLine(@"{0}...not found", ap.PadRight(maxlen, '.'));
                    failed++;
                }
                else
                {
                    Console.Write(@"{0}...", myappx[idx].Name.PadRight(maxlen, '.'));
                    if (!removePackage(myappx[idx].Name, myappx[idx].PackageFullName))
                    {
                        failed++;
                    }
                }
            }
            Exit(failed); //number of apps in list not uninstalled
        }

        static void Wallpaper(ref List<string> argslist)
        {
            //-wallpaper filename | foldername | -bing
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
            }
            string fil = argslist[1];
            bool is_dir = false;
            if (fil.ToLower() == "-bing")
            {
                string ret = bingpaper(ref fil);
                if(ret != null)
                {
                    Error(ret);
                    return;
                }
            }
            if (!File.Exists(fil) && !(is_dir = Directory.Exists(fil)))
            {
                Error(fil, "file or directory does not exist");
                return;
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
            fil = Path.GetFullPath(fil);
            Console.Write($@"setting wallpaper to {fil.Split('\\').Last()}...");
            //0=fail, non-zero=success 
            bool good = 0 != SystemParametersInfo(20, 0, fil, 3);
            if(good) Console.WriteLine("OK"); else { Console.WriteLine("failed"); }
            Exit(good ? 0 : 1);
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
            argslist.RemoveAt(0);
            //now has 1 or more args
            string username = argslist[0];
            string password = argslist.Count() > 1 ? " " + argslist[1] : null;
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

        static void Weather(ref List<string> argslist)
        {
            //-weather [ settings.dat ]
            string fil = argslist.Count() > 1 ? argslist[1] : null;
            if (fil != null && !File.Exists(fil))
            {
                Error(fil, "file does not exist");
                return;
            }
            if (userprofile == null)
            {
                Error("cannot get current user's profile folder");
                return;
            }
            //kill weather app to let us delete the folder/files
            Process[] p = Process.GetProcesses();                             
            foreach (var pw in p.Where(x => x.MainWindowTitle == "Weather"))
            {
                pw.Kill(); //found, kill
                Thread.Sleep(3000); //wait (seems to take a while)
            }
            //weather app folder
            string wxdir = $@"{userprofile}\AppData\Local\Packages\microsoft.Bingweather_8wekyb3d8bbwe";
            //delete everything (try up to 3 times)
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(wxdir, recursive: true);
                }
                catch (Exception)
                {
                    Thread.Sleep(1000);
                    continue; //try again
                }
                break; //it worked
            }
            //continue with whatever we have

            //folders to create
            string[] fol = { "AC", "AppData", "LocalCache", "LocalState",
                "RoamingState", "Settings", "SystemAppData", "TempState" };

            //create all folders
            foreach (string d in fol)
            {
                try
                {
                    Directory.CreateDirectory($@"{wxdir}\{d}");
                }
                catch (Exception)
                {
                    Console.WriteLine($@"{d} - failed");
                    //we are too far in, just keep going
                }
            }
            //if filename provided, try to copy
            if (fil != null)
            {
                try
                {
                    File.Copy(fil, $@"{wxdir}\Settings\settings.dat");
                    Console.WriteLine("Weather app set to settings provided");
                    Exit(0);
                }
                catch (Exception)
                {
                    Console.WriteLine(@"unable to copy settings file provided");
                    //try default settings below
                }

            }
            //either not fil, or fil failed, use defaults
            try
            {
                System.IO.File.WriteAllBytes(
                    $@"{wxdir}\Settings\settings.dat",
                    AppOne.Properties.Resources.settings_dat
                    );                
                Console.WriteLine(@"Weather app set to default settings");
                Exit(0);
            }
            catch
            {
                Console.WriteLine(@"unable to copy default settings");
            }
            Exit(1);
        }

        static void LayoutXml(ref List<string> argslist)
        {
            //-layoutxml -hide | -unhide
            bool hide = argslist.RemoveAll(x => x.ToLower() == "-hide") > 0;
            bool unhide = argslist.RemoveAll(x => x.ToLower() == "-unhide") > 0;
            if (hide == unhide)
            {
                Help(ref argslist);
                return;
            }
            if (!isAdmin())
            {
                ErrorAdmin();
                return;
            }
            string xml = "DefaultLayouts.xml";
            string xmlfull = $@"{sysdrive}\users\default\appdata\local\microsoft\windows\shell\{xml}";
            string xmlfullbak = $@"{xml}.bak";
            if (hide)
            {
                if (!File.Exists(xmlfull))
                {
                    if (File.Exists(xmlfullbak))
                    {
                        Console.WriteLine($@"{xml} is already renamed");
                        Exit(0);
                    }
                    else
                    {
                        Console.WriteLine($@"{xml} seems to be deleted");
                        Exit(0);
                    }
                }
                try
                {
                    File.Move(xmlfull, xmlfullbak);
                    Console.WriteLine($@"{xml} renamed to {xml}.bak");
                    Exit(0);
                }
                catch
                {
                    Error($@"unable to rename {xml}");
                    return;
                }
            }
            //unhide
            if (File.Exists(xmlfull))
            {
                Console.WriteLine($@"{xml} already unhidden");
                Exit(0);
            }
            //check if bak available
            if (File.Exists(xmlfullbak))
            {
                try
                {
                    File.Move(xmlfullbak, xmlfull);
                }
                catch
                {
                    Error($@"unable to restore {xml}");
                    return;
                }
            }
            //neither available
            Error($@"{xml} or {xml}.bak files not found, cannot unhide");
        }

        static void RegImport(ref List<string> argslist)
        {
            //-regimport [ -defaultuser ] filename
            bool du = argslist.RemoveAll(x => x.ToLower() == "-defaultuser") > 0;
            //-regimport filename
            if (argslist.Count() < 2)
            {
                Help(ref argslist);
                return;
            }
            string fil = argslist[1];
            if (!File.Exists(fil))
            {
                Error(fil, "file does not exist");
                return;
            }
            if (du)
            {
                if (!isAdmin())
                {
                    ErrorAdmin();
                    return;
                }
                string retstr = regimportDefaultuser(fil);
                if (retstr != null)
                {
                    Error(retstr);
                    return;
                }
                Console.WriteLine($@"imported reg file {fil} to default user");
                Exit(0);
            }
            //normal import
            if (!processDo("reg.exe", $@"import {fil}"))
            {
                string a = isAdmin() ? null : " (may need admin priveleges)";
                Error("failed to import registry file" + a);
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

        static string bingpaper(ref string fil)
        {
            fil = null; //caller can now test
            //no need to try if no internet
            if (!isInternetUp())
            {
                return "no internet connection to bing.com";
            }
            //get todays bing picture store in public pictures folder
            if (!Directory.Exists(bingfolder))
            {
                try { Directory.CreateDirectory(bingfolder); }
                catch (Exception) { return "cannot create Bing directory: " + bingfolder ; }
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
                return "unable to find jpg on Bing.com";
            }
            string filname = jpgurl.Split('/').Last().Replace("_1920x1080", "");
            string jpgname = $@"{bingfolder}\{filname}";
            //may have already downloaded, check
            if (!File.Exists(jpgname))
            {
                try { wc.DownloadFile(jpgurl, jpgname); } 
                catch (Exception) { return "failed to download " + filname; }
            }
            if (File.Exists(jpgname))
            {
                fil = jpgname;
            }
            return null;
        }

        static bool removePackage(string nam, string fullnam)
        {
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
                Console.WriteLine("failed");
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

        static string regimportDefaultuser(string fil)
        {
            //import into default user hive
            //has to be a HKEY_CURRENT_USER root reg file
            //[HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run]
            string tempkey = @"temp_load_key";
            string loadkey = $@"HKLM\{tempkey}";
            string[] lines = null;
            try
            {
                lines = File.ReadAllLines(fil);
                if (lines.Length == 0) return $@"{fil} is empty";
            }
            catch (Exception)
            {
                return $@"could not read file {fil}";
            }

            for (var i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Replace(@"[HKEY_CURRENT_USER", $@"[{loadkey}");
            }

            string ntuserdat = $@"{sysdrive}\users\default\ntuser.dat";
            string ntuserbak = $@"{ntuserdat}.bak";
            //backup ntuser.dat -> ntuser.dat.bak if not done already
            if (!File.Exists(ntuserbak))
            {
                try
                {
                    File.Copy(ntuserdat, ntuserbak);
                }
                catch (Exception)
                {
                    return "could not backup default user ntuser.dat";
                }
            }
            //check if key already exists (cannot load into existing key)
            if (processDo("reg.exe", $@"query {loadkey}"))
            {
                return $@"reg file import location {tempkey} cannot be used";
            }
            //load ntuser.dat file
            if (!processDo("reg.exe", $@"load {loadkey} {ntuserdat}"))
            {
                return "unable to load default user registry hive";
            }
            //write modified reg file to temp location
            fil = "_" + fil;
            try
            {
                File.WriteAllLines(fil, lines);
            }
            catch
            {
                return $@"cannot save temporary modified reg file";
            }

            //import reg file
            bool ret = processDo("reg.exe", $@"import {fil}");
            //unload ntuser.dat (if previous succeeded or not)
            ret &= processDo("reg.exe", $@"unload {loadkey}");
            //delete temp file
            File.Delete(fil);
            //ret == true if both succeeded
            if (!ret)
            {
                return "failed to import registry file";
            }
            return null;
        }

    }
}


/*
 
        //options removed

        //tzutil /s "Central Standard Time"

        //static void Timezone(ref List<string> argslist)
        //{
        //    //-timezone "timezonename"
        //    if (argslist.Count() < 2)
        //    {
        //        Help(ref argslist);
        //        return;
        //    }
        //    string tz = $@"""{argslist[1]}""";
        //    if (!processDo("tzutil.exe", $@"/s {tz}"))
        //    {
        //        Error($@"unable to set timezone to {tz}");
        //        return;
        //    }
        //    TimeZoneInfo.ClearCachedData();
        //    Console.WriteLine($@"timezone set to {tz}");
        //    Exit(0);
        //}     
     
*/
