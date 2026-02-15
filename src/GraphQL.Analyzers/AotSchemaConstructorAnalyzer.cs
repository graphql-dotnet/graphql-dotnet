using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AotSchemaConstructorAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor AotSchemaConstructorMustCallConfigure = new(
        id: DiagnosticIds.AOT_SCHEMA_CONSTRUCTOR_MUST_CALL_CONFIGURE,
        title: "AotSchema constructor must call Configure or chain to another constructor",
        messageFormat: "Constructor in class '{0}' deriving from 'AotSchema' must either call 'Configure()' or chain to another non-base constructor",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.AOT_SCHEMA_CONSTRUCTOR_MUST_CALL_CONFIGURE);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(AotSchemaConstructorMustCallConfigure);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeConstructorDeclaration, SyntaxKind.ConstructorDeclaration);
    }

    private void AnalyzeConstructorDeclaration(SyntaxNodeAnalysisContext context)
    {
        var constructorDeclaration = (ConstructorDeclarationSyntax)context.Node;

        // Get the containing class
        if (constructorDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
            return;

        // Get the class symbol
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol == null)
            return;

        // Check if the class derives from AotSchema
        if (!DerivesFromAotSchema(classSymbol, context.Compilation))
            return;

        // Check if constructor chains to another non-base constructor using : this(...)
        if (constructorDeclaration.Initializer != null &&
            constructorDeclaration.Initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.ThisKeyword))
        {
            // Constructor chains to another constructor, this is OK
            return;
        }

        // Check if the constructor body calls Configure()
        if (constructorDeclaration.Body != null)
        {
            if (CallsConfigure(constructorDeclaration.Body, context.SemanticModel))
            {
                return;
            }
        }

        // Report diagnostic - constructor does not call Configure() and does not chain to another constructor
        var diagnostic = Diagnostic.Create(
            AotSchemaConstructorMustCallConfigure,
            constructorDeclaration.Identifier.GetLocation(),
            classSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool DerivesFromAotSchema(INamedTypeSymbol classSymbol, Compilation compilation)
    {
        var aotSchemaSymbol = compilation.GetTypeByMetadataName(Constants.MetadataNames.AotSchema);
        if (aotSchemaSymbol == null)
            return false;

        var current = classSymbol.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, aotSchemaSymbol))
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static bool CallsConfigure(BlockSyntax body, SemanticModel semanticModel)
    {
        // Look for any invocation of Configure() in the constructor body
        var invocations = body.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            // Check if this is a call to Configure()
            if (invocation.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.Text == "Configure")
            {
                // Verify it's calling the Configure method from the class (not some other Configure)
                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                    methodSymbol.Name == "Configure" &&
                    methodSymbol.Parameters.Length == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
