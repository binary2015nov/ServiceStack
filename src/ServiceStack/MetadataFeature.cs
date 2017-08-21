using System;
using System.Collections.Generic;
using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Metadata;

namespace ServiceStack
{
    public class MetadataFeature : IPlugin
    {
        public const string PluginLinks = "Plugin Links";
        public const string DebugInfo = "Debug Info";
        public const string EnabledFeatures = "EnabledFeatures";

        public Dictionary<string, Dictionary<string, string>> Sections { get; private set; }

        public Action<IndexOperationsControl> IndexPageFilter { get; set; }
        public Action<OperationControl> DetailPageFilter { get; set; }

        public bool ShowResponseStatusInMetadataPages { get; set; }

        public MetadataFeature()
        {
            Sections = new Dictionary<string, Dictionary<string, string>>();
            AddSection(PluginLinks);
            AddLink(DebugInfo, "operations/metadata", "Operations Metadata");
        }

        public void Register(IAppHost appHost)
        {
            appHost.CatchAllHandlers.Add(ProcessRequest);
        }

        public virtual IHttpHandler ProcessRequest(string httpMethod, string pathInfo, string filePath)
        {
            if (pathInfo.IsNullOrEmpty())
                return null;

            string metadata = HostContext.Config.MetadataRedirectPath.IsNullOrEmpty()
                ? "/metadata"
                : "/" + HostContext.Config.MetadataRedirectPath.TrimStart('/');
            if (pathInfo.Equals(metadata, StringComparison.OrdinalIgnoreCase))
                return new IndexMetadataHandler();
            var pathArray = pathInfo.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (pathArray.Length != 2 || !pathArray[1].Equals("metadata"))
                return null;
                ;
            switch (pathArray[0])
            {
                case "json":
                    return new JsonMetadataHandler();

                case "xml":
                    return new XmlMetadataHandler();

                case "jsv":
                    return new JsvMetadataHandler();
#if !NETSTANDARD1_6
                case "soap11":
                    return new Soap11MetadataHandler();

                case "soap12":
                    return new Soap12MetadataHandler();
#endif

                case "operations":
                    return new CustomResponseHandler((httpReq, httpRes) =>
                        HostContext.AppHost.HasAccessToMetadata(httpReq, httpRes)
                            ? HostContext.Metadata.GetOperationDtos()
                            : null, "Operations");

                default:
                    string contentType;
                    if (HostContext.ContentTypes.ContentTypeFormats.TryGetValue(pathArray[0], out contentType))
                    {
                        var format = ContentFormat.GetContentFormat(contentType);
                        return new CustomMetadataHandler(contentType, format);
                    }
                    return null;
            }
        }

        public void AddSection(string sectionName)
        {
            if (!Sections.ContainsKey(sectionName))
            {
                Sections[sectionName] = new Dictionary<string, string>();               
            }           
        }

        public void AddLink(string sectionName, string href, string title)
        {
            AddSection(sectionName);
            Sections[sectionName][href] = title;
        }
    }
}