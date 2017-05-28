using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace FileLogger.Models
{
    public class LoggedFile
    {
        public Guid Id { get; set; }
        public string Source { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string SerializedLogFilePath { get; set; }

        public TimeSpan? TimeToLive
        {
            get
            {
                if (TimeSpanTicks == 0) return null;
                return (new TimeSpan(TimeSpanTicks));
            }
            set {
                TimeSpanTicks = value == null ? 0 : value.Value.Ticks;
            }
        }

        public long TimeSpanTicks { get; set; }
        public DateTime CreatedDateUtc { get; set; }

        public FileInfo File
        {
            get
            {
                return FilePath == null ? null : new FileInfo(FilePath);
            }
        }

        public FileInfo SerializedLogFile
        {
            get
            {
                return SerializedLogFilePath == null ? null :
                    new FileInfo(SerializedLogFilePath);
            }
        }

        public bool IsExpired
        {
            get
            {            
                if(TimeToLive==null) return false;
                return DateTime.UtcNow >= CreatedDateUtc + TimeToLive.Value;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private LoggedFile()
        {
            //So it can serialize
        }

        public LoggedFile(string source, string name, FileInfo file, 
            TimeSpan? timeToLive)
        {
            if (name==null) throw new ArgumentNullException("name");
            if (name == null) throw new ArgumentNullException("file");
            if (name == null) throw new ArgumentNullException("timeToLive");

            Id = Guid.NewGuid();
            Source = source;
            Name = name;
            FilePath = file.FullName;
            TimeToLive = timeToLive;
            CreatedDateUtc = DateTime.UtcNow;
        }

        public void Serialize(FileInfo file)
        {
            var serializer = 
                new XmlSerializer(typeof(LoggedFile));
            SerializedLogFilePath = file.FullName;
            
            using (var stream = file.OpenWrite())
            using (var xw = XmlWriter.Create(stream))
            {
                serializer.Serialize(xw, this);
            }            
        }

        public void DeleteFiles()
        {
            if (File.Exists) File.Delete();
            if (SerializedLogFile.Exists) SerializedLogFile.Delete();
            FilePath = null;
            SerializedLogFilePath = null;
        }

        public static LoggedFile Deserialize(FileInfo file)
        {
            var serializer = 
                new XmlSerializer(typeof(LoggedFile));
            using (var stream = file.OpenRead())
            using (var sr = new StreamReader(stream))
            {
                var result = (LoggedFile) serializer.Deserialize(sr);
                result.SerializedLogFilePath = file.FullName;              
                return result;
            }
        }
    }
}
