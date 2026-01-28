using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.SourceGenerators.Providers;

/// <summary>
/// Provider D: Identifies candidate class declarations that have AOT-related attributes.
/// Uses efficient syntax-based filtering combined with semantic attribute checking.
/// </summary>
internal static class SyntaxProvider
{
    /// <summary>
    /// Creates an incremental provider that identifies all partial class declarations
    /// decorated with AOT-related attributes.
    /// </summary>
    /// <remarks>
    /// This implementation uses a two-stage approach:
    /// 1. Syntax predicate: Fast check for partial classes with attributes
    /// 2. Semantic transform: Verifies the presence of AOT attributes
    /// </remarks>
    public static IncrementalValuesProvider<ClassDeclarationSyntax> CreateCandidateProvider(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsPartialClassWithAttributes(node),
                transform: static (context, _) => TransformCandidate(context))
            .Where(static classDecl => classDecl != null)!;
    }

    /// <summary>
    /// Fast syntax-only check to determine if a node is a partial class with attributes.
    /// </summary>
    private static bool IsPartialClassWithAttributes(SyntaxNode node)
    {
        // Must be a class declaration
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Must have the partial modifier (AOT schemas must be partial for code generation)
        if (!classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            return false;

        // Must have at least one attribute list
        return classDecl.AttributeLists.Count > 0;
    }

    /// <summary>
    /// Semantic analysis to verify the class has AOT attributes.
    /// </summary>
    private static ClassDeclarationSyntax? TransformCandidate(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Get the semantic model for attribute analysis
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (symbol == null)
            return null;

        // Check if any attribute matches our AOT attribute names
        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
                continue;

            // Get the fully qualified name
            var fullName = attributeClass.ToDisplayString();

            // Check against all AOT attribute names
            // Handle both fully qualified name and short name (e.g., AotQueryTypeAttribute and AotQueryType)
            foreach (var aotAttributeName in Constants.AttributeNames.All)
            {
                if (fullName == aotAttributeName ||
                    fullName.StartsWith(aotAttributeName.Replace("Attribute", "")) ||
                    fullName == aotAttributeName.Replace("GraphQL.DI.", ""))
                {
                    return classDeclaration;
                }
            }
        }

        return null;
    }
}
