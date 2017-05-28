using System;
using System.Collections.Generic;
using System.IO;
using FileLogger.Models;

namespace FileLogger.Services
{
    public class Logger
    {
        private readonly DirectoryInfo _storagePath;

        public Logger(DirectoryInfo storagePath)
        {
            if(storagePath==null) throw new ArgumentNullException("storagePath");

            if (!storagePath.Exists)
            {                
                storagePath.Create();
                storagePath.Refresh();
            }

            _storagePath = storagePath;
        }

        public LoggedFile LogFile(string source, string name, FileInfo file, TimeSpan? timeToLive)
        {
            using (var stream = file.OpenRead())
            {
                return LogStream(source, name, stream, timeToLive);    
            }           
        }

        public LoggedFile LogStream(string source, string name, Stream stream, TimeSpan? timeToLive)
        {
            var guid = Guid.NewGuid();
            var file = NewFile(name, guid);
            
            using (var fs = file.Create())
            {
                stream.Seek(0, SeekOrigin.Begin);
                stream.CopyTo(fs);     
            }
            
            var logfile = new LoggedFile(source, name, file, timeToLive);
            
            logfile.Serialize(
                new FileInfo(
                    string.Format("{0}/{1}.loggedfile.xml"
                        , _storagePath.FullName, guid))
                );
            
            return logfile;
        }

        private FileInfo NewFile(string name, Guid guid)
        {
            //should prevent it from creating subdirectories needlessly.       
            return new FileInfo(
                string.Format("{0}/{1}_{2}"
                    , _storagePath.FullName, guid, name.Replace(@"\", "_").Replace(@"/", "_")));
        }

        public void CleanFiles()
        {
            foreach (var file in _storagePath.GetFiles("*.loggedfile.xml"))
            {
                var item = LoggedFile.Deserialize(file);
                if(item.IsExpired) item.DeleteFiles();
            }
        }


        public
            IEnumerable<LoggedFile> Search(string search)
        {
            var result = new List<LoggedFile>();
            foreach (var file in _storagePath.GetFiles("*.loggedfile.xml"))
            {
                var item = LoggedFile.Deserialize(file);
                
                if (item.IsExpired) continue;

                if (item.Source.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(item);
                    continue;
                }
                if (item.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        public LoggedFile GetFile(Guid id)
        {
            foreach (var file in _storagePath.GetFiles("*.loggedfile.xml"))
            {
                var item = LoggedFile.Deserialize(file);
                if (item.IsExpired) return null;
                if (item.Id.Equals(id)) return item;
            }
            return null;
        }

    }
}
