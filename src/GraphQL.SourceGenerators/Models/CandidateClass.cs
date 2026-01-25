using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.SourceGenerators.Models;

/// <summary>
/// Holds the syntax and semantic information for a candidate class.
/// </summary>
internal readonly record struct CandidateClass(
    ClassDeclarationSyntax ClassDeclarationSyntax,
    SemanticModel SemanticModel);
