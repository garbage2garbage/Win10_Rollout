using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace ConsoleApp
{
    class Program
    {
        enum PinArg { NONE, UNPIN, UNPINTASKBAR, UNPINALL, PIN, LIST, RESTORE };
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
                "   -restore        to restore from a list saved file-" + NL +
                "                   c:\\>" + this_exe + " -restore saved.txt" + NL +
                NL +
                "   -pin            appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "   -unpin          | one or more space separated app name(s)" + NL +
                "   -unpintaskbar   | app names with spaces need to be quoted" + NL +

                //"   -pin            appname1 [ appname2 \"app name 3\" ... ]" + NL +
                //"   -unpin          appname1 [ appname2 \"app name 3\" ... ]" + NL +
                //"   -unpintaskbar   appname1 [ appname2 \"app name 3\" ... ]" + NL +
                NL +
                "   -unpinall       unpin all apps from the start menu and taskbar" + NL +
                "                   (placeholders/suggested apps will not be removed)" + NL
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
            if (args.Count() == 0) {
                usage();
            } else {
                switch (args[0].ToLower()) {
                    case "-unpin": p = PinArg.UNPIN; break;
                    case "-list": p = PinArg.LIST; break;
                    case "-unpinall": p = PinArg.UNPINALL; break;
                    case "-unpintaskbar": p = PinArg.UNPINTASKBAR; break;
                    case "-pin": p = PinArg.PIN; break;
                    case "-restore": p = PinArg.RESTORE; break;
                    //invalid arg
                    default: usage(); break;
                }
            }

            //get command line app name options if option requires it
            if (p == PinArg.UNPIN || p == PinArg.UNPINTASKBAR || p == PinArg.PIN) {
                foreach (string str in args.Skip(1)) {
                    appslist.Add(str.ToLower().Trim());
                }
            }

            //if -restore, fill in list from file name in arg[1]
            //then set p to same as -pin
            //(ends up effectively same as -pin with command line list)
            if (p == PinArg.RESTORE) {
                //check if file name not provided
                if (args.Count() < 2) { usage(); }
                //or file not found
                if (!File.Exists(args[1])) {
                    Console.WriteLine(args[1] + " -> file not found");
                    Environment.Exit(1);
                }
                //read file lines, add to apps list if [B] or [S] app (same as -list format)
                //convert all to lowercase
                //will be same as command line provided list
                foreach (string line in System.IO.File.ReadAllLines(args[1])) {
                    if (line.Count() > 4 && line.StartsWith("[B] ") || line.StartsWith("[S] ")) {
                        appslist.Add(line.Substring(4).ToLower().Trim());
                    };
                }
                //now same as -pin option
                p = PinArg.PIN;
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
                "  [ ] = not pinned" + NL +
                "  [S] = Start menu pinned" + NL +
                "  [T] = Taskbar pinned" + NL +
                "  [B] = Both start menu and taskbar pinned" + NL +
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
                    string s = "[ ] ";                              //not pinned
                    if (is_tb_pinned && is_pinned) { s = "[B] "; }  //pinned start and taskbar
                    else if (is_pinned) { s = "[S] "; }             //pinned start
                    else if (is_tb_pinned) { s = "[T] "; }          //pinned taskbar
                    Console.WriteLine(s + Nam);
                }
            }
            if (p == PinArg.LIST) { Console.WriteLine(""); }
        }
    }
}
