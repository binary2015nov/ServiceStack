﻿using NUnit.Framework;

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
                : base("Cors Feature Tests", typeof(CorsFeatureService).GetAssembly()) { }

            public override void Configure(Funq.Container container)
            {
                Plugins.Add(new CorsFeature { AutoHandleOptionsRequests = true });
            }
        }

        ServiceStackHost appHost;

        [OneTimeSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new CorsFeaturePluginAppHostHttpListener()
                .Init()
                .Start(Constant.ServiceStackBaseHost);
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_Get_CORS_Headers_with_non_matching_OPTIONS_Request()
        {
            "{0}/corsplugin".Fmt(Constant.AbsoluteBaseUri).OptionsFromUrl(responseFilter: r =>
                {
                    Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo(CorsFeature.DefaultOrigin));
                    Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo(CorsFeature.DefaultMethods));
                    Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo(CorsFeature.DefaultHeaders));
                });
        }

        [Test]
        public void Can_Get_CORS_Headers_with_not_found_OPTIONS_Request()
        {
            "{0}/notfound".Fmt(Constant.ServiceStackBaseHost).OptionsFromUrl(responseFilter: r =>
            {
                Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo(CorsFeature.DefaultOrigin));
                Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo(CorsFeature.DefaultMethods));
                Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo(CorsFeature.DefaultHeaders));
            });
        }
    }
}