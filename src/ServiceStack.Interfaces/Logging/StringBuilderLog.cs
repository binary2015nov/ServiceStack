using System;
using System.Text;

namespace ServiceStack.Logging
{
    public class StringBuilderLog : ILog
    {
        private readonly StringBuilder logs;

        public StringBuilderLog(string name, StringBuilder logs)
        {
            this.logs = logs;
        }

        public StringBuilderLog(Type type, StringBuilder logs)
        {
            this.logs = logs;
        }

        public bool IsDebugEnabled { get; set; }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        private void Log(object message, Exception exception)
        {
            var msg = message?.ToString() ?? string.Empty;
            if (exception != null)
            {
                msg += ", Exception: " + exception.Message;
            }
            lock (logs)
                logs.AppendLine(msg);
        }

        /// <summary>
        /// Logs the format.
        /// </summary>
        private void LogFormat(object message, params object[] args)
        {
            var msg = message?.ToString() ?? string.Empty;
            lock (logs)
            {
                logs.AppendFormat(msg, args);
                logs.AppendLine();
            }
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void Log(object message)
        {
            var msg = message?.ToString() ?? string.Empty;
            lock (logs)
            {
                logs.AppendLine(msg);
            }
        }

        public void Debug(object message, Exception exception)
        {
            if (IsDebugEnabled)
                Log(LogLevels.Debug + message, exception);
        }

        public void Debug(object message)
        {
            if (IsDebugEnabled)
                Log(LogLevels.Debug + message);
        }

        public void DebugFormat(string format, params object[] args)
        {
            if (IsDebugEnabled)
                LogFormat(LogLevels.Debug + format, args);
        }

        public void Error(object message, Exception exception)
        {
            Log(LogLevels.Error + message, exception);
        }

        public void Error(object message)
        {
            Log(LogLevels.Error + message);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            LogFormat(LogLevels.Error + format, args);
        }

        public void Fatal(object message, Exception exception)
        {
            Log(LogLevels.Fatal + message, exception);
        }

        public void Fatal(object message)
        {
            Log(LogLevels.Fatal + message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            LogFormat(LogLevels.Fatal + format, args);
        }

        public void Info(object message, Exception exception)
        {
            Log(LogLevels.Info + message, exception);
        }

        public void Info(object message)
        {
            Log(LogLevels.Info + message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            LogFormat(LogLevels.Info + format, args);
        }

        public void Warn(object message, Exception exception)
        {
            Log(LogLevels.Warn + message, exception);
        }

        public void Warn(object message)
        {
            Log(LogLevels.Warn + message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            LogFormat(LogLevels.Warn + format, args);
        }
    }
}
