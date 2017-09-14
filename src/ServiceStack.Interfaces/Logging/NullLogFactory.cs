using System;

namespace ServiceStack.Logging
{
    /// <summary>
    /// Creates a Debug Logger, that logs all messages to: System.Diagnostics.Debug
    /// 
    /// Made public so its testable
    /// </summary>
	public class NullLogFactory : ILogFactory
    {
        public ILog GetLogger(Type type)
        {
			return new NullLogger(type);
        }

        public ILog GetLogger(string name)
        {
            return new NullLogger(name);
        }
    }
}
