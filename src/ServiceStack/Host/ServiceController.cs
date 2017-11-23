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
using ServiceStack.Host.Handlers;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.NativeTypes;
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
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServiceController));

        private readonly ServiceStackHost appHost;

        public ServiceController(ServiceStackHost appHost)
            : this(appHost, appHost.ServiceAssemblies) { }

        public ServiceController(ServiceStackHost appHost, Assembly[] assembliesWithServices) 
            : this(appHost, assembliesWithServices, null) { }

        public ServiceController(ServiceStackHost appHost, Func<IEnumerable<Type>> resolveServicesFn) 
            : this(appHost, appHost.ServiceAssemblies, resolveServicesFn) { }

        public ServiceController(ServiceStackHost appHost, Assembly[] assembliesWithServices, Func<IEnumerable<Type>> resolveServicesFn)
        {
            this.appHost = appHost;
            appHost.Container.DefaultOwner = Owner.External;
            typeFactory = new ContainerResolveCache();
            this.RequestTypeFactoryMap = new Dictionary<Type, Func<IRequest, object>>();
            this.ResolveServicesFn = resolveServicesFn ?? (() => Service.GetServiceTypes(assembliesWithServices ?? appHost.ServiceAssemblies));
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
            foreach (var serviceType in Service.GetServiceTypes(assembly))
            {
                if (appHost.ExcludeAutoRegisteringServiceTypes.Contains(serviceType))
                    continue;

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

            if (!serviceActionMap.TryGetValue(serviceType, out Dictionary<Type, List<ActionContext>> actionMap))
            {
                var serviceExecDef = typeof(ServiceExec<>).MakeGenericType(serviceType);
                serviceExecDef.GetMethod("Reset", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { appHost });

                var property = serviceExecDef.GetProperty("ActionMap", BindingFlags.Public | BindingFlags.Static);
                actionMap = property.GetValue(null) as Dictionary<Type, List<ActionContext>>;

                var iserviceExec = (IServiceExec)serviceExecDef.CreateInstance();
                foreach (var requestType in actionMap.Keys)
                {
                    ServiceExecFn handlerFn = (req, dto) =>
                    {
                        var service = (serviceFactoryFn ?? typeFactory).CreateInstance(req, serviceType) as IService;

                        ServiceExecFn serviceExec = (reqCtx, requestDto) =>
                            iserviceExec.Execute(reqCtx, service, requestDto);

                        return ManagedServiceExec(serviceExec, service, req, dto);
                    };
                    AddToRequestExecMap(requestType, serviceType, handlerFn);
                    var responseType = actionMap[requestType].Select(p => p.ResponseType).FirstOrDefault();
                    appHost.Metadata.Add(serviceType, requestType, responseType);

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
                    if (Logger.IsDebugEnabled)
                        Logger.DebugFormat("Registering {0} service '{1}' with request '{2}'",
                            responseType != null ? "Reply" : "OneWay", serviceType.GetOperationName(), requestType.GetOperationName());
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
                var restPath = new RestPath(requestType, attr.Path, attr.Verbs, attr.Summary, attr.Notes, attr.Matches);

                if (attr is FallbackRouteAttribute defaultAttr)
                {
                    if (appHost.Config.FallbackRestPath != null)
                        throw new NotSupportedException(
                            "Config.FallbackRestPath is already defined. Only 1 [FallbackRoute] is allowed.");

                    appHost.Config.FallbackRestPath = httpReq => restPath.IsMatch(httpReq) ? restPath : null;

                    continue;
                }

                if (!restPath.IsValid)
                    throw new NotSupportedException(
                        $"RestPath '{attr.Path}' on Type '{requestType.GetOperationName()}' is not Valid");

                RegisterRestPath(restPath);
            }
        }

        private static readonly char[] InvalidRouteChars = { '?', '&' };

        public void RegisterRestPath(RestPath restPath)
        {
            if (!restPath.Path.StartsWith("/"))
                throw new ArgumentException($"Route '{restPath.Path}' on '{restPath.RequestType.GetOperationName()}' must start with a '/'");
            if (restPath.Path.IndexOfAny(InvalidRouteChars) != -1)
                throw new ArgumentException($"Route '{restPath.Path}' on '{restPath.RequestType.GetOperationName()}' contains invalid chars. " +
                                            "See http://docs.servicestack.net/routing for info on valid routes.");

            if (!RestPathMap.TryGetValue(restPath.FirstMatchHashKey, out var pathsAtFirstMatch))
            {
                pathsAtFirstMatch = new List<RestPath>();
                RestPathMap[restPath.FirstMatchHashKey] = pathsAtFirstMatch;
            }
            pathsAtFirstMatch.Add(restPath);

            if (!appHost.Metadata.OperationsMap.TryGetValue(restPath.RequestType, out Operation operation))
                return;

            operation.Routes.Add(restPath);
        }

        [Obsolete("Use GetRestPathForRequest(httpMethod, pathInfo, httpReq)")]
        public IRestPath GetRestPathForRequest(string httpMethod, string pathInfo) => GetRestPathForRequest(httpMethod, pathInfo, null);

        /// <summary>
        /// Get Best Matching Route. 
        /// </summary>
        /// <param name="httpMethod"></param>
        /// <param name="pathInfo"></param>
        /// <param name="httpReq">If not null, ensures any Route matches any [Route(Matches)]</param>
        /// <returns></returns>
        public RestPath GetRestPathForRequest(string httpMethod, string pathInfo, IHttpRequest httpReq)
        {
            var matchUsingPathParts = RestPath.GetPathPartsForMatching(pathInfo);

            List<RestPath> firstMatches;
            RestPath bestMatch = null;

            var yieldedHashMatches = RestPath.GetFirstMatchHashKeys(matchUsingPathParts);
            foreach (var potentialHashMatch in yieldedHashMatches)
            {
                if (!this.RestPathMap.TryGetValue(potentialHashMatch, out firstMatches)) continue;

                var bestScore = -1;
                foreach (var restPath in firstMatches)
                {
                    var matchScore = 0;
                    //Handle [Route(Matches)]
                    if (httpReq != null)
                    {
                        var matchFn = restPath.GetRequestRule();
                        if (matchFn != null)
                        {
                            var validRoute = matchFn(httpReq);
                            if (!validRoute)
                                continue;

                            matchScore = 1;
                        }
                    }
                    
                    var score = restPath.MatchScore(httpMethod, matchUsingPathParts) + matchScore;
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
                throw new AmbiguousMatchException(
                    $"Could not register Request '{requestType.FullName}' with service '{serviceType.FullName}' as it has already been assigned to another service.\n" +
                    "Each Request DTO can only be handled by 1 service.");          

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
                    if (response is Task taskResponse)
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

            if (service is IRequiresRequest serviceRequiresContext)
            {
                serviceRequiresContext.Request = req;
            }
        }

        public object ApplyResponseFilters(object response, IRequest req)
        {
            var task = ApplyResponseFiltersAsync(response, req);
            task.Wait();
            return task.Result;
        }

        private async Task<object> ApplyResponseFiltersAsync(object response, IRequest req)
        {
            if (response is Task taskResponse)
            {
                await taskResponse;
                response = taskResponse.GetResult();
            }

            response = await appHost.ApplyResponseConvertersAsync(req, response);

            await appHost.ApplyResponseFiltersAsync(req, req.Response, response);
            if (req.Response.IsClosed)
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

            req.Dto = appHost.ApplyRequestConvertersAsync(req, message.Body).Result;
            if (appHost.ApplyMessageRequestFilters(req, req.Response, message.Body))
                return req.Response.Dto;

            var response = Execute(message.Body, req);

            if (response is Task taskResponse)
                response = taskResponse.GetResult();

            response = appHost.ApplyResponseConvertersAsync(req, response).Result;

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
            return Execute(requestDto, new BasicRequest(requestDto));
        }

        /// <summary>
        /// External HTTP Request called from HTTP handlers
        /// </summary>
        public virtual object Execute(object requestDto, IRequest req)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();

            if (appHost.Config.EnableAccessRestrictions)
                AssertServiceRestrictions(requestType, req.RequestAttributes);

            var handlerFn = GetService(requestType);
            return appHost.OnAfterExecute(req, requestDto, handlerFn(req, requestDto));
        }

        /// <summary>
        /// Execute a Service with a Request DTO.
        /// </summary>
        public virtual async Task<object> ExecuteAsync(object requestDto, IRequest req) //Used by HTTP handlers to Execute Services
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();

            if (appHost.Config.EnableAccessRestrictions)
                AssertServiceRestrictions(requestType, req.RequestAttributes);

            var handlerFn = GetService(requestType);
            var response = handlerFn(req, requestDto);
            if (response is Task responseTask)
            {
                await responseTask;
                response = responseTask.GetResult();
            }
            return appHost.OnAfterExecute(req, requestDto, response);
        }

        // Only Used internally by TypedFilterTests 
        public object Execute(object requestDto, IRequest req, bool applyFilters)
        {
            try
            {
                req.SetInProcessRequest();

                if (applyFilters)
                {
                    var task = appHost.ApplyRequestConvertersAsync(req, requestDto);
                    task.Wait();
                    requestDto = task.Result;

                    appHost.ApplyRequestFiltersAsync(req, req.Response, requestDto).Wait();
                    if (req.Response.IsClosed)
                        return null;
                }

                var response = Execute(requestDto, req);

                return applyFilters
                    ? ApplyResponseFilters(response, req)
                    : response;
            }
            finally
            {
                req.ReleaseIfInProcessRequest();
            }
        }

        public object Execute(IRequest req, bool applyFilters)
        {
            try
            {
                req.SetInProcessRequest();

                var restPath = HostContext.ServiceController.GetRestPathForRequest(req.Verb, req.PathInfo, req as IHttpRequest);

                req.OperationName = restPath.RequestType.GetOperationName();
                var task = RestHandler.CreateRequestAsync(req, restPath);
                task.Wait();
                var requestDto = task.Result;
                req.Dto = requestDto;

                if (applyFilters)
                {
                    requestDto = appHost.ApplyRequestConverters(req, requestDto);

                    appHost.ApplyRequestFiltersAsync(req, req.Response, requestDto).Wait();
                    if (req.Response.IsClosed)
                        return null;
                }

                var response = Execute(requestDto, req);

                return applyFilters
                    ? ApplyResponseFilters(response, req)
                    : response;
            }
            finally
            {
                req.ReleaseIfInProcessRequest();
            }
        }

        public async Task<object> GatewayExecuteAsync(object requestDto, IRequest req, bool applyFilters)
        {
            req.Dto = requestDto;
            var requestType = requestDto.GetType();

            if (appHost.Config.EnableAccessRestrictions)
            {
                AssertServiceRestrictions(requestType, req.RequestAttributes);
            }

            if (applyFilters)
            {
                requestDto = appHost.ApplyRequestConvertersAsync(req, requestDto);
                appHost.ApplyRequestFiltersAsync(req, req.Response, requestDto).Wait();
                if (req.Response.IsClosed)
                    return null;
            }

            var handlerFn = GetService(requestType);
            var response = handlerFn(req, requestDto);

            if (response is Task<object> taskObj)
            {
                response = await taskObj;

                if (response is Task[] tasks)
                {
                    await Task.WhenAll(tasks);

                    object[] ret = null;
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        var tResult = tasks[i].GetResult();
                        if (ret == null)
                            ret = (object[])Array.CreateInstance(tResult.GetType(), tasks.Length);

                        ret[i] = await ApplyResponseFiltersAsync(tResult, req);
                    }
                    return ret;
                }

                return await ApplyResponseFiltersAsync(response, req);
            }

            return applyFilters
                ? await ApplyResponseFiltersAsync(response, req)
                : response;
        }

        public virtual ServiceExecFn GetService(Type requestType)
        {
            if (!requestExecMap.TryGetValue(requestType, out var handlerFn))
            {
                if (requestType.IsArray)
                {
                    var elType = requestType.GetElementType();
                    if (requestExecMap.TryGetValue(elType, out handlerFn))
                    {
                        return requestExecMap[requestType] = CreateAutoBatchServiceExec(handlerFn);
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
                var dtosList = ((IEnumerable)dtos).Map(x => x);
                if (dtosList.Count == 0)
                    return TypeConstants.EmptyObjectArray;

                var firstDto = dtosList[0];

                var firstResponse = handlerFn(req, firstDto);
                if (firstResponse is Exception)
                {
                    req.SetAutoBatchCompletedHeader(0);
                    return firstResponse;
                }

                //sync
                if (!(firstResponse is Task asyncResponse)) 
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
                var batchResponse = HostContext.Async.ContinueWith(req, task, x => {
                    if (firstAsyncError != null)
                        return (object)firstAsyncError;

                    req.SetAutoBatchCompletedHeader(dtosList.Count);
                    return (object) asyncResponses;
                }); //return error or completed responses

                return batchResponse;
            };
        }

        public void AssertServiceRestrictions(Type requestType, RequestAttributes actualAttributes)
        {
            if (!appHost.Config.EnableAccessRestrictions) return;
            if ((RequestAttributes.InProcess & actualAttributes) == RequestAttributes.InProcess) return;

            var hasNoAccessRestrictions = !requestServiceAttrs.TryGetValue(requestType, out var restrictAttr)
                || restrictAttr.HasNoAccessRestrictions;

            if (hasNoAccessRestrictions)
                return;

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
    }

}