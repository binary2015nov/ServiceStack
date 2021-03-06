﻿#if NETSTANDARD2_0

using System;
using ServiceStack.Logging;
using Microsoft.Extensions.Logging;

namespace ServiceStack.NetCore
{
    public class NetCoreLogFactory : ILogFactory
    {
        ILoggerFactory loggerFactory;
        private bool debugEnabled;

        public NetCoreLogFactory(ILoggerFactory loggerFactory, bool debugEnabled = false)
        {
            this.loggerFactory = loggerFactory;
            this.debugEnabled = debugEnabled;
        }

        public ILog GetLogger(Type type)
        {
            return new NetCoreLog(loggerFactory.CreateLogger(type), debugEnabled);
        }

        public ILog GetLogger(string name)
        {
            return new NetCoreLog(loggerFactory.CreateLogger(name), debugEnabled);
        }
    }

    public class NetCoreLog : ILog
    {
        private ILogger log;

        public NetCoreLog(ILogger logger, bool debugEnabled = false)
        {
            this.log = logger;
            this.IsDebugEnabled = debugEnabled;
        }

        public bool IsDebugEnabled { get; }

        public void Debug(object message)
        {
            log.LogDebug(message.ToString());
        }

        public void Debug(object message, Exception exception)
        {
            log.LogDebug(default(EventId), exception, message.ToString());
        }

        public void DebugFormat(string format, params object[] args)
        {
            log.LogDebug(format, args);
        }

        public void Error(object message)
        {
            log.LogError(message.ToString());
        }

        public void Error(object message, Exception exception)
        {
            log.LogError(default(EventId), exception, message.ToString());
        }

        public void ErrorFormat(string format, params object[] args)
        {
            log.LogError(format, args);
        }

        public void Fatal(object message)
        {
            log.LogCritical(message.ToString());
        }

        public void Fatal(object message, Exception exception)
        {
            log.LogCritical(default(EventId), exception, message.ToString());
        }

        public void FatalFormat(string format, params object[] args)
        {
            log.LogCritical(format, args);
        }

        public void Info(object message)
        {
            log.LogInformation(message.ToString());
        }

        public void Info(object message, Exception exception)
        {
            log.LogInformation(default(EventId), exception, message.ToString());
        }

        public void InfoFormat(string format, params object[] args)
        {
            log.LogInformation(format, args);
        }

        public void Warn(object message)
        {
            log.LogWarning(message.ToString());
        }

        public void Warn(object message, Exception exception)
        {
            log.LogWarning(default(EventId), exception, message.ToString());
        }

        public void WarnFormat(string format, params object[] args)
        {
            log.LogWarning(format, args);
        }
    }
}

#endif
