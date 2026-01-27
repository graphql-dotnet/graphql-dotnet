using System.Text;
using GraphQL.Analyzers.SourceGenerators.Providers;
using GraphQL.Analyzers.SourceGenerators.Transformers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

/// <summary>
/// A minimal test generator that scans specified CLR types and reports discovered dependencies.
/// This isolates testing of the TypeSymbolTransformer logic.
/// </summary>
[Generator]
public class TypeSymbolTransformerReportingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find classes marked with [ScanMe] attribute
        var typesToScan = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsScanCandidate(s),
                transform: static (ctx, _) => GetTypeToScan(ctx))
            .Where(static t => t.HasValue)
            .Select(static (t, _) => t!.Value)
            .Collect();

        // Resolve known symbols using AttributeSymbolsProvider
        var attributeSymbols = KnownSymbolsProvider.CreateAttributeSymbolsProvider(context);

        // Combine and transform
        var combined = typesToScan.Combine(attributeSymbols);

        context.RegisterSourceOutput(combined, (spc, data) =>
        {
            var (types, attributeSymbols) = data;

            if (types.Length == 0)
                return;

            var sb = new StringBuilder();

            bool isFirst = true;
            foreach (var item in types.OrderBy(t => t.TypeSymbol.Name))
            {
                var typeSymbol = item.TypeSymbol;
                var isInputType = item.IsInputType;

                // Add blank line between types
                if (!isFirst)
                    sb.AppendLine();

                isFirst = false;

                var result = TypeSymbolTransformer.Transform(typeSymbol, attributeSymbols, isInputType);

                if (result == null)
                {
                    sb.AppendLine($"// Type: {typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    sb.AppendLine("// Result: null (cannot be scanned)");
                    sb.AppendLine("//");
                    continue;
                }

                var scanResult = result.Value;

                sb.AppendLine($"// Type: {scanResult.ScannedType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                sb.AppendLine("//");

                // Discovered CLR Types
                sb.AppendLine($"// DiscoveredInputClrTypes: {scanResult.DiscoveredInputClrTypes.Count}");
                for (int i = 0; i < scanResult.DiscoveredInputClrTypes.Count; i++)
                {
                    sb.AppendLine($"//   [{i}] {scanResult.DiscoveredInputClrTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                }
                sb.AppendLine("//");

                // Discovered Output CLR Types
                sb.AppendLine($"// DiscoveredOutputClrTypes: {scanResult.DiscoveredOutputClrTypes.Count}");
                for (int i = 0; i < scanResult.DiscoveredOutputClrTypes.Count; i++)
                {
                    sb.AppendLine($"//   [{i}] {scanResult.DiscoveredOutputClrTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                }
                sb.AppendLine("//");

                // Discovered GraphTypes
                sb.AppendLine($"// DiscoveredGraphTypes: {scanResult.DiscoveredGraphTypes.Count}");
                for (int i = 0; i < scanResult.DiscoveredGraphTypes.Count; i++)
                {
                    sb.AppendLine($"//   [{i}] {scanResult.DiscoveredGraphTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                }
                sb.AppendLine("//");

                // Input List Types
                sb.AppendLine($"// InputListTypes: {scanResult.InputListTypes.Count}");
                for (int i = 0; i < scanResult.InputListTypes.Count; i++)
                {
                    sb.AppendLine($"//   [{i}] {scanResult.InputListTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                }
            }

            spc.AddSource("TypeScanReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }

    private static bool IsScanCandidate(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax or RecordDeclarationSyntax or InterfaceDeclarationSyntax;
    }

    private static (ITypeSymbol TypeSymbol, bool IsInputType)? GetTypeToScan(GeneratorSyntaxContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not ITypeSymbol symbol)
            return null;

        // Check for [ScanMe] attribute (used in tests)
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == "ScanMeAttribute")
            {
                // Extract the boolean constructor parameter
                bool isInputType = true; // Default value
                if (attribute.ConstructorArguments.Length > 0)
                {
                    var arg = attribute.ConstructorArguments[0];
                    if (arg.Value is bool boolValue)
                    {
                        isInputType = boolValue;
                    }
                }
                return (symbol, isInputType);
            }
        }

        return null;
    }
}
