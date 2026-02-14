using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using GraphQL.Analyzers.SDK;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers.Federation;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class FieldExistenceAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor FieldDoesNotExist = new(
        id: DiagnosticIds.FIELD_DOES_NOT_EXIST,
        title: "Field does not exist",
        messageFormat: "{0} field '{1}' does not exist on type '{2}'",
        category: DiagnosticCategories.FEDERATION,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.FIELD_DOES_NOT_EXIST);

    public static readonly DiagnosticDescriptor FieldsMustNotBeEmpty = new(
        id: DiagnosticIds.FIELDS_MUST_NOT_BE_EMPTY,
        title: "Directive fields must not be empty",
        messageFormat: "{0} directive must not have null or empty fields on type '{1}'",
        category: DiagnosticCategories.FEDERATION,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.FIELDS_MUST_NOT_BE_EMPTY);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(FieldDoesNotExist, FieldsMustNotBeEmpty);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.OnGraphQLGraphType(AnalyzeGraphQLGraphType);
    }

    private void AnalyzeGraphQLGraphType(GraphQLGraphType graphType, SyntaxNodeAnalysisContext context)
    {
        var federationKeys = graphType.FederationKeys;
        if (federationKeys == null || federationKeys.Count == 0)
            return;

        foreach (var key in federationKeys)
            ValidateKeyFields(context, graphType, key);
    }

    private static void ValidateKeyFields(
        SyntaxNodeAnalysisContext context,
        GraphQLGraphType graphType,
        FederationKey key)
    {
        // Check if the key is empty or whitespace
        if (string.IsNullOrWhiteSpace(key.FieldsString))
        {
            var diagnostic = Diagnostic.Create(
                FieldsMustNotBeEmpty,
                key.Location,
                "Key",
                graphType.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        var selectionSet = key.Fields;
        if (selectionSet == null)
            return;

        ValidateSelectionSet(context, graphType, key, selectionSet, graphType.Name, "Key");
    }

    private static void ValidateSelectionSet(
        SyntaxNodeAnalysisContext context,
        GraphQLGraphType graphType,
        FederationKey key,
        GraphQLSelectionSet selectionSet,
        string typeName,
        string attributeName)
    {
        foreach (var field in selectionSet.Selections.OfType<GraphQLField>())
        {
            var fieldName = field.Name.StringValue;
            var graphTypeField = graphType.GetField(fieldName);

            if (graphTypeField == null)
            {
                var fieldLocation = key.GetFieldLocation(fieldName, field.Location.Start);
                var diagnostic = Diagnostic.Create(
                    FieldDoesNotExist,
                    fieldLocation,
                    attributeName,
                    fieldName,
                    typeName);
                context.ReportDiagnostic(diagnostic);
            }
            else
            {
                // Validate nested selection set
                if (field.SelectionSet != null)
                {
                    var nestedGraphType = graphTypeField.GraphType?.GetUnwrappedType();
                    if (nestedGraphType != null)
                        ValidateSelectionSet(context, nestedGraphType, key, field.SelectionSet, nestedGraphType.Name, attributeName);
                }
            }
        }
    }
}
