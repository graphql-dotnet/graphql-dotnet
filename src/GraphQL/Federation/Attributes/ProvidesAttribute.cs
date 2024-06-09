using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@provides" directive attribute.
/// <para>
/// Specifies a set of entity fields that a subgraph can resolve, but only at a
/// particular schema path (at other paths, the subgraph can't resolve those fields).
/// </para>
/// </summary>
/// <remarks>
/// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#provides"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class ProvidesAttribute : GraphQLAttribute
{
    private readonly string _fields;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProvidesAttribute"/> class with a single string representing the provided fields.
    /// </summary>
    /// <param name="fields">A space-separated string of field names that are provided by this field.</param>
    public ProvidesAttribute(string fields)
    {
        _fields = fields;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProvidesAttribute"/> class with multiple strings representing the provided fields.
    /// </summary>
    /// <param name="fields">An array of field names that are provided by this field.</param>
    public ProvidesAttribute(params string[] fields)
        : this(string.Join(" ", fields))
    { }

    /// <inheritdoc/>
    public override void Modify(FieldType fieldType, bool isInputType)
    {
        if (isInputType)
            throw new ArgumentOutOfRangeException(nameof(isInputType), isInputType, "Input types are not supported.");
        fieldType.Provides(_fields);
    }
}
