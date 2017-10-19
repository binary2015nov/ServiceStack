using System.Net;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class IndexPageHttpHandler : HttpAsyncTaskHandler
    {
        public IndexPageHttpHandler() => this.RequestName = nameof(IndexPageHttpHandler);

        public override void ProcessRequest(IRequest request, IResponse response, string operationName)
        {
            var defaultUrl = HostContext.AppHost.Metadata.Config.DefaultMetadataUri;

            if (request.PathInfo == "/")
            {
                var relativeUrl = defaultUrl.Substring(defaultUrl.IndexOf('/'));
                var absoluteUrl = request.GetBaseUrl().AppendPath(relativeUrl);
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.AddHeader(HttpHeaders.Location, absoluteUrl);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.Redirect;
                response.AddHeader(HttpHeaders.Location, defaultUrl);
            }
            response.EndHttpHandlerRequest(skipHeaders:true);
        }

        public override bool IsReusable => true;
    }
}