//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public interface IServiceExec
    {
        object Execute(IRequest request, IService service, object requestDto);
    }

    //public class ServiceRequestExec<TService, TRequest> : IServiceExec
    //{
    //    public object Execute(IRequest requestContext, object instance, object request)
    //    {
    //        return ServiceExec<TService>.Execute(requestContext, instance, request,
    //            typeof(TRequest).GetOperationName());
    //    }
    //}

    //public static class ServiceExecExtensions
    //{
    //    public static IEnumerable<MethodInfo> GetActions(this Type serviceType)
    //    {
    //        foreach (var mi in serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
    //        {
    //            if (mi.GetParameters().Length != 1)
    //                continue;

    //            var actionName = mi.Name.ToUpper();
    //            if (!HttpMethods.AllVerbs.Contains(actionName) && 
    //                actionName != ActionContext.AnyAction &&
    //                !HttpMethods.AllVerbs.Any(verb => ContentTypes.KnownFormats.Any(format => actionName.EqualsIgnoreCase(verb + format))) &&
    //                !ContentTypes.KnownFormats.Any(format => actionName.EqualsIgnoreCase(ActionContext.AnyAction + format)))
    //                continue;

    //            yield return mi;
    //        }
    //    }
    //}

    public class ServiceExec<TService> : IServiceExec
        where TService : IService
    {
        private const string ResponseDtoSuffix = "Response";

        public static Dictionary<Type, List<ActionContext>> ActionMap { get; private set; }

        private static Dictionary<string, InstanceExecFn> execMap;

        static ServiceExec()
        {
            ActionMap = new Dictionary<Type, List<ActionContext>>();
            execMap = new Dictionary<string, InstanceExecFn>();

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

                var reqFilters = new List<IHasRequestFilter>();
                var resFilters = new List<IHasResponseFilter>();

                foreach (var attr in mi.GetCustomAttributes(true))
                {
                    var hasReqFilter = attr as IHasRequestFilter;
                    var hasResFilter = attr as IHasResponseFilter;

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
                MethodInfo methodInfo = typeof(ServiceExec<>).MakeGenericType(typeof(TService)).
                    GetMethod("CreateServiceRunnersFor", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(item.Key);
                methodInfo.Invoke(null, new object[] { item.Value });
            }
        }

        private static void CreateServiceRunnersFor<TRequest>(IEnumerable<ActionContext> collection)
        {
            foreach (var actionCtx in collection)
            {
                var serviceRunner = HostContext.CreateServiceRunner<TRequest>(actionCtx);
                execMap[actionCtx.Id] = serviceRunner.Process;
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
            var actionName = request.Verb
                ?? HttpMethods.Post; //MQ Services

            var overrideVerb = request.GetItem(Keywords.InvokeVerb) as string;
            if (overrideVerb != null)
                actionName = overrideVerb;

            var format = request.ResponseContentType.ToContentFormat()?.ToUpper();

            InstanceExecFn action;
            if (execMap.TryGetValue(ActionContext.Key(actionName + format, request.OperationName), out action) ||
            execMap.TryGetValue(ActionContext.AnyFormatKey(format, request.OperationName), out action) ||
            execMap.TryGetValue(ActionContext.Key(actionName, request.OperationName), out action) ||
            execMap.TryGetValue(ActionContext.AnyKey(request.OperationName), out action))
            {
                return action(request, service, requestDto);
            }

            var expectedMethodName = actionName.Substring(0, 1) + actionName.Substring(1).ToLowerInvariant();
            throw new NotImplementedException(
                "Could not find method named {1}({0}) or Any({0}) on Service {2}"
                .Fmt(request.OperationName, expectedMethodName, typeof(TService).GetOperationName()));
        }

        object IServiceExec.Execute(IRequest request, IService service, object requestDto)
        {
            return Execute(request, (TService)service, requestDto);
        }
    }
}