using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ServiceStack.Host.Handlers;
using ServiceStack.Logging;
using ServiceStack.MiniProfiler;
using ServiceStack.Web;
using ServiceStack.Text;

namespace ServiceStack.Host
{
    public class RestHandler : ServiceStackHandlerBase, IRequestHttpHandler
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(RestHandler));

        public RestHandler()
        {
            this.HandlerAttributes = RequestAttributes.Reply;
        }

        public static IRestPath FindMatchingRestPath(IHttpRequest httpRequest, out string contentType)
        {
            var pathInfo = GetSanitizedPathInfo(httpRequest.PathInfo, out contentType);
            if (contentType != null)
                httpRequest.ResponseContentType = contentType;

            var restPath = HostContext.ServiceController.GetRestPathForRequest(httpRequest.Verb, pathInfo, httpRequest);
            httpRequest.SetRoute(restPath);
            return restPath;
        }

        public static string GetSanitizedPathInfo(string pathInfo, out string contentType)
        {
            contentType = null;
            if (HostContext.Config.AllowRouteContentTypeExtensions)
            {
                var pos = pathInfo.LastIndexOf('.');
                if (pos >= 0)
                {
                    var format = pathInfo.Substring(pos + 1);
                    contentType = HostContext.ContentTypes.GetFormatContentType(format);
                    if (contentType != null)
                    {
                        pathInfo = pathInfo.Substring(0, pos);
                    }
                }
            }
            return pathInfo;
        }

        public override bool RunAsAsync() => true;

        public override async Task ProcessRequestAsync(IRequest httpReq, IResponse httpRes, string operationName)
        {
            try
            {
                var restPath = httpReq.GetRoute();
                if (restPath == null)
                    throw new NotSupportedException("No RestPath found for: " + httpReq.Verb + " " + httpReq.PathInfo);
    
                httpReq.OperationName = operationName = restPath.RequestType.GetOperationName();

                if (appHost.ApplyPreRequestFilters(httpReq, httpRes))
                    return;

                appHost.AssertContentType(httpReq.ResponseContentType);

                var request = httpReq.Dto = await CreateRequestAsync(httpReq, restPath);

                await appHost.ApplyRequestFiltersAsync(httpReq, httpRes, request);
                if (httpRes.IsClosed)
                    return;

                var requestContentType = ContentFormat.GetEndpointAttributes(httpReq.ResponseContentType);
                httpReq.RequestAttributes |= HandlerAttributes | requestContentType;

                var rawResponse = await GetResponseAsync(httpReq, request);
                if (httpRes.IsClosed)
                    return;

                await HandleResponse(httpReq, httpRes, rawResponse);
            }
            //sync with GenericHandler
            catch (TaskCanceledException)
            {
                httpRes.StatusCode = (int)HttpStatusCode.PartialContent;
                httpRes.EndRequest();
            }
            catch (Exception ex)
            {
                if (!appHost.Config.WriteErrorsToResponse)
                {
                    await appHost.ApplyResponseConvertersAsync(httpReq, ex);
                    httpRes.EndRequest();
                }
                else
                {
                    await HandleException(httpReq, httpRes, operationName,
                        await appHost.ApplyResponseConvertersAsync(httpReq, ex) as Exception ?? ex);
                }
            }
        }

        public static async Task<object> CreateRequestAsync(IRequest httpReq, IRestPath restPath)
        {
            if (restPath == null)
                throw new ArgumentNullException(nameof(restPath));

            using (Profiler.Current.Step("Deserialize Request"))
            {
                var dtoFromBinder = GetCustomRequestFromBinder(httpReq, restPath.RequestType);
                if (dtoFromBinder != null)
                    return await HostContext.AppHost.ApplyRequestConvertersAsync(httpReq, dtoFromBinder);

                var requestParams = httpReq.GetFlattenedRequestParams();

                return await HostContext.AppHost.ApplyRequestConvertersAsync(httpReq,
                    CreateRequest(httpReq, restPath, requestParams));
            }
        }

        public static object CreateRequest(IRequest httpReq, IRestPath restPath, Dictionary<string, string> requestParams)
        {
            var requestDto = CreateContentTypeRequest(httpReq, restPath.RequestType, httpReq.ContentType);

            return CreateRequest(httpReq, restPath, requestParams, requestDto);
        }

        public static object CreateRequest(IRequest httpReq, IRestPath restPath, Dictionary<string, string> requestParams, object requestDto)
        {
            var pathInfo = !restPath.IsWildCardPath
                ? GetSanitizedPathInfo(httpReq.PathInfo, out _)
                : httpReq.PathInfo;

            return restPath.CreateRequest(pathInfo, requestParams, requestDto);
        }

        /// <summary>
        /// Used in Unit tests
        /// </summary>
        /// <returns></returns>
        public Task<object> CreateRequestAsync(IRequest httpReq, string operationName)
        {
            return CreateRequestAsync(httpReq, httpReq.GetRoute());
        }
    }

}
