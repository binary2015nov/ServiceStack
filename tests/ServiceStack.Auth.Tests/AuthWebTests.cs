﻿using System.Collections.Generic;
using System.Net;
using NUnit.Framework;

namespace ServiceStack.Auth.Tests
{
    [TestFixture]
    public class AuthWebTests
    {
        public const string BaseUri = "http://localhost:11002/";

        private AppHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new AppHost();
            appHost.Init();
            appHost.Start(BaseUri);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown() => appHost.Dispose();

        [Test]
        public void Can_authenticate_with_HTTP_Basic_Authentication()
        {
            var client = new JsonServiceClient(BaseUri);
            client.UserName = "mythz";
            client.Password = "test";
            var response = client.Get(new RequiresAuth { Name = "Haz Access!" });
            Assert.That(response.Name, Is.EqualTo("Haz Access!"));
        }

        [Test]
        public void Can_Authenticate_with_Metadata()
        {
            var client = new JsonServiceClient(BaseUri);

            var response = client.Send(new Authenticate
            {
                UserName = "demis.bellot@gmail.com",
                Password = "test",
                Meta = new Dictionary<string, string> { { "custom", "metadata" } }
            });
        }
    }
}