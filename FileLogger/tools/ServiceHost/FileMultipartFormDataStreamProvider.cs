using System.Net.Http;
using System.Net.Http.Headers;

#pragma warning disable 1591

namespace ServiceHost
{
    //http://www.strathweb.com/2012/08/a-guide-to-asynchronous-file-uploads-in-asp-net-web-api-rtm/
    //This allows you to get the real name...

    public class FileMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public FileMultipartFormDataStreamProvider(string path)
            : base(path)
        {
        }

        public override string GetLocalFileName(HttpContentHeaders headers)
        {
            var name = !string.IsNullOrWhiteSpace(headers.ContentDisposition.FileName)
                ? headers.ContentDisposition.FileName
                : "NoName";
            return name.Replace("\"", string.Empty);
            //this is here because Chrome submits files in quotation marks which get treated as part of the filename and get escaped
        }
    }
}
