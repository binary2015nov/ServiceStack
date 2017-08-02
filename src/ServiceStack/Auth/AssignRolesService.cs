using System;
using System.Linq;
using ServiceStack.Configuration;

namespace ServiceStack.Auth
{
    [RequiredRole(RoleNames.Admin)]
    [DefaultRequest(typeof(AssignRoles))]
    public class AssignRolesService : Service
    {
        public object Post(AssignRoles request)
        {
            request.UserName.ThrowIfNullOrEmpty();

            var authRepo = AuthRepository;
            using (authRepo as IDisposable)
            {
                var userAuth = authRepo.GetUserAuthByUserName(request.UserName);
                if (userAuth == null)
                    throw HttpError.NotFound(request.UserName);

                authRepo.AssignRoles(userAuth, request.Roles, request.Permissions);

                return new AssignRolesResponse
                {
                    AllRoles = authRepo.GetRoles(userAuth).ToList(),
                    AllPermissions = authRepo.GetPermissions(userAuth).ToList(),
                };
            }
        }
    }
}