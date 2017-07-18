﻿using System;
using System.Collections.Generic;
using System.Globalization;
using ServiceStack.FluentValidation;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class FullRegistrationValidator : RegistrationValidator
    {
        public FullRegistrationValidator() { RuleSet(ApplyTo.Post, () => RuleFor(x => x.DisplayName).NotEmpty()); }
    }

    public class RegistrationValidator : AbstractValidator<Register>
    {
        public RegistrationValidator()
        {
            RuleSet(
                ApplyTo.Post,
                () =>
                {
                    RuleFor(x => x.Password).NotEmpty();
                    RuleFor(x => x.UserName).NotEmpty().When(x => x.Email.IsNullOrEmpty());
                    RuleFor(x => x.Email).NotEmpty().EmailAddress().When(x => x.UserName.IsNullOrEmpty());
                    RuleFor(x => x.UserName)
                        .Must(x =>
                        {
                            var authRepo = HostContext.AppHost.GetAuthRepository(base.Request);
                            using (authRepo as IDisposable)
                            {
                                return authRepo.GetUserAuthByUserName(x) == null;
                            }
                        })
                        .WithErrorCode("AlreadyExists")
                        .WithMessage(ErrorMessages.UsernameAlreadyExists)
                        .When(x => !x.UserName.IsNullOrEmpty());
                    RuleFor(x => x.Email)
                        .Must(x =>
                        {
                            var authRepo = HostContext.AppHost.GetAuthRepository(base.Request);
                            using (authRepo as IDisposable)
                            {
                                return x.IsNullOrEmpty() || authRepo.GetUserAuthByUserName(x) == null;
                            }
                        })
                        .WithErrorCode("AlreadyExists")
                        .WithMessage(ErrorMessages.EmailAlreadyExists)
                        .When(x => !x.Email.IsNullOrEmpty());
                });
            RuleSet(
                ApplyTo.Put,
                () =>
                {
                    RuleFor(x => x.UserName).NotEmpty();
                    RuleFor(x => x.Email).NotEmpty();
                });
        }
    }

    [Obsolete("Use normal RegistrationFeature and have your IAuthRepository implement ICustomUserAuth instead")]
    [DefaultRequest(typeof(Register))]
    public class RegisterService<TUserAuth> : RegisterService
        where TUserAuth : class, IUserAuth { }

    [DefaultRequest(typeof(Register))]
    public class RegisterService : Service
    {
        public static ValidateFn ValidateFn { get; set; }

        public IValidator<Register> RegistrationValidator { get; set; }

        public IAuthEvents AuthEvents { get; set; }

        /// <summary>
        /// Update an existing registraiton
        /// </summary>
        public object Put(Register request)
        {
            return UpdateUserAuth(request);
        }

        /// <summary>
        /// Create new Registration
        /// </summary>
        public object Post(Register request)
        {
            var validateResponse = ValidateFn?.Invoke(this, base.Request.Verb, request);
            if (validateResponse != null)
                return validateResponse;

            RegisterResponse response = null;
            var session = Request.GetSession();
            bool registerNewUser;
            IUserAuth user;

            var authRepo = base.AuthRepository;
            var newUserAuth = CreateUserAuth(request);
            using (authRepo as IDisposable)
            {
                var existingUser = authRepo.GetUserAuth(session, null);
                registerNewUser = existingUser == null;

                if (HostContext.GlobalRequestFilters == null
                    || !HostContext.GlobalRequestFilters.Contains(ValidationFilters.RequestFilter)) //Already gets run
                {
                    RegistrationValidator?.ValidateAndThrow(request, registerNewUser ? ApplyTo.Post : ApplyTo.Put);
                }

                user = registerNewUser
                    ? authRepo.CreateUserAuth(newUserAuth, request.Password)
                    : authRepo.UpdateUserAuth(existingUser, newUserAuth, request.Password);
            }

            if (request.AutoLogin.GetValueOrDefault())
            {
                using (var authService = base.ResolveService<AuthenticateService>())
                {
                    var authResponse = authService.Post(
                        new Authenticate {
                            provider = CredentialsAuthProvider.Name,
                            UserName = request.UserName ?? request.Email,
                            Password = request.Password,
                            Continue = request.Continue
                        });

                    if (authResponse is IHttpError)
                        throw (Exception)authResponse;

                    var typedResponse = authResponse as AuthenticateResponse;
                    if (typedResponse != null)
                    {
                        response = new RegisterResponse
                        {
                            SessionId = typedResponse.SessionId,
                            UserName = typedResponse.UserName,
                            ReferrerUrl = typedResponse.ReferrerUrl,
                            UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                        };
                    }
                }
            }

            if (registerNewUser)
            {
                if (!request.AutoLogin.GetValueOrDefault())
                    session.PopulateSession(user, new List<IAuthTokens>());

                session.OnRegistered(Request, session, this);
                AuthEvents?.OnRegistered(base.Request, session, this);
            }

            if (response == null)
            {
                response = new RegisterResponse
                {
                    UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                    ReferrerUrl = request.Continue
                };
            }

            var isHtml = Request.ResponseContentType.MatchesContentType(MimeTypes.Html);
            if (isHtml)
            {
                if (string.IsNullOrEmpty(request.Continue))
                    return response;

                return new HttpResult(response)
                {
                    Location = request.Continue
                };
            }

            return response;
        }

        public IUserAuth CreateUserAuth(Register request)
        {
            var customUserAuth = AuthRepository as ICustomUserAuth;
            var to = customUserAuth != null
                ? customUserAuth.CreateUserAuth()
                : new UserAuth();

            to.PopulateInstance(request);
            to.PrimaryEmail = request.Email;
            return to;
        }

        /// <summary>
        /// Logic to update UserAuth from Registration info, not enabled on PUT because of security.
        /// </summary>
        public RegisterResponse UpdateUserAuth(Register request)
        {
            var response = ValidateFn?.Invoke(this, base.Request.Verb, request);
            if (response != null)
                return (RegisterResponse)response;

            if (HostContext.GlobalRequestFilters == null
                || !HostContext.GlobalRequestFilters.Contains(ValidationFilters.RequestFilter))
            {
                RegistrationValidator.ValidateAndThrow(request, ApplyTo.Put);
            }

            var session = Request.GetSession();

            var authRepo = base.AuthRepository;
            using (authRepo as IDisposable)
            {
                var existingUser = authRepo.GetUserAuth(session, null);
                if (existingUser == null)
                    throw HttpError.NotFound(ErrorMessages.UserNotExists);

                var newUserAuth = CreateUserAuth(request);
                authRepo.UpdateUserAuth(existingUser, newUserAuth, request.Password);

                return new RegisterResponse
                {
                    UserId = existingUser.Id.ToString(CultureInfo.InvariantCulture),
                };
            }
        }
    }
}