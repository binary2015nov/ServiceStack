using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ServiceStack.Common.Tests
{
    [TestFixture, Ignore("Benchmark")]
    public class PerfUtilsTests
    {
        [Test]
        public void Measure_unique_collections()
        {
            Random rand = new Random();
            var set = new HashSet<int>();
            var avgMicroSecs = PerfUtils.Measure(
                () => set.Add(rand.Next(0, 1000)), runForMs:2000);

            "HashSet: {0}us".Print(avgMicroSecs);

            var list = new List<int>();
            avgMicroSecs = PerfUtils.Measure(
                () => {
                    int i = rand.Next(0, 1000);
                    if (!list.Contains(i))
                        list.Add(i);
                }, runForMs: 2000);

            "List: {0}us".Print(avgMicroSecs);
        }
    }
}