using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@key" directive attribute.
/// <para>
/// Designates an object type as an entity and specifies its key fields. Key fields are a set of fields
/// that a subgraph can use to uniquely identify any instance of the entity.
/// </para>
/// </summary>
/// <remarks>
/// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#key"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class KeyAttribute : GraphQLAttribute
{
    private readonly string _fields;

    /// <summary>
    /// Gets or sets a value indicating whether the key is resolvable.
    /// When set to true, the key can be used for resolving entities.
    /// Default value is true.
    /// </summary>
    public bool Resolvable { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyAttribute"/> class with a single string representing the key fields.
    /// </summary>
    /// <param name="fields">A space-separated string of field names that form the key.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="fields"/> parameter is null or empty.</exception>
    public KeyAttribute(string fields)
    {
        if (string.IsNullOrEmpty(fields))
            throw new ArgumentNullException(nameof(fields));
        _fields = fields;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyAttribute"/> class with multiple strings representing the key fields.
    /// </summary>
    /// <param name="fields">An array of field names that form the key.</param>
    public KeyAttribute(params string[] fields)
        : this(string.Join(" ", fields))
    {
    }

    /// <summary>
    /// Modifies the specified graph type by adding the key directive.
    /// </summary>
    /// <param name="graphType">The graph type to modify. Must be an <see cref="IObjectGraphType"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="graphType"/> is not an <see cref="IObjectGraphType"/>.</exception>
    public override void Modify(IGraphType graphType)
    {
        if (graphType is not IObjectGraphType objectGraphType)
            throw new ArgumentOutOfRangeException(nameof(graphType), graphType, "Only ObjectGraphType is supported.");
        objectGraphType.Key(_fields, Resolvable);
    }
}
