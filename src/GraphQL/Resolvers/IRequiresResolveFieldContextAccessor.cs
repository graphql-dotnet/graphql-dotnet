using GraphQL.Execution;

namespace GraphQL.Resolvers;

/// <summary>
/// Indicates whether a type requires access to <see cref="IResolveFieldContextAccessor"/> during GraphQL execution.
/// </summary>
public interface IRequiresResolveFieldContextAccessor
{
    /// <summary>
    /// Gets a value indicating whether <see cref="IResolveFieldContextAccessor"/> is required for this resolver.
    /// </summary>
    public bool RequiresResolveFieldContextAccessor { get; }
}
