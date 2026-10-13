using GraphQL.Analyzers.Interceptors.Models;
using GraphQL.Analyzers.SourceGenerators.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Interceptors;

/// <summary>
/// Transforms FieldBuilder.ResolveDelegate method invocations into ResolveDelegateInterceptorInfo records.
/// </summary>
internal static class ResolveDelegateInvocationTransformer
{
    /// <summary>
    /// Transforms a single ResolveDelegate method invocation into a ResolveDelegateInterceptorInfo record.
    /// Returns null if the invocation cannot be intercepted (e.g. not a simple method group or unsupported pattern).
    /// </summary>
    public static ResolveDelegateInterceptorInfo? Transform(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return null;

        // Must be named "ResolveDelegate"
        if (methodSymbol.Name != "ResolveDelegate")
            return null;

        // Must be on FieldBuilder<TSourceType, TReturnType>
        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return null;

        if (!IsFieldBuilderType(containingType))
            return null;

        // Must have exactly 2 type arguments (TSourceType, TReturnType)
        if (containingType.TypeArguments.Length != 2)
            return null;

        // Get the interceptable location
        var interceptableLocation = semanticModel.GetInterceptableLocation(invocation);
        if (interceptableLocation == null)
            return null;

        var sourceType = containingType.TypeArguments[0];
        var returnType = containingType.TypeArguments[1];

        // Check the argument - must be a single argument
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count != 1)
            return null;

        var argExpression = arguments[0].Expression;

        // Handle null literal: ResolveDelegate(null)
        if (argExpression is LiteralExpressionSyntax literal &&
            literal.Token.Text == "null")
        {
            return new ResolveDelegateInterceptorInfo
            {
                Location = new(interceptableLocation),
                SourceTypeFullName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                ReturnTypeFullName = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                DeclaringTypeFullName = string.Empty,
                MethodName = string.Empty,
                Parameters = ImmutableEquatableArray<DelegateParameterInfo>.Empty,
                MethodParameterTypeNames = ImmutableEquatableArray<string>.Empty,
                IsStatic = false,
                IsNullDelegate = true
            };
        }

        // Resolve the delegate argument to a method symbol
        // Supports: method groups (e.g. MyMethod, obj.MyMethod, TypeName.StaticMethod)
        var delegateMethodSymbol = ResolveDelegateMethodSymbol(argExpression, semanticModel);
        if (delegateMethodSymbol == null)
            return null;

        var declaringType = delegateMethodSymbol.ContainingType;
        if (declaringType == null)
            return null;

        // Only public methods can be called from generated interceptor code (AOT-compatible)
        if (delegateMethodSymbol.DeclaredAccessibility != Accessibility.Public)
            return null;

        // The declaring type and all its containing types must also be public
        if (!IsTypePubliclyAccessible(declaringType))
            return null;

        // Build parameter info list
        var parameters = new List<DelegateParameterInfo>();
        var methodParamTypeNames = new List<string>();

        foreach (var param in delegateMethodSymbol.Parameters)
        {
            var paramTypeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            methodParamTypeNames.Add(paramTypeName);

            var isContextParam = IsContextParameter(param.Type);

            parameters.Add(new DelegateParameterInfo
            {
                FullyQualifiedTypeName = paramTypeName,
                ParameterName = param.Name,
                IsContextParameter = isContextParam
            });
        }

        return new ResolveDelegateInterceptorInfo
        {
            Location = new(interceptableLocation),
            SourceTypeFullName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ReturnTypeFullName = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            DeclaringTypeFullName = declaringType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodName = delegateMethodSymbol.Name,
            Parameters = parameters.ToImmutableEquatableArray(),
            MethodParameterTypeNames = methodParamTypeNames.ToImmutableEquatableArray(),
            IsStatic = delegateMethodSymbol.IsStatic,
            IsNullDelegate = false
        };
    }

    /// <summary>
    /// Attempts to resolve the method symbol from a delegate argument expression.
    /// Supports method groups (simple identifier or member access).
    /// </summary>
    private static IMethodSymbol? ResolveDelegateMethodSymbol(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        // Method group: just an identifier like "MyMethod"
        if (expression is IdentifierNameSyntax)
        {
            var info = semanticModel.GetSymbolInfo(expression);
            return ResolveMethodFromSymbolInfo(info);
        }

        // Method group: member access like "obj.MyMethod" or "TypeName.StaticMethod"
        if (expression is MemberAccessExpressionSyntax)
        {
            var info = semanticModel.GetSymbolInfo(expression);
            return ResolveMethodFromSymbolInfo(info);
        }

        return null;
    }

    private static IMethodSymbol? ResolveMethodFromSymbolInfo(SymbolInfo info)
    {
        if (info.Symbol is IMethodSymbol method)
            return method;

        // For method groups with overloads, CandidateSymbols may be populated
        // We pick the first candidate if there's only one viable option
        if (info.CandidateSymbols.Length == 1 && info.CandidateSymbols[0] is IMethodSymbol candidate)
            return candidate;

        return null;
    }

    /// <summary>
    /// Determines whether a parameter type is a "context" parameter that is resolved
    /// from the field context rather than from a GraphQL argument.
    /// These include IResolveFieldContext and CancellationToken.
    /// </summary>
    private static bool IsContextParameter(ITypeSymbol type)
    {
        var displayName = type.ToDisplayString();
        return displayName == "GraphQL.IResolveFieldContext" ||
               displayName == "System.Threading.CancellationToken" ||
               type.Name == "IResolveFieldContext";
    }

    /// <summary>
    /// Determines whether a type and all its containing types are accessible from generated interceptor code.
    /// Since the generated code is emitted into the same compilation, internal types are accessible.
    /// Only private and protected types (nested types with restricted access) are inaccessible.
    /// </summary>
    private static bool IsTypePubliclyAccessible(INamedTypeSymbol type)
    {
        INamedTypeSymbol? current = type;
        while (current != null)
        {
            if (current.DeclaredAccessibility == Accessibility.Private ||
                current.DeclaredAccessibility == Accessibility.Protected)
                return false;
            current = current.ContainingType;
        }
        return true;
    }

    private static bool IsFieldBuilderType(INamedTypeSymbol type)
    {
        return type.Name == "FieldBuilder" &&
               type.ContainingNamespace?.ToDisplayString() == "GraphQL.Builders" &&
               type.TypeArguments.Length == 2;
    }
}
