using GraphQL.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Transformers;

/// <summary>
/// Scans a CLR input or output type and discovers its dependencies by examining properties, fields, and methods.
/// </summary>
public static class TypeSymbolTransformer
{
    /// <summary>
    /// Scans a CLR type and discovers referenced CLR types, GraphTypes, and list types.
    /// Returns null if the type cannot be examined (e.g., open generic types).
    /// </summary>
    /// <param name="typeSymbol">The CLR type to scan.</param>
    /// <param name="knownSymbols">Known GraphQL symbol references for comparison.</param>
    /// <param name="isInputType">Indicates whether the type is being scanned as an input type (true) or output type (false).</param>
    public static TypeScanResult? Transform(ITypeSymbol typeSymbol, KnownSymbols knownSymbols, bool isInputType)
    {
        // Return null for types that cannot be examined
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsUnboundGenericType)
            return null;

        if (typeSymbol is ITypeParameterSymbol)
            return null;

        // Return null if type has unbound type parameters
        if (typeSymbol is INamedTypeSymbol named && named.TypeArguments.Any(arg => arg is ITypeParameterSymbol))
            return null;

        // Get members to scan based on MemberScan attribute
        var membersToScan = GetMembersToScan(typeSymbol, isInputType, knownSymbols);

        // Create separate builders for input and output discovered types
        var outputDiscoveredClrTypes = new List<ISymbol>();
        var inputDiscoveredClrTypes = new List<ISymbol>();
        var discoveredGraphTypes = new List<ISymbol>();
        var inputListTypes = new List<ISymbol>();

        // Inspect members to discover types
        foreach (var member in membersToScan)
        {
            ITypeSymbol? memberClrType = member switch
            {
                IPropertySymbol prop => prop.Type,
                IFieldSymbol field => field.Type,
                IMethodSymbol method => method.ReturnType,
                _ => null
            };

            if (memberClrType == null)
                continue;

            // Collect all list types at each level while unwrapping - only for input types
            if (isInputType)
                CollectListTypes(memberClrType, inputListTypes, knownSymbols);

            // Check if member has explicit GraphType override
            var memberGraphType = GetMemberGraphType(member, isInputType, knownSymbols);
            if (memberGraphType != null)
            {
                // Check if it's a GraphQLClrInputTypeReference<T> or GraphQLClrOutputTypeReference<T>
                var clrTypeRefSymbol = isInputType ? knownSymbols.GraphQLClrInputTypeReference : knownSymbols.GraphQLClrOutputTypeReference;
                if (TryExtractClrTypeReference(memberGraphType, clrTypeRefSymbol, out var clrType))
                {
                    // Unwrap the extracted CLR type to handle nullable types and other wrappers (invalid anyway; SchemaTypes will throw)
                    var unwrappedClrType2 = UnwrapClrType(clrType, knownSymbols);

                    // Extract T and add to appropriate discovered CLR types (with deduplication)
                    if (isInputType)
                        AddIfNotExists(inputDiscoveredClrTypes, unwrappedClrType2);
                    else
                        AddIfNotExists(outputDiscoveredClrTypes, unwrappedClrType2);
                }
                else
                {
                    // Add unwrapped GraphType to discoveredGraphTypes (with deduplication)
                    AddIfNotExists(discoveredGraphTypes, memberGraphType);
                }
            }
            else
            {
                // Unwrap nested generic wrappers (recursively)
                var unwrappedClrType = UnwrapClrType(memberClrType, knownSymbols);

                // Discover nested types - only add if not already in the list
                if (isInputType)
                    AddIfNotExists(inputDiscoveredClrTypes, unwrappedClrType);
                else
                    AddIfNotExists(outputDiscoveredClrTypes, unwrappedClrType);
            }

            // Inspect method parameters to discover input types (only for output types)
            if (!isInputType && member is IMethodSymbol methodSymbol)
            {
                foreach (var parameter in methodSymbol.Parameters)
                {
                    // Skip parameters that are injected from context (not GraphQL arguments)
                    if (ShouldSkipParameterProcessing(parameter, knownSymbols))
                        continue;

                    var parameterClrType = parameter.Type;

                    // Check if parameter type is a list type BEFORE unwrapping
                    CollectListTypes(parameterClrType, inputListTypes, knownSymbols);

                    // Check if parameter has explicit GraphType override
                    var parameterGraphType = GetMemberGraphType(parameter, true, knownSymbols);
                    if (parameterGraphType != null)
                    {
                        // Check if it's a GraphQLClrInputTypeReference<T>
                        if (TryExtractClrTypeReference(parameterGraphType, knownSymbols.GraphQLClrInputTypeReference, out var clrType))
                        {
                            // Unwrap the extracted CLR type
                            var unwrappedClrType2 = UnwrapClrType(clrType, knownSymbols);

                            // Extract T and add to inputDiscoveredClrTypes (with deduplication)
                            AddIfNotExists(inputDiscoveredClrTypes, unwrappedClrType2);
                        }
                        else
                        {
                            // Add unwrapped GraphType to discoveredGraphTypes (with deduplication)
                            AddIfNotExists(discoveredGraphTypes, parameterGraphType);
                        }
                    }
                    else
                    {
                        // Unwrap nested generic wrappers
                        var unwrappedParamType = UnwrapClrType(parameterClrType, knownSymbols);

                        // Method parameters are input types
                        AddIfNotExists(inputDiscoveredClrTypes, unwrappedParamType);
                    }
                }
            }
        }

        return new TypeScanResult(
            ScannedType: typeSymbol,
            DiscoveredInputClrTypes: inputDiscoveredClrTypes.ToImmutableEquatableArray(),
            DiscoveredOutputClrTypes: outputDiscoveredClrTypes.ToImmutableEquatableArray(),
            DiscoveredGraphTypes: discoveredGraphTypes.ToImmutableEquatableArray(),
            InputListTypes: inputListTypes.ToImmutableEquatableArray());
    }

    /// <summary>
    /// Recursively collects all list types at each level of nesting.
    /// </summary>
    private static void CollectListTypes(ITypeSymbol type, List<ISymbol> listTypes, KnownSymbols knownSymbols)
    {
        if (IsListType(type, knownSymbols))
        {
            // Only add if not already in the list
            AddIfNotExists(listTypes, type);

            // Get the element type and recursively check for nested lists
            if (type is IArrayTypeSymbol arrayType)
            {
                CollectListTypes(arrayType.ElementType, listTypes, knownSymbols);
            }
            else if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
            {
                CollectListTypes(namedType.TypeArguments[0], listTypes, knownSymbols);
            }
        }
    }

    /// <summary>
    /// Checks if a type is a list type (array or collection interface).
    /// </summary>
    private static bool IsListType(ITypeSymbol type, KnownSymbols knownSymbols)
    {
        if (type is IArrayTypeSymbol)
            return true;

        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            return false;

        var originalDef = namedType.OriginalDefinition;

        return SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.IEnumerableT) ||
               SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.IListT) ||
               SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.ListT) ||
               SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.ICollectionT) ||
               SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.IReadOnlyCollectionT) ||
               SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.IReadOnlyListT) ||
               SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.HashSetT) ||
               SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.ISetT);
    }

    /// <summary>
    /// Recursively unwraps nested generic wrappers to find the underlying CLR type.
    /// </summary>
    private static ITypeSymbol UnwrapClrType(ITypeSymbol type, KnownSymbols knownSymbols)
    {
        // Handle nullable reference types (e.g., string?, Class1?)
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            // Return the non-nullable version
            return UnwrapClrType(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated), knownSymbols);
        }

        if (type is IArrayTypeSymbol arrayType)
            return UnwrapClrType(arrayType.ElementType, knownSymbols);

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var originalDef = namedType.OriginalDefinition;

            // Handle Nullable<T>
            if (namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);

            // Handle Task<T>
            if (knownSymbols.TaskT != null && SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.TaskT) && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);

            // Handle ValueTask<T>
            if (knownSymbols.ValueTaskT != null && SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.ValueTaskT) && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);

            // Handle IDataLoaderResult<T>
            if (knownSymbols.IDataLoaderResultT != null && SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.IDataLoaderResultT) && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);

            // Handle recognized list types
            if (IsListType(type, knownSymbols) && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);
        }

        // Base case: return the unwrapped type
        return type;
    }

    /// <summary>
    /// Recursively unwraps ListGraphType and NonNullGraphType wrappers to find the underlying GraphType.
    /// </summary>
    private static ITypeSymbol UnwrapGraphType(ITypeSymbol graphType, KnownSymbols knownSymbols)
    {
        if (graphType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            // Handle NonNullGraphType<T>
            if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, knownSymbols.NonNullGraphType) &&
                namedType.TypeArguments.Length == 1)
            {
                return UnwrapGraphType(namedType.TypeArguments[0], knownSymbols);
            }

            // Handle ListGraphType<T>
            if (SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, knownSymbols.ListGraphType) &&
                namedType.TypeArguments.Length == 1)
            {
                return UnwrapGraphType(namedType.TypeArguments[0], knownSymbols);
            }
        }

        // Base case: return the unwrapped type
        return graphType;
    }

    /// <summary>
    /// Collects members (fields, properties, methods) that should be scanned for a CLR type.
    /// </summary>
    private static List<ISymbol> GetMembersToScan(ITypeSymbol clrType, bool isInputType, KnownSymbols knownSymbols)
    {
        var membersToScan = new List<ISymbol>();

        // Check for MemberScan attribute on the type
        var memberScanAttribute = clrType.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, knownSymbols.MemberScanAttribute));

        // Determine which member types to scan
        // Default: properties (and methods for output types only)
        bool scanFields = false;
        bool scanProperties = true;
        bool scanMethods = !isInputType; // Methods are scanned by default for output types

        if (memberScanAttribute != null)
        {
            // Extract MemberTypes value from the first constructor argument
            var memberTypesArg = memberScanAttribute.ConstructorArguments.FirstOrDefault();

            if (memberTypesArg.Value is int memberTypes)
            {
                // ScanMemberTypes enum: Properties = 1, Fields = 2, Methods = 4
                scanProperties = (memberTypes & 1) != 0;
                scanFields = (memberTypes & 2) != 0;
                scanMethods = (memberTypes & 4) != 0;
            }
        }

        // Walk up the inheritance hierarchy to collect members from base classes
        var currentType = clrType;
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            ScanTypeMembers(currentType, membersToScan, scanFields, scanProperties, scanMethods, isInputType, knownSymbols, clrType);

            // Move to the base type
            currentType = currentType.BaseType;
        }

        // For interfaces, collect members from all implemented interfaces also
        if (clrType.TypeKind == TypeKind.Interface && clrType is INamedTypeSymbol namedInterface)
        {
            foreach (var interfaceType in namedInterface.AllInterfaces)
            {
                ScanTypeMembers(interfaceType, membersToScan, scanFields, scanProperties, scanMethods, isInputType, knownSymbols, clrType);
            }
        }

        return membersToScan;
    }

    /// <summary>
    /// Scans a specific type for members and adds them to the members list.
    /// </summary>
    private static void ScanTypeMembers(ITypeSymbol typeToScan, List<ISymbol> membersToScan, bool scanFields, bool scanProperties, bool scanMethods, bool isInputType, KnownSymbols knownSymbols, ITypeSymbol originalType)
    {
        // Collect fields if requested
        if (scanFields)
        {
            foreach (var field in typeToScan.GetMembers().OfType<IFieldSymbol>())
            {
                // Only scan public fields
                if (field.DeclaredAccessibility != Accessibility.Public)
                    continue;

                if (ShouldSkipMember(field, knownSymbols))
                    continue;

                // Skip readonly fields for input types (can't be set)
                if (isInputType && field.IsReadOnly)
                    continue;

                membersToScan.Add(field);
            }
        }

        // Collect properties if requested
        if (scanProperties)
        {
            foreach (var property in typeToScan.GetMembers().OfType<IPropertySymbol>())
            {
                // Only scan public properties
                if (property.DeclaredAccessibility != Accessibility.Public)
                    continue;

                if (ShouldSkipMember(property, knownSymbols))
                    continue;

                // Skip read-only properties for input types (can't be set)
                if (isInputType && property.IsReadOnly)
                    continue;

                // Skip write-only properties for output types (can't be read)
                if (!isInputType && property.IsWriteOnly)
                    continue;

                membersToScan.Add(property);
            }
        }

        // Collect methods if requested (for output types)
        if (scanMethods && !isInputType)
        {
            foreach (var method in typeToScan.GetMembers().OfType<IMethodSymbol>())
            {
                // Only scan public methods
                if (method.DeclaredAccessibility != Accessibility.Public)
                    continue;

                if (ShouldSkipMember(method, knownSymbols))
                    continue;

                // Skip special methods (constructors, property accessors, etc.)
                if (method.MethodKind != MethodKind.Ordinary)
                    continue;

                // Skip void methods
                if (method.ReturnsVoid)
                    continue;

                // Skip methods that return Task (methods which do not return a value)
                if (knownSymbols.Task != null && SymbolEqualityComparer.Default.Equals(method.ReturnType, knownSymbols.Task))
                    continue;

                // Skip methods inherited from System.Object (e.g., GetHashCode, GetType, ToString, Equals)
                if (method.ContainingType?.SpecialType == SpecialType.System_Object)
                    continue;

                // For overridden methods, check if the base definition is from System.Object
                if (method.IsOverride && method.OverriddenMethod != null)
                {
                    var baseDefinition = method.OverriddenMethod;
                    while (baseDefinition.OverriddenMethod != null)
                        baseDefinition = baseDefinition.OverriddenMethod;

                    if (baseDefinition.ContainingType?.SpecialType == SpecialType.System_Object)
                        continue;
                }

                // Skip compiler-generated methods for record types
                // Exclude public virtual/override bool Equals(RECORD_TYPE)
                if (IsRecordEqualsMethod(method, originalType))
                    continue;

                // Exclude <Clone>$() method generated for record types
                if (method.Name == "<Clone>$")
                    continue;

                membersToScan.Add(method);
            }
        }
    }

    /// <summary>
    /// Determines if a property or field should be skipped during type discovery.
    /// </summary>
    private static bool ShouldSkipMember(ISymbol member, KnownSymbols knownSymbols)
    {
        // Skip if member has [Ignore] attribute
        foreach (var attribute in member.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, knownSymbols.IgnoreAttribute))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the explicit GraphType from a member's attributes and unwraps it.
    /// </summary>
    private static ITypeSymbol? GetMemberGraphType(ISymbol member, bool isInputType, KnownSymbols knownSymbols)
    {
        // Define which attributes to check based on input/output type
        INamedTypeSymbol? typeAttributeT, typeAttribute, baseTypeAttributeT, baseTypeAttribute;
        if (isInputType)
        {
            typeAttributeT = knownSymbols.InputTypeAttributeT;
            typeAttribute = knownSymbols.InputTypeAttribute;
            baseTypeAttributeT = knownSymbols.InputBaseTypeAttributeT;
            baseTypeAttribute = knownSymbols.InputBaseTypeAttribute;
        }
        else
        {
            typeAttributeT = knownSymbols.OutputTypeAttributeT;
            typeAttribute = knownSymbols.OutputTypeAttribute;
            baseTypeAttributeT = knownSymbols.OutputBaseTypeAttributeT;
            baseTypeAttribute = knownSymbols.OutputBaseTypeAttribute;
        }

        foreach (var attribute in member.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
                continue;

            // Check type-specific Type attribute (generic or non-generic, including derived types)
            var memberGraphType = TryGetGraphTypeFromAttribute(attributeClass, attribute, typeAttributeT, typeAttribute);
            if (memberGraphType != null)
                return UnwrapGraphType(memberGraphType, knownSymbols);

            // Check type-specific BaseType attribute (generic or non-generic, including derived types)
            memberGraphType = TryGetGraphTypeFromAttribute(attributeClass, attribute, baseTypeAttributeT, baseTypeAttribute);
            if (memberGraphType != null)
                return UnwrapGraphType(memberGraphType, knownSymbols);

            // Check BaseGraphType attribute (generic or non-generic, including derived types) - for both input and output types
            memberGraphType = TryGetGraphTypeFromAttribute(attributeClass, attribute, knownSymbols.BaseGraphTypeAttributeT, knownSymbols.BaseGraphTypeAttribute);
            if (memberGraphType != null)
                return UnwrapGraphType(memberGraphType, knownSymbols);
        }

        return null;
    }

    /// <summary>
    /// Determines if a parameter should be skipped during processing (i.e., injected from context).
    /// </summary>
    private static bool ShouldSkipParameterProcessing(IParameterSymbol parameter, KnownSymbols knownSymbols)
    {
        var parameterType = parameter.Type;

        // Check if parameter type is IResolveFieldContext (non-generic)
        if (knownSymbols.IResolveFieldContext != null &&
            SymbolEqualityComparer.Default.Equals(parameterType, knownSymbols.IResolveFieldContext))
            return true;

        // Check if parameter type is CancellationToken
        if (knownSymbols.CancellationToken != null &&
            SymbolEqualityComparer.Default.Equals(parameterType, knownSymbols.CancellationToken))
            return true;

        // Check for attributes that derive from ParameterAttribute
        foreach (var attribute in parameter.GetAttributes())
        {
            if (attribute.AttributeClass == null)
                continue;

            // Check for ParameterAttribute or derived classes (e.g., FromServicesAttribute)
            if (knownSymbols.ParameterAttribute != null)
            {
                var current = attribute.AttributeClass;
                while (current != null)
                {
                    if (SymbolEqualityComparer.Default.Equals(current, knownSymbols.ParameterAttribute))
                        return true;
                    current = current.BaseType;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to extract a GraphType from an attribute by checking if the attribute
    /// matches the generic type (or inherits from it), or is exactly the non-generic type.
    /// </summary>
    /// <param name="attributeClass">The attribute class to check.</param>
    /// <param name="attribute">The attribute data for accessing constructor arguments.</param>
    /// <param name="genericSymbol">The generic version symbol (e.g., InputTypeAttribute&lt;T&gt;).</param>
    /// <param name="nonGenericSymbol">The non-generic version symbol (e.g., InputTypeAttribute).</param>
    /// <returns>The GraphType if found, otherwise null.</returns>
    private static ITypeSymbol? TryGetGraphTypeFromAttribute(
        INamedTypeSymbol attributeClass,
        AttributeData attribute,
        INamedTypeSymbol? genericSymbol,
        INamedTypeSymbol? nonGenericSymbol)
    {
        // Walk the inheritance tree to find if this attribute or any base class matches the generic type
        var current = attributeClass;
        while (current != null)
        {
            // Check if this is the generic version
            if (genericSymbol != null && current.IsGenericType)
            {
                var originalDef = current.OriginalDefinition;
                if (SymbolEqualityComparer.Default.Equals(originalDef, genericSymbol) &&
                    current.TypeArguments.Length == 1)
                {
                    return current.TypeArguments[0];
                }
            }

            // Move to base class
            current = current.BaseType;
        }

        // Check if this attribute is exactly the non-generic version (not derived)
        if (nonGenericSymbol != null &&
            !attributeClass.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(attributeClass, nonGenericSymbol))
        {
            // Try to get the type from the first constructor argument
            var graphTypeArg = attribute.ConstructorArguments.FirstOrDefault();
            if (graphTypeArg.Value is ITypeSymbol typeValue)
                return typeValue;
        }

        return null;
    }

    /// <summary>
    /// Attempts to extract the CLR type from a GraphQLClrInputTypeReference&lt;T&gt; or GraphQLClrOutputTypeReference&lt;T&gt;.
    /// </summary>
    private static bool TryExtractClrTypeReference(ITypeSymbol graphType, INamedTypeSymbol? clrTypeRefSymbol, out ITypeSymbol clrType)
    {
        clrType = null!;

        if (clrTypeRefSymbol == null)
            return false;

        if (graphType is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, clrTypeRefSymbol) &&
            namedType.TypeArguments.Length == 1)
        {
            clrType = namedType.TypeArguments[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a method is a record-generated Equals method.
    /// Record types generate: public virtual bool Equals(RECORD_TYPE? other)
    /// </summary>
    private static bool IsRecordEqualsMethod(IMethodSymbol method, ITypeSymbol recordType)
    {
        // Check method name
        if (method.Name != "Equals")
            return false;

        // Check return type is bool
        if (method.ReturnType.SpecialType != SpecialType.System_Boolean)
            return false;

        // Check it has exactly one parameter of the record type
        if (method.Parameters.Length != 1)
            return false;

        var paramType = method.Parameters[0].Type;

        // Handle nullable reference types - unwrap to compare
        if (paramType.NullableAnnotation == NullableAnnotation.Annotated)
            paramType = paramType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

        return SymbolEqualityComparer.Default.Equals(paramType, recordType);
    }

    /// <summary>
    /// Adds a type symbol to the list if it doesn't already exist.
    /// Uses SymbolEqualityComparer for comparison.
    /// </summary>
    private static void AddIfNotExists(List<ISymbol> list, ISymbol typeToAdd)
    {
        foreach (var existingType in list)
        {
            if (SymbolEqualityComparer.Default.Equals(existingType, typeToAdd))
                return;
        }

        list.Add(typeToAdd);
    }
}
