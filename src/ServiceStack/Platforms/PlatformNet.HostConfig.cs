#if !NETSTANDARD1_6
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Configuration;
using System.Xml.Linq;
using ServiceStack.Configuration;

namespace ServiceStack
{
    public partial class PlatformNet : Platform
    {
        const string NamespacesAppSettingsKey = "servicestack.razor.namespaces";

        public override void InitHostConifg(HostConfig config)
        {
            base.InitHostConifg(config);
            if (HostContext.IsAspNetHost)
            {
                var httpHandlerPath = InferHttpHandlerPath();
                if (httpHandlerPath == null)
                    throw new ConfigurationErrorsException("Unable to infer ServiceStack's <httpHandler.Path/> from your application's configuration file.\n"
                        + "Check with https://github.com/ServiceStack/ServiceStack/wiki/Create-your-first-webservice to ensure you have configured ServiceStack properly.\n"
                        + "Otherwise you can explicitly set your httpHandler.Path by setting: HostConfig.HandlerFactoryPath.");

                config.HandlerFactoryPath = httpHandlerPath;
            }
                
        }

        public override HashSet<string> GetRazorNamespaces()
        {
            var razorNamespaces = new HashSet<string>();
            //Infer from <system.web.webPages.razor> - what VS.NET's intell-sense uses
            var configPath = GetAppConfigPath();
            if (configPath != null)
            {
                var xml = configPath.ReadAllText();
                var doc = XElement.Parse(xml);
                doc.AnyElement("system.web.webPages.razor")
                    .AnyElement("pages")
                    .AnyElement("namespaces")
                    .AllElements("add").ToList()
                    .ForEach(x => razorNamespaces.Add(x.AnyAttribute("namespace").Value));
            }

            //E.g. <add key="servicestack.razor.namespaces" value="System,ServiceStack.Text" />
            if (ConfigUtils.GetNullableAppSetting(NamespacesAppSettingsKey) != null)
            {
                ConfigUtils.GetListFromAppSetting(NamespacesAppSettingsKey)
                    .ForEach(x => razorNamespaces.Add(x));
            }

            return razorNamespaces;
        }

        public override string GetAppConfigPath()
        {
            if (HostContext.AppHost == null)
                return null;

            var configPath = "~/web.config".MapHostAbsolutePath();
            if (File.Exists(configPath))
                return configPath;

            configPath = "~/Web.config".MapHostAbsolutePath(); //*nix FS FTW!
            if (File.Exists(configPath))
                return configPath;

            var appHostDll = new FileInfo(HostContext.AppHost.GetType().Assembly.Location).Name;
            configPath = $"~/{appHostDll}.config".MapAbsolutePath();
            return File.Exists(configPath) ? configPath : null;
        }

        public static string InferHttpHandlerPath()
        {              
            var appConfig = WebConfigurationManager.OpenWebConfiguration("~/");
            foreach (ConfigurationLocation locationConfigLocation in appConfig.Locations)
            {
                var handlerPath = GetHandlerPathFromConfiguration(locationConfigLocation.OpenConfiguration());
                if (handlerPath != null)
                {
                    return CombineHandlerFactoryPath(locationConfigLocation.Path, handlerPath);
                }
            }
            var combinedPath = GetHandlerPathFromConfiguration(appConfig);
            //In some MVC Hosts auto-inferencing doesn't work, in these cases assume the most likely default of "/api" path
            //var isMvcHost = Type.GetType("System.Web.Mvc.Controller") != null;
            //if (isMvcHost)
            //{
            //   combinedPath = CombineHandlerFactoryPath("api", null);
            //}
            return combinedPath;
        }

        public static string GetHandlerPathFromConfiguration(System.Configuration.Configuration configuration)
        {
            //IIS7+ integrated mode system.webServer/handlers
            if (Platform.IsIntegratedPipeline)
            {
                var webServerSection = configuration.GetSection("system.webServer");
                var rawXml = webServerSection?.SectionInformation.GetRawXml();
                if (!rawXml.IsNullOrEmpty())
                    return ExtractHandlerPathFromWebServerConfigurationXml(rawXml);            
            }
            else
            {
                //standard config
                var handlersSection = configuration.GetSection("system.web/httpHandlers") as HttpHandlersSection;
                if (handlersSection != null)
                {
                    for (var i = 0; i < handlersSection.Handlers.Count; i++)
                    {
                        var httpHandler = handlersSection.Handlers[i];
                        if (!httpHandler.Type.StartsWith("ServiceStack"))
                            continue;

                        return httpHandler.Path;
                    }
                }
            }
            return null;
        }

        private static string ExtractHandlerPathFromWebServerConfigurationXml(string rawXml)
        {
            return XDocument.Parse(rawXml).Root.Element("handlers")
                ?.Descendants("add")
                ?.Where(handler => EnsureHandlerTypeAttribute(handler).StartsWith("ServiceStack"))
                .Select(handler => handler.Attribute("path").Value)
                .FirstOrDefault();  
        }

        private static string EnsureHandlerTypeAttribute(XElement handler)
        {
            if (handler.Attribute("type") != null && !string.IsNullOrEmpty(handler.Attribute("type").Value))
            {
                return handler.Attribute("type").Value;
            }
            return string.Empty;
        }

        private static string CombineHandlerFactoryPath(string locationPath, string handlerPath)
        {
            return locationPath.AppendPath(handlerPath.Replace("*", string.Empty)).Trim('/');
        }
    }
}
#endif
