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

        static void usage()
        {
            string this_exe = Environment.GetCommandLineArgs()[0].Split('\\').Last();
            string NL = "\r\n";

            Console.WriteLine(
                NL +
                "   options:        " + this_exe + "              2018@curtvm" + NL +
                NL +
                "   -list           list all pinnable apps" + NL +
                NL +
                "                   [ ] = not pinned" + NL +
                "                   [S] = Start menu pinned" + NL +
                "                   [T] = Taskbar pinned" + NL +
                "                   [B] = Both start menu and taskbar pinned" + NL +
                NL +
                "                   to save a list to a file-" + NL +
                "                   c:\\>" + this_exe + " -list > saved.txt" + NL +
                NL +
                "   -pin            appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "   -unpin          appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "   -unpintaskbar   appname1 [ appname2 \"app name 3\" ... ]" + NL +
                NL +
                "   -unpinall       unpin all (unpinnable) apps from the start menu and taskbar" + NL +
                NL +
                "   -restore        to restore from a -list saved file-" + NL +
                "                   c:\\>" + this_exe + " -restore saved.txt" + NL
                );

            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            PinArg p = PinArg.NONE;
            var appslist = new List<string>();
            //all lowercase and '&' removed- comparisons will be the same
            const string unpin_str = "unpin from start";
            const string unpintb_str = "unpin from taskbar";
            const string pin_str = "pin to start";

            //get command line options
            //check first option
            if (args.Count() == 0) {
                usage();
            } else {
                switch (args[0].ToLower()) {
                    case "-unpin":          p = PinArg.UNPIN;           break;
                    case "-list":           p = PinArg.LIST;            break;
                    case "-unpinall":       p = PinArg.UNPINALL;        break;
                    case "-unpintaskbar":   p = PinArg.UNPINTASKBAR;    break;
                    case "-pin":            p = PinArg.PIN;             break;
                    case "-restore":        p = PinArg.RESTORE;         break;
                    default:                usage();                    break;
                }
            }

            //get command line app name options if option requires it
            if (p == PinArg.UNPIN || p == PinArg.UNPINTASKBAR || p == PinArg.PIN) {
                foreach (string str in args.Skip(1)) {
                    appslist.Add(str.ToLower());
                }
            }

            //if -restore, fill in list from stdin
            //then set p to same as -pin
            //(now is effectively same as -pin with command line list)
            if (p == PinArg.RESTORE) {
                //check if file name not provided or file not found
                if (args.Count() < 2 || !File.Exists(args[1])) { usage(); } 
                //read file lines, add to apps list if [B] or [S] app (same as -list format)
                //convert all to lowercase
                //will efectively be same as command line provided list
                foreach (string line in System.IO.File.ReadAllLines(args[1])) {
                    if (line.StartsWith("[B] ") || line.StartsWith("[S] ")) {
                        appslist.Add(line.Substring(4).ToLower());
                    };
                }
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
            //use another namespace- use start menu places
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
                    switch (v.Name.ToString().ToLower().Replace("&", "")){
                        case unpin_str:
                            is_pinned = true;
                            if (p == PinArg.UNPIN && is_in_apps || p == PinArg.UNPINALL) { v.DoIt(); }
                            break;
                        case pin_str:
                            if (p == PinArg.PIN && is_in_apps) { v.DoIt(); }
                            break;
                        case unpintb_str:
                            is_tb_pinned = true;
                            if (p == PinArg.UNPINTASKBAR && is_in_apps || p == PinArg.UNPINALL) { v.DoIt(); }
                            break;
                    }
                }

                //for file explorer- use previous taskbar info instead
                if (nam == "file explorer" && file_explorer_tbpinned) {
                    is_tb_pinned = true;
                }

                if (p == PinArg.LIST) {
                    string s = "[ ] ";                              //not pinned
                    if (is_tb_pinned && is_pinned) { s = "[B] "; }  //pinned start and taskbar
                    else if (is_pinned) { s = "[S] "; }             //pinned start
                    else if (is_tb_pinned) { s = "[T] "; }          //pinned taskbar
                    Console.WriteLine(s + Nam);
                }
            }
        }
    }
}