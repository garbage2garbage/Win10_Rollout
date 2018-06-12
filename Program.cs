using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;

namespace ConsoleApp1
{
    class Program
    {
        enum PinArg { NONE, UNPIN, UNPINTASKBAR, UNPINALL, PIN, LIST, RESTORE };

        static void help()
        {
            string this_exe = Environment.GetCommandLineArgs()[0].Split('\\').Last();
            Console.WriteLine(
                "   \r\n" + 
                "   " + this_exe  +  "\r\n\r\n" +
                "   options:\r\n\r\n" +
                "   -list           list all pinnable apps\r\n" +
                "\r\n" +
                "                   [ ] = not pinned\r\n" +
                "                   [S] = Start menu pinned\r\n" +
                "                   [T] = Taskbar pinned\r\n" +
                "                   [B] = Both start menu and taskbar pinned\r\n" +
                "\r\n" +
                "   -pin            appname1 [ appname2 \"app name 3\" ... ]\r\n" +
                "   -unpin          appname1 [ appname2 \"app name 3\" ... ]\r\n" +
                "   -unpintaskbar   appname1 [ appname2 \"app name 3\" ... ]\r\n" +
                "   -unpinall       unpin all (unpinnable) apps currently pinned to start menu and taskbar\r\n" +
                "   -restore        restore saved list\r\n" +
                "                     " + this_exe +" -list > saved.txt\r\n" +
                "                     " + this_exe + " -restore < saved.txt  (not powershell)\r\n" +
                "                     type saved.txt | " + this_exe + " -restore\r\n"
                );
        }

        static void restore_list(ref List<string> ls)
        {
            Console.SetIn(new StreamReader(Console.OpenStandardInput(8192))); // This will allow input >256 chars
            while (Console.In.Peek() != -1)
            {
                string line = Console.In.ReadLine();
                if (line.StartsWith("[B] ") || line.StartsWith("[S] ")) {
                    ls.Add(line.Substring(4).ToLower());
                };
            }
        }


        static void Main(string[] args)
        {
            bool listonly = (args.Length == 0);

            PinArg p = PinArg.NONE;
            
            var appslist = new List<string>();

            foreach (string str in args)
            {
                string s = str.ToLower();
                switch (s) {
                    case "-unpin":          p = PinArg.UNPIN;           break;
                    case "-list":           p = PinArg.LIST;            break;
                    case "-unpinall":       p = PinArg.UNPINALL;        break;
                    case "-unpintaskbar":   p = PinArg.UNPINTASKBAR;    break;
                    case "-pin":            p = PinArg.PIN;             break;
                    case "-restore":        p = PinArg.RESTORE;         break;
                    default:                appslist.Add(s);            break;
                }
            }

            if (p == PinArg.RESTORE) {
                restore_list(ref appslist);
                p = PinArg.PIN; //now same as -pin
            }

            string[] apps = appslist.ToArray();

            if (p == PinArg.NONE || apps.Length == 0 && p != PinArg.UNPINALL && p != PinArg.LIST) {
                help();
                Environment.Exit(1);
            }

            Type t = Type.GetTypeFromProgID("Shell.Application");
            dynamic shell = Activator.CreateInstance(t);
            var appobj = shell.NameSpace("shell:AppsFolder");

            //File Explorer needs plan B as will not unpin from AppsFolder namespace
            //use another namespace- is in start menu places- use that
            //env:programdata, hard coded here to c:\programdata
            bool file_explorer_tbpinned = false;
            var feobj = shell.NameSpace("C:\\ProgramData\\Microsoft\\Windows\\Start Menu Places");
            foreach (var v in feobj.Items())
            {
                if (v.path.ToString().Contains("File Explorer"))
                {
                    foreach (var vv in v.Verbs())
                    {
                        if (vv.Name.ToString() == "Unpin from tas&kbar")
                        {
                            file_explorer_tbpinned = true;
                            if (apps.Contains("file explorer") && p == PinArg.UNPINTASKBAR || p == PinArg.UNPINALL)
                            {
                                vv.DoIt();
                            }
                        }
                    }
                }
            }
            

            foreach (var item in appobj.Items())
            {
                string nam = item.Name.ToString();
                bool is_in_apps = apps.Contains(nam.ToLower());
                bool is_pinned = false;
                bool is_tb_pinned = false;

                foreach (var v in item.Verbs())
                {
                    string n = v.Name.ToString();
                    if(n == "Un&pin from Start") {
                        is_pinned = true;
                        if (p == PinArg.UNPIN && is_in_apps || p == PinArg.UNPINALL) {
                            v.DoIt();
                        }
                    }
                    if(n == "&Pin to Start") {
                        if(is_in_apps && p == PinArg.PIN) {
                            v.DoIt();
                        }
                    }
                    if(n == "Unpin from tas&kbar") {
                        is_tb_pinned = true;
                        if(p == PinArg.UNPINTASKBAR && is_in_apps || p == PinArg.UNPINALL) {
                            v.DoIt();
                        }
                    }                    
                }

                if(nam == "File Explorer" && file_explorer_tbpinned) { is_tb_pinned = true; }

                if (p == PinArg.LIST){
                    string s = "[ ] ";
                    if (is_tb_pinned && is_pinned) { s = "[B] "; }
                    else if (is_pinned) { s = "[S] ";  }
                    else if (is_tb_pinned) { s = "[T] "; }
                    Console.WriteLine(s + nam);
                }

            }
        }
    }
}
