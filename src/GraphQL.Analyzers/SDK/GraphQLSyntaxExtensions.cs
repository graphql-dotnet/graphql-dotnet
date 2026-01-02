using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Extension methods for working with GraphQL SDK wrappers.
/// </summary>
public static class GraphQLSyntaxExtensions
{
    /// <summary>
    /// Tries to get a GraphQLFieldInvocation from an invocation expression.
    /// </summary>
    public static GraphQLFieldInvocation? AsGraphQLField(this InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return GraphQLFieldInvocation.TryCreate(invocation, semanticModel);
    }

    /// <summary>
    /// Tries to get a GraphQLGraphType from a class declaration.
    /// </summary>
    public static GraphQLGraphType? AsGraphQLGraphType(this ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
    {
        return GraphQLGraphType.TryCreate(classDeclaration, semanticModel);
    }

    /// <summary>
    /// Tries to get a GraphQLFieldArgument from an invocation expression.
    /// </summary>
    public static GraphQLFieldArgument? AsGraphQLFieldArgument(this InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        return GraphQLFieldArgument.TryCreate(invocation, semanticModel);
    }

    /// <summary>
    /// Gets all GraphQL field invocations within a syntax node.
    /// </summary>
    public static IEnumerable<GraphQLFieldInvocation> GetGraphQLFields(this SyntaxNode node, SemanticModel semanticModel)
    {
        foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var field = GraphQLFieldInvocation.TryCreate(invocation, semanticModel);
            if (field != null)
            {
                yield return field;
            }
        }
    }

    /// <summary>
    /// Gets all GraphQL graph type declarations within a syntax node.
    /// </summary>
    public static IEnumerable<GraphQLGraphType> GetGraphQLGraphTypes(this SyntaxNode node, SemanticModel semanticModel)
    {
        foreach (var classDecl in node.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var graphType = GraphQLGraphType.TryCreate(classDecl, semanticModel);
            if (graphType != null)
            {
                yield return graphType;
            }
        }
    }

    /// <summary>
    /// Finds the first ancestor node of type T.
    /// </summary>
    public static T? FirstAncestorOrSelf<T>(this SyntaxNode node) where T : SyntaxNode
    {
        var current = node;
        while (current != null)
        {
            if (current is T result)
            {
                return result;
            }
            current = current.Parent;
        }
        return null;
    }
}
