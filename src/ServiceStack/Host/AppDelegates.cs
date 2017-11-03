﻿using System;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host
{
	public delegate IHttpHandler HttpHandlerResolver(string httpMethod, string pathInfo, string filePath);

	public delegate bool StreamSerializerResolverDelegate(IRequest requestContext, object dto, IResponse httpRes);

	public delegate void UncatchedExceptionHandler(
		IRequest httpReq, IResponse httpRes, string operationName, Exception ex);

	public delegate object ServiceExceptionHandler(IRequest request, object requestDto, Exception ex);

    public delegate RestPath FallbackRestPathDelegate(IHttpRequest httpReq);

	public interface ITypedFilter
	{
		void Invoke(IRequest req, IResponse res, object dto);
	}

	public interface ITypedFilter<in T>
	{
		void Invoke(IRequest req, IResponse res, T dto);
	}

	public class TypedFilter<T> : ITypedFilter
	{
		private readonly Action<IRequest, IResponse, T> action;
		public TypedFilter(Action<IRequest, IResponse, T> action)
		{
			this.action = action;
		}

		public void Invoke(IRequest req, IResponse res, object dto)
		{
			action(req, res, (T)dto);
		}
	}
}