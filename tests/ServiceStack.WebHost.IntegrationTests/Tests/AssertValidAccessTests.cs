using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
    [TestFixture]
    public class AssertValidAccessTests : AuthenticationTestsBase
    {
        private const string RoleName1 = "Role1";
        private const string RoleName2 = "Role2";

        private const string Permission1 = "Permission1";
        private const string Permission2 = "Permission2";

        [Test]
        public void Authentication_does_return_session_cookies()
        {
            var newUser = RegisterNewUser(autoLogin: false);

            var jsonServiceClient = Login(newUser.UserName, newUser.Password);
            Assert.That(jsonServiceClient.CookieContainer.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Cannot_assign_roles_and_permissions_when_unthenticate()
        {
            var newUser = RegisterNewUser(autoLogin: false);

            try
            {
                var client = new JsonServiceClient(Constants.ServiceStackBaseHost);
                client.Send(
                    new AssignRoles
                    {
                        UserName = newUser.UserName,
                        Roles = { RoleName1, RoleName2 },
                        Permissions = { Permission1, Permission2 }
                    });

                Assert.Fail("Should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Cannot_assign_roles_and_permissions_with_normal_user()
        {
            var newUser = RegisterNewUser(autoLogin: true);

            try
            {
                ServiceClient.Send(
                    new AssignRoles
                    {
                        UserName = newUser.UserName,
                        Roles = { RoleName1, RoleName2 },
                        Permissions = { Permission1, Permission2 }
                    });

                Assert.Fail("Should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Assert.That(webEx.StatusDescription, Is.EqualTo(ErrorMessages.InvalidRole));
            }
        }

        [Test]
        public void Can_Assign_Roles_and_Permissions_to_new_User()
        {
            var newUser = RegisterNewUser();

            var client = Login(AdminRegister.UserName, AdminRegister.Password);

            var response = client.Send(
                new AssignRoles
                {
                    UserName = newUser.UserName,
                    Roles = { RoleName1, RoleName2 },
                    Permissions = { Permission1, Permission2 }
                });

            Console.WriteLine("Assigned Roles: " + response.Dump());

            Assert.That(response.AllRoles, Is.EquivalentTo(new[] { RoleName1, RoleName2 }));
            Assert.That(response.AllPermissions, Is.EquivalentTo(new[] { Permission1, Permission2 }));
        }

        [Test]
        public void Cannot_Unassign_Roles_and_Permissions_when_unthenticate()
        {
            var newUser = RegisterNewUser(autoLogin: false);

            try
            {
                var client = new JsonServiceClient(Constants.ServiceStackBaseHost);
                client.Send(
                    new AssignRoles
                    {
                        UserName = newUser.UserName,
                        Roles = { RoleName1, RoleName2 },
                        Permissions = { Permission1, Permission2 }
                    });

                Assert.Fail("Should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public void Can_UnAssign_Roles_and_Permissions_to_new_User()
        {
            var newUser = RegisterNewUser();

            var client = Login(AdminRegister.UserName, AdminRegister.Password);

            client.Send(
            new AssignRoles
            {
                UserName = newUser.UserName,
                Roles = new List<string> { RoleName1, RoleName2 },
                Permissions = new List<string> { Permission1, Permission2 }
            });

            var response = client.Send(
            new UnAssignRoles
            {
                UserName = newUser.UserName,
                Roles = { RoleName1 },
                Permissions = { Permission2 },
            });

            Console.WriteLine("Remaining Roles: " + response.Dump());

            Assert.That(response.AllRoles, Is.EquivalentTo(new[] { RoleName2 }));
            Assert.That(response.AllPermissions, Is.EquivalentTo(new[] { Permission1 }));
        }

        [Test]
        public void Can_only_access_ContentManagerOnlyService_service_after_Assigned_Role()
        {
            var newUser = RegisterNewUser(autoLogin: true);

            try
            {
                ServiceClient.Send(new ContentManagerOnly());
                Assert.Fail("Should not be allowed - no roles");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Assert.That(webEx.StatusDescription, Is.EqualTo(ErrorMessages.InvalidRole));
            }

            var client = Login(AdminRegister.UserName, AdminRegister.Password);

            client.Send(
                new AssignRoles
                {
                    UserName = newUser.UserName,
                    Roles = new List<string> { RoleName1 },
                });

            var newUserClient = Login(newUser.UserName, newUser.Password);

            try
            {
                newUserClient.Send(new ContentManagerOnly());
                Assert.Fail("Should not be allowed - wrong roles");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Assert.That(webEx.StatusDescription, Is.EqualTo(ErrorMessages.InvalidRole));
            }

            var assignResponse = client.Send(
                new AssignRoles
                {
                    UserName = newUser.UserName,
                    Roles = new List<string> { Constants.ContentManager },
                });

            Assert.That(assignResponse.AllRoles, Is.EquivalentTo(new[] { RoleName1, Constants.ContentManager }));

            var response = newUserClient.Send(new ContentManagerOnly());

            Assert.That(response.Result, Is.EqualTo("Haz Access"));
        }

        [Test]
        public void Can_only_access_ContentPermissionOnlyService_after_Assigned_Permission()
        {
            var newUser = RegisterNewUser(autoLogin: true);

            try
            {
                ServiceClient.Send(new ContentPermissionOnly());
                Assert.Fail("Should not be allowed - no permissions");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Assert.That(webEx.StatusDescription, Is.EqualTo(ErrorMessages.InvalidPermission));
            }

            var client = Login(AdminRegister.UserName, AdminRegister.Password);

            client.Send(
                new AssignRoles
                {
                    UserName = newUser.UserName,
                    Permissions = new List<string> { RoleName1 },
                });

            var newUserClient = Login(newUser.UserName, newUser.Password);

            try
            {
                newUserClient.Send(new ContentPermissionOnly());
                Assert.Fail("Should not be allowed - wrong permissions");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
                Assert.That(webEx.StatusDescription, Is.EqualTo(ErrorMessages.InvalidPermission));
            }

            var assignResponse = client.Send(
                new AssignRoles
                {
                    UserName = newUser.UserName,
                    Permissions = new List<string> { Constants.ContentPermission },
                });

            Assert.That(assignResponse.AllPermissions, Is.EquivalentTo(new[] { RoleName1, Constants.ContentPermission }));

            var response = newUserClient.Send(new ContentPermissionOnly());

            Assert.That(response.Result, Is.EqualTo("Haz Access"));
        }

        [Test]
        public void Cannot_access_Admin_service_by_default()
        {
            try
            {
                var jsonServiceClient = new JsonServiceClient(Constants.ServiceStackBaseHost);
                jsonServiceClient.Send<RequiresRoleInService>(new RequiresRoleInService());

                Assert.Fail("Should not allow access to protected resource");
            }
            catch (Exception ex)
            {
                if (ex.IsUnauthorized()|| ex.IsAny400()) //redirect to login
                    return;

                throw;
            }
        }

        [Test]
        public void Can_access_authenticate_service_with_AuthSecret()
        {            
           var webRes = Constants.ServiceStackBaseHost.AppendPath("requiresadmin")
                .AddQueryParam("authsecret", Constants.AuthSecret).GetWebResponse();
            Assert.That(webRes.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
    }
}