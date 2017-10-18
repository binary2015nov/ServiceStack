namespace ServiceStack
{
    public static class Keywords
    {
        public const string Callback = "callback";
        public const string Format = "format";
        public const string AuthSecret = "authsecret";
        public const string RequestInfo = "requestinfo";
        public const string Debug = "debug";
        public const string Version = "version";
        public const string VersionAbbr = "v";
        public const string Ignore = "ignore";
        public const string IgnorePlaceHolder = "_";
        public const string Bare = "bare";
        public const string SoapMessage = "SoapMessage";
        public const string Route = "__route";
        public const string InvokeVerb = "__verb";
        public const string DbInfo = "__dbinfo";
        public const string CacheInfo = "__cacheinfo";
        public const string ApiKey = "__apikey";
        public const string ApiKeyParam = "apikey";
        public const string Session = "__session";
        public const string JsConfig = "jsconfig";
        public const string SessionId = "ss-id";
        public const string PermanentSessionId = "ss-pid";
        public const string SessionOptionsKey = "ss-opt";
        public const string TokenCookie = "ss-tok";
        public const string HasPreAuthenticated = "__haspreauth";
        public const string HasLogged = "_logged";
        public const string DidAuthenticate = "__didauth";
        public const string IRequest = "__irequest";
        public const string RequestDuration = "_requestDurationStopwatch";
        public const string Code = "code";
        public const string View = "View";
        public const string Template = "Template";
    }

    public static class LocalizedStrings
    {
        public const string Login = "login";
        public const string Auth = "auth";
        public const string Authenticate = "authenticate";
        public const string Redirect = "redirect";
        public const string AssignRoles = "assignroles";
        public const string UnassignRoles = "unassignroles";
        public const string NotModified = "Not Modified";
    }

    public static class ErrorMessages
    {
        //Auth Errors
        public const string UnknownAuthProviderFmt = "No configuration was added for OAuth provider '{0}'";
               
        public const string InvalidBasicAuthCredentials = "Invalid BasicAuth Credentials";
        public const string WindowsAuthFailed = "Windows Auth Failed";
        public const string NotAuthenticated = "Not Authenticated";
        public const string InvalidUsernameOrPassword = "Invalid UserName or Password";
        public const string UsernameOrEmailRequired = "UserName or Email is required";
        public const string UserAccountLocked = "This account has been locked";
        public const string IllegalUsername = "UserName contains invalid characters";
        public const string ShouldNotRegisterAuthSession = "AuthSession's are rehydrated from ICacheClient and should not be registered in IOC's when not in HostContext.TestMode";
        public const string ApiKeyRequiresSecureConnection = "Sending ApiKey over insecure connection forbidden when RequireSecureConnection=true";
        public const string JwtRequiresSecureConnection = "Sending JWT over insecure connection forbidden when RequireSecureConnection=true";
        public const string InvalidSignature = "Invalid Signature";
        public const string TokenInvalidated = "Token has been invalidated";
        public const string TokenExpired = "Token has expired";
        public const string TokenInvalid = "Token is invalid";
        public const string RefreshTokenInvalid = "RefreshToken is Invalid";

        public const string InvalidRole = "Invalid Role";
        public const string InvalidPermission = "Invalid Permission";

        //Register
        public const string UserNotExists = "User does not exist";
        public const string AuthRepositoryNotExists = "No IAuthRepository registered or failed to resolve. Check your IoC registrations.";
        public const string UsernameAlreadyExists = "Username already exists";
        public const string EmailAlreadyExists = "Email already exists";
        public const string RegisterUpdatesDisabled = "Updating User Info is not allowed";

        //AuthRepo
        public const string UserAlreadyExistsTemplate1 = "User '{0}' already exists";
        public const string EmailAlreadyExistsTemplate1 = "Email '{0}' already exists";


        //StaticFileHandler
        public const string FileNotExistsFmt = "Static File '{0}' not found";

        //Server Events
        public const string SubscriptionNotExistsFmt = "Subscription '{0}' does not exist";
        public const string SubscriptionForbiddenFmt = "Access to Subscription '{0}' is forbidden";

        //Validation
        public const string RequestAlreadyProcessedFmt = "Request '{0}' has already been processed";

        //Hosts
        public const string OnlyAllowedInAspNetHosts = "Only ASP.NET Requests accessible via Singletons are supported";
        public const string HostDoesNotSupportSingletonRequest = "This AppHost does not support accessing the current Request via a Singleton";

        //Invalid State
        public static string ConstructorNotFoundForType = "Constructor not found for Type '{0}'";
        public static string ServiceNotFoundForType = "Service not found for Type '{0}'";
        public static string CacheFeatureMustBeEnabled = "HttpCacheFeature Plugin must be registered to use {0}";
        
        //Request
        public static string ContentTypeNotSupported = "ContentType not supported '{0}'";
    }

    public static class HelpMessages
    {
        public const string NativeTypesDtoOptionsTip =
            "To override a DTO option, remove \"{0}\" prefix before updating";
    }

    public static class StrictModeCodes
    {
        public const string CyclicalUserSession = nameof(CyclicalUserSession);
        public const string ReturnsValueType = nameof(ReturnsValueType);
    }
}
