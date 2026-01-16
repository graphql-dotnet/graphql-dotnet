using GraphQL.Conversion;
using GraphQL.Introspection;
using GraphQL.Utilities;
using GraphQLParser;

namespace GraphQL.Types;

/*
 * ================================================================================================
 * NewSchemaTypes - GraphQL Schema Type Collection and Initialization
 * ================================================================================================
 *
 * This class manages the complete lifecycle of type discovery, registration, and initialization
 * for a GraphQL schema. It implements a three-phase approach to ensure all types are properly
 * discovered, processed, and finalized before the schema becomes operational.
 *
 * ================================================================================================
 * THREE-PHASE INITIALIZATION PROCESS
 * ================================================================================================
 *
 * PHASE 1: DISCOVERY
 * ------------------
 * The discovery phase collects known/root types from various sources WITHOUT processing them.
 * This includes introspection types, manually-added types, root operation types, and additional
 * type references. These types are registered but not processed, deferring all recursive
 * field/argument processing until Phase 2.
 *
 * Types are discovered in the following order (which establishes registration priority):
 *
 *   1. Introspection Types (__Schema, __Type, __Field, __InputValue, __EnumValue, __Directive,
 *      __DirectiveLocation, __TypeKind, and optionally __AppliedDirective, __DirectiveArgument)
 *      - These are registered first to ensure they're available for schema introspection
 *      - They use specific feature flags to determine which types to include
 *
 *   2. Manually-Added Type Instances (Schema.AdditionalTypeInstances)
 *      - Pre-instantiated types added via Schema.RegisterType(instance)
 *      - These take precedence over types resolved from the DI container
 *      - Specifically useful for overriding built-in scalars
 *
 *   3. Root Operation Types (Query, Mutation, Subscription)
 *      - The Query type is required; Mutation and Subscription are optional
 *      - These define the entry points for GraphQL operations
 *
 *   4. Additional Type References (Schema.AdditionalTypes)
 *      - Types added via Schema.RegisterType<T>()
 *      - These are CLR types that will be resolved to GraphType instances
 *      - Useful for ensuring types are included even if not directly referenced
 *
 * During discovery, types are added to the Dictionary (keyed by GraphQL type name) and to the
 * _typeDictionary (keyed by CLR type). The skipProcessing flag is set to true, deferring all
 * recursive field/argument processing until Phase 2.
 *
 * PHASE 2: PROCESSING
 * -------------------
 * The processing phase walks through all discovered types and resolves their dependencies,
 * discovering additional types as needed. Before processing each type, the _onBeforeInitialize
 * hook is called (if provided), allowing for last-minute modifications to the type before it's
 * locked in. This phase then handles:
 *
 *   - Field Type Resolution: Converts Type references to ResolvedType instances
 *   - Argument Type Resolution: Resolves types for field and directive arguments
 *   - Interface Implementation: Resolves interface types and updates PossibleTypes
 *   - Union/Interface Possible Types: Resolves object types that implement interfaces or unions
 *   - Name Conversion: Applies INameConverter to field and argument names
 *   - Name Validation: Ensures all names comply with GraphQL naming rules
 *
 * As new types are discovered during processing (e.g., field types, argument types, interface
 * types), they are immediately added to the Dictionary and processed recursively. This continues
 * until all type dependencies are fully resolved.
 *
 * The processing phase also handles:
 *   - Meta fields (__schema, __type, __typename) on the Query type
 *   - Directive arguments and validation
 *   - Enum value name validation
 *
 * PHASE 3: FINALIZATION
 * ---------------------
 * The finalization phase completes the type initialization process:
 *
 *   1. Type Reference Replacement: All GraphQLTypeReference instances are replaced with actual
 *      type instances from the Dictionary. This allows types to reference each other by name
 *      before they're fully initialized. During this process, if a referenced type is not found
 *      in the Dictionary, built-in scalars may be referenced by name and automatically added
 *      (e.g., a GraphQLTypeReference("String") will be replaced with the built-in StringGraphType
 *      if "String" was not explicitly registered during discovery or processing).
 *
 *   2. Interface Description Inheritance: Field descriptions are inherited from interfaces to
 *      implementing types when the implementing type doesn't provide its own description.
 *
 *   3. Type Initialization: Each type's Initialize() method is called, allowing types to perform
 *      any final setup now that all dependencies are resolved.
 *
 * ================================================================================================
 * TYPE REGISTRATION PRIORITY AND DICTIONARY BEHAVIOR
 * ================================================================================================
 *
 * When a type instance is registered (via AddType), it is stored in TWO dictionaries:
 *
 * 1. Dictionary (keyed by GraphQL type name - e.g., "String", "User", "Query")
 *    - This is the primary type lookup used during schema operations
 *    - Only one type can exist per name (duplicates throw an exception)
 *    - Used for type reference resolution and schema introspection
 *
 * 2. _typeDictionary (keyed by CLR Type - e.g., typeof(StringGraphType), typeof(UserType))
 *    - This is used during type resolution to avoid creating duplicate instances
 *    - Maps CLR types to their corresponding GraphType instances
 *    - Used when resolving field types, argument types, and interface implementations
 *
 * The _typeDictionary stores multiple keys for a single GraphType instance:
 *
 *   a) The CLR type passed to AddType (if provided)
 *      - This is typically the type used to resolve the instance from DI
 *
 *   b) The GraphType's own CLR type (if not a base type like ObjectGraphType)
 *      - Base types (ObjectGraphType, InterfaceGraphType, etc.) are excluded because they're
 *        commonly used as base classes for custom types, and we don't want to map the base
 *        type to a specific instance
 *
 *   c) Base scalar types (when a derived scalar has the same name as a built-in scalar)
 *      - If a scalar derives from StringGraphType and is named "String", both the derived type
 *        and typeof(StringGraphType) will map to the same instance
 *      - This allows the derived scalar to be used anywhere the built-in scalar would be used
 *
 * ================================================================================================
 * TYPE RESOLUTION AND MAPPING
 * ================================================================================================
 *
 * For CLR-to-GraphType mappings (e.g., GraphQLClrOutputTypeReference<string> -> StringGraphType),
 * the system uses:
 *
 *   1. Schema.TypeMappings (explicit mappings via RegisterTypeMapping)
 *      - Highest priority; allows per-schema customization
 *
 *   2. IGraphTypeMappingProvider instances (custom mapping providers)
 *      - Allows extensible mapping logic (e.g., for custom conventions)
 *
 *   3. BuiltInScalarMappings (default CLR-to-scalar mappings)
 *      - Maps string -> StringGraphType, int -> IntGraphType, etc.
 *
 *   4. Auto-generated EnumerationGraphType<T> for enum types
 *      - Automatically creates GraphQL enum types from C# enums
 *
 * When resolving a CLR graph type to a GraphType instance, the system follows this priority order:
 *
 *   1. Check _typeDictionary for an already-registered instance
 *      - This ensures we reuse existing instances and respect overrides
 *
 *   2. Try to resolve from the DI container (IServiceProvider)
 *      - This allows types to be registered with dependency injection
 *
 *   3. Check BuiltInScalars dictionary
 *      - Falls back to built-in scalar instances if not found in DI
 *
 *   4. Throw an exception if the type cannot be resolved (includes EnumerationGraphType<T>)
 *
 * When explicit GraphType instances are encountered (e.g., field.ResolvedType is already set,
 * or types in Schema.AdditionalTypeInstances, or root operation types):
 *
 *   - The instance is used directly without any CLR type resolution
 *   - If the type's name has already been registered, then duplicate registration rules apply:
 *
 *     a) ALLOWED - Same instance registered multiple times (reference equality check):
 *        - The operation is idempotent; subsequent registrations are ignored
 *        - Example: The same UserType instance referenced in multiple places
 *
 *     b) ALLOWED - Built-in scalar types with duplicate instances of the same CLR type:
 *        - Multiple instances of StringGraphType, IntGraphType, etc. are permitted
 *        - Only applies to types in the BuiltInScalars dictionary
 *
 *     c) NOT ALLOWED - Different instances or types with the same name:
 *        - Example: Two different instances of UserType both named "User"
 *        - Example: UserObjectType and UserInputType both named "User"
 *
 * ================================================================================================
 * IMPORTANT NOTES AND EDGE CASES
 * ================================================================================================
 *
 * - When an object type implements an interface, it's automatically added to the interface's
 *   PossibleTypes collection. This bidirectional relationship is established during processing.
 *
 * - Only minimal schema validation is performed during initialization (e.g., name validation,
 *   type resolution). For comprehensive schema validation, use Schema.Validate() after the
 *   schema is fully initialized.
 *
 * ================================================================================================
 */

/// <summary>
/// Represents a list of all the graph types utilized by a schema.
/// Also provides lookup for all schema types.
/// </summary>
public sealed partial class SchemaTypes : SchemaTypesBase
{
    /// <summary>
    /// Initializes a new instance by discovering and processing all types from the schema.
    /// </summary>
    /// <param name="schema">The schema instance being initialized.</param>
    /// <param name="serviceProvider">DI container for resolving graph types.</param>
    /// <param name="graphTypeMappings">Custom CLR-to-GraphQL type mappings.</param>
    /// <param name="onBeforeInitialize">Pre-initialization hook called before each type is initialized.</param>
    public SchemaTypes(
        ISchema schema,
        IServiceProvider serviceProvider,
        IEnumerable<IGraphTypeMappingProvider>? graphTypeMappings = null,
        Action<IGraphType>? onBeforeInitialize = null) : base()
    {
        new SchemaTypesInitializer(this, schema, serviceProvider, graphTypeMappings, onBeforeInitialize)
            .Initialize();
    }

    /// <summary>
    /// An implementation that handles type discovery, registration, validation,
    /// and initialization for a schema. This implementation follows the comprehensive specifications for type
    /// discovery, registration, validation, and initialization.
    /// </summary>
    private readonly ref partial struct SchemaTypesInitializer
    {
        private readonly Dictionary<ROM, IGraphType> _dictionary;
        private readonly Dictionary<Type, IGraphType> _typeDictionary;
        private readonly SchemaTypes _schemaTypes;
        private readonly ISchema _schema;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEnumerable<IGraphTypeMappingProvider>? _graphTypeMappings;
        private readonly Action<IGraphType>? _onBeforeInitialize;
        private readonly Dictionary<Type, Type> _inputClrTypeMappingCache;
        private readonly Dictionary<Type, Type> _outputClrTypeMappingCache;

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

        /// <summary>
        /// Initializes a new instance of <see cref="SchemaTypesInitializer"/>.
        /// </summary>
        /// <param name="schemaTypes">The existing SchemaTypes instance.</param>
        /// <param name="schema">The schema instance being initialized.</param>
        /// <param name="serviceProvider">DI container for resolving graph types.</param>
        /// <param name="graphTypeMappings">Custom CLR-to-GraphQL type mappings.</param>
        /// <param name="onBeforeInitialize">Pre-initialization hook called before each type is initialized.</param>
        public SchemaTypesInitializer(
            SchemaTypes schemaTypes,
            ISchema schema,
            IServiceProvider serviceProvider,
            IEnumerable<IGraphTypeMappingProvider>? graphTypeMappings = null,
            Action<IGraphType>? onBeforeInitialize = null)
        {
            _dictionary = schemaTypes.Dictionary;
            _typeDictionary = new();
            _schemaTypes = schemaTypes ?? throw new ArgumentNullException(nameof(schemaTypes));
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _graphTypeMappings = graphTypeMappings;
            _onBeforeInitialize = onBeforeInitialize;

            // Initialize CLR type mapping caches from schema.TypeMappings
            _inputClrTypeMappingCache = new();
            _outputClrTypeMappingCache = new();
            foreach (var (mappedClrType, mappedGraphType) in _schema.TypeMappings)
            {
                // note that a mapped graph type might be both an input and output graph type
                if (mappedGraphType.IsInputType())
                    _inputClrTypeMappingCache[mappedClrType] = mappedGraphType;

                if (mappedGraphType.IsOutputType())
                    _outputClrTypeMappingCache[mappedClrType] = mappedGraphType;
            }
        }

        /// <summary>
        /// Initializes the schema types by performing discovery, processing, and finalization.
        /// </summary>
        public void Initialize()
        {
            // Phase 1: Discovery - collect all types without processing
            DiscoverTypes();

            // Phase 2: Processing - process all discovered types
            ProcessTypes();

            // Phase 3: Finalization - apply type references and cleanup
            FinalizeTypes();
        }

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
            // Process all types that have been discovered so far
            foreach (var type in _dictionary.Values.ToList()) // make a copy to avoid modification during iteration
            {
                ProcessType(type);
            }

            // Process meta fields on query type
            // Note: Meta fields must not have their field names translated by INameConverter
            ProcessField(_schemaTypes.SchemaMetaFieldType, null, null);
            ProcessField(_schemaTypes.TypeMetaFieldType, null, null);
            ProcessField(_schemaTypes.TypeNameMetaFieldType, null, null);

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
        private List<IGraphType> GetIntrospectionTypes()
        {
            var types = new List<IGraphType>();
            var allowAppliedDirectives = _schema.Features.AppliedDirectives;
            var allowRepeatable = _schema.Features.RepeatableDirectives;
            var deprecationOfInputValues = _schema.Features.DeprecationOfInputValues;

            // Yield enum types (no constructor parameters)
            types.Add(new __DirectiveLocation());
            types.Add(new __TypeKind());

            // Yield types with constructor parameters
            types.Add(new __EnumValue(allowAppliedDirectives));
            types.Add(new __Directive(allowAppliedDirectives, allowRepeatable));
            types.Add(new __Field(allowAppliedDirectives, deprecationOfInputValues));
            types.Add(new __InputValue(allowAppliedDirectives, deprecationOfInputValues));
            types.Add(new __Type(allowAppliedDirectives, deprecationOfInputValues));
            types.Add(new __Schema(allowAppliedDirectives));

            // Yield applied directive types only if the feature is enabled
            if (allowAppliedDirectives)
            {
                types.Add(new __DirectiveArgument());
                types.Add(new __AppliedDirective());
            }

            return types;
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
            if (!type.IsIntrospectionType())
                NameValidator.ValidateNameOnSchemaInitialize(typeName, NamedElement.Type);

            // Check for duplicate registration
            if (_dictionary.TryGetValue(typeName, out var existing))
            {
                // Allow same instance to be registered multiple times (idempotent)
                if (ReferenceEquals(existing, type))
                    return;

                // Allow known scalars (built-in scalars provided with GraphQL.NET) to use duplicated instances
                if (existing.GetType() == type.GetType() && type is ScalarGraphType && BuiltInScalars.ContainsKey(existing.GetType()))
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
                    $"or use {nameof(GraphQLTypeReference)} to reference a type instantiated elsewhere.");
            }

            _dictionary[typeName] = type;

            // Map the CLR type (if provided) or the GraphType's own type if it's not a built-in GraphQL type
            if (clrType != null || !_baseTypes.Contains(type.GetType()))
            {
                var typeKey = clrType ?? type.GetType();
                _typeDictionary[typeKey] = type;
            }

            // For scalar types that derive from built-in scalars, also register the base type
            // if the Name matches (indicating it's an override, not a new type)
            if (type is ScalarGraphType)
            {
                var baseType = type.GetType().BaseType;
                while (baseType != null && baseType != typeof(ScalarGraphType) && baseType != typeof(object))
                {
                    if (BuiltInScalars.TryGetValue(baseType, out var builtInInstance))
                    {
                        // Check if the name matches the built-in scalar's name
                        if (builtInInstance.Name == type.Name)
                        {
                            _typeDictionary[baseType] = type;
                        }
                    }
                    baseType = baseType.BaseType;
                }
            }

            // Process the type immediately after adding it (unless skipProcessing is true)
            if (!skipProcessing)
                ProcessType(type);
        }

        /// <summary>
        /// Processes a single graph type, discovering and registering referenced types.
        /// </summary>
        private void ProcessType(IGraphType type)
        {
            // Call the pre-initialization hook, allowing changes to the type before processing
            _onBeforeInitialize?.Invoke(type);

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
            // Any built-in scalars referenced by name will be added automatically if not already present
            new TypeReferenceReplacementVisitor(_dictionary, BuiltInScalarsByName, _schema).Run();

            // Inherit interface field descriptions to implementing types
            InheritInterfaceDescriptions();

            // Initialize all types
            foreach (var type in _dictionary.Values)
            {
                type.Initialize(_schema);
            }
        }

        /// <summary>
        /// Inherits field descriptions from interfaces to implementing types.
        /// </summary>
        private void InheritInterfaceDescriptions()
        {
            foreach (var type in _dictionary.Values)
            {
                if (type is IImplementInterfaces implementation && implementation.ResolvedInterfaces.Count > 0
                    && type is IComplexGraphType complexType)
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
            if (_serviceProvider.GetService(type) is not IGraphType instance)
            {
                // Unable to resolve from DI; check if it's a built-in scalar
                if (BuiltInScalars.TryGetValue(type, out var scalar))
                {
                    instance = scalar;
                }
                else
                {
                    // Throw if we couldn't resolve to a concrete type
                    throw new InvalidOperationException(
                        $"Cannot resolve type '{type.GetFriendlyName()}' to a concrete GraphQL type. Ensure the type is registered in the service provider or is a built-in scalar type.");
                }
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
                // Note: GraphQLClrInputTypeReference and GraphQLClrOutputTypeReference are both classes, and
                //   and so will the graph type be, therefore we can safely use MakeGenericType here.
                // For example, MyType<int, GraphQLClrOutputTypeReference<string>> is essentially MyType<int, {reftype}>
                // since GraphQLClrOutputTypeReference<string> is a reference type; and since any graph type (e.g. StringGraphType)
                // is also a reference type, it is always safe to replace {reftype} with a graph type; so in the above example
                // the constructed type becomes MyType<int, StringGraphType> which is also MyType<int, {reftype}>.
                return MakeGenericTypeNoWarn(genericDef, newGenericArgs);
            }

            return type;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2055:Either the type on which the MakeGenericType is called can't be statically determined, or the type parameters to be used for generic arguments can't be statically determined.")]
        [UnconditionalSuppressMessage("Trimming", "IL3050: Avoid calling members annotated with 'RequiresDynamicCodeAttribute' when publishing as Native AOT")]
        private static Type MakeGenericTypeNoWarn(Type genericTypeDefinition, Type[] innerTypes)
        {
            return genericTypeDefinition.MakeGenericType(innerTypes);
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
            // Check cache first, which includes mappings within schema.TypeMappings
            var cache = isInputType ? _inputClrTypeMappingCache : _outputClrTypeMappingCache;
            if (cache.TryGetValue(clrType, out var cachedType))
                return cachedType;

            // Check mapping providers (includes built-in scalar and enum providers prepended by Schema)
            if (_graphTypeMappings != null)
            {
                Type? graphType = null;

                foreach (var provider in _graphTypeMappings)
                    graphType = provider.GetGraphTypeFromClrType(clrType, isInputType, graphType);

                if (graphType != null)
                {
                    cache[clrType] = graphType;
                    return graphType;
                }
            }

            // No mapping found
            throw new InvalidOperationException($"Could not find type mapping from CLR type '{clrType.FullName}' to GraphType. Did you forget to register the type mapping with the '{nameof(ISchema)}.{nameof(ISchema.RegisterTypeMapping)}'?");
        }

        /// <summary>
        /// Gets the name converter for a type.
        /// </summary>
        private INameConverter GetNameConverter(IGraphType type)
        {
            return type.IsIntrospectionType()
                ? CamelCaseNameConverter.Instance
                : _schema.NameConverter;
        }
    }
}
