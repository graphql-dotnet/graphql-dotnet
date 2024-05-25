namespace GraphQL.Federation;

/// <summary>
/// Provides a mechanism for resolving objects from the <c>_entities</c> field in a GraphQL federation setup.
/// Each object type is associated with a specific implementation of this interface,
/// allowing for type-specific resolution based on the representation passed to the <c>_entities</c> field.
/// </summary>
public interface IFederationResolver
{
    /// <summary>
    /// Gets the CLR type of the representation that this resolver is responsible for.
    /// This property indicates the type to which the 'source' parameter's representation
    /// will be converted before being passed to the <see cref="ResolveAsync(IResolveFieldContext, object)"/> method.
    /// </summary>
    Type SourceType { get; }

    /// <summary>
    /// Asynchronously resolves an object based on the given context and source representation.
    /// The source representation is converted to the CLR type specified by <see cref="SourceType"/>
    /// before being passed to this method's <paramref name="source"/> argument.
    /// </summary>
    /// <param name="context">The context of the field being resolved, providing access to various aspects of the GraphQL execution.</param>
    /// <param name="source">The source representation, converted to the CLR type specified by <see cref="SourceType"/>.</param>
    /// <returns>A task that represents the asynchronous resolve operation. The task result contains the resolved object.</returns>
    ValueTask<object?> ResolveAsync(IResolveFieldContext context, object source);
}

