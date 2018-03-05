#if !NETSTANDARD2_0
using System;
using System.Reflection;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Inherit from this class if you want to host your web services inside an
    /// ASP.NET application.
    /// </summary>
    public abstract class AppHostBase : ServiceStackHost
    {
        protected AppHostBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        {
            Config.HandlerFactoryPath = PlatformNet.InferHttpHandlerPath();
        }

        public override string GetBaseUrl(IRequest httpReq)
        {
            if (!Config.WebHostUrl.IsNullOrEmpty())
                return Config.WebHostUrl;

            var aspReq = (HttpRequestBase)httpReq.OriginalRequest;
            var absoluteUri = aspReq.Url.Scheme + "://" + aspReq.Url.Authority +
                      aspReq.ApplicationPath;

            var baseUrl = absoluteUri.AppendPath(Config.HandlerFactoryPath);
            return Config.WebHostUrl = baseUrl.WithTrailingSlash();
        }

        public override IRequest TryGetCurrentRequest()
        {
            try
            {
                return IsReady ? HttpContext.Current.ToRequest() : null;
            }
            catch
            {
                return null;
            }
        }

        public override string MapProjectPath(string relativePath)
        {
            return relativePath.MapHostAbsolutePath();
        }

        protected override string GetWebRootPath()
        {
            return "~".MapHostAbsolutePath();
        }
    }
}
#endif
