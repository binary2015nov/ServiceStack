using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class ZipServiceClientTests
    { 
        [Test]
        public void Can_send_GZip_client_request_list()
        {
            var client = new JsonServiceClient(Constants.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip
            {
                Name = "GZIP",
                Test = new List<string> { "Test" }
            });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP (1)"));
        }

        [Test]
        public void Can_send_GZip_client_request_list_HttpClient()
        {
            var client = new JsonHttpClient(Constants.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip
            {
                Name = "GZIP",
                Test = new List<string> { "Test" }
            });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP (1)"));
        }

        [Test]
        public void Can_send_GZip_client_request()
        {
            var client = new JsonServiceClient(Constants.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_GZip_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Constants.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_Deflate_client_request()
        {
            var client = new JsonServiceClient(Constants.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new HelloZip { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }

        [Test]
        public void Can_send_Deflate_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Constants.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new HelloZip { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }
    }
}