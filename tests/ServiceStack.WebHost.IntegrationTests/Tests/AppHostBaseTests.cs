using System;
using System.Net;
using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class AppHostBaseTests
    {
        [Test]
        public void Can_download_metadata_page()
        {
            var html = HttpUtils.GetHtmlFromUrl(Constant.ServiceStackBaseHost.AppendPath("metadata"));
            Assert.That(html.Contains("The following operations are supported."));
        }

        [Test]
        public void Can_download_webpage_html_page()
        {
            var html = HttpUtils.GetHtmlFromUrl(Constant.AbsoluteBaseUri + "webpage.html");
            Assert.That(html.Contains("Default index ServiceStack.WebHost.Endpoints.Tests page"));
        }

        [Test]
        public void Gets_404_on_non_existant_page()
        {
            var webRes = HttpUtils.GetWebResponse(Constant.AbsoluteBaseUri + "nonexistant.html");
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void Gets_403_on_page_with_non_whitelisted_extension()
        {
            var webRes = HttpUtils.GetWebResponse(Constant.ServiceStackBaseHost.AppendPath("webpage.forbidden"));
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public void Gets_csv_Hello_response()
        {
            var response = HttpUtils.GetCsvFromUrl(Constant.ServiceStackBaseHost.AppendPath("hello"));
            Assert.That(response.Contains("Hello, World"));
        }
    }
}