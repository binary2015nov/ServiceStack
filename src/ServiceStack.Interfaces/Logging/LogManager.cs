using System;

namespace ServiceStack.Logging
{
    /// <summary>
    /// Provides access to log factories and loggers. This class cannot be inherited.
    /// </summary>
    public static class LogManager
    {
        private static ILogFactory logFactory;

        /// <summary>
        /// Gets or sets the log factory used to create loggers. The default value is <see cref="ServiceStack.Logging.NullLogFactory"/>.
        /// </summary>
        public static ILogFactory LogFactory
        {
            get { return logFactory ?? (logFactory = new NullLogFactory()); }
            set { logFactory = value; }
        }

        public static ILog GetLogger(Type type)
        {
            return LogFactory.GetLogger(type);
        }

        public static ILog GetLogger(string name)
        {
            return LogFactory.GetLogger(name);
        }
    }
}
