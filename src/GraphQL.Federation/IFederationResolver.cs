namespace GraphQL.Federation;

/// <summary>
/// IFederationResolver.
/// </summary>
public interface IFederationResolver
{
    /// <summary>
    /// Type used to deserialize $representations variable item(s).
    /// </summary>
    Type SourceType { get; }

    /// <summary>
    /// Resolver for _service entity item(s).
    /// </summary>
    /// <param name="context">Contains parameters pertaining to the currently executing field.</param>
    /// <param name="source">Deserialized item from $representations variable.</param>
    object? Resolve(IResolveFieldContext context, object source);
}
