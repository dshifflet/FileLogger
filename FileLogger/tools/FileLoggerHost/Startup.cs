using System;
using System.IO;
using System.Timers;
using FileLogger.Services;
using FileLoggerHost.Properties;
using log4net;
using log4net.Config;
using Microsoft.Owin.Hosting;
using ServiceHost;
using Timer = System.Timers.Timer;

#pragma warning disable 1591

[assembly: XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
namespace FileLoggerHost
{
    public static class FileLoggerInfo
    {
        public static DateTime StartDateTime;
        public static long FilesLogged;
        public static long FrequencyInSeconds;
        public static int PollingMinutes;

    }

    class Startup
    {
        // ReSharper disable once UnusedParameter.Local
        static void Main(string[] args)
        {
            Console.WriteLine(@"Starting");

            //Make sure the current directory is this directory
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            var settings = new SvcInstallerSettings("FileLoggerService",
                "Logs Files that are sent to it",
                "File Logger Service",
                null, //default user
                null, //default password
                true, //start automatically
                true); //basically prompt for the login or not
            
            var serviceToRun = new FileLoggerService(settings.ServiceName);
            var installer = new SvcInstaller();
            installer.StartUp(args, settings, serviceToRun);
        }
    }

    public class FileLoggerService : InteractiveServiceBase
    {
        private readonly Timer _timer = new Timer();
        
        private static readonly ILog Log = LogManager.GetLogger("FileLoggerService");
        public FileLoggerService(string serviceName)
        {
            ServiceName = serviceName;
        }

        public override void StartIt()
        {
            
            if (Settings.Default.PollingInMinutes > 0)
            {
                _timer.Interval = Settings.Default.PollingInMinutes * 60 * 1000;
                _timer.Elapsed += _timer_Elapsed;
                _timer.Start();                
            }
            
            FileLoggerInfo.PollingMinutes = Settings.Default.PollingInMinutes;
            FileLoggerInfo.StartDateTime = DateTime.UtcNow;
            Log.InfoFormat("Starting Host");
            try
            {
                WebApp.Start<ConfigureHost>(url: Settings.Default.HostAddress);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return;
            }
            Log.InfoFormat("Hosting at {0}", Settings.Default.HostAddress);
            Log.InfoFormat("Storing files at {0}", Settings.Default.StorageLocation);
        }

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            var logger = new Logger(new DirectoryInfo(Settings.Default.StorageLocation));
            logger.CleanFiles();
            _timer.Start();
        }
    }
}
