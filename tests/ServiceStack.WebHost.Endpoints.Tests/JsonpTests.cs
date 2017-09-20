using System;
using System.Net;
using NUnit.Framework;
using ServiceStack.Logging;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	public class JsonpTests
	{
		protected static string ListeningOn = Config.ListeningOn;

		ExampleAppHostHttpListener appHost;

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			LogManager.LogFactory = new ConsoleLogFactory();

			appHost = new ExampleAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			appHost.Dispose();
		}

		[Test]
		public void Can_GET_single_Movie_using_RestClient_with_JSONP()
		{
			var url = ListeningOn + "all-movies/1?callback=cb";
			string response;

			var webReq = (HttpWebRequest)WebRequest.Create(url);
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
			Assert.That(response.Length, Is.GreaterThan(50));
		} 
	}
}
