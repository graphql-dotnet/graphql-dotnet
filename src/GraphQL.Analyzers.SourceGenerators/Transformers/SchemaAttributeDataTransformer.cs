using GraphQL.Analyzers.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Transformers;

/// <summary>
/// Transforms SchemaAttributeData by processing attributes and walking the type graph to discover all types.
/// Uses TypeSymbolTransformer to collect data from each CLR type it inspects.
/// </summary>
public readonly ref struct SchemaAttributeDataTransformer
{
    private readonly KnownSymbols _knownSymbols;
    private readonly HashSet<ITypeSymbol> _discoveredGraphTypes;
    private readonly Dictionary<ITypeSymbol, ITypeSymbol> _outputClrTypeMappings;
    private readonly Dictionary<ITypeSymbol, ITypeSymbol> _inputClrTypeMappings;
    private readonly HashSet<ITypeSymbol> _inputListTypes;
    private readonly Queue<(ITypeSymbol Type, bool IsInputType)> _clrTypesToProcess;
    private readonly Dictionary<ITypeSymbol, ImmutableEquatableArray<ISymbol>> _graphTypeMembers;

    /// <summary>
    /// Processes attribute data and walks the type graph to discover all referenced types.
    /// </summary>
    /// <param name="schemaData">The raw attribute data extracted from schema class.</param>
    /// <param name="knownSymbols">Known GraphQL symbol references for comparison.</param>
    /// <returns>Processed schema data with all discovered types and mappings.</returns>
    public static ProcessedSchemaData Transform(SchemaAttributeData schemaData, KnownSymbols knownSymbols)
    {
        var transformer = new SchemaAttributeDataTransformer(knownSymbols);
        return transformer.TransformInternal(schemaData);
    }

    private SchemaAttributeDataTransformer(KnownSymbols knownSymbols)
    {
        _knownSymbols = knownSymbols;
        _discoveredGraphTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        _outputClrTypeMappings = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        _inputClrTypeMappings = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        _inputListTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        _clrTypesToProcess = new Queue<(ITypeSymbol Type, bool IsInputType)>();
        _graphTypeMembers = new Dictionary<ITypeSymbol, ImmutableEquatableArray<ISymbol>>(SymbolEqualityComparer.Default);
    }

    /// <inheritdoc cref="Transform(SchemaAttributeData, KnownSymbols)"/>
    private ProcessedSchemaData TransformInternal(SchemaAttributeData schemaData)
    {
        // Step 1: Initialize root type variables
        ITypeSymbol? queryRootGraphType = null;
        ITypeSymbol? mutationRootGraphType = null;
        ITypeSymbol? subscriptionRootGraphType = null;

        // Step 2: Process schema attribute data to populate initial data

        // Process AotTypeMapping<TClr, TGraph> attributes
        foreach (var mapping in schemaData.TypeMappings)
        {
            var clrType = mapping.FromType;
            var graphType = mapping.ToType;

            // Check if TGraph is a ScalarGraphType or IInputObjectGraphType
            var isScalar = IsScalarGraphType(graphType);
            var isInputObject = ImplementsInterface(graphType, _knownSymbols.IInputObjectGraphType);

            // Set input type mapping if applicable
            if (isScalar || isInputObject)
            {
                SetInputTypeMapping(clrType, graphType);
            }

            // Set output type mapping if applicable
            if (isScalar || !isInputObject)
            {
                SetOutputTypeMapping(clrType, graphType);
            }
        }

        // Process AotOutputType<T> attributes
        foreach (var outputTypeInfo in schemaData.OutputTypes)
        {
            TryAddOutputClrType(outputTypeInfo.TypeArgument, outputTypeInfo.IsInterface);
        }

        // Process AotInputType<T> attributes
        foreach (var inputType in schemaData.InputTypes)
        {
            TryAddInputClrType(inputType);
        }

        // Process root types (Query/Mutation/Subscription)
        if (schemaData.QueryType.HasValue)
        {
            var info = schemaData.QueryType.Value;
            if (info.IsClrType)
            {
                queryRootGraphType = TryAddOutputClrType(info.TypeArgument, null);
            }
            else
            {
                queryRootGraphType = info.TypeArgument;
                TryAddGraphType(info.TypeArgument, false, false);
            }
        }

        if (schemaData.MutationType.HasValue)
        {
            var info = schemaData.MutationType.Value;
            if (info.IsClrType)
            {
                mutationRootGraphType = TryAddOutputClrType(info.TypeArgument, null);
            }
            else
            {
                mutationRootGraphType = info.TypeArgument;
                TryAddGraphType(info.TypeArgument, false, false);
            }
        }

        if (schemaData.SubscriptionType.HasValue)
        {
            var info = schemaData.SubscriptionType.Value;
            if (info.IsClrType)
            {
                subscriptionRootGraphType = TryAddOutputClrType(info.TypeArgument, null);
            }
            else
            {
                subscriptionRootGraphType = info.TypeArgument;
                TryAddGraphType(info.TypeArgument, false, false);
            }
        }

        // Process AotRemapType<TFrom, TTo> attributes
        foreach (var mapping in schemaData.RemapTypes)
        {
            TryAddGraphType(mapping.FromType, false, true);
        }

        // Process AotGraphType<T> attributes
        foreach (var graphTypeInfo in schemaData.GraphTypes)
        {
            bool ignoreClrMapping = !graphTypeInfo.AutoRegisterClrMapping;
            TryAddGraphType(graphTypeInfo.TypeArgument, ignoreClrMapping, false);
        }

        // Process explicit list types from AotListType<T>
        foreach (var listType in schemaData.ListTypes)
        {
            _inputListTypes.Add(listType);
        }

        // Step 3: Walk the type graph - process the queue until empty (breadth-first traversal)
        while (_clrTypesToProcess.Count > 0)
        {
            var (currentClrType, isInputType) = _clrTypesToProcess.Dequeue();

            // Use TypeSymbolTransformer to scan the type
            var scanResult = TypeSymbolTransformer.Transform(currentClrType, _knownSymbols, isInputType);

            // Skip if type cannot be scanned (e.g., open generic types)
            if (!scanResult.HasValue)
                continue;

            var result = scanResult.Value;

            // Store the members for the graph type that corresponds to this CLR type
            StoreGraphTypeMembers(currentClrType, isInputType, result.SelectedMembers);

            // Collect input list types discovered during scanning
            foreach (var listType in result.InputListTypes)
            {
                _inputListTypes.Add((ITypeSymbol)listType);
            }

            // Process discovered GraphTypes
            foreach (var discoveredGraphType in result.DiscoveredGraphTypes)
            {
                TryAddGraphType((ITypeSymbol)discoveredGraphType, true, false);
            }

            // Process discovered input CLR types
            foreach (var discoveredInputClrType in result.DiscoveredInputClrTypes)
            {
                TryAddInputClrType((ITypeSymbol)discoveredInputClrType);
            }

            // Process discovered output CLR types
            foreach (var discoveredOutputClrType in result.DiscoveredOutputClrTypes)
            {
                TryAddOutputClrType((ITypeSymbol)discoveredOutputClrType, null);
            }
        }

        // Return the processed schema data
        return new ProcessedSchemaData(
            QueryRootGraphType: queryRootGraphType,
            MutationRootGraphType: mutationRootGraphType,
            SubscriptionRootGraphType: subscriptionRootGraphType,
            DiscoveredGraphTypes: _discoveredGraphTypes.ToImmutableEquatableArray<ISymbol>(),
            OutputClrTypeMappings: _outputClrTypeMappings.Select(kvp => ((ISymbol)kvp.Key, (ISymbol)kvp.Value)).ToImmutableEquatableArray(),
            InputClrTypeMappings: _inputClrTypeMappings.Select(kvp => ((ISymbol)kvp.Key, (ISymbol)kvp.Value)).ToImmutableEquatableArray(),
            InputListTypes: _inputListTypes.ToImmutableEquatableArray<ISymbol>(),
            GeneratedGraphTypesWithMembers: _graphTypeMembers.Select(kvp => ((ISymbol)kvp.Key, kvp.Value)).ToImmutableEquatableArray(),
            RemapTypes: schemaData.RemapTypes.Select(remap => ((ISymbol)remap.FromType, (ISymbol)remap.ToType)).ToImmutableEquatableArray());
    }

    /// <summary>
    /// Tries to add a GraphType if not already discovered.
    /// </summary>
    private void TryAddGraphType(ITypeSymbol graphType, bool ignoreClrMapping, bool remapped)
    {
        if (_discoveredGraphTypes.Add(graphType))
        {
            if (!remapped)
            {
                // Check if this is an AutoRegistering type and enqueue the CLR type for processing
                if (TryExtractAutoRegisteringType(graphType, _knownSymbols.AutoRegisteringInputObjectGraphType, out var inputClrType))
                {
                    _clrTypesToProcess.Enqueue((inputClrType, true));
                }
                else if (TryExtractAutoRegisteringType(graphType, _knownSymbols.AutoRegisteringObjectGraphType, out var outputClrType) ||
                        TryExtractAutoRegisteringType(graphType, _knownSymbols.AutoRegisteringInterfaceGraphType, out outputClrType))
                {
                    _clrTypesToProcess.Enqueue((outputClrType, false));
                }
            }

            // Original mapping logic
            if (!ignoreClrMapping)
            {
                // Extract CLR type from GraphType if it has a source type parameter
                var clrType = ExtractClrTypeFromGraphType(graphType);
                if (clrType != null)
                {
                    _outputClrTypeMappings[clrType] = graphType;
                }
            }
        }
    }

    /// <summary>
    /// Tries to add an output CLR type if not already processed.
    /// Returns the wrapped GraphType that will represent this CLR type, or null if it cannot be created.
    /// </summary>
    private ITypeSymbol? TryAddOutputClrType(ITypeSymbol clrType, bool? isInterface)
    {
        if (_outputClrTypeMappings.TryGetValue(clrType, out var existingGraphType))
        {
            return existingGraphType;
        }

        // Check if this is an enum type - wrap with EnumerationGraphType<T>
        if (clrType.TypeKind == TypeKind.Enum)
        {
            var wrappedEnumGraphType = CreateEnumerationGraphType(clrType);
            if (wrappedEnumGraphType != null)
            {
                _outputClrTypeMappings[clrType] = wrappedEnumGraphType;
                _inputClrTypeMappings[clrType] = wrappedEnumGraphType;
                _discoveredGraphTypes.Add(wrappedEnumGraphType);
            }
            return wrappedEnumGraphType;
        }

        // Check if this is a known built-in scalar type
        if (TryGetBuiltInScalarGraphType(clrType, out var builtInScalarGraphType))
        {
            // Register the built-in scalar mapping for both input and output
            _outputClrTypeMappings[clrType] = builtInScalarGraphType;
            _inputClrTypeMappings[clrType] = builtInScalarGraphType;
            _discoveredGraphTypes.Add(builtInScalarGraphType);
            return builtInScalarGraphType;
        }

        // Determine if this is an interface type
        bool isInterfaceType = isInterface ?? clrType.TypeKind == TypeKind.Interface;

        // Create a synthetic wrapped GraphType representation
        var wrappedGraphType = CreateWrappedGraphType(clrType, isInterfaceType, false);

        if (wrappedGraphType != null)
        {
            _outputClrTypeMappings[clrType] = wrappedGraphType;
            if (_discoveredGraphTypes.Add(wrappedGraphType))
                _clrTypesToProcess.Enqueue((clrType, false));
        }

        return wrappedGraphType;
    }

    /// <summary>
    /// Tries to add an input CLR type if not already processed.
    /// Returns the wrapped GraphType that will represent this CLR type, or null if it cannot be created.
    /// </summary>
    private ITypeSymbol? TryAddInputClrType(ITypeSymbol clrType)
    {
        if (_inputClrTypeMappings.TryGetValue(clrType, out var existingGraphType))
        {
            return existingGraphType;
        }

        // Check if this is a known built-in scalar type
        if (TryGetBuiltInScalarGraphType(clrType, out var builtInScalarGraphType))
        {
            // Register the built-in scalar mapping for both input and output
            _inputClrTypeMappings[clrType] = builtInScalarGraphType;
            _outputClrTypeMappings[clrType] = builtInScalarGraphType;
            _discoveredGraphTypes.Add(builtInScalarGraphType);
            return builtInScalarGraphType;
        }

        // Create a synthetic wrapped GraphType representation
        var wrappedGraphType = CreateWrappedGraphType(clrType, false, true);

        if (wrappedGraphType != null)
        {
            _inputClrTypeMappings[clrType] = wrappedGraphType;
            if (_discoveredGraphTypes.Add(wrappedGraphType))
                _clrTypesToProcess.Enqueue((clrType, true));
        }

        return wrappedGraphType;
    }

    /// <summary>
    /// Sets an explicit input type mapping from CLR type to GraphType.
    /// </summary>
    private void SetInputTypeMapping(ITypeSymbol clrType, ITypeSymbol graphType)
    {
        _inputClrTypeMappings[clrType] = graphType;
    }

    /// <summary>
    /// Sets an explicit output type mapping from CLR type to GraphType.
    /// </summary>
    private void SetOutputTypeMapping(ITypeSymbol clrType, ITypeSymbol graphType)
    {
        _outputClrTypeMappings[clrType] = graphType;
    }

    /// <summary>
    /// Checks if a type implements a specific interface.
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

    /// <summary>
    /// Checks if a type is or derives from ScalarGraphType.
    /// </summary>
    private bool IsScalarGraphType(ITypeSymbol type)
    {
        if (_knownSymbols.ScalarGraphType == null)
            return false;

        var currentType = type as INamedTypeSymbol;
        while (currentType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentType, _knownSymbols.ScalarGraphType))
                return true;

            currentType = currentType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Creates a wrapped GraphType for a CLR type by constructing the appropriate
    /// AutoRegisteringObjectGraphType&lt;T&gt;, AutoRegisteringInputObjectGraphType&lt;T&gt;,
    /// or AutoRegisteringInterfaceGraphType&lt;T&gt;.
    /// Returns null if the generic type definition is not available.
    /// </summary>
    private ITypeSymbol? CreateWrappedGraphType(ITypeSymbol clrType, bool isInterface, bool isInputType)
    {
        INamedTypeSymbol? genericTypeDefinition;

        if (isInputType)
        {
            // Input types: AutoRegisteringInputObjectGraphType<T>
            genericTypeDefinition = _knownSymbols.AutoRegisteringInputObjectGraphType;
        }
        else
        {
            // Output types: AutoRegisteringObjectGraphType<T> or AutoRegisteringInterfaceGraphType<T>
            genericTypeDefinition = isInterface
                ? _knownSymbols.AutoRegisteringInterfaceGraphType
                : _knownSymbols.AutoRegisteringObjectGraphType;
        }

        // Return null if the generic type definition is not available
        if (genericTypeDefinition == null)
            return null;

        // Construct the generic type with the CLR type as the type argument
        return genericTypeDefinition.Construct(clrType);
    }

    /// <summary>
    /// Creates an EnumerationGraphType&lt;T&gt; for an enum type.
    /// Returns null if the EnumerationGraphType is not available.
    /// </summary>
    private ITypeSymbol? CreateEnumerationGraphType(ITypeSymbol enumType)
    {
        var genericTypeDefinition = _knownSymbols.EnumerationGraphType;

        // Return null if the generic type definition is not available
        if (genericTypeDefinition == null)
            return null;

        // Construct the generic type with the enum type as the type argument
        return genericTypeDefinition.Construct(enumType);
    }

    /// <summary>
    /// Attempts to extract the CLR type from an AutoRegistering GraphType (e.g., AutoRegisteringObjectGraphType&lt;T&gt;).
    /// </summary>
    private static bool TryExtractAutoRegisteringType(ITypeSymbol graphType, INamedTypeSymbol? autoRegisteringTypeSymbol, out ITypeSymbol clrType)
    {
        clrType = null!;

        if (autoRegisteringTypeSymbol == null)
            return false;

        if (graphType is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, autoRegisteringTypeSymbol) &&
            namedType.TypeArguments.Length == 1)
        {
            clrType = namedType.TypeArguments[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to extract the CLR type from a GraphType's generic type parameter.
    /// For example, ObjectGraphType&lt;MyClass&gt; returns MyClass.
    /// Only extracts from types that inherit from ComplexGraphType&lt;T&gt; or EnumerationGraphType&lt;T&gt; where T is not object.
    /// </summary>
    private ITypeSymbol? ExtractClrTypeFromGraphType(ITypeSymbol graphType)
    {
        if (graphType is not INamedTypeSymbol namedGraphType)
            return null;

        // Step 1: Check for DoNotMapClrTypeAttribute - if present, return null
        if (_knownSymbols.DoNotMapClrTypeAttribute != null)
        {
            var attributes = namedGraphType.GetAttributes();
            foreach (var attr in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _knownSymbols.DoNotMapClrTypeAttribute))
                {
                    return null;
                }
            }
        }

        // Step 2: Check for ClrTypeMappingAttribute - if present, use that CLR type
        if (_knownSymbols.ClrTypeMappingAttribute != null)
        {
            var attributes = namedGraphType.GetAttributes();
            foreach (var attr in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, _knownSymbols.ClrTypeMappingAttribute))
                {
                    // The ClrTypeMappingAttribute has one constructor argument: the CLR type
                    if (attr.ConstructorArguments.Length == 1 &&
                        attr.ConstructorArguments[0].Value is ITypeSymbol clrType)
                    {
                        return clrType;
                    }
                }
            }
        }

        // Step 3: Walk up the inheritance hierarchy to find ComplexGraphType<T> or EnumerationGraphType<T>
        if (_knownSymbols.ComplexGraphType == null && _knownSymbols.EnumerationGraphType == null)
            return null;

        var currentType = namedGraphType;
        while (currentType != null)
        {
            if (currentType.IsGenericType && currentType.TypeArguments.Length == 1)
            {
                bool isComplexGraphType = _knownSymbols.ComplexGraphType != null &&
                    SymbolEqualityComparer.Default.Equals(currentType.OriginalDefinition, _knownSymbols.ComplexGraphType);

                bool isEnumerationGraphType = _knownSymbols.EnumerationGraphType != null &&
                    SymbolEqualityComparer.Default.Equals(currentType.OriginalDefinition, _knownSymbols.EnumerationGraphType);

                if (isComplexGraphType || isEnumerationGraphType)
                {
                    var typeArgument = currentType.TypeArguments[0];

                    // Only return if the type argument is not System.Object
                    if (typeArgument.SpecialType != SpecialType.System_Object)
                    {
                        return typeArgument;
                    }

                    return null;
                }
            }

            currentType = currentType.BaseType;
        }

        return null;
    }

    /// <summary>
    /// Checks if the given CLR type is a built-in scalar type and returns its corresponding GraphType.
    /// </summary>
    private bool TryGetBuiltInScalarGraphType(ITypeSymbol clrType, out ITypeSymbol graphType)
    {
        graphType = null!;

        // Check the built-in scalar mappings from KnownSymbols
        for (int i = 0; i < _knownSymbols.BuiltInScalarMappings.Count; i++)
        {
            var (mappedClrType, mappedGraphType) = _knownSymbols.BuiltInScalarMappings[i];
            if (SymbolEqualityComparer.Default.Equals(clrType, mappedClrType))
            {
                graphType = mappedGraphType;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Stores the members for the graph type that corresponds to the given CLR type.
    /// </summary>
    private void StoreGraphTypeMembers(ITypeSymbol currentClrType, bool isInputType, ImmutableEquatableArray<ISymbol> selectedMembers)
    {
        // Get the graph type that corresponds to this CLR type
        ITypeSymbol? graphType = null;
        if (isInputType)
        {
            _inputClrTypeMappings.TryGetValue(currentClrType, out graphType);
        }
        else
        {
            _outputClrTypeMappings.TryGetValue(currentClrType, out graphType);
        }

        // Store members if graph type exists and has members
        if (graphType != null)
        {
            _graphTypeMembers[graphType] = selectedMembers;
        }
    }
}
