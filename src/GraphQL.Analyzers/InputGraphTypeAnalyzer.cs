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

    public static readonly DiagnosticDescriptor CanNotFulfillConstructorParameters = new(
        id: DiagnosticIds.CAN_NOT_FULFILL_CONSTRUCTOR_PARAMETERS,
        title: "Can not fulfill mandatory constructor parameters",
        messageFormat: "Defined fields can't fulfill the source type's '{0}' mandatory constructor parameters: {1}",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.CAN_NOT_FULFILL_CONSTRUCTOR_PARAMETERS);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            CanNotMatchInputFieldToTheSourceField,
            CanNotSetSourceField,
            CanNotResolveInputSourceTypeConstructor,
            CanNotFulfillConstructorParameters);

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
            ? GetSourceTypeMembers(parameterSymbol.ConstraintTypes, additionalFilter: _ => true)
            : GetSourceTypeMembers(sourceTypeSymbol, additionalFilter: _ => true);

        var ctor = AnalyzeSourceTypeConstructors(context, classDeclaration, sourceTypeSymbol);

        var declaredFieldNames = AnalyzeInputGraphTypeFields(context, classDeclaration, sourceTypeSymbol, sourceTypeMembers, ctor);

        var mandatoryConstructorParameters = ctor?.Parameters.Where(p => !p.IsOptional).Select(p => p.Name).ToList();
        List<string>? missingFields = null;

        if (mandatoryConstructorParameters?.Count is > 0)
        {
            if (declaredFieldNames == null)
            {
                missingFields = mandatoryConstructorParameters;
            }
            else
            {
                foreach (string constructorParameter in mandatoryConstructorParameters)
                {
                    if (!declaredFieldNames.Contains(constructorParameter))
                    {
                        (missingFields ??= []).Add(constructorParameter);
                    }
                }
            }
        }

        if (missingFields != null)
        {
            var location = FindSourceSymbolUsageLocation(context, classDeclaration, sourceTypeSymbol);
            context.ReportDiagnostic(Diagnostic.Create(
                CanNotFulfillConstructorParameters,
                location,
                sourceTypeSymbol.Name,
                missingFields.Count == 1 ? missingFields[0] : string.Join(", ", missingFields)));
        }
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
        var mandatoryConstructorParameters = ctor?.Parameters.Where(p => !p.IsOptional).Select(p => p.Name).ToList();
        List<string>? missingFields = null;

        if (mandatoryConstructorParameters?.Count is > 0)
        {
            var sourceTypeMembers = sourceTypeSymbol is ITypeParameterSymbol parameterSymbol
                ? GetSourceTypeMembers(parameterSymbol.ConstraintTypes, AdditionalFilter)
                : GetSourceTypeMembers(sourceTypeSymbol, AdditionalFilter);

            var declaredFieldNames =
                new HashSet<string>(sourceTypeMembers.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);

            foreach (string constructorParameter in mandatoryConstructorParameters)
            {
                if (!declaredFieldNames.Contains(constructorParameter))
                {
                    (missingFields ??= []).Add(constructorParameter);
                }
            }
        }

        if (missingFields != null)
        {
            var location = FindSourceSymbolUsageLocation(context, genericName, sourceTypeSymbol);
            context.ReportDiagnostic(Diagnostic.Create(
                CanNotFulfillConstructorParameters,
                location,
                sourceTypeSymbol.Name,
                missingFields.Count == 1 ? missingFields[0] : string.Join(", ", missingFields)));
        }

        static bool AdditionalFilter(ISymbol symbol)
        {
            if (symbol.IsStatic || symbol.IsAbstract || symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            // Is readonly property without backing field (can't be set in constructor)
            // ex: public string Hello => "World!";
            if (symbol is IPropertySymbol { IsReadOnly: true } prop && !prop.IsAutoProperty())
            {
                return false;
            }

            if (symbol.GetAttributes().Any(att => att.AttributeClass?.MetadataName == "GraphQL.IgnoreAttribute"))
            {
                return false;
            }

            return true;
        }
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

    private static IEnumerable<ISymbol> GetSourceTypeMembers(IEnumerable<ITypeSymbol> sourceTypeSymbols, Func<ISymbol, bool> additionalFilter) =>
        sourceTypeSymbols.SelectMany(symbol => GetSourceTypeMembers(symbol, additionalFilter));

    private static IEnumerable<ISymbol> GetSourceTypeMembers(ITypeSymbol sourceTypeSymbol, Func<ISymbol, bool> additionalFilter)
    {
        var symbols = Enumerable.Empty<ISymbol>();

        var nullableSourceTypeSymbol = sourceTypeSymbol;
        while (nullableSourceTypeSymbol != null)
        {
            var fieldsOrProperties = nullableSourceTypeSymbol
                .GetMembers()
                .Where(symbol => !symbol.IsImplicitlyDeclared && symbol is IPropertySymbol or IFieldSymbol)
                .Where(additionalFilter);

            symbols = symbols.Concat(fieldsOrProperties);
            nullableSourceTypeSymbol = nullableSourceTypeSymbol.BaseType;
        }

        return symbols;
    }
}
