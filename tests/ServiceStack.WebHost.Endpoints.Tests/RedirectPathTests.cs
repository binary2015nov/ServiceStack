using Funq;
using NUnit.Framework;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class RedirectPathTests
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(RedirectPathTests), typeof(RedirectPathTests).Assembly)
            {
                Config.DefaultRedirectPath = "~/does-resolve";
            }

            public override void Configure(Container container) { }

            public override string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
            {
                return virtualPath == "~/does-resolve"
                    ? base.ResolveAbsoluteUrl("~/webpage.html", httpReq)
                    : base.ResolveAbsoluteUrl(virtualPath, httpReq);
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
        public void DefaultRedirectPath_RelativeUrl_does_resolve()
        {
            var html = Config.ListeningOn.GetStringFromUrl();
            Assert.That(html, Does.Contain("ServiceStack.WebHost.Endpoints.Tests Web Page"));
        }
    }
}