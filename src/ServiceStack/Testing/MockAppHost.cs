using System;
using System.Collections.Generic;
using System.Reflection;
using Funq;
using ServiceStack.Host;

namespace ServiceStack.Testing
{
    public class MockAppHost : ServiceStackHost
    {
        public MockAppHost(params Assembly[] serviceAssemblies)
            : base(typeof (MockAppHost).GetOperationName(),
                   serviceAssemblies.Length > 0 ? serviceAssemblies : new[]
                   {
#if !NETSTANDARD1_6
                       Assembly.GetExecutingAssembly()
#else
                       typeof(MockAppHost).GetTypeInfo().Assembly
#endif
                   })
        {
            this.ExcludeAutoRegisteringServiceTypes = new HashSet<Type>();
            this.TestMode = true;
            Plugins.Clear();
        }

        public override void Configure(Container container)
        {
            ConfigureAppHost?.Invoke(this);
            ConfigureContainer?.Invoke(container);
        }

        public Action<Container> ConfigureContainer { get; set; }

        public Action<MockAppHost> ConfigureAppHost { get; set; }

        public Action<HostConfig> ConfigFilter { get; set; }

        public Func<MockAppHost, ServiceController> UseServiceController
        {
            set { ServiceController = value(this); }
        }

        protected override void OnBeforeInit()
        {
            ConfigFilter?.Invoke(Config);
        }
    }
}