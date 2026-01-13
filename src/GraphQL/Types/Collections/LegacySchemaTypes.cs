using System.Diagnostics;
using GraphQL.Conversion;
using GraphQL.Introspection;
using GraphQL.Types.Collections;
using GraphQL.Utilities;

namespace GraphQL.Types;

/// <summary>
/// A class that represents a list of all the graph types utilized by a schema.
/// Also provides lookup for all schema types and has algorithms for discovering them.
/// <br/>
/// NOTE: After creating an instance of this class, its contents cannot be changed.
/// </summary>
[Obsolete("LegacySchemaTypes is obsolete and will be removed in a future version. Please use NewSchemaTypes instead.")]
public class LegacySchemaTypes : SchemaTypesBase
{
    private const string INITIALIZATIION_TRACE_KEY = "__INITIALIZATIION_TRACE_KEY__";

    // Introspection types https://spec.graphql.org/October2021/#sec-Schema-Introspection
    private Dictionary<Type, IGraphType> _introspectionTypes;

    private TypeCollectionContext _context;
    private INameConverter _nameConverter;
    private readonly Action<IGraphType>? _onBeforeInitialize;

    /// <summary>
    /// Initializes a new instance with no types registered.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    protected LegacySchemaTypes()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    /// <summary>
    /// Initializes a new instance for the specified schema, and with the specified type resolver.
    /// </summary>
    /// <param name="schema">A schema for which this instance is created.</param>
    /// <param name="serviceProvider">A service provider used to resolve graph types.</param>
    public LegacySchemaTypes(ISchema schema, IServiceProvider serviceProvider)
        : this(schema, serviceProvider, (IEnumerable<IGraphTypeMappingProvider>?)serviceProvider.GetService(typeof(IEnumerable<IGraphTypeMappingProvider>)))
    {
    }

    /// <summary>
    /// Initializes a new instance for the specified schema, with the specified type resolver,
    /// with the specified set of <see cref="IGraphTypeMappingProvider"/> instances.
    /// </summary>
    /// <param name="schema">A schema for which this instance is created.</param>
    /// <param name="serviceProvider">A service provider used to resolve graph types.</param>
    /// <param name="graphTypeMappings">A list of <see cref="IGraphTypeMappingProvider"/> instances used to map CLR types to graph types.</param>
    public LegacySchemaTypes(ISchema schema, IServiceProvider serviceProvider, IEnumerable<IGraphTypeMappingProvider>? graphTypeMappings)
        : this(schema, serviceProvider, graphTypeMappings, null)
    {
    }

    /// <summary>
    /// Initializes a new instance for the specified schema, with the specified type resolver,
    /// with the specified set of <see cref="IGraphTypeMappingProvider"/> instances, and with
    /// an optional delegate to call before initializing each graph type.
    /// </summary>
    /// <param name="schema">A schema for which this instance is created.</param>
    /// <param name="serviceProvider">A service provider used to resolve graph types.</param>
    /// <param name="graphTypeMappings">A list of <see cref="IGraphTypeMappingProvider"/> instances used to map CLR types to graph types.</param>
    /// <param name="onBeforeInitialize">An optional delegate to call before initializing each graph type.</param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public LegacySchemaTypes(ISchema schema, IServiceProvider serviceProvider, IEnumerable<IGraphTypeMappingProvider>? graphTypeMappings, Action<IGraphType>? onBeforeInitialize)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        _onBeforeInitialize = onBeforeInitialize;
        Initialize(schema, serviceProvider, graphTypeMappings);
    }

    private bool _initialized;
    /// <summary>
    /// Initializes the instance for the specified schema, and with the specified type resolver.
    /// </summary>
    /// <param name="schema">A schema for which this instance is created.</param>
    /// <param name="serviceProvider">A service provider used to resolve graph types.</param>
    /// <param name="graphTypeMappings">A service used to map CLR types to graph types.</param>
    protected void Initialize(ISchema schema, IServiceProvider serviceProvider, IEnumerable<IGraphTypeMappingProvider>? graphTypeMappings)
    {
        if (schema == null)
            throw new ArgumentNullException(nameof(schema));
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        if (_initialized)
            throw new InvalidOperationException("SchemaTypes has already been initialized.");
        _initialized = true;

        var (typeInstances, types) = GetSchemaTypes(schema, serviceProvider);
        if (schema.TypeMappings != null)
        {
            // this code could be moved into Schema
            var additionalMappings = schema.TypeMappings.Select(x => new ManualGraphTypeMappingProvider(x.clrType, x.graphType));
            graphTypeMappings = graphTypeMappings != null ? graphTypeMappings.Concat(additionalMappings).ToList() : additionalMappings.ToList();
        }
        var directives = schema.Directives ?? throw new ArgumentNullException(nameof(schema) + "." + nameof(ISchema.Directives));

        _typeDictionary = [];
        if (schema.Features.DeprecationOfInputValues)
        {
            // TODO: remove this code block when the next version of the spec will be released
            schema.Directives.Deprecated.Locations.Add(GraphQLParser.AST.DirectiveLocation.ArgumentDefinition);
            schema.Directives.Deprecated.Locations.Add(GraphQLParser.AST.DirectiveLocation.InputFieldDefinition);
        }
        _introspectionTypes = CreateIntrospectionTypes(schema.Features.AppliedDirectives, schema.Features.RepeatableDirectives, schema.Features.DeprecationOfInputValues);

        _context = new TypeCollectionContext(
           type => BuildGraphQLType(
               type,
               t => _introspectionTypes.TryGetValue(t, out var graphType)
               ? graphType
               : (IGraphType?)serviceProvider.GetService(t)
               ?? (BuiltInScalars.TryGetValue(t, out var scalarType)
               ? scalarType
               : throw new Exception($"Invalid introspection type '{t.GetFriendlyName()}'"))),
           (name, type, ctx) =>
           {
               SetGraphType(name, type);
               ctx.AddType(name, type, null!);
           },
           graphTypeMappings,
           schema);

        // Add manually-added scalar types. To allow overriding of built-in scalars, these must be added
        // prior to adding any other types (including introspection types).
        using (_context.Trace("Loop over manually-added scalar types from AdditionalTypeInstances"))
        {
            foreach (var type in typeInstances)
            {
                if (type is ScalarGraphType)
                    AddType(type, _context);
            }
        }

        // Add introspection types. Note that introspection types rely on the
        // CamelCaseNameConverter, as some fields are defined in pascal case - e.g. Field(x => x.Name)
        _nameConverter = CamelCaseNameConverter.Instance;

        using (_context.Trace("__Schema root type"))
            AddType(_introspectionTypes[typeof(__Schema)], _context);

        // set the name converter properly
        _nameConverter = schema.NameConverter ?? CamelCaseNameConverter.Instance;

        var ctx = new TypeCollectionContext(
            serviceType =>
            {
                // attempt to pull the required service from the service provider
                // if the service provider does not provide an instance, and if
                // the type is a GraphQL.NET built-in type, create an instance of it
                return (IGraphType?)serviceProvider.GetService(serviceType)
                    ?? (BuiltInScalars.TryGetValue(serviceType, out var graphType)
                        ? graphType
                        : throw new InvalidOperationException($"No service for type '{serviceType.GetFriendlyName()}' has been registered."));
            },
            (name, graphType, context) =>
            {
                if (this[name] == null)
                {
                    using var _ = context.Trace("TypeCollectionContext.AddType delegate");
                    AddType(graphType, context);
                }
            },
            graphTypeMappings,
            schema);

        using (ctx.Trace("Loop over manually-added non-scalar types from AdditionalTypeInstances"))
        {
            foreach (var type in typeInstances)
            {
                if (type is not ScalarGraphType)
                    AddTypeIfNotRegistered(type, ctx);
            }
        }

        using (ctx.Trace("Loop over manually-added types from AdditionalTypes"))
        {
            foreach (var type in types)
            {
                _ = AddTypeIfNotRegistered(type, ctx);
            }
        }

        // these fields must not have their field names translated by INameConverter; see HandleField
        using (ctx.Trace("__schema root field"))
            HandleField(null, SchemaMetaFieldType, ctx, false);
        using (ctx.Trace("__type root field"))
            HandleField(null, TypeMetaFieldType, ctx, false);
        using (ctx.Trace("__typename root field"))
            HandleField(null, TypeNameMetaFieldType, ctx, false);

        using (ctx.Trace("Loop for directives"))
        {
            foreach (var directive in directives)
            {
                using var _ = ctx.Trace("Directive '{0}'", directive.Name);
                HandleDirective(directive, ctx);
            }
        }

        ApplyTypeReferences();

        // https://github.com/graphql-dotnet/graphql-dotnet/issues/1004
        InheritInterfaceDescriptions();

        Debug.Assert(ctx.InFlightRegisteredTypes.Count == 0);
        Debug.Assert((ctx.InitializationTrace?.Count ?? 0) == 0);
        Debug.Assert((_context.InitializationTrace?.Count ?? 0) == 0);

        _typeDictionary = null!; // not needed once initialization is complete

        foreach (var type in Dictionary.Values)
        {
            type.Initialize(schema);
        }
    }

    private static (IEnumerable<IGraphType>, IEnumerable<Type>) GetSchemaTypes(ISchema schema, IServiceProvider serviceProvider)
    {
        return (GetSchemaTypeInstances(schema, serviceProvider),
            schema.AdditionalTypes.Select(x => x.GetNamedType()).Where(x => !typeof(ScalarGraphType).IsAssignableFrom(x)));
    }

    private static IEnumerable<IGraphType> GetSchemaTypeInstances(ISchema schema, IServiceProvider serviceProvider)
    {
        // Manually registered AdditionalTypeInstances and AdditionalTypes should be handled first.
        // This is necessary for the correct processing of overridden built-in scalars.

        foreach (var instance in schema.AdditionalTypeInstances)
            yield return instance;

        foreach (var type in schema.AdditionalTypes)
        {
            var type2 = type.GetNamedType();
            if (typeof(ScalarGraphType).IsAssignableFrom(type2))
            {
                yield return (IGraphType)serviceProvider.GetRequiredService(type2);
            }
        }

        if (schema.Query == null)
            throw new InvalidOperationException("Query root type must be provided. See https://spec.graphql.org/October2021/#sec-Schema-Introspection");

        if (schema.Query != null)
            yield return schema.Query;

        if (schema.Mutation != null)
            yield return schema.Mutation;

        if (schema.Subscription != null)
            yield return schema.Subscription;
    }

    private static Dictionary<Type, IGraphType> CreateIntrospectionTypes(bool allowAppliedDirectives, bool allowRepeatable, bool deprecationOfInputValues)
    {
        return (allowAppliedDirectives
            ? new IGraphType[]
            {
                new __DirectiveLocation(),
                new __DirectiveArgument(),
                new __AppliedDirective(),
                new __TypeKind(),
                new __EnumValue(true),
                new __Directive(true, allowRepeatable),
                new __Field(true, deprecationOfInputValues),
                new __InputValue(true, deprecationOfInputValues),
                new __Type(true, deprecationOfInputValues),
                new __Schema(true)
            }
            : new IGraphType[]
            {
                new __DirectiveLocation(),
                //new __DirectiveArgument(), forbidden
                //new __AppliedDirective(),  forbidden
                new __TypeKind(),
                new __EnumValue(false),
                new __Directive(false, allowRepeatable),
                new __Field(false, deprecationOfInputValues),
                new __InputValue(false, deprecationOfInputValues),
                new __Type(false, deprecationOfInputValues),
                new __Schema(false)
            })
        .ToDictionary(t => t.GetType());
    }

    private Dictionary<Type, IGraphType> _typeDictionary;

    private IGraphType BuildGraphQLType(Type type, IGraphType resolvedType)
        => BuildGraphQLType(type, _ => resolvedType);

    /// <summary>
    /// Returns a new instance of the specified graph type, using the specified resolver to
    /// instantiate a new instance if the required type cannot be found from the lookup table.
    /// Defaults to <see cref="Activator.CreateInstance(Type)"/> if no <paramref name="resolve"/>
    /// parameter is specified. List and non-null graph types are instantiated and their
    /// <see cref="IProvideResolvedType.ResolvedType"/> property is set to a new instance of
    /// the base (wrapped) type.
    /// </summary>
    protected internal virtual IGraphType BuildGraphQLType(Type type, Func<Type, IGraphType> resolve)
    {
        var local = resolve;
        local ??= t => (IGraphType)Activator.CreateInstance(t)!;
        resolve = t => FindGraphType(t) ?? local(t);

        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(NonNullGraphType<>))
            {
                var innerType = type.GenericTypeArguments[0];
                var resolvedInner = BuildGraphQLType(innerType, resolve);
                return new NonNullGraphType(resolvedInner);
            }

            if (type.GetGenericTypeDefinition() == typeof(ListGraphType<>))
            {
                var innerType = type.GenericTypeArguments[0];
                var resolvedInner = BuildGraphQLType(innerType, resolve);
                return new ListGraphType(resolvedInner);
            }
        }

        return resolve(type) ??
               throw new InvalidOperationException(
                   $"Expected non-null value, but {nameof(resolve)} delegate return null for '{type.Name}'");
    }


    /// <summary>
    /// Returns a graph type instance from the lookup table by its .NET type.
    /// </summary>
    /// <param name="type">The .NET type of the graph type.</param>
    private IGraphType? FindGraphType(Type type)
    {
        return _typeDictionary == null
            ? null
            : _typeDictionary.TryGetValue(type, out var value) ? value : null;
    }

    private void AddType(IGraphType type, TypeCollectionContext context)
    {
        if (type == null || type is GraphQLTypeReference)
        {
            return;
        }

        if (type is NonNullGraphType || type is ListGraphType)
        {
            throw new ArgumentOutOfRangeException(nameof(type), "Only add root types.");
        }

        OnBeforeInitialize(type);

        if (context.InitializationTrace != null)
            type.WithMetadata(INITIALIZATIION_TRACE_KEY, string.Join(Environment.NewLine, context.InitializationTrace));
        SetGraphType(type.Name, type);

        if (type is IComplexGraphType complexType)
        {
            using var _ = context.Trace("Loop for fields of complex type '{0}'", complexType.Name);
            foreach (var field in complexType.Fields)
            {
                using var __ = context.Trace("Field '{0}.{1}'", complexType.Name, field.Name);
                HandleField(complexType, field, context, true);
            }
        }

        if (type is IObjectGraphType obj)
        {
            using var _ = context.Trace("Loop for interfaces of object type '{0}'", obj.Name);
            foreach (var objectInterface in obj.Interfaces.List)
            {
                using var __ = context.Trace("Interface '{0}'", objectInterface.Name);
                object typeOrError = RebuildType(objectInterface, false, context.ClrToGraphTypeMappings);
                if (typeOrError is string error)
                    throw new InvalidOperationException($"The GraphQL implemented type '{objectInterface.GetFriendlyName()}' for object graph type '{type.Name}' could not be derived implicitly. " + error);
                var objectInterface2 = (Type)typeOrError;
                if (AddTypeIfNotRegistered(objectInterface2, context) is IInterfaceGraphType interfaceInstance)
                {
                    obj.AddResolvedInterface(interfaceInstance);
                    interfaceInstance.AddPossibleType(obj);
                }
            }
        }

        if (type is IInterfaceGraphType iface)
        {
            using var _ = context.Trace("Loop for interfaces of interface type '{0}'", iface.Name);
            foreach (var objectInterface in iface.Interfaces.List)
            {
                using var __ = context.Trace("Interface '{0}'", objectInterface.Name);
                object typeOrError = RebuildType(objectInterface, false, context.ClrToGraphTypeMappings);
                if (typeOrError is string error)
                    throw new InvalidOperationException($"The GraphQL implemented type '{objectInterface.GetFriendlyName()}' for object graph type '{type.Name}' could not be derived implicitly. " + error);
                var objectInterface2 = (Type)typeOrError;
                if (AddTypeIfNotRegistered(objectInterface2, context) is IInterfaceGraphType interfaceInstance)
                {
                    iface.AddResolvedInterface(interfaceInstance);
                }
            }

            if (type is IInterfaceGraphType iface2)
            {
                foreach (var possibleType in iface2.Types)
                {
                    using var __ = context.Trace("Possible clr type '{0}'", possibleType.Name);
                    object typeOrError = RebuildType(possibleType, false, context.ClrToGraphTypeMappings);
                    if (typeOrError is string error)
                        throw new InvalidOperationException($"The GraphQL type '{possibleType.GetFriendlyName()}' for interface graph type '{type.Name}' could not be derived implicitly. " + error);
                    var unionedType2 = (Type)typeOrError;
                    if (AddTypeIfNotRegistered(unionedType2, context) is not IObjectGraphType objType)
                        throw new InvalidOperationException($"The GraphQL type '{possibleType.GetFriendlyName()}' for interface graph type '{type.Name}' could not be derived implicitly. The resolved type is not an {nameof(IObjectGraphType)}.");

                    if (iface2.ResolveType == null && objType != null && objType.IsTypeOf == null)
                    {
                        throw new InvalidOperationException(
                           $"Interface type '{iface2.Name}' does not provide a 'resolveType' function " +
                           $"and possible Type '{objType.Name}' does not provide a 'isTypeOf' function. " +
                            "There is no way to resolve this possible type during execution.");
                    }

                    iface2.AddPossibleType(objType!);
                }
            }
        }

        if (type is UnionGraphType union)
        {
            using var _ = context.Trace("Loop for possible types of union type '{0}'", union.Name);

            foreach (var unionedType in union.PossibleTypes)
            {
                using var __ = context.Trace("Possible graph type '{0}'", unionedType.Name);
                // skip references
                if (unionedType is GraphQLTypeReference)
                    continue;

                AddTypeIfNotRegistered(unionedType, context);

                if (union.ResolveType == null && unionedType.IsTypeOf == null)
                {
                    throw new InvalidOperationException(
                       $"Union type '{union.Name}' does not provide a 'resolveType' function " +
                       $"and possible Type '{unionedType.Name}' does not provide a 'isTypeOf' function. " +
                        "There is no way to resolve this possible type during execution.");
                }
            }

            foreach (var unionedType in union.Types)
            {
                using var __ = context.Trace("Possible clr type '{0}'", unionedType.Name);
                object typeOrError = RebuildType(unionedType, false, context.ClrToGraphTypeMappings);
                if (typeOrError is string error)
                    throw new InvalidOperationException($"The GraphQL type '{unionedType.GetFriendlyName()}' for union graph type '{type.Name}' could not be derived implicitly. " + error);
                var unionedType2 = (Type)typeOrError;
                if (AddTypeIfNotRegistered(unionedType2, context) is not IObjectGraphType objType)
                    throw new InvalidOperationException($"The GraphQL type '{unionedType.GetFriendlyName()}' for union graph type '{type.Name}' could not be derived implicitly. The resolved type is not an {nameof(IObjectGraphType)}.");

                if (union.ResolveType == null && objType != null && objType.IsTypeOf == null)
                {
                    throw new InvalidOperationException(
                       $"Union type '{union.Name}' does not provide a 'resolveType' function " +
                       $"and possible Type '{objType.Name}' does not provide a 'isTypeOf' function. " +
                        "There is no way to resolve this possible type during execution.");
                }

                union.AddPossibleType(objType!);
            }
        }
    }

    private void HandleField(IComplexGraphType? parentType, FieldType field, TypeCollectionContext context, bool applyNameConverter)
    {
        // applyNameConverter will be false while processing the three root introspection query fields: __schema, __type, and __typename
        //
        // During processing of those three root fields, the NameConverter will be set to the schema's selected NameConverter,
        //   and the field names must not be processed by the NameConverter
        //
        // For other introspection types and fields, the NameConverter will be set to CamelCaseNameConverter at the time this
        //   code executes, and applyNameConverter will be true
        //
        // For any other fields, the NameConverter will be set to the schema's selected NameConverter at the time this code
        //   executes, and applyNameConverter will be true

        if (applyNameConverter)
        {
            field.Name = _nameConverter.NameForField(field.Name, parentType!);
            NameValidator.ValidateNameOnSchemaInitialize(field.Name, NamedElement.Field);
        }

        if (field.ResolvedType == null)
        {
            if (field.Type == null)
                throw new InvalidOperationException($"Both ResolvedType and Type properties on field '{parentType?.Name}.{field.Name}' are null.");

            object typeOrError = RebuildType(field.Type, parentType is IInputObjectGraphType, context.ClrToGraphTypeMappings);
            if (typeOrError is string error)
                throw new InvalidOperationException($"The GraphQL type for field '{parentType?.Name}.{field.Name}' could not be derived implicitly. " + error);
            field.Type = (Type)typeOrError;

            var namedType = AddTypeIfNotRegistered(field.Type, context);
            field.ResolvedType = BuildGraphQLType(field.Type, namedType);
        }
        else
        {
            AddTypeIfNotRegistered(field.ResolvedType, context);
        }

        if (field.Arguments?.Count > 0)
        {
            using var _ = context.Trace("Loop for arguments of field '{0}'", field.Name);
            foreach (var arg in field.Arguments.List!)
            {
                using var __ = context.Trace("Argument '{0}'", arg.Name);

                if (applyNameConverter)
                {
                    arg.Name = _nameConverter.NameForArgument(arg.Name, parentType!, field);
                    NameValidator.ValidateNameOnSchemaInitialize(arg.Name, NamedElement.Argument);
                }

                if (arg.ResolvedType == null)
                {
                    if (arg.Type == null)
                        throw new InvalidOperationException($"Both ResolvedType and Type properties on argument '{parentType?.Name}.{field.Name}.{arg.Name}' are null.");

                    object typeOrError = RebuildType(arg.Type, true, context.ClrToGraphTypeMappings);
                    if (typeOrError is string error)
                        throw new InvalidOperationException($"The GraphQL type for argument '{parentType?.Name}.{field.Name}.{arg.Name}' could not be derived implicitly. " + error);
                    arg.Type = (Type)typeOrError;

                    var namedType = AddTypeIfNotRegistered(arg.Type, context);
                    arg.ResolvedType = BuildGraphQLType(arg.Type, namedType);
                }
                else
                {
                    AddTypeIfNotRegistered(arg.ResolvedType, context);
                }
            }
        }
    }

    private void HandleDirective(Directive directive, TypeCollectionContext context)
    {
        if (directive.Arguments?.Count > 0)
        {
            using var _ = context.Trace("Loop for arguments of directive '{0}'", directive.Name);
            foreach (var arg in directive.Arguments.List!)
            {
                using var __ = context.Trace("Argument '{0}'", arg.Name);
                if (arg.ResolvedType == null)
                {
                    if (arg.Type == null)
                        throw new InvalidOperationException($"Both ResolvedType and Type properties on argument '{directive.Name}.{arg.Name}' are null.");

                    object typeOrError = RebuildType(arg.Type, true, context.ClrToGraphTypeMappings);
                    if (typeOrError is string error)
                        throw new InvalidOperationException($"The GraphQL type for argument '{directive.Name}.{arg.Name}' could not be derived implicitly. " + error);
                    arg.Type = (Type)typeOrError;

                    var namedType = AddTypeIfNotRegistered(arg.Type, context);
                    arg.ResolvedType = BuildGraphQLType(arg.Type, namedType);
                }
                else
                {
                    AddTypeIfNotRegistered(arg.ResolvedType, context);
                    arg.ResolvedType = ConvertTypeReference(directive, arg.ResolvedType);
                }
            }
        }
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/pull/1010
    private void AddTypeWithLoopCheck(IGraphType resolvedType, TypeCollectionContext context, Type namedType)
    {
        if (context.InFlightRegisteredTypes.Any(t => t == namedType))
        {
            throw new InvalidOperationException($@"A loop has been detected while registering schema types.
There was an attempt to re-register '{namedType.FullName}' with instance of '{resolvedType.GetType().FullName}'.
Make sure that your ServiceProvider is configured correctly.");
        }

        context.InFlightRegisteredTypes.Push(namedType);
        try
        {
            using var _ = context.Trace("AddTypeWithLoopCheck for type '{0}'", namedType.Name);
            AddType(resolvedType, context);
        }
        finally
        {
            _ = context.InFlightRegisteredTypes.Pop();
        }
    }

    private IGraphType AddTypeIfNotRegistered(Type type, TypeCollectionContext context)
    {
        using var _ = context.Trace("AddTypeIfNotRegistered(Type, TypeCollectionContext) for type '{0}'", type.Name);
        var namedType = type.GetNamedType();
        var foundType = FindGraphType(namedType);
        if (foundType == null)
        {
            foundType = context.ResolveType(namedType);
            AddTypeWithLoopCheck(foundType, context, namedType);
        }
        return foundType;
    }

    private void AddTypeIfNotRegistered(IGraphType type, TypeCollectionContext context)
    {
        var namedType = type.GetNamedType();

        using var _ = context.Trace("AddTypeIfNotRegistered(IGraphType, TypeCollectionContext) for type '{0}'", namedType.Name);

        var existingType = this[namedType.Name];
        if (existingType is null)
        {
            AddType(namedType, context);
        }
        else
        {
            EnsureTypeEquality(existingType, namedType, context);
        }
    }

    private void EnsureTypeEquality(IGraphType existingType, IGraphType newType, TypeCollectionContext context)
    {
        if (ReferenceEquals(existingType, newType))
        {
            return;
        }

        // Ignore scalars
        if (existingType is ScalarGraphType && newType is ScalarGraphType)
        {
            return;
        }

        if (existingType.GetType() != newType.GetType())
        {
            throw new InvalidOperationException($"Unable to register GraphType '{newType.GetType().GetFriendlyName()}' with the name '{newType.Name}'. The name '{newType.Name}' is already registered to '{existingType.GetType().GetFriendlyName()}'. Check your schema configuration.");
        }

        // All other types are considered "potentially wrong" when being re-registered, throw detailed exception
        throw new InvalidOperationException(ErrorMessage());

        string ErrorMessage()
        {
            string error = $"A different instance of the GraphType '{newType.GetType().GetFriendlyName()}' with the name '{newType.Name}' has already been registered within the schema. Please use the same instance for all references within the schema, or use {nameof(GraphQLTypeReference)} to reference a type instantiated elsewhere.";
            string traceInfo = $"To view additional trace enable {nameof(GlobalSwitches)}.{nameof(GlobalSwitches.TrackGraphTypeInitialization)} switch.";
            if (GlobalSwitches.TrackGraphTypeInitialization)
            {
                string trace1 = $"Existing type trace:{Environment.NewLine}{Environment.NewLine}{existingType.GetMetadata<string>(INITIALIZATIION_TRACE_KEY)}";
                string trace2 = $"New type trace:{Environment.NewLine}{Environment.NewLine}{(context.InitializationTrace == null ? "" : string.Join(Environment.NewLine, context.InitializationTrace))}";
                traceInfo = $"{trace1}{Environment.NewLine}{Environment.NewLine}{trace2}";
            }

            return $"{error}{Environment.NewLine}{traceInfo}";
        }
    }

    private object RebuildType(Type type, bool input, IEnumerable<IGraphTypeMappingProvider>? typeMappings)
    {
        if (!type.IsGenericType)
            return type;

        var genericDef = type.GetGenericTypeDefinition();
        if (genericDef == typeof(GraphQLClrOutputTypeReference<>) || genericDef == typeof(GraphQLClrInputTypeReference<>))
        {
            return GetGraphType(type.GetGenericArguments()[0], input, typeMappings);
        }
        else
        {
            var typeList = type.GetGenericArguments();
            bool changed = false;
            for (int i = 0; i < typeList.Length; i++)
            {
                object typeOrError = RebuildType(typeList[i], input, typeMappings);
                if (typeOrError is string)
                    return typeOrError;
                var changedType = (Type)typeOrError;
                changed |= changedType != typeList[i];
                typeList[i] = changedType;
            }
            return changed ? genericDef.MakeGenericType(typeList) : type;
        }
    }

    private object GetGraphType(Type clrType, bool input, IEnumerable<IGraphTypeMappingProvider>? typeMappings)
    {
        var ret = GetGraphTypeFromClrType(clrType, input, typeMappings);

        if (ret == null)
        {
            string additionalMessage = typeof(IGraphType).IsAssignableFrom(clrType)
                ? $" Note that '{clrType.FullName}' is already a GraphType (i.e. not CLR type like System.DateTime or System.String). Most likely you need to specify corresponding CLR type instead of GraphType."
                : "";
            return $"Could not find type mapping from CLR type '{clrType.FullName}' to GraphType. Did you forget to register the type mapping with the '{nameof(ISchema)}.{nameof(ISchema.RegisterTypeMapping)}'?{additionalMessage}";
        }

        return ret;
    }

    /// <summary>
    /// Returns a graph type for a specified input or output CLR type.
    /// This method is called when a graph type is specified as a <see cref="GraphQLClrInputTypeReference{T}"/> or <see cref="GraphQLClrOutputTypeReference{T}"/>.
    /// </summary>
    /// <param name="clrType">The CLR type to be mapped.</param>
    /// <param name="isInputType">Indicates if the CLR type should be mapped to an input or output graph type.</param>
    /// <param name="typeMappings">The list of registered type mappings on the schema.</param>
    /// <returns>The graph type to be used, or <see langword="null"/> if no match can be found.</returns>
    /// <remarks>
    /// This method should not return wrapped types such as <see cref="ListGraphType"/> or <see cref="NonNullGraphType"/>.
    /// These are handled within <see cref="GraphQL.TypeExtensions.GetGraphTypeFromType(Type, bool, bool)"/>,
    /// and should already have been wrapped around the type reference.
    /// </remarks>
    protected virtual Type? GetGraphTypeFromClrType(Type clrType, bool isInputType, IEnumerable<IGraphTypeMappingProvider>? typeMappings)
    {
        Type? mappedType = null;

        // check custom mappings first
        if (typeMappings != null)
        {
            foreach (var mapping in typeMappings)
            {
                mappedType = mapping.GetGraphTypeFromClrType(clrType, isInputType, mappedType);
            }
        }

        if (mappedType != null)
            return mappedType;

        // then built-in mappings
        if (BuiltInScalarMappingProvider.BuiltInScalarMappings.TryGetValue(clrType, out var graphType))
            return graphType;

        // create an enumeration graph type if applicable
        if (clrType.IsEnum)
            return typeof(EnumerationGraphType<>).MakeGenericType(clrType);

        return null;
    }

    private void ApplyTypeReferences()
    {
        // ToList() is a necessary measure here since otherwise we get System.InvalidOperationException: 'Collection was modified; enumeration operation may not execute.'
        foreach (var type in Dictionary.Values.ToList())
        {
            ApplyTypeReference(type);
        }
    }

    private void ApplyTypeReference(IGraphType type)
    {
        if (type is IComplexGraphType complexType)
        {
            foreach (var field in complexType.Fields)
            {
                field.ResolvedType = ConvertTypeReference(type, field.ResolvedType!);

                if (field.Arguments?.Count > 0)
                {
                    foreach (var arg in field.Arguments.List!)
                    {
                        arg.ResolvedType = ConvertTypeReference(type, arg.ResolvedType!);
                    }
                }
            }
        }

        if (type is IObjectGraphType objectType)
        {
            var list = objectType.ResolvedInterfaces.List;
            for (int i = 0; i < list.Count; ++i)
            {
                var interfaceType = (IInterfaceGraphType)ConvertTypeReference(objectType, list[i]);

                interfaceType.AddPossibleType(objectType);

                list[i] = interfaceType;
            }
        }

        if (type is IInterfaceGraphType iface)
        {
            var list = iface.ResolvedInterfaces.List;
            for (int i = 0; i < list.Count; ++i)
            {
                list[i] = (IInterfaceGraphType)ConvertTypeReference(iface, list[i]);
            }
        }

        if (type is UnionGraphType union)
        {
            var list = union.PossibleTypes.List;
            for (int i = 0; i < list.Count; ++i)
            {
                var unionType = ConvertTypeReference(union, list[i]) as IObjectGraphType;

                if (union.ResolveType == null && unionType != null && unionType.IsTypeOf == null)
                {
                    throw new InvalidOperationException(
                       $"Union type '{union.Name}' does not provide a 'resolveType' function " +
                       $"and possible Type '{union.Name}' does not provide a 'isTypeOf' function. " +
                        "There is no way to resolve this possible type during execution.");
                }

                list[i] = unionType!;
            }
        }
    }

    private IGraphType ConvertTypeReference(INamedType parentType, IGraphType type)
    {
        if (type is NonNullGraphType nonNull)
        {
            nonNull.ResolvedType = ConvertTypeReference(parentType, nonNull.ResolvedType!);
            return nonNull;
        }

        if (type is ListGraphType list)
        {
            list.ResolvedType = ConvertTypeReference(parentType, list.ResolvedType!);
            return list;
        }

        if (type is GraphQLTypeReference reference)
        {
            var type2 = this[reference.TypeName];
            if (type2 == null)
            {
                type2 = BuiltInScalarsByName.TryGetValue(reference.TypeName, out var scalar) ? scalar : null;
                if (type2 != null)
                    SetGraphType(type2.Name, type2);
            }
            if (type2 == null)
            {
                throw new InvalidOperationException($"Unable to resolve reference to type '{reference.TypeName}' on '{parentType.Name}'");
            }
            type = type2;
        }

        return type;
    }

    private void SetGraphType(string typeName, IGraphType graphType)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentOutOfRangeException(nameof(typeName), "A type name is required to lookup.");
        }

        var type = graphType.GetType();
        if (Dictionary.TryGetValue(typeName, out var existingGraphType))
        {
            if (ReferenceEquals(existingGraphType, graphType) || existingGraphType.GetType() == type)
            {
                // Soft schema configuration error.
                // Intentionally or inadvertently, a situation may arise when the same GraphType is registered more that one time.
                // This may be due to the simultaneous registration of GraphType instances and the GraphType types. In this case
                // the duplicate MUST be ignored, otherwise errors will occur.
            }
            else if (type.IsAssignableFrom(existingGraphType.GetType()) && typeof(ScalarGraphType).IsAssignableFrom(type))
            {
                // This can occur when a built-in scalar graph type is overridden by preregistering a replacement graph type that
                // has the same name and inherits from it.

                if (!_typeDictionary.ContainsKey(type))
                    _typeDictionary.Add(type, existingGraphType);
            }
            else
            {
                // Fatal schema configuration error.
                throw new InvalidOperationException($"Unable to register GraphType '{type.GetFriendlyName()}' with the name '{typeName}'. The name '{typeName}' is already registered to '{existingGraphType.GetType().GetFriendlyName()}'. Check your schema configuration.");
            }
        }
        else
        {
            Dictionary.Add(typeName, graphType);
            // if building a schema from code, the .NET types will not be unique, which should be ignored
            if (!_typeDictionary.ContainsKey(type))
                _typeDictionary.Add(type, graphType);
        }
    }

    private void InheritInterfaceDescriptions()
    {
        foreach (var fieldOwner in Dictionary.Values.OfType<IComplexGraphType>())
        {
            if (fieldOwner is IImplementInterfaces implementation && implementation.ResolvedInterfaces.Count > 0)
            {
                foreach (var field in fieldOwner.Fields.Where(field => field.Description == null))
                {
                    foreach (var iface in implementation.ResolvedInterfaces.List)
                    {
                        var fieldFromInterface = iface.GetField(field.Name);
                        if (fieldFromInterface?.Description != null)
                        {
                            field.Description = fieldFromInterface.Description;
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called before a graph type is initialized. By default, calls the delegate
    /// specified in the constructor if supplied.
    /// </summary>
    /// <param name="graphType">The graph type that is about to be initialized.</param>
    protected virtual void OnBeforeInitialize(IGraphType graphType)
    {
        _onBeforeInitialize?.Invoke(graphType);
    }
}
