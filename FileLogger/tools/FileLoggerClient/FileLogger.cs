using System;
using System.IO;
using System.Net.Http;
using FileLoggerClient.Properties;

namespace FileLoggerClient
{
    public static class FileLogger
    {
        private static readonly HttpClient Client = new HttpClient();
        private static readonly bool Enabled;
        
        static FileLogger()
        {
            //todo
            //check for config setting
            var baseUri = Settings.Default.BaseUri;

            //check for environment variable
            //check for environment consul, find service
            //check for consul via dns, find service???  Or too dangerous?
            if (!string.IsNullOrEmpty(baseUri))
            {
                Enabled = true;
                Client.BaseAddress = new Uri(baseUri);    
            }                
        }

        public static void LogFile(string source, int timeToLiveMinutes, FileInfo fileInfo)
        {
            LogFile(source, fileInfo.Name, timeToLiveMinutes, fileInfo.OpenRead());
        }

        public static void LogFile(string source, string name, int timeToLiveMinutes, FileInfo fileInfo)
        {
            LogFile(source, name, timeToLiveMinutes, fileInfo.OpenRead());
        }
        
        public static void LogFile(string source, string name, int timeToLiveMinutes, Stream stream)
        {
            if (!Enabled) return;

            var form = new MultipartFormDataContent
            {
                {new StreamContent(stream), "\"file\"", "\"" + name + "\""},
                {new StringContent(source), "source"},
                {new StringContent(name), "name"},
                {new StringContent(timeToLiveMinutes.ToString()), "minutesToLive"}
            };
            
            //We don't care if it works or not it's a logger.  It shouldn't fail stuff just because someone else
            //can't configure the thing properly....
            try
            {
                Client.PostAsync(string.Format("{0}/files", Client.BaseAddress), form);
            }
            catch
            {
                //empty
            }
            
        }
    }
}
