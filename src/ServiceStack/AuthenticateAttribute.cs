﻿using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Auth;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Indicates that the request dto, which is associated with this attribute,
    /// requires authentication.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AuthenticateAttribute : RequestFilterAsyncAttribute
    {
        /// <summary>
        /// Restrict authentication to a specific <see cref="IAuthProvider"/>.
        /// For example, if this attribute should only permit access
        /// if the user is authenticated with <see cref="BasicAuthProvider"/>,
        /// you should set this property to <see cref="BasicAuthProvider.Name"/>.
        /// </summary>
        public string Provider { get; set; }

        /// <summary>
        /// Redirect the client to a specific URL if authentication failed.
        /// If this property is null, simply `401 Unauthorized` is returned.
        /// </summary>
        public string HtmlRedirect { get; set; }

        public AuthenticateAttribute(ApplyTo applyTo)
            : base(applyTo)
        {
            this.Priority = (int)RequestFilterPriority.Authenticate;
        }

        public AuthenticateAttribute()
            : this(ApplyTo.All) { }

        public AuthenticateAttribute(string provider)
            : this(ApplyTo.All)
        {
            this.Provider = provider;
        }

        public AuthenticateAttribute(ApplyTo applyTo, string provider)
            : this(applyTo)
        {
            this.Provider = provider;
        }

        public override async Task ExecuteAsync(IRequest req, IResponse res, object requestDto)
        {
            if (HostContext.HasValidAuthSecret(req))
                return;

            if (AuthenticateService.AuthProviders.Length == 0)
                throw new InvalidOperationException(
                    "The AuthenticateService must be initialized by calling AuthenticateService.Init to use an authenticate attribute");

            var matchingOAuthConfigs = AuthenticateService.AuthProviders.Where(
                x => Provider.IsNullOrEmpty() || x.Provider == Provider);

            if (matchingOAuthConfigs.Count() == 0)
            {
                await res.WriteError(req, requestDto, $"No OAuth Configs found matching {this.Provider ?? "any"} provider");
                res.EndRequest();
                return;
            }

            req.PopulateFromRequestIfHasSessionId(requestDto);

            //Call before GetSession so Exceptions can bubble
            req.Items[Keywords.HasPreAuthenticated] = true;
            matchingOAuthConfigs.OfType<IAuthWithRequest>()
                .Each(x => x.PreAuthenticate(req, res));

            var session = req.GetSession();
            if (session == null || !matchingOAuthConfigs.Any(x => session.IsAuthorized(x.Provider)))
            {
                if (this.DoHtmlRedirectIfConfigured(req, res, true)) return;

                await AuthProvider.HandleFailedAuth(matchingOAuthConfigs.First(), session, req, res);
            }
        }

        protected bool DoHtmlRedirectIfConfigured(IRequest req, IResponse res, bool includeRedirectParam = false)
        {
            var htmlRedirect = this.HtmlRedirect ?? AuthenticateService.HtmlRedirect;
            if (htmlRedirect != null && req.ResponseContentType.MatchesContentType(MimeTypes.Html))
            {
                DoHtmlRedirect(htmlRedirect, req, res, includeRedirectParam);
                return true;
            }
            return false;
        }

        public static void DoHtmlRedirect(string redirectUrl, IRequest req, IResponse res, bool includeRedirectParam)
        {
            var url = req.ResolveAbsoluteUrl(redirectUrl);
            if (includeRedirectParam)
            {
                var absoluteRequestPath = req.ResolveAbsoluteUrl("~" + req.PathInfo + ToQueryString(req.QueryString));
                url = url.AddQueryParam(HostContext.ResolveLocalizedString(LocalizedStrings.Redirect), absoluteRequestPath);
            }

            res.RedirectToUrl(url);
        }

        private static string ToQueryString(INameValueCollection queryStringCollection)
        {
            if (queryStringCollection == null || queryStringCollection.Count == 0)
                return string.Empty;

            return "?" + queryStringCollection.ToFormUrlEncoded();
        }

        public bool Equals(AuthenticateAttribute other)
        {
            if (other == null)
                return false;

            return string.Equals(Provider, other.Provider) && string.Equals(HtmlRedirect, other.HtmlRedirect);
        }

        public override bool Equals(object other)
        {
            return Equals(other as AuthenticateAttribute);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = GetType().Name.GetHashCode();
                hashCode = (hashCode * 397) ^ (Provider?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (HtmlRedirect?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}
