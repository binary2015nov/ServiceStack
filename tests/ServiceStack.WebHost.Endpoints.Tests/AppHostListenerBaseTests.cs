using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.Host.Handlers;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class AppHostListenerBaseTests
    {
        ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureStartUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            appHost = new ExampleAppHostHttpListener()
                .Init()
                .Start(Config.ListeningOn);

            "ExampleAppHost Created at {0}, listening on {1}".Print(DateTime.Now, Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Root_path_redirects_to_metadata_page()
        {
            var html = Config.ListeningOn.GetStringFromUrl();
            Assert.That(html.Contains("The following operations are supported."));
        }

        [Test]
        public void Can_download_webpage_html_page()
        {
            var html = (Config.ListeningOn + "webpage.html").GetHtmlFromUrl();
            Assert.That(html.Contains("ServiceStack.WebHost.Endpoints.Tests Web Page"));
        }

        [Test]
        public void Can_download_requestinfo_json()
        {
            var response = (Config.ListeningOn + "?debug=requestinfo").GetJsonFromUrl().To<RequestInfoResponse>();
            Assert.IsNull(response.StartUpErrors);
        }

        [Test]
        public void Gets_404_on_non_existant_page()
        {
            var webRes = (Config.ListeningOn + "nonexistant.html").GetWebResponse();
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void Gets_403_on_page_with_non_whitelisted_extension()
        {
            var webRes = (Config.ListeningOn + "webpage.forbidden").GetWebResponse();
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public void Can_call_GetFactorial_WebService()
        {
            var client = new XmlServiceClient(Config.ListeningOn);
            var request = new GetFactorial { ForNumber = 3 };
            var response = client.Send<GetFactorialResponse>(request);

            Assert.That(response.Result, Is.EqualTo(GetFactorialService.GetFactorial(request.ForNumber)));
        }

        [Test]
        public void Can_call_jsv_debug_on_GetFactorial_WebService()
        {
            string url = Config.ListeningOn + "jsv/reply/GetFactorial?ForNumber=3&debug=true";
            var contents = url.GetStringFromUrl();

            Console.WriteLine("JSV DEBUG: " + contents);

            Assert.That(contents, Is.Not.Null);
        }

        [Test]
        public void Calling_missing_web_service_does_not_break_HttpListener()
        {
            var missingUrl = Config.ListeningOn + "missing.html";
            int errorCount = 0;
            try
            {
                missingUrl.GetStringFromUrl();
            }
            catch (Exception ex)
            {
                errorCount++;
                Console.WriteLine("Error [{0}]: {1}", ex.GetType().Name, ex.Message);
            }
            try
            {
                missingUrl.GetStringFromUrl();
            }
            catch (Exception ex)
            {
                errorCount++;
                Console.WriteLine("Error [{0}]: {1}", ex.GetType().Name, ex.Message);
            }

            Assert.That(errorCount, Is.EqualTo(2));
        }

        [Test]
        public void Can_call_MoviesZip_WebService()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            var request = new MoviesZip();
            var response = client.Send<MoviesZipResponse>(request);

            Assert.That(response.Movies.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Calling_not_implemented_method_returns_405()
        {
            var client = new JsonServiceClient(Config.ListeningOn);
            try
            {
                var response = client.Put<MoviesZipResponse>("all-movies.zip", new MoviesZip());
                Assert.Fail("Should throw 405 excetpion");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(405));
            }
        }

        [Test]
        public void Can_GET_single_gethttpresult_using_RestClient_with_JSONP_from_service_returning_HttpResult()
        {
            var url = Config.ListeningOn + "gethttpresult?callback=cb";
            string response;

            var webReq = WebRequest.CreateHttp(url);
            webReq.Accept = "*/*";
            using (var webRes = webReq.GetResponse())
            {
                Assert.That(webRes.ContentType, Does.StartWith(MimeTypes.JavaScript));
                response = webRes.ReadToEnd();
            }

            Assert.That(response, Is.Not.Null, "No response received");
            Console.WriteLine(response);
            Assert.That(response, Does.StartWith("cb("));
            Assert.That(response, Does.EndWith(")"));
        }

#if !NETCORE_SUPPORT
        [Test]
        public void Can_infer_handler_path_from_listener_uris()
        {
            var map = new Dictionary<string, string> {
                {"http://*:1337/",null},
                {"http://localhost:1337/",null},
                {"http://127.0.0.1:1337/",null},
                {"http://*/",null},
                {"http://localhost/",null},
                {"http://localhost:1337/subdir/","subdir"},
                {"http://localhost:1337/subdir/subdir2/","subdir/subdir2"},
            };

            foreach (var entry in map)
            {
                var handlerPath = ServiceStack.Host.HttpListener.ListenerRequest.GetHandlerPathIfAny(entry.Key);
                Assert.That(handlerPath, Is.EqualTo(entry.Value));
            }
        }
#endif

    }
}
