using System.Text;
using GraphQL.Analyzers.SourceGenerators.Providers;
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
            var attributeSymbols = KnownSymbolsProvider.Create(context);

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
                sb.AppendLine($"// IgnoreAttribute: {symbols.IgnoreAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// DoNotMapClrTypeAttribute: {symbols.DoNotMapClrTypeAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// ClrTypeMappingAttribute: {symbols.ClrTypeMappingAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// MemberScanAttribute: {symbols.MemberScanAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// ParameterAttribute: {symbols.ParameterAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// GraphQLConstructorAttribute: {symbols.GraphQLConstructorAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// InstanceSourceAttribute: {symbols.InstanceSourceAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// InputTypeAttributeT: {symbols.InputTypeAttributeT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// InputTypeAttribute: {symbols.InputTypeAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// InputBaseTypeAttributeT: {symbols.InputBaseTypeAttributeT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// InputBaseTypeAttribute: {symbols.InputBaseTypeAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// OutputTypeAttributeT: {symbols.OutputTypeAttributeT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// OutputTypeAttribute: {symbols.OutputTypeAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// OutputBaseTypeAttributeT: {symbols.OutputBaseTypeAttributeT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// OutputBaseTypeAttribute: {symbols.OutputBaseTypeAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// BaseGraphTypeAttributeT: {symbols.BaseGraphTypeAttributeT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// BaseGraphTypeAttribute: {symbols.BaseGraphTypeAttribute?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IEnumerableT: {symbols.IEnumerableT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IListT: {symbols.IListT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// ListT: {symbols.ListT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// ICollectionT: {symbols.ICollectionT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IReadOnlyCollectionT: {symbols.IReadOnlyCollectionT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IReadOnlyListT: {symbols.IReadOnlyListT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// HashSetT: {symbols.HashSetT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// ISetT: {symbols.ISetT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// Task: {symbols.Task?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// TaskT: {symbols.TaskT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// ValueTaskT: {symbols.ValueTaskT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IDataLoaderResultT: {symbols.IDataLoaderResultT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IObservableT: {symbols.IObservableT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IAsyncEnumerableT: {symbols.IAsyncEnumerableT?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IResolveFieldContext: {symbols.IResolveFieldContext?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// CancellationToken: {symbols.CancellationToken?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IInputObjectGraphType: {symbols.IInputObjectGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IObjectGraphType: {symbols.IObjectGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// IInterfaceGraphType: {symbols.IInterfaceGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// ScalarGraphType: {symbols.ScalarGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// ComplexGraphType: {symbols.ComplexGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// EnumerationGraphType: {symbols.EnumerationGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AutoRegisteringObjectGraphType: {symbols.AutoRegisteringObjectGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AutoRegisteringInputObjectGraphType: {symbols.AutoRegisteringInputObjectGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine($"// AutoRegisteringInterfaceGraphType: {symbols.AutoRegisteringInterfaceGraphType?.ToDisplayString() ?? "NULL"}");
                sb.AppendLine("//");
                sb.AppendLine($"// BuiltInScalarMappings ({symbols.BuiltInScalarMappings.Count} mappings):");
                for (int i = 0; i < symbols.BuiltInScalarMappings.Count; i++)
                {
                    var (clrType, graphType) = symbols.BuiltInScalarMappings[i];
                    sb.AppendLine($"//   {clrType?.ToDisplayString() ?? "NULL"} -> {graphType?.ToDisplayString() ?? "NULL"}");
                }

                spc.AddSource("AttributeSymbolsReport.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            });
        }
    }
}
