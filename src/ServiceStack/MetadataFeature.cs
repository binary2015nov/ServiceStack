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
        public const string Features = "Features";

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
            var pathArray = pathInfo.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathArray.Length == 0)
                return null;
            return GetHandlerForPathParts(pathArray);
        }

        private IHttpHandler GetHandlerForPathParts(string[] pathParts)
        {
            var pathController = pathParts[0].ToLowerInvariant();
            if (pathParts.Length == 1)
            {
                if (pathController == "metadata")
                    return new IndexMetadataHandler();

                return null;
            }

            var pathAction = pathParts[1].ToLowerInvariant();
#if !NETSTANDARD1_6
            if (pathAction == "wsdl")
            {
                if (pathController == "soap11")
                    return new Soap11WsdlMetadataHandler();
                if (pathController == "soap12")
                    return new Soap12WsdlMetadataHandler();
            }
#endif

            if (pathAction != "metadata") return null;

            switch (pathController)
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
                    if (HostContext.ContentTypes
                        .ContentTypeFormats.TryGetValue(pathController, out contentType))
                    {
                        var format = ContentFormat.GetContentFormat(contentType);
                        return new CustomMetadataHandler(contentType, format);
                    }
                    break;
            }
            return null;
        }

        public void AddSection(string sectionName)
        {
            if (Sections.ContainsKey(sectionName))
                return;

            Sections[sectionName] = new Dictionary<string, string>();
        }

        public void AddLink(string sectionName, string href, string title)
        {
            AddSection(sectionName);
            Sections[sectionName][href] = title;
        }
    }
}