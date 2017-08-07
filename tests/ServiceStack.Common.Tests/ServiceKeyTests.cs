using System;
using NUnit.Framework;
using Funq;
using ServiceStack.Auth;

namespace ServiceStack.Common.Tests
{
    [TestFixture]
    public class ServiceKeyTests
    {
        [Test]
        public void Func_Container_Hash_provider_is_Equal()
        {
            var source = new ServiceKey { FactoryType = typeof(Func<Container, IHashProvider>) };
            var other = new ServiceKey { FactoryType = typeof(Func<Container, IHashProvider>) };
            Assert.IsTrue(source.Equals(other));
            Assert.That(source, Is.EqualTo(other));
        }
    }
}
