using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    [TestFixture]
    public class AppHostHttpListenerLongRunningBaseTests
    {
        private ServiceStackHost appHost;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new ExampleAppHostHttpListenerPool()
                .Init()
                .Start(Config.ListeningOn);

            Console.WriteLine(@"ExampleAppHost Created at {0}, listening on {1}", appHost.CreateAt, Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }   

        [Test, Ignore("Performance test")]
        public void PerformanceTest()
        {
            const int clientCount = 500;
            var threads = new List<Thread>(clientCount);
       
            ThreadPool.SetMinThreads(500, 50);
            ThreadPool.SetMaxThreads(1000, 50);           

            for (int i = 0; i < clientCount; i++)
            {
                threads.Add(new Thread(() => {
                    var html = (Config.ListeningOn + "long_running").GetStringFromUrl();
                }));
            }

            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < clientCount; i++)
            {
                threads[i].Start();
            }


            for (int i = 0; i < clientCount; i++)
            {
                threads[i].Join();
            }

            sw.Stop();

            Console.WriteLine("Elapsed time for " + clientCount + " requests : " + sw.Elapsed);
        }

        [Test, Ignore("You have to manually check the test output if there where NullReferenceExceptions!")]
        public void Rapid_Start_Stop_should_not_cause_exceptions()
        {
            var localAppHost = new ExampleAppHostHttpListener();

            for (int i = 0; i < 100; i++)
            {
                localAppHost.Start(GetBaseAddressWithFreePort());
#if !NETCORE_SUPPORT                
                localAppHost.Stop();
#endif
            }
        }

        private static string GetBaseAddressWithFreePort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            IPEndPoint endPoint = listener.LocalEndpoint as IPEndPoint;

            if (endPoint != null)
            {
                string address = endPoint.Address.ToString();
                int port = endPoint.Port;
                Uri uri = new UriBuilder("http://", address, port).Uri;

                listener.Stop();

                return uri.ToString();
            }

            throw new InvalidOperationException("Can not find a port to start the WpcsServer!");
        }
    }
}
