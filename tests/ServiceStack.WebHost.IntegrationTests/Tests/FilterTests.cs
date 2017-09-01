using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class FilterTests
    {
        [Test]
        public void Can_call_service_returning_string()
        {
            var response = Constant.ServiceStackBaseHost.AppendPath("hello2/world")
                .GetJsonFromUrl();

            Assert.That(response, Is.EqualTo("world"));
        }
    }
}