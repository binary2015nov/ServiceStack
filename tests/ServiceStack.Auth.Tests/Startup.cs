using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace ServiceStack.Auth.Tests
{
    [Explicit]
    [TestFixture]
    public class RazorAppHostTests
    {
        [Ignore("")]
        [Test]
        public void Run_for_10Mins()
        {
            using (var appHost = new AppHost())
            {
                appHost.Init();
                appHost.Start("http://localhost:11002/");

                //Process.Start("http://localhost:11002/");

                //Thread.Sleep(TimeSpan.FromMinutes(10));
            }
        }
    }
}