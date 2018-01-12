using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using ServiceStack.Logging;

namespace ServiceStack.WebHost.Endpoints.Tests
{

    [TestFixture]
    public class HttpResultContentTypeTests
    {
        public class SimpleAppHostHttpListener : AppHostHttpListenerBase
        {
            //Tell Service Stack the name of your application and where to find your web services
            public SimpleAppHostHttpListener() : base("Test Services", typeof(SimpleAppHostHttpListener).Assembly) { }

            /// <summary>
            /// AppHostHttpListenerBase method.
            /// </summary>
            /// <param name="container">SS's funq container</param>
            public override void Configure(Funq.Container container)
            {
                HostContext.Config.GlobalResponseHeaders.Clear();

                PreRequestFilters.Add((req,res) => res.UseBufferedStream = true);

                //Signal advanced web browsers what HTTP Methods you accept
                //base.SetConfig(new EndpointHostConfig());
                Routes.Add<PlainText>("/test/plaintext", "GET");
            }
        }

        /// <summary>
        /// *Request* DTO
        /// </summary>
        public class PlainText
        {
            /// <summary>
            /// Controls if the service calls response.ContentType or just new HttpResult
            /// </summary>
            public bool SetContentType { get; set; }
            /// <summary>
            /// Text to respond with
            /// </summary>
            public string Text { get; set; }
        }

        [Route("/plain-dto")]
        public class PlainDto : IReturn<PlainDto>
        {
            public string Name { get; set; }
        }

        [Route("/httpresult-dto")]
        public class HttpResultDto : IReturn<HttpResultDto>
        {
            public string Name { get; set; }
        }

        public class HttpResultServices : Service
        {
            public object Any(PlainText request)
            {
                var contentType = "text/plain";
                var response = new HttpResult(request.Text, contentType);
                if (request.SetContentType)
                {
                    response.ContentType = contentType;
                }
                return response;
            }

            public object Any(PlainDto request) => request;

            public object Any(HttpResultDto request) => new HttpResult(request, HttpStatusCode.Created);
        }
		private static string ListeningOn = Config.ListeningOn;
		SimpleAppHostHttpListener appHost;

		[OneTimeSetUp]
		public void TestFixtureStartUp() 
		{
            LogManager.LogFactory = new TestLogFactory();
            appHost = new SimpleAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);

			System.Console.WriteLine("ExampleAppHost Created at {0}, listening on {1}",
			                         appHost.CreatedAt, ListeningOn);
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			appHost.Dispose();

            //Clear the logs so other tests dont inherit log entries
            TestLogger.GetLogs().Clear();
        }

        [Test]
        public void When_Buffered_does_not_return_ChunkedEncoding_for_DTO_responses()
        {
            var response = Config.ListeningOn.CombineWith("plain-dto").AddQueryParam("name", "foo")
                .GetJsonFromUrl(responseFilter: res =>
                {
                    res.Headers[HttpHeaders.TransferEncoding].Print();
                    Assert.That(res.Headers[HttpHeaders.TransferEncoding], Is.Null);
                    Assert.That(res.ContentLength, Is.GreaterThan(0));
                }).FromJson<PlainDto>();

            Assert.That(response.Name, Is.EqualTo("foo"));
        }

        [Test]
        public void When_Buffered_does_not_return_ChunkedEncoding_for_DTO_responses_in_HttpResult()
        {
            var response = Config.ListeningOn.CombineWith("httpresult-dto").AddQueryParam("name", "foo")
                .GetJsonFromUrl(responseFilter: res =>
                {
                    res.Headers[HttpHeaders.TransferEncoding].Print();
                    Assert.That(res.Headers[HttpHeaders.TransferEncoding], Is.Null);
                    Assert.That(res.ContentLength, Is.GreaterThan(0));
                }).FromJson<HttpResultDto>();

            Assert.That(response.Name, Is.EqualTo("foo"));
        }

        /// <summary>
        /// This test calls a simple web service which uses HttpResult(string responseText, string contentType) constructor 
        /// to set the content type. 
        /// 
        /// 
        /// </summary>
        /// <param name="setContentTypeBrutally">If true the service additionally 'brutally' sets the content type after using HttpResult constructor which should do it anyway</param>
        //This test case fails on mono 2.6.7 (the content type is 'text/html')
        [TestCase(false)]
        [TestCase(true)]
        public void TestHttpRestulSettingContentType(bool setContentTypeBrutally) {
            string text = "Some text";
            string url = string.Format("{0}/test/plaintext?SetContentTypeBrutally={1}&Text={2}", ListeningOn, setContentTypeBrutally,text);
            HttpWebRequest req = WebRequest.CreateHttp(url);

            HttpWebResponse res = null;
            try
            {
                res = (HttpWebResponse)req.GetResponse();

                string downloaded;
                using (StreamReader s = new StreamReader(res.GetResponseStream()))
                {
                    downloaded = s.ReadToEnd();
                }

                Assert.AreEqual(text, downloaded, "Checking the downloaded string");

                Assert.AreEqual("text/plain", res.ContentType, "Checking for expected contentType");
            }
            finally
            {
                res?.Close();
            }
        }
    }
}
