using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Host
{
	public class TestAppHostHttpListener : AppHostHttpListenerBase
	{
		public TestAppHostHttpListener() : base("Example Service", typeof(TestService).Assembly) { }

		public override void Configure(Funq.Container container)
		{
			container.Register<IFoo>(c => new Foo());
		}
	}
}