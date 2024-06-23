using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@override" directive attribute.
/// <para>
/// Indicates that an object field is now resolved by this subgraph instead of another subgraph where
/// it's also defined. This enables you to migrate a field from one subgraph to another.
/// </para>
/// </summary>
/// <remarks>
/// See <see href="https://www.apollographql.com/docs/federation/federated-types/federated-directives#override"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field)]
public class OverrideAttribute : GraphQLAttribute
{
    /// <summary>
    /// Gets the name of the service from which the field is overridden.
    /// </summary>
    public string From { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OverrideAttribute"/> class with the specified service name.
    /// </summary>
    /// <param name="from">The name of the service from which the field is overridden.</param>
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
