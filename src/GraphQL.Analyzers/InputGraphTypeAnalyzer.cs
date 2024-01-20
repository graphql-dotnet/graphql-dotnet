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
    private static readonly char[] _forceTypesAnalysisSeparator = [','];

    public static string ForceTypesAnalysisOption => "dotnet_diagnostic.input_graph_type_analyzer.force_types_analysis";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            CanNotMatchInputFieldToTheSourceField,
            CanNotSetSourceField,
            CanNotResolveInputSourceTypeConstructor);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeGenericName, SyntaxKind.GenericName);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var inputTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken);
        if (inputTypeSymbol == null)
        {
            return;
        }

        var sourceTypeSymbol = GetSourceTypeSymbol(inputTypeSymbol, context);
        if (sourceTypeSymbol == null || sourceTypeSymbol.SpecialType == SpecialType.System_Object)
        {
            return;
        }

        var sourceTypeMembers = sourceTypeSymbol is ITypeParameterSymbol parameterSymbol
            ? GetSourceTypeMembers(parameterSymbol.ConstraintTypes)
            : GetSourceTypeMembers(sourceTypeSymbol);

        var ctor = AnalyzeSourceTypeConstructors(context, classDeclaration, sourceTypeSymbol);

        AnalyzeInputGraphTypeFields(context, classDeclaration, sourceTypeSymbol, sourceTypeMembers, ctor);
    }

    private void AnalyzeGenericName(SyntaxNodeAnalysisContext context)
    {
        var genericName = (GenericNameSyntax)context.Node;

        // we currently support only direct usages of AutoRegisteringInputObjectGraphType
        // but not their derived types
        // ex: Filed<AutoRegisteringInputObjectGraphType<MySourceType>>()
        if (genericName.Identifier.Text != Constants.Types.AutoRegisteringInputObjectGraphType)
        {
            return;
        }

        // types derived from AutoRegisteringInputObjectGraphType analyzed by
        // AnalyzeClassDeclaration method
        if (genericName.Parent is SimpleBaseTypeSyntax)
        {
            return;
        }

        var type = context.SemanticModel.GetSymbolInfo(genericName, context.CancellationToken);
        if (type.Symbol is not INamedTypeSymbol inputTypeSymbol)
        {
            return;
        }

        var sourceTypeSymbol = GetSourceTypeSymbol(inputTypeSymbol, context);
        if (sourceTypeSymbol == null || sourceTypeSymbol.SpecialType == SpecialType.System_Object)
        {
            return;
        }

        var ctor = AnalyzeSourceTypeConstructors(context, genericName, sourceTypeSymbol);
    }

    private static ITypeSymbol? GetSourceTypeSymbol(
        INamedTypeSymbol typeSymbol,
        SyntaxNodeAnalysisContext context)
    {
        // quick test for interface implementation before iterating on base types
        if (!typeSymbol.AllInterfaces.Any(i => i.Name == Constants.Interfaces.IInputObjectGraphType))
        {
            return null;
        }

        var genericInputObjectGraphType = context.Compilation.GetTypeByMetadataName(Constants.MetadataNames.InputObjectGraphType);
        var parseDictionaryBaseMethod = genericInputObjectGraphType?.GetMembers(Constants.MethodNames.ParseDictionary)
            .OfType<IMethodSymbol>()
            .Single(); // analyzers are not supposed to throw exceptions, but we expect this to fail in tests if the base type changes

        string? forceTypesAnalysisOption = context.Options.GetStringOption(ForceTypesAnalysisOption, context.Node.SyntaxTree);
        var forceTypesAnalysis = forceTypesAnalysisOption
            ?.Split(_forceTypesAnalysisSeparator, StringSplitOptions.RemoveEmptyEntries)
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

    private static IEnumerable<ISymbol> GetSourceTypeMembers(IEnumerable<ITypeSymbol> sourceTypeSymbols) =>
        sourceTypeSymbols.SelectMany(GetSourceTypeMembers);

    private static IEnumerable<ISymbol> GetSourceTypeMembers(ITypeSymbol sourceTypeSymbol)
    {
        var symbols = Enumerable.Empty<ISymbol>();

        var nullableSourceTypeSymbol = sourceTypeSymbol;
        while (nullableSourceTypeSymbol != null)
        {
            var fieldsOrProperties = nullableSourceTypeSymbol
                .GetMembers()
                .Where(symbol => !symbol.IsImplicitlyDeclared && symbol is IPropertySymbol or IFieldSymbol);

            symbols = symbols.Concat(fieldsOrProperties);
            nullableSourceTypeSymbol = nullableSourceTypeSymbol.BaseType;
        }

        return symbols;
    }
}
