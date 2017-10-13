//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public interface IServiceExec
    {
        object Execute(IRequest request, IService service, object requestDto);
    }

    class ServiceExec<TService> : IServiceExec where TService : IService
    {
        private const string ResponseDtoSuffix = "Response";

        public static Dictionary<Type, List<ActionContext>> ActionMap { get; private set; }

        private static Dictionary<string, InstanceExecFn> execMap;

        public static void Reset(IAppHost appHost)
        {
            ActionMap = new Dictionary<Type, List<ActionContext>>();
            execMap = new Dictionary<string, InstanceExecFn>(StringComparer.OrdinalIgnoreCase);

            foreach (var mi in Service.GetActions(typeof(TService)))
            {
                var actionName = mi.Name.ToUpper();
                var args = mi.GetParameters();

                var requestType = args[0].ParameterType;
                var actionCtx = new ActionContext
                {
                    Id = ActionContext.Key(actionName, requestType.GetOperationName()),
                    ServiceType = typeof(TService),
                    RequestType = requestType,
                };

                try
                {
                    actionCtx.ServiceAction = CreateExecFn(requestType, mi);
                }
                catch
                {
                    //Potential problems with MONO, using reflection for fallback
                    actionCtx.ServiceAction = (service, request) =>
                                              mi.Invoke(service, new[] { request });
                }
                var returnMarker = requestType.GetTypeWithGenericTypeDefinitionOf(typeof(IReturn<>));
                var responseType = returnMarker != null ?
                    returnMarker.GetGenericArguments()[0]
                    : mi.ReturnType != typeof(object) && mi.ReturnType != typeof(void) ?
                      mi.ReturnType
#if NETSTANDARD2_0
                    : Type.GetType(requestType.FullName + ResponseDtoSuffix + "," + requestType.GetAssembly().GetName().Name);
#else
                    : AssemblyUtils.FindType(requestType.FullName + ResponseDtoSuffix);
#endif
                if (responseType?.Name == "Task`1" && responseType.GetGenericArguments()[0] != typeof(object))
                    responseType = responseType.GetGenericArguments()[0];

                actionCtx.ResponseType = responseType;
                var reqFilters = new List<IRequestFilterBase>();
                var resFilters = new List<IResponseFilterBase>();

                foreach (var attr in mi.GetCustomAttributes(true))
                {
                    var hasReqFilter = attr as IRequestFilterBase;
                    var hasResFilter = attr as IResponseFilterBase;

                    if (hasReqFilter != null)
                        reqFilters.Add(hasReqFilter);

                    if (hasResFilter != null)
                        resFilters.Add(hasResFilter);
                }

                if (reqFilters.Count > 0)
                    actionCtx.RequestFilters = reqFilters.ToArray();

                if (resFilters.Count > 0)
                    actionCtx.ResponseFilters = resFilters.ToArray();

                if (!ActionMap.ContainsKey(requestType))
                    ActionMap[requestType] = new List<ActionContext>();

                ActionMap[requestType].Add(actionCtx);
            }
            foreach (var item in ActionMap)
            {
                var mi = appHost.GetType().GetMethod("CreateServiceRunner", BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(item.Key);
                foreach (var actionCtx in item.Value)
                {
                    var serviceRunner = (IServiceRunner)mi.Invoke(appHost, new object[] { actionCtx });
                    execMap[actionCtx.Id] = serviceRunner.Process;
                }
            }
        }

        private static ActionInvokerFn CreateExecFn(Type requestType, MethodInfo mi)
        {
            var serviceType = typeof(TService);

            var serviceParam = Expression.Parameter(typeof(object), "serviceObj");
            var serviceStrong = Expression.Convert(serviceParam, serviceType);

            var requestDtoParam = Expression.Parameter(typeof(object), "requestDto");
            var requestDtoStrong = Expression.Convert(requestDtoParam, requestType);

            Expression callExecute = Expression.Call(
                serviceStrong, mi, requestDtoStrong);

            if (mi.ReturnType != typeof(void))
            {
                var executeFunc = Expression.Lambda<ActionInvokerFn>
                (callExecute, serviceParam, requestDtoParam).Compile();

                return executeFunc;
            }
            else
            {
                var executeFunc = Expression.Lambda<VoidActionInvokerFn>
                (callExecute, serviceParam, requestDtoParam).Compile();

                return (service, request) =>
                {
                    executeFunc(service, request);
                    return null;
                };
            }
        }

        public object Execute(IRequest request, TService service, object requestDto)
        {
            var actionName = request.Verb ?? HttpMethods.Post; //MQ Services

            var overrideVerb = request.GetItem(Keywords.InvokeVerb) as string;
            if (overrideVerb != null)
                actionName = overrideVerb;

            var operationName = requestDto.GetType().GetOperationName();
            string format = request.ResponseContentType.ToContentFormat()?.ToUpper();
            InstanceExecFn action;
            if (execMap.TryGetValue(ActionContext.Key(actionName + format, operationName), out action) ||
            execMap.TryGetValue(ActionContext.AnyFormatKey(format, operationName), out action) ||
            execMap.TryGetValue(ActionContext.Key(actionName, operationName), out action) ||
            execMap.TryGetValue(ActionContext.AnyKey(operationName), out action))
            {
                return action(request, service, requestDto);
            }

            var expectedMethodName = actionName.Substring(0, 1) + actionName.Substring(1).ToLowerInvariant();
            throw new NotImplementedException(
                "Could not find method named {1}({0}) or Any({0}) on Service {2}"
                .Fmt(operationName, expectedMethodName, typeof(TService).GetOperationName()));
        }

        object IServiceExec.Execute(IRequest request, IService service, object requestDto)
        {
            return Execute(request, (TService)service, requestDto);
        }
    }
}