using System.Runtime.Serialization;
using NUnit.Framework;
using ServiceStack.Metadata;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class XmlMetaDataHandlerTests
    {
        [Test]
        public void when_creating_a_response_for_a_dto_with_default_constructor_the_response_is_not_empty()
        {
            var handler = new XmlMetadataHandler();
            var xmlResponse = handler.CreateResponse(typeof(DefaultConstructor));
            xmlResponse.Print();
            Assert.That(xmlResponse, Does.StartWith(
                "<DefaultConstructor xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.servicestack.net/types\">"));
        }

        [Test]
        public void when_creating_a_response_for_a_dto_with_no_default_constructor_the_response_is_not_empty()
        {
            var handler = new XmlMetadataHandler();
            var xmlResponse = handler.CreateResponse(typeof(NoDefaultConstructor));
            xmlResponse.Print();
            Assert.That(xmlResponse, Does.StartWith(
                "<NoDefaultConstructor xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.servicestack.net/types\">"));
        }
    }

    [DataContract(Namespace = "http://schemas.servicestack.net/types")]
    public class DefaultConstructor
    {
        [DataMember]
        public string Value { get; set; }
    }

    [DataContract(Namespace = "http://schemas.servicestack.net/types")]
    public class NoDefaultConstructor
    {
        public NoDefaultConstructor(string test) { }

        [DataMember]
        public string Value { get; set; }
    }
}
