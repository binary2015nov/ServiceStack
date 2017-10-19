using Check.ServiceModel;
using NUnit.Framework;
using ServiceStack;

namespace CheckWeb
{
    [TestFixture]
    public class CheckWebTests
    {
        public const string BaseUrl = "http://localhost:55799/";

        [Test]
        public void Can_send_echoes_POST()
        {
            var client = new JsonServiceClient(BaseUrl);

            var response = client.Post(new Echoes { Sentence = "Foo" });
        }
    }
}