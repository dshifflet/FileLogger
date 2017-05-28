using System;
using System.IO;
using FileLoggerClient;

namespace DirLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("<source> <directory> <ttl>");
                return;
            }
            var source = args[0];

            var di = new DirectoryInfo(args[1]);
            if (!di.Exists)
            {
                Console.WriteLine("Please specify a directory that exists");
            }
            int ttl;
            if (!int.TryParse(args[2], out ttl))
            {
                Console.WriteLine("Please specify an integer for TTL.");    
            }
            var idx = 0;
            foreach (var file in di.GetFiles())
            {
                FileLogger.LogFile(source, ttl, file);
                Console.WriteLine("Logged {0} ({1})", file.Name, ++idx);
            }
            Console.WriteLine("Waiting");
            Console.ReadLine();
        }
    }
}
