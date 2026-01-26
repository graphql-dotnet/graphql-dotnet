using System.Collections.Immutable;
using GraphQL.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Transformers;

/// <summary>
/// Transforms a CandidateClass into SchemaAttributeData by extracting and categorizing
/// all AOT-related attributes and their generic type arguments.
/// </summary>
public static class AttributeDataTransformer
{
    /// <summary>
    /// Extracts all AOT attribute data from a candidate class and assembles it into
    /// an immutable SchemaAttributeData record.
    /// </summary>
    /// <param name="candidate">The candidate class to transform.</param>
    /// <param name="attributeSymbols">The resolved attribute symbols for symbolic comparison.</param>
    public static SchemaAttributeData? Transform(CandidateClass candidate, AotAttributeSymbols attributeSymbols)
    {
        if (candidate.SemanticModel.GetDeclaredSymbol(candidate.ClassDeclarationSyntax) is not INamedTypeSymbol schemaSymbol)
            return null;

        // Prepare builders for each attribute category
        var queryTypes = ImmutableArray.CreateBuilder<RootTypeInfo>();
        var mutationTypes = ImmutableArray.CreateBuilder<RootTypeInfo>();
        var subscriptionTypes = ImmutableArray.CreateBuilder<RootTypeInfo>();
        var outputTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var inputTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var graphTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var typeMappings = ImmutableArray.CreateBuilder<TypeMappingInfo>();
        var listTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var remapTypes = ImmutableArray.CreateBuilder<TypeMappingInfo>();

        // Iterate through all attributes on the class
        foreach (var attribute in schemaSymbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
                continue;

            // Get the unbound generic type for comparison
            var unboundAttributeType = attributeClass.IsGenericType
                ? attributeClass.ConstructUnboundGenericType()
                : attributeClass;

            // Categorize by attribute type using symbolic comparison
            if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotQueryType))
            {
                if (TryExtractRootTypeInfo(attributeClass, attributeSymbols.IGraphType, out var info))
                    queryTypes.Add(info);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotMutationType))
            {
                if (TryExtractRootTypeInfo(attributeClass, attributeSymbols.IGraphType, out var info))
                    mutationTypes.Add(info);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotSubscriptionType))
            {
                if (TryExtractRootTypeInfo(attributeClass, attributeSymbols.IGraphType, out var info))
                    subscriptionTypes.Add(info);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotOutputType))
            {
                if (TryExtractSingleTypeArgument(attributeClass, out var typeArg))
                    outputTypes.Add(typeArg);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotInputType))
            {
                if (TryExtractSingleTypeArgument(attributeClass, out var typeArg))
                    inputTypes.Add(typeArg);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotGraphType))
            {
                if (TryExtractSingleTypeArgument(attributeClass, out var typeArg))
                    graphTypes.Add(typeArg);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotTypeMapping))
            {
                if (TryExtractTypeMappingInfo(attributeClass, out var mapping))
                    typeMappings.Add(mapping);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotListType))
            {
                if (TryExtractSingleTypeArgument(attributeClass, out var typeArg))
                    listTypes.Add(typeArg);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotRemapType))
            {
                if (TryExtractTypeMappingInfo(attributeClass, out var mapping))
                    remapTypes.Add(mapping);
            }
        }

        return new SchemaAttributeData(
            SchemaClass: schemaSymbol,
            QueryTypes: queryTypes.ToImmutable(),
            MutationTypes: mutationTypes.ToImmutable(),
            SubscriptionTypes: subscriptionTypes.ToImmutable(),
            OutputTypes: outputTypes.ToImmutable(),
            InputTypes: inputTypes.ToImmutable(),
            GraphTypes: graphTypes.ToImmutable(),
            TypeMappings: typeMappings.ToImmutable(),
            ListTypes: listTypes.ToImmutable(),
            RemapTypes: remapTypes.ToImmutable());
    }

    /// <summary>
    /// Extracts type info for root types (Query/Mutation/Subscription), determining if it's a CLR type or graph type.
    /// </summary>
    private static bool TryExtractRootTypeInfo(INamedTypeSymbol attributeClass, INamedTypeSymbol? iGraphTypeSymbol, out RootTypeInfo info)
    {
        info = default;

        if (attributeClass.TypeArguments.Length != 1)
            return false;

        var typeArg = attributeClass.TypeArguments[0];
        var isClrType = !ImplementsInterface(typeArg, iGraphTypeSymbol);

        info = new RootTypeInfo(typeArg, isClrType);
        return true;
    }

    /// <summary>
    /// Extracts a single generic type argument from an attribute.
    /// </summary>
    private static bool TryExtractSingleTypeArgument(INamedTypeSymbol attributeClass, out ITypeSymbol typeArgument)
    {
        typeArgument = null!;

        if (attributeClass.TypeArguments.Length != 1)
            return false;

        typeArgument = attributeClass.TypeArguments[0];
        return true;
    }

    /// <summary>
    /// Extracts two generic type arguments for type mapping attributes.
    /// </summary>
    private static bool TryExtractTypeMappingInfo(INamedTypeSymbol attributeClass, out TypeMappingInfo mapping)
    {
        mapping = default;

        if (attributeClass.TypeArguments.Length != 2)
            return false;

        mapping = new TypeMappingInfo(
            FromType: attributeClass.TypeArguments[0],
            ToType: attributeClass.TypeArguments[1]);

        return true;
    }

    /// <summary>
    /// Checks if a type implements a specific interface (directly or indirectly) using symbolic comparison.
    /// </summary>
    private static bool ImplementsInterface(ITypeSymbol type, INamedTypeSymbol? interfaceSymbol)
    {
        if (interfaceSymbol == null)
            return false;

        foreach (var iface in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, interfaceSymbol))
                return true;
        }
        return false;
    }
}
