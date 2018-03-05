using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Redis;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Generic + Useful IService base class
    /// </summary>
    public class Service : IService, IServiceBase, IDisposable
    {
        public static IResolver GlobalResolver { get; set; }

        public static bool IsServiceType(Type type)
        {
            return typeof(IService).IsAssignableFrom(type) 
                && !type.IsAbstract && !type.IsGenericTypeDefinition && !type.ContainsGenericParameters;
        }

        public static IEnumerable<Type> GetServiceTypes(params Assembly[] assembliesWithServices)
        {
            if (assembliesWithServices == null || assembliesWithServices.Length == 0)
                throw new ArgumentException("No Assemblies provided to extract the service.\n"
                    + "To register your services, please provide the assemblies where your services are defined.");

            string assemblyName = string.Empty, typeName = string.Empty;
            try
            {
                var results = new List<Type>();
                foreach (var assembly in assembliesWithServices)
                {
                    assemblyName = assembly.FullName;
                    foreach (var type in assembly.GetTypes().Where(IsServiceType))
                    {
                        typeName = type.GetOperationName();
                        results.Add(type);
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new TypeLoadException($"Failed loading types, last assembly '{assemblyName}', type: '{typeName}'", ex);
            }
        }

        public static IEnumerable<MethodInfo> GetActions(Type type)
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (methodInfo.IsGenericMethod || methodInfo.GetParameters().Length != 1)
                    continue;

                var paramType = methodInfo.GetParameters()[0].ParameterType;
                if (paramType.IsValueType || paramType == typeof(string))
                    continue;

                string actionName = methodInfo.Name.ToUpper();
                if (!HttpMethods.AllVerbs.Contains(actionName) && actionName != ActionContext.AnyAction &&
                !HttpMethods.AllVerbs.Any(verb => ContentTypes.KnownFormats.Any(format => actionName.EqualsIgnoreCase(verb + format))) &&
                !ContentTypes.KnownFormats.Any(format => actionName.EqualsIgnoreCase(ActionContext.AnyAction + format)))
                    continue;

                yield return methodInfo;
            }
        }

        private IResolver resolver;
        public virtual IResolver GetResolver() => resolver ?? GlobalResolver;

        public virtual Service SetResolver(IResolver resolver)
        {
            this.resolver = resolver;
            return this;
        }

        public virtual T TryResolve<T>()
        {
            return this.GetResolver() == null
                ? default(T)
                : this.GetResolver().TryResolve<T>();
        }

        public virtual T ResolveService<T>()
        {
            var service = TryResolve<T>();
            return HostContext.ResolveService(this.Request, service);
        }

        public IRequest Request { get; set; }

        protected virtual IResponse Response => Request?.Response;

        private ICacheClient cache;
        public virtual ICacheClient Cache => cache ?? (cache = HostContext.AppHost.GetCacheClient(Request));

        private MemoryCacheClient localCache;
        /// <summary>
        /// Returns <see cref="MemoryCacheClient"></see>. cache is only persisted for this running app instance.
        /// </summary>
        public virtual MemoryCacheClient LocalCache => localCache ?? (localCache = HostContext.AppHost.GetMemoryCacheClient(Request));

        private IDbConnection db;
        public virtual IDbConnection Db => db ?? (db = HostContext.AppHost.GetDbConnection(Request));

        private IRedisClient redis;
        public virtual IRedisClient Redis => redis ?? (redis = HostContext.AppHost.GetRedisClient(Request));

        private IMessageProducer messageProducer;
        public virtual IMessageProducer MessageProducer => messageProducer ?? (messageProducer = HostContext.AppHost.GetMessageProducer(Request));

        private ISessionFactory sessionFactory;
        public virtual ISessionFactory SessionFactory => sessionFactory ?? (sessionFactory = TryResolve<ISessionFactory>()) ?? new SessionFactory(Cache);

        private IAuthRepository authRepository;
        public virtual IAuthRepository AuthRepository => authRepository ?? (authRepository = HostContext.AppHost.GetAuthRepository(Request));

        private IServiceGateway gateway;
        public virtual IServiceGateway Gateway => gateway ?? (gateway = HostContext.AppHost.GetServiceGateway(Request));

        /// <summary>
        /// Cascading collection of virtual file sources, inc. Embedded Resources, File System, In Memory, S3
        /// </summary>
        public IVirtualPathProvider VirtualFileSources => HostContext.VirtualFileSources;

        /// <summary>
        /// Read/Write Virtual FileSystem. Defaults to FileSystemVirtualPathProvider
        /// </summary>
        public IVirtualFiles VirtualFiles => HostContext.VirtualFiles;

        /// <summary>
        /// Dynamic Session Bag
        /// </summary>
        private ISession session;
        public virtual ISession SessionBag => session ?? (session = TryResolve<ISession>() //Easier to mock
            ?? SessionFactory.GetOrCreateSession(Request, Response));

        /// <summary>
        /// Typed UserSession
        /// </summary>
        protected virtual TUserSession SessionAs<TUserSession>()
        {
            if (HostContext.TestMode)
            {
                var mockSession = TryResolve<TUserSession>();
                if (Equals(mockSession, default(TUserSession)))
                    mockSession = TryResolve<IAuthSession>() is TUserSession 
                        ? (TUserSession)TryResolve<IAuthSession>() 
                        : default(TUserSession);

                if (!Equals(mockSession, default(TUserSession)))
                    return mockSession;
            }

            return SessionFeature.GetOrCreateSession<TUserSession>(Cache, Request, Response);
        }

        /// <summary>
        /// If user found in session for this request is authenticated.
        /// </summary>
        public virtual bool IsAuthenticated => this.GetSession().IsAuthenticated;

        /// <summary>
        /// Publish a MQ message over the <see cref="IMessageProducer"></see> implementation.
        /// </summary>
        public virtual void PublishMessage<T>(T message)
        {
            if (MessageProducer == null)
                throw new NullReferenceException("No IMessageFactory was registered, cannot PublishMessage");

            MessageProducer.Publish(message);
        }

        /// <summary>
        /// Disposes all created disposable properties of this service
        /// and executes disposing of all request <see cref="IDposable"></see>s 
        /// (warning, manualy triggering this might lead to unwanted disposing of all request related objects and services.)
        /// </summary>
        public virtual void Dispose()
        {
            db?.Dispose();
            redis?.Dispose();
            messageProducer?.Dispose();
            using (authRepository as IDisposable) { }

            RequestContext.Instance.ReleaseDisposables();

            Request.ReleaseIfInProcessRequest();
        }
    }
}
