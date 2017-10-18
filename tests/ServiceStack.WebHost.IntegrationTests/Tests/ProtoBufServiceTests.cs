using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.ProtoBuf;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class ProtoBufServiceTests
    {
        [Test]
        public void Can_Send_ProtoBuf_request()
        {
            var client = new ProtoBufServiceClient(Constants.ServiceStackBaseHost)
            {
                RequestFilter = req =>
                    Assert.That(req.Accept, Is.EqualTo(MimeTypes.ProtoBuf))
            };

            var request = CreateProtoBufEmail();
            var response = client.Send<ProtoBufEmail>(request);

            response.PrintDump();
            Assert.That(response.FromAddress, Is.EqualTo(request.FromAddress));
            Assert.That(response.ToAddress, Is.EqualTo(request.ToAddress));
            Assert.That(response.Subject, Is.EqualTo(request.Subject));
            Assert.That(response.Body, Is.EqualTo(request.Body));
            Assert.That(response.AttachmentData, Is.EqualTo(request.AttachmentData));
        }

        [Test]
        public async Task Can_Send_ProtoBuf_request_Async()
        {
            var client = new ProtoBufServiceClient(Constants.ServiceStackBaseHost)
            {
                RequestFilter = req =>
                    Assert.That(req.Accept, Is.EqualTo(MimeTypes.ProtoBuf))
            };

            var request = CreateProtoBufEmail();
            var response = await client.SendAsync<ProtoBufEmail>(request);

            response.PrintDump();
            Assert.That(response.FromAddress, Is.EqualTo(request.FromAddress));
            Assert.That(response.ToAddress, Is.EqualTo(request.ToAddress));
            Assert.That(response.Subject, Is.EqualTo(request.Subject));
            Assert.That(response.Body, Is.EqualTo(request.Body));
            Assert.That(response.AttachmentData, Is.EqualTo(request.AttachmentData));
        }

        [Test]
        public void Does_return_ProtoBuf_when_using_ProtoBuf_Content_Type_and_Wildcard()
        {
            var bytes = Constants.ServiceStackBaseHost.AppendPath("x-protobuf/reply/ProtoBufEmail")
                .PostBytesToUrl(accept: "{0}, */*".Fmt(MimeTypes.ProtoBuf),
                    requestBody: CreateProtoBufEmail().ToProtoBuf(),
                    responseFilter: res => Assert.That(res.ContentType, Is.EqualTo(MimeTypes.ProtoBuf)));

            Assert.That(bytes.Length, Is.GreaterThan(0));

            bytes = Constants.ServiceStackBaseHost.AppendPath("x-protobuf/reply/ProtoBufEmail")
                .GetBytesFromUrl(accept: "{0}, */*".Fmt(MimeTypes.ProtoBuf),
                    responseFilter: res => Assert.That(res.ContentType, Is.EqualTo(MimeTypes.ProtoBuf)));
        }

        private static ProtoBufEmail CreateProtoBufEmail()
        {
            var request = new ProtoBufEmail
            {
                ToAddress = "to@email.com",
                FromAddress = "from@email.com",
                Subject = "Subject",
                Body = "Body",
                AttachmentData = Encoding.UTF8.GetBytes("AttachmentData"),
            };
            return request;
        }

    }
}