using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@requires" directive attribute.
/// This attribute is used to indicate that a field requires certain fields from the parent type, enabling complex field resolution across services.
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
