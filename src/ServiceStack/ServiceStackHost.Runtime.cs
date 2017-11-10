﻿// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.IO;
using ServiceStack.Messaging;
using ServiceStack.Metadata;
using ServiceStack.MiniProfiler;
using ServiceStack.Redis;
using ServiceStack.Serialization;
using ServiceStack.Support.WebHost;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract partial class ServiceStackHost
    {
        public virtual async Task<object> ApplyRequestConvertersAsync(IRequest req, object requestDto)
        {
            foreach (var converter in RequestConverters)
            {
                requestDto = await converter(req, requestDto) ?? requestDto;
                if (req.Response.IsClosed)
                    return requestDto;
            }

            return requestDto;
        }

        public virtual async Task<object> ApplyResponseConvertersAsync(IRequest req, object responseDto)
        {
            foreach (var converter in ResponseConverters)
            {
                responseDto = await converter(req, responseDto) ?? responseDto;
                if (req.Response.IsClosed)
                    return responseDto;
            }

            return responseDto;
        }

        /// <summary>
        /// Apply PreRequest Filters for participating Custom Handlers, e.g. RazorFormat, MarkdownFormat, etc
        /// </summary>
        public virtual bool ApplyCustomHandlerRequestFilters(IRequest httpReq, IResponse httpRes)
        {
            return ApplyPreRequestFilters(httpReq, httpRes);
        }

        /// <summary>
        /// Applies the raw request filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public virtual bool ApplyPreRequestFilters(IRequest httpReq, IResponse httpRes)
        {
            if (PreRequestFilters.Count == 0)
                return false;

            using (Profiler.Current.Step("Executing Pre RequestFilters"))
            {
                foreach (var requestFilter in PreRequestFilters)
                {
                    requestFilter(httpReq, httpRes);
                    if (httpRes.IsClosed) break;
                }

                return httpRes.IsClosed;
            }
        }

        [Obsolete("Use ApplyRequestFiltersAsync")]
        public virtual bool ApplyRequestFilters(IRequest req, IResponse res, object requestDto)
        {
            ApplyRequestFiltersAsync(req, res, requestDto).Wait();
            return res.IsClosed;
        }

        /// <summary>
        /// Applies the request filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public virtual async Task ApplyRequestFiltersAsync(IRequest req, IResponse res, object requestDto)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (res == null) throw new ArgumentNullException(nameof(res));

            if (res.IsClosed)
                return;

            using (Profiler.Current.Step("Executing Request Filters Async"))
            {
                if (!req.IsMultiRequest())
                {
                    await ApplyRequestFiltersSingleAsync(req, res, requestDto);
                    return;
                }

                var dtos = (IEnumerable)requestDto;
                foreach (var dto in dtos)
                {
                    await ApplyRequestFiltersSingleAsync(req, res, dto);
                    if (res.IsClosed)
                        return;
                }
            }
        }

        protected virtual async Task ApplyRequestFiltersSingleAsync(IRequest req, IResponse res, object requestDto)
        {
            //Exec all RequestFilter attributes with Priority < 0
            var attributes = FilterAttributeCache.GetRequestFilterAttributes(requestDto.GetType());
            var i = 0;
            for (; i < attributes.Length && attributes[i].Priority < 0; i++)
            {
                var attribute = attributes[i];
                Container.AutoWire(attribute);
                if (attribute is IHasRequestFilter filterSync)
                    filterSync.RequestFilter(req, res, requestDto);
                else if (attribute is IHasRequestFilterAsync filterAsync)
                    await filterAsync.RequestFilterAsync(req, res, requestDto);

                Release(attribute);
                if (res.IsClosed) 
                    return;
            }

            ExecTypedFilters(GlobalTypedRequestFilters, req, res, requestDto);
            if (res.IsClosed) 
                return;

            //Exec global filters
            foreach (var requestFilter in GlobalRequestFilters)
            {
                requestFilter(req, res, requestDto);
                if (res.IsClosed) 
                    return;
            }
            
            foreach (var requestFilter in GlobalRequestFiltersAsync)
            {
                await requestFilter(req, res, requestDto);
                if (res.IsClosed) 
                    return;
            }

            //Exec remaining RequestFilter attributes with Priority >= 0
            for (; i < attributes.Length && attributes[i].Priority >= 0; i++)
            {
                var attribute = attributes[i];
                Container.AutoWire(attribute);
                
                if (attribute is IHasRequestFilter filterSync)
                    filterSync.RequestFilter(req, res, requestDto);
                else if (attribute is IHasRequestFilterAsync filterAsync)
                    await filterAsync.RequestFilterAsync(req, res, requestDto);

                Release(attribute);
                if (res.IsClosed) 
                    return;
            }
        }

        [Obsolete("Use ApplyResponseFiltersAsync")]
        public virtual bool ApplyResponseFilters(IRequest req, IResponse res, object response)
        {
            ApplyResponseFiltersAsync(req, res, response).Wait();
            return res.IsClosed;
        }
        
        /// <summary>
        /// Applies the response filters. Returns whether or not the request has been handled 
        /// and no more processing should be done.
        /// </summary>
        /// <returns></returns>
        public virtual async Task ApplyResponseFiltersAsync(IRequest req, IResponse res, object response)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (res == null) throw new ArgumentNullException(nameof(res));

            if (res.IsClosed)
                return;

            using (Profiler.Current.Step("Executing Request Filters Async"))
            {
                var batchResponse = req.IsMultiRequest() ? response as IEnumerable : null;
                if (batchResponse == null)
                {
                    await ApplyResponseFiltersSingleAsync(req, res, response);
                    return;
                }

                foreach (var dto in batchResponse)
                {
                    await ApplyResponseFiltersSingleAsync(req, res, dto);
                    if (res.IsClosed)
                        return;
                }
            }
        }

        protected virtual async Task ApplyResponseFiltersSingleAsync(IRequest req, IResponse res, object response)
        {
            var attributes = req.Dto != null
                ? FilterAttributeCache.GetResponseFilterAttributes(req.Dto.GetType())
                : null;

            //Exec all ResponseFilter attributes with Priority < 0
            var i = 0;
            if (attributes != null)
            {
                for (; i < attributes.Length && attributes[i].Priority < 0; i++)
                {
                    var attribute = attributes[i];
                    Container.AutoWire(attribute);
                    
                    if (attribute is IHasResponseFilter filterSync)
                        filterSync.ResponseFilter(req, res, response);
                    else if (attribute is IHasResponseFilterAsync filterAsync)
                        await filterAsync.ResponseFilterAsync(req, res, response);

                    Release(attribute);
                    if (res.IsClosed) 
                        return;
                }
            }

            if (response != null)
            {
                ExecTypedFilters(GlobalTypedResponseFilters, req, res, response);
                if (res.IsClosed) 
                    return;
            }

            //Exec global filters
            foreach (var responseFilter in GlobalResponseFilters)
            {
                responseFilter(req, res, response);
                if (res.IsClosed) 
                    return;
            }

            foreach (var responseFilter in GlobalResponseFiltersAsync)
            {
                await responseFilter(req, res, response);
                if (res.IsClosed) 
                    return;
            }

            //Exec remaining RequestFilter attributes with Priority >= 0
            if (attributes != null)
            {
                for (; i < attributes.Length; i++)
                {
                    var attribute = attributes[i];
                    Container.AutoWire(attribute);
                    
                    if (attribute is IHasResponseFilter filterSync)
                        filterSync.ResponseFilter(req, res, response);
                    else if (attribute is IHasResponseFilterAsync filterAsync)
                        await filterAsync.ResponseFilterAsync(req, res, response);

                    Release(attribute);
                    if (res.IsClosed) 
                        return;
                }
            }
        }

        public virtual bool ApplyMessageRequestFilters(IRequest req, IResponse res, object requestDto)
        {
            ExecTypedFilters(GlobalTypedMessageRequestFilters, req, res, requestDto);
            if (res.IsClosed) return res.IsClosed;

            //Exec global filters
            foreach (var requestFilter in GlobalMessageRequestFilters)
            {
                requestFilter(req, res, requestDto);
                if (res.IsClosed) return res.IsClosed;
            }

            foreach (var requestFilter in GlobalMessageRequestFiltersAsync)
            {
                requestFilter(req, res, requestDto).Wait();
                if (res.IsClosed) return res.IsClosed;
            }

            return res.IsClosed;
        }

        public virtual bool ApplyMessageResponseFilters(IRequest req, IResponse res, object response)
        {
            ExecTypedFilters(GlobalTypedMessageResponseFilters, req, res, response);
            if (res.IsClosed) return res.IsClosed;

            //Exec global filters
            foreach (var responseFilter in GlobalMessageResponseFilters)
            {
                responseFilter(req, res, response);
                if (res.IsClosed) return res.IsClosed;
            }

            return res.IsClosed;
        }

        public virtual void ExecTypedFilters(Dictionary<Type, ITypedFilter> typedFilters,
            IRequest req, IResponse res, object dto)
        {
            if (typedFilters.Count == 0) return;

            var dtoType = dto.GetType();
            typedFilters.TryGetValue(dtoType, out var typedFilter);
            if (typedFilter != null)
            {
                typedFilter.Invoke(req, res, dto);
                if (res.IsClosed) return;
            }

            var dtoInterfaces = dtoType.GetInterfaces();
            foreach (var dtoInterface in dtoInterfaces)
            {
                typedFilters.TryGetValue(dtoInterface, out typedFilter);
                if (typedFilter != null)
                {
                    typedFilter.Invoke(req, res, dto);
                    if (res.IsClosed) return;
                }
            }
        }

        public MetadataPagesConfig MetadataPagesConfig => new MetadataPagesConfig(Metadata, ContentTypes.ContentTypeFormats.Keys);

        public virtual TimeSpan GetDefaultSessionExpiry(IRequest req)
        {
            var sessionFeature = this.GetPlugin<SessionFeature>();
            if (sessionFeature != null)
            {
                return req.IsPermanentSession()
                    ? sessionFeature.PermanentSessionExpiry ?? SessionFeature.DefaultPermanentSessionExpiry
                    : sessionFeature.SessionExpiry ?? SessionFeature.DefaultSessionExpiry;
            }

            return req.IsPermanentSession()
                ? SessionFeature.DefaultPermanentSessionExpiry
                : SessionFeature.DefaultSessionExpiry;
        }

        public bool HasFeature(Feature feature)
        {
            return (feature & Config.EnableFeatures) == feature;
        }

        public void AssertFeatures(Feature usesFeatures)
        {
            if (Config.EnableFeatures == Feature.All) return;

            if (!HasFeature(usesFeatures))
            {
                throw new UnauthorizedAccessException(
                    $"'{usesFeatures}' EnabledFeatures have been disabled by your administrator");
            }
        }

        public void AssertContentType(string contentType)
        {
            if (Config.EnableFeatures == Feature.All) return;

            AssertFeatures(contentType.ToFeature());
        }

        public bool HasAccessToMetadata(IRequest httpReq, IResponse httpRes)
        {
            if (!HasFeature(Feature.Metadata))
            {
                HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Available");
                return false;
            }

            if (Config.MetadataVisibility != RequestAttributes.Any)
            {
                var actualAttributes = httpReq.GetAttributes();
                if ((actualAttributes & Config.MetadataVisibility) != Config.MetadataVisibility)
                {
                    HandleErrorResponse(httpReq, httpRes, HttpStatusCode.Forbidden, "Metadata Not Visible");
                    return false;
                }
            }
            return true;
        }

        public void HandleErrorResponse(IRequest httpReq, IResponse httpRes, HttpStatusCode errorStatus, string errorStatusDescription = null)
        {
            if (httpRes.IsClosed) return;

            httpRes.StatusDescription = errorStatusDescription;

            var handler = GetCustomErrorHandler(errorStatus)
                ?? GlobalHtmlErrorHttpHandler
                ?? GetNotFoundHandler();

            handler.ProcessRequestAsync(httpReq, httpRes, httpReq.OperationName);
        }

        public IServiceStackHandler GetCustomErrorHandler(int errorStatusCode)
        {
            try
            {
                return GetCustomErrorHandler((HttpStatusCode)errorStatusCode);
            }
            catch
            {
                return null;
            }
        }

        public IServiceStackHandler GetCustomErrorHandler(HttpStatusCode errorStatus)
        {
            IServiceStackHandler httpHandler = null;
            CustomErrorHttpHandlers?.TryGetValue(errorStatus, out httpHandler);

            return httpHandler;
        }

        public IServiceStackHandler GetNotFoundHandler()
        {
            IServiceStackHandler httpHandler = null;
            CustomErrorHttpHandlers?.TryGetValue(HttpStatusCode.NotFound, out httpHandler);

            return httpHandler ?? new NotFoundHttpHandler();
        }

        public IHttpHandler GetCustomErrorHttpHandler(HttpStatusCode errorStatus)
        {
            var ssHandler = GetCustomErrorHandler(errorStatus)
                ?? GetNotFoundHandler();
            if (ssHandler == null) return null;
            var httpHandler = ssHandler as IHttpHandler;
            return httpHandler ?? new ServiceStackHttpHandler(ssHandler);
        }

        public bool HasValidAuthSecret(IRequest httpReq)
        {
            if (Config.AdminAuthSecret == null)
                return false;

            return httpReq.GetParam(Keywords.AuthSecret) == Config.AdminAuthSecret;
        }

        public virtual Exception ResolveResponseException(Exception ex)
        {
            return Config?.ReturnsInnerException == true && ex.InnerException != null && !(ex is IHttpError)
                ? ex.InnerException
                : ex;
        }

        public virtual void OnExceptionTypeFilter(Exception ex, ResponseStatus responseStatus)
        {
            var argEx = ex as ArgumentException;
            var isValidationSummaryEx = argEx is ValidationException;
            if (argEx != null && !isValidationSummaryEx && argEx.ParamName != null)
            {
                var paramMsgIndex = argEx.Message.LastIndexOf("Parameter name:", StringComparison.Ordinal);
                var errorMsg = paramMsgIndex > 0
                    ? argEx.Message.Substring(0, paramMsgIndex)
                    : argEx.Message;

                if (responseStatus.Errors == null)
                    responseStatus.Errors = new List<ResponseError>();

                responseStatus.Errors.Add(new ResponseError
                {
                    ErrorCode = ex.GetType().Name,
                    FieldName = argEx.ParamName,
                    Message = errorMsg,
                });
                return;
            }

            var serializationEx = ex as SerializationException;
            var errors = serializationEx?.Data["errors"] as List<RequestBindingError>;
            if (errors != null)
            {
                if (responseStatus.Errors == null)
                    responseStatus.Errors = new List<ResponseError>();

                responseStatus.Errors = errors.Select(e => new ResponseError
                {
                    ErrorCode = ex.GetType().Name,
                    FieldName = e.PropertyName,
                    Message = e.PropertyValueString != null 
                        ? $"'{e.PropertyValueString}' is an Invalid value for '{e.PropertyName}'"
                        : $"Invalid Value for '{e.PropertyName}'"
                }).ToList();
            }
        }

        public virtual void OnLogError(Type type, string message, Exception innerEx = null)
        {
            if (innerEx != null)
                Logger.Error(message, innerEx);
            else
                Logger.Error(message);
        }

        public virtual void OnSaveSession(IRequest httpReq, IAuthSession session, TimeSpan? expiresIn = null)
        {
            if (httpReq == null) return;

            var sessionKey = SessionFeature.GetSessionKey(session.Id ?? httpReq.GetOrCreateSessionId());
            session.LastModified = DateTime.UtcNow;
            this.GetCacheClient().CacheSet(sessionKey, session, expiresIn ?? GetDefaultSessionExpiry(httpReq));

            httpReq.Items[Keywords.Session] = session;
        }

        /// <summary>
        /// Inspect or modify ever new UserSession created or resolved from cache. 
        /// return null if Session is invalid to create new Session.
        /// </summary>
        public virtual IAuthSession OnSessionFilter(IAuthSession session, string withSessionId) => session;

        public virtual bool AllowSetCookie(IRequest req, string cookieName)
        {
            if (!Config.AllowSessionCookies)
                return cookieName != Keywords.SessionId
                    && cookieName != Keywords.PermanentSessionId
                    && cookieName != Keywords.SessionOptionsKey
                    && cookieName != HttpHeaders.XUserAuthId;

            return true;
        }

        public virtual IRequest TryGetCurrentRequest()
        {
            return null;
        }

        public virtual object OnAfterExecute(IRequest req, object requestDto, object response)
        {
            var responseDto = response is Task responseTask ? responseTask.GetResult() : response;

            if (req.Response.Dto == null)
            req.Response.Dto = responseDto;

            return responseDto;
        }

        public virtual MetadataTypesConfig GetTypesConfigForMetadata(IRequest req)
        {
            var typesConfig = new NativeTypesFeature().MetadataTypesConfig;
            typesConfig.IgnoreTypesInNamespaces.Clear();
            typesConfig.IgnoreTypes.Add(typeof(ResponseStatus));
            typesConfig.IgnoreTypes.Add(typeof(ResponseError));
            return typesConfig;
        }

        /// <summary>
        /// Gets IDbConnection Checks if DbInfo is seat in RequestContext.
        /// See multitenancy: http://docs.servicestack.net/multitenancy
        /// Called by itself, <see cref="Service"></see> and <see cref="ServiceStack.Razor.ViewPageBase"></see>
        /// </summary>
        /// <param name="req">Provided by services and pageView, can be helpfull when overriding this method</param>
        /// <returns></returns>
        public virtual IDbConnection GetDbConnection(IRequest req = null)
        {
            var dbFactory = Container.TryResolve<IDbConnectionFactory>();

            ConnectionInfo connInfo;
            if (req != null && (connInfo = req.GetItem(Keywords.DbInfo) as ConnectionInfo) != null)
            {
                if (!(dbFactory is IDbConnectionFactoryExtended dbFactoryExtended))
                    throw new NotSupportedException("ConnectionInfo can only be used with IDbConnectionFactoryExtended");

                if (connInfo.ConnectionString != null && connInfo.ProviderName != null)
                    return dbFactoryExtended.OpenDbConnectionString(connInfo.ConnectionString, connInfo.ProviderName);

                if (connInfo.ConnectionString != null)
                    return dbFactoryExtended.OpenDbConnectionString(connInfo.ConnectionString);

                if (connInfo.NamedConnection != null)
                    return dbFactoryExtended.OpenDbConnection(connInfo.NamedConnection);
            }

            return dbFactory.OpenDbConnection();
        }

        /// <summary>
        /// Resolves <see cref="IRedisClient"></see> based on <see cref="IRedisClientsManager"></see>.GetClient();
        /// Called by itself, <see cref="Service"></see> and <see cref="ServiceStack.Razor.ViewPageBase"></see>
        /// </summary>
        /// <param name="req">Provided by services and pageView, can be helpfull when overriding this method</param>
        /// <returns></returns>
        public virtual IRedisClient GetRedisClient(IRequest req = null)
        {
            return Container.TryResolve<IRedisClientsManager>().GetClient();
        }

        /// <summary>
        /// Tries to resolve <see cref="IRedisClient"></see> through Ioc container.
        /// If not registered, it falls back to <see cref="IRedisClientsManager"></see>.GetClient();
        /// Called by itself, <see cref="Service"></see> and <see cref="ServiceStack.Razor.ViewPageBase"></see>
        /// </summary>
        /// <param name="req">Provided by services and pageView, can be helpfull when overriding this method</param>
        /// <returns></returns>
        public virtual ICacheClient GetCacheClient(IRequest req)
        {
            return ((IResolver)req).GetCacheClient();
        }

        /// <summary>
        /// Returns <see cref="MemoryCacheClient"></see>. cache is only persisted for this running app instance.
        /// Called by <see cref="Service"></see>.MemoryCacheClient
        /// </summary>
        /// <param name="req">Provided by services and pageView, can be helpfull when overriding this method</param>
        /// <returns>Nullable MemoryCacheClient</returns>
        public virtual MemoryCacheClient GetMemoryCacheClient(IRequest req)
        {
            return Container.TryResolve<MemoryCacheClient>();
        }

        /// <summary>
        /// Returns <see cref="IMessageProducer"></see> from the IOC container.
        /// Called by itself, <see cref="Service"></see> and <see cref="ServiceStack.Razor.ViewPageBase"></see>
        /// </summary>
        /// <param name="req">Provided by services and PageViewBase, can be helpfull when overriding this method</param>
        /// <returns></returns>
        public virtual IMessageProducer GetMessageProducer(IRequest req = null)
        {
            return (Container.TryResolve<IMessageFactory>()
                ?? Container.TryResolve<IMessageService>().MessageFactory).CreateMessageProducer();
        }

        public virtual IServiceGateway GetServiceGateway(IRequest req)
        {
            var factory = Container.TryResolve<IServiceGatewayFactory>();
            return factory != null ? factory.GetServiceGateway(req) 
                : Container.TryResolve<IServiceGateway>()
                ?? new InProcessServiceGateway(req);
        }

        public virtual IAuthRepository GetAuthRepository(IRequest req = null)
        {
            return TryResolve<IAuthRepository>();
        }

        public virtual ICookies GetCookies(IHttpResponse res) => new Cookies(res);

        public virtual bool ShouldCompressFile(IVirtualFile file)
        {
            return !string.IsNullOrEmpty(file.Extension) 
                && Config.CompressFilesWithExtensions.Contains(file.Extension)
                && (file.Length > Config.CompressFilesLargerThanBytes);
        }
    }

}