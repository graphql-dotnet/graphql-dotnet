using System.Collections.Immutable;
using GraphQL.Analyzers.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Transformers;

/// <summary>
/// Transforms a CandidateClass into SchemaAttributeData by extracting and categorizing
/// all AOT-related attributes and their generic type arguments.
/// </summary>
public static class CandidateClassTransformer
{
    /// <summary>
    /// Extracts all AOT attribute data from a candidate class and assembles it into
    /// an immutable SchemaAttributeData record.
    /// </summary>
    /// <param name="candidate">The candidate class to transform.</param>
    /// <param name="attributeSymbols">The resolved attribute symbols for symbolic comparison.</param>
    public static SchemaAttributeData Transform(CandidateClass candidate, KnownSymbols attributeSymbols)
    {
        var schemaSymbol = candidate.ClassSymbol;

        // Prepare nullable holders for root types (only zero or one allowed)
        RootTypeInfo? queryType = null;
        RootTypeInfo? mutationType = null;
        RootTypeInfo? subscriptionType = null;

        // Prepare builders for collection types
        var outputTypes = ImmutableArray.CreateBuilder<OutputTypeInfo>();
        var inputTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var graphTypes = ImmutableArray.CreateBuilder<GraphTypeInfo>();
        var typeMappings = ImmutableArray.CreateBuilder<TypeMappingInfo>();
        var listTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var remapTypes = ImmutableArray.CreateBuilder<TypeMappingInfo>();

        // Iterate through all attributes on the class
        foreach (var attribute in schemaSymbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
                continue;

            // Get the original (unbound) generic type definition for comparison
            var unboundAttributeType = attributeClass.IsGenericType
                ? attributeClass.OriginalDefinition
                : attributeClass;

            // Categorize by attribute type using symbolic comparison
            if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotQueryTypeAttribute))
            {
                if (queryType == null && TryExtractRootTypeInfo(attributeClass, attributeSymbols.IGraphType, out var info))
                    queryType = info;
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotMutationTypeAttribute))
            {
                if (mutationType == null && TryExtractRootTypeInfo(attributeClass, attributeSymbols.IGraphType, out var info))
                    mutationType = info;
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotSubscriptionTypeAttribute))
            {
                if (subscriptionType == null && TryExtractRootTypeInfo(attributeClass, attributeSymbols.IGraphType, out var info))
                    subscriptionType = info;
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotOutputTypeAttribute))
            {
                if (TryExtractOutputTypeInfo(attribute, attributeClass, out var info))
                    outputTypes.Add(info);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotInputTypeAttribute))
            {
                if (TryExtractSingleTypeArgument(attributeClass, out var typeArg))
                    inputTypes.Add(typeArg);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotGraphTypeAttribute))
            {
                if (TryExtractGraphTypeInfo(attribute, attributeClass, out var info))
                    graphTypes.Add(info);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotTypeMappingAttribute))
            {
                if (TryExtractTypeMappingInfo(attributeClass, out var mapping))
                    typeMappings.Add(mapping);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotListTypeAttribute))
            {
                if (TryExtractSingleTypeArgument(attributeClass, out var typeArg))
                    listTypes.Add(typeArg);
            }
            else if (SymbolEqualityComparer.Default.Equals(unboundAttributeType, attributeSymbols.AotRemapTypeAttribute))
            {
                if (TryExtractTypeMappingInfo(attributeClass, out var mapping))
                    remapTypes.Add(mapping);
            }
        }

        return new SchemaAttributeData(
            SchemaClass: schemaSymbol,
            QueryType: queryType,
            MutationType: mutationType,
            SubscriptionType: subscriptionType,
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
    /// Extracts type info for AotOutputType attributes, including the optional IsInterface property.
    /// </summary>
    private static bool TryExtractOutputTypeInfo(AttributeData attribute, INamedTypeSymbol attributeClass, out OutputTypeInfo info)
    {
        info = default;

        if (attributeClass.TypeArguments.Length != 1)
            return false;

        var typeArg = attributeClass.TypeArguments[0];

        // Try to extract the Kind enum property value and convert to bool?
        // OutputTypeKind: Auto = 0 (null), Object = 1 (false), Interface = 2 (true)
        bool? isInterface = null;
        foreach (var namedArg in attribute.NamedArguments)
        {
            if (namedArg.Key == Constants.PropertyNames.KIND && namedArg.Value.Value is int kindValue)
            {
                isInterface = kindValue switch
                {
                    0 => null,    // OutputTypeKind.Auto
                    1 => false,   // OutputTypeKind.Object
                    2 => true,    // OutputTypeKind.Interface
                    _ => null
                };
                break;
            }
        }

        info = new OutputTypeInfo(typeArg, isInterface);
        return true;
    }

    /// <summary>
    /// Extracts type info for AotGraphType attributes, including the AutoRegisterClrMapping property.
    /// </summary>
    private static bool TryExtractGraphTypeInfo(AttributeData attribute, INamedTypeSymbol attributeClass, out GraphTypeInfo info)
    {
        info = default;

        if (attributeClass.TypeArguments.Length != 1)
            return false;

        var typeArg = attributeClass.TypeArguments[0];

        // Try to extract the AutoRegisterClrMapping property value (default is true)
        bool autoRegisterClrMapping = true;
        foreach (var namedArg in attribute.NamedArguments)
        {
            if (namedArg.Key == Constants.PropertyNames.AUTO_REGISTER_CLR_MAPPING && namedArg.Value.Value is bool boolValue)
            {
                autoRegisterClrMapping = boolValue;
                break;
            }
        }

        info = new GraphTypeInfo(typeArg, autoRegisterClrMapping);
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
