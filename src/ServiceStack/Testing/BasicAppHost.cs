using System;
using System.Collections.Generic;
using System.Reflection;
using Funq;
using ServiceStack.Host;

namespace ServiceStack.Testing
{
    public class BasicAppHost : ServiceStackHost
    {
        public BasicAppHost(params Assembly[] serviceAssemblies) : base(typeof(BasicAppHost).GetOperationName(),
            serviceAssemblies.Length > 0 ? serviceAssemblies : new[]
            {
#if !NETSTANDARD2_0
                Assembly.GetExecutingAssembly()
#else
                typeof(BasicAppHost).Assembly
#endif
            })
        {
            TestMode = true;
            Plugins.Clear();
        }

        public override void Configure(Container container)
        {
            ConfigureAppHost?.Invoke(this);
            ConfigureContainer?.Invoke(container);
        }

        public Action<Container> ConfigureContainer { get; set; }

        public Action<BasicAppHost> ConfigureAppHost { get; set; }

        public Action<HostConfig> ConfigFilter { get; set; }

        public Func<BasicAppHost, ServiceController> UseServiceController
        {
            set => ServiceController = value(this);
        }

        protected override void OnBeforeInit()
        {
            ConfigFilter?.Invoke(Config);
        }
    }
}