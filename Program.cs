using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;


//all strings- get to lowercase when need to make comparisons

namespace ConsoleApp1
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
                "   " + this_exe + "                          2018 @ curtvm" +  NL +
                NL +
                "   options:" + NL +
                NL +
                "   -list           list all pinnable apps" + NL +
                NL +
                "                   [ ] = not pinned" + NL +
                "                   [S] = Start menu pinned" + NL +
                "                   [T] = Taskbar pinned" + NL +
                "                   [B] = Both start menu and taskbar pinned" + NL +
                NL +
                "                   to save a list to a file-" + NL +
                "                     " + this_exe +" -list > saved.txt" + NL +
                NL +
                "   -pin            appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "   -unpin          appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "   -unpintaskbar   appname1 [ appname2 \"app name 3\" ... ]" + NL +
                "   -unpinall       unpin all (unpinnable) apps from the start menu and taskbar" + NL +
                "   -restore        to restore saved list from a file-" + NL +
                "                     type saved.txt | " + this_exe + " -restore" + NL
                );

            Environment.Exit(1);
        }

        //read stdin lines, add to apps list if [B] or [S] app
        //fill list with lowercase
        //will efectively be same as command line provided list
        static void restore_list(ref List<string> ls)
        {
            // This will allow input >256 chars
            Console.SetIn(new StreamReader(Console.OpenStandardInput(8192)));
            while (Console.In.Peek() != -1) {
                string line = Console.In.ReadLine();
                if (line.StartsWith("[B] ") || line.StartsWith("[S] ")) {
                    ls.Add(line.Substring(4).ToLower());
                };
            }
        }


        static void Main(string[] args)
        {
            PinArg p = PinArg.NONE;
            var appslist = new List<string>();
            //all lowercase and '&' removed- comparisons will be the same
            string unpin_str = "unpin from start";
            string unpintb_str = "unpin from taskbar";
            string pin_str = "pin to start";

            //get command line options
            //first option
            switch (args[0].ToLower()) {
                case "-unpin":          p = PinArg.UNPIN;           break;
                case "-list":           p = PinArg.LIST;            break;
                case "-unpinall":       p = PinArg.UNPINALL;        break;
                case "-unpintaskbar":   p = PinArg.UNPINTASKBAR;    break;
                case "-pin":            p = PinArg.PIN;             break;
                case "-restore":        p = PinArg.RESTORE;         break;
                default:                usage();                    break;
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
            if(p == PinArg.RESTORE) {
                restore_list(ref appslist);
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
                string nam = item.Name.ToString().ToLower();
                bool is_in_apps = apps.Contains(nam);
                bool is_pinned = false;
                bool is_tb_pinned = false;

                foreach (var v in item.Verbs()) {
                    switch (v.Name.ToString().ToLower().Replace("&",""){
                        case unpin_str:
                            is_pinned = true;
                            if (p == PinArg.UNPIN && is_in_apps || p == PinArg.UNPINALL) { v.DoIt(); }
                            break;
                        case pin_str:
                            if(is_in_apps && p == PinArg.PIN) { v.DoIt(); }
                            break;
                        case unpintb_str:
                            is_tb_pinned = true;
                            if(p == PinArg.UNPINTASKBAR && is_in_apps || p == PinArg.UNPINALL) { v.DoIt(); }
                            break;
                    }
                }

                //for file explorer- use previous taskbar info instead
                if(nam == "file explorer" && file_explorer_tbpinned) { is_tb_pinned = true; }

                if (p == PinArg.LIST){
                    string s = "[ ] ";                              //not pinned
                    if (is_tb_pinned && is_pinned) { s = "[B] "; }  //pinned start and taskbar
                    else if (is_pinned) { s = "[S] ";  }            //pinned start
                    else if (is_tb_pinned) { s = "[T] "; }          //pinned taskbar
                    Console.WriteLine(s + nam);
                }
            }
        }
    }
}
