using GraphQL.Federation.Extensions;
using GraphQL.Types;

namespace GraphQL.Federation.Attributes;

/// <summary>
/// Adds "@external" directive.
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
