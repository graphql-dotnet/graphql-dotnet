using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@requires" directive attribute.
/// <para>
/// Indicates that the resolver for a particular entity field depends on the values of other entity fields that
/// are resolved by other subgraphs. This tells the router that it needs to fetch the values of those externally
/// defined fields first, even if the original client query didn't request them.
/// </para>
/// </summary>
/// <remarks>
/// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#requires"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class RequiresAttribute : GraphQLAttribute
{
    private readonly string _fields;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresAttribute"/> class with a single string representing the required fields.
    /// </summary>
    /// <param name="fields">A space-separated string of field names that are required by this field.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="fields"/> parameter is null or empty.</exception>
    public RequiresAttribute(string fields)
    {
        if (string.IsNullOrEmpty(fields))
            throw new ArgumentNullException(nameof(fields));
        _fields = fields;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresAttribute"/> class with multiple strings representing the required fields.
    /// </summary>
    /// <param name="fields">An array of field names that are required by this field.</param>
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
