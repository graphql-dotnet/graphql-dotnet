using GraphQL.Types;

namespace GraphQL.Federation.Attributes;

/// <summary>
/// Adds "@requires" directive.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class RequiresAttribute : GraphQLAttribute
{
    private readonly string _fields;

    /// <summary> .ctor </summary>
    public RequiresAttribute(string fields)
    {
        if (string.IsNullOrEmpty(fields))
            throw new ArgumentNullException(nameof(fields));
        _fields = fields;
    }
    /// <summary> .ctor </summary>
    public RequiresAttribute(params string[] fields)
        : this(string.Join(" ", fields))
    { }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            throw new ArgumentOutOfRangeException(nameof(isInputType), isInputType, "Input types are not supported.");
        fieldType.Requires(_fields);
    }
}
