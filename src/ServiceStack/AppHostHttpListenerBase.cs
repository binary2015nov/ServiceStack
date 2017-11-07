#if !NETSTANDARD2_0

using System;
using System.Reflection;
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

        protected static int CalculatePoolSize()
        {
            return Environment.ProcessorCount * ThreadsPerProcessor;
        }

        public string HandlerPath { get { return Config.HandlerFactoryPath; } set { Config.HandlerFactoryPath = value; } }

        protected AppHostHttpListenerBase(string serviceName, params Assembly[] assembliesWithServices)
            : this(serviceName, "", assembliesWithServices) { }

        protected AppHostHttpListenerBase(string serviceName, string handlerPath, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        {
            HandlerPath = handlerPath;
            Config.MetadataRedirectPath = HandlerPath.IsNullOrEmpty() ? "metadata" : handlerPath.AppendPath("metadata");
        }
    }
}

#endif
