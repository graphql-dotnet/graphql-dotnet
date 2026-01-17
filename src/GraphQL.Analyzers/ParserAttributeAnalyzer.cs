using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ParserAttributeAnalyzer : ParserValidatorAttributeAnalyzer
{
    public ParserAttributeAnalyzer()
        : base(ParserMethodMustBeValid)
    {
    }

    public static readonly DiagnosticDescriptor ParserMethodMustBeValid = new(
        id: DiagnosticIds.PARSER_METHOD_MUST_BE_VALID,
        title: "Parser method must be valid",
        messageFormat: "Parser method '{0}' signature must be '{1}static object {0}(object value)' or '{1}static object {0}(object value, IValueConverter converter)'",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.PARSER_METHOD_MUST_BE_VALID);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    private void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;
        if (attribute.Name.ToString()
            is not Constants.AttributeNames.Parser
            and not Constants.AttributeNames.Parser + Constants.AttributeNames.Attribute)
        {
            return;
        }

        if (!attribute.IsGraphQLSymbol(context.SemanticModel))
            return;

        // parserType or/and parserMethodName
        if (attribute.ArgumentList?.Arguments.Count is < 1 or > 2)
            return;

        var arguments = attribute.GetAttributeArguments(context.SemanticModel);
        if (arguments == null)
            return;

        if (!TryGetType(
                attribute,
                arguments,
                argumentName: "parserType",
                context.SemanticModel,
                out var parserType,
                out bool allowNonPublicMethods))
        {
            return;
        }

        if (!TryGetMethodName(
               arguments,
               context.SemanticModel,
               argumentName: "parserMethodName",
               defaultMethodName: "Parse",
               out string? parserMethodName))
        {
            return;
        }

        bool hasValidMethod = false;
        bool hasParserMethods = false;
        foreach (var method in parserType.GetMembers(parserMethodName).OfType<IMethodSymbol>())
        {
            hasParserMethods = true;
            if (!method.IsStatic)
                continue;

            // Check return type
            if (method.ReturnType.SpecialType != SpecialType.System_Object)
                continue;

            // Check accessibility
            if (!allowNonPublicMethods && method.DeclaredAccessibility != Accessibility.Public)
                continue;

            // Valid signatures:
            // 1. static object MethodName(object value)
            // 2. static object MethodName(object value, IValueConverter converter)
            if (method.Parameters.Length == 1)
            {
                // Check for Func<object, object> signature
                if (method.Parameters[0].Type.SpecialType == SpecialType.System_Object)
                {
                    hasValidMethod = true;
                    break;
                }
            }
            else if (method.Parameters.Length == 2)
            {
                // Check for Func<object, IValueConverter, object> signature
                if (method.Parameters[0].Type.SpecialType == SpecialType.System_Object &&
                    IsIValueConverter(method.Parameters[1].Type))
                {
                    hasValidMethod = true;
                    break;
                }
            }
        }

        if (hasValidMethod)
            return;

        if (!hasParserMethods)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    CouldNotFindMethod,
                    attribute.Parent!.GetLocation(),
                    parserMethodName,
                    parserType.Name));
        }
        else
        {
            string accessor = allowNonPublicMethods ? string.Empty : "public ";
            context.ReportDiagnostic(
                Diagnostic.Create(
                    ParserMethodMustBeValid,
                    attribute.Parent!.GetLocation(),
                    parserMethodName,
                    accessor));
        }
    }

    private static bool IsIValueConverter(ITypeSymbol type)
    {
        // Check if the type is GraphQL.IValueConverter
        return type.ToDisplayString() == "GraphQL.IValueConverter";
    }
}
