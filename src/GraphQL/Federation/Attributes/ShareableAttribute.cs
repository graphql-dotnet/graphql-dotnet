using GraphQL.Federation.Extensions;
using GraphQL.Types;

namespace GraphQL.Federation.Attributes;

/// <summary>
/// Adds "@shareable" directive.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
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
