using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers;

public static class Extensions
{
    public static InvocationExpressionSyntax? FindMethodInvocationExpression(this ExpressionSyntax expression, string methodName)
    {
        var simpleNameSyntax = expression.FindSimpleNameSyntax(methodName);
        return simpleNameSyntax?.FindMethodInvocationExpression();
    }

    public static SimpleNameSyntax? FindSimpleNameSyntax(this ExpressionSyntax expression, string builderName)
    {
        return expression.DescendantNodes()
            .OfType<SimpleNameSyntax>()
            .FirstOrDefault(simpleNameSyntax => simpleNameSyntax.Identifier.Text == builderName);
    }

    public static InvocationExpressionSyntax? FindMethodInvocationExpression(this SimpleNameSyntax fieldSimpleName)
    {
        return fieldSimpleName.Parent switch
        {
            InvocationExpressionSyntax findFieldInvocation => findFieldInvocation,
            MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax findFieldInvocation } => findFieldInvocation,
            _ => null
        };
    }

    public static bool IsGraphQLSymbol(this ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
    {
        var symbolInfo = context.SemanticModel.GetSymbolInfo(expression);

        return symbolInfo.Symbol?.IsGraphQLSymbol()
               ?? symbolInfo.CandidateSymbols
                   .All(symbol => symbol.IsGraphQLSymbol());
    }

    public static bool IsGraphQLSymbol(this ISymbol symbol)
    {
        // GraphQL, GraphQL.MicrosoftDI...
        return symbol.ContainingAssembly.Name.StartsWith(Constants.GraphQL);
    }

    public static IMethodSymbol? GetMethodSymbol(this ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
    {
        return context.SemanticModel.GetSymbolInfo(expression).Symbol as IMethodSymbol;
    }

    public static ArgumentSyntax? GetMethodArgument(this InvocationExpressionSyntax invocation, string argumentName, SemanticModel semanticModel)
    {
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var namedArguments = GetNamedArguments(invocation);
        return GetArgument(argumentName, namedArguments, invocation, methodSymbol);
    }

    public static Dictionary<string, ArgumentSyntax> GetNamedArguments(InvocationExpressionSyntax invocation)
    {
        return invocation.ArgumentList.Arguments
            .Where(arg => arg.NameColon != null)
            .ToDictionary(arg => arg.NameColon!.Name.Identifier.Text);
    }

    public static bool GetBoolOption(this AnalyzerOptions analyzerOptions, string name, SyntaxTree tree, bool defaultValue = default)
    {
        var config = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);

        if (config.TryGetValue(name, out string? configValue))
        {
            if (bool.TryParse(configValue, out bool value))
            {
                return value;
            }
        }

        return defaultValue;
    }

    public static Location GetMethodInvocationLocation(this MemberAccessExpressionSyntax memberAccessExpressionSyntax)
    {
        var methodNameLocation = memberAccessExpressionSyntax.Name.GetLocation();
        var argsLocation = ((InvocationExpressionSyntax)memberAccessExpressionSyntax.Parent!).ArgumentList.GetLocation();
        return Location.Create(
            methodNameLocation.SourceTree!,
            TextSpan.FromBounds(methodNameLocation.SourceSpan.Start, argsLocation.SourceSpan.End));
    }

    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source) => new(source);

    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        => new(source, comparer);

    private static ArgumentSyntax? GetArgument(
        string argumentName,
        IDictionary<string, ArgumentSyntax> namedArguments,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol)
    {
        if (namedArguments.TryGetValue(argumentName, out var msgArg))
        {
            return msgArg;
        }

        int paramIndex = GetParamIndex(argumentName, methodSymbol);
        var argument = paramIndex != -1 && invocation.ArgumentList.Arguments.Count > paramIndex
            ? invocation.ArgumentList.Arguments[paramIndex]
            : null;

        // if requested argument is a named argument we should find it in 'namedArguments' dict
        // if we got here and found named argument - it's another argument placed an the requested
        // argument index, and requested argument has a default value (optional)
        return argument is { NameColon: null }
            ? argument
            : null;
    }

    private static int GetParamIndex(string argumentName, IMethodSymbol methodSymbol)
    {
        var param = methodSymbol.Parameters.SingleOrDefault(p => p.Name == argumentName);
        return param != null
            ? methodSymbol.Parameters.IndexOf(param)
            : -1;
    }
}
