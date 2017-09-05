using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Logging;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class ServiceRoutes : IServiceRoutes
    {
        private readonly static ILog Logger = LogManager.GetLogger(typeof(ServiceRoutes));

        public List<RestPath> RestPaths { get; private set; }

        public ServiceRoutes()
        {
            RestPaths = new List<RestPath>();
        }

        public IServiceRoutes Add<TRequest>(string restPath)
        {
            if (HasExistingRoute(typeof(TRequest), restPath)) return this;

            RestPaths.Add(new RestPath(typeof(TRequest), restPath));
            return this;
        }

        public IServiceRoutes Add<TRequest>(string restPath, string verbs)
        {
            if (HasExistingRoute(typeof(TRequest), restPath)) return this;

            RestPaths.Add(new RestPath(typeof(TRequest), restPath, verbs));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            RestPaths.Add(new RestPath(requestType, restPath, verbs));
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, int priority)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            RestPaths.Add(new RestPath(requestType, restPath, verbs)
            {
                Priority = priority
            });
            return this;
        }

        public IServiceRoutes Add(Type requestType, string restPath, string verbs, string summary, string notes)
        {
            if (HasExistingRoute(requestType, restPath)) return this;

            RestPaths.Add(new RestPath(requestType, restPath, verbs, summary, notes));
            return this;
        }

        public bool HasExistingRoute(Type requestType, string restPath)
        {
            var existingRoute = RestPaths.FirstOrDefault(
                x => x.RequestType == requestType && x.Path == restPath);

            if (existingRoute != null)
            {
                var existingRouteMsg = "Existing Route for '{0}' at '{1}' already exists".Fmt(requestType.GetOperationName(), restPath);

                Logger.Warn(existingRouteMsg);
                return true;
            }

            return false;
        }
    }
}