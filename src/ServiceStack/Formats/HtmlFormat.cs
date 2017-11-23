using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Serialization;
using ServiceStack.Templates;
using ServiceStack.Web;
using ServiceStack.Html;

namespace ServiceStack.Formats
{
    public class HtmlFormat : IPlugin
    {
        public static string TitleFormat
            = @"{0} Snapshot of {1}";

        public static string HtmlTitleFormat
            = @"Snapshot of <i>{0}</i> generated by <a href=""https://servicestack.net"">ServiceStack</a> on <b>{1}</b>";

        public static bool Humanize = true;

        public const string ModelKey = "Model";
        public const string ErrorStatusKey = "__errorStatus";

        public List<IViewEngine> ViewEngines { get; set; }

        public void Register(IAppHost appHost)
        {
            //Register this in ServiceStack with the custom formats
            appHost.ContentTypes.RegisterAsync(MimeTypes.Html, SerializeToStreamAsync, null);
            appHost.ContentTypes.RegisterAsync(MimeTypes.JsonReport, SerializeToStreamAsync, null);

            appHost.Config.DefaultContentType = MimeTypes.Html;
            appHost.Config.IgnoreFormatsInMetadata.Add(MimeTypes.Html.ToContentFormat());
            appHost.Config.IgnoreFormatsInMetadata.Add(MimeTypes.JsonReport.ToContentFormat());

            ViewEngines = appHost.ViewEngines;
        }

        public Task SerializeToStreamAsync(IRequest req, object response, Stream outputStream)
        {
            var res = req.Response;
            if (req.GetItem("HttpResult") is IHttpResult httpResult && httpResult.Headers.ContainsKey(HttpHeaders.Location)
                && httpResult.StatusCode != System.Net.HttpStatusCode.Created)
                return TypeConstants.EmptyTask;

            try
            {
                if (res.StatusCode >= 400)
                {
                    var responseStatus = response.GetResponseStatus();
                    req.Items[ErrorStatusKey] = responseStatus;
                }

                if (response is CompressedResult)
                {
                    if (res.Dto != null)
                        response = res.Dto;
                    else
                        throw new ArgumentException("Cannot use Cached Result as ViewModel");
                }

                foreach (var viewEngine in ViewEngines)
                {
                    var task = viewEngine.ProcessRequestAsync(req, response, outputStream);
                    task.Wait();
                    if (task.Result)
                        return task;
                }
            }
            catch (Exception ex)
            {
                if (res.StatusCode < 400)
                    throw;

                //If there was an exception trying to render a Error with a View, 
                //It can't handle errors so just write it out here.
                response = DtoUtils.CreateErrorResponse(req.Dto, ex);
            }

            //Handle Exceptions returning string
            if (req.ResponseContentType == MimeTypes.PlainText)
            {
                req.ResponseContentType = MimeTypes.Html;
                res.ContentType = MimeTypes.Html;
            }

            if (req.ResponseContentType != MimeTypes.Html && req.ResponseContentType != MimeTypes.JsonReport)
                return TypeConstants.EmptyTask;

            var dto = response.GetDto();
            if (!(dto is string html))
            {
                // Serialize then escape any potential script tags to avoid XSS when displaying as HTML
                var json = JsonDataContractSerializer.Instance.SerializeToString(dto) ?? "null";
                json = json.Replace("<", "&lt;").Replace(">", "&gt;");

                var now = DateTime.Now;
                var requestName = req.OperationName ?? dto.GetType().GetOperationName();

                html = HtmlTemplates.GetHtmlFormatTemplate()
                    .Replace("${Dto}", json)
                    .Replace("${Title}", string.Format(TitleFormat, requestName, now))
                    .Replace("${MvcIncludes}", MiniProfiler.Profiler.RenderIncludes()?.ToString())
                    .Replace("${Header}", string.Format(HtmlTitleFormat, requestName, now))
                    .Replace("${ServiceUrl}", req.AbsoluteUri)
                    .Replace("${Humanize}", Humanize.ToString().ToLower());
            }

            var utf8Bytes = html.ToUtf8Bytes();
            return outputStream.WriteAsync(utf8Bytes, 0, utf8Bytes.Length);
        }
    }
}