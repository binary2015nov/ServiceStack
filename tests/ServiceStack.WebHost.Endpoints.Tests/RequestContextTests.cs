using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class RequestContextTests
	{
		private static string ListeningOn = Config.ListeningOn;

		public class HeadersAppHostHttpListener : AppHostHttpListenerBase
		{
			public HeadersAppHostHttpListener() 
                : base("Request Filters Tests", typeof(HeadersService).GetAssembly())
            {
                //Signal advanced web browsers what HTTP Methods you accept
                Config.GlobalResponseHeaders = new Dictionary<string, string> {
                    { "Access-Control-Allow-Origin", "*" },
                    { "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
                };
            }

			public override void Configure(Container container)
			{
				this.GlobalRequestFilters.Add((req, res, dto) =>
				{
					var requestFilter = dto as RequestFilter;
					if (requestFilter != null)
					{
						res.StatusCode = requestFilter.StatusCode;
						if (!requestFilter.HeaderName.IsNullOrEmpty())
						{
							res.AddHeader(requestFilter.HeaderName, requestFilter.HeaderValue);
						}
					}
				});
			}
		}

		HeadersAppHostHttpListener appHost;

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			appHost = new HeadersAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			appHost.Dispose();
		}

		public static Dictionary<string, string> GetResponseHeaders(string urlString)
		{
			var webRequest = PclExport.Instance.CreateWebRequest(urlString);

			var webResponse = PclExport.Instance.GetResponse(webRequest);

			var map = new Dictionary<string, string>();
			for (var i = 0; i < webResponse.Headers.Count; i++)
			{
				var headerKey = webResponse.Headers.AllKeys[i];
				map[headerKey] = webResponse.Headers[headerKey];
			}

			return map;
		}

		[Test]
		public void Can_resolve_CustomHeader()
		{
			var webRequest = WebRequest.CreateHttp(
				ListeningOn + "json/reply/Headers?Name=X-CustomHeader");
			webRequest.Headers["X-CustomHeader"] = "CustomValue";

			var response = JsonSerializer.DeserializeFromStream<HeadersResponse>(
				webRequest.GetResponse().GetResponseStream());

			Assert.That(response.Value, Is.EqualTo("CustomValue"));
		}

		[Test]
		public void Does_Send_Global_Headers()
		{
            var headers = GetResponseHeaders(ListeningOn + "json/reply/Headers");
			Assert.That(headers["Access-Control-Allow-Origin"], Is.EqualTo("*"));
			Assert.That(headers["Access-Control-Allow-Methods"], Is.EqualTo("GET, POST, PUT, DELETE, OPTIONS"));
		}

		[Test]
		public void Does_return_bare_401_StatusCode()
		{
			try
			{
				var webRequest = WebRequest.CreateHttp(
					ListeningOn + "json/reply/RequestFilter?StatusCode=401");

				webRequest.GetResponse();

				Assert.Fail("Should throw 401 WebException");
			}
			catch (WebException ex)
			{
				var httpResponse = (HttpWebResponse)ex.Response;
				Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
			}
		}

		[Test]
		public void Does_return_bare_401_with_AuthRequired_header()
		{
			try
			{
				var webRequest = (HttpWebRequest)WebRequest.Create(ListeningOn 
					+ "json/reply/RequestFilter?StatusCode=401"
					+ "&HeaderName=" + HttpHeaders.WwwAuthenticate
					+ "&HeaderValue=" + "Basic realm=\"Auth Required\"".UrlEncode());

				webRequest.GetResponse();

				Assert.Fail("Should throw 401 WebException");
			}
			catch (WebException ex)
			{
				var httpResponse = (HttpWebResponse)ex.Response;
				Assert.That(httpResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

				Assert.That(ex.Response.Headers[HttpHeaders.WwwAuthenticate],
					Is.EqualTo("Basic realm=\"Auth Required\""));
			}
		}


	}
}
