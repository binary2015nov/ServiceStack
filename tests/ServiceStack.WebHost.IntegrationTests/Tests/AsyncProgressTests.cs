// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class AsyncProgressTests
    {
        [Test]
        public async Task Can_report_progress_when_downloading_async()
        {
            await AsyncDownloadWithProgress(new TestProgress());         
        }

        [Test]
        public async Task Can_report_progress_when_downloading_async_with_Post()
        {
            await AsyncDownloadWithProgress(new TestProgressString());
        }

        [Test]
        [Explicit("Setting Content-Length requires IIS integrated pipeline mode")]
        public async Task Can_report_progress_when_downloading_async_with_Post_bytes()
        {
            await AsyncDownloadWithProgress(new TestProgressBytes());
        }

        [Test]
        [Explicit("Setting Content-Length requires IIS integrated pipeline mode")]
        public async Task Can_report_progress_when_downloading_async_with_Post_File_bytes()
        {
            await AsyncDownloadWithProgress(new TestProgressBinaryFile());
        }

        [Test]
        [Explicit("Setting Content-Length requires IIS integrated pipeline mode")]
        public async Task Can_report_progress_when_downloading_async_with_Post_File_text()
        {
            await AsyncDownloadWithProgress(new TestProgressTextFile());
        }

        private async Task AsyncDownloadWithProgress<TResponse>(IReturn<TResponse> requestDto)
        {
            AsyncServiceClient.BufferSize = 100;          
            var asyncClient = new JsonServiceClient(Constant.ServiceStackBaseHost);
            var progress = new List<string>();

            //Note: total = -1 when 'Transfer-Encoding: chunked'
            //Available in ASP.NET or in HttpListener when downloading responses with known lengths: 
            //E.g: Strings, Files, etc.
            asyncClient.OnDownloadProgress = (done, total) =>
                                                progress.Add("{0}/{1} bytes downloaded".Fmt(done, total));

            var response = await asyncClient.PostAsync(requestDto);

            progress.Each(x => x.Print());

            Assert.That(progress.Count, Is.GreaterThan(0));
            Assert.That(progress.First(), Is.EqualTo("100/1160 bytes downloaded"));
            Assert.That(progress.Last(), Is.EqualTo("1160/1160 bytes downloaded"));     
        }
    }
}