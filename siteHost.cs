using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace siteHost
{  
     public class SiteOptions
    {   
        public Func<string, byte[], byte[]> ResolveCustomResource { get; set; }
        public SiteOptions()
        {
        //Here is where options can go into the constructor..empty for now

        }
    }
    
    public class Site
    {
        
        static readonly Task EmptyTask = Task.FromResult(0);

        readonly RequestDelegate next;
        
        readonly SiteOptions options;

          
    public Site(RequestDelegate next, SiteOptions options)
        {    
            this.next = next;
            this.options = options;
        }

    public Task Invoke(HttpContext httpContext)
        {
            // reference embedded resouces
            const string prefix = "siteHost.SwaggerUI.";

            var path = httpContext.Request.Path.Value.Trim('/');
            if (path == "") path = "index.html";
            var filePath = prefix + path.Replace("/", ".");
            var mediaType = GetMIMEType(filePath);
       
            var myAssembly = typeof(Site).GetTypeInfo().Assembly;

            //just the list of files in the assembly, only used for debugging
            string[] names = myAssembly.GetManifestResourceNames();

            using (var stream = myAssembly.GetManifestResourceStream(filePath))
            {
                if (options.ResolveCustomResource == null)
                {
                    if (stream == null)
                    {
                        // not found, standard request.
                        return next(httpContext);
                    }

                    httpContext.Response.Headers["Content-Type"] = new[] { mediaType };
                    httpContext.Response.StatusCode = 200;
                    var response = httpContext.Response.Body;
                    stream.CopyTo(response);
                }
                else
                {
                    byte[] bytes;
                    if (stream == null)
                    {
                        bytes = options.ResolveCustomResource(path, null);
                    }
                    else
                    {
                        using (var ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            bytes = options.ResolveCustomResource(path, ms.ToArray());
                        }
                    }

                    if (bytes == null)
                    {
                        // not found, standard request.
                        return next(httpContext);
                    }

                    httpContext.Response.Headers["Content-Type"] = new[] { mediaType };
                    httpContext.Response.StatusCode = 200;
                    var response = httpContext.Response.Body;
                    response.Write(bytes, 0, bytes.Length);
                }
            }

            return EmptyTask;
        }
        
        static string GetMIMEType(string path)
        {
            var extension = path.Split('.').Last();

            switch (extension)
            {
                case "css":
                    return "text/css";
                case "js":
                    return "text/javascript";
                case "json":
                    return "application/json";
                case "gif":
                    return "image/gif";
                case "png":
                    return "image/png";
                case "eot":
                    return "application/vnd.ms-fontobject";
                case "woff":
                    return "application/font-woff";
                case "woff2":
                    return "application/font-woff2";
                case "otf":
                    return "application/font-sfnt";
                case "ttf":
                    return "application/font-sfnt";
                case "svg":
                    return "image/svg+xml";
                case "ico":
                    return "image/x-icon";
                default:
                    return "text/html";
            }
        }
    }

    public static class SiteExtensions
    {
        public static IApplicationBuilder UseSite(this IApplicationBuilder app, SiteOptions options)
        {
            return app.UseMiddleware<Site>(options);
        }

    }

    
}
