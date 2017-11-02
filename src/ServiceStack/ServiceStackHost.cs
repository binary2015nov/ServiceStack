// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using Funq;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Formats;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Metadata;
using ServiceStack.MiniProfiler;
using ServiceStack.NativeTypes;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract partial class ServiceStackHost : IAppHost, IFunqlet, IHasContainer, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServiceStackHost));

        public readonly DateTime CreatedAt = DateTime.Now;

        public HostConfig Config { get; private set; }

        protected ServiceStackHost(string serviceName, params Assembly[] assembliesWithServices)
        {
            Platform.HostInstance = this;
            Config = new HostConfig { DebugMode = GetType().Assembly.IsDebugBuild() }; 
            AppSettings = ServiceStack.Configuration.AppSettings.Default;
            Metadata = new ServiceMetadata();
            ServiceName = serviceName;
            ServiceAssemblies = assembliesWithServices;
            Container = new Container { DefaultOwner = Owner.External };
            ContentTypes = ServiceStack.Host.ContentTypes.Default;
            Routes = new ServiceRoutes();
            WebHostPhysicalPath = GetWebRootPath();
            PreRequestFilters = new List<Action<IRequest, IResponse>>();
            RequestConverters = new List<Func<IRequest, object, Task<object>>>();
            ResponseConverters = new List<Func<IRequest, object, Task<object>>>();
            GlobalRequestFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalRequestFiltersAsync = new List<Func<IRequest, IResponse, object, Task>>();
            GlobalTypedRequestFilters = new Dictionary<Type, ITypedFilter>();
            GlobalResponseFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalResponseFiltersAsync = new List<Func<IRequest, IResponse, object, Task>>();
            GlobalTypedResponseFilters = new Dictionary<Type, ITypedFilter>();
            GlobalMessageRequestFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalMessageRequestFiltersAsync = new List<Func<IRequest, IResponse, object, Task>>();
            GlobalTypedMessageRequestFilters = new Dictionary<Type, ITypedFilter>();
            GlobalMessageResponseFilters = new List<Action<IRequest, IResponse, object>>();
            GlobalTypedMessageResponseFilters = new Dictionary<Type, ITypedFilter>();
            GatewayRequestFilters = new List<Action<IRequest, object>>();
            GatewayRequestFiltersAsync = new List<Func<IRequest, object, Task>>();
            GatewayResponseFilters = new List<Action<IRequest, object>>();
            GatewayResponseFiltersAsync = new List<Func<IRequest, object, Task>>();
            ViewEngines = new List<IViewEngine>();
            ServiceExceptionHandlers = new List<ServiceExceptionHandler>();
            UncaughtExceptionHandlers = new List<UncatchedExceptionHandler>();
            AfterInitCallbacks = new List<Action<IAppHost>>();
            OnDisposeCallbacks = new List<Action<IAppHost>>();
            OnEndRequestCallbacks = new List<Action<IRequest>>();
            AddVirtualFileSources = new List<IVirtualPathProvider>();
            RawHttpHandlers = new List<Func<IHttpRequest, IHttpHandler>> {
                ReturnRedirectHandler
            };
            CatchAllHandlers = new List<HttpHandlerResolver>();
            CustomErrorHttpHandlers = new Dictionary<HttpStatusCode, IServiceStackHandler> {
                { HttpStatusCode.Forbidden, new ForbiddenHttpHandler() },
                { HttpStatusCode.NotFound, new NotFoundHttpHandler() },
            };
            StartUpErrors = new List<ResponseStatus>();
            AsyncErrors = new List<ResponseStatus>();
            Plugins = new List<IPlugin> {
                new HtmlFormat(),
                new CsvFormat(),
                new PredefinedRoutesFeature(),
                new MetadataFeature(),
                new NativeTypesFeature(),
                new HttpCacheFeature()
            };
            ExcludeAutoRegisteringServiceTypes = new HashSet<Type> {
                typeof(AuthenticateService),
                typeof(RegisterService),
                typeof(AssignRolesService),
                typeof(UnAssignRolesService),
                typeof(NativeTypesService),
                typeof(PostmanService),
            };
        }

        public string ServiceName { get; set; }

        public Assembly[] ServiceAssemblies { get; set; }

        public IAppSettings AppSettings { get; set; }

        /// <summary>
        /// The AppHost.Container. Note: it is not thread safe to register dependencies after AppStart.
        /// </summary>
        public Container Container { get; private set; }

        public string WebHostPhysicalPath { get; set; }

        public DateTime ReadyAt { get; private set; }

        protected virtual void OnBeforeInit() { }

        protected virtual void OnAfterInit() { }

        public abstract void Configure(Container container);

        /// <summary>
        /// Initializes the AppHost.
        /// Calls the <see cref="Configure"/> method.
        /// Should be called before start.
        /// </summary>
        public virtual ServiceStackHost Init()
        {
            if (Ready) throw new InvalidOperationException($"The current method has been already invoked.");

            HostContext.AppHost = this;
            
            OnBeforeInit();

            Container.Register<IAppSettings>(AppSettings);
            Container.Register<IHashProvider>(c => new SaltedHash()).ReusedWithin(ReuseScope.None);
            if (Config.DebugMode)           
                Plugins.Add(new RequestInfoFeature());

            Service.GlobalResolver = this;
            ServiceController = ServiceController ?? CreateServiceController();
            Configure(Container);      
            ConfigurePlugins();
            try
            {
                ServiceController.Init();
            }
            catch (Exception ex)
            {
                OnStartupException(ex);
            }
            List<IVirtualPathProvider> pathProviders = null;
            if (VirtualFileSources == null)
            {
                pathProviders = GetVirtualFileSources().Where(x => x != null).ToList();

                VirtualFileSources = pathProviders.Count > 1
                    ? new MultiVirtualFiles(pathProviders.ToArray())
                    : pathProviders.First();
            }

            if (VirtualFiles == null)
                VirtualFiles = pathProviders?.FirstOrDefault(x => x is FileSystemVirtualFiles) as IVirtualFiles
                    ?? GetVirtualFileSources().FirstOrDefault(x => x is FileSystemVirtualFiles) as IVirtualFiles;
            try
            {
                InitFinal();
            }
            catch (Exception ex)
            {
                OnStartupException(ex);
            }
            OnAfterInit();      
            ReadyAt = DateTime.Now;
            LogInitResult();
            return this;
        }

        public bool Ready => ReadyAt != DateTime.MinValue;

        /// <summary>
        /// Collection of added plugins.
        /// </summary>
        public List<IPlugin> Plugins { get; private set; }

        /// <summary>
        /// If app currently runs for unit tests. Used for overwritting AuthSession.
        /// </summary>
        public bool TestMode { get; set; }

        public ServiceMetadata Metadata { get; private set; }

        public ServiceController ServiceController { get; set; }

        protected virtual ServiceController CreateServiceController()
        {
            return new ServiceController(this, ServiceAssemblies);
            //Alternative way to inject Service Resolver strategy
            //return new ServiceController(this, () => ServiceAssemblies.SelectMany(x => x.GetTypes()));
        }

        private void ConfigurePlugins()
        {
            //Some plugins need to initialize before other plugins are registered.
            foreach (var plugin in Plugins)
            {
                if (plugin is IPreInitPlugin preInitPlugin)
                {
                    try
                    {
                        preInitPlugin.Configure(this);
                    }
                    catch (Exception ex)
                    {
                        OnStartupException(ex);
                    }
                }
            }
        }

        //After configure called
        public virtual void InitFinal()
        {
            if (Config.EnableFeatures != Feature.All)
            {
                if ((Feature.Xml & Config.EnableFeatures) != Feature.Xml)
                {
                    Config.IgnoreFormatsInMetadata.Add("xml");
                    Config.PreferredContentTypes.Remove(MimeTypes.Xml);
                }
                if ((Feature.Json & Config.EnableFeatures) != Feature.Json)
                {
                    Config.IgnoreFormatsInMetadata.Add("json");
                    Config.PreferredContentTypes.Remove(MimeTypes.Json);
                }
                if ((Feature.Jsv & Config.EnableFeatures) != Feature.Jsv)
                {
                    Config.IgnoreFormatsInMetadata.Add("jsv");
                    Config.PreferredContentTypes.Remove(MimeTypes.Jsv);
                }
                if ((Feature.Csv & Config.EnableFeatures) != Feature.Csv)
                {
                    Config.IgnoreFormatsInMetadata.Add("csv");
                    Config.PreferredContentTypes.Remove(MimeTypes.Csv);
                }
                if ((Feature.Html & Config.EnableFeatures) != Feature.Html)
                {
                    Config.IgnoreFormatsInMetadata.Add("html");
                    Config.PreferredContentTypes.Remove(MimeTypes.Html);
                }
                if ((Feature.Soap11 & Config.EnableFeatures) != Feature.Soap11)
                    Config.IgnoreFormatsInMetadata.Add("soap11");
                if ((Feature.Soap12 & Config.EnableFeatures) != Feature.Soap12)
                    Config.IgnoreFormatsInMetadata.Add("soap12");
            }

            if ((Feature.Html & Config.EnableFeatures) != Feature.Html)
                Plugins.RemoveAll(x => x is HtmlFormat);

            if ((Feature.Csv & Config.EnableFeatures) != Feature.Csv)
                Plugins.RemoveAll(x => x is CsvFormat);

            if ((Feature.PredefinedRoutes & Config.EnableFeatures) != Feature.PredefinedRoutes)
                Plugins.RemoveAll(x => x is PredefinedRoutesFeature);

            if ((Feature.Metadata & Config.EnableFeatures) != Feature.Metadata)
            {
                Plugins.RemoveAll(x => x is MetadataFeature);
                Plugins.RemoveAll(x => x is NativeTypesFeature);
            }

            if ((Feature.RequestInfo & Config.EnableFeatures) != Feature.RequestInfo)
                Plugins.RemoveAll(x => x is RequestInfoFeature);

            if ((Feature.Razor & Config.EnableFeatures) != Feature.Razor)
                Plugins.RemoveAll(x => x is IRazorPlugin);    //external

            if ((Feature.ProtoBuf & Config.EnableFeatures) != Feature.ProtoBuf)
                Plugins.RemoveAll(x => x is IProtoBufPlugin); //external

            if ((Feature.MsgPack & Config.EnableFeatures) != Feature.MsgPack)
                Plugins.RemoveAll(x => x is IMsgPackPlugin);  //external


            var plugins = Plugins.ToArray();
            delayLoadPlugin = true;
            LoadPluginsInternal(plugins);

            AfterPluginsLoaded();

            Metadata.Config.IgnoreFormats = Config.IgnoreFormatsInMetadata;

            if (!TestMode && Container.Exists<IAuthSession>())
                throw new Exception(ErrorMessages.ShouldNotRegisterAuthSession);

            if (!Container.Exists<ICacheClient>())
            {
                if (Container.Exists<IRedisClientsManager>())
                    Container.Register(c => c.Resolve<IRedisClientsManager>().GetCacheClient());
                else
                    Container.Register<ICacheClient>(MemoryCacheClient.Default);
            }

            if (!Container.Exists<MemoryCacheClient>())
                Container.Register(MemoryCacheClient.Default);

            if (Container.Exists<IMessageService>()
                && !Container.Exists<IMessageFactory>())
            {
                Container.Register(c => c.Resolve<IMessageService>().MessageFactory);
            }

            if (Container.Exists<IUserAuthRepository>()
                && !Container.Exists<IAuthRepository>())
            {
                Container.Register<IAuthRepository>(c => c.Resolve<IUserAuthRepository>());
            }
            
            if (Config.LogUnobservedTaskExceptions)
            {
                TaskScheduler.UnobservedTaskException += (sender, args) =>
                {
                    args.SetObserved();
                    args.Exception.Handle(ex =>
                    {
                        lock (AsyncErrors)
                        {
                            AsyncErrors.Add(DtoUtils.CreateErrorResponse(null, ex).GetResponseStatus());
                            return true;
                        }
                    });
                };
            }

            foreach (var callback in AfterInitCallbacks)
            {
                callback(this);
            }
            //Register any routes configured on Routes
            foreach (var restPath in Routes.RestPaths)
            {
                ServiceController.RegisterRestPath(restPath);

                //Auto add Route Attributes so they're available in T.ToUrl() extension methods
                restPath.RequestType
                    .AddAttributes(new RouteAttribute(restPath.Path, restPath.AllowedVerbs)
                    {
                        Priority = restPath.Priority,
                        Summary = restPath.Summary,
                        Notes = restPath.Notes,
                    });
            }

            HttpHandlerFactory.Init();
        }

        private void AfterPluginsLoaded()
        {
            MetadataFeature feature = GetPlugin<MetadataFeature>();
            feature?.AddSection(MetadataFeature.AvailableFeatures);
            foreach (var plugin in Plugins)
            {
                try
                {
                    string title = plugin.GetType().Name;
                    feature?.AddLink(MetadataFeature.AvailableFeatures, "#" + title, title);
                    var preInitPlugin = plugin as IPostInitPlugin;
                    if (preInitPlugin != null)
                    {
                        preInitPlugin.AfterPluginsLoaded(this);
                    }
                }
                catch (Exception ex)
                {
                    OnStartupException(ex);
                }                
            }
        }

        private void LogInitResult()
        {
            var elapsed = DateTime.Now - CreatedAt;
            if (StartUpErrors.Any())
            {
                Config.GlobalResponseHeaders["X-Startup-Errors"] = StartUpErrors.Count.ToString();
                Logger.ErrorFormat(
                    "Initializing Application {0} took {1}ms. {2} error(s) detected: {3}",
                    ServiceName,
                    elapsed.TotalMilliseconds,
                    StartUpErrors.Count,
                    StartUpErrors.Dump());
            }
            else
            {
                Logger.InfoFormat(
                    "Initializing Application {0} took {1}ms. No errors detected.",
                    ServiceName,
                    elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Gets Full Directory Path of where the app is running
        /// </summary>
        protected virtual string GetWebRootPath() => "~".MapAbsolutePath();

        public virtual List<IVirtualPathProvider> GetVirtualFileSources()
        {
            var pathProviders = new List<IVirtualPathProvider> {
                new FileSystemVirtualFiles(WebHostPhysicalPath)
            };

            pathProviders.AddRange(Config.EmbeddedResourceSources
                .Map(x => new ResourceVirtualFiles(x) { LastModified = Platform.GetAssemblyLastModified(x) } ));

            if (AddVirtualFileSources.Count > 0)
                pathProviders.AddRange(AddVirtualFileSources);
            return pathProviders;
        }

        /// <summary>
        /// Starts the AppHost.
        /// this methods needs to be overwritten in subclass to provider a listener to start handling requests.
        /// </summary>
        /// <param name="urlBase">Url to listen to</param>
        public virtual ServiceStackHost Start(string urlBase)
        {
            throw new NotSupportedException($"The current method is not supported by this AppHost - {GetType().FullName}.");
        }

        // Rare for a user to auto register all avaialable services in ServiceStack.dll
        // But happens when ILMerged, so exclude autoregistering SS services by default 
        // and let them register them manually
        public HashSet<Type> ExcludeAutoRegisteringServiceTypes { get; private set; }

        public ServiceRoutes Routes { get; private set; }

        public IEnumerable<RestPath> RestPaths => ServiceController?.RestPathMap.SelectMany(x => x.Value) ?? Routes.RestPaths;

        public Dictionary<Type, Func<IRequest, object>> RequestBinders => ServiceController?.RequestTypeFactoryMap;

        public ContentTypes ContentTypes { get; set; }

        /// <summary>
        /// Collection of PreRequest filters.
        /// They are called before each request is handled by a service, but after an HttpHandler is by the <see cref="HttpHandlerFactory"/> chosen.
        /// called in <see cref="ApplyPreRequestFilters"/>.
        /// </summary>
        public List<Action<IRequest, IResponse>> PreRequestFilters { get; private set; }

        /// <summary>
        /// Collection of RequestConverters.
        /// Can be used to convert/change Input Dto
        /// Called after routing and model binding, but before request filters.
        /// All request converters are called unless <see cref="IResponse.IsClosed"></see>
        /// Converter can return null, orginal model will be used.
        /// 
        /// Note one converter could influence the input for the next converter!
        /// </summary>
        public List<Func<IRequest, object, Task<object>>> RequestConverters { get; private set; }


        /// <summary>
        /// Collection of ResponseConverters.
        /// Can be used to convert/change Output Dto
        /// 
        /// Called directly after response is handled, even before <see cref="ApplyResponseFiltersAsync"></see>!
        /// </summary>
        public List<Func<IRequest, object, Task<object>>> ResponseConverters { get; private set; }

        public List<Action<IRequest, IResponse, object>> GlobalRequestFilters { get; private set; }

        public List<Func<IRequest, IResponse, object, Task>> GlobalRequestFiltersAsync { get; private set; }

        public Dictionary<Type, ITypedFilter> GlobalTypedRequestFilters { get; private set; }

        public List<Action<IRequest, IResponse, object>> GlobalResponseFilters { get; private set; }

        public List<Func<IRequest, IResponse, object, Task>> GlobalResponseFiltersAsync { get; set; }

        public Dictionary<Type, ITypedFilter> GlobalTypedResponseFilters { get; private set; }

        public List<Action<IRequest, IResponse, object>> GlobalMessageRequestFilters { get; private set; }

        public List<Func<IRequest, IResponse, object, Task>> GlobalMessageRequestFiltersAsync { get; private set; }

        public Dictionary<Type, ITypedFilter> GlobalTypedMessageRequestFilters { get; private set; }

        public List<Action<IRequest, IResponse, object>> GlobalMessageResponseFilters { get; private set; }

        public Dictionary<Type, ITypedFilter> GlobalTypedMessageResponseFilters { get; private set; }

        /// <summary>
        /// Lists of view engines for this app.
        /// If view is needed list is looped until view is found.
        /// </summary>
        public List<IViewEngine> ViewEngines { get; set; }

        public List<ServiceExceptionHandler> ServiceExceptionHandlers { get; set; }

        public List<UncatchedExceptionHandler> UncaughtExceptionHandlers { get; set; }

        public List<Action<IAppHost>> AfterInitCallbacks { get; set; }

        public List<Action<IAppHost>> OnDisposeCallbacks { get; set; }

        public List<Action<IRequest>> OnEndRequestCallbacks { get; set; }

        public List<Func<IHttpRequest, IHttpHandler>> RawHttpHandlers { get; private set; }

        public List<HttpHandlerResolver> CatchAllHandlers { get; private set; }

        public IServiceStackHandler GlobalHtmlErrorHttpHandler { get; set; }

        public Dictionary<HttpStatusCode, IServiceStackHandler> CustomErrorHttpHandlers { get; private set; }

        public List<ResponseStatus> StartUpErrors { get; set; }

        public List<ResponseStatus> AsyncErrors { get; set; }

        public IVirtualFiles VirtualFiles { get; set; }

        public IVirtualPathProvider VirtualFileSources { get; set; }

        public List<IVirtualPathProvider> AddVirtualFileSources { get; set; }

        public List<Action<IRequest, object>> GatewayRequestFilters { get; set; }

        public List<Func<IRequest, object, Task>> GatewayRequestFiltersAsync { get; set; }

        public List<Action<IRequest, object>> GatewayResponseFilters { get; set; }

        public List<Func<IRequest, object, Task>> GatewayResponseFiltersAsync { get; set; }

        [Obsolete("Renamed to VirtualFileSources")]
        public IVirtualPathProvider VirtualPathProvider
        {
            get { return VirtualFileSources; }
            set { VirtualFileSources = value; }
        }

        /// <summary>
        /// Executed immediately before a Service is executed. Use return to change the request DTO used, must be of the same type.
        /// </summary>
        public virtual object OnPreExecuteServiceFilter(IService service, object request, IRequest httpReq, IResponse httpRes)
        {
            return request;
        }

        /// <summary>
        /// Executed immediately after a service is executed. Use return to change response used.
        /// </summary>
        public virtual object OnPostExecuteServiceFilter(IService service, object response, IRequest httpReq, IResponse httpRes)
        {
            return response;
        }

        /// <summary>
        /// Occurs when the Service throws an Exception.
        /// </summary>
        public virtual object OnServiceException(IRequest request, object requestDto, Exception ex)
        {
            object lastError = null;
            foreach (var errorHandler in ServiceExceptionHandlers)
            {
                lastError = errorHandler(request, requestDto, ex) ?? lastError;
            }
            return lastError;
        }

        /// <summary>
        /// Occurs when an exception is thrown whilst processing a request.
        /// </summary>
        public virtual void OnUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            if (UncaughtExceptionHandlers.Count > 0)
            {
                foreach (var errorHandler in UncaughtExceptionHandlers)
                {
                    errorHandler(httpReq, httpRes, operationName, ex);
                }
            }
        }

        public virtual void HandleUncaughtException(IRequest httpReq, IResponse httpRes, string operationName, Exception ex)
        {
            //Only add custom error messages to StatusDescription
            var httpError = ex as IHttpError;
            var errorMessage = httpError?.Message;
            var statusCode = ex.ToStatusCode();

            //httpRes.WriteToResponse always calls .Close in it's finally statement so 
            //if there is a problem writing to response, by now it will be closed
            httpRes.WriteErrorToResponse(httpReq, httpReq.ResponseContentType, operationName, errorMessage, ex, statusCode).Wait();
        }

        protected virtual void OnStartupException(Exception ex)
        {
            this.StartUpErrors.Add(DtoUtils.CreateErrorResponse(null, ex).GetResponseStatus());

            if (Config.StrictMode.GetValueOrDefault())
                throw ex;
        }

        public virtual void Release(object instance)
        {
            try
            {
                if (Container.Adapter is IRelease iocAdapterReleases)
                {
                    iocAdapterReleases.Release(instance);
                }
                else
                {
                    var disposable = instance as IDisposable;
                    disposable?.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("ServiceStackHost.Release", ex);
            }
        }

        public virtual void OnEndRequest(IRequest request = null)
        {
            try
            {
                var disposables = RequestContext.Instance.Items.Values;
                foreach (var item in disposables)
                {
                    Release(item);
                }

                RequestContext.Instance.EndRequest();

                foreach (var fn in OnEndRequestCallbacks)
                {
                    fn(request);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error when Disposing Request Context", ex);
            }
        }

        /// <summary>
        /// Register singleton in the Ioc Container of the AppHost.
        /// </summary>
        public virtual void Register<T>(T instance)
        {
            this.Container.Register(instance);
        }

        /// <summary>
        /// Registers type to be automatically wired by the Ioc container of the AppHost.
        /// </summary>
        /// <typeparam name="T">Concrete type</typeparam>
        /// <typeparam name="TAs">Abstract type</typeparam>
        public virtual void RegisterAs<T, TAs>() where T : TAs
        {
            this.Container.RegisterAutoWiredAs<T, TAs>();
        }

        /// <summary>
        /// Tries to resolve type through the ioc container of the AppHost. 
        /// Can return null.
        /// </summary>
        public virtual T TryResolve<T>()
        {
            return this.Container.TryResolve<T>();
        }

        /// <summary>
        /// Resolves Type through the Ioc container of the AppHost.
        /// </summary>
        /// <exception cref="ResolutionException">If type is not registered</exception>
        public virtual T Resolve<T>()
        {
            return this.Container.Resolve<T>();
        }

        /// <summary>
        /// Looks for first plugin of this type in Plugins.
        /// Reflection performance penalty.
        /// </summary>
        public TPlugin GetPlugin<TPlugin>() where TPlugin : class, IPlugin
        {
            return Plugins.FirstOrDefault(x => x is TPlugin) as TPlugin;
        }

        public bool HasPlugin<TPlugin>() where TPlugin : class, IPlugin
        {
            return Plugins.FirstOrDefault(x => x is TPlugin) != null;
        }

        public virtual IServiceRunner<TRequest> CreateServiceRunner<TRequest>(ActionContext actionContext)
        {
            //cached per service action
            return new ServiceRunner<TRequest>(this, actionContext);
        }

        public virtual string ResolveLocalizedString(string text, IRequest request)
        {
            return text;
        }

        public virtual string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
        {
            if (virtualPath.IsNullOrEmpty())      
                return httpReq.AbsoluteUri;

            var baseUrl = GetBaseUrl(httpReq);
            if (virtualPath.StartsWith("~"))
                return baseUrl.AppendPath(virtualPath.TrimStart('~'));

            return baseUrl.AppendPath(virtualPath);              
        }

        public virtual string GetBaseUrl(IRequest httpReq)
        {
            if (!Config.WebHostUrl.IsNullOrEmpty())
                return Config.WebHostUrl;

            var absoluteUri = httpReq.AbsoluteUri;
            var index = httpReq.PathInfo.IsNullOrEmpty() || httpReq.PathInfo == "/"
                ? absoluteUri.IndexOf("?", StringComparison.Ordinal)
                : absoluteUri.IndexOf(httpReq.PathInfo.TrimEnd('/'), StringComparison.Ordinal) + 1;

            var hostUrl = index > 0 ? absoluteUri.Substring(0, index) : absoluteUri;
            return Config.WebHostUrl = hostUrl.WithTrailingSlash();
        }

        public virtual string ResolvePhysicalPath(string virtualPath, IRequest httpReq)
        {
            return VirtualFileSources.CombineVirtualPath(VirtualFileSources.RootDirectory.RealPath, virtualPath);
        }

        private bool delayLoadPlugin;
        public virtual void LoadPlugin(params IPlugin[] plugins)
        {
            if (delayLoadPlugin)
            {
                LoadPluginsInternal(plugins);
                Plugins.AddRange(plugins);
            }
            else
            {
                foreach (var plugin in plugins)
                {
                    Plugins.Add(plugin);
                }
            }
        }

        internal virtual void LoadPluginsInternal(params IPlugin[] plugins)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    plugin.Register(this);
                }
                catch (Exception ex)
                {
                    OnStartupException(ex);
                }
            }
        }

        public virtual object ExecuteService(object requestDto)
        {
            return ExecuteService(requestDto, RequestAttributes.None);
        }

        public virtual object ExecuteService(object requestDto, IRequest request)
        {
            using (Profiler.Current.Step("Execute Service"))
            {
                return ServiceController.Execute(requestDto, request);
            }
        }

        public virtual object ExecuteService(object requestDto, RequestAttributes requestAttributes)
        {
            return ServiceController.Execute(requestDto, new BasicRequest(requestDto, requestAttributes));
        }

        public virtual object ExecuteMessage(IMessage mqMessage)
        {
            return ServiceController.ExecuteMessage(mqMessage, new BasicRequest(mqMessage));
        }

        public virtual object ExecuteMessage(IMessage message, IRequest req)
        {
            return ServiceController.ExecuteMessage(message, req);
        }

        public virtual void RegisterService(Type serviceType, params string[] atRestPaths)
        {
            ServiceController.RegisterService(serviceType);
            var reqAttr = serviceType.FirstAttribute<DefaultRequestAttribute>();
            if (reqAttr != null)
            {
                foreach (var atRestPath in atRestPaths)
                {
                    if (atRestPath == null)
                        continue;

                    ServiceController.RegisterRestPath(new RestPath(reqAttr.RequestType, atRestPath));
                }
            }
        }

        public void RegisterServicesInAssembly(Assembly assembly)
        {
            ServiceController.RegisterServicesInAssembly(assembly);
        }

        public virtual RouteAttribute[] GetRouteAttributes(Type requestType)
        {
            return requestType.AllAttributes<RouteAttribute>();
        }

        public virtual string GenerateWsdl(WsdlTemplateBase wsdlTemplate)
        {
            var wsdl = wsdlTemplate.ToString();

            wsdl = wsdl.Replace("http://schemas.datacontract.org/2004/07/ServiceStack", Config.WsdlServiceNamespace);

            if (Config.WsdlServiceNamespace != HostConfig.DefaultWsdlNamespace)
            {
                wsdl = wsdl.Replace(HostConfig.DefaultWsdlNamespace, Config.WsdlServiceNamespace);
            }

            return wsdl;
        }

        public void RegisterTypedRequestFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedRequestFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        public void RegisterTypedRequestFilter<T>(Func<Container, ITypedFilter<T>> filter)
        {
            RegisterTypedFilter(RegisterTypedRequestFilter, filter);
        }

        public void RegisterTypedResponseFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedResponseFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        public void RegisterTypedResponseFilter<T>(Func<Container, ITypedFilter<T>> filter)
        {
            RegisterTypedFilter(RegisterTypedResponseFilter, filter);
        }

        private void RegisterTypedFilter<T>(Action<Action<IRequest, IResponse, T>> registerTypedFilter, Func<Container, ITypedFilter<T>> filter)
        {
            registerTypedFilter.Invoke((request, response, dto) =>
            {
                // The filter MUST be resolved inside the RegisterTypedFilter call.
                // Otherwise, the container will not be able to resolve some auto-wired dependencies.
                filter
                    .Invoke(Container)
                    .Invoke(request, response, dto);
            });
        }

        public void RegisterTypedMessageRequestFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedMessageRequestFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        public void RegisterTypedMessageResponseFilter<T>(Action<IRequest, IResponse, T> filterFn)
        {
            GlobalTypedMessageResponseFilters[typeof(T)] = new TypedFilter<T>(filterFn);
        }

        public virtual string MapProjectPath(string relativePath)
        {
            return relativePath.MapProjectPath();
        }

        public virtual string ResolvePathInfo(IRequest request, string originalPathInfo, out bool isDirectory)
        {
            var pathInfo = NormalizePathInfo(originalPathInfo, Config.HandlerFactoryPath);
            isDirectory = VirtualFileSources.DirectoryExists(pathInfo);

            if (!isDirectory && pathInfo.Length > 1 && pathInfo[pathInfo.Length - 1] == '/')
                pathInfo = pathInfo.TrimEnd('/');

            return pathInfo;
        }

        public static string NormalizePathInfo(string pathInfo, string mode)
        {
            if (string.IsNullOrEmpty(mode))
                return pathInfo;

            var pathNoPrefix = pathInfo[0] == '/'
                ? pathInfo.Substring(1)
                : pathInfo;

            var normalizedPathInfo = pathNoPrefix.StartsWith(mode)
                ? pathNoPrefix.Substring(mode.Length)
                : pathInfo;

            return normalizedPathInfo.Length > 0 && normalizedPathInfo[0] != '/'
                ? '/' + normalizedPathInfo
                : normalizedPathInfo;
        }

        public virtual IHttpHandler ReturnRedirectHandler(IHttpRequest httpReq)
        {
            var pathInfo = NormalizePathInfo(httpReq.OriginalPathInfo, Config.HandlerFactoryPath);
            return Config.RedirectPaths.TryGetValue(pathInfo, out string redirectPath)
                ? new RedirectHttpHandler { RelativeUrl = redirectPath }
                : null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //clear managed resources here
                foreach (var callback in OnDisposeCallbacks)
                {
                    callback(this);
                }

                if (Container != null)
                {
                    Container.Dispose();
                    Container = null;
                }

                Platform.Reset();
                if (HostContext.AppHost == this) HostContext.AppHost = null;
            }
            //clear unmanaged resources here
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ServiceStackHost()
        {
            Dispose(false);
        }
    }
}
