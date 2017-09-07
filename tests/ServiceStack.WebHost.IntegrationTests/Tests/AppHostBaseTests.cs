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
            var html = Constant.ServiceStackBaseHost.AppendPath("metadata").GetStringFromUrl();
            Assert.That(html.Contains("The following operations are supported."));
        }

        [Test]
        public void Can_download_webpage_html_page()
        {
            var html = (Constant.AbsoluteBaseUri + "webpage.html").GetHtmlFromUrl();
            Assert.That(html.Contains("ServiceStack.WebHost.IntegrationTests Web Page"));
        }

        [Test]
        public void Gets_404_on_non_existant_page()
        {
            var webRes = (Constant.AbsoluteBaseUri + "nonexistant.html").GetWebResponse();
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void Gets_403_on_page_with_non_whitelisted_extension()
        {
            var webRes = Constant.ServiceStackBaseHost.AppendPath("webpage.forbidden").GetWebResponse();
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public void Can_Get_Custom_Route_Hello_response()
        {
            var response = Constant.ServiceStackBaseHost.AppendPath("hello").GetJsonFromUrl();
            Assert.AreEqual("{\"result\":\"Hello, World\"}", response);
        }
    }
}