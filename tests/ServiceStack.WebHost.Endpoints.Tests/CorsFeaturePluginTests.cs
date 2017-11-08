using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/corsplugin", "GET")]
    public class CorsFeaturePlugin { }

    public class CorsFeaturePluginResponse
    {
        public bool IsSuccess { get; set; }
    }

    public class CorsFeaturePluginService : IService
    {
        public object Any(CorsFeaturePlugin request)
        {
            return new CorsFeaturePluginResponse { IsSuccess = true };
        }
    }

    [TestFixture]
    public class CorsFeaturePluginTests
    {
        public class CorsFeaturePluginAppHostHttpListener
            : AppHostHttpListenerBase
        {
            public CorsFeaturePluginAppHostHttpListener()
                : base("Cors Feature Tests", typeof(CorsFeatureService).Assembly) { }

            public override void Configure(Funq.Container container)
            {
                Plugins.Add(new CorsFeature { AutoHandleOptionsRequests = true });
            }
        }

        ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new CorsFeaturePluginAppHostHttpListener()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_Get_CORS_Headers_with_non_matching_OPTIONS_Request()
        {
            "{0}/corsplugin".Fmt(Config.AbsoluteBaseUri).OptionsFromUrl(responseFilter: r =>
                {
                    Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo(CorsFeature.DefaultOrigin));
                    Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo(CorsFeature.DefaultMethods));
                    Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo(CorsFeature.DefaultHeaders));
                });
        }

        [Test]
        public void Can_Get_CORS_Headers_with_not_found_OPTIONS_Request()
        {
            "{0}/notfound".Fmt(Config.AbsoluteBaseUri).OptionsFromUrl(responseFilter: r =>
            {
                Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo(CorsFeature.DefaultOrigin));
                Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo(CorsFeature.DefaultMethods));
                Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo(CorsFeature.DefaultHeaders));
            });
        }
    }
}