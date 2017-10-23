#if !NETSTANDARD2_0

using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Host.HttpListener;

namespace ServiceStack
{
    /// <summary>
    /// Inherit from this class if you want to host your web services inside a 
    /// Console Application, Windows Service, etc.
    /// 
    /// Usage of HttpListener allows you to host webservices on the same port (:80) as IIS 
    /// however it requires admin user privillages.
    /// </summary>
    public abstract class AppHostHttpListenerBase : HttpListenerBase
    {
        protected AppHostHttpListenerBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices) { }

        protected AppHostHttpListenerBase(string serviceName, string handlerPath, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        {
            HandlerPath = handlerPath;
        }
    }
}

#endif
