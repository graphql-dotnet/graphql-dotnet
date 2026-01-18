using GraphQL.DI;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.MicrosoftDI;

/// <summary>
/// An implementation of <see cref="IGraphQLBuilder"/> which uses the Microsoft dependency injection framework
/// to register services and configure options.
/// </summary>
public class GraphQLAotBuilder : GraphQLBuilder
{
    /// <summary>
    /// Initializes a new instance for the specified service collection.
    /// </summary>
    /// <remarks>
    /// Registers various default services via <see cref="GraphQLBuilderBase.RegisterDefaultServices"/>
    /// after executing the configuration delegate.
    /// </remarks>
    public GraphQLAotBuilder(IServiceCollection services, Action<IGraphQLBuilder>? configure)
        : base(services, configure, true)
    {
    }
}
