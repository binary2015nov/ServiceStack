using System;
using System.Diagnostics;
using System.Threading;
using Funq;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class BuiltinRouteServices : Service {}

    public class BuiltinRouteTests
    {
        public class BuiltinPathAppHost : AppSelfHostBase
        {
            public BuiltinPathAppHost()
                : base(typeof(BuiltinPathAppHost).Name, typeof(BuiltinRouteServices).GetAssembly()) { }

            public override void Configure(Container container)
            {
                PreRequestFilters.Add((req, res) =>
                {
                    req.UseBufferedStream = true;
                    res.UseBufferedStream = true;
                });
            }
        }

        ServiceStackHost appHost;
        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            appHost = new BuiltinPathAppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

        [Test]
        public void Can_download_metadata_page()
        {
            var contents = "{0}/metadata".Fmt(Config.AbsoluteBaseUri).GetStringFromUrl();
            Assert.That(contents, Does.Contain("The following operations are supported."));
        }

        [Test]
        public void Can_download_File_Template_OperationControl()
        {
            var contents = "{0}/json/metadata?op=Hello".Fmt(Config.AbsoluteBaseUri).GetStringFromUrl();
            Assert.That(contents, Does.Contain("/hello/{Name}"));
        }
    }
}