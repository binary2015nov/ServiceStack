﻿using NUnit.Framework;
using ServiceStack.Testing;

namespace ServiceStack.ServiceHost.Tests
{
    [TestFixture]
    public class BasicAppHostTests
    {
        [Test]
        public void Can_dispose_without_init()
        {
            MockAppHost appHost = new MockAppHost();
            appHost.Dispose();
        }
    }
}
