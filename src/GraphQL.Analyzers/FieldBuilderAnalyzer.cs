using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FieldBuilderAnalyzer : DiagnosticAnalyzer
{
    // Violation: Field<T>("name", "description", ...)
    // Fixed:     Field<T>("name").Description("description")...
    public static readonly DiagnosticDescriptor DoNotUseObsoleteFieldMethods = new(
        id: DiagnosticIds.DO_NOT_USE_OBSOLETE_FIELD_METHODS,
        title: "Don't use obsolete 'Field' methods",
        messageFormat: "Don't use obsolete 'Field' methods",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.DO_NOT_USE_OBSOLETE_FIELD_METHODS);

    private static readonly HashSet<string> _supportedNames = new()
    {
        Constants.MethodNames.Field,
        Constants.MethodNames.FieldAsync,
        Constants.MethodNames.FieldDelegate,
        Constants.MethodNames.FieldSubscribe,
        Constants.MethodNames.FieldSubscribeAsync,
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DoNotUseObsoleteFieldMethods);

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

    private void AnalyzeExpressionSyntax(SyntaxNodeAnalysisContext context, SimpleNameSyntax genericNameSyntax)
    {
        string name = genericNameSyntax.Identifier.Text;
        if (!_supportedNames.Contains(name))
        {
            return;
        }

        if (!genericNameSyntax.IsGraphQLSymbol(context))
        {
            return;
        }

        var methodSymbol = genericNameSyntax.GetMethodSymbol(context);
        if (methodSymbol == null)
        {
            return;
        }

        if (methodSymbol.ReturnType.Name != Constants.Types.FieldType)
        {
            return;
        }

        var fieldInvocation = genericNameSyntax.FindMethodInvocationExpression()!;

        ReportFieldTypeDiagnostic(
            context,
            fieldInvocation,
            DoNotUseObsoleteFieldMethods,
            isAsyncField: name.EndsWith("Async"),
            isDelegate: name == Constants.MethodNames.FieldDelegate);
    }

    private void ReportFieldTypeDiagnostic(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocationExpressionSyntax,
        DiagnosticDescriptor diagnosticDescriptor,
        bool isAsyncField,
        bool isDelegate)
    {
        var props = ImmutableDictionary<string, string?>.Empty;

        if (isAsyncField)
        {
            props = props.Add(Constants.Properties.IsAsync, "true");
        }

        if (isDelegate)
        {
            props = props.Add(Constants.Properties.IsDelegate, "true");
        }

        var location = invocationExpressionSyntax.GetLocation();
        var diagnostic = Diagnostic.Create(diagnosticDescriptor, location, properties: props);
        context.ReportDiagnostic(diagnostic);
    }
}
