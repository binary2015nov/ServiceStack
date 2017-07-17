using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class HelloWorldServiceClientTests
    {
        public static IEnumerable ServiceClients
        {
            get
            {
                return new IServiceClient[] {
                    new JsonServiceClient(Constant.ServiceStackBaseUri),
                    new JsvServiceClient(Constant.ServiceStackBaseUri),
                    new XmlServiceClient(Constant.ServiceStackBaseUri),
                    new Soap11ServiceClient(Constant.ServiceStackBaseUri),
                    new Soap12ServiceClient(Constant.ServiceStackBaseUri)
                };
            }
        }

        public static IEnumerable RestClients
        {
            get
            {
                return new IRestClient[] {
                    new JsonServiceClient(Constant.ServiceStackBaseUri),
                    new JsvServiceClient(Constant.ServiceStackBaseUri),
                    new XmlServiceClient(Constant.ServiceStackBaseUri),
                };
            }
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Sync_Call_HelloWorld_with_Sync_ServiceClients_on_PreDefined_Routes(IServiceClient client)
        {
            var response = client.Send<HelloResponse>(new Hello { Name = "World!" });

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test, TestCaseSource("RestClients")]
        public async Task Async_Call_HelloWorld_with_ServiceClients_on_PreDefined_Routes(IServiceClient client)
        {
            var response = await client.SendAsync<HelloResponse>(new Hello { Name = "World!" });

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Sync_Call_HelloWorld_with_RestClients_on_UserDefined_Routes(IRestClient client)
        {
            var response = client.Get<HelloResponse>("/hello/World!");

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }

        [Test, TestCaseSource("RestClients")]
        public async Task Async_Call_HelloWorld_with_Async_ServiceClients_on_UserDefined_Routes(IServiceClient client)
        {
            var response = await client.GetAsync<HelloResponse>("/hello/World!");

            Assert.That(response.Result, Is.EqualTo("Hello, World!"));
        }
    }
}