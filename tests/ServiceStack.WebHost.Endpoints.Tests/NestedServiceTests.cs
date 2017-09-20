using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class NestedServiceTests
    {
        private ExampleAppHostHttpListener appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListener();
            appHost.Init();
            appHost.Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_call_nested_service_with_ServiceClient()
        {
            var client = new JsonServiceClient(Config.ListeningOn);

            var reqRoot = new Root { Id = 1 };
            Assert.That(reqRoot.ToGetUrl(), Is.EqualTo("/root/1"));
            
            var reqNested = new Root.Nested { Id = 2 };
            Assert.That(reqNested.ToGetUrl(), Is.EqualTo("/root.nested/2"));
            
            var root = client.Get(reqRoot);
            Assert.That(root.Id, Is.EqualTo(1));

            var nested = client.Get(reqNested);
            Assert.That(nested.Id, Is.EqualTo(2));
        }
    }


    [Route("/root/{Id}")]
    public class Root : IReturn<Root>
    {
        public int Id { get; set; }

        [Route("/root.nested/{Id}")]
        public class Nested : IReturn<Nested>
        {
            public int Id { get; set; }
        }
    }

    public class NestedService : Service
    {
        public object Any(Root request)
        {
            return request;
        }

        public object Any(Root.Nested request)
        {
            return request;
        }
    }

}
