using System.Text;
using GraphQL.SourceGenerators.Providers;
using GraphQL.SourceGenerators.Transformers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

public partial class InputTypeSymbolTransformerTests
{
    /// <summary>
    /// A minimal test generator that scans specified CLR types and reports discovered dependencies.
    /// This isolates testing of the TypeSymbolTransformer logic.
    /// </summary>
    [Generator]
    public class ReportingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find classes marked with [ScanInputType] attribute
            var typesToScan = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsScanCandidate(s),
                    transform: static (ctx, _) => GetTypeToScan(ctx))
                .Where(static t => t != null)
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
                foreach (var typeSymbol in types.OrderBy(t => t!.Name))
                {
                    if (typeSymbol == null)
                        continue;

                    // Add blank line between types
                    if (!isFirst)
                        sb.AppendLine();

                    isFirst = false;

                    var result = TypeSymbolTransformer.Transform(typeSymbol, attributeSymbols, true);

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
                    sb.AppendLine($"// DiscoveredInputClrTypes: {scanResult.DiscoveredInputClrTypes.Length}");
                    for (int i = 0; i < scanResult.DiscoveredInputClrTypes.Length; i++)
                    {
                        sb.AppendLine($"//   [{i}] {scanResult.DiscoveredInputClrTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // Discovered GraphTypes
                    sb.AppendLine($"// DiscoveredGraphTypes: {scanResult.DiscoveredGraphTypes.Length}");
                    for (int i = 0; i < scanResult.DiscoveredGraphTypes.Length; i++)
                    {
                        sb.AppendLine($"//   [{i}] {scanResult.DiscoveredGraphTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                    sb.AppendLine("//");

                    // Input List Types
                    sb.AppendLine($"// InputListTypes: {scanResult.InputListTypes.Length}");
                    for (int i = 0; i < scanResult.InputListTypes.Length; i++)
                    {
                        sb.AppendLine($"//   [{i}] {scanResult.InputListTypes[i].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}");
                    }
                }

                spc.AddSource("TypeScanReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            });
        }

        private static bool IsScanCandidate(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax or RecordDeclarationSyntax;
        }

        private static ITypeSymbol? GetTypeToScan(GeneratorSyntaxContext context)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;
            if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration) is not ITypeSymbol symbol)
                return null;

            // Check for [ScanMe] attribute (used in tests)
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name == "ScanMeAttribute")
                    return symbol;
            }

            return null;
        }
    }
}
