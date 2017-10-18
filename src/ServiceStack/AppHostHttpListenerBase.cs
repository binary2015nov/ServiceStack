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
        public static int ThreadsPerProcessor = 16;

        public static int CalculatePoolSize()
        {
            return Environment.ProcessorCount * ThreadsPerProcessor;
        }

        public string HandlerPath { get { return Config.HandlerFactoryPath; } set { Config.HandlerFactoryPath = value; } }

        protected AppHostHttpListenerBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices) { }

        protected AppHostHttpListenerBase(string serviceName, string handlerPath, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        {
            HandlerPath = handlerPath;
        }

        protected override async Task ProcessRequestAsync(HttpListenerContext context)
        {
            if (string.IsNullOrEmpty(context.Request.RawUrl))
                return;
            
            RequestContext.Instance.StartRequestContext();

            var operationName = context.Request.GetOperationName().UrlDecode();

            var httpReq = context.ToRequest(operationName);
            var httpRes = httpReq.Response;

            var handler = HttpHandlerFactory.GetHandler(httpReq);

            var serviceStackHandler = handler as IServiceStackHandler;
            if (serviceStackHandler == null)
                throw new NotImplementedException($"Cannot execute handler: {handler} at PathInfo: {httpReq.PathInfo}");
  
            var task = serviceStackHandler.ProcessRequestAsync(httpReq, httpRes, operationName);
            await HostContext.Async.ContinueWith(httpReq, task, x => httpRes.Close(), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.AttachedToParent);
            //Matches Exceptions handled in HttpListenerBase.InitTask() 
        }
    }
}

#endif
