using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ValidateArgumentsAttributeAnalyzer : ParserValidatorAttributeAnalyzer
{
    public ValidateArgumentsAttributeAnalyzer()
        : base(ValidateArgumentsMethodMustBeValid)
    {
    }

    public static readonly DiagnosticDescriptor ValidateArgumentsMethodMustBeValid = new(
        id: DiagnosticIds.VALIDATE_ARGUMENTS_METHOD_MUST_BE_VALID,
        title: "ValidateArguments method must be valid",
        messageFormat: "Validator method '{0}' signature must be '{1}static ValueTask {0}(FieldArgumentsValidationContext context)'",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.VALIDATE_ARGUMENTS_METHOD_MUST_BE_VALID);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(analysisContext =>
        {
            var validationContextType = analysisContext.Compilation.GetTypeByMetadataName("GraphQL.Validation.FieldArgumentsValidationContext");
            var valueTaskType = analysisContext.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
            if (validationContextType != null && valueTaskType != null)
            {
                analysisContext.RegisterSyntaxNodeAction(ctx => AnalyzeAttribute(ctx, validationContextType, valueTaskType), SyntaxKind.Attribute);
            }
        });
    }

    private void AnalyzeAttribute(SyntaxNodeAnalysisContext context, INamedTypeSymbol validationContextType, INamedTypeSymbol valueTaskType)
    {
        var attribute = (AttributeSyntax)context.Node;
        if (attribute.Name.ToString()
            is not Constants.AttributeNames.ValidateArguments
            and not Constants.AttributeNames.ValidateArguments + Constants.AttributeNames.Attribute)
        {
            return;
        }

        if (!attribute.IsGraphQLSymbol(context.SemanticModel))
            return;

        // validationType or/and validationMethodName
        if (attribute.ArgumentList?.Arguments.Count is < 1 or > 2)
            return;

        var arguments = attribute.GetAttributeArguments(context.SemanticModel);
        if (arguments == null)
            return;

        if (!TryGetType(
                attribute,
                arguments,
                argumentName: "validationType",
                context.SemanticModel,
                out var validatorType,
                out bool allowNonPublicMethods))
        {
            return;
        }

        if (!TryGetMethodName(
               arguments,
               context.SemanticModel,
               argumentName: "validationMethodName",
               defaultMethodName: "ValidateArguments",
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

            if (method.Parameters.Length != 1 || !SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, validationContextType))
                continue;

            if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, valueTaskType))
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
                    ValidateArgumentsMethodMustBeValid,
                    attribute.Parent!.GetLocation(),
                    validatorMethodName,
                    accessor));
        }
    }
}
