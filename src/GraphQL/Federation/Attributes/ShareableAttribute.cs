using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@shareable" directive attribute.
/// This attribute is used to mark types or fields as shareable, indicating that they can be resolved by multiple services.
/// </summary>
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
