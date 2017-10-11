﻿using System;
using System.Net;
using System.Web;
using ServiceStack.Text;
using ServiceStack.Web;
#if NETSTANDARD2_0
using Microsoft.AspNetCore.Http;
#endif

namespace ServiceStack.Host
{
    public class Cookies : ICookies
    {
        public const string RootPath = "/";

        readonly IHttpResponse httpRes;

        public Cookies(IHttpResponse httpRes)
        {
            this.httpRes = httpRes;
        }

        /// <summary>
        /// Sets a persistent cookie which never expires
        /// </summary>
        public void AddPermanentCookie(string cookieName, string cookieValue, bool? secureOnly = null)
        {
            var cookie = new Cookie(cookieName, cookieValue, RootPath)
            {
                Expires = DateTime.UtcNow.AddYears(20)
            };
            if (secureOnly != null)
            {
                cookie.Secure = secureOnly.Value;
            }
            httpRes.SetCookie(cookie);
        }

        /// <summary>
        /// Sets a session cookie which expires after the browser session closes
        /// </summary>
        public void AddSessionCookie(string cookieName, string cookieValue, bool? secureOnly = null)
        {
            var cookie = new Cookie(cookieName, cookieValue, RootPath);
            if (secureOnly != null)
            {
                cookie.Secure = secureOnly.Value;
            }
            httpRes.SetCookie(cookie);
        }

        /// <summary>
        /// Deletes a specified cookie by setting its value to empty and expiration to -1 days
        /// </summary>
        public void DeleteCookie(string cookieName)
        {
            var cookie = new Cookie(cookieName, string.Empty, RootPath)
            {
                Expires = DateTime.UtcNow.AddDays(-1)
            };
            httpRes.SetCookie(cookie);
        }
    }

    public static class CookiesExtensions
    {
        private static readonly DateTime Session = DateTime.MinValue;

#if !NETSTANDARD2_0
        public static HttpCookie ToHttpCookie(this Cookie cookie)
        {
            var httpCookie = new HttpCookie(cookie.Name, cookie.Value)
            {
                Path = cookie.Path,
                Expires = cookie.Expires,
                HttpOnly = !HostContext.Config.AllowNonHttpOnlyCookies || cookie.HttpOnly,
                Secure = cookie.Secure
            };
            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                httpCookie.Domain = cookie.Domain;
            }
            else if (HostContext.Config.RestrictAllCookiesToDomain != null)
            {
                httpCookie.Domain = HostContext.Config.RestrictAllCookiesToDomain;
            }
            return httpCookie;
        }
#endif

#if NETSTANDARD2_0
        public static CookieOptions ToCookieOptions(this Cookie cookie)
        {
            var cookieOptions = new CookieOptions
            {
                Path = cookie.Path,
                Expires = cookie.Expires == DateTime.MinValue ? (DateTimeOffset?)null : cookie.Expires,
                HttpOnly = !HostContext.Config.AllowNonHttpOnlyCookies || cookie.HttpOnly,
                Secure = cookie.Secure
            };

            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                cookieOptions.Domain = cookie.Domain;
            }
            else if (HostContext.Config.RestrictAllCookiesToDomain != null)
            {
                cookieOptions.Domain = HostContext.Config.RestrictAllCookiesToDomain;
            }
            return cookieOptions;
        }
#endif

        public static string AsHeaderValue(this Cookie cookie)
        {
            var path = cookie.Expires == Session
                ? "/"
                : cookie.Path ?? "/";

            var sb = StringBuilderCache.Allocate();

            sb.Append($"{cookie.Name}={cookie.Value};path={path}");

            if (cookie.Expires != Session)
            {
                sb.Append($";expires={cookie.Expires:R}");
            }

            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                sb.Append($";domain={cookie.Domain}");
            }
            else if (HostContext.Config.RestrictAllCookiesToDomain != null)
            {
                sb.Append($";domain={HostContext.Config.RestrictAllCookiesToDomain}");
            }

            if (cookie.Secure)
            {
                sb.Append(";Secure");
            }
            if (!HostContext.Config.AllowNonHttpOnlyCookies || cookie.HttpOnly)
            {
                sb.Append(";HttpOnly");
            }

            return StringBuilderCache.Retrieve(sb);
        }
    }
}
