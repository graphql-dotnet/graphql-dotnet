using System.Collections.Immutable;
using System.Text;
using GraphQL.Analyzers.Helpers;
using GraphQL.Analyzers.SDK;
using GraphQLParser.AST;
using Microsoft.CodeAnalysis;
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

    public static readonly DiagnosticDescriptor KeyMustNotBeNullOrEmpty = new(
        id: DiagnosticIds.KEY_MUST_NOT_BE_NULL_OR_EMPTY,
        title: "Key must not be null or empty",
        messageFormat: "Key must not be null or empty on type '{0}'",
        category: DiagnosticCategories.FEDERATION,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.KEY_MUST_NOT_BE_NULL_OR_EMPTY);

    public static readonly DiagnosticDescriptor DuplicateKey = new(
        id: DiagnosticIds.DUPLICATE_KEY,
        title: "Duplicate key",
        messageFormat: "Duplicate key '{0}' on type '{1}'",
        category: DiagnosticCategories.FEDERATION,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.DUPLICATE_KEY);

    public static readonly DiagnosticDescriptor RedundantKey = new(
        id: DiagnosticIds.REDUNDANT_KEY,
        title: "Redundant key",
        messageFormat: "Key '{0}' is redundant because key '{1}' already exists on type '{2}'",
        category: DiagnosticCategories.FEDERATION,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.REDUNDANT_KEY);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            KeyFieldDoesNotExist,
            KeyMustNotBeNullOrEmpty,
            DuplicateKey,
            RedundantKey);

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

        DetectDuplicateKeys(context, graphType, federationKeys);
        DetectRedundantKeys(context, graphType, federationKeys);
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
                KeyMustNotBeNullOrEmpty,
                key.Location,
                graphType.Name);
            context.ReportDiagnostic(diagnostic);
            return;
        }

        var selectionSet = key.Fields;
        if (selectionSet == null)
            return;

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
                var fieldLocation = key.GetFieldLocation(fieldName, field.Location.Start);
                var diagnostic = Diagnostic.Create(
                    KeyFieldDoesNotExist,
                    fieldLocation,
                    fieldName,
                    typeName);
                context.ReportDiagnostic(diagnostic);
            }
            else if (field.SelectionSet != null)
            {
                // Validate nested selection set
                var nestedGraphType = graphTypeField.GraphType?.GetUnwrappedType();
                if (nestedGraphType != null)
                    ValidateSelectionSet(context, nestedGraphType, key, field.SelectionSet, nestedGraphType.Name);
            }
        }
    }

    private static void DetectDuplicateKeys(
        SyntaxNodeAnalysisContext context,
        GraphQLGraphType graphType,
        IReadOnlyList<FederationKey> keys)
    {
        var normalizedKeys = new Dictionary<string, List<FederationKey>>(StringComparer.OrdinalIgnoreCase);

        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key.FieldsString))
                continue;

            if (key.Fields == null)
                continue;

            var normalizedKey = NormalizeSelectionSet(key.Fields);
            if (!normalizedKeys.TryGetValue(normalizedKey, out var duplicates))
            {
                duplicates = [];
                normalizedKeys[normalizedKey] = duplicates;
            }

            duplicates.Add(key);
        }

        foreach (var duplicates in normalizedKeys.Values)
        {
            if (duplicates.Count <= 1)
                continue;

            // Report diagnostic for all duplicates except the first one
            for (int i = 1; i < duplicates.Count; i++)
            {
                var duplicate = duplicates[i];
                var diagnostic = Diagnostic.Create(
                    DuplicateKey,
                    duplicate.Location,
                    duplicate.FieldsString,
                    graphType.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static string NormalizeSelectionSet(GraphQLSelectionSet selectionSet)
    {
        var sb = new StringBuilder();
        NormalizeSelectionSetCore(selectionSet, sb);
        return sb.ToString();
    }

    private static void NormalizeSelectionSetCore(GraphQLSelectionSet selectionSet, StringBuilder sb)
    {
        var fields = selectionSet.Selections
            .OfType<GraphQLField>()
            .OrderBy(f => f.Name.StringValue, StringComparer.OrdinalIgnoreCase);

        var isFirst = true;
        foreach (var field in fields)
        {
            if (isFirst)
                isFirst = false;
            else
                sb.Append(' ');

            sb.Append(field.Name.StringValue);

            if (field.SelectionSet != null)
            {
                sb.Append(" { ");
                NormalizeSelectionSetCore(field.SelectionSet, sb);
                sb.Append(" }");
            }
        }
    }

    private static void DetectRedundantKeys(
        SyntaxNodeAnalysisContext context,
        GraphQLGraphType graphType,
        IReadOnlyList<FederationKey> keys)
    {
        var keyFieldPaths = new List<(FederationKey Key, HashSet<string> FieldPaths)>();

        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key.FieldsString))
                continue;

            if (key.Fields == null)
                continue;

            var fieldPaths = ExtractFieldPaths(key.Fields);
            keyFieldPaths.Add((key, fieldPaths));
        }

        // Check for redundant keys
        for (int i = 0; i < keyFieldPaths.Count; i++)
        {
            for (int j = 0; j < keyFieldPaths.Count; j++)
            {
                if (i == j)
                    continue;

                var (keyI, pathsI) = keyFieldPaths[i];
                var (keyJ, pathsJ) = keyFieldPaths[j];

                // Check if keyI is a superset of keyJ (meaning keyI is redundant)
                if (pathsJ.IsSubsetOf(pathsI) && !pathsI.SetEquals(pathsJ))
                {
                    var diagnostic = Diagnostic.Create(
                        RedundantKey,
                        keyI.Location,
                        keyI.FieldsString,
                        keyJ.FieldsString,
                        graphType.Name);
                    context.ReportDiagnostic(diagnostic);
                    break; // Only report once per key
                }
            }
        }
    }

    private static HashSet<string> ExtractFieldPaths(GraphQLSelectionSet selectionSet, string prefix = "")
    {
        var fieldPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in selectionSet.Selections.OfType<GraphQLField>())
        {
            var fieldName = field.Name.StringValue;
            var fullPath = string.IsNullOrEmpty(prefix) ? fieldName : $"{prefix}.{fieldName}";

            if (field.SelectionSet != null)
            {
                // For nested fields, recursively extract paths
                var nestedPaths = ExtractFieldPaths(field.SelectionSet, fullPath);
                foreach (var path in nestedPaths)
                    fieldPaths.Add(path);
            }
            else
            {
                fieldPaths.Add(fullPath);
            }
        }

        return fieldPaths;
    }
}
