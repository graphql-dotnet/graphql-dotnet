using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.Interceptors.Models;

/// <summary>
/// Contains primitive diagnostic information that can be reported by the source generator.
/// </summary>
internal sealed record DiagnosticInfo
{
    /// <summary>
    /// The diagnostic ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The diagnostic title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The diagnostic message format.
    /// </summary>
    public required string MessageFormat { get; init; }

    /// <summary>
    /// The diagnostic category.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// The diagnostic severity.
    /// </summary>
    public required DiagnosticSeverity Severity { get; init; }

    /// <summary>
    /// The location where the diagnostic should be reported (stored as string for equality).
    /// </summary>
    public string? LocationString { get; init; }

    /// <summary>
    /// Optional message arguments for formatting (stored as string for equality).
    /// </summary>
    public string? MessageArgsString { get; init; }

    /// <summary>
    /// Creates a Diagnostic from this info.
    /// </summary>
    public Diagnostic CreateDiagnostic(Location? location)
    {
        var descriptor = new DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            Severity,
            isEnabledByDefault: true);

        return Diagnostic.Create(descriptor, location ?? Location.None);
    }
}
