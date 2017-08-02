using System;
using System.Collections.Generic;
using System.Globalization;
using ServiceStack.FluentValidation;
using ServiceStack.Validation;

namespace ServiceStack.Auth
{
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
            return Post(request);
        }

        /// <summary>
        /// Create new Registration
        /// </summary>
        public object Post(Register request)
        {
            var validateRes = ValidateFn?.Invoke(this, Request.Verb, request);
            if (validateRes != null)
                return validateRes;

            bool registerNewUser;
            IUserAuth user;
            var session = this.GetSession();
            using (AuthRepository as IDisposable)
            {
                var existingUser = AuthRepository.GetUserAuth(session, null);
                registerNewUser = existingUser == null;
                if (HostContext.GetPlugin<ValidationFeature>() == null)
                {
                    RegistrationValidator?.ValidateAndThrow(request, registerNewUser ? ApplyTo.Post : ApplyTo.Put);
                }
                var newUserAuth = ConvertToUserAuth(request);

                user = registerNewUser
                    ? AuthRepository.CreateUserAuth(newUserAuth, request.Password)
                    : AuthRepository.UpdateUserAuth(existingUser, newUserAuth, request.Password);
            }
            RegisterResponse responseDto = new RegisterResponse {
                UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                ReferrerUrl = request.Continue
            };
            AuthenticateService authService;
            if (request.AutoLogin && (authService = base.ResolveService<AuthenticateService>()) != null)
            {
                object authResponse;
                using (authService)
                {
                    authResponse = authService.Post(
                        new Authenticate {
                            provider = CredentialsAuthProvider.Name,
                            UserName = request.UserName ?? request.Email,
                            Password = request.Password,
                            Continue = request.Continue
                        });                   
                }     
                var typedAuthResponse = authResponse as AuthenticateResponse;
                if (typedAuthResponse != null)
                {
                    responseDto = new RegisterResponse
                    {
                        SessionId = typedAuthResponse.SessionId,
                        UserName = typedAuthResponse.UserName,
                        ReferrerUrl = typedAuthResponse.ReferrerUrl,
                        UserId = user.Id.ToString(CultureInfo.InvariantCulture),
                    };
                }
                if (authResponse is Exception)
                    throw (Exception)authResponse;
            }
            if (registerNewUser)
            {
                if (!request.AutoLogin)
                    session.PopulateSession(user, new List<IAuthTokens>());

                session.OnRegistered(Request, session, this);
                AuthEvents?.OnRegistered(Request, session, this);
            }
            if (Request.ResponseContentType.MatchesContentType(MimeTypes.Html))
            {
                if (request.Continue.IsNullOrEmpty())
                    return responseDto;

                return new HttpResult(responseDto) { Location = request.Continue };
            }
            return responseDto;
        }

        public IUserAuth ConvertToUserAuth(Register request)
        {
            var customUserAuth = AuthRepository as ICustomUserAuth;
            var to = customUserAuth != null
                ? customUserAuth.CreateUserAuth()
                : new UserAuth();

            to.PopulateInstance(request);
            to.PrimaryEmail = request.Email;
            return to;
        }
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

    public class FullRegistrationValidator : RegistrationValidator
    {
        public FullRegistrationValidator() { RuleSet(ApplyTo.Post, () => RuleFor(x => x.DisplayName).NotEmpty()); }
    }
}