using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ServiceStack.Host.Handlers;
using ServiceStack.Metadata;

namespace ServiceStack
{
    public class MetadataFeature : IPlugin
    {
        public const string PluginLinks = "Plugin Links";
        public const string DebugInfo = "Debug Info";
        public const string AvailableFeatures = "Available Features";

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
            string metadata = HostContext.Config.MetadataRedirectPath.IsNullOrEmpty()
                ? "/metadata"
                : HostContext.Config.MetadataRedirectPath;

            if (pathInfo.TrimEnd('/').Equals(metadata, StringComparison.OrdinalIgnoreCase))
            {
                if (pathInfo.Length == metadata.Length)
                    return new IndexMetadataHandler();
                else
                    return new RedirectHttpHandler { RelativeUrl = metadata };
            }

            var pathArray = pathInfo.ToLower().Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (pathArray.Length != 2)
                return null;
                    
            switch (pathArray[0])
            {
                case "json":
                    return pathArray[1] == "metadata" ? new JsonMetadataHandler() : null;

                case "xml":
                    return pathArray[1] == "metadata" ? new XmlMetadataHandler() : null;

                case "jsv":
                    return pathArray[1] == "metadata" ? new JsvMetadataHandler() : null;
#if !NETSTANDARD2_0
                case "soap11":
                    return pathArray[1] == "metadata"
                        ? new Soap11MetadataHandler() as IHttpHandler
                        : (pathArray[1] == "wsdl" ? new Soap11WsdlMetadataHandler() : null);

                case "soap12":
                    return pathArray[1] == "metadata"
                        ? new Soap12MetadataHandler() as IHttpHandler
                        : (pathArray[1] == "wsdl" ? new Soap12WsdlMetadataHandler() : null);
#endif

                case "operations":
                    return new CustomResponseHandler((httpReq, httpRes) =>
                        HostContext.AppHost.HasAccessToMetadata(httpReq, httpRes)
                            ? HostContext.Metadata.GetOperationDtos()
                            : null, "Operations");

                default:
                    string contentType;
                    if (HostContext.ContentTypes.ContentTypeFormats.TryGetValue(pathArray[0], out contentType) && pathArray[1] == "metadata")
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