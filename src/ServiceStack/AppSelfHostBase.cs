#if !NETSTANDARD2_0

using System;
using System.Net;
using System.Reflection;
using System.Threading;
using Amib.Threading;
using ServiceStack.Logging;

namespace ServiceStack
{
    /// <summary>
    /// Wrapper class for the HTTPListener to allow easier access to the
    /// server, for start and stop management and event routing of the actual
    /// inbound requests.
    /// </summary>
    public abstract class AppSelfHostBase : AppHostHttpListenerBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AppSelfHostBase));

        private readonly AutoResetEvent listenForNextRequest = new AutoResetEvent(false);

        private readonly SmartThreadPool threadPoolManager;

        private const int IdleTimeout = 300;

        protected AppSelfHostBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        {
            threadPoolManager = new SmartThreadPool(IdleTimeout,
                maxWorkerThreads: Math.Max(16, Environment.ProcessorCount * 2));
        }

        protected AppSelfHostBase(string serviceName, string handlerPath, params Assembly[] assembliesWithServices)
            : base(serviceName, handlerPath, assembliesWithServices)
        {
            threadPoolManager = new SmartThreadPool(IdleTimeout,
                maxWorkerThreads: Math.Max(16, Environment.ProcessorCount * 2));
        }

        // Loop here to begin processing of new requests.
        protected override void Listen(object state)
        {
            while (IsListening)
            {
                if (Listener == null) return;

                try
                {
                    Listener.BeginGetContext(ListenerCallback, Listener);
                    listenForNextRequest.WaitOne();
                }
                catch (Exception ex)
                {
                    Logger.Error("Listen()", ex);
                    return;
                }
                if (Listener == null) return;
            }
        }

        // Handle the processing of a request in here.
        private void ListenerCallback(IAsyncResult asyncResult)
        {
            var listener = asyncResult.AsyncState as HttpListener;
            HttpListenerContext context;

            if (listener == null) return;
            var isListening = listener.IsListening;

            try
            {
                if (!isListening)
                {
                    Logger.DebugFormat("Ignoring ListenerCallback() as HttpListener is no longer listening");
                    return;
                }
                // The EndGetContext() method, as with all Begin/End asynchronous methods in the .NET Framework,
                // blocks until there is a request to be processed or some type of data is available.
                context = listener.EndGetContext(asyncResult);
            }
            catch (Exception ex)
            {
                // You will get an exception when httpListener.Stop() is called
                // because there will be a thread stopped waiting on the .EndGetContext()
                // method, and again, that is just the way most Begin/End asynchronous
                // methods of the .NET Framework work.
                string errMsg = ex + ": " + isListening;
                Logger.Warn(errMsg);
                return;
            }
            finally
            {
                // Once we know we have a request (or exception), we signal the other thread
                // so that it calls the BeginGetContext() (or possibly exits if we're not
                // listening any more) method to start handling the next incoming request
                // while we continue to process this request on a different thread.
                listenForNextRequest.Set();
            }

            if (Config.DebugMode && Logger.IsDebugEnabled)
                Logger.Debug($"{context.Request.UserHostAddress} Request : {context.Request.RawUrl}");

            OnBeginRequest(context);

            threadPoolManager.QueueWorkItem(ProcessRequestContext, context);
        }

        private bool disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (disposed) return;

            lock (this)
            {
                if (disposed) return;

                if (disposing)
                    threadPoolManager.Dispose();

                // new shared cleanup logic
                disposed = true;

                base.Dispose(disposing);
            }
        }
    }
}

#endif
