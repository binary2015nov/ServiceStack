﻿#if !NETSTANDARD2_0

//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Net;
using System.Reflection;
using System.Threading;
using ServiceStack.Logging;

namespace ServiceStack
{
    public abstract class AppHostHttpListenerPoolBase : AppHostHttpListenerBase
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AppHostHttpListenerPoolBase));

        private class ThreadPoolManager : IDisposable
        {
            private readonly object syncRoot = new object();
            private volatile bool isDisposing;
            private readonly AutoResetEvent autoResetEvent;
            private int avalaibleThreadCount = 0;

            public ThreadPoolManager(int poolSize)
            {
                autoResetEvent = new AutoResetEvent(false);
                avalaibleThreadCount = poolSize;
            }

            public Thread Peek(ThreadStart threadStart)
            {
                while (!isDisposing && avalaibleThreadCount == 0)
                    autoResetEvent.WaitOne();

                lock (syncRoot)
                {
                    if (isDisposing)
                        return null;

                    if (Interlocked.Decrement(ref avalaibleThreadCount) < 0)
                        return Peek(threadStart);
                }

                return new Thread(threadStart);
            }

            public void Free()
            {
                Interlocked.Increment(ref avalaibleThreadCount);
                autoResetEvent.Set();
            }

            /// <summary>
            /// Exécute les tâches définies par l'application associées à la libération ou à la redéfinition des ressources non managées.
            /// </summary>
            /// <filterpriority>2</filterpriority>
            public void Dispose()
            {
                lock (this)
                {
                    if (isDisposing)
                        return;

                    isDisposing = true;
                }
            }
        }

        private readonly AutoResetEvent listenForNextRequest = new AutoResetEvent(false);
        private readonly ThreadPoolManager threadPoolManager;

        protected AppHostHttpListenerPoolBase(string serviceName, params Assembly[] assembliesWithServices)
            : this(serviceName, CalculatePoolSize(), assembliesWithServices) { }

        protected AppHostHttpListenerPoolBase(string serviceName, int poolSize, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        {
            threadPoolManager = new ThreadPoolManager(poolSize);
        }

        protected AppHostHttpListenerPoolBase(string serviceName, string handlerPath, params Assembly[] assembliesWithServices)
            : this(serviceName, handlerPath, CalculatePoolSize(), assembliesWithServices) { }

        protected AppHostHttpListenerPoolBase(string serviceName, string handlerPath, int poolSize, params Assembly[] assembliesWithServices)
            : base(serviceName, handlerPath, assembliesWithServices)
        {
            threadPoolManager = new ThreadPoolManager(poolSize);
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

            if (Config.DebugMode)
                Logger.DebugFormat("{0} Request : {1}", context.Request.UserHostAddress, context.Request.RawUrl);

            OnBeginRequest(context);

            threadPoolManager.Peek(() =>
            {
                ProcessRequestContext(context);

                threadPoolManager.Free();
            }).Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)           
                threadPoolManager.Dispose();
            
            base.Dispose(disposing);
        }
    }
}

#endif
