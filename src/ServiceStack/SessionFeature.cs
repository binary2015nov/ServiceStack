using System;
using System.Web;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.MiniProfiler.Helpers;
using ServiceStack.Web;

namespace ServiceStack
{
    public class SessionFeature : IPlugin
    {
        public static TimeSpan DefaultSessionExpiry = TimeSpan.FromDays(7 * 2); //2 weeks
        public static TimeSpan DefaultPermanentSessionExpiry = TimeSpan.FromDays(7 * 4); //4 weeks

        private static Func<IAuthSession> sessionFn;
        public static Func<IAuthSession> DefaultSessionFactory
        {
            get { return sessionFn ?? (() => new AuthUserSession()); }
            set { sessionFn = value; }
        }

        [Obsolete("Removing rarely used feature, if needed override OnSessionFilter() and return null if invalid session")]
        public static bool VerifyCachedSessionId = false;

        public TimeSpan? SessionExpiry { get; set; }
        public TimeSpan? SessionBagExpiry { get; set; }
        public TimeSpan? PermanentSessionExpiry { get; set; }

        public void Register(IAppHost appHost)
        {
            //Add permanent and session cookies if not already set.
            appHost.GlobalRequestFilters.Add(AddSessionIdToRequestFilter);
        }

        public static void AddSessionIdToRequestFilter(IRequest req, IResponse res, object requestDto)
        {
            if (req.PopulateFromRequestIfHasSessionId(requestDto))
                return;

            if (req.GetTemporarySessionId().IsNullOrEmpty())
            {
                req.CreateTemporarySessionId(res);
            }
            if (req.GetPermanentSessionId().IsNullOrEmpty())
            {
                req.CreatePermanentSessionId(res);
            }
        }

        public static string CreateSessionIds(IRequest httpReq = null, IResponse httpRes = null)
        {
            if (httpReq == null)
                httpReq = HostContext.GetCurrentRequest();
            if (httpRes == null)
                httpRes = httpReq.Response;

            return httpReq.CreateSessionIds(httpRes);
        }

        public static string GetSessionKey(IRequest request)
        {
            var sessionId = request.GetSessionId();
            return GetSessionKey(sessionId);
        }

        public static string GetSessionKey(string sessionId)
        {
            return sessionId == null ? null : IdUtils.CreateUrn<IAuthSession>(sessionId);
        }

        public static T GetOrCreateSession<T>(ICacheClient cache = null, IRequest httpReq = null, IResponse httpRes = null)
        {
            if (httpReq == null)
                httpReq = HostContext.GetCurrentRequest();

            var iSession = httpReq.GetSession();
            if (iSession is T)
                return (T)iSession;

            var sessionId = httpReq.GetSessionId();
            var sessionKey = GetSessionKey(sessionId);
            if (sessionKey != null)
            {
                var session = (cache ?? httpReq.GetCacheClient()).Get<T>(sessionKey);
                if (!Equals(session, default(T)))
                    return (T)HostContext.AppHost.OnSessionFilter((IAuthSession)session, sessionId);
            }

            return (T)CreateNewSession(httpReq, sessionId);
        }

        public static IAuthSession CreateNewSession(IRequest request, string sessionId)
        {
            var session = DefaultSessionFactory();
            session.Id = sessionId ?? CreateSessionIds(request);
            session.OnCreated(request);

            var authEvents = HostContext.TryResolve<IAuthEvents>();
            authEvents?.OnCreated(request, session);

            return session;
        }
    }
}