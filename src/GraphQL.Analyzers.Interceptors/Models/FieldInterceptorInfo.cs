namespace GraphQL.Analyzers.Interceptors.Models;

/// <summary>
/// Contains primitive information about a Field method call that should be intercepted.
/// This record can hold both successful transformation data and diagnostic information.
/// </summary>
internal sealed record FieldInterceptorInfo
{
    /// <summary>
    /// The interceptable location of the Field method call.
    /// </summary>
    public required ComparableInterceptableLocation Location { get; init; }

    /// <summary>
    /// The fully qualified name of the source type (TSourceType from ComplexGraphType&lt;TSourceType&gt;).
    /// </summary>
    public required string SourceTypeFullName { get; init; }

    /// <summary>
    /// The fully qualified name of the property type (TProperty from the Field method).
    /// </summary>
    public required string PropertyTypeFullName { get; init; }

    /// <summary>
    /// The name of the property or field being accessed (e.g., "Name" from x => x.Name).
    /// </summary>
    public required string MemberName { get; init; }

    /// <summary>
    /// Indicates whether the Field call has a 'name' parameter (string).
    /// </summary>
    public bool HasNameParameter { get; init; }

    /// <summary>
    /// Indicates whether the Field call has a 'nullable' parameter (bool).
    /// </summary>
    public bool HasNullableParameter { get; init; }

    /// <summary>
    /// Indicates whether the Field call has a 'type' parameter (Type).
    /// </summary>
    public bool HasTypeParameter { get; init; }
}
