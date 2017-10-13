using System;
using System.IO;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    public class HelloWorldRawHttpPostTests
    {
        [Test]
        public void Post_JSON_to_HelloWorld()
        {
            var httpReq = WebRequest.CreateHttp(Constants.ServiceStackBaseHost + "/hello");
            httpReq.Method = "POST";
            httpReq.ContentType = httpReq.Accept = "application/json";

            using (var stream = httpReq.GetRequestStream())
            using (var sw = new StreamWriter(stream))
            {
                sw.Write("{\"Name\":\"World!\"}");
            }

            using (var response = httpReq.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                Assert.That(reader.ReadToEnd(), Is.EqualTo("{\"result\":\"Hello, World!\"}"));
            }
        }

        [Test]
        public void Post_JSON_to_HelloWorld_1()
        {
            var urlString = Constants.ServiceStackBaseHost.AppendPath("hello");
            var actualString = urlString.GetStringFromUrl(method: HttpMethods.Post, 
                requestBody: "{\"Name\":\"World!\"}",
                contentType: MimeTypes.Json, accept: MimeTypes.Json);
            Assert.That(actualString, Is.EqualTo("{\"result\":\"Hello, World!\"}"));
        }

        [Test]
        public void Post_JSON_to_HelloWorld_2()
        {
            var urlString = Constants.ServiceStackBaseHost.AppendPath("hello");
            var actualString = urlString.PostJsonToUrl(
                json: "{\"Name\":\"World!\"}");
            Assert.That(actualString, Is.EqualTo("{\"result\":\"Hello, World!\"}"));
        }

        [Test]
        public void Post_XML_to_HelloWorld()
        {
            var httpReq = WebRequest.CreateHttp(Constants.ServiceStackBaseHost + "/hello");
            httpReq.Method = "POST";
            httpReq.ContentType = httpReq.Accept = "application/xml";

            using (var stream = httpReq.GetRequestStream())
            using (var sw = new StreamWriter(stream))
            {
                sw.Write("<Hello xmlns=\"http://schemas.servicestack.net/types\"><Name>World!</Name></Hello>");
            }

            using (var response = httpReq.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                Assert.That(reader.ReadToEnd(), Is.EqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><HelloResponse xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.servicestack.net/types\"><Result>Hello, World!</Result></HelloResponse>"));
            }
        }

        [Test]
        public void Post_XML_to_HelloWorld_1()
        {
            var urlString = Constants.ServiceStackBaseHost.AppendPath("hello");
            var actualString = urlString.GetStringFromUrl(method: HttpMethods.Post,
                requestBody: "<Hello xmlns=\"http://schemas.servicestack.net/types\"><Name>World!</Name></Hello>",
                contentType: MimeTypes.Xml, accept: MimeTypes.Xml);
            Assert.That(actualString, Is.EqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><HelloResponse xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.servicestack.net/types\"><Result>Hello, World!</Result></HelloResponse>"));
        }

        [Test]
        public void Post_XML_to_HelloWorld_2()
        {
            var urlString = Constants.ServiceStackBaseHost.AppendPath("hello");
            var actualString = urlString.PostXmlToUrl(
                xml: "<Hello xmlns=\"http://schemas.servicestack.net/types\"><Name>World!</Name></Hello>");
            Assert.That(actualString, Is.EqualTo("<?xml version=\"1.0\" encoding=\"utf-8\"?><HelloResponse xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.servicestack.net/types\"><Result>Hello, World!</Result></HelloResponse>"));
        }
    }
}