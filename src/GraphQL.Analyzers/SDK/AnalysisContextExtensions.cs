using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers.SDK;

/// <summary>
/// Provides extension methods for <see cref="AnalysisContext"/> to simplify analyzer development for GraphQL types.
/// </summary>
public static class AnalysisContextExtensions
{
    /// <summary>
    /// Registers a syntax node action that will be invoked for each GraphQL graph type class declaration.
    /// Only classes that represent GraphQL types (implementing IGraphType or deriving from graph type base classes) will trigger the action.
    /// </summary>
    /// <param name="context">The analysis context to register the action with.</param>
    /// <param name="action">The action to execute for each GraphQL graph type. Receives the graph type and analysis context.</param>
    public static void OnGraphQLGraphType(this AnalysisContext context, Action<GraphQLGraphType, SyntaxNodeAnalysisContext> action)
    {
        context.RegisterSyntaxNodeAction(
            analysisContext =>
            {
                var classDeclaration = (ClassDeclarationSyntax)analysisContext.Node;
                var graphType = GraphQLGraphType.TryCreate(classDeclaration, analysisContext.SemanticModel);
                if (graphType != null)
                    action(graphType, analysisContext);
            }, SyntaxKind.ClassDeclaration);
    }
}
