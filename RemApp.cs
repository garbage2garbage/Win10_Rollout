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
                    Console.WriteLine("{0,-" + maxl + "}    {1}",item.Name,item.Path.ToString().Split('!')[0]);
                }
            }



            //Console.WriteLine(WindowsIdentity.GetCurrent().Name);

                PackageManager packageManager = new Windows.Management.Deployment.PackageManager();
            IEnumerable<Package> packages =
                packageManager.FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);

            var max_name_len = 0;
            foreach (Package package in packages)
            {
                if (package.Id.Name.Length > max_name_len)
                {
                    max_name_len = package.Id.Name.Length;
                }
            }

            foreach (Package package in packages)
            {
                DisplayPackageInfo(package, max_name_len);
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

        static void DisplayPackageInfo(Windows.ApplicationModel.Package package, int maxlen)
        {
            Console.WriteLine("{0,-" + maxlen + "}    {1}", package.Id.Name, package.Id.FamilyName);
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
