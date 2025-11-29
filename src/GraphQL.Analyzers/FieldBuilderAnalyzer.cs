using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using GraphQL.Analyzers.SDK;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FieldBuilderAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor CantInferFieldNameFromExpression = new(
        id: DiagnosticIds.CAN_NOT_INFER_FIELD_NAME_FROM_EXPRESSION,
        title: "Can't infer a Field name from expression",
        messageFormat: "Can't infer a Field name from expression: '{0}'",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.CAN_NOT_INFER_FIELD_NAME_FROM_EXPRESSION);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(CantInferFieldNameFromExpression);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var fieldInvocation = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);

        var fieldExpression = fieldInvocation?.FieldExpression;
        if (fieldExpression == null)
        {
            return;
        }

        if (!fieldExpression.IsValid)
        {
            var diagnostic = Diagnostic.Create(
                CantInferFieldNameFromExpression,
                fieldExpression.Syntax.GetLocation(),
                fieldExpression.Syntax.ToString());

            context.ReportDiagnostic(diagnostic);
        }
    }
}
