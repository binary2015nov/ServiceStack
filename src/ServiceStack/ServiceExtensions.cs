using System;
using System.Linq;
using System.Net;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Redis;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class ServiceExtensions
    {
        public static IHttpResult Redirect(this IServiceBase service, string url)
        {
            return service.Redirect(url, "Moved Temporarily");
        }

        public static IHttpResult Redirect(this IServiceBase service, string url, string message)
        {
            return new HttpResult(HttpStatusCode.Redirect, message)
            {
                ContentType = service.Request.ResponseContentType,
                Headers = {
                    { HttpHeaders.Location, url }
                },
            };
        }

        public static IHttpResult AuthenticationRequired(this IServiceBase service)
        {
            return new HttpResult
            {
                StatusCode = HttpStatusCode.Unauthorized,
                ContentType = service.Request.ResponseContentType,
                Headers = {
                    { HttpHeaders.WwwAuthenticate, $"{AuthenticateService.DefaultOAuthProvider} realm=\"{AuthenticateService.DefaultOAuthRealm}\"" }
                },
            };
        }

        public static string GetSessionId(this IServiceBase service)
        {
            var req = service.Request;
            var sessionId = req.GetSessionId();
            if (sessionId == null)
                throw new ArgumentNullException("sessionId", "Session not set. Is Session being set in RequestFilters?");

            return sessionId;
        }

        public static ICacheClient GetCacheClient(this IResolver service)
        {
            var cache = service.TryResolve<ICacheClient>();
            if (cache != null)
                return cache;

            var redisManager = service.TryResolve<IRedisClientsManager>();
            if (redisManager != null)
                return redisManager.GetCacheClient();

            // If they don't have an ICacheClient configured use an In Memory one.
            return MemoryCacheClient.Default;
        }

        public static void SaveSession(this IServiceBase service, IAuthSession session, TimeSpan? expiresIn = null)
        {
            if (service == null || session == null) return;

            service.Request.SaveSession(session, expiresIn);
        }

        public static void RemoveSession(this IServiceBase service)
        {
            service?.Request.RemoveSession();
        }

        public static void RemoveSession(this Service service)
        {
            service?.Request.RemoveSession();
        }

        public static void CacheSet<T>(this ICacheClient cache, string key, T value, TimeSpan? expiresIn)
        {
            if (expiresIn.HasValue)
                cache.Set(key, value, expiresIn.Value);
            else
                cache.Set(key, value);
        }

        public static void SaveSession(this IRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null)
        {
            HostContext.AppHost.OnSaveSession(httpReq, session, expiresIn);
        }

        public static void RemoveSession(this IRequest httpReq)
        {
            RemoveSession(httpReq, httpReq.GetSessionId());
        }

        public static void RemoveSession(this IRequest httpReq, string sessionId)
        {
            if (httpReq == null) return;
            if (sessionId == null)
                throw new ArgumentNullException(nameof(sessionId));

            var sessionKey = SessionFeature.GetSessionKey(sessionId);
            httpReq.GetCacheClient().Remove(sessionKey);

            httpReq.Items.Remove(Keywords.Session);
        }

        public static IAuthSession GetSession(this IServiceBase service, bool reload = false)
        {
            return service.Request?.GetSession(reload);
        }

        public static TUserSession SessionAs<TUserSession>(this IRequest req)
        {
            if (HostContext.AppHost.TestMode)
            {
                var mockSession = req.TryResolve<TUserSession>();
                if (!Equals(mockSession, default(TUserSession)))
                    mockSession = req.TryResolve<IAuthSession>() is TUserSession
                        ? (TUserSession)req.TryResolve<IAuthSession>()
                        : default(TUserSession);

                if (!Equals(mockSession, default(TUserSession)))
                    return mockSession;
            }

            return SessionFeature.GetOrCreateSession<TUserSession>(req.GetCacheClient(), req, req.Response);
        }

        public static bool IsAuthenticated(this IRequest req)
        {
            if (HostContext.HasValidAuthSecret(req))
                return true;     
            
            var session = req.GetSession();
            return session != null && AuthenticateService.AuthProviders.Any(x => session.IsAuthorized(x.Provider));
        }

        public static TimeSpan? GetSessionTimeToLive(this ICacheClient cache, string sessionId)
        {
            var sessionKey = SessionFeature.GetSessionKey(sessionId);
            return cache.GetTimeToLive(sessionKey);
        }

        public static TimeSpan? GetSessionTimeToLive(this IRequest httpReq)
        {
            return httpReq.GetCacheClient().GetSessionTimeToLive(httpReq.GetSessionId());
        }

        public static object RunAction<TService, TRequest>(
            this TService service, TRequest request, Func<TService, TRequest, object> invokeAction,
            IRequest requestContext = null)
            where TService : IService
        {
            var actionCtx = new ActionContext
            {
                RequestFilters = TypeConstants<IRequestFilterBase>.EmptyArray,
                ResponseFilters = TypeConstants<IResponseFilterBase>.EmptyArray,
                ServiceType = typeof(TService),
                RequestType = typeof(TRequest),
                ServiceAction = (instance, req) => invokeAction(service, request)
            };

            requestContext = requestContext ?? new MockHttpRequest();
            ServiceController.InjectRequestContext(service, requestContext);
            var runner = HostContext.CreateServiceRunner<TRequest>(actionCtx);
            var responseTask = runner.ExecuteAsync(requestContext, service, request);
            var response = responseTask.Result;
            return response;
        }
    }
}
