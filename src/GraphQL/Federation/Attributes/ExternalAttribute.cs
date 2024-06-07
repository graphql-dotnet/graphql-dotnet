using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@external" directive attribute.
/// This attribute is used to mark a field as external, indicating that the field is owned by another service.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class ExternalAttribute : GraphQLAttribute
{
    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            throw new ArgumentOutOfRangeException(nameof(isInputType), isInputType, "Input types are not supported.");
        fieldType.External();
    }
}
