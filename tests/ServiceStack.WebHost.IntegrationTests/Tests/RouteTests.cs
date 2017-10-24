using NUnit.Framework;
using ServiceStack.Host.Handlers;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [Route("/routeinfo/{Path*}")]
    public class GetRouteInfo
    {
        public string Path { get; set; }
    }

    public class GetRouteInfoResponse
    {
        public string BaseUrl { get; set; }
        public string ResolvedUrl { get; set; }
    }

    public class RouteInfoService : Service
    {
        public object Any(GetRouteInfo request)
        {
            return new GetRouteInfoResponse
            {
                BaseUrl = base.Request.GetBaseUrl(),
                ResolvedUrl = base.Request.ResolveAbsoluteUrl("~/resolved")
            };
        }
    }

    public class RouteInfoPathTests
    {
        [Test]
        public void ApiPath_returns_BaseUrl()
        {
            var url = Constants.ServiceStackBaseHost;

            var reqInfoResponse = url.AddQueryParam("debug", "requestinfo")
                .GetJsonFromUrl().FromJson<RequestInfoResponse>();
            Assert.That(reqInfoResponse.ApplicationBaseUrl, Is.EqualTo(url));
            Assert.That(reqInfoResponse.ResolveAbsoluteUrl, Is.EqualTo(url.AppendPath("resolve")));

            var response = url.AppendPath("/routeinfo").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
            Assert.That(response.BaseUrl.TrimEnd('/'), Is.EqualTo(url.TrimEnd('/')));
            Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

            response = url.AppendPath("/routeinfo/dir").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
            Assert.That(response.BaseUrl, Is.EqualTo(url));
            Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));

            response = url.AppendPath("/routeinfo/dir/sub").GetJsonFromUrl().FromJson<GetRouteInfoResponse>();
            Assert.That(response.BaseUrl, Is.EqualTo(url));
            Assert.That(response.ResolvedUrl, Is.EqualTo(url.AppendPath("resolved")));
        }
    }
}