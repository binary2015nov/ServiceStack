using System;

namespace ServiceStack.Logging
{
	/// <summary>
	/// Default logger is to Console.WriteLine
	/// </summary>
	public class ConsoleLogger : ILog
	{
		public string Name { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugLogger"/> class.
		/// </summary>
		public ConsoleLogger(string name)
		{
			Name = name.EndsWith("-") ? name : name + "-";
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DebugLogger"/> class.
		/// </summary>
		public ConsoleLogger(Type type)
		{
			Name = type.Name + "-";
		}

		public bool IsDebugEnabled { get; set; }

		/// <summary>
		/// Logs the specified message.
		/// </summary>
		private static void Log(object message, Exception exception)
		{
			var msg = message?.ToString() ?? string.Empty;
			if (exception != null)
			{
				msg += ", Exception: " + exception.Message;
			}
			Console.WriteLine(msg);
		}

		/// <summary>
		/// Logs the format.
		/// </summary>
		private static void LogFormat(object message, params object[] args)
		{
			string msg = message?.ToString() ?? string.Empty;
			Console.WriteLine(msg, args);
		}

		/// <summary>
		/// Logs the specified message.
		/// </summary>
		private static void Log(object message)
		{
			string msg = message?.ToString() ?? string.Empty;
			Console.WriteLine(msg);
		}

		public void Debug(object message, Exception exception)
		{
			Log(Name + LogLevels.Debug + message, exception);
		}

		public void Debug(object message)
		{
			Log(Name + LogLevels.Debug + message);
		}

		public void DebugFormat(string format, params object[] args)
		{
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
