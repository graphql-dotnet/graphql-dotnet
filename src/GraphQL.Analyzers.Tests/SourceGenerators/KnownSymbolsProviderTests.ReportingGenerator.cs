using System.Text;
using GraphQL.SourceGenerators.Providers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace GraphQL.Analyzers.Tests.SourceGenerators;

public partial class KnownSymbolsProviderTests
{
    /// <summary>
    /// A minimal test generator that uses AttributeSymbolsProvider to report 
    /// which attribute symbols were resolved. This isolates testing of the symbol resolution logic.
    /// </summary>
    [Generator]
    public class ReportingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Get attribute symbols
            var attributeSymbols = KnownSymbolsProvider.CreateAttributeSymbolsProvider(context);

            // Report the resolved symbols
            context.RegisterSourceOutput(attributeSymbols, (spc, symbols) =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("// Attribute Symbols Resolution:");
                sb.AppendLine("//");
                sb.AppendLine($"// AotQueryType: {symbols.AotQueryType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AotMutationType: {symbols.AotMutationType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AotSubscriptionType: {symbols.AotSubscriptionType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AotOutputType: {symbols.AotOutputType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AotInputType: {symbols.AotInputType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AotGraphType: {symbols.AotGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AotTypeMapping: {symbols.AotTypeMapping?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AotListType: {symbols.AotListType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AotRemapType: {symbols.AotRemapType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IGraphType: {symbols.IGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// NonNullGraphType: {symbols.NonNullGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// ListGraphType: {symbols.ListGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// GraphQLClrInputTypeReference: {symbols.GraphQLClrInputTypeReference?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// GraphQLClrOutputTypeReference: {symbols.GraphQLClrOutputTypeReference?.ToDisplayString() ?? "NULL"}");

                spc.AddSource("AttributeSymbolsReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            });
        }
    }
}
