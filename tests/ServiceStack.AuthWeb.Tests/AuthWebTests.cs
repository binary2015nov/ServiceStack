using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using ServiceStack.Text;

namespace ServiceStack.AuthWeb.Tests
{
    [TestFixture]
    public class AuthWebTests
    {
        public const string BaseUri = "http://localhost:11001/";

        [Test]
        public void Can_authenticate_with_HTTP_Basic_Authentication()
        {
            var client = new JsonServiceClient(BaseUri);
            client.UserName = "mythz";
            client.Password = "test";
            var response = client.Get(new RequiresAuth { Name = "Haz Access!" });
            Assert.That(response.Name, Is.EqualTo("Haz Access!"));
        }

        [Test]
        public void Can_authenticate_with_ASPNET_Windows_Authentication()
        {
            var client = new JsonServiceClient(BaseUri)
            {
                //Credentials = CredentialCache.DefaultCredentials,
                Credentials = new NetworkCredential("mythz", "test", "macbook")
            };

            var response = client.Get(new RequiresAuth { Name = "Haz Access!" });

            Assert.That(response.Name, Is.EqualTo("Haz Access!"));
        }

        [Test]
        public void Can_Authenticate_with_Metadata()
        {
            var client = new JsonServiceClient(BaseUri);
            try
            {
                var response = client.Send(new Authenticate
                {
                    UserName = "demis.bellot@gmail.com",
                    Password = "test",
                    //Meta = new Dictionary<string, string> { { "custom", "metadata" } }
                });
            }
            catch (Exception ex)
            {
                ex.PrintDump();
                throw;
            }
        }
    }
}