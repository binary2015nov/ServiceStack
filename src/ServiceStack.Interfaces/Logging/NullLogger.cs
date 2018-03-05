using System;

namespace ServiceStack.Logging
{
	public class NullLogger : ILog
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NullLogger"/> class.
		/// </summary>
		public NullLogger(string name) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="NullLogger"/> class.
        /// </summary>
        public NullLogger(Type type) { }

		public bool IsDebugEnabled { get; set; }

		public void Debug(object message, Exception exception) { }

		public void Debug(object message) { }

		public void DebugFormat(string format, params object[] args) { }

		public void Info(object message, Exception exception) { }

		public void Info(object message) { }

		public void InfoFormat(string format, params object[] args) { }

		public void Error(object message, Exception exception) { }

		public void Error(object message) { }

		public void ErrorFormat(string format, params object[] args) { }

		public void Fatal(object message, Exception exception) { }

		public void Fatal(object message) { }

		public void FatalFormat(string format, params object[] args) { }

		public void Warn(object message, Exception exception) { }

		public void Warn(object message) { }

		public void WarnFormat(string format, params object[] args) { }
	}
}
