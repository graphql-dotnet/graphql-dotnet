using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers.SDK.Examples;

/// <summary>
/// Example analyzer that demonstrates how to use the GraphQL SDK wrappers.
/// This analyzer reports fields that don't have descriptions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2000:Add analyzer diagnostic IDs to analyzer release tracking", Justification = "Example analyzer for SDK demonstration purposes")]
public class ExampleFieldDescriptionAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MissingDescriptionRule = new(
        id: "GRAPHQL_SDK_EXAMPLE_001",
        title: "Field is missing description",
        messageFormat: "Field '{0}' is missing a description",
        category: "Documentation",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "All GraphQL fields should have descriptions for better API documentation.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(MissingDescriptionRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Register to analyze class declarations (graph types)
        context.RegisterSyntaxNodeAction(AnalyzeGraphType, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeGraphType(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Try to wrap the class declaration as a GraphQL graph type
        var graphType = GraphQLGraphType.TryCreate(classDeclaration, context.SemanticModel);
        if (graphType == null)
        {
            return; // Not a GraphQL graph type
        }

        // Analyze all fields in this graph type
        foreach (var field in graphType.Fields)
        {
            // Check if the field has a description
            if (field.Description == null)
            {
                // Report diagnostic at the field name location (or entire invocation if name not available)
                var location = field.Name?.Location ?? field.Location;
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingDescriptionRule,
                    location,
                    field.Name?.Value ?? "unknown"));
            }
        }
    }
}
