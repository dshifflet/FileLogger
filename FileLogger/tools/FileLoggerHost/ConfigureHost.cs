using System;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using ServiceHost;


namespace FileLoggerHost
{

    /// <summary>
    /// 
    /// </summary>
    public static class HostConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        public static bool InTestMode = false;
    }


    /// <summary>
    /// Configures the Host
    /// </summary>
    public class ConfigureStaticHost : AbstractConfigureHost
    {
        /// <summary>
        /// Host Configuration
        /// </summary>
        /// <param name="appBuilder"></param>
        public new void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host.
            var config = new HttpConfiguration();
            var staticLocalPath = Properties.Settings.Default.StaticFilesLocation;

            if (!string.IsNullOrEmpty(staticLocalPath))
            {
                var fs = new PhysicalFileSystem(staticLocalPath); //where the files are on the host drive.
                var options = new FileServerOptions
                {
                    FileSystem = fs,
                    RequestPath = new PathString("") //the uri to access the files over http
                };
                appBuilder.UseFileServer(options);
            }
            appBuilder.UseWebApi(config);
            config.EnsureInitialized();
            Console.WriteLine("Configured Static Host");
        }
    }

    /// <summary>
    /// Configure the WebAPI
    /// </summary>
    public class ConfigureHost : AbstractConfigureHost
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        /// <summary>
        /// Configure the WebAPI
        /// </summary>
        /// <param name="appBuilder"></param>
        public new void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host.
            var config = new HttpConfiguration();
            
            //JSON all the TIME!!!
            //var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes
            //    .FirstOrDefault(t => t.MediaType == "application/xml");
            //config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);
            if (HostConfiguration.InTestMode)
            {
                BasicConfiguration(appBuilder, config, "File Logger Service", "v1", null, null);    
            }
            else
            {                
                BasicConfiguration(appBuilder, config, "File Logger Service", "v1", Properties.Settings.Default.StaticFilesLocation, "/ui");    
                Console.WriteLine("Access the ui at '/ui");
            }
            
        }
    }
}
