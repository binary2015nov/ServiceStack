using System;

namespace ServiceStack.Logging
{
	/// <summary>
	/// Default logger is to System.Diagnostics.Debug.WriteLine
	/// </summary>
	public class DebugLogger : ILog
	{
		public string Name { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugLogger"/> class.
		/// </summary>
		public DebugLogger(string name)
		{
			Name = name.EndsWith("-") ? name : name + "-";
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugLogger"/> class.
		/// </summary>
		public DebugLogger(Type type)
		{
			Name = type.Name + "-";
		}

		/// <summary>
		/// Logs the specified message.
		/// </summary>
		private static void Log(object message, Exception exception)
		{
			string msg = message?.ToString() ?? string.Empty;
			if (exception != null)
			{
				msg += (msg == string.Empty ? "Exception: " : ", Exception: ") + exception.Message;
			}
			System.Diagnostics.Debug.WriteLine(msg);
		}

		/// <summary>
		/// Logs the format.
		/// </summary>
		private static void LogFormat(object format, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine(string.Format(format?.ToString(), args));
		}

		/// <summary>
		/// Logs the specified message.
		/// </summary>
		private static void Log(object message)
		{
			System.Diagnostics.Debug.WriteLine(message?.ToString() ?? string.Empty);
		}

		public bool IsDebugEnabled { get; set; }

		public void Debug(object message, Exception exception)
        {
            if (IsDebugEnabled)
                Log(Name + LogLevels.Debug + message, exception);
		}

		public void Debug(object message)
        {
            if (IsDebugEnabled)
                Log(Name + LogLevels.Debug + message);
		}

		public void DebugFormat(string format, params object[] args)
        {
            if (IsDebugEnabled)
                LogFormat(Name + LogLevels.Debug + format, args);
		}

		public void Error(object message, Exception exception)
		{
			Log(Name + LogLevels.Error + message, exception);
		}

		public void Error(object message)
		{
			Log(Name + LogLevels.Error + message);
		}

		public void ErrorFormat(string format, params object[] args)
		{
			LogFormat(Name + LogLevels.Error + format, args);
		}

		public void Fatal(object message, Exception exception)
		{
			Log(Name + LogLevels.Fatal + message, exception);
		}

		public void Fatal(object message)
		{
			Log(Name + LogLevels.Fatal + message);
		}

		public void FatalFormat(string format, params object[] args)
		{
			LogFormat(Name + LogLevels.Fatal + format, args);
		}

		public void Info(object message, Exception exception)
		{
			Log(Name + LogLevels.Info + message, exception);
		}

		public void Info(object message)
		{
			Log(Name + LogLevels.Info + message);
		}

		public void InfoFormat(string format, params object[] args)
		{
			LogFormat(Name + LogLevels.Info + format, args);
		}

		public void Warn(object message, Exception exception)
		{
			Log(Name + LogLevels.Warn + message, exception);
		}

		public void Warn(object message)
		{
			Log(Name + LogLevels.Warn + message);
		}

		public void WarnFormat(string format, params object[] args)
		{
			LogFormat(Name + LogLevels.Warn + format, args);
		}
	}
}