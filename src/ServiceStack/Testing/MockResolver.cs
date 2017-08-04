using Funq;
using ServiceStack.Configuration;

namespace ServiceStack.Testing
{
    public class MockResolver : IResolver
    {
        private readonly Container container;

        public MockResolver() : this(new Container()) {}

        public MockResolver(Container container)
        {
            this.container = container;
        }

        public T TryResolve<T>()
        {
            return this.container.TryResolve<T>();
        }
    }
}