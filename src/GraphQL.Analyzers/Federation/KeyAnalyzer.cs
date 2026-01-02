using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using GraphQL.Analyzers.SDK;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers.Federation;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KeyAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor KeyFieldDoesNotExist = new(
        id: DiagnosticIds.KEY_FIELD_DOES_NOT_EXIST,
        title: "Key field does not exist",
        messageFormat: "Key field '{0}' does not exist on type '{1}'",
        category: DiagnosticCategories.FEDERATION,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.KEY_FIELD_DOES_NOT_EXIST);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(KeyFieldDoesNotExist);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);

        if (graphType == null)
        {
            return;
        }

        var federationKeys = graphType.FederationKeys;
        if (federationKeys == null || federationKeys.Count == 0)
        {
            return;
        }

        foreach (var key in federationKeys)
        {
            ValidateKeyFields(context, graphType, key);
        }
    }

    private static void ValidateKeyFields(
        SyntaxNodeAnalysisContext context,
        GraphQLGraphType graphType,
        FederationKey key)
    {
        var selectionSet = key.Fields;
        if (selectionSet == null)
        {
            return;
        }

        ValidateSelectionSet(context, graphType, key, selectionSet, graphType.Name);
    }

    private static void ValidateSelectionSet(
        SyntaxNodeAnalysisContext context,
        GraphQLGraphType graphType,
        FederationKey key,
        GraphQLSelectionSet selectionSet,
        string typeName)
    {
        foreach (var field in selectionSet.Selections.OfType<GraphQLField>())
        {
            var fieldName = field.Name.StringValue;
            var graphTypeField = graphType.GetField(fieldName);

            if (graphTypeField == null)
            {
                var fieldLocation = key.GetFieldLocation(fieldName);
                var diagnostic = Diagnostic.Create(
                    KeyFieldDoesNotExist,
                    fieldLocation,
                    fieldName,
                    typeName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
