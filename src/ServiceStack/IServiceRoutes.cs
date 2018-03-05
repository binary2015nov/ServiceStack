using System;
using System.Collections.Generic;
using ServiceStack.Host;

namespace ServiceStack.Web
{
    /// <summary>
    /// Allow the registration of user-defined routes for services
    /// </summary>
    public interface IServiceRoutes : IEnumerable<RestPath>
    {
        IServiceRoutes Add(RestPath restPath);
    }
}