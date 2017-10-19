using System;
using System.Collections.Generic;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Metadata
{
    public class MetadataPagesConfig
    {
        private readonly ServiceMetadata metadata;
        private readonly HashSet<string> ignoredFormats;
        private readonly Dictionary<string, MetadataConfig> metadataConfigMap;
        public IEnumerable<MetadataConfig> AvailableFormatConfigs { get { return metadataConfigMap.Values; } }

        public MetadataPagesConfig(ServiceMetadata metadata, IEnumerable<string> contentTypeFormats)
        {
            this.ignoredFormats = metadata.Config.IgnoreFormats;
            this.metadata = metadata;

            metadataConfigMap = new Dictionary<string, MetadataConfig>();

            foreach (var format in contentTypeFormats)
            {
                if (!ignoredFormats.Contains(format))
                {
                    var config = metadata.Config.GetEndpointConfig(format);
                    if (config != null)
                    {
                        metadataConfigMap[format] = config;
                    }
                }
            }         
        }

        public MetadataConfig GetMetadataConfig(string format)
        {
            if (metadataConfigMap.ContainsKey(format))
                return metadataConfigMap[format];

            return null;
        }

        public bool IsVisible(IRequest request, Format format, string operation)
        {
            if (ignoredFormats.Contains(format.FromFormat())) return false;
            return metadata.IsVisible(request, format, operation);
        }

        public bool CanAccess(IRequest request, Format format, string operation)
        {
            if (ignoredFormats.Contains(format.FromFormat())) return false;
            return metadata.CanAccess(request, format, operation);
        }

        public bool CanAccess(Format format, string operation)
        {
            if (ignoredFormats.Contains(format.FromFormat())) return false;
            return metadata.CanAccess(format, operation);
        }

        public bool AlwaysHideInMetadata(string operationName)
        {
            Operation operation;
            metadata.OperationNamesMap.TryGetValue(operationName.ToLowerInvariant(), out operation);
            return operation?.RestrictTo?.VisibilityTo == RequestAttributes.None;
        }
    }
}