#if NETSTANDARD2_0

using System;
using System.Reflection;

namespace ServiceStack
{
    [Obsolete("//Just created for compatibility to run tests on .NET Core, please use AppSelfHostBase")]
    public abstract class AppHostHttpListenerBase : AppSelfHostBase 
    {
        protected AppHostHttpListenerBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices) { }
    }

    [Obsolete("//Just created for compatibility to run tests on .NET Core, please use AppSelfHostBase")]
    public abstract class AppHostHttpListenerPoolBase : AppHostHttpListenerBase 
    {
        protected AppHostHttpListenerPoolBase(string serviceName, int poolSize, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        { }

        protected AppHostHttpListenerPoolBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        { }
    }

    [Obsolete("//Just created for compatibility to run tests on .NET Core, please use AppSelfHostBase")]
    public abstract class AppHostHttpListenerSmartPoolBase : AppHostHttpListenerPoolBase 
    {
        protected AppHostHttpListenerSmartPoolBase(string serviceName, params Assembly[] assembliesWithServices)
            : base(serviceName, assembliesWithServices)
        { }
    }
}

#endif