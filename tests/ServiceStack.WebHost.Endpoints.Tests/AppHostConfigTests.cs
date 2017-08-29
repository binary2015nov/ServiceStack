using Funq;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class AppHostConfigTests
	{
		ServiceStackHost appHost;

		[OneTimeSetUp]
        public void TestFixtureSetUp()
		{
			appHost = new TestConfigAppHostHttpListener()
			    .Init()
			    .Start(Config.ListeningOn);
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
            appHost.Dispose();
        }
        
		[Test]
		public void Actually_uses_the_BclJsonSerializers()
		{
			var json = (Config.ListeningOn + "login/user/pass").GetJsonFromUrl();

			json.Print();
			Assert.That(json, Is.EqualTo("{\"pwd\":\"pass\",\"uname\":\"user\"}")
                .Or.EqualTo("{\"uname\":\"user\",\"pwd\":\"pass\"}"));
		}
	}

    public class TestConfigAppHostHttpListener : AppHostHttpListenerBase
    {
        public TestConfigAppHostHttpListener() : base("TestConfigAppHost Service", typeof(BclDto).GetAssembly())
        {
            Config.UseBclJsonSerializers = true;
        }

        public override void Configure(Container container)
        {

        }
    }
}
