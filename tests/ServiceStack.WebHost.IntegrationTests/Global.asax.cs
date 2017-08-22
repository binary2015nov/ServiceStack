using System;
using ServiceStack.Messaging;
using ServiceStack.MiniProfiler;

namespace ServiceStack.WebHost.IntegrationTests
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            var appHost = new AppHost();
            appHost.Init();
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (Request.IsLocal)
                Profiler.Start();
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            Profiler.Stop();

            var mqHost = HostContext.TryResolve<IMessageService>();
            if (mqHost != null)
                mqHost.Start();
        }
    }
}