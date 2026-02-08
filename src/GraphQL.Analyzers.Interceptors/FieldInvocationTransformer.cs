using GraphQL.Analyzers.Interceptors.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers.Interceptors;

/// <summary>
/// Transforms Field method invocations into FieldInterceptorInfo records.
/// </summary>
internal static class FieldInvocationTransformer
{
    /// <summary>
    /// Transforms a single Field method invocation into a FieldInterceptorInfo record.
    /// </summary>
    public static FieldInterceptorInfo? Transform(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return null;

        // Check if this is a Field method on ComplexGraphType<TSourceType>
        if (methodSymbol.Name != "Field")
            return null;

        var containingType = methodSymbol.ContainingType;
        if (containingType == null)
            return null;

        // Check if containing type is ComplexGraphType<TSourceType>
        if (!IsComplexGraphType(containingType))
            return null;

        // Check if method has the signature we're looking for:
        // Field<TProperty>(string name, Expression<Func<TSourceType, TProperty>> expression, ...)
        // OR Field<TProperty>(Expression<Func<TSourceType, TProperty>> expression, ...)
        if (methodSymbol.TypeArguments.Length != 1)
            return null;

        if (methodSymbol.Parameters.Length < 1)
            return null;

        // Check if any parameter is an Expression<Func<TSourceType, TProperty>>
        IParameterSymbol? expressionParam = null;
        foreach (var param in methodSymbol.Parameters)
        {
            if (IsExpressionFuncType(param.Type))
            {
                expressionParam = param;
                break;
            }
        }

        if (expressionParam == null)
            return null;

        // Get the interceptable location
        var interceptableLocation = semanticModel.GetInterceptableLocation(invocation);
        if (interceptableLocation == null)
            return null;

        // Extract type information
        var sourceType = containingType.TypeArguments[0];
        var propertyType = methodSymbol.TypeArguments[0];

        // Find the expression argument - look for a lambda expression
        LambdaExpressionSyntax? lambdaExpression = null;
        foreach (var arg in invocation.ArgumentList.Arguments)
        {
            if (arg.Expression is LambdaExpressionSyntax lambda)
            {
                lambdaExpression = lambda;
                break;
            }
        }

        if (lambdaExpression == null)
            return null;

        // Validate that the lambda body is a simple member access (property or field)
        var (isValid, memberName, diagnostic) = ValidateAndExtractMemberAccess(lambdaExpression, invocation.GetLocation());

        if (!isValid && diagnostic != null)
        {
            return new FieldInterceptorInfo
            {
                Location = interceptableLocation,
                SourceTypeFullName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                PropertyTypeFullName = propertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Diagnostic = diagnostic
            };
        }

        return new FieldInterceptorInfo
        {
            Location = interceptableLocation,
            SourceTypeFullName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            PropertyTypeFullName = propertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MemberName = memberName
        };
    }

    private static (bool IsValid, string? MemberName, DiagnosticInfo? Diagnostic) ValidateAndExtractMemberAccess(
        LambdaExpressionSyntax lambda,
        Location location)
    {
        // The body should be a simple member access: x.PropertyName
        if (lambda.Body is not MemberAccessExpressionSyntax memberAccess)
        {
            return (false, null, new DiagnosticInfo
            {
                Id = "GQL_INT002",
                Title = "Complex Field Expression",
                MessageFormat = "Field expression must be a simple property or field access (e.g., x => x.PropertyName). Complex expressions are not supported for AOT compilation.",
                Category = "GraphQL.Interceptors",
                Severity = DiagnosticSeverity.Warning,
                LocationString = location.ToString()
            });
        }

        // Extract the member name
        var memberName = memberAccess.Name.Identifier.Text;

        return (true, memberName, null);
    }

    private static bool IsComplexGraphType(INamedTypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            if (current.Name == "ComplexGraphType" &&
                current.ContainingNamespace?.ToDisplayString() == "GraphQL.Types" &&
                current.TypeArguments.Length == 1)
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    private static bool IsExpressionFuncType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
            return false;

        if (namedType.Name != "Expression")
            return false;

        if (namedType.ContainingNamespace?.ToDisplayString() != "System.Linq.Expressions")
            return false;

        if (namedType.TypeArguments.Length != 1)
            return false;

        var funcType = namedType.TypeArguments[0];
        if (funcType is not INamedTypeSymbol funcNamedType)
            return false;

        return funcNamedType.Name == "Func" && funcNamedType.TypeArguments.Length == 2;
    }
}
