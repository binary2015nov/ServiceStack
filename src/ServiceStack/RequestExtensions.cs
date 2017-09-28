using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Web;

namespace ServiceStack
{
    public static class RequestExtensions
    {
        public static IAuthSession GetSession(this IRequest request, bool reload = false)
        {
            if (HostContext.TestMode)
            {
                var mockSession = request.TryResolve<IAuthSession>(); //testing
                if (mockSession != null)
                    return mockSession;
            }

            object oSession = null;
            if (!reload)
                request.Items.TryGetValue(Keywords.Session, out oSession);

            // Apply PreAuthenticate Filters from IAuthWithRequest AuthProviders
            if (oSession == null && !request.Items.ContainsKey(Keywords.HasPreAuthenticated))
            {
                request.Items[Keywords.HasPreAuthenticated] = true;
                foreach (var authProvider in AuthenticateService.AuthWithRequestProviders)
                {
                    try { authProvider.PreAuthenticate(request, request.Response); } catch { }                  
                    //throw new Exception("Error in GetSession() when ApplyPreAuthenticateFilters", ex);
                    /*treat errors as non-existing session*/                   
                }
                request.Items.TryGetValue(Keywords.Session, out oSession);
            }

            var sessionId = request.GetSessionId() ?? request.CreateSessionIds(request.Response);
            var session = oSession as IAuthSession;
            if (session != null)
                session = HostContext.AppHost.OnSessionFilter(session, sessionId);
            if (session != null)
                return session;

            var sessionKey = SessionFeature.GetSessionKey(sessionId);
            if (sessionKey != null)
            {
                session = request.GetCacheClient().Get<IAuthSession>(sessionKey);

                if (session != null)
                    session = HostContext.AppHost.OnSessionFilter(session, sessionId);
            }

            if (session == null)
            {
                var newSession = SessionFeature.CreateNewSession(request, sessionId);
                session = HostContext.AppHost.OnSessionFilter(newSession, sessionId) ?? newSession;
            }

            request.Items[Keywords.Session] = session;
            return session;
        }

        public static AuthUserSession ReloadSession(this IRequest request)
        {
            return request.GetSession() as AuthUserSession;
        }

        public static string GetCompressionType(this IRequest request)
        {
            if (request.RequestPreferences.AcceptsDeflate)
                return CompressionTypes.Deflate;

            if (request.RequestPreferences.AcceptsGzip)
                return CompressionTypes.GZip;

            return null;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetContentEncoding(this IRequest request)
        {
            return request.Headers.Get(HttpHeaders.ContentEncoding);
        }

        public static Stream GetInputStream(this IRequest req, Stream stream)
        {
            var enc = req.GetContentEncoding();
            if (enc == CompressionTypes.Deflate)
                return new DeflateStream(stream, CompressionMode.Decompress);
            if (enc == CompressionTypes.GZip)
                return new GZipStream(stream, CompressionMode.Decompress);

            return stream;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetHeader(this IRequest request, string headerName)
        {
            return request.Headers.Get(headerName);
        }

        public static string GetParamInRequestHeader(this IRequest request, string name)
        {
            //Avoid reading request body for non x-www-form-urlencoded requests
            return request.Headers[name]
                ?? request.QueryString[name]
                ?? (!HostContext.Config.SkipFormDataInCreatingRequest && request.ContentType.MatchesContentType(MimeTypes.FormUrlEncoded)
                        ? request.FormData[name]
                        : null);
        }

        /// <summary>
        /// Returns the optimized result for the IRequestContext. 
        /// Does not use or store results in any cache.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        public static object ToOptimizedResult(this IRequest request, object dto)
        {
            dto = dto.GetDto();
            request.Response.Dto = dto;

            var compressionType = request.GetCompressionType();
            if (compressionType == null)
                return HostContext.ContentTypes.SerializeToString(request, dto);

            using (var ms = new MemoryStream())
            using (var compressionStream = GetCompressionStream(ms, compressionType))
            {
                HostContext.ContentTypes.SerializeToStream(request, dto, compressionStream);
                compressionStream.Close();

                var compressedBytes = ms.ToArray();
                return new CompressedResult(compressedBytes, compressionType, request.ResponseContentType)
                {
                    Status = request.Response.StatusCode
                };
            }
        }

        private static Stream GetCompressionStream(Stream outputStream, string compressionType)
        {
            if (compressionType == CompressionTypes.Deflate)
                return StreamExt.DeflateProvider.DeflateStream(outputStream);
            if (compressionType == CompressionTypes.GZip)
                return StreamExt.GZipProvider.GZipStream(outputStream);

            throw new NotSupportedException(compressionType);
        }

        /// <summary>
        /// Overload for the <see cref="ContentCacheManager.Resolve"/> method returning the most
        /// optimized result based on the MimeType and CompressionType from the IRequestContext.
        /// </summary>
        public static object ToOptimizedResultUsingCache<T>(
            this IRequest requestContext, ICacheClient cacheClient, string cacheKey,
            Func<T> factoryFn)
        {
            return requestContext.ToOptimizedResultUsingCache(cacheClient, cacheKey, null, factoryFn);
        }

        /// <summary>
        /// Overload for the <see cref="ContentCacheManager.Resolve"/> method returning the most
        /// optimized result based on the MimeType and CompressionType from the IRequestContext.
        /// <param name="expireCacheIn">How long to cache for, null is no expiration</param>
        /// </summary>
        public static object ToOptimizedResultUsingCache<T>(
            this IRequest requestContext, ICacheClient cacheClient, string cacheKey,
            TimeSpan? expireCacheIn, Func<T> factoryFn)
        {
            var cacheResult = cacheClient.ResolveFromCache(cacheKey, requestContext);
            if (cacheResult != null)
                return cacheResult;

            cacheResult = cacheClient.Cache(cacheKey, factoryFn(), requestContext, expireCacheIn);
            return cacheResult;
        }

        /// <summary>
        /// Clears all the serialized and compressed caches set 
        /// by the 'Resolve' method for the cacheKey provided
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="cacheClient"></param>
        /// <param name="cacheKeys"></param>
        public static void RemoveFromCache(
            this IRequest requestContext, ICacheClient cacheClient, params string[] cacheKeys)
        {
            cacheClient.ClearCaches(cacheKeys);
        }

        /// <summary>
        /// Store an entry in the IHttpRequest.Items Dictionary
        /// </summary>
        public static void SetItem(this IRequest httpReq, string key, object value)
        {
            if (httpReq == null) return;

            httpReq.Items[key] = value;
        }

        /// <summary>
        /// Get an entry from the IHttpRequest.Items Dictionary
        /// </summary>
        public static object GetItem(this IRequest httpReq, string key)
        {
            if (httpReq == null) return null;

            object value;
            httpReq.Items.TryGetValue(key, out value);
            return value;
        }

#if !NETSTANDARD1_6
        public static RequestBaseWrapper ToHttpRequestBase(this IRequest request)
        {
            return new RequestBaseWrapper((IHttpRequest)request);
        }
#endif

        public static void SetInProcessRequest(this IRequest httpReq)
        {
            if (httpReq == null) return;

            httpReq.RequestAttributes |= RequestAttributes.InProcess;
        }

        public static bool IsInProcessRequest(this IRequest request)
        {
            return (RequestAttributes.InProcess & request?.RequestAttributes) == RequestAttributes.InProcess;
        }

        public static void ReleaseIfInProcessRequest(this IRequest httpReq)
        {
            if (httpReq == null) return;

            httpReq.RequestAttributes = httpReq.RequestAttributes & ~RequestAttributes.InProcess;
        }

        internal static T TryResolveInternal<T>(this IRequest request)
        {
            if (typeof(T) == typeof(IRequest))
                return (T)request;
            if (typeof(T) == typeof(IResponse))
                return (T)request.Response;

            var hasResolver = request as IHasResolver;
            return hasResolver != null 
                ? hasResolver.Resolver.TryResolve<T>() 
                : request.TryResolve<T>();
        }

        public static IVirtualFile GetFile(this IRequest request) => request is IHasVirtualFiles vfs ? vfs.GetFile() : null;
        public static IVirtualDirectory GetDirectory(this IRequest request) => request is IHasVirtualFiles vfs ? vfs.GetDirectory() : null;
        public static bool IsFile(this IRequest request) => request is IHasVirtualFiles vfs && vfs.IsFile;
        public static bool IsDirectory(this IRequest request) => request is IHasVirtualFiles vfs && vfs.IsDirectory;
    }
}