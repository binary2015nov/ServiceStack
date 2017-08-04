using NUnit.Framework;
using ServiceStack.Host.Handlers;
using ServiceStack.Testing;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class AllowFilesTests
    {
        [Test]
        public void Does_allow_valid_FilePaths()
        {
            using (new MockAppHost
            {
                ConfigFilter = config =>
                {
                    config.AllowFileExtensions.Add("aaa");
                    config.AllowFilePaths.Add("dir/**/*.zzz");
                }
            }.Init())
            {
                Assert.That(StaticFileHandler.ShouldAllow("a.js"));
                Assert.That(StaticFileHandler.ShouldAllow("a.aaa"));
                Assert.That(StaticFileHandler.ShouldAllow("dir/a/b/c/a.aaa"));
                Assert.That(!StaticFileHandler.ShouldAllow("a.zzz"));
                Assert.That(StaticFileHandler.ShouldAllow("dir/a.zzz"));
                Assert.That(StaticFileHandler.ShouldAllow("dir/a/b/c/a.zzz"));

                Assert.That(!StaticFileHandler.ShouldAllow("a.json"));
                Assert.That(StaticFileHandler.ShouldAllow("jspm_packages/a.json"));
                Assert.That(StaticFileHandler.ShouldAllow("jspm_packages/a/b/c/a.json"));
            }
        }
    }
}