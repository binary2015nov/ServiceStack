using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class ServiceRoutes : IServiceRoutes
    {
        private List<RestPath> restPaths = new List<RestPath>();
        public IEnumerable<RestPath> RestPaths { get { return restPaths; } }

        public IServiceRoutes Add<TRequest>(string restPath)
        {
            if (HasExistingRoute(typeof(TRequest), restPath)) return this;

            restPaths.Add(new RestPath(typeof(TRequest), restPath));
            return this;
        }

        public IServiceRoutes Add<TRequest>(string restPath, string verbs)
        {
            if (HasExistingRoute(typeof(TRequest), restPath)) return this;

            restPaths.Add(new RestPath(typeof(TRequest), restPath, verbs));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            restPaths.Add(new RestPath(requestType, restPath, verbs));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, int priority)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            restPaths.Add(new RestPath(requestType, restPath, verbs) { Priority = priority });
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, string summary, string notes)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            restPaths.Add(new RestPath(requestType, restPath, verbs, summary, notes));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, string summary, string notes, string matchRule)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            appHost.RestPaths.Add(new RestPath(requestType, restPath, verbs, summary, notes, matchRule));
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasExistingRoute(Type requestType, string restPath)
        {
            return RestPaths.FirstOrDefault(x => x.RequestType == requestType && x.Path == restPath) != null;
        }
    }
}