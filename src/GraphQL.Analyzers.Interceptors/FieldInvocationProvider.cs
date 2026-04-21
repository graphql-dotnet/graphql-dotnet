using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Interceptors;

/// <summary>
/// Provides eligible Field method call candidates for interception.
/// </summary>
internal static class FieldInvocationProvider
{
    /// <summary>
    /// Creates an incremental values provider that identifies potential Field method invocations
    /// and transforms them using the provided delegate.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    /// <param name="transform">Transform delegate that receives the invocation syntax and semantic model.</param>
    public static IncrementalValuesProvider<T> Create<T>(
        IncrementalGeneratorInitializationContext context,
        Func<InvocationExpressionSyntax, SemanticModel, CancellationToken, T> transform)
    {
        return context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsFieldInvocationCandidate(node),
                transform: (ctx, token) =>
                {
                    var invocation = (InvocationExpressionSyntax)ctx.Node;
                    return transform(invocation, ctx.SemanticModel, token);
                });
    }

    private static bool IsFieldInvocationCandidate(SyntaxNode node)
    {
        // Look for invocation expressions with "Field" as the method name
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Name.Identifier.Text == "Field";
        }

        if (invocation.Expression is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.Text == "Field";
        }

        return false;
    }
}
