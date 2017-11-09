using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Web;
using Funq;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Metadata;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class HostContext
    {
        public static ServiceStackHost AppHost { get; internal set; }

        public static bool IsReady() => AppHost != null && AppHost.Ready;

        public static RequestContext RequestContext => RequestContext.Instance;

        public static AsyncContext Async = new AsyncContext();

        internal static ServiceStackHost AssertAppHost()
        {
            if (AppHost == null)
                throw new ConfigurationErrorsException(
                    "ServiceStack: AppHost does not exist or has not been initialized. " +
                    "Make sure you have created an AppHost and started it with 'new AppHost().Init();' " +
                    " in your Global.asax Application_Start() or alternative Application StartUp");
                    
            return AppHost;
        }

#if !NETSTANDARD2_0
        public static bool IsAspNetHost => AppHost is AppHostBase;
        public static bool IsHttpListenerHost => AppHost is ServiceStack.Host.HttpListener.HttpListenerBase;
        public static bool IsNetCore => false;
#else
        public static bool IsAspNetHost => false;
        public static bool IsHttpListenerHost => false;
        public static bool IsNetCore => true;
#endif

        public static T TryResolve<T>() => AssertAppHost().TryResolve<T>();

        public static T Resolve<T>() => AssertAppHost().Resolve<T>();

        public static Container Container => AssertAppHost().Container;

        public static ServiceController ServiceController => AssertAppHost().ServiceController;

        public static MetadataPagesConfig MetadataPagesConfig => AssertAppHost().MetadataPagesConfig;

        public static IContentTypes ContentTypes => AssertAppHost().ContentTypes;

        public static HostConfig Config => AssertAppHost().Config;

        public static IAppSettings AppSettings => AssertAppHost().AppSettings;

        public static ServiceMetadata Metadata => AssertAppHost().Metadata;

        public static bool TestMode
        {
            get => AppHost != null && AppHost.TestMode;
            set => AppHost.TestMode = value;
        }

        public static List<HttpHandlerResolver> CatchAllHandlers => AssertAppHost().CatchAllHandlers;

        public static List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers => AssertAppHost().RawHttpHandlers;

        public static List<Action<IRequest, IResponse, object>> GlobalRequestFilters => AssertAppHost().GlobalRequestFilters;

        public static List<Action<IRequest, IResponse, object>> GlobalResponseFilters => AssertAppHost().GlobalResponseFilters;

        public static List<Action<IRequest, IResponse, object>> GlobalMessageRequestFilters => AssertAppHost().GlobalMessageRequestFilters;

        public static List<Action<IRequest, IResponse, object>> GlobalMessageResponseFilters => AssertAppHost().GlobalMessageResponseFilters;

        public static bool ApplyCustomHandlerRequestFilters(IRequest httpReq, IResponse httpRes)
        {
            return AssertAppHost().ApplyCustomHandlerRequestFilters(httpReq, httpRes);
        }

        public static bool ApplyPreRequestFilters(IRequest httpReq, IResponse httpRes)
        {
            return AssertAppHost().ApplyPreRequestFilters(httpReq, httpRes);
        }

        public static Task ApplyRequestFiltersAsync(IRequest httpReq, IResponse httpRes, object requestDto)
        {
            return AssertAppHost().ApplyRequestFiltersAsync(httpReq, httpRes, requestDto);
        }

        public static Task ApplyResponseFiltersAsync(IRequest httpReq, IResponse httpRes, object response)
        {
            return AssertAppHost().ApplyResponseFiltersAsync(httpReq, httpRes, response);
        }

        /// <summary>
        /// Read/Write Virtual FileSystem. Defaults to FileSystemVirtualPathProvider
        /// </summary>
        public static IVirtualFiles VirtualFiles => AssertAppHost().VirtualFiles;

        /// <summary>
        /// Cascading collection of virtual file sources, inc. Embedded Resources, File System, In Memory, S3
        /// </summary>
        public static IVirtualPathProvider VirtualFileSources => AssertAppHost().VirtualFileSources;

        public static ICacheClient Cache => TryResolve<ICacheClient>();

        public static MemoryCacheClient LocalCache => TryResolve<MemoryCacheClient>();

        /// <summary>
        /// Call to signal the completion of a ServiceStack-handled Request
        /// </summary>
        internal static void CompleteRequest(IRequest request)
        {
            try
            {
                AssertAppHost().OnEndRequest(request);
            }
            catch (Exception) { }
        }

        public static IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            return AssertAppHost().CreateServiceRunner<TRequest>(actionContext);
        }

        public static object ExecuteService(object requestDto, IRequest request)
        {
            return AssertAppHost().ExecuteService(requestDto, request);           
        }

        public static TPlugin AssertPlugin<TPlugin>() 
            where TPlugin : class, IPlugin
        {
            return GetPlugin<TPlugin>()
                ?? throw new NotImplementedException($"Plugin '{typeof(TPlugin).Name}' has not been registered.");
        }

        public static TPlugin GetPlugin<TPlugin>()
            where TPlugin : class, IPlugin
        {
            var appHost = AppHost;
            return appHost?.GetPlugin<TPlugin>();
        }

        public static bool HasPlugin<TPlugin>() 
            where TPlugin : class, IPlugin
        {
            var appHost = AppHost;
            return appHost != null && appHost.HasPlugin<TPlugin>();
        }

        public static void Release(object service)
        {
            if (AppHost != null)
            {
                AppHost.Release(service);
            }
            else
            {
                using (service as IDisposable) { }
            }
        }

        public static UnauthorizedAccessException UnauthorizedAccess(RequestAttributes requestAttrs)
        {
            return new UnauthorizedAccessException($"Request with '{requestAttrs}' is not allowed");
        }

        public static string ResolveLocalizedString(string text, IRequest request = null)
        {
            return AssertAppHost().ResolveLocalizedString(text, request);
        }

        public static string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
        {
            return AssertAppHost().ResolveAbsoluteUrl(virtualPath, httpReq);
        }

        public static string ResolvePhysicalPath(string virtualPath, IRequest httpReq)
        {
            return AssertAppHost().ResolvePhysicalPath(virtualPath, httpReq);
        }

        private static string defaultOperationNamespace;
        public static string DefaultOperationNamespace
        {
            get => defaultOperationNamespace ?? (defaultOperationNamespace = GetDefaultNamespace());
            set => defaultOperationNamespace = value;
        }

        public static string GetDefaultNamespace()
        {
            if (!string.IsNullOrEmpty(defaultOperationNamespace)) return null;

            foreach (var operationType in Metadata.RequestTypes)
            {
                var attrs = operationType.AllAttributes<DataContractAttribute>();

                if (attrs.Length <= 0) continue;

                var attr = attrs[0];
                if (string.IsNullOrEmpty(attr.Namespace)) continue;

                return attr.Namespace;
            }

            return null;
        }

        public static object RaiseServiceException(IRequest httpReq, object request, Exception ex)
        {
            return AssertAppHost().OnServiceException(httpReq, request, ex);
        }

        public static void RaiseUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            AssertAppHost().OnUncaughtException(httpReq, httpRes, operationName, ex);
        }

        public static void RaiseAndHandleUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            AssertAppHost().OnUncaughtException(httpReq, httpRes, operationName, ex);

            if (httpRes.IsClosed)
                return;

            AssertAppHost().HandleUncaughtException(httpReq, httpRes, operationName, ex);
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service from a ASP.NET HttpContext.
        /// </summary>
        public static T ResolveService<T>(HttpContextBase httpCtx = null) where T : class, IRequiresRequest
        {
            var httpReq = httpCtx != null ? httpCtx.ToRequest() : GetCurrentRequest();
            return ResolveService(httpReq, AssertAppHost().Container.Resolve<T>());
        }

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service from a HttpListenerContext.
        /// </summary>
        public static T ResolveService<T>(HttpListenerContext httpCtx) where T : class, IRequiresRequest
        {
            return ResolveService(httpCtx.ToRequest(), AssertAppHost().Container.Resolve<T>());
        }
#endif

        /// <summary>
        /// Resolves and auto-wires a ServiceStack Service.
        /// </summary>
        public static T ResolveService<T>(IRequest httpReq) where T : class, IRequiresRequest
        {
            return ResolveService(httpReq, AssertAppHost().Container.Resolve<T>());
        }

        public static T ResolveService<T>(IRequest httpReq, T service)
        {
            if (service is IRequiresRequest hasRequest)
            {
                httpReq.SetInProcessRequest();
                hasRequest.Request = httpReq;
            }
            return service;
        }

        public static bool HasValidAuthSecret(IRequest httpReq)
        {
            return AssertAppHost().HasValidAuthSecret(httpReq);
        }

        public static bool HasFeature(Feature feature)
        {
            return AssertAppHost().HasFeature(feature);
        }

        public static IRequest GetCurrentRequest()
        {
            var req = AssertAppHost().TryGetCurrentRequest();
            if (req == null)
                throw new NotImplementedException(ErrorMessages.HostDoesNotSupportSingletonRequest);

            return req;
        }

        public static IRequest TryGetCurrentRequest()
        {
            return AssertAppHost().TryGetCurrentRequest();
        }
    }
}
