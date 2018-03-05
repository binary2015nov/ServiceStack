using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public class ServiceRoutes : IServiceRoutes
    {
        private List<RestPath> restPaths = new List<RestPath>();

        public virtual IServiceRoutes Add(RestPath restPath)
        {
            if (restPath == null || HasExistingRoute(restPath.RequestType, restPath.Path))
                return this;

            //Auto add Route Attributes so they're available in T.ToUrl() extension methods
            restPath.RequestType
                .AddAttributes(new RouteAttribute(restPath.Path, restPath.AllowedVerbs)
                {
                    Priority = restPath.Priority,
                    Summary = restPath.Summary,
                    Notes = restPath.Notes,
                    Matches = restPath.MatchRule
                });

            restPaths.Add(restPath);

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasExistingRoute(Type requestType, string restPath)
        {
            return restPaths.FirstOrDefault(x => x.RequestType == requestType && x.Path == restPath) != null;
        }

        public IEnumerator<RestPath> GetEnumerator()
        {
            foreach (var restPath in restPaths)
            {
                yield return restPath;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}