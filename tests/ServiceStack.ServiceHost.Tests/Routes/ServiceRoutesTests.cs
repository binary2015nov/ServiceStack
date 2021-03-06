﻿using System.Linq;
using NUnit.Framework;
using ServiceStack.Host;

namespace ServiceStack.ServiceHost.Tests.Routes
{
    [TestFixture]
    public class ServiceRoutesTests
    {
        [Test]
        public void Can_Register_NewApi_Routes_From_Assembly()
        {
            var routes = new ServiceRoutes();
            routes.AddFromAssembly(typeof(NewApiRestServiceWithAllVerbsImplemented).Assembly);

            RestPath restWithAllMethodsRoute =
                (from r in routes
                 where r.Path == "/NewApiRequestDto"
                 select r).FirstOrDefault();

            Assert.That(restWithAllMethodsRoute, Is.Not.Null);

            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("GET"));
            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("POST"));
            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("PUT"));
            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("DELETE"));
            Assert.That(restWithAllMethodsRoute.AllowedVerbs.Contains("PATCH"));

            RestPath restWithAllMethodsRoute2 =
                (from r in routes
                 where r.Path == "/NewApiRequestDto2"
                 select r).FirstOrDefault();

            Assert.That(restWithAllMethodsRoute2, Is.Not.Null);

            Assert.That(restWithAllMethodsRoute2.AllowedVerbs.Contains("GET"));
            Assert.That(restWithAllMethodsRoute2.AllowedVerbs.Contains("POST"));
            Assert.That(restWithAllMethodsRoute2.AllowedVerbs.Contains("PUT"));
            Assert.That(restWithAllMethodsRoute2.AllowedVerbs.Contains("DELETE"));
            Assert.That(restWithAllMethodsRoute2.AllowedVerbs.Contains("PATCH"));
        }

        [Test]
        public void Can_Register_NewApi_Routes_With_Id_and_Any_Fallback_From_Assembly()
        {
            var routes = new ServiceRoutes();
            routes.AddFromAssembly(typeof(NewApiRequestDtoWithIdService).Assembly);

            var route = (from r in routes
                         where r.Path == "/NewApiRequestDtoWithId"
                         select r).FirstOrDefault();

            Assert.That(route, Is.Not.Null);
            Assert.That(route.AllowedVerbs, Is.Null);

            route = (from r in routes
                     where r.Path == "/NewApiRequestDtoWithId/{Id}"
                     select r).FirstOrDefault();

            Assert.That(route, Is.Not.Null);
            Assert.That(route.AllowedVerbs, Is.Null);
        }

        [Test]
        public void Can_Register_NewApi_Routes_With_Field_Id_and_Any_Fallback_From_Assembly()
        {
            var routes = new ServiceRoutes();
            routes.AddFromAssembly(typeof(NewApiRequestDtoWithFieldIdService).Assembly);

            var route = (from r in routes
                         where r.Path == "/NewApiRequestDtoWithFieldId"
                         select r).FirstOrDefault();

            Assert.That(route, Is.Not.Null);
            Assert.That(route.AllowedVerbs, Is.Null);

            route = (from r in routes
                     where r.Path == "/NewApiRequestDtoWithFieldId/{Id}"
                     select r).FirstOrDefault();

            Assert.That(route, Is.Not.Null);
            Assert.That(route.AllowedVerbs, Is.Null);
        }

        [Test]
        public void Can_Register_Routes_Using_Add_Extension()
        {
            var routes = new ServiceRoutes();
            routes.Add<Customer>("/Users/{0}/Orders/{1}", ApplyTo.Get, x => x.Name, x => x.OrderId);
            var route = routes.Last();
            Assert.That(route.Path, Is.EqualTo("/Users/{Name}/Orders/{OrderId}"));
        }
    }

    public class Customer
    {
        public string Name { get; set; }
        public int OrderId { get; set; }
    }
}