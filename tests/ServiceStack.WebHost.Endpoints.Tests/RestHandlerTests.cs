﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Moq;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.Host.Handlers;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class RestHandlerTests
	{
		ServiceStackHost appHost;

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			appHost = new TestAppHost().Init();
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			appHost.Dispose();
		}

		[Test]
		public void Throws_binding_exception_when_unable_to_match_path_values()
		{
			var path = "/request/{will_not_match_property_id}/pathh";
			var request = ConfigureRequest(path);
			var response = new Mock<IHttpResponse>().Object;

			request.SetRoute(new RestPath(typeof(RequestType), path));

			try
			{
				var handler = new RestHandler();
				handler.ProcessRequestAsync(request, response, string.Empty).Wait();
				Assert.Fail("Should throw ArgumentException");
			}
			catch (AggregateException aex)
			{
				Assert.That(aex.InnerExceptions.Count, Is.EqualTo(1));
				Assert.That(aex.InnerException.GetType().Name, Is.EqualTo("ArgumentException"));
			}
		}

		[Test]
		public void Throws_binding_exception_when_unable_to_bind_request()
		{
			var path = "/request/{id}/path";
			var request = ConfigureRequest(path);
			var response = new Mock<IHttpResponse>().Object;

			var handler = new RestHandler();

			request.SetRoute(new RestPath(typeof(RequestType), path));
			

			try
			{
				handler.ProcessRequestAsync(request, response, string.Empty).Wait();
				Assert.Fail("Should throw SerializationException");
			}
			catch (AggregateException aex)
			{
				Assert.That(aex.InnerExceptions.Count, Is.EqualTo(1));
				Assert.That(aex.InnerException.GetType().Name, Is.EqualTo("SerializationException"));
			}
		}

        private IHttpRequest ConfigureRequest(string path)
        {
            var request = new Mock<IHttpRequest>();
            request.Setup(x => x.Items).Returns(new Dictionary<string, object>());
            request.Setup(x => x.QueryString).Returns(new NameValueCollection());
            request.Setup(x => x.PathInfo).Returns(path);

			return request.Object;
		}

		public class RequestType
		{
			public int Id { get; set; }
		}
	}
}
