using System.Collections.Generic;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
    public class NotFoundHttpHandler : HttpAsyncTaskHandler
    {
        public NotFoundHttpHandler() => this.RequestName = nameof(NotFoundHttpHandler);

        public bool? IsIntegratedPipeline { get; set; }
        public string WebHostPhysicalPath { get; set; }
        public string WebHostUrl { get; set; }
        public string DefaultRootFileName { get; set; }
        public string DefaultHandler { get; set; }

        public override void ProcessRequest(IRequest request, IResponse response, string operationName)
        {
            HostContext.AppHost.OnLogError(typeof(NotFoundHttpHandler),
                $"{request.UserHostAddress} Request not found: {request.RawUrl}");

            var sb = new StringBuilder();
            sb.AppendLine(@"<html>
<head>
    <title>We're sorry, 404 - Not Found.</title>
</head>
<body>");
            sb.Append("<h2>We're sorry, 404 - Not Found.</h2>");
            var responseStatus = response.Dto.GetResponseStatus();
            if (responseStatus != null)
            {
                sb.Append(
                    responseStatus.ErrorCode != responseStatus.Message
                    ? $"Error ({responseStatus.ErrorCode}): {responseStatus.Message}<br />"
                    : $"Error: {responseStatus.Message ?? responseStatus.ErrorCode}<br />");
            }

            if (HostContext.Config.DebugMode)
            {
                sb.Append("<p>Handler for Request not found (404):</p>")
                    .Append("<ul>")
                    .Append("<li>Request.HttpMethod: " + request.Verb + "</li>")
                    .Append("<li>Request.PathInfo: " + request.PathInfo + "</li>")
                    .Append("<li>Request.QueryString: " + request.QueryString + "</li>")
                    .Append("<li>Request.RawUrl: " + request.RawUrl + "</li>")
                    .Append("</ul>");
            }

            response.ContentType = MimeTypes.Html;
            response.StatusCode = 404;

            if (responseStatus != null)
                response.StatusDescription = responseStatus.ErrorCode;
            sb.Append("</body></html>");
            var text = sb.ToString();
            response.EndHttpHandlerRequest(skipClose: true, afterHeaders: r => r.Write(text));
        }

        public override bool IsReusable => true;
    }
}