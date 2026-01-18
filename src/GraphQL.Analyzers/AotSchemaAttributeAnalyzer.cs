using System.Collections.Immutable;
using GraphQL.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GraphQL.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AotSchemaAttributeAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor AotSchemaAttributeMustBeOnAotSchema = new(
        id: DiagnosticIds.AOT_SCHEMA_ATTRIBUTE_MUST_BE_ON_AOT_SCHEMA,
        title: "AOT schema attributes must be applied to classes deriving from AotSchema",
        messageFormat: "The '{0}' attribute can only be applied to classes that derive from 'AotSchema'",
        category: DiagnosticCategories.USAGE,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLinks.AOT_SCHEMA_ATTRIBUTE_MUST_BE_ON_AOT_SCHEMA);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(AotSchemaAttributeMustBeOnAotSchema);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Get the class symbol
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol == null)
            return;

        // Check if the class has any attributes
        var attributes = classSymbol.GetAttributes();
        if (attributes.IsEmpty)
            return;

        // Get the AotSchemaAttribute symbol
        var aotSchemaAttributeSymbol = context.Compilation.GetTypeByMetadataName(Constants.MetadataNames.AotSchemaAttribute);
        if (aotSchemaAttributeSymbol == null)
            return;

        // Check if any attribute derives from or is AotSchemaAttribute
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null)
                continue;

            // Check if the attribute is AotSchemaAttribute or derives from it
            if (IsAotSchemaAttribute(attribute.AttributeClass, aotSchemaAttributeSymbol))
            {
                // Check if the class derives from AotSchema
                if (!DerivesFromAotSchema(classSymbol, context.Compilation))
                {
                    // Find the attribute syntax
                    var attributeSyntax = FindAttributeSyntax(classDeclaration, attribute, context.SemanticModel);
                    if (attributeSyntax != null)
                    {
                        var diagnostic = Diagnostic.Create(
                            AotSchemaAttributeMustBeOnAotSchema,
                            attributeSyntax.GetLocation(),
                            attribute.AttributeClass.Name);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }

    private static bool IsAotSchemaAttribute(INamedTypeSymbol attributeClass, INamedTypeSymbol aotSchemaAttributeSymbol)
    {
        var current = attributeClass;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, aotSchemaAttributeSymbol))
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static bool DerivesFromAotSchema(INamedTypeSymbol classSymbol, Compilation compilation)
    {
        var aotSchemaSymbol = compilation.GetTypeByMetadataName(Constants.MetadataNames.AotSchema);
        if (aotSchemaSymbol == null)
            return false;

        var current = classSymbol;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, aotSchemaSymbol))
                return true;

            current = current.BaseType;
        }

        return false;
    }

    private static AttributeSyntax? FindAttributeSyntax(
        ClassDeclarationSyntax classDeclaration,
        AttributeData attribute,
        SemanticModel semanticModel)
    {
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attributeSyntax in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attributeSyntax);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
                    SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, attribute.AttributeClass))
                {
                    return attributeSyntax;
                }
            }
        }

        return null;
    }
}
