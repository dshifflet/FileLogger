using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileLoggerClient.Tests
{
    [TestClass]
    [DeploymentItem("testfile.MSG")]
    [DeploymentItem("Microsoft.Owin.Host.HttpListener.dll")]
    [DeploymentItem("Microsoft.Owin.Host.HttpListener.xml")]
    public class ClientTests
    {
        [TestMethod]
        public void CanLogFile()
        {
            var fi = new FileInfo("testfile.MSG");
            FileLogger.LogFile("TEST", fi.Name, 15, fi);
        }
    }
}
