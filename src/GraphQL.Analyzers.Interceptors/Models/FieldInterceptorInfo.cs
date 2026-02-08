using Microsoft.CodeAnalysis.CSharp;

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
    public InterceptableLocation? Location { get; init; }

    /// <summary>
    /// The fully qualified name of the source type (TSourceType from ComplexGraphType&lt;TSourceType&gt;).
    /// </summary>
    public string? SourceTypeFullName { get; init; }

    /// <summary>
    /// The fully qualified name of the property type (TProperty from the Field method).
    /// </summary>
    public string? PropertyTypeFullName { get; init; }

    /// <summary>
    /// The name of the property or field being accessed (e.g., "Name" from x => x.Name).
    /// Only set if the expression is a simple member access.
    /// </summary>
    public string? MemberName { get; init; }

    /// <summary>
    /// Optional diagnostic information if transformation failed or encountered issues.
    /// </summary>
    public DiagnosticInfo? Diagnostic { get; init; }

    /// <summary>
    /// Indicates whether this info represents a valid interceptor (no diagnostic).
    /// </summary>
    public bool IsValid => Diagnostic == null && Location != null && SourceTypeFullName != null && PropertyTypeFullName != null && MemberName != null;
}
