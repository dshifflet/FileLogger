using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using Swashbuckle.Application;
using Swashbuckle.Swagger;

#pragma warning disable 1591

namespace ServiceHost
{
    public abstract class AbstractConfigureHost
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        /// <summary>
        /// Configure the WebAPI
        /// </summary>
        /// <param name="appBuilder"></param>
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host.
            var config = new HttpConfiguration();
            BasicConfiguration(appBuilder, config, "Insert Something Here", "v???");
        }

        protected void BasicConfiguration(IAppBuilder appBuilder, HttpConfiguration config, string name, string version)
        {
            BasicConfiguration(appBuilder, config, name, version, null, null);
        }

        protected void BasicConfiguration(IAppBuilder appBuilder, HttpConfiguration config, string name, string version,
            string staticLocalPath, string staticPublicPath)
        {
            if (Properties.Settings.Default.UseSwagger)
            {
                ConfigureSwagger(appBuilder, config, name, version);    
            }
            
            //turn on attribute routing...
            config.MapHttpAttributeRoutes();

            if (!string.IsNullOrEmpty(staticLocalPath))
            {
                var fs = new PhysicalFileSystem(staticLocalPath); //where the files are on the host drive.
                var options = new FileServerOptions
                {
                    FileSystem = fs,
                    RequestPath = new PathString(staticPublicPath) //the uri to access the files over http

                };
                appBuilder.UseFileServer(options);                
            }
            
            appBuilder.UseWebApi(config);
            config.EnsureInitialized();            
            Console.WriteLine("Configured Host");
        }
        
        protected void ConfigureSwagger(IAppBuilder appBuilder, HttpConfiguration config, string title, string version)
        {
            //turn on swagger for documentation

            //swagger needs the host url which is a pain to get...
            var addresses = ((List<IDictionary<string, object>>)appBuilder.Properties["host.Addresses"]).First();
            var path = addresses["path"].ToString();
            if (!string.IsNullOrEmpty(path) && path.EndsWith("/"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            //You need to turn on the XML documentation.  Project - Properties - Build - Output XML docs...
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var commentsFileName = Assembly.GetExecutingAssembly().GetName().Name + ".XML";
            var commentsFile = Path.Combine(baseDirectory, commentsFileName);

            config
                .EnableSwagger(c =>
                {
                    var applyApiKeySecurity = new ApplyApiKeySecurity(
                        key: "api_key",
                        name: "api_key",
                        description: "Api Key (Authorization)",
                        @in: "header"
                        );
                    applyApiKeySecurity.Apply(c);

                    c.RootUrl(req =>
                    {
                        var hosturl = string.Format("{0}://{1}:{2}{3}",
                            addresses["scheme"],
                            req.RequestUri.Host,
                            addresses["port"],
                            path);
                        return hosturl;
                    });

                    c.SingleApiVersion(version, title);
                    c.IncludeXmlComments(commentsFile);
                }
                ).EnableSwaggerUi();
        }
    }
    
    /// <summary>
    ///     Allows the API KEY to work with Swagger
    /// </summary>
    public class ApplyApiKeySecurity : IDocumentFilter, IOperationFilter
    {
        /// <summary>
        ///     Constructor for Swagger API Key Support
        /// </summary>
        /// <param name="key"></param>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="in"></param>
        public ApplyApiKeySecurity(string key, string name, string description, string @in)
        {
            Key = key;
            Name = name;
            Description = description;
            In = @in;
        }

        /// <summary>
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// </summary>
        public string In { get; private set; }

        /// <summary>
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// </summary>
        /// <param name="swaggerDoc"></param>
        /// <param name="schemaRegistry"></param>
        /// <param name="apiExplorer"></param>
        public void Apply(SwaggerDocument swaggerDoc, SchemaRegistry schemaRegistry, IApiExplorer apiExplorer)
        {
            IList<IDictionary<string, IEnumerable<string>>> security =
                new List<IDictionary<string, IEnumerable<string>>>();
            security.Add(new Dictionary<string, IEnumerable<string>>
            {
                {Key, new string[0]}
            });

            swaggerDoc.security = security;
        }

        /// <summary>
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="schemaRegistry"></param>
        /// <param name="apiDescription"></param>
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            operation.parameters = operation.parameters ?? new List<Parameter>();
            operation.parameters.Add(new Parameter
            {
                name = Name,
                description = Description,
                @in = In,
                required = true,
                type = "string"
            });
        }

        /// <summary>
        /// </summary>
        /// <param name="c"></param>
        public void Apply(SwaggerDocsConfig c)
        {
            c.ApiKey(Key)
                .Name(Name)
                .Description(Description)
                .In(In);
            c.DocumentFilter(() => this);
            c.OperationFilter(() => this);
        }
    }
}
