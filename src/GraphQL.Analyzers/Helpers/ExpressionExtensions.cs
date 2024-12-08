using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Helpers;

public static class ExpressionExtensions
{
    /// <summary>
    /// Finds the <see cref="InvocationExpressionSyntax"/> for a method with the specified name
    /// within the given <see cref="ExpressionSyntax"/>.
    /// </summary>
    /// <param name="expression">The <see cref="ExpressionSyntax"/> to search within.</param>
    /// <param name="methodName">The name of the method to find.</param>
    /// <returns>The <see cref="InvocationExpressionSyntax"/> if found; otherwise, <see langword="null" />.</returns>
    public static InvocationExpressionSyntax? FindMethodInvocationExpression(this ExpressionSyntax expression, string methodName)
    {
        var simpleNameSyntax = expression.FindSimpleNameSyntax(methodName);
        return simpleNameSyntax?.FindMethodInvocationExpression();
    }

    /// <summary>
    /// Finds the <see cref="SimpleNameSyntax"/> for a simple name with the specified name
    /// within the given <see cref="ExpressionSyntax"/>.
    /// </summary>
    /// <param name="expression">The <see cref="ExpressionSyntax"/> to search within.</param>
    /// <param name="name">The name of the simple name to find.</param>
    /// <returns>The <see cref="SimpleNameSyntax"/> if found; otherwise, <see langword="null"/>.</returns>
    public static SimpleNameSyntax? FindSimpleNameSyntax(this ExpressionSyntax expression, string name) =>
        expression.FindNameSyntax<SimpleNameSyntax>(name);

    /// <summary>
    /// Finds the <see cref="GenericNameSyntax"/> for a generic name with the specified name
    /// within the given <see cref="ExpressionSyntax"/>.
    /// </summary>
    /// <param name="expression">The <see cref="ExpressionSyntax"/> to search within.</param>
    /// <param name="name">The name of the generic name to find.</param>
    /// <returns>The <see cref="GenericNameSyntax"/> if found; otherwise, <see langword="null"/>.</returns>
    public static GenericNameSyntax? FindGenericNameSyntax(this ExpressionSyntax expression, string name) =>
        expression.FindNameSyntax<GenericNameSyntax>(name);

    /// <summary>
    /// Finds the specified type of name syntax with the given name within the ExpressionSyntax.
    /// </summary>
    /// <typeparam name="TNameSyntax">The type of name syntax to find.</typeparam>
    /// <param name="expression">The <see cref="ExpressionSyntax"/> to search within.</param>
    /// <param name="name">The name to find.</param>
    /// <returns>The found name syntax if present; otherwise, <see langword="null"/>.</returns>
    public static TNameSyntax? FindNameSyntax<TNameSyntax>(this ExpressionSyntax expression, string name)
        where TNameSyntax : SimpleNameSyntax =>
        expression.DescendantNodes()
            .OfType<TNameSyntax>()
            .FirstOrDefault(simpleNameSyntax => simpleNameSyntax.Identifier.Text == name);

    /// <summary>
    /// Finds the <see cref="InvocationExpressionSyntax"/> for a method based on a <see cref="SimpleNameSyntax"/>.
    /// </summary>
    /// <param name="methodSimpleName">The <see cref="SimpleNameSyntax"/> representing the method name.</param>
    /// <returns>The <see cref="InvocationExpressionSyntax"/> if found; otherwise, <see langword="null"/>.</returns>
    public static InvocationExpressionSyntax? FindMethodInvocationExpression(this SimpleNameSyntax methodSimpleName) =>
        methodSimpleName.Parent switch
        {
            InvocationExpressionSyntax invocation => invocation,
            MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } => invocation,
            _ => null
        };

    /// <summary>
    /// Gets the <see cref="IMethodSymbol"/> from the given <see cref="ExpressionSyntax"/>
    /// within the specified <see cref="SyntaxNodeAnalysisContext"/>.
    /// </summary>
    /// <param name="expression">The <see cref="ExpressionSyntax"/> representing a method invocation.</param>
    /// <param name="context">The <see cref="SyntaxNodeAnalysisContext"/> for semantic analysis.</param>
    /// <returns>The <see cref="IMethodSymbol"/> if found; otherwise, <see langword="null"/>.</returns>
    public static IMethodSymbol? GetMethodSymbol(this ExpressionSyntax expression, SyntaxNodeAnalysisContext context) =>
        context.SemanticModel.GetSymbolInfo(expression).Symbol as IMethodSymbol;

    /// <summary>
    /// Gets the method argument from a method invocation with the specified argument name.
    /// </summary>
    /// <param name="invocation">The <see cref="InvocationExpressionSyntax"/> representing the method invocation.</param>
    /// <param name="argumentName">The name of the argument to retrieve.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for semantic analysis.</param>
    /// <returns>The <see cref="ArgumentSyntax"/> if found; otherwise, <see langword="null"/>.</returns>
    public static ArgumentSyntax? GetMethodArgument(this InvocationExpressionSyntax invocation, string argumentName, SemanticModel semanticModel)
    {
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var namedArguments = invocation.GetNamedArguments();
        return GetArgument(argumentName, namedArguments, invocation, methodSymbol);
    }

    /// <summary>
    /// Gets a dictionary of named arguments from the given <see cref="InvocationExpressionSyntax"/>.
    /// </summary>
    /// <param name="invocation">The <see cref="InvocationExpressionSyntax"/> to extract named arguments from.</param>
    /// <returns>A dictionary of named arguments.</returns>
    public static Dictionary<string, ArgumentSyntax> GetNamedArguments(this InvocationExpressionSyntax invocation) =>
        invocation.ArgumentList.Arguments
            .Where(arg => arg.NameColon != null)
            .ToDictionary(arg => arg.NameColon!.Name.Identifier.Text);

    /// <summary>
    /// Gets the attribute arguments expressions.
    /// </summary>
    /// <param name="attributeSyntax">The <see cref="AttributeSyntax"/> to analyze.</param>
    /// <param name="semanticModel">The <see cref="SemanticModel"/> for semantic analysis.</param>
    /// <returns>Dictionary with an argument name as a key and argument expression as a value.</returns>
    public static Dictionary<string, ExpressionSyntax>? GetAttributeArguments(this AttributeSyntax attributeSyntax, SemanticModel semanticModel)
    {
        if (attributeSyntax.ArgumentList is null)
            return null;

        IMethodSymbol? constructor = null;
        var arguments = new Dictionary<string, ExpressionSyntax>(attributeSyntax.ArgumentList.Arguments.Count);
        for (int i = 0; i < attributeSyntax.ArgumentList.Arguments.Count; i++)
        {
            var argument = attributeSyntax.ArgumentList.Arguments[i];
            if (argument.NameColon != null)
            {
                arguments[argument.NameColon.Name.ToString()] = argument.Expression;
            }
            else
            {
                constructor ??= semanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                if (constructor == null)
                {
                    // if we couldn't find a constructor, this is likely because there is a compilation error
                    // for example int value passed into string parameter
                    return null;
                }

                arguments[constructor.Parameters[i].Name] = argument.Expression;
            }
        }

        return arguments;
    }

    /// <summary>
    /// Gets the location of a method invocation including the method name and arguments.
    /// </summary>
    /// <param name="memberAccessExpressionSyntax">The <see cref="MemberAccessExpressionSyntax"/> representing the method invocation.</param>
    /// <returns>The <see cref="Location"/> of the method invocation.</returns>
    public static Location GetMethodInvocationLocation(this MemberAccessExpressionSyntax memberAccessExpressionSyntax)
    {
        var methodNameLocation = memberAccessExpressionSyntax.Name.GetLocation();
        var argsLocation = ((InvocationExpressionSyntax)memberAccessExpressionSyntax.Parent!).ArgumentList.GetLocation();
        return Location.Create(
            methodNameLocation.SourceTree!,
            TextSpan.FromBounds(methodNameLocation.SourceSpan.Start, argsLocation.SourceSpan.End));
    }


    /// <summary>
    /// If the <paramref name="symbol"/> is a method symbol, returns <see langword="true"/> if the method's return type is "awaitable",
    /// but not if it's <see langword="dynamic"/>.
    /// If the <paramref name="symbol"/> is a type symbol, returns <see langword="true"/> if that type is "awaitable".
    /// An "awaitable" is any type that exposes a GetAwaiter method which returns a valid "awaiter".
    /// This GetAwaiter method may be an instance method or an extension method.
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
        if (!returnType.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(p => p.Name == WellKnownMemberNames.IsCompleted &&
                          p.Type.SpecialType == SpecialType.System_Boolean &&
                          p.GetMethod != null))
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
        if (!methods.Any(x => x.Name == WellKnownMemberNames.OnCompleted &&
                              x is { ReturnsVoid: true, Parameters: [{ Type.TypeKind: TypeKind.Delegate }] }))
        {
            return false;
        }

        // void GetResult() || T GetResult()
        return methods.Any(m => m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any());
    }

    private static ArgumentSyntax? GetArgument(
        string argumentName,
        Dictionary<string, ArgumentSyntax> namedArguments,
        InvocationExpressionSyntax invocation,
        IMethodSymbol methodSymbol)
    {
        if (namedArguments.TryGetValue(argumentName, out var arg))
            return arg;

        int paramIndex = GetParamIndex(argumentName, methodSymbol);
        var argument = paramIndex != -1 && invocation.ArgumentList.Arguments.Count > paramIndex
            ? invocation.ArgumentList.Arguments[paramIndex]
            : null;

        // if requested argument is a named argument we should find it in 'namedArguments' dict
        // if we got here and found named argument - it's another argument placed at the requested
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
