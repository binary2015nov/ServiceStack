#if NETSTANDARD2_0

using System;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Web;
using ServiceStack.Logging;
using ServiceStack.NetCore;
using ServiceStack.Host;
using ServiceStack.Host.NetCore;
using ServiceStack.Host.Handlers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack.Configuration;
using ServiceStack.IO;

namespace ServiceStack
{
    public abstract class AppHostBase : ServiceStackHost
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AppHostBase));

        protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices) 
            : base(serviceName, assembliesWithServices) { }

        IApplicationBuilder app;

        public virtual void Bind(IApplicationBuilder app)
        {
            if (this.app == app)
                return;

            if (!Ready)
            {
                Container.Adapter = new NetCoreContainerAdapter(app.ApplicationServices);
                Init();
            }
            app.Use(ProcessRequest);
            this.app = app;
        }

        /// <summary>
        /// The FilePath used in Virtual File Sources
        /// </summary>
        protected override string GetWebRootPath()
        {
            if (app == null)
                return base.GetWebRootPath();

            var env = app.ApplicationServices.GetService<IHostingEnvironment>();
            return env.WebRootPath ?? env.ContentRootPath;
        }

        protected override void OnBeforeInit()
        {
            if (app != null)
            {
                //Initialize VFS
                var env = app.ApplicationServices.GetService<IHostingEnvironment>();
                WebHostPhysicalPath = env.ContentRootPath;
                Config.DebugMode = env.IsDevelopment();

                //Set VirtualFiles to point to ContentRootPath (Project Folder)
                VirtualFiles = new FileSystemVirtualFiles(env.ContentRootPath);
                RegisterLicenseFromAppSettings(AppSettings);
            }
        }

        public static void RegisterLicenseFromAppSettings(IAppSettings settings)
        {
            //Automatically register license key stored in <appSettings/>
            var licenceKeyText = settings.Get(NetStandardPclExport.AppSettingsKey);
            if (!licenceKeyText.IsNullOrEmpty())
            {
                LicenseUtils.RegisterLicense(licenceKeyText);
            }
        }

        public virtual async Task ProcessRequest(HttpContext context, Func<Task> next)
        {
            //Keep in sync with Kestrel/AppSelfHostBase.cs
            var operationName = context.Request.GetOperationName().UrlDecode() ?? "Home";
            var pathInfo = context.Request.Path.HasValue
                ? context.Request.Path.Value
                : "/";

            var mode = Config.HandlerFactoryPath;
            if (!string.IsNullOrEmpty(mode))
            {
                if (pathInfo.IndexOf(mode, StringComparison.Ordinal) != 1)
                {
                    await next();
                    return;
                }

                pathInfo = pathInfo.Substring(mode.Length + 1);
            }

            RequestContext.Instance.StartRequestContext();

            NetCoreRequest httpReq;
            IResponse httpRes;
            System.Web.IHttpHandler handler;

            try 
            {
                httpReq = new NetCoreRequest(context, operationName, RequestAttributes.None, pathInfo); 
                httpReq.RequestAttributes = httpReq.GetAttributes();
                httpRes = httpReq.Response;
                handler = HttpHandlerFactory.GetHandler(httpReq);
            } 
            catch (Exception ex) //Request Initialization error
            {
                //var logFactory = context.Features.Get<ILoggerFactory>();
                //if (logFactory != null)
                //{
                //    var log = logFactory.CreateLogger(GetType());
                //    log.LogError(default(EventId), ex, ex.Message);
                //}
                Logger.Error(ex.Message, ex);

                context.Response.ContentType = MimeTypes.PlainText;
                await context.Response.WriteAsync($"{ex.GetType().Name}: {ex.Message}");
                if (Config.DebugMode)
                    await context.Response.WriteAsync($"\nStackTrace:\n{ex.StackTrace}");
                return;
            }

            if (handler is IServiceStackHandler serviceStackHandler)
            {
                if (serviceStackHandler is NotFoundHttpHandler)
                {
                    await next();
                    return;
                }

                if (!string.IsNullOrEmpty(serviceStackHandler.RequestName))
                    operationName = serviceStackHandler.RequestName;

                try
                {
                    await serviceStackHandler.ProcessRequestAsync(httpReq, httpRes, operationName);
                }
                catch (Exception ex)
                {
                    //var logFactory = context.Features.Get<ILoggerFactory>();
                    //if (logFactory != null)
                    //{
                    //    var log = logFactory.CreateLogger(GetType());
                    //    log.LogError(default(EventId), ex, ex.Message);
                    //}
                    Logger.Error(ex.Message, ex);
                }
                finally
                {
                    httpRes.Close();
                }
                //Matches Exceptions handled in HttpListenerBase.InitTask()

                return;
            }

            await next();
        }

        public override string MapProjectPath(string relativePath)
        {
            return relativePath.MapHostAbsolutePath();
        }

        public override IRequest TryGetCurrentRequest()
        {
            return GetOrCreateRequest(app.ApplicationServices.GetService<IHttpContextAccessor>());
        }

        /// <summary>
        /// Creates an IRequest from IHttpContextAccessor if it's been registered as a singleton
        /// </summary>
        public static IRequest GetOrCreateRequest(IHttpContextAccessor httpContextAccessor)
        {
            return GetOrCreateRequest(httpContextAccessor?.HttpContext);
        }

        public static IRequest GetOrCreateRequest(HttpContext httpContext)
        {
            if (httpContext != null)
            {
                if (httpContext.Items.TryGetValue(Keywords.IRequest, out var oRequest))
                    return (IRequest) oRequest;

                var req = httpContext.ToRequest();
                httpContext.Items[Keywords.IRequest] = req;

                return req;
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public static class NetCoreAppHostExtensions
    {
        public static IApplicationBuilder UseServiceStack(this IApplicationBuilder app, AppHostBase appHost)
        {
            appHost.Bind(app);
            return app;
        }

        public static IHttpRequest ToRequest(this HttpContext httpContext, string operationName = null)
        {
            var req = new NetCoreRequest(httpContext, operationName, RequestAttributes.None);
            req.RequestAttributes = req.GetAttributes();
            return req;
        }
    }
}

#endif
