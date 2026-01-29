using GraphQL.Analyzers.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace GraphQL.Analyzers.SourceGenerators.Transformers;

/// <summary>
/// Transforms SchemaAttributeData and ProcessedSchemaData into primitive-only GeneratedTypeEntry instances.
/// This transformer yields one entry for the schema class itself, then one entry for each generated graph type.
/// </summary>
public static class ProcessedSchemaDataTransformer
{
    /// <summary>
    /// Transforms schema data into a collection of GeneratedTypeEntry instances containing only primitive data.
    /// </summary>
    /// <param name="processedData">The processed schema data containing discovered types and mappings.</param>
    /// <param name="knownSymbols">Known GraphQL symbol references for comparison.</param>
    /// <returns>An enumerable of GeneratedTypeEntry instances.</returns>
    public static IEnumerable<GeneratedTypeEntry> Transform(
        ProcessedSchemaData processedData,
        KnownSymbols knownSymbols)
    {
        // Create a name cache to ensure each graph type symbol gets a consistent unique name
        var nameCache = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        var usedNames = new Dictionary<string, int>(StringComparer.Ordinal);

        // Get schema class namespace and hierarchy
        var schemaClass = processedData.SchemaClass;
        string? schemaNamespace = schemaClass.ContainingNamespace?.ToDisplayString();
        if (schemaNamespace == "<global namespace>")
            schemaNamespace = null;
        var schemaHierarchy = GetPartialClassHierarchy(schemaClass).ToImmutableEquatableArray();

        // First, yield the schema class entry
        yield return new GeneratedTypeEntry(
            SchemaClass: TransformSchemaClass(processedData, knownSymbols, nameCache, usedNames),
            OutputGraphType: null,
            InputGraphType: null,
            Namespace: schemaNamespace,
            PartialClassHierarchy: schemaHierarchy);

        // Then, yield entries for each generated graph type
        foreach (var (graphType, members) in processedData.GeneratedGraphTypesWithMembers)
        {
            var graphTypeSymbol = (ITypeSymbol)graphType;

            // Check if this is an AutoRegistering type and extract the CLR type
            if (TryExtractAutoRegisteringType(graphTypeSymbol, knownSymbols.AutoRegisteringInputObjectGraphType, out var inputClrType))
            {
                yield return new GeneratedTypeEntry(
                    SchemaClass: null,
                    OutputGraphType: null,
                    InputGraphType: TransformInputGraphType(graphTypeSymbol, inputClrType, members, knownSymbols, nameCache, usedNames),
                    Namespace: schemaNamespace,
                    PartialClassHierarchy: schemaHierarchy);
            }
            else if (TryExtractAutoRegisteringType(graphTypeSymbol, knownSymbols.AutoRegisteringObjectGraphType, out var outputClrType) ||
                     TryExtractAutoRegisteringType(graphTypeSymbol, knownSymbols.AutoRegisteringInterfaceGraphType, out outputClrType))
            {
                yield return new GeneratedTypeEntry(
                    SchemaClass: null,
                    OutputGraphType: TransformOutputGraphType(graphTypeSymbol, outputClrType, members, knownSymbols, nameCache, usedNames),
                    InputGraphType: null,
                    Namespace: schemaNamespace,
                    PartialClassHierarchy: schemaHierarchy);
            }
        }
    }

    /// <summary>
    /// Transforms the schema class data into primitive-only SchemaClassData.
    /// </summary>
    private static SchemaClassData TransformSchemaClass(
        ProcessedSchemaData processedData,
        KnownSymbols knownSymbols,
        Dictionary<ISymbol, string> nameCache,
        Dictionary<string, int> usedNames)
    {
        var schemaClass = processedData.SchemaClass;

        // Check if schema class has a constructor defined in a class declaration with AOT attributes
        bool hasConstructor = HasConstructor(schemaClass, knownSymbols);

        // Build a lookup dictionary for RemapTypes
        var remapTypeLookup = new Dictionary<ITypeSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var (fromType, toType) in processedData.RemapTypes)
        {
            remapTypeLookup[(ITypeSymbol)fromType] = (ITypeSymbol)toType;
        }

        // Transform registered graph types
        var registeredGraphTypes = new List<RegisteredGraphTypeData>();
        foreach (var discoveredGraphType in processedData.DiscoveredGraphTypes)
        {
            var graphTypeSymbol = (ITypeSymbol)discoveredGraphType;
            var fullyQualifiedName = GetFullyQualifiedTypeName(graphTypeSymbol);

            // Check if this is an AOT-generated type (AutoRegisteringObjectGraphType, etc.)
            string? aotGeneratedTypeName = null;
            if (IsAutoRegisteringType(graphTypeSymbol, knownSymbols))
            {
                aotGeneratedTypeName = GetUniqueGraphTypeName(graphTypeSymbol, nameCache, usedNames);
            }

            // Check if this graph type has a remap override
            string? overrideTypeName = null;
            if (remapTypeLookup.TryGetValue(graphTypeSymbol, out var remappedToType))
            {
                overrideTypeName = GetFullyQualifiedTypeName(remappedToType);
            }

            // Get constructor data for non-AOT-generated types
            ConstructorData? constructorData = null;
            if (aotGeneratedTypeName == null)
            {
                // Use the remapped type symbol if available, otherwise use the original graph type symbol
                var symbolForConstructor = remappedToType ?? graphTypeSymbol;
                constructorData = GetConstructorData(symbolForConstructor, knownSymbols);
            }

            registeredGraphTypes.Add(new RegisteredGraphTypeData(
                FullyQualifiedGraphTypeName: fullyQualifiedName,
                AotGeneratedTypeName: aotGeneratedTypeName,
                OverrideTypeName: overrideTypeName,
                ConstructorData: constructorData));
        }

        // Transform type mappings
        var typeMappings = new List<TypeMappingData>();
        foreach (var (clrType, graphType) in processedData.OutputClrTypeMappings)
        {
            typeMappings.Add(new TypeMappingData(
                FullyQualifiedClrTypeName: GetFullyQualifiedTypeName((ITypeSymbol)clrType),
                FullyQualifiedGraphTypeName: GetFullyQualifiedTypeName((ITypeSymbol)graphType)));
        }
        foreach (var (clrType, graphType) in processedData.InputClrTypeMappings)
        {
            // Only add if not already present
            var clrTypeName = GetFullyQualifiedTypeName((ITypeSymbol)clrType);
            var graphTypeName = GetFullyQualifiedTypeName((ITypeSymbol)graphType);
            if (!typeMappings.Any(tm => tm.FullyQualifiedClrTypeName == clrTypeName && tm.FullyQualifiedGraphTypeName == graphTypeName))
            {
                typeMappings.Add(new TypeMappingData(
                    FullyQualifiedClrTypeName: clrTypeName,
                    FullyQualifiedGraphTypeName: graphTypeName));
            }
        }

        // Get root type names
        string? queryRootTypeName = processedData.QueryRootGraphType != null
            ? GetFullyQualifiedTypeName(processedData.QueryRootGraphType)
            : null;
        string? mutationRootTypeName = processedData.MutationRootGraphType != null
            ? GetFullyQualifiedTypeName(processedData.MutationRootGraphType)
            : null;
        string? subscriptionRootTypeName = processedData.SubscriptionRootGraphType != null
            ? GetFullyQualifiedTypeName(processedData.SubscriptionRootGraphType)
            : null;

        // Categorize list types (using HashSet to avoid duplicates)
        var arrayListTypes = new HashSet<ListElementTypeData>();
        var genericListTypes = new HashSet<ListElementTypeData>();
        var hashSetTypes = new HashSet<ListElementTypeData>();

        foreach (var listType in processedData.InputListTypes)
        {
            var listTypeSymbol = (ITypeSymbol)listType;

            if (listTypeSymbol is IArrayTypeSymbol arrayType)
            {
                var elementTypeData = CreateListElementTypeData(arrayType.ElementType);
                arrayListTypes.Add(elementTypeData);
            }
            else if (listTypeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var originalDef = namedType.OriginalDefinition;
                var elementTypeData = CreateListElementTypeData(namedType.TypeArguments[0]);

                if (SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.HashSetT) ||
                    SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.ISetT))
                {
                    hashSetTypes.Add(elementTypeData);
                }
                else if (SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.ListT))
                {
                    genericListTypes.Add(elementTypeData);
                }
                else
                {
                    arrayListTypes.Add(elementTypeData);
                }
            }
        }

        return new SchemaClassData(
            HasConstructor: hasConstructor,
            RegisteredGraphTypes: registeredGraphTypes.ToImmutableEquatableArray(),
            TypeMappings: typeMappings.ToImmutableEquatableArray(),
            QueryRootTypeName: queryRootTypeName,
            MutationRootTypeName: mutationRootTypeName,
            SubscriptionRootTypeName: subscriptionRootTypeName,
            ArrayListTypes: arrayListTypes.ToImmutableEquatableArray(),
            GenericListTypes: genericListTypes.ToImmutableEquatableArray(),
            HashSetTypes: hashSetTypes.ToImmutableEquatableArray());
    }

    /// <summary>
    /// Transforms an output graph type into OutputGraphTypeData.
    /// </summary>
    private static OutputGraphTypeData TransformOutputGraphType(
        ITypeSymbol graphTypeSymbol,
        ITypeSymbol clrType,
        ImmutableEquatableArray<ISymbol> members,
        KnownSymbols knownSymbols,
        Dictionary<ISymbol, string> nameCache,
        Dictionary<string, int> usedNames)
    {
        // Determine if it's an interface
        bool isInterface = clrType.TypeKind == TypeKind.Interface;

        // Get fully qualified CLR type name
        string fullyQualifiedClrTypeName = GetFullyQualifiedTypeName(clrType);

        // Get graph type class name
        string graphTypeClassName = GetUniqueGraphTypeName(graphTypeSymbol, nameCache, usedNames);

        // Transform members
        var selectedMembers = TransformOutputMembers(members, clrType, knownSymbols);

        // Get instance source
        var instanceSource = GetInstanceSource(clrType, knownSymbols);

        // Get constructor data if needed
        ConstructorData? constructorData = null;
        if (instanceSource == InstanceSource.GetServiceOrCreateInstance || instanceSource == InstanceSource.NewInstance)
        {
            constructorData = GetConstructorData(clrType, knownSymbols);
        }

        return new OutputGraphTypeData(
            IsInterface: isInterface,
            FullyQualifiedClrTypeName: fullyQualifiedClrTypeName,
            GraphTypeClassName: graphTypeClassName,
            SelectedMembers: selectedMembers.ToImmutableEquatableArray(),
            InstanceSource: instanceSource,
            ConstructorData: constructorData);
    }

    /// <summary>
    /// Transforms an input graph type into InputGraphTypeData.
    /// </summary>
    private static InputGraphTypeData TransformInputGraphType(
        ITypeSymbol graphTypeSymbol,
        ITypeSymbol clrType,
        ImmutableEquatableArray<ISymbol> members,
        KnownSymbols knownSymbols,
        Dictionary<ISymbol, string> nameCache,
        Dictionary<string, int> usedNames)
    {
        // Get fully qualified CLR type name
        string fullyQualifiedClrTypeName = GetFullyQualifiedTypeName(clrType);

        // Get graph type class name
        string graphTypeClassName = GetUniqueGraphTypeName(graphTypeSymbol, nameCache, usedNames);

        // Transform members
        var inputMembers = TransformInputMembers(members, clrType);

        // Get constructor parameters
        var constructorParameters = GetInputConstructorParameters(clrType, members, knownSymbols);

        return new InputGraphTypeData(
            FullyQualifiedClrTypeName: fullyQualifiedClrTypeName,
            GraphTypeClassName: graphTypeClassName,
            Members: inputMembers.ToImmutableEquatableArray(),
            ConstructorParameters: constructorParameters.ToImmutableEquatableArray());
    }

    /// <summary>
    /// Transforms output members into OutputMemberData instances.
    /// </summary>
    private static List<OutputMemberData> TransformOutputMembers(
        ImmutableEquatableArray<ISymbol> members,
        ITypeSymbol clrType,
        KnownSymbols knownSymbols)
    {
        var result = new List<OutputMemberData>();

        foreach (var member in members)
        {
            string? declaringTypeName = null;
            if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, clrType))
            {
                declaringTypeName = GetFullyQualifiedTypeName(member.ContainingType);
            }

            MemberKind memberKind;
            bool isStatic;
            bool isSourceStreamResolver = false;
            var methodParameters = ImmutableEquatableArray<MethodParameterData>.Empty;

            switch (member)
            {
                case IFieldSymbol field:
                    memberKind = MemberKind.Field;
                    isStatic = field.IsStatic;
                    break;
                case IPropertySymbol property:
                    memberKind = MemberKind.Property;
                    isStatic = property.IsStatic;
                    break;
                case IMethodSymbol method:
                    memberKind = MemberKind.Method;
                    isStatic = method.IsStatic;
                    isSourceStreamResolver = IsSourceStreamResolver(method, knownSymbols);
                    methodParameters = TransformMethodParameters(method);
                    break;
                default:
                    continue;
            }

            result.Add(new OutputMemberData(
                DeclaringTypeFullyQualifiedName: declaringTypeName,
                MemberName: member.Name,
                MemberKind: memberKind,
                IsStatic: isStatic,
                IsSourceStreamResolver: isSourceStreamResolver,
                MethodParameters: methodParameters));
        }

        return result;
    }

    /// <summary>
    /// Transforms method parameters into MethodParameterData instances.
    /// </summary>
    private static ImmutableEquatableArray<MethodParameterData> TransformMethodParameters(IMethodSymbol method)
    {
        var parameters = new List<MethodParameterData>();

        foreach (var parameter in method.Parameters)
        {
            parameters.Add(new MethodParameterData(
                FullyQualifiedTypeName: GetFullyQualifiedTypeName(parameter.Type)));
        }

        return parameters.ToImmutableEquatableArray();
    }

    /// <summary>
    /// Transforms input members into InputMemberData instances.
    /// </summary>
    private static List<InputMemberData> TransformInputMembers(
        ImmutableEquatableArray<ISymbol> members,
        ITypeSymbol clrType)
    {
        var result = new List<InputMemberData>();

        foreach (var member in members)
        {
            string? declaringTypeName = null;
            if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, clrType))
            {
                declaringTypeName = GetFullyQualifiedTypeName(member.ContainingType);
            }

            // Get the type of the member
            ITypeSymbol? memberType = member switch
            {
                IPropertySymbol property => property.Type,
                IFieldSymbol field => field.Type,
                _ => null
            };

            string fullyQualifiedTypeName = memberType != null
                ? GetFullyQualifiedTypeName(memberType)
                : "object";

            result.Add(new InputMemberData(
                DeclaringTypeFullyQualifiedName: declaringTypeName,
                MemberName: member.Name,
                FullyQualifiedTypeName: fullyQualifiedTypeName));
        }

        return result;
    }

    /// <summary>
    /// Gets constructor parameters for an input type.
    /// </summary>
    private static List<InputConstructorParameterData> GetInputConstructorParameters(
        ITypeSymbol clrType,
        ImmutableEquatableArray<ISymbol> members,
        KnownSymbols knownSymbols)
    {
        var result = new List<InputConstructorParameterData>();

        if (clrType is not INamedTypeSymbol namedType)
            return result;

        var constructor = IdentifyConstructor(namedType, knownSymbols);
        if (constructor == null)
            return result;

        // Create a set of member names for matching
        var memberNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var member in members)
        {
            memberNames.Add(member.Name);
        }

        foreach (var parameter in constructor.Parameters)
        {
            // Only include parameters that match a member
            if (memberNames.Contains(parameter.Name))
            {
                result.Add(new InputConstructorParameterData(
                    MemberName: parameter.Name));
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the instance source for a CLR type.
    /// </summary>
    private static InstanceSource GetInstanceSource(ITypeSymbol clrType, KnownSymbols knownSymbols)
    {
        if (knownSymbols.InstanceSourceAttribute == null)
            return InstanceSource.ContextSource;

        // Check for InstanceSourceAttribute on the type
        var attribute = clrType.GetAttributes()
            .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, knownSymbols.InstanceSourceAttribute));

        if (attribute == null)
            return InstanceSource.ContextSource;

        // Extract the InstanceSource enum value from the first constructor argument
        var instanceSourceArg = attribute.ConstructorArguments.FirstOrDefault();
        if (instanceSourceArg.Value is int intValue)
        {
            return (InstanceSource)intValue;
        }

        return InstanceSource.ContextSource;
    }

    /// <summary>
    /// Gets constructor data for a CLR type.
    /// </summary>
    private static ConstructorData? GetConstructorData(ITypeSymbol clrType, KnownSymbols knownSymbols)
    {
        if (clrType is not INamedTypeSymbol namedType)
            return null;

        var constructor = IdentifyConstructor(namedType, knownSymbols);
        if (constructor == null)
            return null;

        // Transform constructor parameters
        var parameters = new List<ConstructorParameterData>();
        foreach (var parameter in constructor.Parameters)
        {
            // Check if this is IResolveFieldContext
            string? fullyQualifiedTypeName = null;
            if (knownSymbols.IResolveFieldContext != null &&
                !SymbolEqualityComparer.Default.Equals(parameter.Type, knownSymbols.IResolveFieldContext))
            {
                fullyQualifiedTypeName = GetFullyQualifiedTypeName(parameter.Type);
            }

            parameters.Add(new ConstructorParameterData(
                FullyQualifiedTypeName: fullyQualifiedTypeName));
        }

        // Get required properties (public properties with init/set that are more visible than the class)
        var requiredProperties = new List<RequiredPropertyData>();
        foreach (var member in namedType.GetMembers().OfType<IPropertySymbol>())
        {
            // Skip if not public
            if (member.DeclaredAccessibility != Accessibility.Public)
                continue;

            // Skip if read-only
            if (member.IsReadOnly)
                continue;

            // Check if it's a required property or has init accessor
            bool isRequired = member.IsRequired || member.SetMethod?.IsInitOnly == true;

            if (isRequired)
            {
                requiredProperties.Add(new RequiredPropertyData(
                    Name: member.Name,
                    FullyQualifiedTypeName: GetFullyQualifiedTypeName(member.Type)));
            }
        }

        return new ConstructorData(
            Parameters: parameters.ToImmutableEquatableArray(),
            RequiredProperties: requiredProperties.ToImmutableEquatableArray());
    }

    /// <summary>
    /// Identifies the constructor to use when constructing instances of the type.
    /// </summary>
    private static IMethodSymbol? IdentifyConstructor(INamedTypeSymbol typeSymbol, KnownSymbols knownSymbols)
    {
        // Get public constructors, excluding implicit struct constructors for now
        var publicConstructors = typeSymbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsImplicitlyDeclared)
            .ToList();

        // Check for constructor marked with GraphQLConstructorAttribute
        if (knownSymbols.GraphQLConstructorAttribute != null)
        {
            var markedConstructor = publicConstructors.FirstOrDefault(c =>
                c.GetAttributes().Any(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, knownSymbols.GraphQLConstructorAttribute)));

            if (markedConstructor != null)
                return markedConstructor;
        }

        // Check for public parameterless constructor
        var parameterlessConstructor = publicConstructors.FirstOrDefault(c => c.Parameters.Length == 0);
        if (parameterlessConstructor != null)
            return parameterlessConstructor;

        // If there is only one explicit public constructor, use it
        if (publicConstructors.Count == 1)
            return publicConstructors[0];

        // For structs, fall back to implicit parameterless constructor
        if (typeSymbol.TypeKind == TypeKind.Struct)
        {
            var implicitConstructor = typeSymbol.Constructors
                .FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public &&
                                     c.IsImplicitlyDeclared &&
                                     c.Parameters.Length == 0);
            if (implicitConstructor != null)
                return implicitConstructor;
        }

        return null;
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
    /// Checks if a graph type is an auto-registering type.
    /// </summary>
    private static bool IsAutoRegisteringType(ITypeSymbol graphType, KnownSymbols knownSymbols)
    {
        if (graphType is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            return false;

        var originalDef = namedType.OriginalDefinition;

        return (knownSymbols.AutoRegisteringObjectGraphType != null &&
                SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.AutoRegisteringObjectGraphType)) ||
               (knownSymbols.AutoRegisteringInterfaceGraphType != null &&
                SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.AutoRegisteringInterfaceGraphType)) ||
               (knownSymbols.AutoRegisteringInputObjectGraphType != null &&
                SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.AutoRegisteringInputObjectGraphType));
    }

    /// <summary>
    /// Gets the partial class hierarchy for a type (innermost to outermost) with visibility information.
    /// </summary>
    private static List<PartialClassInfo> GetPartialClassHierarchy(ITypeSymbol type)
    {
        var hierarchy = new List<PartialClassInfo>();
        var current = type as INamedTypeSymbol;

        while (current != null)
        {
            var accessibility = current.DeclaredAccessibility switch
            {
                Accessibility.Public => ClassAccessibility.Public,
                Accessibility.Private => ClassAccessibility.Private,
                _ => ClassAccessibility.Internal
            };
            hierarchy.Add(new PartialClassInfo(current.Name, accessibility));
            current = current.ContainingType;
        }

        // Reverse to get outermost to innermost
        hierarchy.Reverse();
        return hierarchy;
    }

    /// <summary>
    /// Creates a ListElementTypeData from a type symbol, handling nullability based on whether it's a value or reference type.
    /// </summary>
    private static ListElementTypeData CreateListElementTypeData(ITypeSymbol elementType)
    {
        string elementTypeName;
        bool isNullable;

        // Check if the element type is a value type
        if (elementType.IsValueType)
        {
            // For value types, check if it's already nullable (e.g., int? which is Nullable<int>)
            elementTypeName = GetFullyQualifiedTypeName(elementType);
            isNullable = elementType is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }
        else
        {
            // For reference types, convert to nullable reference type by appending '?'
            elementTypeName = GetFullyQualifiedTypeName(elementType.WithNullableAnnotation(NullableAnnotation.Annotated));
            isNullable = true;
        }

        return new ListElementTypeData(elementTypeName, isNullable);
    }

    /// <summary>
    /// Gets the fully qualified type name for a type symbol.
    /// </summary>
    private static string GetFullyQualifiedTypeName(ITypeSymbol type)
    {
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>
    /// Gets the base class name for a graph type (for generated types), without uniqueness check.
    /// </summary>
    private static string GetGraphTypeName(ITypeSymbol graphType)
    {
        if (graphType is INamedTypeSymbol namedType)
        {
            // For generic types, create a name based on the CLR type
            if (namedType.IsGenericType && namedType.TypeArguments.Length == 1)
            {
                var clrType = namedType.TypeArguments[0];
                return $"{clrType.Name}GraphType";
            }

            return namedType.Name;
        }

        return graphType.Name;
    }

    /// <summary>
    /// Gets a unique class name for a graph type by tracking used names and appending numeric suffixes when needed.
    /// Uses a name cache to ensure the same graph type symbol always gets the same unique name.
    /// </summary>
    private static string GetUniqueGraphTypeName(
        ITypeSymbol graphType,
        Dictionary<ISymbol, string> nameCache,
        Dictionary<string, int> usedNames)
    {
        // Check if we've already assigned a name to this graph type symbol
        if (nameCache.TryGetValue(graphType, out string? cachedName))
        {
            return cachedName;
        }

        // Get the base name for this graph type
        string baseName = GetGraphTypeName(graphType);
        string uniqueName;

        // Check if this base name has been used before
        if (usedNames.TryGetValue(baseName, out int count))
        {
            // Name has been used, increment the counter and append it
            count++;
            usedNames[baseName] = count;
            uniqueName = $"{baseName}{count}";
        }
        else
        {
            // First time using this base name, track it
            usedNames[baseName] = 1;
            uniqueName = baseName;
        }

        // Cache this name for this graph type symbol
        nameCache[graphType] = uniqueName;
        return uniqueName;
    }

    /// <summary>
    /// Checks if the schema class has public constructors that are not marked with GraphQLConstructorAttribute.
    /// </summary>
    private static bool HasConstructor(INamedTypeSymbol schemaClass, KnownSymbols knownSymbols)
    {
        // Count all public constructors (excluding implicit ones) that are NOT marked with GraphQLConstructorAttribute
        var count = schemaClass.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsImplicitlyDeclared)
            .Count(c => knownSymbols.GraphQLConstructorAttribute == null ||
                        !c.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, knownSymbols.GraphQLConstructorAttribute)));

        return count > 0;
    }

    /// <summary>
    /// Checks if a method is a source stream resolver based on its return type.
    /// Returns true if the return type matches:
    /// IObservable&lt;T&gt;, Task&lt;IObservable&lt;T&gt;&gt;, ValueTask&lt;IObservable&lt;T&gt;&gt;,
    /// IAsyncEnumerable&lt;T&gt;, Task&lt;IAsyncEnumerable&lt;T&gt;&gt;, ValueTask&lt;IAsyncEnumerable&lt;T&gt;&gt;
    /// </summary>
    private static bool IsSourceStreamResolver(IMethodSymbol method, KnownSymbols knownSymbols)
    {
        var returnType = method.ReturnType;

        // Check direct match: IObservable<T> or IAsyncEnumerable<T>
        if (IsStreamType(returnType, knownSymbols))
            return true;

        // Check wrapped in Task<T> or ValueTask<T>
        if (returnType is INamedTypeSymbol namedReturnType && namedReturnType.IsGenericType)
        {
            var originalDef = namedReturnType.OriginalDefinition;

            // Check if it's Task<T> or ValueTask<T>
            if ((knownSymbols.TaskT != null && SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.TaskT)) ||
                (knownSymbols.ValueTaskT != null && SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.ValueTaskT)))
            {
                // Check if the type argument is IObservable<T> or IAsyncEnumerable<T>
                if (namedReturnType.TypeArguments.Length == 1)
                {
                    return IsStreamType(namedReturnType.TypeArguments[0], knownSymbols);
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Helper method to check if a type is IObservable&lt;T&gt; or IAsyncEnumerable&lt;T&gt;.
    /// </summary>
    private static bool IsStreamType(ITypeSymbol type, KnownSymbols knownSymbols)
    {
        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType)
            return false;

        var originalDef = namedType.OriginalDefinition;

        return (knownSymbols.IObservableT != null && SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.IObservableT)) ||
               (knownSymbols.IAsyncEnumerableT != null && SymbolEqualityComparer.Default.Equals(originalDef, knownSymbols.IAsyncEnumerableT));
    }
}
