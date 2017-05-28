using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;
#pragma warning disable 1591

// ReSharper disable LocalizableElement

namespace ServiceHost
{

    public class InteractiveServiceBase : ServiceBase
    {
        public virtual void StartIt()
        {
        }
    }

    [RunInstaller(true)]
    public class SvcInstaller : Installer
    {

        public void StartUp(string[] args, SvcInstallerSettings settings, InteractiveServiceBase serviceToRun)
        {
            if (Environment.UserInteractive) //Debugging or executing from the command line
            {
                if (args.Any()) //installing or uninstalling
                {
                    ProcessArgs(args, settings);
                }
                else //interactive mode...
                {
                    var t = new Task(serviceToRun.StartIt);
                    t.Start();
                    Console.WriteLine("Hit Enter to Exit");
                    Console.ReadLine();
                }
            }
            else //Running as a service
            {
                ServiceBase.Run(serviceToRun); //Run the service
            }
        }

        public override string HelpText
        {
            get
            {
                var msg = "Please specify the following parameters:\r\n";
                msg += "ServiceName\r\n";
                msg += "Description\r\n";
                msg += "DisplayName\r\n";
                msg += "UserName\r\n";
                msg += "Password\r\n";
                msg += "AutomaticStart (true|false)\r\n";
                msg += "UseUserAccount (true|false)\r\n";
                return msg;
            }
        }

        public override void Install(IDictionary stateSaver)
        {
            if (Context != null && Context.Parameters != null)
            {
                try
                {
                    var settings = new SvcInstallerSettings(Context.Parameters);
                    RegisterService(settings);
                    stateSaver.Add("SvcName", settings.ServiceName);
                    base.Install(stateSaver);
                    return;
                }
                catch (Exception ex)
                {
                    var prevcolor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Message);
                    Console.ForegroundColor = prevcolor;
                }
            }
            Console.WriteLine("WARNING: Unable to install service.  Review previous exception.  If installing via installutil use '/?' for help.");
        }

        public override void Uninstall(IDictionary savedState)
        {
            if (savedState != null && savedState["SvcName"] != null)
            {
                var settings = new SvcInstallerSettings(savedState["SvcName"].ToString());
                RegisterService(settings);
                base.Uninstall(savedState);
            }
            else
            {
                Console.WriteLine("Unable to Uninstall. savedState is null or corrupted.");
                Console.WriteLine("If all else fails inspect the service and doing a 'sc delete <service-name>' from the command line.");
            }
        }

        private void RegisterService(SvcInstallerSettings settings)
        {
            if (string.IsNullOrEmpty(settings.ServiceName))
            {
                Console.WriteLine("Unable to locate ServiceName.");
                return;
            }

            var serviceProcessInstaller = new ServiceProcessInstaller();
            var serviceInstaller = new ServiceInstaller();

            //if you want a user name and password
            if (settings.UseUserAccount)
            {
                serviceProcessInstaller.Username = settings.UserName;
                serviceProcessInstaller.Password = settings.Password;
            }
            else
            {
                //or nothing
                serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            }

            serviceInstaller.StartType = settings.AutomaticStart ? ServiceStartMode.Automatic : ServiceStartMode.Manual;

            //these have to match it is the key!  The linkage...
            serviceInstaller.ServiceName = settings.ServiceName;
            serviceInstaller.Description = settings.Description;
            serviceInstaller.DisplayName = settings.DisplayName;

            Installers.AddRange(new Installer[] {
            serviceProcessInstaller,
            serviceInstaller});
        }

        public static void ProcessArgs(string[] args, SvcInstallerSettings settings)
        {
            if (args.Length > 1)
            {
                for (var x = 1; x < args.Length; x++)
                {
                    if (args[x].Equals("-user", StringComparison.OrdinalIgnoreCase))
                    {
                        if (x + 1 < args.Length &&
                            !args[x + 1].StartsWith("-"))
                        {
                            settings.UserName = args[x + 1];
                        }
                        settings.UseUserAccount = true;
                    }

                    if (args[x].Equals("-password", StringComparison.OrdinalIgnoreCase))
                    {
                        if (x + 1 < args.Length)
                        {
                            settings.Password = args[x + 1];
                            settings.UseUserAccount = true;
                        }
                        else
                        {
                            Console.WriteLine("When specifying a password a second parameter should follow that specifies the password");
                        }
                    }
                }
            }

            if (args[0].Equals("-install", StringComparison.OrdinalIgnoreCase))
            {
                Uninstall(settings.ServiceName);
                Install(settings);
                return;
            }
            if (args[0].Equals("-installstart", StringComparison.OrdinalIgnoreCase))
            {
                Uninstall(settings.ServiceName);
                Install(settings);
                Start(settings.ServiceName);
                return;
            }
            if (args[0].Equals("-uninstall", StringComparison.OrdinalIgnoreCase))
            {
                Uninstall(settings.ServiceName);
                return;
            }

            if (args[0].Equals("-stop", StringComparison.OrdinalIgnoreCase))
            {
                Stop(settings.ServiceName);
                return;
            }
            if (args[0].Equals("-start", StringComparison.OrdinalIgnoreCase))
            {
                Start(settings.ServiceName);
                return;
            }
            if (args[0].Equals("-restart", StringComparison.OrdinalIgnoreCase))
            {
                Stop(settings.ServiceName);
                Start(settings.ServiceName);
                return;
            }

            Console.WriteLine("I don't know what that argument means.  Sorry.  HAVE A NICE DAY! :)");
        }

        private static bool DoesServiceExist(string serviceName)
        {
            var svcs = ServiceController.GetServices();
            if (svcs.All(o => o.ServiceName != serviceName))
            {
                Console.WriteLine("Service {0} is not installed", serviceName);
                return false;
            }
            return true;
        }

        private static void Start(string serviceName)
        {
            if (!DoesServiceExist(serviceName)) return;

            var sc = new ServiceController(serviceName);
            if (sc.Status != ServiceControllerStatus.Stopped)
            {
                Console.WriteLine("Service is already {0}", sc.Status);
                return;
            }

            Console.WriteLine("Starting the Service {0}", serviceName);
            var service = new ServiceController(serviceName);
            service.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, new TimeSpan(0, 0, 60));

            Console.WriteLine("Service is currently {0}", sc.Status);
        }

        private static void Stop(string serviceName)
        {
            if (!DoesServiceExist(serviceName)) return;

            var sc = new ServiceController(serviceName);
            if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.Paused)
            {
                Console.WriteLine("Service is already {0}", sc.Status);
                return;
            }

            Console.WriteLine("Stopping the Service {0}", serviceName);
            var service = new ServiceController(serviceName);
            if (service.CanStop)
            {
                service.Stop();
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped, new TimeSpan(0, 0, 60));

            Console.WriteLine("Service is currently {0}", sc.Status);
        }

        private static void Install(SvcInstallerSettings settings)
        {
            Console.WriteLine("Installing the Service from {0}", Assembly.GetExecutingAssembly().Location);
            //context parameters don't seem to come across
            var args = new List<string>();
            args.AddRange(settings.ToParameters());
            args.Add(Assembly.GetExecutingAssembly().Location);
            ManagedInstallerClass.InstallHelper(args.ToArray());
        }

        private static void Uninstall(string serviceName)
        {
            if (!DoesServiceExist(serviceName)) return;
            Stop(serviceName);
            Console.WriteLine("Uninstalling the Service");
            ManagedInstallerClass.InstallHelper(new[]
                {
                    "/u",
                    Assembly.GetExecutingAssembly().Location
                });
        }
    }

    /// <summary>
    /// Change the install options in here...
    /// </summary>
    public class SvcInstallerSettings
    {
        public string ServiceName { get; private set; }
        public string Description { get; private set; }
        public string DisplayName { get; private set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool AutomaticStart { get; private set; }
        public bool UseUserAccount { get; set; }

        public SvcInstallerSettings(string serviceName)
        {
            ServiceName = serviceName;
        }

        public SvcInstallerSettings(string serviceName, string description,
            string displayName, string userName, string password, bool automaticStart, bool useUserAccount)
        {
            ServiceName = serviceName;
            Description = description;
            DisplayName = displayName;
            UserName = userName;
            Password = password;
            AutomaticStart = automaticStart;
            UseUserAccount = useUserAccount;
        }

        public SvcInstallerSettings(StringDictionary dict)
        {
            ServiceName = dict["ServiceName"];
            Description = dict["Description"];
            DisplayName = dict["DisplayName"];
            UserName = dict["UserName"];
            Password = dict["Password"];
            if (dict["AutomaticStart"].Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                AutomaticStart = true;
            }
            if (dict["UseUserAccount"].Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                UseUserAccount = true;
            }
        }

        public IEnumerable<string> ToParameters()
        {
            var r = new List<string>
            {
                string.Format("/ServiceName={0}", ServiceName),
                string.Format("/Description={0}", Description),
                string.Format("/DisplayName={0}", DisplayName),
                string.Format("/UserName={0}", UserName),
                string.Format("/Password={0}", Password),
                AutomaticStart ? "/AutomaticStart=true" : "/AutomaticStart=false",
                UseUserAccount ? "/UseUserAccount=true" : "/UseUserAccount=false"
            };
            return r;
        }
    }
}