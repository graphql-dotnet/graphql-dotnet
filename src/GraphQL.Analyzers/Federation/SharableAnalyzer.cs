using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using GraphQL.Analyzers.SDK;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers.Federation;

/// <summary>
/// Analyzer to detect when @shareable directive is incorrectly applied.
/// According to Apollo Federation specifications, @shareable is only allowed on object types,
/// not on interface types or input types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SharableAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor ShareableNotAllowedOnInterface = new(
        id: DiagnosticIds.SHAREABLE_NOT_ALLOWED_ON_INTERFACE_OR_INPUT,
        title: "@shareable directive is not allowed on interface or input types",
        messageFormat: "Field '{0}' on {1} type '{2}' cannot use @shareable directive which is only allowed on object types",
        category: DiagnosticCategories.FEDERATION,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.SHAREABLE_NOT_ALLOWED_ON_INTERFACE_OR_INPUT);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(ShareableNotAllowedOnInterface);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.OnGraphQLGraphType(AnalyzeGraphQLGraphType);
    }

    private void AnalyzeGraphQLGraphType(GraphQLGraphType graphType, SyntaxNodeAnalysisContext context)
    {
        // Only check interface types and input types
        if (graphType.IsObjectType)
            return;

        string typeKind = graphType.IsInterfaceType ? "interface" : "input";
        foreach (var field in graphType.Fields)
        {
            if (field.IsShareable?.Value == true)
            {
                var fieldName = field.GetName()?.Value ?? "<unknown>";
                var diagnostic = Diagnostic.Create(
                    ShareableNotAllowedOnInterface,
                    field.IsShareable.Location,
                    fieldName,
                    typeKind,
                    graphType.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
