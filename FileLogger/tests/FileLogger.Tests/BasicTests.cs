using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FileLogger.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FileLogger.Tests
{
    [TestClass]
    [DeploymentItem("TestFile.txt")]
    public class BasicTests
    {
        private readonly Logger _logger = new Logger(new DirectoryInfo("./logfiles"));
        private readonly FileInfo _testFile = new FileInfo("TestFile.txt");

        [TestMethod]
        public void CanLogFile()
        {
            var test = _logger.LogFile("TEST", "testfile.txt", _testFile, null);
            Assert.IsTrue(test.File.Exists);
            Assert.IsTrue(test.SerializedLogFile.Exists);
            test.DeleteFiles();
            Assert.IsNull(test.File);
            Assert.IsNull(test.SerializedLogFile);
        }

        [TestMethod]
        public void CanExpireFiles()
        {
            var test = _logger.LogFile("TEST", "testfile.txt", _testFile, new TimeSpan(0,0,2));
            try
            {
                var watch = new Stopwatch();
                watch.Start();
                while (watch.Elapsed.Seconds < 3) { }
                Assert.IsTrue(test.IsExpired);
            }
            finally
            {
                test.DeleteFiles();    
            }            
        }

        [TestMethod]
        public void CanCleanFiles()
        {
            var test = _logger.LogFile("TEST", "testfile.txt", _testFile, new TimeSpan(0,0,2));
            Assert.IsTrue(test.File.Exists);
            var watch = new Stopwatch();
            watch.Start();
            while (watch.Elapsed.Seconds < 3) { }
            _logger.CleanFiles();
            Assert.IsFalse(test.File.Exists);
        }

        [TestMethod]
        public void CanSearchFiles()
        {
            var test = _logger.LogFile("TEST", "testfile.txt", _testFile, null);
            try
            {
                Assert.IsTrue(_logger.Search("test").Any());
                Assert.IsFalse(_logger.Search("DAVE").Any());
            }
            finally
            {
                test.DeleteFiles();
            }
        }

        [TestMethod]
        public void CanGetFile()
        {
            var check = _logger.LogFile("TEST", "testfile.txt", _testFile, null);
            try
            {
                var test = _logger.GetFile(check.Id);
                Assert.IsTrue(test.File.Exists);
            }
            finally
            {
                check.DeleteFiles();
            }
        }
    }
}
