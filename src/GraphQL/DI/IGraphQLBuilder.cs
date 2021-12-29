namespace GraphQL.DI
{
    /// <summary>
    /// An interface for configuring GraphQL.NET services.
    /// </summary>
    public interface IGraphQLBuilder
    {
        /// <summary>
        /// Provides an interface for registering services with the dependency injection provider.
        /// </summary>
        IServiceRegister Services { get; }
    }
}
