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

    public static readonly DiagnosticDescriptor KeyFieldMustNotHaveArguments = new(
        id: DiagnosticIds.KEY_FIELD_MUST_NOT_HAVE_ARGUMENTS,
        title: "Key field must not have arguments",
        messageFormat: "Key field '{0}' must not have arguments on type '{1}'",
        category: DiagnosticCategories.FEDERATION,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.KEY_FIELD_MUST_NOT_HAVE_ARGUMENTS);

    public static readonly DiagnosticDescriptor KeyFieldMustNotBeInterfaceOrUnion = new(
        id: DiagnosticIds.KEY_FIELD_MUST_NOT_BE_INTERFACE_OR_UNION,
        title: "Key field must not be an interface or union type",
        messageFormat: "Key field '{0}' on type '{1}' returns {2} type '{3}' which is not allowed in key fields",
        category: DiagnosticCategories.FEDERATION,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.KEY_FIELD_MUST_NOT_BE_INTERFACE_OR_UNION);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DuplicateKey,
            RedundantKey,
            KeyFieldMustNotHaveArguments,
            KeyFieldMustNotBeInterfaceOrUnion);

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
        // Check if the key is empty or whitespace - handled by FieldExistenceAnalyzer
        if (string.IsNullOrWhiteSpace(key.FieldsString))
            return;

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

            if (graphTypeField != null)
            {
                // Check if the field has arguments in the GraphType definition
                if (graphTypeField.Arguments.Count > 0)
                {
                    var fieldLocation = key.GetFieldLocation(fieldName, field.Location.Start);
                    var diagnostic = Diagnostic.Create(
                        KeyFieldMustNotHaveArguments,
                        fieldLocation,
                        fieldName,
                        typeName);
                    context.ReportDiagnostic(diagnostic);
                }

                // Check if the field type is an interface or union
                var fieldGraphType = graphTypeField.GraphType?.GetUnwrappedType();
                if (fieldGraphType != null && (fieldGraphType.IsInterfaceType || fieldGraphType.IsUnionType))
                {
                    var fieldLocation = key.GetFieldLocation(fieldName, field.Location.Start);
                    var typeKind = fieldGraphType.IsInterfaceType ? "an interface" : "a union";
                    var diagnostic = Diagnostic.Create(
                        KeyFieldMustNotBeInterfaceOrUnion,
                        fieldLocation,
                        fieldName,
                        typeName,
                        typeKind,
                        fieldGraphType.Name);
                    context.ReportDiagnostic(diagnostic);
                }

                // Validate nested selection set
                if (field.SelectionSet != null)
                {
                    var nestedGraphType = graphTypeField.GraphType?.GetUnwrappedType();
                    if (nestedGraphType != null)
                        ValidateSelectionSet(context, nestedGraphType, key, field.SelectionSet, nestedGraphType.Name);
                }
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
        var keyFieldPaths = new List<(FederationKey Key, HashSet<string> FieldPaths)>(keys.Count);

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
