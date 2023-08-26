using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers;

public static class Extensions
{
    public static InvocationExpressionSyntax FindFieldInvocationExpression(this ExpressionSyntax expression)
    {
        var fieldNameSyntax = expression.FindFieldSimpleNameSyntax();
        return fieldNameSyntax.FindFieldInvocationExpression();
    }

    public static SimpleNameSyntax FindFieldSimpleNameSyntax(this ExpressionSyntax expression)
    {
        while (true)
        {
            if (expression is SimpleNameSyntax { Identifier.Text: Constants.MethodNames.Field } simpleNameSyntax)
            {
                return simpleNameSyntax;
            }

            switch (expression)
            {
                case InvocationExpressionSyntax invocationExpression:
                    expression = invocationExpression.Expression;
                    continue;

                case MemberAccessExpressionSyntax memberAccessExpression:
                    var findFieldSimpleNameSyntax = FindFieldSimpleNameSyntax(memberAccessExpression.Expression);
                    if (findFieldSimpleNameSyntax != null)
                    {
                        return findFieldSimpleNameSyntax;
                    }

                    expression = memberAccessExpression.Name;
                    continue;

                default:
                    return null;
            }
        }
    }

    public static InvocationExpressionSyntax FindFieldInvocationExpression(this SimpleNameSyntax fieldSimpleName)
    {
        if (fieldSimpleName == null)
        {
            return null;
        }

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

    public static IMethodSymbol GetMethodSymbol(this ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
    {
        return context.SemanticModel.GetSymbolInfo(expression).Symbol as IMethodSymbol;
    }

    public static ArgumentSyntax GetArgument(this InvocationExpressionSyntax invocation, string argumentName, SemanticModel semanticModel)
    {
        var methodSymbol = (IMethodSymbol)semanticModel.GetSymbolInfo(invocation).Symbol;
        var namedArguments = GetNamedArguments(invocation);
        return GetArgument(argumentName, namedArguments, invocation, methodSymbol);
    }

    public static Dictionary<string, ArgumentSyntax> GetNamedArguments(InvocationExpressionSyntax invocation)
    {
        return invocation!.ArgumentList.Arguments
            .Where(arg => arg.NameColon != null)
            .ToDictionary(arg => arg.NameColon.Name.Identifier.Text);
    }

    public static bool GetBoolOption(this AnalyzerOptions analyzerOptions, string name, SyntaxTree tree, bool defaultValue = default)
    {
        var config = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);

        if (config.TryGetValue(name, out var configValue))
        {
            if (bool.TryParse(configValue, out var value))
            {
                return value;
            }
        }

        return defaultValue;
    }

    public static Location GetMethodInvocationLocation(this MemberAccessExpressionSyntax memberAccessExpressionSyntax)
    {
        var methodNameLocation = memberAccessExpressionSyntax.Name.GetLocation();
        var argsLocation = ((InvocationExpressionSyntax)memberAccessExpressionSyntax.Parent)!.ArgumentList.GetLocation();
        return Location.Create(
            methodNameLocation.SourceTree!,
            TextSpan.FromBounds(methodNameLocation.SourceSpan.Start, argsLocation.SourceSpan.End));
    }

    private static ArgumentSyntax GetArgument(
        string argumentName,
        IDictionary<string, ArgumentSyntax> namedArguments,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol)
    {
        if (namedArguments.TryGetValue(argumentName, out var msgArg))
        {
            return msgArg;
        }

        var paramIndex = GetParamIndex(argumentName, methodSymbol);
        return paramIndex != -1
            ? invocation.ArgumentList.Arguments[paramIndex]
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
