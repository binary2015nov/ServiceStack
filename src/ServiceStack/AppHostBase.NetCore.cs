#if NETSTANDARD2_0

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Web;
using ServiceStack.NetCore;
using ServiceStack.Host;
using ServiceStack.Host.NetCore;
using ServiceStack.Host.Handlers;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ServiceStack.Configuration;
using ServiceStack.IO;

namespace ServiceStack
{
    public abstract class AppHostBase : ServiceStackHost
    {
        protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices) 
            : base(serviceName, assembliesWithServices) { }

        IApplicationBuilder app;
        IHostingEnvironment env => app?.ApplicationServices.GetService<IHostingEnvironment>();
        public virtual void Bind(IApplicationBuilder app)
        {
            this.app = app;

            WebHostPhysicalPath = GetWebRootPath();
            Config.DebugMode = env.IsDevelopment();

            RegisterLicenseFromAppSettings(AppSettings);
            if (!IsReady)
            {
                Container.Adapter = new NetCoreContainerAdapter(app.ApplicationServices);
                Init();
            }
            app.Use(ProcessRequest);
        }

        /// <summary>
        /// The FilePath used in Virtual File Sources
        /// </summary>
        protected override string GetWebRootPath()
        {
            if (env == null)
                return base.GetWebRootPath();

            return env.WebRootPath ?? env.ContentRootPath;
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
        
        public Func<HttpContext, Task<bool>> NetCoreHandler { get; set; }

        public virtual async Task ProcessRequest(HttpContext context, Func<Task> next)
        {
            if (NetCoreHandler != null)
            {
                var handled = await NetCoreHandler(context);
                if (handled)
                    return;
            }
            
            //Keep in sync with Kestrel/AppSelfHostBase.cs
            var operationName = context.Request.GetOperationName().UrlDecode() ?? "Home";
            var pathInfo = context.Request.Path.HasValue
                ? context.Request.Path.Value
                : "/";

            var mode = Config.HandlerFactoryPath;
            if (!mode.IsNullOrEmpty())
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
            IServiceStackHandler handler;

            try 
            {
                httpReq = new NetCoreRequest(context, operationName, RequestAttributes.None, pathInfo); 
                httpReq.RequestAttributes = httpReq.GetAttributes() | RequestAttributes.Http;
                
                httpRes = httpReq.Response;
                handler = HttpHandlerFactory.GetHandler(httpReq);
            } 
            catch (Exception ex) //Request Initialization error
            {
                var logFactory = context.Features.Get<ILoggerFactory>();
                if (logFactory != null)
                {
                    var log = logFactory.CreateLogger(GetType());
                    log.LogError(default(EventId), ex, ex.Message);
                }

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
                    var logFactory = context.Features.Get<ILoggerFactory>();
                    if (logFactory != null)
                    {
                        var log = logFactory.CreateLogger(GetType());
                        log.LogError(default(EventId), ex, ex.Message);
                    }
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
            if (env?.ContentRootPath != null && relativePath?.StartsWith("~") == true)
                return Path.GetFullPath(env.ContentRootPath.CombineWith(relativePath.Substring(1)));

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
    }

    public static class NetCoreAppHostExtensions
    {
        public static IApplicationBuilder UseServiceStack(this IApplicationBuilder app, AppHostBase appHost)
        {
            appHost.Bind(app);
            return app;
        }

        public static IApplicationBuilder Use(this IApplicationBuilder app, IServiceStackHandler httpHandler)
        {
            return app.Use(httpHandler.Middleware);
        }

        public static IHttpRequest ToRequest(this HttpContext httpContext, string operationName = null)
        {
            var req = new NetCoreRequest(httpContext, operationName, RequestAttributes.None);
            req.RequestAttributes = req.GetAttributes() | RequestAttributes.Http;
            return req;
        }
    }
}

#endif
