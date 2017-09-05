using System;
using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    public class AuthTestsBase
    {
        public const string RoleName1 = "Role1";
        public const string RoleName2 = "Role2";

        public const string Permission1 = "Permission1";
        public const string Permission2 = "Permission2";

        private JsonServiceClient serviceClient;
        public JsonServiceClient ServiceClient => serviceClient ?? (serviceClient = new JsonServiceClient(Constant.ServiceStackBaseHost));

        protected Register AdminRegister;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            AdminRegister = CreateAdminUser();
        }

        public Register CreateAdminUser()
        {
            var adminRegister = new Register
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
                ServiceClient.Send(adminRegister);
            }
            catch (WebServiceException ex)
            {
                ("Error while creating Admin User: " + ex.Message).Print();
            }
            return adminRegister;
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
            var client = new JsonServiceClient(Constant.ServiceStackBaseHost);
            client.Send(new Authenticate
            {
                UserName = userName,
                Password = password,
                RememberMe = true,
            });

            return client;
        }
    }
}