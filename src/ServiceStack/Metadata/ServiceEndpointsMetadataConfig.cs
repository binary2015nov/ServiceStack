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
                Soap11 = new SoapMetadataConfig("soap11", "SOAP 1.1", "/soap11", "/soap11", "/soap11/metadata", "soap11"),
                Soap12 = new SoapMetadataConfig("soap12", "SOAP 1.2", "/soap12", "/soap12", "/soap12/metadata", "soap12"),
                Custom = config
            };
        }

        public string DefaultMetadataUri { get; set; }
        public SoapMetadataConfig Soap11 { get; set; }
        public SoapMetadataConfig Soap12 { get; set; }
        public MetadataConfig Custom { get; set; }

        public HashSet<string> IgnoreFormats { get; set; }

        public MetadataConfig GetEndpointConfig(string contentType)
        {
            if (contentType == "soap11")
                return this.Soap11;
            if (contentType == "soap12")
                return this.Soap12;

            return Custom.Create(contentType);
        }
    }
}