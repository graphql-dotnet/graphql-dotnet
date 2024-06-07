using GraphQL.Types;

namespace GraphQL.Federation;

/// <summary>
/// Represents a GraphQL Federation "@override" directive attribute.
/// This attribute is used to indicate that a field overrides a field from another service.
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
