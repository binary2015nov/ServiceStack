using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Funq;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class AppHostExtensions
    {
        public static void RegisterService<TService>(this IAppHost appHost, params string[] atRestPaths) where TService : IService
        {
            appHost.RegisterService(typeof(TService), atRestPaths);
        }

        public static void RegisterRequestBinder<TRequest>(this IAppHost appHost, Func<IRequest, object> binder)
        {
            appHost.RequestBinders[typeof(TRequest)] = binder;
        }

        public static void AddPluginsFromAssembly(this IAppHost appHost, params Assembly[] assembliesWithPlugins)
        {
            var ssHost = (ServiceStackHost)appHost;
            foreach (Assembly assembly in assembliesWithPlugins)
            {
                var pluginTypes =
                    from t in assembly.GetExportedTypes()
                    where t.GetInterfaces().Any(x => x == typeof(IPlugin))
                    select t;

                foreach (var pluginType in pluginTypes)
                {
                    var plugin = pluginType.CreateInstance() as IPlugin;
                    if (plugin != null)
                    {
                        ssHost.LoadPlugin(plugin);
                    } 
                }
            }
        }

        public static TPlugin GetPlugin<TPlugin>(this IAppHost appHost) where TPlugin : class, IPlugin
        {
            return appHost.Plugins.FirstOrDefault(x => x is TPlugin) as TPlugin;
        }

        public static bool HasPlugin<TPlugin>(this IAppHost appHost) where TPlugin : class, IPlugin
        {
            return appHost.Plugins.FirstOrDefault(x => x is TPlugin) != null;
        }

        public static bool HasMultiplePlugins<TPlugin>(this IAppHost appHost) where TPlugin : class, IPlugin
        {
            return appHost.Plugins.Count(x => x is TPlugin) > 1;
        }

        /// <summary>
        /// Get an IAppHost container. 
        /// Note: Registering dependencies should only be done during setup/configuration 
        /// stage and remain immutable there after for thread-safety.
        /// </summary>
        /// <param name="appHost"></param>
        /// <returns></returns>
        public static Container GetContainer(this IAppHost appHost)
        {
            var hasContainer = appHost as IHasContainer;
            return hasContainer?.Container;
        }

        public static string Localize(this string text, IRequest request)
        {
            return HostContext.AppHost.ResolveLocalizedString(text, request);
        }

        public static IAppHost Start(this IAppHost appHost, IEnumerable<string> urlBases)
        {
#if !NETSTANDARD2_0
            var listener = (ServiceStack.Host.HttpListener.HttpListenerBase)appHost;
            listener.Start(urlBases);
#endif
            return appHost;
        }

        public static List<IPlugin> AddIfNotExists<TPlugin>(this List<IPlugin> plugins, TPlugin plugin) where TPlugin : class, IPlugin
        {
            if (!plugins.Any(x => x is TPlugin))
                plugins.Add(plugin);
            
            return plugins;
        }
    }

}