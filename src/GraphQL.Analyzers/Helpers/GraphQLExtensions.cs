using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Helpers;

public static class GraphQLExtensions
{
    private static readonly byte[] _publicKey = typeof(GraphQLExtensions).Assembly.GetName().GetPublicKey();

    /// <summary>
    /// Checks if the given invocation expression represents a method call defined by the GraphQL library
    /// with the specified method name.
    /// </summary>
    /// <param name="invocation">The <see cref="InvocationExpressionSyntax"/> to check.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for semantic analysis.</param>
    /// <param name="methodName">The expected method name to match.</param>
    /// <returns>
    /// <see langword="true"/> if the invocation represents a call to a method with the specified name
    /// that is defined by the GraphQL library; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool IsGraphQLMethodInvocation(
        this InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        string methodName)
    {
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return false;
        }

        if (methodSymbol.Name != methodName)
        {
            return false;
        }

        return methodSymbol.IsGraphQLSymbol();
    }

    /// <summary>
    /// Checks if the given <see cref="SyntaxNode"/> represents a symbol defined by the GraphQL library.
    /// </summary>
    /// <param name="syntaxNode">The <see cref="SyntaxNode"/> to check.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for semantic analysis.</param>
    /// <returns>
    /// <see langword="true"/> if the symbol represented by the <paramref name="syntaxNode"/>
    /// is defined by the GraphQL library; otherwise, returns <see langword="false"/>.
    /// </returns>
    public static bool IsGraphQLSymbol(this SyntaxNode syntaxNode, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);

        return symbolInfo.Symbol?.IsGraphQLSymbol()
               ?? symbolInfo.CandidateSymbols
                   .All(symbol => symbol.IsGraphQLSymbol());
    }

    /// <summary>
    /// Checks if the given <see cref="ISymbol"/> represents a symbol defined by the GraphQL library.
    /// </summary>
    /// <param name="symbol">The <see cref="ISymbol"/> to check.</param>
    /// <returns><see langword="true"/> if the symbol is a GraphQL symbol; otherwise, <see langword="false"/>.</returns>
    public static bool IsGraphQLSymbol(this ISymbol symbol)
    {
        // GraphQL, GraphQL.MicrosoftDI...
        var assembly = symbol.ContainingAssembly;
        if (assembly == null)
        {
            return false;
        }

        if (!assembly.Identity.HasPublicKey)
        {
            return false;
        }

        return assembly.Identity.PublicKey.SequenceEqual(_publicKey);
    }

    /// <summary>
    /// Gets the return type symbol of the <see cref="ExpressionSyntax"/> which represents a method defined by
    /// the <c>FieldBuilder&lt;TSourceType,TReturnType&gt;</c> type.
    /// </summary>
    /// <param name="expression">
    /// The <see cref="ExpressionSyntax"/> representing the method defined on <c>FieldBuilder&lt;TSourceType,TReturnType&gt;</c> type.
    /// </param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for semantic analysis.</param>
    /// <returns>
    /// The <see cref="ISymbol"/> representing the return type of the method defined on the
    /// <c>FieldBuilder&lt;TSourceType,TReturnType&gt;</c> type, or <see langword="null"/> if
    /// <paramref name="expression"/> doesn't represent a method or the method is not defined by the
    /// <c>FieldBuilder&lt;TSourceType,TReturnType&gt;</c> method.
    /// </returns>
    public static ISymbol? GetFieldBuilderReturnTypeSymbol(
        this ExpressionSyntax expression,
        SemanticModel semanticModel)
    {
        var resolveMethodInfo = semanticModel.GetSymbolInfo(expression);
        if (resolveMethodInfo.Symbol is IMethodSymbol { ReturnType: INamedTypeSymbol { MetadataName: "FieldBuilder`2" } namedType })
        {
            return namedType.TypeArguments[1];
        }

        return null;
    }
}
