using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@shareable" directive attribute.
/// <para>
/// Indicates that an object type's field is allowed to be resolved by multiple subgraphs (by default in
/// Federation 2, object fields can be resolved by only one subgraph).
/// </para>
/// </summary>
/// <remarks>
/// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#shareable"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = true)]
public class ShareableAttribute : GraphQLAttribute
{
    /// <inheritdoc/>
    public override void Modify(IGraphType graphType)
    {
        if (graphType is IObjectGraphType objectGraphType)
            objectGraphType.Shareable();
        else
            throw new ArgumentOutOfRangeException(nameof(graphType), "Only object types are supported.");
    }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            throw new ArgumentOutOfRangeException(nameof(isInputType), "Input types are not supported.");
        fieldType.Shareable();
    }
}
