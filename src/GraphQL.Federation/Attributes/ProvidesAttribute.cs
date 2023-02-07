using GraphQL.Federation.Extensions;
using GraphQL.Types;

namespace GraphQL.Federation.Attributes;

/// <summary>
/// Adds "@provides" directive.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class ProvidesAttribute : GraphQLAttribute
{
    private readonly string _fields;

    /// <summary> .ctor </summary>
    public ProvidesAttribute(string fields)
    {
        _fields = fields;
    }
    /// <summary> .ctor </summary>
    public ProvidesAttribute(params string[] fields)
        : this(string.Join(" ", fields.Select(x => x.ToCamelCase())))
    { }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            throw new ArgumentOutOfRangeException(nameof(isInputType), isInputType, "Input types are not supported.");
        fieldType.Provides(_fields);
    }
}
