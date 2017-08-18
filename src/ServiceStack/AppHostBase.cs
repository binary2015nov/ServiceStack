#if !NETSTANDARD1_6
using System.Reflection;
using System.Web;
using ServiceStack.Host.AspNet;
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
        { }

        public override string ResolvePhysicalPath(string virtualPath, IRequest request)
        {
            if (request is AspNetRequest)
	        {
                return ((AspNetRequest)request).HttpRequest.PhysicalPath;	    ;
	        }
            return base.ResolvePhysicalPath(virtualPath, request);
        }

        public override IRequest TryGetCurrentRequest()
        {
            try
            {
                return Ready ? HttpContext.Current.ToRequest() : null;
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
    }
}
#endif
