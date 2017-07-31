using System;
using System.Configuration;
using System.Linq;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    /// <summary>
    /// Inject logic into existing services by introspecting the request and injecting your own
    /// validation logic. Exceptions thrown will have the same behaviour as if the service threw it.
    /// 
    /// If a non-null object is returned the request will short-circuit and return that response.
    /// </summary>
    /// <param name="service">The instance of the service</param>
    /// <param name="httpMethod">GET,POST,PUT,DELETE</param>
    /// <param name="requestDto"></param>
    /// <returns>Response DTO; non-null will short-circuit execution and return that response</returns>
    public delegate object ValidateFn(IServiceBase service, string httpMethod, object requestDto);

    [DefaultRequest(typeof(Authenticate))]
    public class AuthenticateService : Service
    {  
        public static ValidateFn ValidateFn { get; set; }

        public static string DefaultOAuthProvider { get; private set; }
        public static string DefaultOAuthRealm { get; private set; }
        public static string HtmlRedirect { get; internal set; }
        public static Func<AuthFilterContext, object> AuthResponseDecorator { get; internal set; }
        public static IAuthProvider[] AuthProviders { get; private set; }
        internal static IAuthWithRequest[] AuthWithRequestProviders = TypeConstants<IAuthWithRequest>.EmptyArray;
        internal static IAuthResponseFilter[] AuthResponseFilters = TypeConstants<IAuthResponseFilter>.EmptyArray;

        public static void Init(params IAuthProvider[] authProviders)
        {
            var oauthProvider = authProviders.FirstOrDefault(p => p is IOAuthProvider);
            if (oauthProvider != null)
            {
                DefaultOAuthProvider = oauthProvider.Provider;
                DefaultOAuthRealm = oauthProvider.AuthRealm;
            }

            AuthProviders = authProviders;
            AuthWithRequestProviders = authProviders.OfType<IAuthWithRequest>().ToArray();
            AuthResponseFilters = authProviders.OfType<IAuthResponseFilter>().ToArray();
        }

        public static IAuthProvider GetAuthProvider(string provider)
        {
            if (AuthProviders.Length == 0)
                return null;
            if (provider == AuthProviderCatagery.LogoutAction)
                return AuthProviders[0];

            foreach (var authProvider in AuthProviders)
            {
                if (string.Compare(authProvider.Provider, provider, StringComparison.OrdinalIgnoreCase) == 0)
                    return authProvider;
            }

            return null;
        }

        public void Options(Authenticate request) { }

        public object Get(Authenticate request)
        {
            return Post(request);
        }

        public object Post(Authenticate request)
        {
            var validationRes = ValidateFn?.Invoke(this, Request.Verb, request);
            if (validationRes != null)
                return validationRes;

            if (AuthProviders == null || AuthProviders.Length == 0)
                throw new Exception("No auth providers have been registered in your app host.");

            if (request.RememberMe)         
                Request.AddSessionOptions(SessionOptions.Permanent);           

            var provider = request.provider ?? AuthProviders[0].Provider;
            if (provider == AuthProviderCatagery.CredentialsAliasProvider)
                provider = AuthProviderCatagery.CredentialsProvider;

            var authProvider = GetAuthProvider(provider);
            if (authProvider == null)
                throw HttpError.NotFound(ErrorMessages.UnknownAuthProviderFmt.Fmt(provider.SafeInput()));

            if (AuthProviderCatagery.LogoutAction.EqualsIgnoreCase(request.provider))
                return authProvider.Logout(this, request);

            var authWithRequest = authProvider as IAuthWithRequest;
            if (authWithRequest != null && !Request.IsInProcessRequest())
            {
                //IAuthWithRequest normally doesn't call Authenticate directly, but they can to return Auth Info
                //But as AuthenticateService doesn't have [Authenticate] we need to call it manually
                new AuthenticateAttribute().Execute(Request, Response, request);
                if (Response.IsClosed)
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

                if (request.provider == null && !session.IsAuthenticated)
                    throw HttpError.Unauthorized(ErrorMessages.NotAuthenticated);

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

                var authResponse = response as AuthenticateResponse;
                if (authResponse != null)
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

                if (isHtml && request.provider != null)
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
                
                var provider = request.provider ?? AuthProviders[0].Provider;
                var authProvider = GetAuthProvider(provider);
                if (authProvider == null)
                    throw HttpError.NotFound(ErrorMessages.UnknownAuthProviderFmt.Fmt(provider.SafeInput()));

                if (request.provider == AuthProviderCatagery.LogoutAction)
                    return authProvider.Logout(this, request) as AuthenticateResponse;

                var result = Authenticate(request, provider, this.GetSession(), authProvider);
                var httpError = result as HttpError;
                if (httpError != null)
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
            if (request.provider == null && request.UserName == null)
                return null; //Just return sessionInfo if no provider or username is given

            var authFeature = HostContext.GetPlugin<AuthFeature>();
            var generateNewCookies = authFeature == null || authFeature.GenerateNewSessionCookiesOnAuthentication;

            object response = null;

            var doAuth = !(authFeature?.SkipAuthenticationIfAlreadyAuthenticated == true)
                || !authProvider.IsAuthorized(session, session.GetAuthTokens(provider), request);

            if (doAuth)
            {
                if (generateNewCookies)
                    Request.GenerateNewSessionCookies(session);

                response = authProvider.Authenticate(this, session, request);
            }
            else
            {
                if (generateNewCookies)
                {
                    Request.GenerateNewSessionCookies(session);
                    authProvider.SaveSession(this, session, (authProvider as AuthProvider)?.SessionExpiry);
                }
            }
            return response;
        }

        public object Delete(Authenticate request)
        {
            var response = ValidateFn?.Invoke(this, HttpMethods.Delete, request);
            if (response != null)
                return response;

            this.RemoveSession();

            return new AuthenticateResponse();
        }
    }
}

