using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    public abstract class AsyncTaskTestsBase
    {
        protected abstract IServiceClient CreateServiceClient();

        private const int Param = 3;

        [Test]
        public void GetSync_GetFactorialGenericSync()
        {
            var response = CreateServiceClient().Get(new GetFactorialSync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialGenericAsync()
        {
            var response = CreateServiceClient().Get(new GetFactorialGenericAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialObjectAsync()
        {
            var response = CreateServiceClient().Get(new GetFactorialObjectAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialAwaitAsync()
        {
            var response = CreateServiceClient().Get(new GetFactorialAwaitAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialDelayAsync()
        {
            var response = CreateServiceClient().Get(new GetFactorialDelayAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public void GetSync_GetFactorialUnmarkedAsync()
        {
            var response = CreateServiceClient().Get<GetFactorialResponse>(
                new GetFactorialUnmarkedAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }


        [Test]
        public async Task GetAsync_GetFactorialGenericSync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialSync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialGenericAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialGenericAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialObjectAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialObjectAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialAwaitAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialAwaitAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialDelayAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialDelayAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialNewTaskAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialNewTaskAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialNewTcsAsync()
        {
            var response = await CreateServiceClient().GetAsync(new GetFactorialNewTcsAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialUnmarkedAsync()
        {
            var response = await CreateServiceClient().GetAsync<GetFactorialResponse>(
                new GetFactorialUnmarkedAsync { ForNumber = Param });
            Assert.That(response.Result, Is.EqualTo(FactorialService.GetFactorial(Param)));
        }

        [Test]
        public async Task GetAsync_GetFactorialVoidAsync()
        {
            await CreateServiceClient()
                .GetAsync(new GetFactorialVoidAsync { ForNumber = Param });
        }


        [TestFixture]
        public class JsonAsyncRestServiceClientTests : AsyncTaskTestsBase
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new JsonServiceClient(Constants.ServiceStackBaseHost);
            }
        }

        [TestFixture]
        public class JsvAsyncRestServiceClientTests : AsyncTaskTestsBase
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new JsvServiceClient(Constants.ServiceStackBaseHost);
            }
        }

        [TestFixture]
        public class XmlAsyncRestServiceClientTests : AsyncTaskTestsBase
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new XmlServiceClient(Constants.ServiceStackBaseHost);
            }
        }

        [TestFixture]
        public class CsvAsyncRestServiceClientTests : AsyncTaskTestsBase
        {
            protected override IServiceClient CreateServiceClient()
            {
                return new CsvServiceClient(Constants.ServiceStackBaseHost);
            }
        }
    }

    [TestFixture, Ignore("stand alone")]
    public class AsyncLoadTests
    {
        const int NoOfTimes = 1000;

        [Test]
        public void Load_test_GetFactorialSync_sync()
        {
            var client = new JsonServiceClient(Constants.ServiceStackBaseHost);

            for (var i = 0; i < NoOfTimes; i++)
            {
                var response = client.Get(new GetFactorialSync { ForNumber = 3 });
                if (i % 100 == 0)
                {
                    "{0}: {1}".Print(i, response.Result);
                }
            }
        }

        [Test]
        public Task Load_test_GetFactorialSync_async()
        {
            var client = new JsonServiceClient(Constants.ServiceStackBaseHost);

            int i = 0;

            var fetchTasks = NoOfTimes.Times(() =>
                client.GetAsync(new GetFactorialSync { ForNumber = 3 })
                .ContinueWith(t =>
                {
                    if (++i % 100 == 0)
                    {
                        "{0}: {1}".Print(i, t.Result.Result);
                    }
                }));

            return Task.WhenAll(fetchTasks);
        }

        [Test]
        public void Load_test_GetFactorialGenericAsync_sync()
        {
            var client = new JsonServiceClient(Constants.ServiceStackBaseHost);

            for (var i = 0; i < NoOfTimes; i++)
            {
                var response = client.Get(new GetFactorialGenericAsync { ForNumber = 3 });
                if (i % 100 == 0)
                {
                    "{0}: {1}".Print(i, response.Result);
                }
            }
        }

        [Test]
        public Task Load_test_GetFactorialGenericAsync_async()
        {
            var client = new JsonServiceClient(Constants.ServiceStackBaseHost);

            int i = 0;

            var fetchTasks = NoOfTimes.Times(() =>
                client.GetAsync(new GetFactorialGenericAsync { ForNumber = 3 })
                .ContinueWith(t =>
                {
                    if (++i % 100 == 0)
                    {
                        "{0}: {1}".Print(i, t.Result.Result);
                    }
                }));

            return Task.WhenAll(fetchTasks);
        }
    }
}