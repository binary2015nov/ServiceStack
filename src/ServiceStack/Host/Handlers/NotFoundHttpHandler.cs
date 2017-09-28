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
            HostContext.AppHost.OnLogError(typeof(NotFoundHttpHandler), $"{request.UserHostAddress} Request not found: {request.RawUrl}");

            var responseStatus = response.Dto.GetResponseStatus();
            if (responseStatus != null)
                response.StatusDescription = responseStatus.ErrorCode;

            response.ContentType = request.ResponseContentType;
            response.StatusCode = 404;
            var sb = new StringBuilder();
            if (request.ResponseContentType == MimeTypes.Html)
            {
                sb.AppendLine(@"<html>
<head>
    <title>We're sorry, 404 - Not Found.</title>
</head>
<body>");
                sb.Append("<h2>We're sorry, 404 - Not Found.</h2>");
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
                        .Append("<li>HttpMethod: " + request.Verb + "</li>")
                        .Append("<li>PathInfo: " + request.PathInfo + "</li>")
                        .Append("<li>QueryString: " + request.QueryString + "</li>")
                        .Append("<li>RawUrl: " + request.RawUrl + "</li>")
                        .Append("</ul>");
                }
                
                sb.Append("</body></html>"); 
            }
            else
            {
                if (responseStatus != null)
                {
                    sb.AppendLine(responseStatus.ErrorCode != responseStatus.Message
                        ? $"Error ({responseStatus.ErrorCode}): {responseStatus.Message}\n"
                        : $"Error: {responseStatus.Message ?? responseStatus.ErrorCode}\n");
                }

                if (HostContext.Config.DebugMode)
                {
                    sb.AppendLine("Handler for Request not found (404):\n")
                        .AppendLine("HttpMethod: " + request.Verb + "\n")
                        .AppendLine("PathInfo: " + request.PathInfo + "\n")
                        .AppendLine("QueryString: " + request.QueryString + "\n")
                        .AppendLine("RawUrl: " + request.RawUrl + "\n");

                }
            }
            var text = sb.ToString();
            response.EndHttpHandlerRequest(skipClose: true, afterHeaders: r => r.Write(text));
        }

        public override bool IsReusable => true;
    }
}