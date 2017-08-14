using Funq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [ExcludeMetadata]
    [Route("/content/{Id}")]
    public class ContentRoute
    {
        public int Id { get; set; }
    }

    public class ContentRouteService : Service
    {
        public object Any(ContentRoute request)
        {
            if (Request.ResponseContentType == MimeTypes.Html)
            {
                return AnyHtml(request);
            }
            return request;
        }

        public object Get(ContentRoute request)
        {
            if (Request.ResponseContentType == MimeTypes.Html)
                return GetHtml(request);
            
            request.Id++;
            return request;
        }

        public object AnyHtml(ContentRoute request)
        {
            return $@"
<html>
<body>
    <h1>AnyHtml {request.Id}</h1>
</body>
</html>";
        }

        public object GetHtml(ContentRoute request)
        {
            return $@"
<html>
<body>
    <h1>GetHtml {request.Id}</h1>
</body>
</html>";
        }
    }

    public class ContentTypeRouteTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(ContentTypeRouteTests), typeof(ContentRouteService).GetAssembly()) { }

            public override void Configure(Container container) { }
        }

        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            appHost = new AppHost()
                .Init()
                .Start(Constant.ListeningOn);
        }
    
        [OneTimeTearDown] public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void GET_Html_Request_calls_GetHtml()
        {
            var html = HttpUtils.GetHtmlFromUrl(Constant.ListeningOn.AppendPath("/content/1"));

            Assert.That(html, Does.Contain("<h1>GetHtml 1</h1>"));
        }

        [Test]
        public void POST_Html_Request_calls_AnyHtml()
        {
            var html = HttpUtils.GetHtmlFromUrl(Constant.ListeningOn.AppendPath("/content/1"), method: HttpMethods.Post, requestBody: "");

            Assert.That(html, Does.Contain("<h1>AnyHtml 1</h1>"));
        }

        [Test]
        public void GET_JSON_Request_calls_GetJson()
        {
            var client = new JsonServiceClient(Constant.ListeningOn);

            var response = client.Get<ContentRoute>(new ContentRoute { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(1 + 1));
        }

        [Test]
        public void POST_JSON_Request_calls_Any()
        {
            var client = new JsonServiceClient(Constant.ListeningOn);

            var response = client.Post<ContentRoute>(new ContentRoute { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(1));
        }
    }
}