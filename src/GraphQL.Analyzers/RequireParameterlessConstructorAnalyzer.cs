using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RequireParameterlessConstructorAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor RequireParameterlessConstructor = new(
        id: DiagnosticIds.REQUIRE_PARAMETERLESS_CONSTRUCTOR,
        title: "Require parameterless constructor",
        messageFormat: "The type '{0}' must define public parameterless constructor",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.REQUIRE_PARAMETERLESS_CONSTRUCTOR);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(RequireParameterlessConstructor);

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
        if (classDeclaration.BaseList == null)
        {
            return;
        }

        var classConstructors = context.SemanticModel.GetDeclaredSymbol(classDeclaration)?.Constructors;
        if (HasRequireParameterlessConstructor(classConstructors))
        {
            return;
        }

        // iterate all base types and interfaces and check for [RequireParameterlessConstructor] attribute
        foreach (var baseType in classDeclaration.BaseList.Types)
        {
            var baseTypeSymbol = context.SemanticModel.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol;
            if (baseTypeSymbol?.IsGraphQLSymbol() != true)
            {
                continue;
            }

            if (!HasRequireParameterlessConstructorAttribute(baseTypeSymbol))
            {
                continue;
            }

            ReportDiagnostic(
                context,
                classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.Text);
        }
    }

    private void AnalyzeGenericName(SyntaxNodeAnalysisContext context)
    {
        var genericName = (GenericNameSyntax)context.Node;
        if (genericName.Parent?.IsGraphQLSymbol(context.SemanticModel) != true)
        {
            return;
        }

        var typeInfo = context.SemanticModel.GetTypeInfo(genericName.Parent);
        if (typeInfo.Type is not INamedTypeSymbol namedTypeSymbol || namedTypeSymbol.TypeParameters.Length == 0)
        {
            return;
        }

        // Check if any generic parameter has [RequireParameterlessConstructor] attribute
        // then check if matching generic argument has parameterless constructor
        for (int i = 0; i < namedTypeSymbol.TypeParameters.Length; i++)
        {
            if (!HasRequireParameterlessConstructorAttribute(namedTypeSymbol.TypeParameters[i]))
            {
                continue;
            }

            if (namedTypeSymbol.TypeArguments[i] is not INamedTypeSymbol typeArg)
            {
                continue;
            }

            if (HasRequireParameterlessConstructor(typeArg.Constructors))
            {
                continue;
            }

            ReportDiagnostic(
                context,
                genericName.TypeArgumentList.Arguments[i].GetLocation(),
                typeArg.Name);
        }
    }

    private static bool HasRequireParameterlessConstructorAttribute(ISymbol typeParam) =>
        typeParam
            .GetAttributes()
            .Any(data => data.AttributeClass?.MetadataName ==
                         Constants.MetadataNames.RequireParameterlessConstructorAttribute);

    private static bool HasRequireParameterlessConstructor(ImmutableArray<IMethodSymbol>? constructors) =>
        constructors?.Length is null or 0 ||
        constructors.Value.Any(ctor =>
            ctor.Parameters.Length == 0 &&
            ctor.DeclaredAccessibility == Accessibility.Public);

    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location, string typeName) =>
        context.ReportDiagnostic(Diagnostic.Create(
            RequireParameterlessConstructor,
            location,
            typeName));
}
