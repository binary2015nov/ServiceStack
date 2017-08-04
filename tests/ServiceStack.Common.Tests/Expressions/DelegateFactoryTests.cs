using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using ServiceStack.Reflection;

namespace ServiceStack.Common.Tests.Expressions
{
    [TestFixture]
    public class DelegateFactoryTests
    {
        private const string TextValue = "Hello, World!";
        private const int Times = 10000;

        [Test]
        public void String_test_with_direct_call()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            for (var i = 0; i < Times; i++)
            {
                TextValue.ToUpper();
            }

            stopWatch.Stop();
            Console.WriteLine("Totally took: {0}ms", stopWatch.ElapsedMilliseconds);
        }

        [Test]
        public void String_test_with_func_call()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Func<string> action = TextValue.ToUpper;

            for (var i = 0; i < Times; i++)
            {
                action();
            }

            stopWatch.Stop();
            Console.WriteLine("Totally took: {0}ms", stopWatch.ElapsedMilliseconds);
        }

        [Test]
        public void String_test_with_reflection()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var methodInfo = typeof(string).GetMethod("ToUpper", new Type[] { });

            for (var i = 0; i < Times; i++)
            {
                methodInfo.Invoke(TextValue, new object[] { });
            }

            stopWatch.Stop();
            Console.WriteLine("Totally took: {0}ms", stopWatch.ElapsedMilliseconds);
        }

        [Test]
        public void String_test_with_delegate()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var method = typeof(string).GetMethod("ToUpper", new Type[] { });
            var delMethod = DelegateFactory.Create(method);

            for (var i = 0; i < Times; i++)
            {
                delMethod(TextValue, new object[] { });
            }

            stopWatch.Stop();
            Console.WriteLine("Totally took: {0}ms", stopWatch.ElapsedMilliseconds);
        }

        [Test]
        public void Delegate_factory_create_method_timing()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var method = typeof(string).GetMethod("ToUpper", new Type[] { });
            DelegateFactory.Create(method);

            stopWatch.Stop();
            Console.WriteLine("Totally took: {0}ms", stopWatch.ElapsedMilliseconds);
        }
    }
}