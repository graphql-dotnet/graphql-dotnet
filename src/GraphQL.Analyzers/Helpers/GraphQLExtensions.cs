using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Helpers;

public static class GraphQLExtensions
{
    /// <summary>
    /// Checks if the given <see cref="ExpressionSyntax"/> represents a symbol defined by the GraphQL library.
    /// </summary>
    /// <param name="expression">The <see cref="ExpressionSyntax"/> to check.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for semantic analysis.</param>
    /// <returns>
    /// <see langword="true"/> if the symbol represented by the <paramref name="expression"/>
    /// is defined by the GraphQL library; otherwise, returns <see langword="false"/>.
    /// If the given expression doesn't represent a symbol, the method returns <see langword="false"/>.
    /// </returns>
    public static bool IsGraphQLSymbol(this ExpressionSyntax expression, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(expression);

        return symbolInfo.Symbol?.IsGraphQLSymbol()
               ?? symbolInfo.CandidateSymbols
                   .All(symbol => symbol.IsGraphQLSymbol());
    }

    /// <summary>
    /// Checks if the given <see cref="ISymbol"/> represents a symbol defined by the GraphQL library.
    /// </summary>
    /// <param name="symbol">The <see cref="ISymbol"/> to check.</param>
    /// <returns><see langword="true"/> if the symbol is a GraphQL symbol; otherwise, <see langword="false"/>.</returns>
    public static bool IsGraphQLSymbol(this ISymbol symbol) =>
        // GraphQL, GraphQL.MicrosoftDI...
        symbol.ContainingAssembly?.Name.StartsWith(Constants.GraphQL) == true;

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
