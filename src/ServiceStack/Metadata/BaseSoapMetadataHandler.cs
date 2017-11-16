#if !NETSTANDARD2_0
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.UI;
using System.Xml.Schema;
using ServiceStack.Web;
using ServiceStack.Host;

namespace ServiceStack.Metadata
{
    public abstract class BaseSoapMetadataHandler : BaseMetadataHandler
    {
        protected BaseSoapMetadataHandler()
        {
            RequestName = OperationName = GetType().GetOperationName().Replace("Handler", "");
        }

        public string OperationName { get; set; }
    }
}
#endif