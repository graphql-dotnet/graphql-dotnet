using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers.SDK.Examples;

/// <summary>
/// Example analyzer that demonstrates field name resolution from multiple sources:
/// - Explicit names: Field&lt;T&gt;("name")
/// - Const references: Field&lt;T&gt;(Constants.Name)
/// - Expression-based: Field(x => x.PropertyName)
/// - Expression with override: Field("customName", x => x.PropertyName)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2000:Add analyzer diagnostic IDs to analyzer release tracking", Justification = "Example analyzer for SDK demonstration purposes")]
public class ExampleFieldNameResolutionAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor FieldNameResolutionInfo = new(
        id: "GRAPHQL_SDK_EXAMPLE_005",
        title: "Field name resolution",
        messageFormat: "Field name '{0}' resolved from {1}",
        category: "Information",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(FieldNameResolutionInfo);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeField(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        var field = GraphQLFieldInvocation.TryCreate(invocation, context.SemanticModel);
        if (field?.Name is not { } fieldName)
        {
            return;
        }

        // Determine the source of the field name
        string source = DetermineNameSource(invocation, context.SemanticModel);

        // Report info diagnostic showing how the name was resolved
        context.ReportDiagnostic(Diagnostic.Create(
            FieldNameResolutionInfo,
            fieldName.Location,
            fieldName.Value,
            source));
    }

    private static string DetermineNameSource(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol methodSymbol)
        {
            return "unknown";
        }

        // Check for explicit name argument
        var nameArg = GetArgument(invocation, "name", methodSymbol);
        if (nameArg != null)
        {
            if (nameArg.Expression is LiteralExpressionSyntax)
            {
                return "string literal";
            }
            if (nameArg.Expression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
            {
                return "const field reference";
            }
        }

        // Check for expression argument
        var expressionArg = GetArgument(invocation, "expression", methodSymbol);
        if (expressionArg?.Expression is SimpleLambdaExpressionSyntax lambda)
        {
            if (lambda.Body is MemberAccessExpressionSyntax)
            {
                // If there's also a name argument, it's an explicit override
                if (nameArg != null)
                {
                    return "explicit override (expression with custom name)";
                }
                return "property expression (inferred from lambda)";
            }
        }

        return "unknown";
    }

    private static ArgumentSyntax? GetArgument(InvocationExpressionSyntax invocation, string argumentName, IMethodSymbol methodSymbol)
    {
        // Check named arguments
        foreach (var arg in invocation.ArgumentList.Arguments)
        {
            if (arg.NameColon?.Name.Identifier.Text == argumentName)
            {
                return arg;
            }
        }

        // Check positional arguments
        var paramIndex = Array.FindIndex(methodSymbol.Parameters.ToArray(), p => p.Name == argumentName);
        if (paramIndex >= 0 && paramIndex < invocation.ArgumentList.Arguments.Count)
        {
            var arg = invocation.ArgumentList.Arguments[paramIndex];
            if (arg.NameColon == null)
            {
                return arg;
            }
        }

        return null;
    }
}
