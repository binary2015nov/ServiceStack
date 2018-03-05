using System;

namespace ServiceStack.Logging
{
	/// <summary>
	/// Creates a empty Logger, that does not log anything.
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
