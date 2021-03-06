﻿using System;
using AutorestClient;
using NUnit.Framework;
using ServiceStack.OpenApi.Tests.Host;
using ServiceStack.OpenApi.Tests.Services;

namespace ServiceStack.OpenApi.Tests
{
    [TestFixture]
    class AnnotatedPropertiesTests : GeneratedClientTestBase
    {
        [Test]
        public void Can_get_annotated_service_with_array_enum()
        {
            var client = new ServiceStackAutorestClient(new Uri(Constants.AbsoluteBaseUri));

            var dto = new GetMovie { Id = 1, Includes = new[] {"Genres", "Releases" } };

            var result = client.GetMovieId.Post(dto.Id, dto.Includes);

            Assert.That(result.Includes, Is.EquivalentTo(dto.Includes));
        }
    }
}
