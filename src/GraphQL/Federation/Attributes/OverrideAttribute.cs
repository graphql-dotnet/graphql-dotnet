using GraphQL.Federation.Extensions;
using GraphQL.Types;

namespace GraphQL.Federation.Attributes;

/// <summary>
/// Adds "@override" directive.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class OverrideAttribute : GraphQLAttribute
{
    /// <summary> .ctor </summary>
    public string From { get; }

    /// <summary> .ctor </summary>
    public OverrideAttribute(string from)
    {
        From = from;
    }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            throw new ArgumentOutOfRangeException(nameof(isInputType), isInputType, "Input types are not supported.");
        fieldType.Override(From);
    }
}
