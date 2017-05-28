using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using log4net;
#pragma warning disable 1591

namespace ServiceHost
{

    public interface IApiKeyController
    {
        string ApiKey { get; set; }
    }

    public class NoClientCacheAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var response = actionExecutedContext.Response;
            if (response != null)
            {
                response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            }
        }
    }

    public class ApiKeyAuthorizeAttribute : ActionFilterAttribute
    {
        private static readonly ILog Log = LogManager.GetLogger("LogRequest");

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            //this is the only easy way to do this...  Sucks...
            var controller = (IApiKeyController) actionContext.ControllerContext.Controller;
            var apikey = controller.ApiKey;

            IEnumerable<string> headers;
            if (actionContext.Request.Headers.TryGetValues("API_KEY", out headers))
            {
                if (headers.Any(header => header.Equals(apikey, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }
            }
            //check the properties...
            //have to be able to handle api keys from the url line...  This allows you to support ancient stuff.
            var querypairs = actionContext.Request.GetQueryNameValuePairs();
            if (querypairs.Where(o => o.Key.Equals("api_key", StringComparison.OrdinalIgnoreCase))
                .Any(pair => pair.Value.Equals(apikey, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            //Got nothing so let the client know this is Verboten.
            actionContext.Response = new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("Forbidden.  Please use the correct api key.")
            };
            //Now log it...
            if (Log.IsInfoEnabled)
            {
                Log.InfoFormat("{0} {1} {2} {3} (Forbidden)",
                    actionContext.Response.StatusCode,
                    actionContext.Request.Method,
                    actionContext.Request.RequestUri,
                    "Forbidden Request"
                    );
            }
        }
    }

    /// <summary>
    ///     This logs the in and out for HTTP requests
    /// </summary>
    public class LogRequestAttribute : ActionFilterAttribute
    {
        private static readonly ILog Log = LogManager.GetLogger("LogRequest");

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            // pre-processing
            if (Log.IsInfoEnabled)
            {
                Log.InfoFormat("{0} {1} (Executing)", actionContext.Request.Method, actionContext.Request.RequestUri);
            }
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Exception != null)
            {
                if (Log.IsInfoEnabled)
                {
                    Log.ErrorFormat("{0} {1} (EXCEPTION) {2}\r\n{3}",
                        actionExecutedContext.Request.Method,
                        actionExecutedContext.Request.RequestUri,
                        actionExecutedContext.Exception.Message,
                        actionExecutedContext.Exception);
                }
                return;
            }
            var objectContent = actionExecutedContext.Response.Content as ObjectContent;

            if (Log.IsDebugEnabled && objectContent != null)
            {
                Log.DebugFormat("{0} {1} {2} (Executed) Type:{3} Value: {4}",
                    actionExecutedContext.Response.StatusCode,
                    actionExecutedContext.Request.Method,
                    actionExecutedContext.Request.RequestUri,
                    objectContent.ObjectType,
                    objectContent.Value
                    );
            }

            if (Log.IsInfoEnabled)
            {
                var message = "";
                if (!actionExecutedContext.Response.IsSuccessStatusCode)
                {
                    //log the message...
                    message = actionExecutedContext.Response.Content.ReadAsStringAsync().Result;
                }
                Log.InfoFormat("{0} {1} {2} {3} (Executed)",
                    actionExecutedContext.Response.StatusCode,
                    actionExecutedContext.Request.Method,
                    actionExecutedContext.Request.RequestUri,
                    message
                    );
            }
        }
    }
}
