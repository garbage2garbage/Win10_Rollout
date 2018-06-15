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

     */
