using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class BasicAuthProvider : CredentialsAuthProvider, IAuthWithRequest
    {
        public new static string Name = AuthProviderCatageries.BasicProvider;
        public new static string Realm = "/auth/" + AuthProviderCatageries.BasicProvider;

        public BasicAuthProvider()
        {
            this.Provider = Name;
            this.AuthRealm = Realm;
        }

        public BasicAuthProvider(IAppSettings appSettings) : base(appSettings, Realm, Name) { }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var userName = request.UserName;
            var password = request.Password;

            if (userName.IsNullOrEmpty() || password.IsNullOrEmpty())
                throw HttpError.Unauthorized(ErrorMessages.InvalidBasicAuthCredentials.loca);

            return Authenticate(authService, session, userName, password, request.Continue);
        }

        public virtual void PreAuthenticate(IRequest req, IResponse res)
        {
            //API Keys are sent in Basic Auth Username and Password is Empty
            var userPass = req.GetBasicAuthUserAndPassword();
            if (!string.IsNullOrEmpty(userPass?.Value))
            {
                using (var authService = HostContext.ResolveService<AuthenticateService>(req))
                {
                    var response = authService.Post(new Authenticate
                    {
                        Provider = this.Provider,
                        UserName = userPass.Value.Key,
                        Password = userPass.Value.Value
                    });
                }
            }
        }

        public override void OnFailedAuthentication(IAuthSession session, IRequest httpReq, IResponse httpRes)
        {
            httpRes.AddHeader(HttpHeaders.WwwAuthenticate, $"{Provider} realm=\"{Realm}\"");
            base.OnFailedAuthentication(session, httpReq, httpRes);
        }
    }
}