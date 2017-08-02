using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// can only execute, if the user has specific roles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class RequiredRoleAttribute : AuthenticateAttribute
    {
        public string[] RequiredRoles { get; set; }

        public RequiredRoleAttribute(ApplyTo applyTo, params string[] roles)
        {
            this.RequiredRoles = roles;
            this.ApplyTo = applyTo;
            this.Priority = (int)RequestFilterPriority.RequiredRole;
        }

        public RequiredRoleAttribute(params string[] roles)
            : this(ApplyTo.All, roles)
        { }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            base.Execute(req, res, requestDto); //first check if session is authenticated
            if (res.IsClosed)
                return; //AuthenticateAttribute already closed the request (ie auth failed)
            try
            {
                var authRepo = HostContext.AppHost.GetAuthRepository(req);
                using (authRepo as IDisposable)
                {
                    AssertRequiredRoles(req, authRepo, RequiredRoles);
                }
            }
            catch
            {
                if (DoHtmlRedirectIfConfigured(req, res))
                    return;
                throw;
            }
        }

        public bool HasAllRoles(IRequest req, IAuthRepository authRepo)
        {
            try
            {
                AssertRequiredRoles(req, authRepo, RequiredRoles);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check all session is in all supplied roles otherwise a 401 HttpError is thrown
        /// </summary>
        public static void AssertRequiredRoles(IRequest req, IAuthRepository authRepo, params string[] requiredRoles)
        {
            if (requiredRoles.IsEmpty() || HostContext.HasValidAuthSecret(req))
                return;

            var session = req.GetSession();
            if (session != null)
            {
                if (session.HasRole(RoleNames.Admin, authRepo))
                    return;
                if (requiredRoles.All(x => session.HasRole(x, authRepo)))
                    return;

                session.UpdateFromUserAuthRepo(req);
            }

            if (session != null && requiredRoles.All(x => session.HasRole(x, authRepo)))
                return;

            var statusCode = session != null && session.IsAuthenticated
                ? (int)HttpStatusCode.Forbidden
                : (int)HttpStatusCode.Unauthorized;
            throw new HttpError(statusCode, ErrorMessages.InvalidRole);
        }

        public bool Equals(RequiredRoleAttribute other)
        {
            if (other == null)
                return false;

            return base.Equals(other) && Equals(RequiredRoles, other.RequiredRoles);
        }

        public override bool Equals(object other)
        {
            return Equals(other as RequiredRoleAttribute);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (RequiredRoles?.GetHashCode() ?? 0);
            }
        }
    }
}
