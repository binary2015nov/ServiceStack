﻿using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [Route("/hellojson/{Name}")]
    public class HelloJson
    {
        public string Name { get; set; }
    }

    public class HelloJsonResponse
    {
        public string Name { get; set; }
    }

    public class Services : Service
    {
        public object Any(HelloJson request)
        {
            return new HelloJsonResponse
            {
                Name = "Hello, {0}!".Fmt(request.Name ?? "World")
            };
        }
    }

    public class CustomFormatTests
    {
        public class AppHost : AppHostHttpListenerBase
        {
            public AppHost() : base(typeof(CustomFormatTests).Name, typeof(CustomFormatTests).Assembly)
            {
                Config.DefaultContentType = MimeTypes.Json;
                Config.EnableFeatures = Feature.All.Remove(Feature.Html);
            }

            public override void Configure(Container container) { }
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
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_get_service_with_default_content_type()
        {
            var json = Config.AbsoluteBaseUri.AppendPaths("hellojson", "World")
                .GetStringFromUrl(accept: "text/html,*/*;q=0.9");

            Assert.That(json, Is.EqualTo("{\"Name\":\"Hello, World!\"}")
                            .Or.EqualTo("{\"name\":\"Hello, World!\"}"));
        }
    }
}