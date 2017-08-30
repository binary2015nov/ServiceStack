using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Web;

namespace ServiceStack.Auth
{
    public class BasicAuthProvider : CredentialsAuthProvider, IAuthWithRequest
    {
        public new static string Name = AuthProviderCatagery.BasicProvider;
        public new static string Realm = "/auth/" + AuthProviderCatagery.BasicProvider;

        public BasicAuthProvider()
        {
            this.Provider = Name;
            this.AuthRealm = Realm;
        }

        public BasicAuthProvider(IAppSettings appSettings)
            : base(appSettings, Realm, Name) { }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            var userName = request.UserName;
            var password = request.Password;

            if (userName.IsNullOrEmpty() || password.IsNullOrEmpty())
                throw HttpError.Unauthorized(ErrorMessages.InvalidBasicAuthCredentials);

            return Authenticate(authService, session, userName, password, request.Continue);
        }

        public void PreAuthenticate(IRequest req, IResponse res)
        {
            //API Keys are sent in Basic Auth Username and Password is Empty
            var userPass = req.GetBasicAuthUserAndPassword();
            if (!string.IsNullOrEmpty(userPass?.Value))
            {
                //Need to run SessionFeature filter since its not executed before this attribute (Priority -100)			
                SessionFeature.AddSessionIdToRequestFilter(req, res, null); //Required to get req.GetSessionId()

                using (var authService = HostContext.ResolveService<AuthenticateService>(req))
                {
                    var response = authService.Post(new Authenticate
                    {
                        provider = Name,
                        UserName = userPass.Value.Key,
                        Password = userPass.Value.Value
                    });
                }
            }
            //else
            //{
            //    res.AddHeader("WWW-Authenticate", $"Basic realm=\"{Realm}\"");
            //}
        }
    }
}