using System;
using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public class RequestInfoFeature : IPlugin
    {
        public void Register(IAppHost appHost)
        {
            appHost.RawHttpHandlers.Add(GetRequestInfoHandler);
            appHost.CatchAllHandlers.Add(GetRequestInfoHandler);
            appHost.PreRequestFilters.Add((req, res) =>
            {
                HttpHandlerFactory.DebugLastHandlerArgs = req.Verb + "|" + req.RawUrl + "|" + req.GetPhysicalPath();
            });
            appHost.GetPlugin<MetadataFeature>()?.AddLink(MetadataFeature.DebugInfo, $"?{Keywords.Debug}={Keywords.RequestInfo}", "Request Info");
        }

        public static IHttpHandler GetRequestInfoHandler(IRequest request)
        {
            if (request.QueryString[Keywords.Debug] != Keywords.RequestInfo)
                return null;
            
            var reqInfo = RequestInfoHandler.GetRequestInfo(request);
            if (reqInfo == null)
                return null;

            reqInfo.Host = HostContext.Config.DebugHttpListenerHostEnvironment + "_v" + Env.ServiceStackVersion + "_" + HostContext.ServiceName;
            reqInfo.PathInfo = request.PathInfo;
            reqInfo.GetPathUrl = request.GetPathUrl();

            return new RequestInfoHandler { RequestInfo = reqInfo };
        }

        public static IHttpHandler GetRequestInfoHandler(string httpMethod, string pathInfo, string filePath)
        {
            if (string.IsNullOrEmpty(pathInfo))
                return null;

            var array = pathInfo.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length == 0 || !array[0].Equals(Keywords.RequestInfo, StringComparison.OrdinalIgnoreCase))
                return null;

            return new RequestInfoHandler();
        }
    }
}