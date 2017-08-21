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

        public static HashSet<string> WebHostRootFileNames { get; private set; }
        private static string WebHostPhysicalPath;
        private static IHttpHandler DefaultHttpHandler;
        private static IHttpHandler NonRootModeDefaultHttpHandler;
        private static IHttpHandler ForbiddenHttpHandler;
        private static IHttpHandler NotFoundHttpHandler;
        private static readonly IHttpHandler StaticFilesHandler = new StaticFileHandler();
        private static bool HostAutoRedirectsDirs;
        protected static HostConfig AppHostConfig;

        internal static void Init()
        {
            var appHost = HostContext.AppHost;
            AppHostConfig = appHost.Config;

            WebHostRootFileNames = Env.IsWindows
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>();

            var isAspNetHost = HostContext.IsAspNetHost;
            WebHostPhysicalPath = appHost.VirtualFileSources.RootDirectory.RealPath;
            HostAutoRedirectsDirs = isAspNetHost && !Env.IsMono;

            //Apache+mod_mono treats path="servicestack*" as path="*" so takes over root path, so we need to serve matching resources
            var hostedAtRootPath = AppHostConfig.HandlerFactoryPath == null;

            //DefaultHttpHandler not supported in IntegratedPipeline mode
            if (!Platform.IsIntegratedPipeline && isAspNetHost && !hostedAtRootPath && !Env.IsMono)
                DefaultHttpHandler = new DefaultHttpHandler();

            var rootFiles = appHost.VirtualFileSources.GetRootFiles().ToList();
            string defaultRootFileName = null;
            foreach (var file in rootFiles)
            {
                var fileNameLower = file.Name.ToLowerInvariant();
                if (defaultRootFileName == null && AppHostConfig.DefaultDocuments.Contains(fileNameLower))
                {
                    //Can't serve Default.aspx pages so ignore and allow for next default document
                    if (!fileNameLower.EndsWith(".aspx"))
                    {
                        defaultRootFileName = file.Name;
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

            if (!AppHostConfig.DefaultRedirectPath.IsNullOrEmpty())
            {
                DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = AppHostConfig.DefaultRedirectPath };
                NonRootModeDefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = AppHostConfig.DefaultRedirectPath };
            }
            else
            {
                var metadataHandler = AppHostConfig.MetadataRedirectPath.IsNullOrEmpty()
                    ? new IndexPageHttpHandler() as IHttpHandler
                    : new RedirectHttpHandler { RelativeUrl = AppHostConfig.MetadataRedirectPath };
               
                if (hostedAtRootPath)
                {
                    if (DefaultHttpHandler == null)
                        DefaultHttpHandler = metadataHandler;
                }
                else
                {
                    NonRootModeDefaultHttpHandler = metadataHandler;
                }
            }

            var defaultRedirectHanlder = DefaultHttpHandler as RedirectHttpHandler;
            var debugDefaultHandler = defaultRedirectHanlder != null
                ? defaultRedirectHanlder.RelativeUrl
                : typeof(DefaultHttpHandler).GetOperationName();

            ForbiddenHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.Forbidden) ?? new ForbiddenHttpHandler
            {
                IsIntegratedPipeline = Platform.IsIntegratedPipeline,
                WebHostPhysicalPath = WebHostPhysicalPath,
                WebHostRootFileNames = WebHostRootFileNames,
                WebHostUrl = AppHostConfig.WebHostUrl,
                DefaultRootFileName = defaultRootFileName,
                DefaultHandler = debugDefaultHandler,
            };
            
            NotFoundHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.NotFound) ?? new NotFoundHttpHandler
            {
                IsIntegratedPipeline = Platform.IsIntegratedPipeline,
                WebHostPhysicalPath = WebHostPhysicalPath,
                WebHostRootFileNames = WebHostRootFileNames,
                WebHostUrl = AppHostConfig.WebHostUrl,
                DefaultRootFileName = defaultRootFileName,
                DefaultHandler = debugDefaultHandler,
            };          
        }

#if !NETSTANDARD1_6
        // Entry point for ASP.NET
        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            var httpContext = context.Request.RequestContext.HttpContext;
            var opeationName = AppHostConfig.StripApplicationVirtualPath? url.TrimPrefixes(context.Request.ApplicationPath) : url;
            var httpReq = new ServiceStack.Host.AspNet.AspNetRequest(httpContext, opeationName);
            return GetHandler(httpReq);
        }
#endif

        // Entry point for HttpListener
        public static IHttpHandler GetHandler(IHttpRequest httpReq)
        {
            var appHost = HostContext.AppHost;
            foreach (var rawHttpHandler in appHost.RawHttpHandlers)
            {
                var handler = rawHttpHandler(httpReq);
                if (handler != null) return handler;
            }

            string location = AppHostConfig.HandlerFactoryPath;
            string pathInfo = httpReq.PathInfo;
            string physicalPath = httpReq.PhysicalPath;
            lastHandlerArgs = httpReq.Verb + "|" + httpReq.RawUrl + "|" + physicalPath;

            //Default Request /
            if (pathInfo.IsNullOrEmpty() || pathInfo == "/")
            {
                //If the fallback route can handle it, let it
                if (AppHostConfig.FallbackRestPath != null)
                {
                    string contentType;
                    var sanitizedPath = RestHandler.GetSanitizedPathInfo(pathInfo, out contentType);

                    var restPath = AppHostConfig.FallbackRestPath(httpReq.HttpMethod, sanitizedPath, physicalPath);
                    if (restPath != null)
                    {
                        return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
                    }
                }

                //e.g. CatchAllHandler to Process Markdown files
                //var catchAllHandler = GetCatchAllHandlerIfAny(httpReq.HttpMethod, pathInfo, physicalPath);
                //if (catchAllHandler != null) return catchAllHandler;

                if (location.IsNullOrEmpty())
                    return DefaultHttpHandler;

                return NonRootModeDefaultHttpHandler;
            }
            return GetHandlerForPathInfo(httpReq.HttpMethod, pathInfo, pathInfo, physicalPath) ?? NotFoundHttpHandler;
        }

        // no handler registered 
        // serve the file from the filesystem, restricting to a safelist of extensions
        public static bool ShouldAllow(string filePath)
        {
            var parts = filePath.SplitOnLast('.');
            if (parts.Length == 1 || string.IsNullOrEmpty(parts[1]))
                return false;

            var fileExt = parts[1];
            if (AppHostConfig.AllowFileExtensions.Contains(fileExt))
                return true;

            foreach (var pathGlob in AppHostConfig.AllowFilePaths)
            {
                if (filePath.GlobPath(pathGlob))
                    return true;
            }
            return false;
        }

        public static IHttpHandler GetHandlerForPathInfo(string httpMethod, string pathInfo, string requestPath, string filePath)
        {
            var appHost = HostContext.AppHost;

            var pathParts = pathInfo.Trim('/').Split('/');
            if (pathParts.Length == 0) return NotFoundHttpHandler;

            string contentType;
            var restPath = RestHandler.FindMatchingRestPath(httpMethod, pathInfo, out contentType);
            if (restPath != null)
                return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
            var existingFile = pathParts[0];
            var matchesRootDirOrFile = appHost.Config.DebugMode
                ? appHost.VirtualFileSources.FileExists(existingFile) ||
                  appHost.VirtualFileSources.DirectoryExists(existingFile)
                : WebHostRootFileNames.Contains(existingFile);

            if (matchesRootDirOrFile)
            {
                var fileExt = System.IO.Path.GetExtension(filePath);
                var isFileRequest = !string.IsNullOrEmpty(fileExt);

                if (!isFileRequest && !HostAutoRedirectsDirs)
                {
                    //If pathInfo is for Directory try again with redirect including '/' suffix
                    if (!pathInfo.EndsWith("/"))
                    {
                        var appFilePath = filePath.Substring(0, filePath.Length - requestPath.Length);
                        var redirect = StaticFileHandler.DirectoryExists(filePath, appFilePath);
                        if (redirect)
                        {
                            return new RedirectHttpHandler
                            {
                                RelativeUrl = pathInfo + "/",
                            };
                        }
                    }
                }

                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(httpMethod, pathInfo, filePath);
                if (catchAllHandler != null) return catchAllHandler;

                if (!isFileRequest)
                {
                    return appHost.VirtualFileSources.DirectoryExists(pathInfo)
                        ? StaticFilesHandler
                        : NotFoundHttpHandler;
                }

                return ShouldAllow(requestPath)
                    ? StaticFilesHandler
                    : ForbiddenHttpHandler;
            }

            var handler = GetCatchAllHandlerIfAny(httpMethod, pathInfo, filePath);
            if (handler != null) return handler;

            if (appHost.Config.FallbackRestPath != null)
            {
                restPath = appHost.Config.FallbackRestPath(httpMethod, pathInfo, filePath);
                if (restPath != null)
                {
                    return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
                }
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

        public void ReleaseHandler(IHttpHandler handler)
        {

        }
    }
}