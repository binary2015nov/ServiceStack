namespace ServiceStack.Configuration
{
    public interface IResolver
    {
        /// <summary>
        /// Resolve a dependency from the AppHost's IOC
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        TService TryResolve<TService>();
    }

    public interface IHasResolver
    {
        IResolver Resolver { get; }
    }
}