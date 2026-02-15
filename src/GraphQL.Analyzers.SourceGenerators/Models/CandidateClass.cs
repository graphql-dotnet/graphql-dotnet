using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Models;

/// <summary>
/// Holds the symbol information for a candidate class.
/// </summary>
public readonly record struct CandidateClass(
    INamedTypeSymbol ClassSymbol);
