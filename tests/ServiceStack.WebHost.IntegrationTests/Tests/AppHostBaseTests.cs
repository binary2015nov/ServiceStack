using System.Net;
using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class AppHostBaseTests
    {
        [Test]
        public void Can_download_webpage_html_page()
        {
            var html = (Constants.ServiceStackBaseHost + "webpage.html").GetHtmlFromUrl();
            Assert.That(html.Contains("ServiceStack.WebHost.IntegrationTests Web Page"));
        }

        [Test]
        public void Gets_404_on_non_existant_page()
        {
            var webRes = (Constants.ServiceStackBaseHost + "nonexistant.html").GetWebResponse();
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void Can_download_metadata_page()
        {
            var metadata = Constants.ServiceStackBaseHost.AppendPath("metadata").GetStringFromUrl();
            Assert.That(metadata.Contains("The following operations are supported."));
        }

        [Test]
        public void Gets_403_on_page_with_non_whitelisted_extension()
        {
            var webRes = Constants.ServiceStackBaseHost.AppendPath("webpage.forbidden").GetWebResponse();
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }
    }
}