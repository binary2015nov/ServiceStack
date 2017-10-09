using Funq;
using NUnit.Framework;
using ServiceStack.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture, Ignore("Uncomment the commented code to test")]
    public class SharedDtoTests
    {
        [Route("/shareddto")]
        public class RequestDto : IReturn<ResponseDto> { }

        public class ResponseDto
        {
            public string ServiceName { get; set; }
        }

        public class Service1 : IService
        {
            public object Get(RequestDto req)
            {
                return new ResponseDto { ServiceName = GetType().Name };
            }
        }

        //public class Service2 : IService
        //{
        //    public object Post(RequestDto req)
        //    {
        //        return new ResponseDto { ServiceName = GetType().Name };
        //    }
        //}

        class SharedDtoAppHost : AppHostHttpListenerBase
        {

            public SharedDtoAppHost() : base("Shared dto tests", typeof(Service1).GetAssembly()) { }

            public override void Configure(Container container) { }

            protected override ServiceController CreateServiceController()
            {
                return base.CreateServiceController();
            }
        }

        private ServiceStackHost AppHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            AppHost = new SharedDtoAppHost();
            AppHost.Init();
            AppHost.Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            AppHost.Dispose();
        }

        protected static IRestClient[] RestClients =
        {
            new JsonServiceClient(Config.ListeningOn),
            new XmlServiceClient(Config.ListeningOn),
            new JsvServiceClient(Config.ListeningOn)
        };

        [Test, TestCaseSource(nameof(RestClients))]
        public void Can_call_service1(IRestClient client)
        {
            var response = client.Get(new RequestDto());
            Assert.That(response.ServiceName, Is.EqualTo(typeof(Service1).Name));
        }

        [Test, TestCaseSource(nameof(RestClients))]
        public void Cannot_call_service2(IRestClient client)
        {
            try
            {
                var response = client.Post(new RequestDto());
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.StatusCode, Is.EqualTo(405));
                Assert.That(ex.Message, Is.EqualTo("Could not find method named {1}({0}) or Any({0}) on Service {2}"
                    .Fmt(typeof(RequestDto).GetOperationName(), "Post", typeof(Service1).GetOperationName())));
            }
        }
    }
}
