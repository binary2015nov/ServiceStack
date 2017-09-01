using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/testcontenttype")]
    public class TestContentType : IReturn<TestContentType>
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TestContentTypeService : Service
    {
        public object Any(TestContentType request)
        {
            return request;
        }
    }


    [TestFixture]
    public class ContentTypeTests
    {
        private static string ListeningOn = Config.ListeningOn;

        ExampleAppHostHttpListener appHost;
        readonly JsonServiceClient client = new JsonServiceClient(ListeningOn);

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Does_return_JSON()
        {
            var json = ListeningOn.AppendPath("testcontenttype")
                .AddQueryParam("id", 1)
                .GetStringFromUrl(accept: MimeTypes.Json,
                    responseFilter: res =>
                        Assert.That(res.ContentType.MatchesContentType(MimeTypes.Json)));

            var dto = json.FromJson<TestContentType>();
            Assert.That(dto.Id, Is.EqualTo(1));
        }

        [Test]
        public void Does_return_JSON_UpperCase()
        {
            var json = ListeningOn.AppendPath("testcontenttype")
                .AddQueryParam("id", 1)
                .GetStringFromUrl(accept: MimeTypes.Json.ToUpper(),
                    responseFilter: res =>
                        Assert.That(res.ContentType.MatchesContentType(MimeTypes.Json)));

            var dto = json.FromJson<TestContentType>();
            Assert.That(dto.Id, Is.EqualTo(1));
        }

        [Test]
        public void Does_return_JSON_extension()
        {
            var json = ListeningOn.AppendPath("testcontenttype.json")
                .AddQueryParam("id", 1)
                .GetStringFromUrl(responseFilter: res =>
                        Assert.That(res.ContentType.MatchesContentType(MimeTypes.Json)));

            var dto = json.FromJson<TestContentType>();
            Assert.That(dto.Id, Is.EqualTo(1));
        }

        [Test]
        public void Does_return_JSON_format()
        {
            var json = ListeningOn.AppendPath("testcontenttype")
                .AddQueryParam("id", 1)
                .AddQueryParam("format", "json")
                .GetStringFromUrl(responseFilter: res =>
                        Assert.That(res.ContentType.MatchesContentType(MimeTypes.Json)));

            var dto = json.FromJson<TestContentType>();
            Assert.That(dto.Id, Is.EqualTo(1));
        }       
    }
}