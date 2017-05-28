using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Web.Http;
using FileLogger.Services;
using FileLoggerHost.Properties;
using log4net;
using ServiceHost;

namespace FileLoggerHost.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class FileLogController : ApiController, IApiKeyController
    {
        /// <summary>
        /// 
        /// </summary>
        public string ApiKey { get; set; }
        // ReSharper disable once UnusedMember.Local
        private static readonly ILog Log = LogManager.GetLogger("FileLogController");
        private static readonly Logger Logger = new Logger(new DirectoryInfo(Settings.Default.StorageLocation));

        /// <summary>
        ///     Health Check...
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("health")]
        [NoClientCache]
        public IHttpActionResult HealthCheck()
        {
            return Ok(string.Format("\r\nVersion: {0} Polling: {1}mins, Uptime Hours: {2}, Logged: {3}",
                Assembly.GetExecutingAssembly().GetName().Version,
                FileLoggerInfo.PollingMinutes > 0 ? FileLoggerInfo.PollingMinutes.ToString() : "DISABLED",
                Math.Round((DateTime.UtcNow - FileLoggerInfo.StartDateTime).TotalHours, 2),
                FileLoggerInfo.FilesLogged));
        }

        /// <summary>
        /// Search for Files
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("files/search")]
        [NoClientCache]
        [LogRequest]
        public IHttpActionResult Search(FormDataCollection formData)
        {
            var searchTxt = formData.Get("searchTxt");
            return Ok(Logger.Search(searchTxt));                
        }

        /// <summary>
        /// Search for Files
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("files/search/{searchText}")]
        [NoClientCache]
        [LogRequest]
        public IHttpActionResult Search(string searchText)
        {
            return Ok(Logger.Search(searchText));
        }

        /// <summary>
        /// Search for Files
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("files/search")]
        [NoClientCache]
        [LogRequest]
        public IHttpActionResult Search()
        {
            return Ok(Logger.Search(""));
        }

        /// <summary>
        /// Log a file...  aka Upload it
        /// </summary>
        [HttpPost]
        [Route("files")]
        [NoClientCache]
        [LogRequest]
        public IHttpActionResult UploadFile()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return StatusCode(HttpStatusCode.UnsupportedMediaType);
            }

            var filesReadToProvider = Request.Content.ReadAsMultipartAsync().Result;

            string source = null;
            string name = null;
            string fileName = null;
            var minutes = 0;
            var fileBytes = new byte[0];
            foreach (var stream in filesReadToProvider.Contents)
            {
                if (stream.Headers.ContentDisposition.DispositionType.Equals("form-data"))
                {
                    switch (stream.Headers.ContentDisposition.Name.Replace("\"", "").ToLower())
                    {
                        case "name":
                            name = stream.ReadAsStringAsync().Result;
                            break;
                        case "source":
                            source = stream.ReadAsStringAsync().Result;
                            break;
                        case "minutestolive":
                            if (!int.TryParse(stream.ReadAsStringAsync().Result, out minutes))
                            {
                                return Content(HttpStatusCode.BadRequest,
                                    "This end point needs a file, source, name and minutesToLive (int)");
                            }
                            break;
                        case "file":
                            fileBytes = stream.ReadAsByteArrayAsync().Result;
                            fileName = stream.Headers.ContentDisposition.FileName;
                            break;
                    }
                }
            }
            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(fileName))
            {
                name = fileName;
            }

            if (name == null || source == null)
            {
                return BadRequest("This end point needs a file, source, name and minutesToLive (int)");
            }
            name = name.Replace("\"", "");
            Logger.LogStream(source, name, new MemoryStream(fileBytes),
                minutes <= 0 ? (TimeSpan?)null : new TimeSpan(0, minutes, 0)
                );

            Interlocked.Increment(ref FileLoggerInfo.FilesLogged);

            return Content(HttpStatusCode.OK, "File Stored");
        }

        /// <summary>
        /// Download a file based on the id
        /// </summary>
        /// <param name="fileId"></param>
        [HttpGet]
        [Route("files/{fileId}")]
        [NoClientCache]
        [LogRequest]
        public IHttpActionResult Download(Guid fileId)
        {
            return DownloadFile(fileId);
        }

        private IHttpActionResult DownloadFile(Guid fileId)
        {
            var file = Logger.GetFile(fileId);
            if (file == null)
            {
                return BadRequest("Unable to locate file. Maybe it expired?");
            }

            if (file.File.Exists)
            {
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                var bytes = File.ReadAllBytes(file.File.FullName);

                result.Content = new ByteArrayContent(bytes);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = file.Name
                };
                return ResponseMessage(result);
            }

            return BadRequest("Unable to download file.");            
        }
    }
}
