using System.Net;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class ContentTypeDisabledTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() : base(nameof(ContentTypeDisabledTests), typeof(TestContentTypeService).Assembly)
            {
                Config.EnableFeatures = Feature.All.Remove(Feature.Xml | Feature.Csv | Feature.Jsv | Feature.Soap);
            }

            public override void Configure(Funq.Container container) { }

            protected override void OnAfterInit()
            {
                Config.DefaultContentType = MimeTypes.Json;
            }
        }

        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown() => appHost.Dispose();

        [Test]
        public void Disabling_XML_ContentType_fallbacks_to_DefaultContentType()
        {
            var json = Config.ListeningOn.AppendPath("testcontenttype")
                .GetStringFromUrl(
                    requestFilter: req => {
                        req.Accept = "text/xml,*/*";
                    },
                    responseFilter: res => {
                        Assert.That(res.ContentType.MatchesContentType(MimeTypes.Json));
                    });
        }

        [Test]
        public void Requesting_only_disabled_ContentType_returns_Forbidden_response()
        {
            try
            {
                Config.ListeningOn.AppendPath("testcontenttype")
                    .GetStringFromUrl(requestFilter: req => req.Accept = "text/xml");
            }
            catch (WebException ex)
            {
                Assert.That(ex.GetStatus(), Is.EqualTo(403));
            }
        }

        [Test]
        public void Disabling_XML_ContentType_prevents_posting_XML()
        {
            var client = new XmlServiceClient(Config.ListeningOn);

            try
            {
                client.Post(new TestContentType { Id = 1 });
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(403));
                Assert.That(ex.StatusDescription, Is.EqualTo(nameof(HttpStatusCode.Forbidden)));
            }
        }

        [Test]
        public void Can_use_JSON_when_other_default_ContentTypes_are_removed()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var response = client.Post(new TestContentType { Id = 1 });
            Assert.That(response.Id, Is.EqualTo(1));
        }
    }
}