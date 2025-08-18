using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
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

    private static readonly HashSet<string> _supportedNames =
    [
        Constants.MethodNames.Field
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(CantInferFieldNameFromExpression);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeGenericNameSyntax, SyntaxKind.GenericName);
        context.RegisterSyntaxNodeAction(AnalyzeInvocationExpressionSyntax, SyntaxKind.InvocationExpression);
    }

    // Field<T>()
    private void AnalyzeGenericNameSyntax(SyntaxNodeAnalysisContext context)
    {
        var genericNameSyntax = (GenericNameSyntax)context.Node;

        AnalyzeExpressionSyntax(context, genericNameSyntax);
    }

    // Field(Type)
    private void AnalyzeInvocationExpressionSyntax(SyntaxNodeAnalysisContext context)
    {
        var invocationExpressionSyntax = (InvocationExpressionSyntax)context.Node;

        if (invocationExpressionSyntax.Expression is not IdentifierNameSyntax identifierSyntax)
        {
            return;
        }

        AnalyzeExpressionSyntax(context, identifierSyntax);
    }

    private static void AnalyzeExpressionSyntax(SyntaxNodeAnalysisContext context, SimpleNameSyntax genericNameSyntax)
    {
        string name = genericNameSyntax.Identifier.Text;
        if (!_supportedNames.Contains(name))
        {
            return;
        }

        if (!genericNameSyntax.IsGraphQLSymbol(context.SemanticModel))
        {
            return;
        }

        var methodSymbol = genericNameSyntax.GetMethodSymbol(context);
        if (methodSymbol == null)
        {
            return;
        }

        AnalyzeExpressionBasedFieldBuilder(context, genericNameSyntax, methodSymbol, name);
    }

    private static void AnalyzeExpressionBasedFieldBuilder(
        SyntaxNodeAnalysisContext context,
        SimpleNameSyntax genericNameSyntax,
        IMethodSymbol methodSymbol,
        string name)
    {
        if (name != Constants.MethodNames.Field || methodSymbol.ReturnType.Name != Constants.Types.FieldBuilder)
        {
            return;
        }

        var fieldInvocation = genericNameSyntax.FindMethodInvocationExpression()!;

        var expressionArg = GetArgument(Constants.ArgumentNames.Expression);
        if (expressionArg == null)
        {
            return;
        }

        if (expressionArg.Expression is SimpleLambdaExpressionSyntax { Body: not MemberAccessExpressionSyntax })
        {
            var nameArg = GetArgument(Constants.ArgumentNames.Name);
            if (nameArg == null)
            {
                ReportFieldTypeDiagnostic(
                    context,
                    expressionArg.GetLocation(),
                    CantInferFieldNameFromExpression,
                    isExpression: true,
                    messageArgs: expressionArg.Expression.ToString());
            }
        }

        ArgumentSyntax? GetArgument(string argName) =>
            fieldInvocation.GetMethodArgument(argName, context.SemanticModel);
    }

    private static void ReportFieldTypeDiagnostic(
        SyntaxNodeAnalysisContext context,
        Location location,
        DiagnosticDescriptor diagnosticDescriptor,
        bool isAsyncField = false,
        bool isDelegate = false,
        bool isExpression = false,
        params object?[]? messageArgs)
    {
        var props = ImmutableDictionary<string, string?>.Empty;

        if (isAsyncField)
        {
            props = props.Add(Constants.AnalyzerProperties.IsAsync, "true");
        }

        if (isDelegate)
        {
            props = props.Add(Constants.AnalyzerProperties.IsDelegate, "true");
        }

        if (isExpression)
        {
            props = props.Add(Constants.AnalyzerProperties.IsExpression, "true");
        }

        var diagnostic = Diagnostic.Create(diagnosticDescriptor, location, props, messageArgs);
        context.ReportDiagnostic(diagnostic);
    }
}
