using System.Collections.Generic;

namespace ServiceStack.Metadata
{
    public class ServiceEndpointsMetadataConfig
    {
        private ServiceEndpointsMetadataConfig()
        {
            IgnoreFormats = new HashSet<string>();
        }

        /// <summary>
        /// Changes the links for the servicestack/metadata page
        /// </summary>
        public static ServiceEndpointsMetadataConfig Create(string serviceStackHandlerPath)
        {
            var config = new MetadataConfig("{0}", "{0}", "/{0}/reply", "/{0}/oneway", "/{0}/metadata");
            return new ServiceEndpointsMetadataConfig
            {
                DefaultMetadataUri = "/metadata",          
                Xml = config.Create("xml"),
                Json = config.Create("json"),
                Jsv = config.Create("jsv"),
#if !NETSTANDARD2_0
                Soap11 = new SoapMetadataConfig("soap11", "SOAP 1.1", "/soap11", "/soap11", "/soap11/metadata", "soap11"),
                Soap12 = new SoapMetadataConfig("soap12", "SOAP 1.2", "/soap12", "/soap12", "/soap12/metadata", "soap12"),
#endif
                Custom = config
            };
        }

        public string DefaultMetadataUri { get; set; }
        public SoapMetadataConfig Soap11 { get; set; }
        public SoapMetadataConfig Soap12 { get; set; }
        public MetadataConfig Xml { get; set; }
        public MetadataConfig Json { get; set; }
        public MetadataConfig Jsv { get; set; }
        public MetadataConfig Custom { get; set; }

        public HashSet<string> IgnoreFormats { get; set; }

        public MetadataConfig GetEndpointConfig(string contentType)
        {
            contentType = contentType.ToLowerSafe();
            switch (contentType)
            {
                case MimeTypes.Soap11:
                    return this.Soap11;
                case MimeTypes.Soap12:
                    return this.Soap12;
                case MimeTypes.Xml:
                    return this.Xml;
                case MimeTypes.Json:
                    return this.Json;
                case MimeTypes.Jsv:
                    return this.Jsv;
            }

            var format = ContentFormat.GetContentFormat(contentType);
            return Custom.Create(format);
        }
    }
}