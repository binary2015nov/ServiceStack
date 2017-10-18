using NUnit.Framework;
using ServiceStack.ProtoBuf;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class CachedServiceTests
    {
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            var jsonClient = new JsonServiceClient(Constants.ServiceStackBaseHost);
            jsonClient.Post<ResetMoviesResponse>("reset-movies", new ResetMovies());
        }

        [Test]
        public void Can_call_Cached_WebService_with_JSON()
        {
            var client = new JsonServiceClient(Constants.ServiceStackBaseHost);

            var response = client.Get<MoviesResponse>("/cached/movies");

            Assert.That(response.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public void Can_call_Cached_WebService_with_ProtoBuf()
        {
            var client = new ProtoBufServiceClient(Constants.ServiceStackBaseHost);

            var response = client.Get<MoviesResponse>("/cached/movies");

            Assert.That(response.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public void Can_call_Cached_WebService_with_ProtoBuf_without_compression()
        {
            var client = new ProtoBufServiceClient(Constants.ServiceStackBaseHost);
            client.DisableAutoCompression = true;
            client.Get<MoviesResponse>("/cached/movies");
            var response2 = client.Get<MoviesResponse>("/cached/movies");

            Assert.That(response2.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public void Can_call_Cached_WebService_with_JSONP()
        {
            var url = Constants.ServiceStackBaseHost.AppendPath("/cached/movies?callback=cb");
            var jsonp = url.GetJsonFromUrl();
            jsonp.Print();
            Assert.That(jsonp.StartsWith("cb("));
        }
    }
}