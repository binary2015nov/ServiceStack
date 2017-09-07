using System;
using NUnit.Framework;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    public abstract class AuthenticationTestsBase
    {
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
                Email = Constant.AdminEmail,
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