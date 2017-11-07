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
        [ThreadStatic] private static string lastHandlerArgs;
        public static string LastHandlerArgs => lastHandlerArgs;

        public static string DefaultRootFileName { get; private set; }

        public static HashSet<string> WebHostRootFileNames { get; private set; }
        private static string WebHostPhysicalPath;
        private static IHttpHandler DefaultHttpHandler;
        private static IHttpHandler ForbiddenHttpHandler;
        private static IHttpHandler NotFoundHttpHandler;
        private static readonly IHttpHandler StaticFilesHandler = new StaticFileHandler();
        private static bool HostAutoRedirectsDirs;

        internal static void Init()
        {
            var appHost = HostContext.AppHost;

            WebHostRootFileNames = Env.IsWindows
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            WebHostPhysicalPath = appHost.VirtualFileSources.RootDirectory.RealPath;
            HostAutoRedirectsDirs = HostContext.IsAspNetHost && !Env.IsMono;

            //Apache+mod_mono treats path="servicestack*" as path="*" so takes over root path, so we need to serve matching resources
            var hostedAtRootPath = appHost.Config.HandlerFactoryPath.IsNullOrEmpty();

#if !NETSTANDARD2_0
            //DefaultHttpHandler not supported in IntegratedPipeline mode
            if (!Platform.IsIntegratedPipeline && HostContext.IsAspNetHost && !hostedAtRootPath && !Env.IsMono)
                DefaultHttpHandler = new DefaultHttpHandler();
#endif
            var rootFiles = appHost.VirtualFileSources.GetRootFiles().ToList();
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
                WebHostRootFileNames.Add(file.Name);
            }

            foreach (var dir in appHost.VirtualFileSources.GetRootDirectories())
            {
                WebHostRootFileNames.Add(dir.Name);
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
            var debugDefaultHandler = defaultRedirectHanlder != null ? defaultRedirectHanlder.RelativeUrl : null;

            ForbiddenHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.Forbidden) ?? new ForbiddenHttpHandler
            {
                IsIntegratedPipeline = Platform.IsIntegratedPipeline,
                WebHostPhysicalPath = WebHostPhysicalPath,
                WebHostUrl = appHost.Config.WebHostUrl,
                DefaultRootFileName = DefaultRootFileName,
                DefaultHandler = debugDefaultHandler,
            };
            
            NotFoundHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.NotFound) ?? new NotFoundHttpHandler
            {
                IsIntegratedPipeline = Platform.IsIntegratedPipeline,
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
            var opeationName = HostContext.AppHost.Config.StripApplicationVirtualPath? url.TrimPrefixes(context.Request.ApplicationPath) : url;
            var httpReq = new ServiceStack.Host.AspNet.AspNetRequest(httpContext, opeationName) { PhysicalPath = pathTranslated };
            return GetHandler(httpReq);
        }
#endif

        // Entry point for HttpListener and .NET Core
        public static IHttpHandler GetHandler(IHttpRequest httpReq)
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
            lastHandlerArgs = httpReq.Verb + "|" + httpReq.RawUrl + "|" + physicalPath;

            //Default Request /
            if (pathInfo.IsNullOrEmpty() || pathInfo == "/")
            {
                //If the fallback route can handle it, let it
                var restPath = appHost.Config.FallbackRestPath?.Invoke(httpReq);
                if (restPath != null)
                {
                    httpReq.SetRoute(restPath);
                    return new RestHandler { RequestName = restPath.RequestType.GetOperationName() };
                }
                
                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(httpReq.HttpMethod, pathInfo, httpReq.GetPhysicalPath());
                if (catchAllHandler != null) return catchAllHandler;

                return DefaultHttpHandler ?? NotFoundHttpHandler;
            }
            return GetHandlerForPathInfo(httpReq, physicalPath) ?? NotFoundHttpHandler;
        }

        public static bool ShouldAllow(string filePath)
        {
            if (filePath.IsNullOrEmpty() || filePath == "/")
                return true;

            foreach (var path in HostContext.AppHost.Config.ForbiddenPaths)
            {
                if (filePath.StartsWith(path))
                    return false;
            }

            var parts = filePath.SplitOnLast('.');
            if (parts.Length == 1 || parts[1].IsNullOrEmpty())
                return false;

            var fileExt = parts[1];
            if (HostContext.AppHost.Config.AllowFileExtensions.Contains(fileExt))
                return true;

            foreach (var pathGlob in HostContext.AppHost.Config.AllowFilePaths)
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
            var isFile = httpReq.IsFile();
            var isDirectory = httpReq.IsDirectory();

            if (!isFile && !isDirectory && Env.IsMono)
                isDirectory = StaticFileHandler.MonoDirectoryExists(filePath, filePath.Substring(0, filePath.Length - pathInfo.Length));

            var httpMethod = httpReq.Verb;

            var pathParts = pathInfo.TrimStart('/').Split('/');
            if (pathParts.Length == 0) return NotFoundHttpHandler;

            string contentType = null;
            var restPath = RestHandler.FindMatchingRestPath(httpReq, out contentType);
            if (restPath != null)
                return new RestHandler { RequestName = restPath.RequestType.GetOperationName() };

            if (isFile || isDirectory)
            {
                //If pathInfo is for Directory try again with redirect including '/' suffix
                if (appHost.Config.RedirectDirectoriesToTrailingSlashes && isDirectory && !httpReq.OriginalPathInfo.EndsWith("/"))
                    return new RedirectHttpHandler { RelativeUrl = pathInfo + "/" };

                var catchAllHandler = GetCatchAllHandlerIfAny(httpMethod, pathInfo, filePath);
                if (catchAllHandler != null)
                    return catchAllHandler;

                return isDirectory || ShouldAllow(pathInfo)
                    ? StaticFilesHandler
                    : ForbiddenHttpHandler;
            }

            var handler = GetCatchAllHandlerIfAny(httpMethod, pathInfo, filePath);
            if (handler != null) return handler;

            restPath = appHost.Config.FallbackRestPath?.Invoke(httpReq);
            if (restPath != null)
            {
                httpReq.SetRoute(restPath);
                return new RestHandler { RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
            }
            
            return null;
        }

        private static IHttpHandler GetCatchAllHandlerIfAny(string httpMethod, string pathInfo, string filePath)
        {
            foreach (var httpHandlerResolver in HostContext.CatchAllHandlers)
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