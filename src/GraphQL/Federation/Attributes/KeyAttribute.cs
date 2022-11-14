using GraphQL.Federation.Extensions;
using GraphQL.Types;

namespace GraphQL.Federation.Attributes;

/// <summary>
/// Adds "@key" directive.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class KeyAttribute : GraphQLAttribute
{
    private readonly string _fields;

    /// <summary> Resolvable. </summary>
    public bool Resolvable { get; set; } = true;


    /// <summary> .ctor </summary>
    public KeyAttribute(string fields)
    {
        if (string.IsNullOrEmpty(fields))
            throw new ArgumentNullException(nameof(fields));
        _fields = fields;
    }
    /// <summary> .ctor </summary>
    public KeyAttribute(params string[] fields)
        : this(string.Join(" ", fields.Select(x => x.ToCamelCase())))
    { }

    /// <inheritdoc/>
    public override void Modify(IGraphType graphType)
    {
        if (graphType is not IObjectGraphType objectGraphType)
            throw new ArgumentOutOfRangeException(nameof(graphType), graphType, "Only ObjectGraphType is supported.");
        objectGraphType.Key(_fields, Resolvable);
    }
}
