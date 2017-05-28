using FileLoggerClient.Tests.Properties;
using FileLoggerHost;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileLoggerClient.Tests
{
    [TestClass]
    public class TestAssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            if (Settings.Default.SelfHost)
            {
                HostConfiguration.InTestMode = true;
                WebApp.Start<ConfigureHost>(url: Settings.Default.HostAddress);
            }
        }
    }
}
