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
            var source = new ServiceKey(typeof(Func<Container, IHashProvider>), string.Empty);
            var other = new ServiceKey(typeof(Func<Container, IHashProvider>), string.Empty);
            Assert.IsTrue(source.Equals(other));
            Assert.That(source, Is.EqualTo(other));
        }
    }
}
