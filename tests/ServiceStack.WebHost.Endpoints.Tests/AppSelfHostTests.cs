using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class SpinWait : IReturn<SpinWait>
    {
        public int? Iterations { get; set; }
    }

    public class Sleep : IReturn<Sleep>
    {
        public int? ForMs { get; set; }
    }

    public class PerfServices : Service
    {
        private const int DefaultIterations = 1000 * 1000;
        private const int DefaultMs = 100;

        public object Any(SpinWait request)
        {
#if NETCORE
            int i = request.Iterations.GetValueOrDefault(DefaultIterations);
            //SpinWait.SpinUntil(i-- > 0);
#else
            Thread.SpinWait(request.Iterations.GetValueOrDefault(DefaultIterations));
#endif
            return request;
        }

        public object Any(Sleep request)
        {
            Thread.Sleep(request.ForMs.GetValueOrDefault(DefaultMs));
            return request;
        }
    }

    public class AppHostSmartPool : AppHostHttpListenerSmartPoolBase
    {
        public AppHostSmartPool() : base("SmartPool Test", typeof(PerfServices).Assembly) { }

        public override void Configure(Funq.Container container) { }
    }

    [TestFixture]
    public class AppSelfHostTests
    {
        private ServiceStackHost appHost;

        private string listeningOn;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            var port = Platform.FindFreeTcpPort(startingFrom: 5000);
            if (port < 5000)
                throw new Exception("Expected port >= 5000, got: " + port);

            listeningOn = "http://localhost:{0}/".Fmt(port);

            appHost = new AppHostSmartPool()
                .Init()
                .Start(listeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_call_SelfHost_Services()
        {
            var client = new JsonServiceClient(listeningOn);

            client.Get(new Sleep { ForMs = 100 });
            client.Get(new SpinWait { Iterations = 1000 });
        }

        [Test]
        public async Task Can_call_SelfHost_Services_async()
        {
            var client = new JsonServiceClient(listeningOn);

            var sleep = await client.GetAsync(new Sleep { ForMs = 100 });
            var spin = await client.GetAsync(new SpinWait { Iterations = 1000 });
        }
    }
}