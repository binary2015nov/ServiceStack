﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [Route("/hellozip")]
    [DataContract]
    public class HelloZip : IReturn<HelloZipResponse>
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public List<string> Test { get; set; }
    }

    [DataContract]
    public class HelloZipResponse
    {
        [DataMember]
        public string Result { get; set; }
    }

    public class HelloZipService : IService
    {
        public object Any(HelloZip request)
        {
            return request.Test == null
                ? new HelloZipResponse { Result = $"Hello, {request.Name}" }
                : new HelloZipResponse { Result = $"Hello, {request.Name} ({request.Test?.Count})" };
        }
    }

    [TestFixture]
    public class ZipServiceClientTests
    { 
        [Test]
        public void Can_send_GZip_client_request_list()
        {
            var client = new JsonServiceClient(Constant.ServiceStackBaseHost)
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
            var client = new JsonHttpClient(Constant.ServiceStackBaseHost)
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
            var client = new JsonServiceClient(Constant.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_GZip_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Constant.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_Deflate_client_request()
        {
            var client = new JsonServiceClient(Constant.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new HelloZip { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }

        [Test]
        public void Can_send_Deflate_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Constant.ServiceStackBaseHost)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new HelloZip { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }
    }
}