using System;
using System.Net;
using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    public class AuthTestsBase
    {
        public string RoleName1 = "Role1";
        public string RoleName2 = "Role2";
        public const string ContentManager = "ContentManager";
        public const string ContentPermission = "ContentPermission";

        public string Permission1 = "Permission1";
        public string Permission2 = "Permission2";

        protected Register AdminRegister;

        private JsonServiceClient serviceClient;
        public JsonServiceClient ServiceClient => serviceClient ?? (serviceClient = new JsonServiceClient(Constant.ServiceStackBaseUri));

        public Register CreateAdminUser()
        {
            AdminRegister = new Register
            {
                UserName = "Admin",
                DisplayName = "The Admin User",
                Email = Constant.AdminEmail, //this email is automatically assigned as Admin in Web.Config
                FirstName = "Admin",
                LastName = "User",
                Password = Constant.AdminPassword,
            };
            try
            {
                ServiceClient.Send(AdminRegister);
            }
            catch (WebServiceException ex)
            {
                ("Error while creating Admin User: " + ex.Message).Print();
            }
            return AdminRegister;
        }

        public Register RegisterNewUser(bool autoLogin = false)
        {
            var userId = Environment.TickCount % 10000;

            var registerDto = new Register
            {
                UserName = "UserName" + userId,
                DisplayName = "DisplayName" + userId,
                Email = "user{0}@sf.com".Fmt(userId),
                FirstName = "FirstName" + userId,
                LastName = "LastName" + userId,
                Password = "Password" + userId,
                AutoLogin = autoLogin
            };

            ServiceClient.Send(registerDto);

            return registerDto;
        }

        public JsonServiceClient Login(string userName, string password)
        {
            var client = new JsonServiceClient(Constant.ServiceStackBaseUri);
            client.Send(new Authenticate
            {
                UserName = userName,
                Password = password,
                RememberMe = true,
            });

            return client;
        }

        public JsonServiceClient AuthenticateWithAdminUser()
        {
            var serviceClient = new JsonServiceClient(Constant.ServiceStackBaseUri);
            serviceClient.Send(new Authenticate
            {
                UserName = AdminRegister.UserName,
                Password = AdminRegister.Password,
                RememberMe = true,
            });

            return serviceClient;
        }

        protected void AssertUnAuthorized(WebServiceException webEx)
        {
            Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            Assert.That(webEx.StatusDescription, Is.EqualTo(HttpStatusCode.Unauthorized.ToString()));
        }
    }
}