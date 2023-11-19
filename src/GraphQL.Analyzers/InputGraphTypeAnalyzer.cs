using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class InputGraphTypeAnalyzer : DiagnosticAnalyzer
{
    public static string ForceTypesAnalysisOption => "dotnet_diagnostic.input_graph_type_analyzer.force_types_analysis";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            CanNotMatchInputFieldToTheSourceField,
            CanNotSetSourceField,
            CanNotResolveInputObjectConstructor);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var sourceTypeSymbol = GetSourceTypeSymbol(classDeclaration, context);
        if (sourceTypeSymbol == null || sourceTypeSymbol.SpecialType == SpecialType.System_Object)
        {
            return;
        }

        AnalyzeSourceTypeConstructors(context, classDeclaration, sourceTypeSymbol);
        AnalyzeInputGraphTypeFields(context, classDeclaration, sourceTypeSymbol);
    }

    private static ITypeSymbol? GetSourceTypeSymbol(
        ClassDeclarationSyntax inputClassDeclaration,
        SyntaxNodeAnalysisContext context)
    {
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(inputClassDeclaration);
        if (typeSymbol == null)
        {
            return null;
        }

        // quick test for interface implementation before iterating on base types
        if (!typeSymbol.AllInterfaces.Any(i => i.Name == Constants.Interfaces.IInputObjectGraphType))
        {
            return null;
        }

        var genericInputObjectGraphType = context.Compilation.GetTypeByMetadataName("GraphQL.Types.InputObjectGraphType`1");
        var parseDictionaryBaseMethod = genericInputObjectGraphType?.GetMembers(Constants.MethodNames.ParseDictionary)
            .OfType<IMethodSymbol>()
            .Single(); // analyzers are not supposed to throw exceptions but we expect this to fail in tests if the base type changes

        string? forceTypesAnalysisOption = context.Options.GetStringOption(ForceTypesAnalysisOption, context.Node.SyntaxTree);
        var forceTypesAnalysis = forceTypesAnalysisOption
            ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList();

        var sourceTypeSymbol = typeSymbol;
        while (sourceTypeSymbol != null)
        {
            if (SymbolEqualityComparer.Default.Equals(sourceTypeSymbol.OriginalDefinition, genericInputObjectGraphType))
            {
                return sourceTypeSymbol.TypeArguments.Single(); // <TSourceType>
            }

            bool overridesParseDictionary = sourceTypeSymbol
                .GetMembers("ParseDictionary")
                .OfType<IMethodSymbol>()
                .Any(m => SymbolEqualityComparer.Default.Equals(m.OverriddenMethod?.OriginalDefinition, parseDictionaryBaseMethod));

            if (overridesParseDictionary && forceTypesAnalysis?.Contains($"{sourceTypeSymbol.ContainingNamespace}.{sourceTypeSymbol.Name}") != true)
            {
                return null;
            }

            sourceTypeSymbol = sourceTypeSymbol.BaseType;
        }

        return null;
    }
}
