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

    public static SimpleNameSyntax? FindSimpleNameSyntax(this ExpressionSyntax expression, string name)
    {
        return expression.FindNameSyntax<SimpleNameSyntax>(name);
    }

    public static GenericNameSyntax? FindGenericNameSyntax(this ExpressionSyntax expression, string name)
    {
        return expression.FindNameSyntax<GenericNameSyntax>(name);
    }

    public static TNameSyntax? FindNameSyntax<TNameSyntax>(this ExpressionSyntax expression, string name)
        where TNameSyntax : SimpleNameSyntax
    {
        return expression.DescendantNodes()
            .OfType<TNameSyntax>()
            .FirstOrDefault(simpleNameSyntax => simpleNameSyntax.Identifier.Text == name);
    }

    public static InvocationExpressionSyntax? FindMethodInvocationExpression(this SimpleNameSyntax methodSimpleName)
    {
        return methodSimpleName.Parent switch
        {
            InvocationExpressionSyntax invocation => invocation,
            MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } => invocation,
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

    /// <summary>
    /// If the <paramref name="symbol"/> is a method symbol, returns <see langword="true"/> if the method's return type is "awaitable", but not if it's <see langword="dynamic"/>.
    /// If the <paramref name="symbol"/> is a type symbol, returns <see langword="true"/> if that type is "awaitable".
    /// An "awaitable" is any type that exposes a GetAwaiter method which returns a valid "awaiter". This GetAwaiter method may be an instance method or an extension method.
    /// </summary>
    /// https://github.com/dotnet/roslyn/blob/342787a7e6906de95080654e28e8db5dd9116b9a/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ISymbolExtensions.cs#L577
    public static bool IsAwaitableNonDynamic([NotNullWhen(true)] this ISymbol? symbol, SemanticModel semanticModel, int position)
    {
        var methodSymbol = symbol as IMethodSymbol;
        ITypeSymbol? typeSymbol = null;

        if (methodSymbol == null)
        {
            typeSymbol = symbol as ITypeSymbol;
            if (typeSymbol == null)
            {
                return false;
            }
        }
        else
        {
            if (methodSymbol.ReturnType == null)
            {
                return false;
            }
        }

        // otherwise: needs valid GetAwaiter
        var potentialGetAwaiters = semanticModel.LookupSymbols(
            position,
            container: typeSymbol ?? methodSymbol!.ReturnType.OriginalDefinition,
            name: WellKnownMemberNames.GetAwaiter,
            includeReducedExtensionMethods: true);

        var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where(x => !x.Parameters.Any());
        return getAwaiters.Any(VerifyGetAwaiter);
    }

    public static bool IsValidGetAwaiter(this IMethodSymbol symbol)
        => symbol.Name == WellKnownMemberNames.GetAwaiter &&
        VerifyGetAwaiter(symbol);

    private static bool VerifyGetAwaiter(IMethodSymbol getAwaiter)
    {
        var returnType = getAwaiter.ReturnType;
        if (returnType == null)
        {
            return false;
        }

        // bool IsCompleted { get }
        if (!returnType.GetMembers().OfType<IPropertySymbol>().Any(p => p.Name == WellKnownMemberNames.IsCompleted && p.Type.SpecialType == SpecialType.System_Boolean && p.GetMethod != null))
        {
            return false;
        }

        var methods = returnType.GetMembers().OfType<IMethodSymbol>();

        // NOTE: (vladres) The current version of C# Spec, §7.7.7.3 'Runtime evaluation of await expressions', requires that
        // NOTE: the interface method INotifyCompletion.OnCompleted or ICriticalNotifyCompletion.UnsafeOnCompleted is invoked
        // NOTE: (rather than any OnCompleted method conforming to a certain pattern).
        // NOTE: Should this code be updated to match the spec?

        // void OnCompleted(Action) 
        // Actions are delegates, so we'll just check for delegates.
        if (!methods.Any(x => x.Name == WellKnownMemberNames.OnCompleted && x.ReturnsVoid && x.Parameters is [{ Type.TypeKind: TypeKind.Delegate }]))
            return false;

        // void GetResult() || T GetResult()
        return methods.Any(m => m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any());
    }

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
