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
        public static readonly HashSet<string> WebHostRootFileNames = new HashSet<string>();
        private static string WebHostPhysicalPath = null;
        private static IHttpHandler DefaultHttpHandler = null;
        private static IHttpHandler NonRootModeDefaultHttpHandler = null;
        private static IHttpHandler ForbiddenHttpHandler = null;
        private static IHttpHandler NotFoundHttpHandler = null;
        private static readonly IHttpHandler StaticFilesHandler = new StaticFileHandler();
        private static bool IsIntegratedPipeline = false;
        private static bool HostAutoRedirectsDirs = false;

        [ThreadStatic] private static string debugLastHandlerArgs;
        public static string DebugLastHandlerArgs => debugLastHandlerArgs;

        internal static void Init()
        {
#if !NETSTANDARD1_6
            //MONO doesn't implement this property
            var pi = typeof(HttpRuntime).GetProperty("UsingIntegratedPipeline");
            if (pi != null)
            {
                IsIntegratedPipeline = (bool) pi.GetGetMethod().Invoke(null, TypeConstants.EmptyObjectArray);
            }
#endif
            var appHost = HostContext.AppHost;
            var config = appHost.Config;

            var isAspNetHost = HostContext.IsAspNetHost;
            WebHostPhysicalPath = appHost.VirtualFileSources.RootDirectory.RealPath;
            HostAutoRedirectsDirs = isAspNetHost && !Env.IsMono;

            //Apache+mod_mono treats path="servicestack*" as path="*" so takes over root path, so we need to serve matching resources
            var hostedAtRootPath = config.HandlerFactoryPath == null;

            //DefaultHttpHandler not supported in IntegratedPipeline mode
            if (!IsIntegratedPipeline && isAspNetHost && !hostedAtRootPath && !Env.IsMono)
                DefaultHttpHandler = new DefaultHttpHandler();

            var rootFiles = appHost.VirtualFileSources.GetRootFiles().ToList();
            string defaultRootFileName = null;
            foreach (var file in rootFiles)
            {
                var fileNameLower = file.Name.ToLowerInvariant();
                if (defaultRootFileName == null && config.DefaultDocuments.Contains(fileNameLower))
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

            if (!config.DefaultRedirectPath.IsNullOrEmpty())
            {
                DefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = config.DefaultRedirectPath };
                NonRootModeDefaultHttpHandler = new RedirectHttpHandler { RelativeUrl = config.DefaultRedirectPath };
            }
            else
            {
                IHttpHandler metadataHandler;
                if (config.MetadataRedirectPath.IsNullOrEmpty())
                {
                    metadataHandler = new IndexPageHttpHandler();
                }
                else
                {
                    metadataHandler = new RedirectHttpHandler { RelativeUrl = config.MetadataRedirectPath };
                }
                if (DefaultHttpHandler == null)
                    DefaultHttpHandler = hostedAtRootPath ? metadataHandler : NotFoundHttpHandler;
                NonRootModeDefaultHttpHandler = metadataHandler;
            }

            var defaultRedirectHanlder = DefaultHttpHandler as RedirectHttpHandler;
            var debugDefaultHandler = defaultRedirectHanlder != null
                ? defaultRedirectHanlder.RelativeUrl
                : typeof(DefaultHttpHandler).GetOperationName();

            ForbiddenHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.Forbidden);
            if (ForbiddenHttpHandler == null)
            {
                ForbiddenHttpHandler = new ForbiddenHttpHandler
                {
                    IsIntegratedPipeline = IsIntegratedPipeline,
                    WebHostPhysicalPath = WebHostPhysicalPath,
                    WebHostRootFileNames = WebHostRootFileNames,
                    WebHostUrl = config.WebHostUrl,
                    DefaultRootFileName = defaultRootFileName,
                    DefaultHandler = debugDefaultHandler,
                };
            }

            NotFoundHttpHandler = appHost.GetCustomErrorHttpHandler(HttpStatusCode.NotFound);
            if (NotFoundHttpHandler == null)
            {
                NotFoundHttpHandler = new NotFoundHttpHandler
                {
                    IsIntegratedPipeline = IsIntegratedPipeline,
                    WebHostPhysicalPath = WebHostPhysicalPath,
                    WebHostRootFileNames = WebHostRootFileNames,
                    WebHostUrl = config.WebHostUrl,
                    DefaultRootFileName = defaultRootFileName,
                    DefaultHandler = debugDefaultHandler,
                };
            }
        }

#if !NETSTANDARD1_6
        // Entry point for ASP.NET
        public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            var httpContext = context.Request.RequestContext.HttpContext;
            var httpReq = new ServiceStack.Host.AspNet.AspNetRequest(httpContext, url.SanitizedVirtualPath());
            return GetHandler(httpReq);
        }
#endif
        public static string GetBaseUrl()
        {
            return HostContext.Config.WebHostUrl;
        }

        // Entry point for HttpListener
        public static IHttpHandler GetHandler(IHttpRequest httpReq)
        {
            var appHost = HostContext.AppHost;
            foreach (var rawHttpHandler in appHost.RawHttpHandlers)
            {
                var reqInfo = rawHttpHandler(httpReq);
                if (reqInfo != null) return reqInfo;
            }

            var config = appHost.Config;
            string location = config.HandlerFactoryPath;
            string pathInfo = httpReq.PathInfo;
            string physicalPath = httpReq.GetPhysicalPath();
            if (config.DebugMode)
                debugLastHandlerArgs = httpReq.Verb + "|" + httpReq.RawUrl + "|" + physicalPath;

            //Default Request /
            if (pathInfo.IsNullOrEmpty() || pathInfo == "/")
            {
                //If the fallback route can handle it, let it
                if (config.FallbackRestPath != null)
                {
                    string contentType;
                    var sanitizedPath = RestHandler.GetSanitizedPathInfo(pathInfo, out contentType);

                    var restPath = appHost.Config.FallbackRestPath(httpReq.HttpMethod, sanitizedPath, physicalPath);
                    if (restPath != null)
                    {
                        return new RestHandler { RestPath = restPath, RequestName = restPath.RequestType.GetOperationName(), ResponseContentType = contentType };
                    }
                }

                //e.g. CatchAllHandler to Process Markdown files
                var catchAllHandler = GetCatchAllHandlerIfAny(httpReq.HttpMethod, pathInfo, physicalPath);
                if (catchAllHandler != null) return catchAllHandler;

                if (location.IsNullOrEmpty())
                    return DefaultHttpHandler;

                return NonRootModeDefaultHttpHandler;
            }
            return GetHandlerForPathInfo(httpReq.HttpMethod, pathInfo, pathInfo, physicalPath) ?? NotFoundHttpHandler;
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
            var existingFile = pathParts[0].ToLower();
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

        // no handler registered 
        // serve the file from the filesystem, restricting to a safelist of extensions
        public static bool ShouldAllow(string filePath)
        {
            var parts = filePath.SplitOnLast('.');
            if (parts.Length == 1 || string.IsNullOrEmpty(parts[1]))
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

        public void ReleaseHandler(IHttpHandler handler)
        {
        }
    }
}