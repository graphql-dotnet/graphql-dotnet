using GraphQL.Types;

namespace GraphQL.Federation.Resolvers;

/// <summary>
/// Provides a mechanism for resolving objects from the <c>_entities</c> field in a GraphQL federation setup.
/// Each object type is associated with a specific implementation of this interface,
/// allowing for type-specific resolution based on the representation passed to the <c>_entities</c> field.
/// </summary>
public interface IFederationResolver
{
    /// <summary>
    /// Determines whether the source representation matches the required keys for this resolver.
    /// </summary>
    bool MatchKeys(IDictionary<string, object?> representation);

    /// <summary>
    /// Parses the source representation into a CLR type that can be used by the resolver.
    /// </summary>
    /// <param name="graphType">The object or interface graph type associated with the entity being resolved.</param>
    /// <param name="representation">The source representation provided by the Apollo Router.</param>
    object ParseRepresentation(IComplexGraphType graphType, IDictionary<string, object?> representation);

    /// <summary>
    /// Asynchronously resolves an object based on the given context and source representation.
    /// The source representation is parsed by <see cref="ParseRepresentation(IComplexGraphType, IDictionary{string, object?})"/>
    /// during the validation phase before being passed to this method's <paramref name="parsedRepresentation"/> argument.
    /// </summary>
    /// <param name="context">The context of the field being resolved, providing access to various aspects of the GraphQL execution.</param>
    /// <param name="graphType">The object graph type associated with the entity being resolved.</param>
    /// <param name="parsedRepresentation">The source representation, parsed by <see cref="ParseRepresentation(IComplexGraphType, IDictionary{string, object?})"/>.</param>
    /// <returns>A task that represents the asynchronous resolve operation. The task result contains the resolved object.</returns>
    ValueTask<object?> ResolveAsync(IResolveFieldContext context, IComplexGraphType graphType, object parsedRepresentation);
}
