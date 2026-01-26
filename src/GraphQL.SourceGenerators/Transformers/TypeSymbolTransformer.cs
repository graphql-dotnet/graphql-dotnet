using System.Collections.Immutable;
using GraphQL.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.SourceGenerators.Transformers;

/// <summary>
/// Scans a CLR input type and discovers its dependencies by examining properties and fields.
/// </summary>
public static class TypeSymbolTransformer
{
    /// <summary>
    /// Scans a CLR input type and discovers referenced CLR types, GraphTypes, and list types.
    /// Returns null if the type cannot be examined (e.g., open generic types).
    /// </summary>
    /// <param name="typeSymbol">The CLR type to scan.</param>
    /// <param name="knownSymbols">Known GraphQL symbol references for comparison.</param>
    /// <param name="isInputType">Indicates whether the type is being scanned as an input type.</param>
    public static InputTypeScanResult? Transform(ITypeSymbol typeSymbol, KnownSymbols knownSymbols, bool isInputType)
    {
        _ = isInputType;

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

        var discoveredClrTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var discoveredGraphTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();
        var inputListTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();

        // Inspect members to discover types
        foreach (var member in membersToScan)
        {
            ITypeSymbol? memberClrType = member switch
            {
                IPropertySymbol prop => prop.Type,
                IFieldSymbol field => field.Type,
                _ => null
            };

            if (memberClrType == null)
                continue;

            // Collect all list types at each level while unwrapping - do this regardless of GraphType attribute
            CollectListTypes(memberClrType, inputListTypes, knownSymbols);

            // Check if member has explicit GraphType override
            var memberGraphType = GetMemberGraphType(member, isInputType, knownSymbols);
            if (memberGraphType != null)
            {
                // Check if it's a GraphQLClrInputTypeReference<T>
                if (TryExtractClrTypeReference(memberGraphType, knownSymbols.GraphQLClrInputTypeReference, out var clrType))
                {
                    // Extract T and add to discoveredClrTypes (with deduplication)
                    bool clrRefTypeExists = false;
                    foreach (var existingClrType in discoveredClrTypes)
                    {
                        if (SymbolEqualityComparer.Default.Equals(existingClrType, clrType))
                        {
                            clrRefTypeExists = true;
                            break;
                        }
                    }

                    if (!clrRefTypeExists)
                    {
                        discoveredClrTypes.Add(clrType);
                    }
                }
                else
                {
                    // Add unwrapped GraphType to discoveredGraphTypes (with deduplication)
                    bool graphTypeExists = false;
                    foreach (var existingGraphType in discoveredGraphTypes)
                    {
                        if (SymbolEqualityComparer.Default.Equals(existingGraphType, memberGraphType))
                        {
                            graphTypeExists = true;
                            break;
                        }
                    }

                    if (!graphTypeExists)
                    {
                        discoveredGraphTypes.Add(memberGraphType);
                    }
                }

                continue;
            }

            // Unwrap nested generic wrappers (recursively)
            var unwrappedClrType = UnwrapClrType(memberClrType, knownSymbols);

            // Discover nested input types - only add if not already in the list
            bool clrTypeExists = false;
            foreach (var existingClrType in discoveredClrTypes)
            {
                if (SymbolEqualityComparer.Default.Equals(existingClrType, unwrappedClrType))
                {
                    clrTypeExists = true;
                    break;
                }
            }

            if (!clrTypeExists)
            {
                discoveredClrTypes.Add(unwrappedClrType);
            }
        }

        return new InputTypeScanResult(
            ScannedType: typeSymbol,
            DiscoveredClrTypes: discoveredClrTypes.ToImmutable(),
            DiscoveredGraphTypes: discoveredGraphTypes.ToImmutable(),
            InputListTypes: inputListTypes.ToImmutable());
    }

    /// <summary>
    /// Recursively collects all list types at each level of nesting.
    /// </summary>
    private static void CollectListTypes(ITypeSymbol type, ImmutableArray<ITypeSymbol>.Builder listTypes, KnownSymbols knownSymbols)
    {
        if (IsListType(type, knownSymbols))
        {
            // Only add if not already in the list
            bool alreadyExists = false;
            foreach (var existingType in listTypes)
            {
                if (SymbolEqualityComparer.Default.Equals(existingType, type))
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (!alreadyExists)
            {
                listTypes.Add(type);
            }

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
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.OriginalDefinition.ToDisplayString();

            // Handle Nullable<T>
            if (typeName == "System.Nullable<T>" && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);

            // Handle Task<T>
            if (typeName == "System.Threading.Tasks.Task<TResult>" && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);

            // Handle ValueTask<T>
            if (typeName == "System.Threading.Tasks.ValueTask<TResult>" && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);

            // Handle IDataLoaderResult<T>
            if (typeName == "GraphQL.DataLoader.IDataLoaderResult<T>" && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);

            // Handle recognized list types
            if (IsListType(type, knownSymbols) && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0], knownSymbols);
        }

        if (type is IArrayTypeSymbol arrayType)
            return UnwrapClrType(arrayType.ElementType, knownSymbols);

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
    /// Collects members (fields, properties) that should be scanned for a CLR type.
    /// </summary>
    private static ImmutableArray<ISymbol> GetMembersToScan(ITypeSymbol clrType, bool isInputType, KnownSymbols knownSymbols)
    {
        var membersToScan = ImmutableArray.CreateBuilder<ISymbol>();

        // Check for MemberScan attribute on the type
        var memberScanAttribute = clrType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "MemberScanAttribute");

        // Determine which member types to scan
        // Default: properties and methods (but methods are not valid for input types)
        bool scanFields = false;
        bool scanProperties = true;

        if (memberScanAttribute != null)
        {
            // Extract ScanFlags value if present
            var scanFlagsArg = memberScanAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "ScanFlags");

            if (scanFlagsArg.Value.Value is int scanFlags)
            {
                // Assuming: Fields = 1, Properties = 2, Methods = 4
                scanFields = (scanFlags & 1) != 0;
                scanProperties = (scanFlags & 2) != 0;
            }
        }

        // Collect fields if requested
        if (scanFields)
        {
            foreach (var field in clrType.GetMembers().OfType<IFieldSymbol>())
            {
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
            foreach (var property in clrType.GetMembers().OfType<IPropertySymbol>())
            {
                if (ShouldSkipMember(property, knownSymbols))
                    continue;

                // Skip read-only properties for input types (can't be set)
                if (isInputType && property.IsReadOnly)
                    continue;

                membersToScan.Add(property);
            }
        }

        return membersToScan.ToImmutable();
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
        if (!isInputType)
            return null;

        foreach (var attribute in member.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
                continue;

            // Check InputType attribute (generic or non-generic, including derived types)
            var memberGraphType = TryGetGraphTypeFromAttribute(
                attributeClass,
                attribute,
                knownSymbols.InputTypeAttributeT,
                knownSymbols.InputTypeAttribute);

            if (memberGraphType != null)
                return UnwrapGraphType(memberGraphType, knownSymbols);

            // Check InputBaseType attribute (generic or non-generic, including derived types)
            memberGraphType = TryGetGraphTypeFromAttribute(
                attributeClass,
                attribute,
                knownSymbols.InputBaseTypeAttributeT,
                knownSymbols.InputBaseTypeAttribute);

            if (memberGraphType != null)
                return UnwrapGraphType(memberGraphType, knownSymbols);

            // Check BaseGraphType attribute (generic or non-generic, including derived types)
            memberGraphType = TryGetGraphTypeFromAttribute(
                attributeClass,
                attribute,
                knownSymbols.BaseGraphTypeAttributeT,
                knownSymbols.BaseGraphTypeAttribute);

            if (memberGraphType != null)
                return UnwrapGraphType(memberGraphType, knownSymbols);
        }

        return null;
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
    /// Attempts to extract the CLR type from a GraphQLClrInputTypeReference&lt;T&gt;.
    /// </summary>
    private static bool TryExtractClrTypeReference(ITypeSymbol graphType, INamedTypeSymbol? clrInputTypeRefSymbol, out ITypeSymbol clrType)
    {
        clrType = null!;

        if (clrInputTypeRefSymbol == null)
            return false;

        if (graphType is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, clrInputTypeRefSymbol) &&
            namedType.TypeArguments.Length == 1)
        {
            clrType = namedType.TypeArguments[0];
            return true;
        }

        return false;
    }
}
