using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@inaccessible" directive attribute.
/// This attribute is used to mark elements as inaccessible, indicating they should not be exposed to consumers.
/// </summary>
/// <remarks>
/// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#inaccessible"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Enum | AttributeTargets.Parameter)]
public class InaccessibleAttribute : GraphQLAttribute
{
    /// <inheritdoc/>
    public override void Modify(IGraphType graphType)
    {
        graphType.Inaccessible();
    }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            throw new ArgumentOutOfRangeException(nameof(isInputType), isInputType, "Input types are not supported.");
        fieldType.Inaccessible();
    }
}
