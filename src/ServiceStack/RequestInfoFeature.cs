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
            appHost.GetPlugin<MetadataFeature>()?
                .AddLink(MetadataFeature.DebugInfo, $"?{Keywords.Debug}={Keywords.RequestInfo}", "Request Info");
        }

        public static IHttpHandler GetRequestInfoHandler(IRequest request)
        {
            if (request.QueryString[Keywords.Debug] != Keywords.RequestInfo)
                return null;

            if (HostContext.Config.DebugMode || HostContext.HasValidAuthSecret(request))
                return new RequestInfoHandler();

            var session = request.GetSession();     
            if (session != null && session.Roles.Contains("admin"))         
                return new RequestInfoHandler();
            
            return null;
        }

        public static IHttpHandler GetRequestInfoHandler(string httpMethod, string pathInfo, string filePath)
        {
            if (pathInfo.IsNullOrEmpty())
                return null;

            var array = pathInfo.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (array.Length == 0 || !array[0].Equals(Keywords.RequestInfo, StringComparison.OrdinalIgnoreCase))
                return null;

            return new RequestInfoHandler();
        }
    }
}