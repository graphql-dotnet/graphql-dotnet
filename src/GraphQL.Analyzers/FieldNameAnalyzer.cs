using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FieldNameAnalyzer : DiagnosticAnalyzer
{
    // Violation: Field<T>().Name("xxx") or Field<T>(x => x.Prop).Name("xxx")
    // Fixed:     Field<T>("xxx")        or Field<T>("xxx", x => x.Prop)
    public static readonly DiagnosticDescriptor DefineTheNameInFieldMethod = new(
        id: DiagnosticIds.DEFINE_THE_NAME_IN_FIELD_METHOD,
        title: "Define the name in 'Field', 'Connection' or 'ConnectionBuilder.Create' method",
        messageFormat: "The name should be provided via '{0}' method",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.DEFINE_THE_NAME_IN_FIELD_METHOD);

    // Violation: Field<T>("xxx").Name("xxx")
    // Fixed:     Field<T>("xxx")
    public static readonly DiagnosticDescriptor NameMethodInvocationCanBeRemoved = new(
        id: DiagnosticIds.NAME_METHOD_INVOCATION_CAN_BE_REMOVED,
        title: "'Name' method invocation can be removed",
        messageFormat: "The name should be provided via '{0}' method. 'Name' method invocation can be removed.",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.NAME_METHOD_INVOCATION_CAN_BE_REMOVED);

    // Violation: Field<T>("xxx").Name("yyy")
    // Fixed:     Field<T>("xxx") or Field<T>("yyy")
    public static readonly DiagnosticDescriptor DifferentNamesDefinedByFieldAndNameMethods = new(
        id: DiagnosticIds.DIFFERENT_NAMES_DEFINED_BY_FIELD_AND_NAME_METHODS,
        title: "Different names defined by 'Field', 'Connection' or 'ConnectionBuilder.Create' and 'Name' methods",
        messageFormat: "The name should be provided via '{0}' method. Different names defined by '{0}' and 'Name' methods.",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.DIFFERENT_NAMES_DEFINED_BY_FIELD_AND_NAME_METHODS);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DefineTheNameInFieldMethod,
        NameMethodInvocationCanBeRemoved,
        DifferentNamesDefinedByFieldAndNameMethods);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
    }

    private void AnalyzeMemberAccessExpression(SyntaxNodeAnalysisContext context)
    {
        var nameMemberAccessExpression = (MemberAccessExpressionSyntax)context.Node;
        if (nameMemberAccessExpression.Name.Identifier.Text != Constants.MethodNames.Name)
        {
            return;
        }

        if (!nameMemberAccessExpression.IsGraphQLSymbol(context))
        {
            return;
        }

        if (!TryGetBuilderInvocation(
                nameMemberAccessExpression,
                out string? builderName,
                out var builderMethodInvocation))
        {
            return;
        }

        // Field().Name("xxx")
        if (!builderMethodInvocation.ArgumentList.Arguments.Any())
        {
            ReportNameDiagnostic(context, nameMemberAccessExpression, DefineTheNameInFieldMethod, builderName);
            return;
        }

        // Field(x => x.CompanyName).Name("xxx")
        var fieldName = builderMethodInvocation.GetMethodArgument(Constants.ArgumentNames.Name, context.SemanticModel);
        if (fieldName == null)
        {
            ReportNameDiagnostic(context, nameMemberAccessExpression, DefineTheNameInFieldMethod, builderName);
            return;
        }

        var nameName = ((InvocationExpressionSyntax)nameMemberAccessExpression.Parent!).ArgumentList.Arguments.First();

        if (fieldName.Expression.IsEquivalentTo(nameName.Expression))
        {
            // Field("xxx").Name("xxx")
            ReportNameDiagnostic(context, nameMemberAccessExpression, NameMethodInvocationCanBeRemoved, builderName);
        }
        else
        {
            // Field("xxx").Name("yyy")
            ReportNameDiagnostic(context, nameMemberAccessExpression, DifferentNamesDefinedByFieldAndNameMethods, builderName);
        }
    }

    private static bool TryGetBuilderInvocation(
        MemberAccessExpressionSyntax nameMemberAccessExpression,
        [NotNullWhen(true)] out string? builderName,
        [NotNullWhen(true)] out InvocationExpressionSyntax? builderMethodInvocation)
    {
        builderName = Constants.MethodNames.Field;
        builderMethodInvocation = nameMemberAccessExpression.FindMethodInvocationExpression(builderName);
        if (builderMethodInvocation != null)
        {
            return true;
        }

        builderName = Constants.MethodNames.Connection;
        builderMethodInvocation = nameMemberAccessExpression.FindMethodInvocationExpression(builderName);
        if (builderMethodInvocation != null)
        {
            return true;
        }

        builderName = Constants.MethodNames.Create;
        builderMethodInvocation = nameMemberAccessExpression.FindMethodInvocationExpression(builderName);
        if (builderMethodInvocation != null)
        {
            return true;
        }

        builderName = null;
        builderMethodInvocation = null;
        return false;
    }

    private static void ReportNameDiagnostic(
        SyntaxNodeAnalysisContext context,
        MemberAccessExpressionSyntax nameMemberAccessExpression,
        DiagnosticDescriptor diagnosticDescriptor,
        string builderMethodName)
    {
        var props = new Dictionary<string, string?>
        {
            [Constants.Properties.BuilderMethodName] = builderMethodName
        }.ToImmutableDictionary();

        var location = nameMemberAccessExpression.GetMethodInvocationLocation();
        var diagnostic = Diagnostic.Create(
            diagnosticDescriptor,
            location,
            props,
            messageArgs: builderMethodName);
        context.ReportDiagnostic(diagnostic);
    }
}
