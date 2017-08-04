using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

namespace ServiceStack.Common.Tests.Expressions
{
    [TestFixture]
    public class ExpressionTests
    {
        [Test]
        public void Simple_func_and_equivalent_expression_tests()
        {
            Func<int, int> addLambda = x => x + 4;
            Assert.That(addLambda(4), Is.EqualTo(4 + 4));

            Func<int, int> addMethod = Add;
            Assert.That(addMethod(4), Is.EqualTo(4 + 4));

            Expression<Func<int, int>> addExpr = x => x + 4;
            Func<int, int> addFromExpr = addExpr.Compile();
            Assert.That(addFromExpr(4), Is.EqualTo(4 + 4));
        }

        [Test]
        public void MethodCallExpression_to_call_a_instance_method()
        {
            Expression<Func<int, int>> callAddMethodExpr = x => Add(x);
            var addMethodCall = (MethodCallExpression)callAddMethodExpr.Body;
            Assert.That(addMethodCall.Method.Name, Is.EqualTo("Add"));
        }

        [Test]
        public void MethodCallExpression_to_call_a_static_method()
        {
            Expression<Func<int, int>> callAddMethodExpr = x => StaticAdd(x);
            var addMethodCall = (MethodCallExpression)callAddMethodExpr.Body;
            Assert.That(addMethodCall.Method.Name, Is.EqualTo("StaticAdd"));
        }

        [Test]
        public void Dynamic_MethodCallExpression_to_call_a_static_method()
        {
            MethodInfo methodInfo = GetType().GetMethod("StaticAdd", BindingFlags.Static | BindingFlags.Public);
            ParameterExpression parameterExpression = Expression.Parameter(typeof(int), "a");
            var addMethodCall = Expression.Call(methodInfo, parameterExpression);
            Assert.That(addMethodCall.Method.Name, Is.EqualTo("StaticAdd"));
        }

        [Test]
        public void Simple_func_timing_tests()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Func<int, int> add = Add;
            Assert.That(add(4), Is.EqualTo(4 + 4));

            stopWatch.Stop();
            Console.WriteLine("Totally took: {0}ms", stopWatch.ElapsedMilliseconds);
        }

        [Test]
        public void Simple_expression_timing_tests()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Expression<Func<int, int>> addExpr = x => x + 4;
            Func<int, int> addFromExpr = addExpr.Compile();
            Assert.That(addFromExpr(4), Is.EqualTo(4 + 4));

            stopWatch.Stop();
            Console.WriteLine("Totally took: {0}ms", stopWatch.ElapsedMilliseconds);
        }

        public int Add(int a)
        {
            return a + 4;
        }

        public static int StaticAdd(int a)
        {
            return a + 4;
        }
    }
}
