using System.Diagnostics;
using GraphQL.Conversion;
using GraphQL.Introspection;
using GraphQL.Utilities;
using GraphQL.Utilities.Visitors;
using GraphQLParser;

namespace GraphQL.Types;

/// <summary>
/// A new implementation of <see cref="SchemaTypes"/> that represents a collection of all graph types
/// utilized by a schema. This implementation follows the comprehensive specifications for type discovery,
/// registration, validation, and initialization.
/// </summary>
public class NewSchemaTypes : SchemaTypes
{
    private readonly ISchema _schema;
    private readonly IServiceProvider _serviceProvider;
    private readonly IEnumerable<IGraphTypeMappingProvider>? _graphTypeMappings;
    private readonly Action<IGraphType>? _onBeforeInitialize;

    // Set of base GraphQL types that may be used to build a schema manually, and as such must not use type mapping
    private static readonly HashSet<Type> _baseTypes = new()
    {
        typeof(ObjectGraphType),
        typeof(InterfaceGraphType),
        typeof(UnionGraphType),
        typeof(EnumerationGraphType),
        typeof(InputObjectGraphType),
        typeof(GraphQLTypeReference)
    };

    // Secondary storage for CLR type to GraphType mapping during initialization
    private readonly Dictionary<Type, IGraphType> _typeDictionary = new();

    /// <summary>
    /// Initializes a new instance of <see cref="NewSchemaTypes"/>.
    /// </summary>
    /// <param name="schema">The schema instance being initialized.</param>
    /// <param name="serviceProvider">DI container for resolving graph types.</param>
    /// <param name="graphTypeMappings">Custom CLR-to-GraphQL type mappings.</param>
    /// <param name="onBeforeInitialize">Pre-initialization hook called before each type is initialized.</param>
    public NewSchemaTypes(
        ISchema schema,
        IServiceProvider serviceProvider,
        IEnumerable<IGraphTypeMappingProvider>? graphTypeMappings = null,
        Action<IGraphType>? onBeforeInitialize = null)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _graphTypeMappings = graphTypeMappings;
        _onBeforeInitialize = onBeforeInitialize;

        // Phase 1: Discovery - collect all types without processing
        DiscoverTypes();

        // Phase 2: Processing - process all discovered types
        ProcessTypes();

        // Phase 3: Finalization - apply type references and cleanup
        FinalizeTypes();

        // Clean up temporary data structures
        _typeDictionary = null!;
        _graphTypeMappings = null;
        _onBeforeInitialize = null;
        _schema = null!;
        _serviceProvider = null!;
    }

    /// <inheritdoc/>
    protected internal override Dictionary<ROM, IGraphType> Dictionary { get; } = new();

    /// <summary>
    /// Phase 1: Discovers all types from multiple sources without processing them.
    /// </summary>
    private void DiscoverTypes()
    {
        if (_schema.Query == null)
            throw new InvalidOperationException("Query root type must be provided. See https://spec.graphql.org/October2021/#sec-Schema-Introspection");

        // 1. Register introspection types without processing
        foreach (var introspectionType in GetIntrospectionTypes())
        {
            AddType(introspectionType, introspectionType.GetType(), skipProcessing: true);
        }

        // 2. Register manually-added types without processing
        foreach (var instance in _schema.AdditionalTypeInstances)
        {
            AddType(instance, skipProcessing: true);
        }

        // 3. Register root operation types without processing
        AddType(_schema.Query, skipProcessing: true);

        if (_schema.Mutation != null)
        {
            AddType(_schema.Mutation, skipProcessing: true);
        }

        if (_schema.Subscription != null)
        {
            AddType(_schema.Subscription, skipProcessing: true);
        }

        // 4. Register type references from AdditionalTypes without processing
        foreach (var type in _schema.AdditionalTypes)
        {
            var (graphType, type2) = ResolveType(type);
            AddType(graphType, type2, skipProcessing: true);
        }
    }

    /// <summary>
    /// Phase 2: Processes all discovered types.
    /// </summary>
    private void ProcessTypes()
    {
        // Process all types that have been discovered
        var typesToProcess = Dictionary.Values.ToList(); // snapshot to avoid modification during iteration
        foreach (var type in typesToProcess)
        {
            ProcessType(type);
        }

        // Process meta fields on query type
        // Note: Meta fields must not have their field names translated by INameConverter
        ProcessField(SchemaMetaFieldType, null, null);
        ProcessField(TypeMetaFieldType, null, null);
        ProcessField(TypeNameMetaFieldType, null, null);

        // Process directives
        var nameConverter = _schema.NameConverter;
        foreach (var directive in _schema.Directives.List)
        {
            NameValidator.ValidateNameOnSchemaInitialize(directive.Name, NamedElement.Directive);
            ProcessArguments(directive.Arguments, nameConverter, null, null, directive);
        }
    }

    /// <summary>
    /// Gets all introspection types required by the GraphQL specification.
    /// </summary>
    private IEnumerable<IGraphType> GetIntrospectionTypes()
    {
        var allowAppliedDirectives = _schema.Features.AppliedDirectives;
        var allowRepeatable = _schema.Features.RepeatableDirectives;
        var deprecationOfInputValues = _schema.Features.DeprecationOfInputValues;

        // Yield enum types (no constructor parameters)
        yield return new __DirectiveLocation();
        yield return new __TypeKind();

        // Yield types with constructor parameters
        yield return new __EnumValue(allowAppliedDirectives);
        yield return new __Directive(allowAppliedDirectives, allowRepeatable);
        yield return new __Field(allowAppliedDirectives, deprecationOfInputValues);
        yield return new __InputValue(allowAppliedDirectives, deprecationOfInputValues);
        yield return new __Type(allowAppliedDirectives, deprecationOfInputValues);
        yield return new __Schema(allowAppliedDirectives);

        // Yield applied directive types only if the feature is enabled
        if (allowAppliedDirectives)
        {
            yield return new __DirectiveArgument();
            yield return new __AppliedDirective();
        }
    }

    /// <summary>
    /// Adds a type to the collection and optionally processes it immediately.
    /// </summary>
    /// <param name="type">The GraphType instance to add.</param>
    /// <param name="clrType">Optional CLR type that was used to resolve this GraphType.</param>
    /// <param name="skipProcessing">If true, the type will be registered but not processed. Processing can be done later by calling ProcessType.</param>
    private void AddType(IGraphType type, Type? clrType = null, bool skipProcessing = false)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        // Skip GraphQLTypeReference - these will be resolved during finalization
        if (type is GraphQLTypeReference)
            return;

        // Don't register wrapper types directly
        if (type is NonNullGraphType || type is ListGraphType)
            throw new ArgumentException(
                "Cannot register NonNullGraphType or ListGraphType directly. These are created automatically.",
                nameof(type));

        var typeName = type.Name;

        // Validate the name
        if (!IsIntrospectionType(type))
            NameValidator.ValidateNameOnSchemaInitialize(typeName, NamedElement.Type);

        // Check for duplicate registration
        if (Dictionary.TryGetValue(typeName, out var existing))
        {
            // Allow same instance to be registered multiple times (idempotent)
            if (ReferenceEquals(existing, type))
                return;

            // Allow known scalars (built-in scalars provided with GraphQL.NET) to use duplicated instances
            if (existing.GetType() == type.GetType() && type is ScalarGraphType && BuiltInCustomScalars.ContainsKey(existing.GetType()))
                return;

            // Check if types are different classes with same name
            if (existing.GetType() != type.GetType())
            {
                throw new InvalidOperationException(
                    $"Unable to register GraphType '{type.GetType().GetFriendlyName()}' with the name '{typeName}'. " +
                    $"The name '{typeName}' is already registered to '{existing.GetType().GetFriendlyName()}'. " +
                    $"Check your schema configuration.");
            }

            // Same type class but different instances
            throw new InvalidOperationException(
                $"A different instance of the GraphType '{type.GetType().GetFriendlyName()}' with the name '{typeName}' " +
                $"has already been registered within the schema. Please use the same instance for all references within the schema, " +
                $"or use {nameof(GraphQLTypeReference)} to reference a type instantiated elsewhere." + @"
" +
                $"To view additional trace enable {nameof(GlobalSwitches)}.{nameof(GlobalSwitches.TrackGraphTypeInitialization)} switch.");
        }

        Dictionary[typeName] = type;

        // Map the CLR type (if provided) or the GraphType's own type if it's not a built-in GraphQL type
        if (clrType != null || !_baseTypes.Contains(type.GetType()))
        {
            var typeKey = clrType ?? type.GetType();
            Debug.WriteLine($"Storing mapping from CLR type '{typeKey.GetFriendlyName()}' to GraphType '{typeName}' of type '{type.GetType().GetFriendlyName()}'");
            _typeDictionary[typeKey] = type;
        }
        else
        {
            Debug.WriteLine($"Not storing mapping for GraphType '{typeName}' of type '{type.GetType().GetFriendlyName()}' as it is a built-in GraphQL type");
        }
        //if (clrType != null)
        //{
        //    Debug.WriteLine($"Storing mapping from CLR type '{clrType.GetFriendlyName()}' to GraphType '{typeName}' of type '{type.GetType().GetFriendlyName()}'");
        //    _typeDictionary[clrType] = type;
        //}
        //else
        //{
        //    Debug.WriteLine($"Not storing mapping for GraphType '{typeName}' of type '{type.GetType().GetFriendlyName()}' as no CLR type was provided");
        //}

        // For scalar types that derive from built-in scalars, also register the base type
        // if the Name matches (indicating it's an override, not a new type)
        if (type is ScalarGraphType)
        {
            var baseType = type.GetType().BaseType;
            while (baseType != null && baseType != typeof(ScalarGraphType) && baseType != typeof(object))
            {
                if (BuiltInCustomScalars.ContainsKey(baseType))
                {
                    // Check if the name matches the built-in scalar's name
                    var builtInInstance = GetBuiltInScalar(baseType);
                    if (builtInInstance.Name == type.Name)
                    {
                        _typeDictionary[baseType] = type;
                    }
                }
                baseType = baseType.BaseType;
            }
        }

        // Call the pre-initialization hook, allowing changes to the type before initialization
        _onBeforeInitialize?.Invoke(type);

        // Process the type immediately after adding it (unless skipProcessing is true)
        if (!skipProcessing)
        {
            ProcessType(type);
        }
    }

    /// <summary>
    /// Processes a single graph type, discovering and registering referenced types.
    /// </summary>
    private void ProcessType(IGraphType type)
    {
        if (type is IComplexGraphType complexType)
            ProcessComplexType(complexType);

        if (type is IImplementInterfaces implementer)
            ProcessImplementInterfaces(implementer);

        if (type is IAbstractGraphType abstractType)
            ProcessAbstractType(abstractType);

        if (type is EnumerationGraphType enumGraphType)
        {
            foreach (var value in enumGraphType.Values)
            {
                NameValidator.ValidateNameOnSchemaInitialize(value.Name, NamedElement.EnumValue);
            }
        }
    }

    /// <summary>
    /// Processes a complex type (object, interface, or input object) by processing its fields and arguments.
    /// </summary>
    private void ProcessComplexType(IComplexGraphType complexType)
    {
        if (complexType.Fields == null)
            return;

        var nameConverter = GetNameConverter(complexType);

        foreach (var field in complexType.Fields.List)
        {
            ProcessField(field, complexType, nameConverter);
        }
    }

    /// <summary>
    /// Processes a single field by applying name conversion and resolving its type and arguments.
    /// </summary>
    /// <param name="field">The field to process.</param>
    /// <param name="parentType">The parent complex type, or null for meta fields.</param>
    /// <param name="nameConverter">The name converter to use.</param>
    private void ProcessField(FieldType field, IComplexGraphType? parentType, INameConverter? nameConverter)
    {
        // Apply name conversion
        if (nameConverter != null)
        {
            field.Name = nameConverter.NameForField(field.Name, parentType!);
            NameValidator.ValidateNameOnSchemaInitialize(field.Name, NamedElement.Field);
        }

        // Resolve field type
        if (field.ResolvedType == null)
        {
            if (field.Type == null)
            {
                throw new InvalidOperationException(
                    $"Field '{parentType?.Name}.{field.Name}' must have either Type or ResolvedType set.");
            }

            try
            {
                field.ResolvedType = BuildGraphQLType(field.Type);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"The GraphQL type for field '{parentType?.Name}.{field.Name}' could not be derived implicitly. {ex.Message}", ex);
            }
        }
        else
        {
            // ResolvedType is already set, ensure it's registered
            AddType(field.ResolvedType.GetNamedType());
        }

        // Process field arguments
        ProcessArguments(field.Arguments, nameConverter, parentType, field, null);
    }

    /// <summary>
    /// Processes a collection of query arguments by applying name conversion and resolving their types.
    /// </summary>
    private void ProcessArguments(QueryArguments? arguments, INameConverter? nameConverter, IComplexGraphType? parentType, FieldType? field, Directive? directive)
    {
        if (arguments?.List == null)
            return;

        foreach (var argument in arguments.List)
        {
            // Apply name conversion for field arguments, not directive arguments
            if (parentType != null && field != null && nameConverter != null)
                argument.Name = nameConverter.NameForArgument(argument.Name, parentType, field);
            NameValidator.ValidateNameOnSchemaInitialize(argument.Name, NamedElement.Argument);

            // Resolve argument type
            if (argument.ResolvedType == null)
            {
                if (argument.Type == null)
                {
                    var context = directive != null
                        ? $"Directive '{directive.Name}' argument '{argument.Name}'"
                        : field != null
                            ? $"Argument '{parentType?.Name}.{field.Name}({argument.Name})'"
                            : $"Argument '{argument.Name}'";
                    throw new InvalidOperationException($"{context} must have either Type or ResolvedType set.");
                }

                argument.ResolvedType = BuildGraphQLType(argument.Type);
            }
            else
            {
                // ResolvedType is already set, ensure it's registered
                AddType(argument.ResolvedType.GetNamedType());
            }
        }
    }

    /// <summary>
    /// Processes types that implement interfaces (objects and interfaces) by resolving their interface references.
    /// </summary>
    private void ProcessImplementInterfaces(IImplementInterfaces implementer)
    {
        // Process ResolvedInterfaces collection (already registered GraphType instances)
        foreach (var resolvedInterface in implementer.ResolvedInterfaces.List)
        {
            // Skip references - they will be resolved during finalization
            if (resolvedInterface is GraphQLTypeReference)
                continue;

            // Ensure the interface is registered
            AddType(resolvedInterface);

            // For object types, add them as possible types to the interface
            if (implementer is IObjectGraphType objectType)
            {
                resolvedInterface.PossibleTypes.Add(objectType);
            }
        }

        // Process Interfaces collection (CLR types only)
        foreach (var iface in implementer.Interfaces.List)
        {
            if (iface is Type clrType)
            {
                try
                {
                    var resolved = BuildGraphQLType(clrType, allowWrappers: false);
                    if (resolved is not IInterfaceGraphType resolvedInterface)
                    {
                        throw new InvalidOperationException($"The resolved type is not an {nameof(IInterfaceGraphType)}.");
                    }
                    implementer.ResolvedInterfaces.Add(resolvedInterface);

                    // For object types, add them as possible types to the interface
                    if (implementer is IObjectGraphType objectType)
                    {
                        resolvedInterface.PossibleTypes.Add(objectType);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"The GraphQL implemented type '{clrType.GetFriendlyName()}' for graph type '{implementer.Name}' could not be derived implicitly. {ex.Message}", ex);
                }
            }
        }
    }

    /// <summary>
    /// Processes abstract types (interfaces and unions) by resolving their possible types.
    /// </summary>
    private void ProcessAbstractType(IAbstractGraphType abstractType)
    {
        // Process PossibleTypes collection (already registered GraphType instances)
        foreach (var possibleType in abstractType.PossibleTypes.List)
        {
            // Skip references - they will be resolved during finalization
            if (possibleType is GraphQLTypeReference)
                continue;

            AddType(possibleType);
        }

        // Process Types collection (CLR types that need to be resolved)
        foreach (var clrType in abstractType.Types.ToList())
        {
            try
            {
                var resolved = BuildGraphQLType(clrType, allowWrappers: false);
                if (resolved is not IObjectGraphType resolvedObject)
                {
                    throw new InvalidOperationException($"The resolved type is not an {nameof(IObjectGraphType)}.");
                }
                abstractType.PossibleTypes.Add(resolvedObject);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"The GraphQL type '{clrType.GetFriendlyName()}' for {(abstractType is IInterfaceGraphType ? "interface" : "union")} graph type '{abstractType.Name}' could not be derived implicitly. {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Phase 3: Finalizes types by replacing type references, inheriting interface descriptions and initializing all types.
    /// </summary>
    private void FinalizeTypes()
    {
        // Replace GraphQLTypeReference instances with actual types from the dictionary
        new TypeReferenceReplacementVisitor(Dictionary, BuiltInCustomScalars.Values.ToDictionary(x => x.Name), _schema).Run();

        // Inherit interface field descriptions to implementing types
        InheritInterfaceDescriptions();

        // Initialize all types
        foreach (var type in Dictionary.Values)
        {
            type.Initialize(_schema);
        }
    }

    /// <summary>
    /// Inherits field descriptions from interfaces to implementing types.
    /// </summary>
    private void InheritInterfaceDescriptions()
    {
        foreach (var type in Dictionary.Values)
        {
            if (type is IImplementInterfaces implementation && implementation.ResolvedInterfaces.Count > 0)
            {
                if (type is IComplexGraphType complexType)
                {
                    foreach (var field in complexType.Fields.List)
                    {
                        if (string.IsNullOrWhiteSpace(field.Description))
                        {
                            // Search for description in implemented interfaces
                            foreach (var iface in implementation.ResolvedInterfaces.List)
                            {
                                var interfaceField = iface.GetField(field.Name);
                                if (interfaceField?.Description != null)
                                {
                                    field.Description = interfaceField.Description;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resolves a CLR graph type to a GraphQL type instance.
    /// Returns the resolved instance and the original CLR type used for resolution.
    /// The original CLR type may differ from <paramref name="type"/> if
    /// <see cref="GraphQLClrOutputTypeReference{T}"/> or <see cref="GraphQLClrInputTypeReference{T}"/> was used.
    /// Be sure to pass the returned CLR type to <see cref="AddType(IGraphType, Type?, bool)"/> when registering the resolved instance.
    /// </summary>
    private (IGraphType Instance, Type Type) ResolveType(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (typeof(NonNullGraphType).IsAssignableFrom(type) || typeof(ListGraphType).IsAssignableFrom(type))
        {
            throw new InvalidOperationException(
                "Cannot resolve NonNullGraphType or ListGraphType directly. These are created automatically.");
        }

        // Resolve any CLR type references within the type
        type = ResolveClrTypeReferences(type);

        // Check if already registered
        if (_typeDictionary.TryGetValue(type, out var existing))
            return (existing, type);

        // Try to get from service provider
        var instance = _serviceProvider.GetService(type) as IGraphType;

        if (instance == null)
        {
            // Check if it's a built-in scalar
            if (BuiltInCustomScalars.TryGetValue(type, out var scalar))
            {
                instance = scalar;
            }
        }

        // Throw if we couldn't resolve to a concrete type
        if (instance == null)
        {
            throw new InvalidOperationException(
                $"Cannot resolve type '{type.GetFriendlyName()}' to a concrete GraphQL type. Ensure the type is registered in the service provider or is a built-in scalar type.");
        }

        return (instance, type);
    }

    /// <summary>
    /// Recursively resolves CLR type references within a type, replacing <see cref="GraphQLClrOutputTypeReference{T}"/>
    /// and <see cref="GraphQLClrInputTypeReference{T}"/> with their mapped GraphQL types.
    /// This handles both direct CLR type references and those nested within generic type arguments.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>The resolved type with all CLR type references replaced.</returns>
    private Type ResolveClrTypeReferences(Type type)
    {
        if (!type.IsGenericType)
            return type;

        var genericDef = type.GetGenericTypeDefinition();

        // Handle direct CLR type references
        if (genericDef == typeof(GraphQLClrOutputTypeReference<>) ||
            genericDef == typeof(GraphQLClrInputTypeReference<>))
        {
            var clrType = type.GetGenericArguments()[0];
            return GetGraphTypeFromClrType(clrType, genericDef == typeof(GraphQLClrInputTypeReference<>));
        }

        // Recursively process generic arguments to handle nested CLR type references
        var genericArgs = type.GetGenericArguments();
        bool needsRebuild = false;
        var newGenericArgs = new Type[genericArgs.Length];

        for (int i = 0; i < genericArgs.Length; i++)
        {
            var arg = genericArgs[i];
            var resolvedArg = ResolveClrTypeReferences(arg);
            newGenericArgs[i] = resolvedArg;
            if (resolvedArg != arg)
                needsRebuild = true;
        }

        // Rebuild the generic type if any arguments were resolved
        if (needsRebuild)
        {
            return genericDef.MakeGenericType(newGenericArgs);
        }

        return type;
    }

    /// <summary>
    /// Builds a GraphQL type from a CLR type, handling wrappers (NonNull, List).
    /// </summary>
    private IGraphType BuildGraphQLType(Type type, bool allowWrappers = true)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        // Handle NonNullGraphType<T>
        if (allowWrappers && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(NonNullGraphType<>))
        {
            var innerType = type.GetGenericArguments()[0];
            var resolvedInner = BuildGraphQLType(innerType);
            return new NonNullGraphType(resolvedInner);
        }

        // Handle ListGraphType<T>
        if (allowWrappers && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ListGraphType<>))
        {
            var innerType = type.GetGenericArguments()[0];
            var resolvedInner = BuildGraphQLType(innerType);
            return new ListGraphType(resolvedInner);
        }

        // Resolve as a regular graph type
        var (resolved, type2) = ResolveType(type);
        AddType(resolved, type2);
        return resolved;
    }

    /// <summary>
    /// Gets a GraphQL type from a CLR type using mapping providers and built-in mappings.
    /// </summary>
    private Type GetGraphTypeFromClrType(Type clrType, bool isInputType)
    {
        // Check schema explicit type mappings
        foreach (var (mappedClrType, mappedGraphType) in _schema.TypeMappings)
        {
            if (mappedClrType == clrType && (isInputType ? mappedGraphType.IsInputType() : mappedGraphType.IsOutputType()))
                return mappedGraphType;
        }

        // Check custom mapping providers next
        if (_graphTypeMappings != null)
        {
            Type? graphType = null;

            foreach (var provider in _graphTypeMappings)
                graphType = provider.GetGraphTypeFromClrType(clrType, isInputType, graphType);

            if (graphType != null)
                return graphType;
        }

        // Fall back to built-in scalar mappings
        if (BuiltInScalarMappings.TryGetValue(clrType, out var builtInType))
            return builtInType;

        // Auto-generate EnumerationGraphType<T> for enum types
        if (clrType.IsEnum)
            return typeof(EnumerationGraphType<>).MakeGenericType(clrType);

        // No mapping found
        throw new InvalidOperationException($"Could not find type mapping from CLR type '{clrType.FullName}' to GraphType. Did you forget to register the type mapping with the '{nameof(ISchema)}.{nameof(ISchema.RegisterTypeMapping)}'?");
    }

    /// <summary>
    /// Gets a built-in scalar instance.
    /// </summary>
    private ScalarGraphType GetBuiltInScalar(Type type)
    {
        return BuiltInCustomScalars[type];
    }

    /// <summary>
    /// Gets the name converter for a type.
    /// </summary>
    private INameConverter GetNameConverter(IGraphType type)
    {
        return IsIntrospectionType(type)
            ? CamelCaseNameConverter.Instance
            : _schema.NameConverter;
    }

    /// <summary>
    /// Checks if a type is an introspection type.
    /// </summary>
    private bool IsIntrospectionType(IGraphType type)
    {
        return type.Name?.StartsWith("__") == true;
    }
}
