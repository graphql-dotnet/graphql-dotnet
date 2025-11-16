using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers.SDK.Examples;

/// <summary>
/// Example analyzer that validates field naming conventions.
/// Demonstrates accessing field properties and reporting multiple diagnostics.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2000:Add analyzer diagnostic IDs to analyzer release tracking", Justification = "Example analyzer for SDK demonstration purposes")]
public class ExampleFieldNamingAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor FieldNameStartsWithUnderscore = new(
        id: "GRAPHQL_SDK_EXAMPLE_002",
        title: "Field name should not start with underscore",
        messageFormat: "Field name '{0}' should not start with underscore",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FieldNameNotCamelCase = new(
        id: "GRAPHQL_SDK_EXAMPLE_003",
        title: "Field name should be camelCase",
        messageFormat: "Field name '{0}' should be in camelCase format",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FieldNameNotBeNullOrEmpty = new(
        id: "GRAPHQL_SDK_EXAMPLE_004",
        title: "Field name should not be null",
        messageFormat: "Field name should not be null",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(FieldNameStartsWithUnderscore, FieldNameNotCamelCase, FieldNameNotBeNullOrEmpty);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Register to analyze invocation expressions (field definitions)
        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Try to wrap the invocation as a GraphQL field
        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);
        if (field?.Name is not { } fieldNameProperty)
        {
            return; // Not a Field() invocation or can't determine field name
        }

        var fieldName = fieldNameProperty.Value;
        var isNullOrWhiteSpace = string.IsNullOrWhiteSpace(fieldName);
        if (isNullOrWhiteSpace)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                FieldNameNotBeNullOrEmpty,
                fieldNameProperty.Location));
        }

        // Check if field name starts with underscore
        // Report diagnostic at the exact location of the field name
        if (!isNullOrWhiteSpace && fieldName!.StartsWith("_", StringComparison.Ordinal) == true)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                FieldNameStartsWithUnderscore,
                fieldNameProperty.Location,
                fieldName));
        }

        // Check if field name is camelCase (first letter lowercase, no underscores)
        if (!isNullOrWhiteSpace && !IsCamelCase(fieldName!))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                FieldNameNotCamelCase,
                fieldNameProperty.Location,
                fieldName));
        }

        // Example: Access more field properties
        if (field.Arguments.Count > 0)
        {
            // Analyze arguments
            foreach (var argument in field.Arguments)
            {
                // Could check argument naming conventions here
                // Now we can report diagnostics on the argument name itself
                if (argument.Name is { } argName && argName.Value!.StartsWith("_", StringComparison.Ordinal))
                {
                    // Report at the exact location of the argument name
                    context.ReportDiagnostic(Diagnostic.Create(
                        FieldNameStartsWithUnderscore,
                        argName.Location,
                        argName.Value));
                }
            }
        }

        // Example: Check declaring graph type
        if (field.DeclaringGraphType != null)
        {
            var isInputType = field.DeclaringGraphType.IsInputType;
            var sourceType = field.DeclaringGraphType.SourceType;
            // Could perform analysis based on the declaring type
            _ = (isInputType, sourceType); // Use variables to avoid warnings
        }
    }

    private static bool IsCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        // First character should be lowercase
        if (!char.IsLower(name[0]))
            return false;

        // Should not contain underscores
        if (name.Contains('_'))
            return false;

        return true;
    }
}
