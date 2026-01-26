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
    public static InputTypeScanResult? Transform(ITypeSymbol typeSymbol, KnownSymbols knownSymbols)
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
        var membersToScan = GetMembersToScan(typeSymbol, isInputType: true);

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

            // Check if member has explicit GraphType override
            var memberGraphType = GetMemberGraphType(member, isInputType: true, knownSymbols);
            if (memberGraphType != null)
            {
                // Check if it's a GraphQLClrInputTypeReference<T>
                if (TryExtractClrTypeReference(memberGraphType, knownSymbols.GraphQLClrInputTypeReference, out var clrType))
                {
                    // Extract T and add to discoveredClrTypes
                    discoveredClrTypes.Add(clrType);
                }
                else
                {
                    // Add unwrapped GraphType to discoveredGraphTypes
                    discoveredGraphTypes.Add(memberGraphType);
                }

                continue;
            }

            // Collect all list types at each level while unwrapping
            CollectListTypes(memberClrType, inputListTypes);

            // Unwrap nested generic wrappers (recursively)
            var unwrappedClrType = UnwrapClrType(memberClrType);

            // Discover nested input types
            discoveredClrTypes.Add(unwrappedClrType);
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
    private static void CollectListTypes(ITypeSymbol type, ImmutableArray<ITypeSymbol>.Builder listTypes)
    {
        if (IsListType(type))
        {
            listTypes.Add(type);

            // Get the element type and recursively check for nested lists
            if (type is IArrayTypeSymbol arrayType)
            {
                CollectListTypes(arrayType.ElementType, listTypes);
            }
            else if (type is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
            {
                CollectListTypes(namedType.TypeArguments[0], listTypes);
            }
        }
    }

    /// <summary>
    /// Checks if a type is a list type (array or collection interface).
    /// </summary>
    private static bool IsListType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
            return true;

        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            return false;

        var typeName = namedType.OriginalDefinition.ToDisplayString();

        return typeName switch
        {
            "System.Collections.Generic.IEnumerable<T>" => true,
            "System.Collections.Generic.IList<T>" => true,
            "System.Collections.Generic.List<T>" => true,
            "System.Collections.Generic.IReadOnlyList<T>" => true,
            "System.Collections.Generic.ICollection<T>" => true,
            "System.Collections.Generic.IReadOnlyCollection<T>" => true,
            _ => false
        };
    }

    /// <summary>
    /// Recursively unwraps nested generic wrappers to find the underlying CLR type.
    /// </summary>
    private static ITypeSymbol UnwrapClrType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.OriginalDefinition.ToDisplayString();

            // Handle Nullable<T>
            if (typeName == "System.Nullable<T>" && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0]);

            // Handle Task<T>
            if (typeName == "System.Threading.Tasks.Task<TResult>" && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0]);

            // Handle ValueTask<T>
            if (typeName == "System.Threading.Tasks.ValueTask<TResult>" && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0]);

            // Handle IDataLoaderResult<T>
            if (typeName == "GraphQL.DataLoader.IDataLoaderResult<T>" && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0]);

            // Handle recognized list types
            if (IsListType(type) && namedType.TypeArguments.Length == 1)
                return UnwrapClrType(namedType.TypeArguments[0]);
        }

        if (type is IArrayTypeSymbol arrayType)
            return UnwrapClrType(arrayType.ElementType);

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
    private static ImmutableArray<ISymbol> GetMembersToScan(ITypeSymbol clrType, bool isInputType)
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
                if (ShouldSkipMember(field))
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
                if (ShouldSkipMember(property))
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
    private static bool ShouldSkipMember(ISymbol member)
    {
        // Skip if member has [Ignore] attribute
        foreach (var attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == "IgnoreAttribute")
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the explicit GraphType from a member's attributes and unwraps it.
    /// </summary>
    private static ITypeSymbol? GetMemberGraphType(ISymbol member, bool isInputType, KnownSymbols knownSymbols)
    {
        ITypeSymbol? memberGraphType = null;

        foreach (var attribute in member.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass == null)
                continue;

            // For input types, check input-specific attributes first
            if (isInputType)
            {
                // Check InputType<TGraphType> or InputType (non-generic)
                if (attributeClass.Name == "InputTypeAttribute")
                {
                    if (attributeClass.IsGenericType && attributeClass.TypeArguments.Length == 1)
                        memberGraphType = attributeClass.TypeArguments[0];
                    else if (!attributeClass.IsGenericType)
                    {
                        // Check for GraphType property in constructor or named arguments
                        var graphTypeArg = attribute.ConstructorArguments.FirstOrDefault();
                        if (graphTypeArg.Value is ITypeSymbol typeValue)
                            memberGraphType = typeValue;
                        else
                        {
                            var namedArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "GraphType");
                            if (namedArg.Value.Value is ITypeSymbol namedTypeValue)
                                memberGraphType = namedTypeValue;
                        }
                    }
                    break;
                }
                // Check InputBaseType<TGraphType> or InputBaseType
                else if (attributeClass.Name == "InputBaseTypeAttribute")
                {
                    if (attributeClass.IsGenericType && attributeClass.TypeArguments.Length == 1)
                        memberGraphType = attributeClass.TypeArguments[0];
                    else if (!attributeClass.IsGenericType)
                    {
                        var graphTypeArg = attribute.ConstructorArguments.FirstOrDefault();
                        if (graphTypeArg.Value is ITypeSymbol typeValue)
                            memberGraphType = typeValue;
                        else
                        {
                            var namedArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "GraphType");
                            if (namedArg.Value.Value is ITypeSymbol namedTypeValue)
                                memberGraphType = namedTypeValue;
                        }
                    }
                    break;
                }
                // Check BaseGraphType<TGraphType> or BaseGraphType
                else if (attributeClass.Name == "BaseGraphTypeAttribute")
                {
                    if (attributeClass.IsGenericType && attributeClass.TypeArguments.Length == 1)
                        memberGraphType = attributeClass.TypeArguments[0];
                    else if (!attributeClass.IsGenericType)
                    {
                        var graphTypeArg = attribute.ConstructorArguments.FirstOrDefault();
                        if (graphTypeArg.Value is ITypeSymbol typeValue)
                            memberGraphType = typeValue;
                        else
                        {
                            var namedArg = attribute.NamedArguments.FirstOrDefault(arg => arg.Key == "GraphType");
                            if (namedArg.Value.Value is ITypeSymbol namedTypeValue)
                                memberGraphType = namedTypeValue;
                        }
                    }
                    break;
                }
            }
        }

        if (memberGraphType == null)
            return null;

        // Unwrap the GraphType to get the base type
        return UnwrapGraphType(memberGraphType, knownSymbols);
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
