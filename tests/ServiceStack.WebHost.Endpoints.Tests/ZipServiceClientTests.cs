using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
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
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new ZipAppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown() => appHost.Dispose();

        class ZipAppHost : AppSelfHostBase
        {
            public ZipAppHost() : base(nameof(ZipServiceClientTests), typeof(HelloZipService).Assembly) { }

            public override void Configure(Funq.Container container) { }
        }

        [Test]
        public void Can_send_GZip_client_request_list()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
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
        public async Task Can_send_GZip_client_request_list_async()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = await client.PostAsync(new HelloZip
            {
                Name = "GZIP",
                Test = new List<string> { "Test" }
            });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP (1)"));
        }

        [Test]
        public void Can_send_GZip_client_request_list_HttpClient()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
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
        public async Task Can_send_GZip_client_request_list_HttpClient_async()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = await client.PostAsync(new HelloZip
            {
                Name = "GZIP",
                Test = new List<string> { "Test" }
            });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP (1)"));
        }

        [Test]
        public void Can_send_GZip_client_request()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_Deflate_client_request()
        {
            var client = new JsonServiceClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new HelloZip { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }

        [Test]
        public void Can_send_GZip_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }

        [Test]
        public void Can_send_Deflate_client_request_HttpClient()
        {
            var client = new JsonHttpClient(Config.ListeningOn)
            {
                RequestCompressionType = CompressionTypes.Deflate,
            };
            var response = client.Post(new HelloZip { Name = "Deflate" });
            Assert.That(response.Result, Is.EqualTo("Hello, Deflate"));
        }

        [Ignore("Integration Test"), Test]
        public void Can_send_gzip_client_request_ASPNET()
        {
            var client = new JsonServiceClient(Config.AspNetServiceStackBaseUri)
            {
                RequestCompressionType = CompressionTypes.GZip,
            };
            var response = client.Post(new HelloZip { Name = "GZIP" });
            Assert.That(response.Result, Is.EqualTo("Hello, GZIP"));
        }
    }
}