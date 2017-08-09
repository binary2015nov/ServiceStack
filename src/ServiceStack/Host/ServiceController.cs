using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Funq;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.Serialization;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public delegate object ServiceExecFn(IRequest request, object requestDto);
    public delegate object InstanceExecFn(IRequest request, object intance, object requestDto);
    public delegate object ActionInvokerFn(object intance, object requestDto);
    public delegate void VoidActionInvokerFn(object intance, object requestDto);

    public class ServiceController : IServiceController
    {
        private static ILog Logger = LogManager.GetLogger(typeof(ServiceController));

        private readonly ServiceStackHost appHost;

        public ServiceController(ServiceStackHost appHost) : this(appHost, appHost.ServiceAssemblies) { }

        public ServiceController(ServiceStackHost appHost, Assembly[] assembliesWithServices) : this(appHost, assembliesWithServices, null) { }

        public ServiceController(ServiceStackHost appHost, Func<IEnumerable<Type>> resolveServicesFn) : this(appHost, appHost.ServiceAssemblies, resolveServicesFn) { }

        public ServiceController(ServiceStackHost appHost, Assembly[] assembliesWithServices, Func<IEnumerable<Type>> resolveServicesFn)
        {
            this.appHost = appHost;
            appHost.Container.DefaultOwner = Owner.External;
            typeFactory = new ContainerResolveCache(appHost.Container);
            this.RequestTypeFactoryMap = new Dictionary<Type, Func<IRequest, object>>();
            this.ResolveServicesFn = resolveServicesFn ?? (() => GetServiceTypes(assembliesWithServices));
        }

        readonly Dictionary<Type, ServiceExecFn> requestExecMap
            = new Dictionary<Type, ServiceExecFn>();

        readonly Dictionary<Type, RestrictAttribute> requestServiceAttrs
            = new Dictionary<Type, RestrictAttribute>();

        public Dictionary<Type, Func<IRequest, object>> RequestTypeFactoryMap { get; set; }

        public string DefaultOperationsNamespace { get; set; }

        public Func<IEnumerable<Type>> ResolveServicesFn { get; set; }

        private ContainerResolveCache typeFactory;

        public ServiceController Init()
        {
            foreach (var serviceType in ResolveServicesFn())
            {
                RegisterService(serviceType);
            }
            return this;
        }

        public void RegisterServicesInAssembly(Assembly assembly)
        {
            foreach (var serviceType in GetServiceTypes(assembly))
            {
                RegisterService(serviceType);
            }
        }

        public void RegisterService(Type serviceType)
        {
            RegisterService(serviceType, typeFactory);
        }

        private readonly Dictionary<Type, Dictionary<Type, List<ActionContext>>> serviceActionMap = new Dictionary<Type, Dictionary<Type, List<ActionContext>>>();

        public void RegisterService(Type serviceType, ITypeFactory serviceFactoryFn)
        {
            if (!Service.IsServiceType(serviceType))
                throw new ArgumentException($"{serviceType.FullName} is not a service type that implements IService");

            Dictionary<Type, List<ActionContext>> actionMap;
            if (!serviceActionMap.TryGetValue(serviceType, out actionMap))
            {
                var serviceExecDef = typeof(ServiceExec<>).MakeGenericType(serviceType);
                serviceExecDef.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { });

                var property = serviceExecDef.GetProperty("ActionMap", BindingFlags.Public | BindingFlags.Static);
                actionMap = property.GetValue(null) as Dictionary<Type, List<ActionContext>>;

                var iserviceExec = (IServiceExec)serviceExecDef.CreateInstance();
                foreach (var requestType in actionMap.Keys)
                {
                    ServiceExecFn handlerFn = (req, dto) =>
                    {
                        var service = serviceFactoryFn.CreateInstance(req, serviceType) as IService;

                        ServiceExecFn serviceExec = (reqCtx, requestDto) =>
                            iserviceExec.Execute(reqCtx, service, requestDto);

                        return ManagedServiceExec(serviceExec, service, req, dto);
                    };
                    AddToRequestExecMap(requestType, serviceType, handlerFn);
                    RegisterRestPaths(requestType);
                    if (typeof(IRequiresRequestStream).IsAssignableFrom(requestType))
                    {
                        this.RequestTypeFactoryMap[requestType] = req =>
                        {
                            var restPath = req.GetRoute();
                            var request = restPath != null
                                ? RestHandler.CreateRequest(req, restPath, req.GetRequestParams(), requestType.CreateInstance())
                                : KeyValueDataContractDeserializer.Instance.Parse(req.QueryString, requestType);

                            var rawReq = (IRequiresRequestStream)request;
                            rawReq.RequestStream = req.InputStream;
                            return rawReq;
                        };
                    }
                    foreach (var actionContext in actionMap[requestType])
                    {
                        Type responseType = actionContext.ResponseType;
                        appHost.Metadata.Add(serviceType, requestType, responseType);
                        if (Logger.IsDebugEnabled)
                            Logger.DebugFormat("Registering {0} service '{1}' with request '{2}'",
                                responseType != null ? "Reply" : "OneWay", serviceType.GetOperationName(), requestType.GetOperationName());
                    }
                }
                serviceActionMap[serviceType] = actionMap;
                appHost.Container.RegisterAutoWiredType(serviceType);
            }
        }

        public readonly Dictionary<string, List<RestPath>> RestPathMap = new Dictionary<string, List<RestPath>>();

        public void RegisterRestPaths(Type requestType)
        {
            var attrs = appHost.GetRouteAttributes(requestType);
            foreach (RouteAttribute attr in attrs)
            {
                var restPath = new RestPath(requestType, attr.Path, attr.Verbs, attr.Summary, attr.Notes);

                var defaultAttr = attr as FallbackRouteAttribute;
                if (defaultAttr != null)
                {
                    if (appHost.Config.FallbackRestPath != null)
                        throw new NotSupportedException(
                            "Config.FallbackRestPath is already defined. Only 1 [FallbackRoute] is allowed.");

                    appHost.Config.FallbackRestPath = (httpMethod, pathInfo, filePath) =>
                    {
                        var pathInfoParts = RestPath.GetPathPartsForMatching(pathInfo);
                        return restPath.IsMatch(httpMethod, pathInfoParts) ? restPath : null;
                    };

                    continue;
                }

                if (!restPath.IsValid)
                    throw new NotSupportedException(
                        $"RestPath '{attr.Path}' on Type '{requestType.GetOperationName()}' is not Valid");

                RegisterRestPath(restPath);
            }
        }

        private static readonly char[] InvalidRouteChars = new[] { '?', '&' };

        public void RegisterRestPath(RestPath restPath)
        {
            if (!restPath.Path.StartsWith("/"))
                throw new ArgumentException($"Route '{restPath.Path}' on '{restPath.RequestType.GetOperationName()}' must start with a '/'");
            if (restPath.Path.IndexOfAny(InvalidRouteChars) != -1)
                throw new ArgumentException($"Route '{restPath.Path}' on '{restPath.RequestType.GetOperationName()}' contains invalid chars. " +
                                            "See https://github.com/ServiceStack/ServiceStack/wiki/Routing for info on valid routes.");

            List<RestPath> pathsAtFirstMatch;
            if (!RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out pathsAtFirstMatch))
            {
                pathsAtFirstMatch = new List<RestPath>();
                RestPathMap[restPath.FirstMatchHashKey] = pathsAtFirstMatch;
            }
            pathsAtFirstMatch.Add(restPath);
        }

        public IRestPath GetRestPathForRequest(string httpMethod, string pathInfo)
        {
            var matchUsingPathParts = RestPath.GetPathPartsForMatching(pathInfo);

            List<RestPath> firstMatches;
            IRestPath bestMatch = null;

            var yieldedHashMatches = RestPath.GetFirstMatchHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedHashMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                foreach (var restPath in firstMatches)
                {
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
                    if (score > bestScore) 
                    {
                        bestScore = score;
                        bestMatch = restPath;
                    }
                }
                if (bestScore > 0)
                {
                    return bestMatch;
                }
            }

            var yieldedWildcardMatches = RestPath.GetFirstMatchWildCardHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedWildcardMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                foreach (var restPath in firstMatches)
                {
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = restPath;
                    }
                }
                if (bestScore > 0)
                {
                    return bestMatch;
                }
            }

            return null;
        }

        private void AddToRequestExecMap(Type requestType, Type serviceType, ServiceExecFn handlerFn)
        {
            if (requestExecMap.ContainsKey(requestType))
            {
                throw new AmbiguousMatchException(
                    $"Could not register Request '{requestType.FullName}' with service '{serviceType.FullName}' as it has already been assigned to another service.\n" +
                    "Each Request DTO can only be handled by 1 service.");
            }

            requestExecMap.Add(requestType, handlerFn);

            var requestAttrs = requestType.AllAttributes<RestrictAttribute>();
            if (requestAttrs.Length > 0)
            {
                requestServiceAttrs[requestType] = requestAttrs[0];
            }
            else
            {
                var serviceAttrs = serviceType.AllAttributes<RestrictAttribute>();
                if (serviceAttrs.Length > 0)
                {
                    requestServiceAttrs[requestType] = serviceAttrs[0];
                }
            }
        }

        private object ManagedServiceExec(ServiceExecFn serviceExec, IService service, IRequest request, object requestDto)
        {
            try
            {
                InjectRequestContext(service, request);

                object response = null;
                try
                {
                    requestDto = appHost.OnPreExecuteServiceFilter(service, requestDto, request, request.Response);

                    if (request.Dto == null) // Don't override existing batched DTO[]
                        request.Dto = requestDto; 

                    //Executes the service and returns the result
                    response = serviceExec(request, requestDto);

                    response = appHost.OnPostExecuteServiceFilter(service, response, request, request.Response);

                    return response;
                }
                finally
                {
                    //Gets disposed by AppHost or ContainerAdapter if set
                    var taskResponse = response as Task;
                    if (taskResponse != null)
                    {
                        HostContext.Async.ContinueWith(request, taskResponse, task => appHost.Release(service));
                    }
                    else
                    {
                        appHost.Release(service);
                    }
                }
            }
            catch (TargetInvocationException tex)
            {
                //Mono invokes using reflection
                throw tex.InnerException ?? tex;
            }
        }

        internal static void InjectRequestContext(object service, IRequest req)
        {
            if (req == null) return;

            var serviceRequiresContext = service as IRequiresRequest;
            if (serviceRequiresContext != null)
            {
                serviceRequiresContext.Request = req;
            }
        }

        public object ApplyResponseFilters(object response, IRequest req)
        {
            var taskResponse = response as Task;
            if (taskResponse != null)
            {
                response = taskResponse.GetResult();
            }

            return ApplyResponseFiltersInternal(response, req);
        }

        private object ApplyResponseFiltersInternal(object response, IRequest req)
        {
            response = appHost.ApplyResponseConverters(req, response);

            if (appHost.ApplyResponseFilters(req, req.Response, response))
                return req.Response.Dto;

            return response;
        }

        /// <summary>
        /// Execute MQ
        /// </summary>
        public object ExecuteMessage(IMessage mqMessage)
        {
            return ExecuteMessage(mqMessage, new BasicRequest(mqMessage));
        }

        /// <summary>
        /// Execute MQ with requestContext
        /// </summary>
        public object ExecuteMessage(IMessage message, IRequest req)
        {
            RequestContext.Instance.StartRequestContext();
            
            req.PopulateFromRequestIfHasSessionId(message.Body);

            req.Dto = appHost.ApplyRequestConverters(req, message.Body);
            if (appHost.ApplyMessageRequestFilters(req, req.Response, message.Body))
                return req.Response.Dto;

            var response = Execute(message.Body, req);

            var taskResponse = response as Task;
            if (taskResponse != null)
                response = taskResponse.GetResult();

            response = appHost.ApplyResponseConverters(req, response);

            if (appHost.ApplyMessageResponseFilters(req, req.Response, response))
                response = req.Response.Dto;

            req.Response.EndMqRequest();

            return response;
        }

        /// <summary>
        /// Execute using empty RequestContext
        /// </summary>
        public object Execute(object requestDto)
        {
            return Execute(requestDto, new BasicRequest());
        }

        public virtual object Execute(object requestDto, IRequest req)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();

            if (appHost.Config.EnableAccessRestrictions)
                AssertServiceRestrictions(requestType, req.RequestAttributes);

            var handlerFn = GetService(requestType);
            var response = appHost.OnAfterExecute(req, requestDto, handlerFn(req, requestDto));
            return response;
        }

        public object Execute(object requestDto, IRequest req, bool applyFilters)
        {
            if (applyFilters)
            {
                requestDto = appHost.ApplyRequestConverters(req, requestDto);
                if (appHost.ApplyRequestFilters(req, req.Response, requestDto))
                    return null;
            }

            var response = Execute(requestDto, req);

            return applyFilters
                ? ApplyResponseFilters(response, req)
                : response;
        }

        [Obsolete("Use Execute(IRequest, applyFilters:true)")]
        public object Execute(IRequest req)
        {
            return Execute(req, applyFilters:true);
        }

        public object Execute(IRequest req, bool applyFilters)
        {
            string contentType;
            var restPath = RestHandler.FindMatchingRestPath(req.Verb, req.PathInfo, out contentType);
            req.SetRoute(restPath as RestPath);
            req.OperationName = restPath.RequestType.GetOperationName();
            var requestDto = RestHandler.CreateRequest(req, restPath);
            req.Dto = requestDto;

            if (applyFilters)
            {
                requestDto = appHost.ApplyRequestConverters(req, requestDto);
                if (appHost.ApplyRequestFilters(req, req.Response, requestDto))
                    return null;
            }

            var response = Execute(requestDto, req);

            return applyFilters 
                ? ApplyResponseFilters(response, req) 
                : response;
        }

        public Task<object> ExecuteAsync(object requestDto, IRequest req, bool applyFilters)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();

            if (appHost.Config.EnableAccessRestrictions)
            {
                AssertServiceRestrictions(requestType, req.RequestAttributes);
            }

            if (applyFilters)
            {
                requestDto = appHost.ApplyRequestConverters(req, requestDto);
                if (appHost.ApplyRequestFilters(req, req.Response, requestDto))
                    return TypeConstants.EmptyTask;
            }

            var handlerFn = GetService(requestType);
            var response = handlerFn(req, requestDto);

            var taskObj = response as Task<object>;
            if (taskObj != null)
            {
                return HostContext.Async.ContinueWith(req, taskObj, t => 
                {
                    var taskArray = t.Result as Task[];
                    if (taskArray != null)
                    {
                        return Task.Factory.ContinueWhenAll(taskArray, tasks =>
                        {
                            object[] ret = null;
                            for (int i = 0; i < tasks.Length; i++)
                            {
                                var tResult = tasks[i].GetResult();
                                if (ret == null)
                                    ret = (object[])Array.CreateInstance(tResult.GetType(), tasks.Length);

                                ret[i] = ApplyResponseFiltersInternal(tResult, req);
                            }
                            return (object)ret;
                        });
                    }

                    return ApplyResponseFiltersInternal(t.Result, req).AsTaskResult();
                }).Unwrap();
            }

            return applyFilters
                ? ApplyResponseFiltersInternal(response, req).AsTaskResult()
                : response.AsTaskResult();
        }

        public virtual ServiceExecFn GetService(Type requestType)
        {
            ServiceExecFn handlerFn;
            if (!requestExecMap.TryGetValue(requestType, out handlerFn))
            {
                if (requestType.IsArray)
                {
                    var elType = requestType.GetElementType();
                    if (requestExecMap.TryGetValue(elType, out handlerFn))
                    {
                        return CreateAutoBatchServiceExec(handlerFn);
                    }
                }

                throw new NotImplementedException($"Unable to resolve service '{requestType.GetOperationName()}'");
            }

            return handlerFn;
        }

        private static ServiceExecFn CreateAutoBatchServiceExec(ServiceExecFn handlerFn)
        {
            return (req, dtos) => 
            {
                var dtosList = ((IEnumerable) dtos).Map(x => x);
                if (dtosList.Count == 0)
                    return TypeConstants.EmptyObjectArray;

                var firstDto = dtosList[0];

                var firstResponse = handlerFn(req, firstDto);
                if (firstResponse is Exception)
                {
                    req.SetAutoBatchCompletedHeader(0);
                    return firstResponse;
                }

                var asyncResponse = firstResponse as Task;

                //sync
                if (asyncResponse == null) 
                {
                    var ret = firstResponse != null
                        ? (object[])Array.CreateInstance(firstResponse.GetType(), dtosList.Count)
                        : new object[dtosList.Count];

                    ret[0] = firstResponse; //don't re-execute first request
                    for (var i = 1; i < dtosList.Count; i++)
                    {
                        var dto = dtosList[i];
                        var response = handlerFn(req, dto);
                        //short-circuit on first error
                        if (response is Exception)
                        {
                            req.SetAutoBatchCompletedHeader(i);
                            return response;
                        }

                        ret[i] = response;
                    }
                    req.SetAutoBatchCompletedHeader(dtosList.Count);
                    return ret;
                }

                //async
                var asyncResponses = new Task[dtosList.Count];
                Task firstAsyncError = null;

                //execute each async service sequentially
                var task = dtosList.EachAsync((dto, i) =>
                {
                    //short-circuit on first error and don't exec any more handlers
                    if (firstAsyncError != null)
                        return firstAsyncError;

                    asyncResponses[i] = i == 0
                        ? asyncResponse //don't re-execute first request
                        : (Task) handlerFn(req, dto);

                    var asyncResult = asyncResponses[i].GetResult();
                    if (asyncResult is Exception)
                    {
                        req.SetAutoBatchCompletedHeader(i);
                        return firstAsyncError = asyncResponses[i];
                    }
                    return asyncResponses[i];
                });
                return HostContext.Async.ContinueWith(req, task, x => {
                    if (firstAsyncError != null)
                        return (object)firstAsyncError;

                    req.SetAutoBatchCompletedHeader(dtosList.Count);
                    return (object) asyncResponses;
                }); //return error or completed responses
            };
        }

        public void AssertServiceRestrictions(Type requestType, RequestAttributes actualAttributes)
        {
            if (!appHost.Config.EnableAccessRestrictions) return;
            if ((RequestAttributes.InProcess & actualAttributes) == RequestAttributes.InProcess) return;

            RestrictAttribute restrictAttr;
            var hasNoAccessRestrictions = !requestServiceAttrs.TryGetValue(requestType, out restrictAttr)
                || restrictAttr.HasNoAccessRestrictions;

            if (hasNoAccessRestrictions)
            {
                return;
            }

            var failedScenarios = StringBuilderCache.Allocate();
            foreach (var requiredScenario in restrictAttr.AccessibleToAny)
            {
                var allServiceRestrictionsMet = (requiredScenario & actualAttributes) == actualAttributes;
                if (allServiceRestrictionsMet)
                {
                    return;
                }

                var passed = requiredScenario & actualAttributes;
                var failed = requiredScenario & ~(passed);

                failedScenarios.Append($"\n -[{failed}]");
            }

            var internalDebugMsg = (RequestAttributes.InternalNetworkAccess & actualAttributes) != 0
                ? "\n Unauthorized call was made from: " + actualAttributes
                : "";

            throw new UnauthorizedAccessException(
                $"Could not execute service '{requestType.GetOperationName()}', The following restrictions were not met: " +
                $"'{StringBuilderCache.Retrieve(failedScenarios)}'{internalDebugMsg}");
        }

        public static List<Type> GetServiceTypes(params Assembly[] assembliesWithServices)
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
                    foreach (var type in assembly.GetTypes().Where(Service.IsServiceType))
                    {
                        if (HostContext.AppHost != null && HostContext.AppHost.ExcludeAutoRegisteringServiceTypes.Contains(type))
                            continue;

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
    }
}