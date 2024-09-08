using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ValidatorAttributeAnalyzer : ParserValidatorAttributeAnalyzer
{
    public ValidatorAttributeAnalyzer()
        : base(ValidatorMethodMustBeValid)
    {
    }

    public static readonly DiagnosticDescriptor ValidatorMethodMustBeValid = new(
        id: DiagnosticIds.VALIDATOR_METHOD_MUST_BE_VALID,
        title: "Validator method must be valid",
        messageFormat: "Validator method '{0}' signature must be '{1}static object {0}(object value)'",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.VALIDATOR_METHOD_MUST_BE_VALID);

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
            is not Constants.AttributeNames.Validator
            and not Constants.AttributeNames.Validator + Constants.AttributeNames.Attribute)
        {
            return;
        }

        if (!attribute.IsGraphQLSymbol(context.SemanticModel))
            return;

        // validatorType or/and validatorMethodName
        if (attribute.ArgumentList?.Arguments.Count is < 1 or > 2)
            return;

        var arguments = attribute.GetAttributeArguments(context.SemanticModel);
        if (arguments == null)
            return;

        if (!TryGetType(
                attribute,
                arguments,
                argumentName: "validatorType",
                context.SemanticModel,
                out var validatorType,
                out bool allowNonPublicMethods))
        {
            return;
        }

        if (!TryGetMethodName(
               arguments,
               context.SemanticModel,
               argumentName: "validatorMethodName",
               defaultMethodName: "Validate",
               out string? validatorMethodName))
        {
            return;
        }

        bool hasValidMethod = false;
        bool hasValidatorMethods = false;
        foreach (var method in validatorType.GetMembers(validatorMethodName).OfType<IMethodSymbol>())
        {
            hasValidatorMethods = true;
            if (!method.IsStatic)
                continue;

            if (method.Parameters.Length != 1 || method.Parameters[0].Type.SpecialType != SpecialType.System_Object)
                continue;

            if (method.ReturnType.SpecialType != SpecialType.System_Object)
                continue;

            if (!allowNonPublicMethods && method.DeclaredAccessibility != Accessibility.Public)
                continue;

            hasValidMethod = true;
            break;
        }

        if (hasValidMethod)
            return;

        if (!hasValidatorMethods)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    CouldNotFindMethod,
                    attribute.Parent!.GetLocation(),
                    validatorMethodName,
                    validatorType.Name));
        }
        else
        {
            string accessor = allowNonPublicMethods ? string.Empty : "public ";
            context.ReportDiagnostic(
                Diagnostic.Create(
                    ValidatorMethodMustBeValid,
                    attribute.Parent!.GetLocation(),
                    validatorMethodName,
                    accessor));
        }
    }
}
