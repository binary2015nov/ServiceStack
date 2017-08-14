using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.ProtoBuf;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class CachedServiceTests
	{
        ExampleAppHostHttpListener appHost;

        [OneTimeSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(Constant.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_call_Cached_WebService_with_JSON()
        {
            var client = new JsonServiceClient(Constant.ServiceStackBaseHost);

            var response = client.Get<MoviesResponse>("/cached/movies");

            Assert.That(response.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public void Can_call_Cached_WebService_with_JSON_string()
        {
            var client = new JsonServiceClient(Constant.ServiceStackBaseHost);

            var response = client.Get<string>("/cached-string/TEXT");

            Assert.That(response, Is.EqualTo("TEXT"));
        }

        [Test]
        public void Can_call_CachedWithTimeout_WebService_with_JSON()
        {
            var client = new JsonServiceClient(Constant.ServiceStackBaseHost);

            var response = client.Get<MoviesResponse>("/cached-timeout/movies");

            Assert.That(response.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public void Can_call_CachedWithTimeout_and_Redis_WebService_with_JSON()
        {
            var client = new JsonServiceClient(Constant.ServiceStackBaseHost);

            var response = client.Get<MoviesResponse>("/cached-timeout-redis/movies");

            Assert.That(response.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public void Can_call_Cached_WebService_with_ProtoBuf()
        {
            var client = new ProtoBufServiceClient(Constant.ServiceStackBaseHost);

            var response = client.Get<MoviesResponse>("/cached/movies");

            Assert.That(response.Movies.Count, Is.EqualTo(ResetMoviesService.Top5Movies.Count));
        }

        [Test]
        public void Can_call_Cached_WebService_with_JSONP()
        {
            var url = Constant.ServiceStackBaseHost.CombineWith("/cached/movies?callback=cb");
            var jsonp = url.GetJsonFromUrl();
            Assert.That(jsonp.StartsWith("cb("));
        }
    }
}