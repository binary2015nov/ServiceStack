using System;
using System.Linq;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    [DefaultRequest(typeof(Authenticate))]
    public class AuthenticateService : Service
    {  
        public static ValidateFn ValidateFn { get; set; }

        public static string DefaultOAuthProvider { get; private set; }
        public static string DefaultOAuthRealm { get; private set; }
        public static string HtmlRedirect { get; internal set; }
        public static Func<AuthFilterContext, object> AuthResponseDecorator { get; internal set; }
        public static IAuthProvider[] AuthProviders { get; private set; } = TypeConstants<IAuthProvider>.EmptyArray;
        internal static IAuthWithRequest[] AuthWithRequestProviders = TypeConstants<IAuthWithRequest>.EmptyArray;
        internal static IAuthResponseFilter[] AuthResponseFilters = TypeConstants<IAuthResponseFilter>.EmptyArray;

        public static IUserSessionSource GetUserSessionSource()
        {
            var userSessionSource = HostContext.TryResolve<IUserSessionSource>();
            if (userSessionSource != null)
                return userSessionSource;

            if (AuthProviders != null)
            {
                foreach (var authProvider in AuthProviders)
                {
                    if (authProvider is IUserSessionSource sessionSource) //don't remove
                        return sessionSource;
                }
            }

            return null;
        }

        /// <summary>
        /// Get specific AuthProvider
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static IAuthProvider GetAuthProvider(string provider)
        {
            if (provider.IsNullOrEmpty())
                throw new ArgumentException(nameof(provider));

            if (AuthProviders.Length == 0)
                return null;
            if (provider == AuthProviderCatageries.LogoutAction)
                return AuthProviders[0];

            foreach (var authProvider in AuthProviders)
            {
                if (string.Compare(authProvider.Provider, provider, StringComparison.OrdinalIgnoreCase) == 0)
                    return authProvider;
            }

            return null;
        }

        public static JwtAuthProviderReader GetJwtAuthProvider() => GetAuthProvider(JwtAuthProviderReader.Name) as JwtAuthProviderReader;

        public static JwtAuthProviderReader GetRequiredJwtAuthProvider()
        {
            var jwtProvider = GetJwtAuthProvider();
            if (jwtProvider == null)
                throw new NotSupportedException("JwtAuthProvider is required but was not registered in AuthFeature's AuthProviders");

            return jwtProvider;
        }

        public static void Init(params IAuthProvider[] authProviders)
        {
            if (authProviders.Length == 0)
                throw new ArgumentNullException(nameof(authProviders));

            DefaultOAuthProvider = authProviders[0].Provider;
            DefaultOAuthRealm = authProviders[0].AuthRealm;

            AuthProviders = authProviders;
            AuthWithRequestProviders = authProviders.OfType<IAuthWithRequest>().ToArray();
            AuthResponseFilters = authProviders.OfType<IAuthResponseFilter>().ToArray();
        }

        public void Options(Authenticate request) { }

        public object Get(Authenticate request)
        {
            return Post(request);
        }

        public object Post(Authenticate request)
        {
            var validateRes = ValidateFn?.Invoke(this, Request.Verb, request);
            if (validateRes != null)
                return validateRes;

            if (AuthProviders == null || AuthProviders.Length == 0)
                throw new Exception("No auth providers have been registered in your app host.");

            var provider = request.Provider ?? AuthProviders[0].Provider;
            if (provider == AuthProviderCatageries.CredentialsAliasProvider)
                provider = AuthProviderCatageries.CredentialsProvider;

            var authProvider = GetAuthProvider(provider);
            if (authProvider == null)
                throw HttpError.NotFound(ErrorMessages.UnknownAuthProviderFmt.Fmt(provider.SafeInput()));

            if (request.RememberMe)
                Request.AddSessionOptions(SessionOptions.Permanent);

            if (AuthProviderCatageries.LogoutAction.EqualsIgnoreCase(request.Provider))
                return authProvider.Logout(this, request);

            if (authProvider is IAuthWithRequest && !base.Request.IsInProcessRequest())
            {
                //IAuthWithRequest normally doesn't call Authenticate directly, but they can to return Auth Info
                //But as AuthenticateService doesn't have [Authenticate] we need to call it manually
                new AuthenticateAttribute().ExecuteAsync(base.Request, base.Response, request).Wait();
                if (base.Response.IsClosed)
                    return null;
            }

            var session = this.GetSession();

            var isHtml = Request.ResponseContentType.MatchesContentType(MimeTypes.Html);
            try
            {
                var response = Authenticate(request, provider, session, authProvider);

                // The above Authenticate call may end an existing session and create a new one so we need
                // to refresh the current session reference.
                session = this.GetSession();

                if (request.Provider == null && !session.IsAuthenticated)
                    throw HttpError.Unauthorized(ErrorMessages.NotAuthenticated.Localize(Request));

                var referrerUrl = request.Continue
                    ?? session.ReferrerUrl
                    ?? Request.GetHeader(HttpHeaders.Referer)
                    ?? authProvider.CallbackUrl;

                var alreadyAuthenticated = response == null;
                response = response ?? new AuthenticateResponse {
                    UserId = session.UserAuthId,
                    UserName = session.UserAuthName,
                    DisplayName = session.DisplayName 
                        ?? session.UserName 
                        ?? $"{session.FirstName} {session.LastName}".Trim(),
                    SessionId = session.Id,
                    ReferrerUrl = referrerUrl,
                };

                if (response is AuthenticateResponse authResponse)
                {
                    var authCtx = new AuthFilterContext {
                        AuthService = this,
                        AuthProvider = authProvider,
                        AuthRequest = request,
                        AuthResponse = authResponse,
                        Session = session,
                        AlreadyAuthenticated = alreadyAuthenticated,
                        DidAuthenticate = Request.Items.ContainsKey(Keywords.DidAuthenticate),
                    };

                    foreach (var responseFilter in AuthResponseFilters)
                    {
                        responseFilter.Execute(authCtx);
                    }

                    if (AuthResponseDecorator != null)
                    {
                        return AuthResponseDecorator(authCtx);
                    }
                }

                if (isHtml && request.Provider != null)
                {
                    if (alreadyAuthenticated)
                        return this.Redirect(referrerUrl.SetParam("s", "0"));

                    if (!(response is IHttpResult) && !string.IsNullOrEmpty(referrerUrl))
                    {
                        return new HttpResult(response) {
                            Location = referrerUrl
                        };
                    }
                }

                return response;
            }
            catch (HttpError ex)
            {
                var errorReferrerUrl = Request.GetHeader(HttpHeaders.Referer);
                if (isHtml && errorReferrerUrl != null)
                {
                    errorReferrerUrl = errorReferrerUrl.SetParam("f", ex.Message.Localize(Request));
                    return HttpResult.Redirect(errorReferrerUrl);
                }

                throw;
            }
        }

        /// <summary>
        /// Public API entry point to authenticate via code
        /// </summary>
        /// <param name="request"></param>
        /// <returns>null; if already autenticated otherwise a populated instance of AuthResponse</returns>
        public AuthenticateResponse Authenticate(Authenticate request)
        {
            //Remove HTML Content-Type to avoid auth providers issuing browser re-directs
            var hold = Request.ResponseContentType;
            try
            {
                Request.ResponseContentType = MimeTypes.PlainText;

                if (request.RememberMe)
                    Request.AddSessionOptions(SessionOptions.Permanent);
                
                var provider = request.Provider ?? AuthProviders[0].Provider;
                var authProvider = GetAuthProvider(provider);
                if (authProvider == null)
                    throw HttpError.NotFound(ErrorMessages.UnknownAuthProviderFmt.Fmt(provider.SafeInput()));

                if (request.Provider == AuthProviderCatageries.LogoutAction)
                    return authProvider.Logout(this, request) as AuthenticateResponse;

                var result = Authenticate(request, provider, this.GetSession(), authProvider);
                if (result is HttpError httpError)
                    throw httpError;

                return result as AuthenticateResponse;
            }
            finally
            {
                Request.ResponseContentType = hold;
            }
        }

        /// <summary>
        /// The specified <paramref name="session"/> may change as a side-effect of this method. If
        /// subsequent code relies on current <see cref="IAuthSession"/> data be sure to reload
        /// the session istance via <see cref="ServiceExtensions.GetSession(IServiceBase,bool)"/>.
        /// </summary>
        private object Authenticate(Authenticate request, string provider, IAuthSession session, IAuthProvider authProvider)
        {
            if (request.Provider == null && request.UserName == null)
                return null; //Just return sessionInfo if no provider or username is given

            var authFeature = HostContext.GetPlugin<AuthFeature>();
            var generateNewCookies = authFeature == null || authFeature.GenerateNewSessionCookiesOnAuthentication;

            if (generateNewCookies)
                this.Request.GenerateNewSessionCookies(session);

            var response = authProvider.Authenticate(this, session, request);

            return response;
        }

        public object Delete(Authenticate request)
        {
            var validateRes = ValidateFn?.Invoke(this, HttpMethods.Delete, request);
            if (validateRes != null)
                return validateRes;

            this.RemoveSession();

            return new AuthenticateResponse();
        }
    }
}

