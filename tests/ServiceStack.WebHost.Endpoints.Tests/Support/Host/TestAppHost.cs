using System.Reflection;
using Funq;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Host
{
	public class TestAppHost : ServiceStackHost
	{
        public TestAppHost(params Assembly[] assembliesWithServices)
            : base("Example Service", assembliesWithServices.Length > 0 ? assembliesWithServices : new[] { typeof(Nested).Assembly })
		{

		}

		public override void Configure(Container container)
		{
			container.Register<IFoo>(c => new Foo());
		}
	}

	public interface IFoo { }
	public class Foo : IFoo { }
}