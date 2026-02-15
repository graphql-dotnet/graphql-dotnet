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
        if (constructorDeclaration.Body != null && CallsConfigure(constructorDeclaration.Body, context.SemanticModel, classSymbol))
        {
            return;
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

        return IsSameOrBaseType(aotSchemaSymbol, classSymbol.BaseType);
    }

    private static bool CallsConfigure(BlockSyntax body, SemanticModel semanticModel, INamedTypeSymbol containingType)
    {
        foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            // Reject explicit base.Configure()
            if (invocation.Expression is MemberAccessExpressionSyntax ma &&
                ma.Expression is BaseExpressionSyntax &&
                ma.Name.Identifier.Text == Constants.MethodNames.Configure)
            {
                continue;
            }

            // Bind the invoked method symbol (handles IdentifierName, MemberAccess, etc.)
            var info = semanticModel.GetSymbolInfo(invocation);

            // Prefer the chosen symbol; fall back to candidates if needed
            var method =
                info.Symbol as IMethodSymbol ??
                info.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

            if (method is null)
                continue;

            if (method.Name != Constants.MethodNames.Configure || method.Parameters.Length != 0)
                continue;

            if (!IsSameOrBaseType(method.ContainingType, containingType))
                continue;

            return true;
        }

        return false;
    }

    private static bool IsSameOrBaseType(INamedTypeSymbol candidate, INamedTypeSymbol? target)
    {
        for (var t = target; t is not null; t = t.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(candidate, t))
                return true;
        }
        return false;
    }
}
