﻿using NUnit.Framework;
using ServiceStack.ProtoBuf;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class CustomHeadersTests
    {
        [Test]
        public void GetRequest()
        {
            var client = new JsonServiceClient(Constants.ServiceStackBaseHost);
            client.Headers.Add("Foo", "abc123");
            var response = client.Get(new CustomHeaders());
            Assert.That(response.Foo, Is.EqualTo("abc123"));
            Assert.That(response.Bar, Is.Null);
        }

        [Test]
        public void PostRequest()
        {
            var client = new XmlServiceClient(Constants.ServiceStackBaseHost);
            client.Headers.Add("Bar", "abc123");
            client.Headers.Add("Foo", "xyz");
            var response = client.Post(new CustomHeaders());
            Assert.That(response.Bar, Is.EqualTo("abc123"));
            Assert.That(response.Foo, Is.EqualTo("xyz"));
        }

        [Test]
        public void Delete()
        {
            var client = new ProtoBufServiceClient(Constants.ServiceStackBaseHost);
            client.Headers.Add("Bar", "abc123");
            client.Headers.Add("Foo", "xyz");
            var response = client.Delete(new CustomHeaders());
            Assert.That(response.Bar, Is.EqualTo("abc123"));
            Assert.That(response.Foo, Is.EqualTo("xyz"));
        }
    }
}