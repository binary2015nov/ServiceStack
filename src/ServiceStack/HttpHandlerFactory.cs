using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
using ServiceStack.Web;

namespace ServiceStack
{
    public class HttpHandlerFactory : IHttpHandlerFactory
    {
        private static string WebHostPhysicalPath;
        public static string DefaultRootFileName { get; private set; }

        private static IHttpHandler DefaultHttpHandler;
        private static IHttpHandler ForbiddenHttpHandler;
        private static IHttpHandler NotFoundHttpHandler;
        private static readonly IHttpHandler StaticFilesHandler = new StaticFileHandler();

        [ThreadStatic]
        public static string LastHandlerArgs;

        internal static void Init()
        {
            var appHost = HostContext.AppHost;

            WebHostPhysicalPath = appHost.VirtualFileSources.RootDirectory.RealPath;

            //Apache+mod_mono treats path="servicestack*" as path="*" so takes over root path, so we need to serve matching resources
            var hostedAtRootPath = appHost.Config.HandlerFactoryPath.IsNullOrEmpty();

//#if !NETSTANDARD2_0
//            //DefaultHttpHandler not supported in IntegratedPipeline mode
//            if (!Platform.IsIntegratedPipeline && HostContext.IsAspNetHost && !hostedAtRootPath && !Env.IsMono)
//                DefaultHttpHandler = new DefaultHttpHandler();
//#endif
            var rootFiles = appHost.VirtualFileSources.GetRootFiles();
            DefaultRootFileName = null;
            foreach (var file in rootFiles)
            {
                var fileNameLower = file.Name.ToLowerInvariant();
                if (DefaultRootFileName == null && appHost.Config.DefaultDocuments.Contains(fileNameLower))
                {
                    //Can't serve Default.aspx pages so ignore and allow for next default document
                    if (!fileNameLower.EndsWith(".aspx"))
                    {
                        DefaultRootFileName = file.Name;
                        StaticFileHandler.SetDefaultFile(file.VirtualPath, file.ReadAllBytes(), file.LastModified);

                        if (DefaultHttpHandler == null)
                            DefaultHttpHandler = new StaticFileHandler(file);
                    }
                }
            }

            if (!appHost.Config.DefaultRedirectPath.IsNullOrEmpty())
                DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = appHost.Config.DefaultRedirectPath };

            var metadataHandler = new RedirectHttpHandler();
            if (!appHost.Config.MetadataRedirectPath.IsNullOrEmpty())
                metadataHandler.RelativeUrl = appHost.Config.MetadataRedirectPath;
            else
                metadataHandler.RelativeUrl = "metadata";
            if (hostedAtRootPath)
            {
                if (DefaultHttpHandler == null)
                    DefaultHttpHandler = metadataHandler;
            }
            else
            {
                DefaultHttpHandler = metadataHandler;
            }

            var defaultRedirectHanlder = DefaultHttpHandler as RedirectHttpHandler;
            var debugDefaultHandler = defaultRedirectHanlder?.RelativeUrl;

            ForbiddenHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.Forbidden) ?? new ForbiddenHttpHandler
            {
                WebHostPhysicalPath = WebHostPhysicalPath,
                WebHostUrl = appHost.Config.WebHostUrl,
                DefaultRootFileName = DefaultRootFileName,
                DefaultHandler = debugDefaultHandler,
            };
            
            NotFoundHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.NotFound) ?? new NotFoundHttpHandler
            {
                WebHostPhysicalPath = WebHostPhysicalPath,
                WebHostUrl = appHost.Config.WebHostUrl,
                DefaultRootFileName = DefaultRootFileName,
                DefaultHandler = debugDefaultHandler,
            };
            
        }

#if !NETSTANDARD2_0
        // Entry point for ASP.NET
        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            var httpContext = context.Request.RequestContext.HttpContext;
            var httpReq = new ServiceStack.Host.AspNet.AspNetRequest(httpContext, url.SanitizedVirtualPath()) { PhysicalPath = pathTranslated };
            return GetHandler(httpReq);
        }
#endif

        // Entry point for HttpListener and .NET Core
        public static IServiceStackHandler GetHandler(IHttpRequest httpReq)
        {
            var appHost = HostContext.AppHost;
            foreach (var rawHttpHandler in appHost.RawHttpHandlers)
            {
                var handler = rawHttpHandler(httpReq);
                if (handler != null)
                    return handler;
            }

            string location = appHost.Config.HandlerFactoryPath;
            var pathInfo = httpReq.PathInfo;
            string physicalPath = httpReq.PhysicalPath;
            LastHandlerArgs = httpReq.Verb + "|" + httpReq.RawUrl + "|" + physicalPath;

            //Default Request /
            if (pathInfo == "/")
            {
                //If the fallback route can handle it, let it
                var restPath = appHost.Config.FallbackRestPath?.Invoke(httpReq);
                if (restPath != null)
                {
                    httpReq.SetRoute(restPath);
                    return new RestHandler { RequestName = restPath.RequestType.GetOperationName() };
                }
                
                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(httpReq.HttpMethod, pathInfo, physicalPath);
                if (catchAllHandler != null) return catchAllHandler;

                return DefaultHttpHandler ?? NotFoundHttpHandler;
            }
            return GetHandlerForPathInfo(httpReq, physicalPath) ?? NotFoundHttpHandler;
        }

        public static bool ShouldAllow(string filePath)
        {
            if (filePath.IsNullOrEmpty() || filePath == "/")
                return true;

            foreach (var path in HostContext.Config.ForbiddenPaths)
            {
                if (filePath.StartsWith(path))
                    return false;
            }

            var parts = filePath.SplitOnLast('.');
            if (parts.Length == 1 || parts[1].IsNullOrEmpty())
                return false;

            var fileExt = parts[1];
            if (HostContext.Config.AllowFileExtensions.Contains(fileExt))
                return true;

            foreach (var pathGlob in HostContext.Config.AllowFilePaths)
            {
                if (filePath.GlobPath(pathGlob))
                    return true;
            }
            return false;
        }

        public static IHttpHandler GetHandlerForPathInfo(IHttpRequest httpReq, string filePath)
        {
            var appHost = HostContext.AppHost;

            var pathInfo = httpReq.PathInfo;
            var httpMethod = httpReq.Verb;

            var pathParts = pathInfo.TrimStart('/').Split('/');
            if (pathParts.Length == 0) return NotFoundHttpHandler;

            var restPath = RestHandler.FindMatchingRestPath(httpReq, out string contentType);
            if (restPath != null)
                return new RestHandler { RequestName = restPath.RequestType.GetOperationName() };

            var catchAllHandler = GetCatchAllHandlerIfAny(httpMethod, pathInfo, filePath);
            if (catchAllHandler != null)
                return catchAllHandler;

            var isFile = httpReq.IsFile();
            var isDirectory = httpReq.IsDirectory();

            if (!isFile && !isDirectory && Env.IsMono)
                isDirectory = StaticFileHandler.MonoDirectoryExists(filePath, filePath.Substring(0, filePath.Length - pathInfo.Length));

            if (isFile || isDirectory)
            {
                //If pathInfo is for Directory try again with redirect including '/' suffix
                if (appHost.Config.RedirectDirectoriesToTrailingSlashes && isDirectory && !httpReq.OriginalPathInfo.EndsWith("/"))
                    return new RedirectHttpHandler { RelativeUrl = pathInfo + "/" };
      
                return isDirectory || ShouldAllow(pathInfo)
                    ? StaticFilesHandler
                    : ForbiddenHttpHandler;
            }

            restPath = appHost.Config.FallbackRestPath?.Invoke(httpReq);
            if (restPath != null)
            {
                httpReq.SetRoute(restPath);
                return new RestHandler { RequestName = restPath.RequestType.GetOperationName() };
            }
            
            return null;
        }

        private static IHttpHandler GetCatchAllHandlerIfAny(string httpMethod, string pathInfo, string filePath)
        {
            foreach (var httpHandlerResolver in HostContext.AppHost.CatchAllHandlers)
            {
                var httpHandler = httpHandlerResolver(httpMethod, pathInfo, filePath);
                if (httpHandler != null)
                    return httpHandler;
            }

            return null;
        }

        public void ReleaseHandler(IHttpHandler handler) { }
    }
}