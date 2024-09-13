using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

public abstract class ParserValidatorAttributeAnalyzer : DiagnosticAnalyzer
{
    protected ParserValidatorAttributeAnalyzer(params DiagnosticDescriptor[] descriptors)
    {
        SupportedDiagnostics = ImmutableArray.Create(CouldNotFindMethod).AddRange(descriptors);
    }

    public static readonly DiagnosticDescriptor CouldNotFindMethod = new(
        id: DiagnosticIds.COULD_NOT_FIND_METHOD,
        title: "Could not find method",
        messageFormat: "Couldn't find method '{0}' on type '{1}'",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.COULD_NOT_FIND_METHOD);

    public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    protected static bool TryGetType(
        AttributeSyntax attribute,
        Dictionary<string, ExpressionSyntax> arguments,
        string argumentName,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out ITypeSymbol? type,
        out bool allowNonPublicMethods)
    {
        allowNonPublicMethods = false;
        type = null;

        var declaringClass = attribute.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (declaringClass == null)
            return false;

        var declaringType = semanticModel.GetDeclaredSymbol(declaringClass);
        if (declaringType == null)
            return false;

        if (arguments.TryGetValue(argumentName, out var typeExpression))
        {
            if (typeExpression is not TypeOfExpressionSyntax typeOf)
                return false;

            type = semanticModel.GetTypeInfo(typeOf.Type).Type;
            allowNonPublicMethods = SymbolEqualityComparer.Default.Equals(type, declaringType);
            return type != null;
        }

        allowNonPublicMethods = true;
        type = declaringType;
        return true;
    }

    protected static bool TryGetMethodName(
        Dictionary<string, ExpressionSyntax> arguments,
        SemanticModel semanticModel,
        string argumentName,
        string defaultMethodName,
        [NotNullWhen(true)] out string? methodName)
    {
        if (!arguments.TryGetValue(argumentName, out var methodNameExpression))
        {
            methodName = defaultMethodName;
            return true;
        }

        // "MethodName"
        if (methodNameExpression is LiteralExpressionSyntax literal)
        {
            methodName = literal.Token.ValueText;
            return true;
        }

        // nameof(MethodName) or nameof(ClassName.MethodName)
        if (methodNameExpression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.Text: "nameof" } } invocation)
        {
            methodName = invocation.ArgumentList.Arguments[0].Expression
                .DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .LastOrDefault()
                ?.Identifier
                .ValueText;

            return methodName != null;
        }

        // const: MethodName or Class.MethodName
        if (methodNameExpression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
        {
            var constSymbolInfo = semanticModel.GetSymbolInfo(methodNameExpression);
            if (constSymbolInfo.Symbol is IFieldSymbol { ConstantValue: string constant })
            {
                methodName = constant;
                return true;
            }
        }

        methodName = null;
        return false;
    }
}
