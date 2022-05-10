using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Numerics;
using GraphQL.Conversion;
using GraphQL.Instrumentation;
using GraphQL.Introspection;
using GraphQL.Resolvers;
using GraphQL.Types.Collections;
using GraphQL.Types.Relay;
using GraphQL.Utilities;
using GraphQLParser;

namespace GraphQL.Types
{
    /// <summary>
    /// A class that represents a list of all the graph types utilized by a schema.
    /// Also provides lookup for all schema types and has algorithms for discovering them.
    /// <br/>
    /// NOTE: After creating an instance of this class, its contents cannot be changed.
    /// </summary>
    public class SchemaTypes : IEnumerable<IGraphType>
    {
        /// <summary>
        /// Returns a dictionary of default CLR type to graph type mappings for a set of built-in (primitive) types.
        /// </summary>
        public static ReadOnlyDictionary<Type, Type> BuiltInScalarMappings { get; } = new(new Dictionary<Type, Type>
        {
            [typeof(int)] = typeof(IntGraphType),
            [typeof(long)] = typeof(LongGraphType),
            [typeof(BigInteger)] = typeof(BigIntGraphType),
            [typeof(double)] = typeof(FloatGraphType),
            [typeof(float)] = typeof(FloatGraphType),
            [typeof(decimal)] = typeof(DecimalGraphType),
            [typeof(string)] = typeof(StringGraphType),
            [typeof(bool)] = typeof(BooleanGraphType),
            [typeof(DateTime)] = typeof(DateTimeGraphType),
#if NET6_0_OR_GREATER
            [typeof(DateOnly)] = typeof(DateOnlyGraphType),
            [typeof(TimeOnly)] = typeof(TimeOnlyGraphType),
#endif
            [typeof(DateTimeOffset)] = typeof(DateTimeOffsetGraphType),
            [typeof(TimeSpan)] = typeof(TimeSpanSecondsGraphType),
            [typeof(Guid)] = typeof(IdGraphType),
            [typeof(short)] = typeof(ShortGraphType),
            [typeof(ushort)] = typeof(UShortGraphType),
            [typeof(ulong)] = typeof(ULongGraphType),
            [typeof(uint)] = typeof(UIntGraphType),
            [typeof(byte)] = typeof(ByteGraphType),
            [typeof(sbyte)] = typeof(SByteGraphType),
            [typeof(Uri)] = typeof(UriGraphType),
        });

        // Introspection types https://spec.graphql.org/October2021/#sec-Schema-Introspection
        private Dictionary<Type, IGraphType> _introspectionTypes;

        // Standard scalars https://spec.graphql.org/October2021/#sec-Scalars
        private readonly Dictionary<Type, IGraphType> _builtInScalars = new IGraphType[]
        {
            new StringGraphType(),
            new BooleanGraphType(),
            new FloatGraphType(),
            new IntGraphType(),
            new IdGraphType(),
        }
        .ToDictionary(t => t.GetType());

        // .NET custom scalars
        private readonly Dictionary<Type, IGraphType> _builtInCustomScalars = new IGraphType[]
        {
            new DateGraphType(),
            #if NET6_0_OR_GREATER
            new DateOnlyGraphType(),
            new TimeOnlyGraphType(),
            #endif
            new DateTimeGraphType(),
            new DateTimeOffsetGraphType(),
            new TimeSpanSecondsGraphType(),
            new TimeSpanMillisecondsGraphType(),
            new DecimalGraphType(),
            new UriGraphType(),
            new GuidGraphType(),
            new ShortGraphType(),
            new UShortGraphType(),
            new UIntGraphType(),
            new LongGraphType(),
            new BigIntGraphType(),
            new ULongGraphType(),
            new ByteGraphType(),
            new SByteGraphType(),
        }
        .ToDictionary(t => t.GetType());

        private TypeCollectionContext _context;
        private INameConverter _nameConverter;

        /// <summary>
        /// Initializes a new instance with no types registered.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected SchemaTypes()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
        }

        /// <summary>
        /// Initializes a new instance for the specified schema, and with the specified type resolver.
        /// </summary>
        /// <param name="schema">A schema for which this instance is created.</param>
        /// <param name="serviceProvider">A service provider used to resolve graph types.</param>
        public SchemaTypes(ISchema schema, IServiceProvider serviceProvider)
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
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SchemaTypes(ISchema schema, IServiceProvider serviceProvider, IEnumerable<IGraphTypeMappingProvider>? graphTypeMappings)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            Initialize(schema, serviceProvider, graphTypeMappings);
        }

        private bool _initialized = false;
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

            var types = GetSchemaTypes(schema, serviceProvider);
            if (schema.TypeMappings != null)
            {
                // this code could be moved into Schema
                var additionalMappings = schema.TypeMappings.Select(x => new ManualGraphTypeMappingProvider(x.clrType, x.graphType));
                graphTypeMappings = graphTypeMappings != null ? graphTypeMappings.Concat(additionalMappings).ToList() : additionalMappings.ToList();
            }
            var directives = schema.Directives ?? throw new ArgumentNullException(nameof(schema) + "." + nameof(ISchema.Directives));

            _typeDictionary = new Dictionary<Type, IGraphType>();
            _introspectionTypes = CreateIntrospectionTypes(schema.Features.AppliedDirectives, schema.Features.RepeatableDirectives);

            _context = new TypeCollectionContext(
               type => BuildGraphQLType(type, t => _builtInScalars.TryGetValue(t, out var graphType) ? graphType : _introspectionTypes.TryGetValue(t, out graphType) ? graphType : (IGraphType)Activator.CreateInstance(t)!),
               (name, type, ctx) =>
               {
                   SetGraphType(name, type);
                   ctx.AddType(name, type, null!);
               },
               graphTypeMappings);

            // Add manually-added scalar types. To allow overriding of built-in scalars, these must be added
            // prior to adding any other types (including introspection types).
            foreach (var type in types)
            {
                if (type is ScalarGraphType)
                    AddType(type, _context);
            }

            // Add introspection types. Note that introspection types rely on the
            // CamelCaseNameConverter, as some fields are defined in pascal case - e.g. Field(x => x.Name)
            _nameConverter = CamelCaseNameConverter.Instance;

            foreach (var introspectionType in _introspectionTypes.Values)
                AddType(introspectionType, _context);

            // set the name converter properly
            _nameConverter = schema.NameConverter ?? CamelCaseNameConverter.Instance;

            var ctx = new TypeCollectionContext(
                t => _builtInScalars.TryGetValue(t, out var graphType) ? graphType : (IGraphType)serviceProvider.GetRequiredService(t),
                (name, graphType, context) =>
                {
                    if (this[name] == null)
                    {
                        AddType(graphType, context);
                    }
                },
                graphTypeMappings);

            foreach (var type in types)
            {
                if (type is not ScalarGraphType)
                    AddType(type, ctx);
            }

            // these fields must not have their field names translated by INameConverter; see HandleField
            HandleField(null, SchemaMetaFieldType, ctx, false);
            HandleField(null, TypeMetaFieldType, ctx, false);
            HandleField(null, TypeNameMetaFieldType, ctx, false);

            foreach (var directive in directives)
            {
                HandleDirective(directive, ctx);
            }

            ApplyTypeReferences();

            Debug.Assert(ctx.InFlightRegisteredTypes.Count == 0);

            _typeDictionary = null!; // not needed once initialization is complete
        }

        private static IEnumerable<IGraphType> GetSchemaTypes(ISchema schema, IServiceProvider serviceProvider)
        {
            // Manually registered AdditionalTypeInstances and AdditionalTypes should be handled first.
            // This is necessary for the correct processing of overridden built-in scalars.

            foreach (var instance in schema.AdditionalTypeInstances)
                yield return instance;

            foreach (var type in schema.AdditionalTypes)
                yield return (IGraphType)serviceProvider.GetRequiredService(type.GetNamedType());

            //TODO: According to the specification, Query is a required type. But if you uncomment these lines, then the mass of tests begin to fail, because they do not set Query.
            // if (Query == null)
            //    throw new InvalidOperationException("Query root type must be provided. See https://spec.graphql.org/October2021/#sec-Schema-Introspection");

            if (schema.Query != null)
                yield return schema.Query;

            if (schema.Mutation != null)
                yield return schema.Mutation;

            if (schema.Subscription != null)
                yield return schema.Subscription;
        }

        private static Dictionary<Type, IGraphType> CreateIntrospectionTypes(bool allowAppliedDirectives, bool allowRepeatable)
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
                    new __Field(true),
                    new __InputValue(true),
                    new __Type(true),
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
                    new __Field(false),
                    new __InputValue(false),
                    new __Type(false),
                    new __Schema(false)
                })
            .ToDictionary(t => t.GetType());
        }

        /// <summary>
        /// Returns a dictionary that relates type names to graph types.
        /// </summary>
        protected internal virtual Dictionary<ROM, IGraphType> Dictionary { get; } = new Dictionary<ROM, IGraphType>();
        private Dictionary<Type, IGraphType> _typeDictionary;

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<IGraphType> GetEnumerator() => Dictionary.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets the count of all the graph types utilized by the schema.
        /// </summary>
        public int Count => Dictionary.Count;

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
                    var nonNull = (NonNullGraphType)Activator.CreateInstance(type)!;
                    nonNull.ResolvedType = BuildGraphQLType(type.GenericTypeArguments[0], resolve);
                    return nonNull;
                }

                if (type.GetGenericTypeDefinition() == typeof(ListGraphType<>))
                {
                    var list = (ListGraphType)Activator.CreateInstance(type)!;
                    list.ResolvedType = BuildGraphQLType(type.GenericTypeArguments[0], resolve);
                    return list;
                }
            }

            return resolve(type) ??
                   throw new InvalidOperationException(
                       $"Expected non-null value, but {nameof(resolve)} delegate return null for '{type.Name}'");
        }

        /// <summary>
        /// Applies all delegates specified by the middleware builder to the schema.
        /// <br/><br/>
        /// When applying to the schema, modifies the resolver of each field of each graph type adding required behavior.
        /// Therefore, as a rule, this method should be called only once - during schema initialization.
        /// </summary>
        public void ApplyMiddleware(IFieldMiddlewareBuilder fieldMiddlewareBuilder)
        {
            var transform = (fieldMiddlewareBuilder ?? throw new ArgumentNullException(nameof(fieldMiddlewareBuilder))).Build();

            // allocation free optimization if no middlewares are defined
            if (transform != null)
            {
                ApplyMiddleware(transform);
            }
        }

        /// <summary>
        /// Applies the specified middleware transform delegate to the schema.
        /// <br/><br/>
        /// When applying to the schema, modifies the resolver of each field of each graph type adding required behavior.
        /// Therefore, as a rule, this method should be called only once - during schema initialization.
        /// </summary>
        public void ApplyMiddleware(Func<FieldMiddlewareDelegate, FieldMiddlewareDelegate> transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            foreach (var item in Dictionary)
            {
                if (item.Value is IComplexGraphType complex)
                {
                    foreach (var field in complex.Fields.List)
                    {
                        var inner = field.Resolver ?? NameFieldResolver.Instance;

                        var fieldMiddlewareDelegate = transform(context => inner.ResolveAsync(context));

                        field.Resolver = new FuncFieldResolver<object>(fieldMiddlewareDelegate.Invoke);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a graph type instance from the lookup table by its GraphQL type name.
        /// </summary>
        public IGraphType? this[ROM typeName]
        {
            get
            {
                if (typeName.IsEmpty)
                {
                    throw new ArgumentOutOfRangeException(nameof(typeName), "A type name is required to lookup.");
                }

                return Dictionary.TryGetValue(typeName, out var type) ? type : null;
            }
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

            SetGraphType(type.Name, type);

            if (type is IComplexGraphType complexType)
            {
                foreach (var field in complexType.Fields)
                {
                    HandleField(complexType, field, context, true);
                }
            }

            if (type is IObjectGraphType obj)
            {
                foreach (var objectInterface in obj.Interfaces.List)
                {
                    if (AddTypeIfNotRegistered(objectInterface, context) is IInterfaceGraphType interfaceInstance)
                    {
                        obj.AddResolvedInterface(interfaceInstance);
                        interfaceInstance.AddPossibleType(obj);

                        if (interfaceInstance.ResolveType == null && obj.IsTypeOf == null)
                        {
                            throw new InvalidOperationException(
                               $"Interface type '{interfaceInstance.Name}' does not provide a 'resolveType' function " +
                               $"and possible Type '{obj.Name}' does not provide a 'isTypeOf' function. " +
                                "There is no way to resolve this possible type during execution.");
                        }
                    }
                }
            }

            if (type is UnionGraphType union)
            {
                if (!union.Types.Any() && !union.PossibleTypes.Any())
                {
                    throw new InvalidOperationException($"Must provide types for Union '{union}'.");
                }

                foreach (var unionedType in union.PossibleTypes)
                {
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
                    var objType = AddTypeIfNotRegistered(unionedType, context) as IObjectGraphType;

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
                foreach (var arg in field.Arguments.List!)
                {
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
                foreach (var arg in directive.Arguments.List!)
                {
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
                AddType(resolvedType, context);
            }
            finally
            {
                context.InFlightRegisteredTypes.Pop();
            }
        }

        private IGraphType AddTypeIfNotRegistered(Type type, TypeCollectionContext context)
        {
            var namedType = type.GetNamedType();
            var foundType = FindGraphType(namedType);
            if (foundType == null)
            {
                if (namedType == typeof(PageInfoType))
                {
                    foundType = new PageInfoType();
                    AddType(foundType, context);
                }
                else if (namedType.IsGenericType && (namedType.ImplementsGenericType(typeof(EdgeType<>)) || namedType.ImplementsGenericType(typeof(ConnectionType<,>))))
                {
                    foundType = (IGraphType)Activator.CreateInstance(namedType)!;
                    AddType(foundType, context);
                }
                else if (_builtInCustomScalars.TryGetValue(namedType, out var builtInCustomScalar))
                {
                    foundType = builtInCustomScalar;
                    AddType(foundType, _context);
                }
                else
                {
                    foundType = context.ResolveType(namedType);
                    AddTypeWithLoopCheck(foundType, context, namedType);
                }
            }
            return foundType;
        }

        private void AddTypeIfNotRegistered(IGraphType type, TypeCollectionContext context)
        {
            var (namedType, namedType2) = type.GetNamedTypes();
            namedType ??= context.ResolveType(namedType2!);

            var foundType = this[namedType.Name];
            if (foundType == null)
            {
                AddType(namedType, context);
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
                var changed = false;
                for (var i = 0; i < typeList.Length; i++)
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
                return $"Could not find type mapping from CLR type '{clrType.FullName}' to GraphType. Did you forget to register the type mapping with the '{nameof(ISchema)}.{nameof(ISchema.RegisterTypeMapping)}'?";

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
        /// These are handled within <see cref="GraphQL.TypeExtensions.GetGraphTypeFromType(Type, bool, TypeMappingMode)"/>,
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
            if (BuiltInScalarMappings.TryGetValue(clrType, out var graphType))
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

                    if (objectType.IsTypeOf == null && interfaceType.ResolveType == null)
                    {
                        throw new InvalidOperationException(
                               $"Interface type '{interfaceType.Name}' does not provide a 'resolveType' function " +
                               $"and possible Type '{objectType.Name}' does not provide a 'isTypeOf' function.  " +
                                "There is no way to resolve this possible type during execution.");
                    }

                    interfaceType.AddPossibleType(objectType);

                    list[i] = interfaceType;
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
                    type2 = _builtInScalars.Values.FirstOrDefault(t => t.Name == reference.TypeName) ?? _builtInCustomScalars.Values.FirstOrDefault(t => t.Name == reference.TypeName);
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
                    throw new InvalidOperationException($@"Unable to register GraphType '{type.FullName}' with the name '{typeName}'. The name '{typeName}' is already registered to '{existingGraphType.GetType().FullName}'. Check your schema configuration.");
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

        /// <summary>
        /// Returns the <see cref="FieldType"/> instance for the <c>__schema</c> meta-field.
        /// </summary>
        protected internal virtual FieldType SchemaMetaFieldType { get; } = new SchemaMetaFieldType();

        /// <summary>
        /// Returns the <see cref="FieldType"/> instance for the <c>__type</c> meta-field.
        /// </summary>
        protected internal virtual FieldType TypeMetaFieldType { get; } = new TypeMetaFieldType();

        /// <summary>
        /// Returns the <see cref="FieldType"/> instance for the <c>__typename</c> meta-field.
        /// </summary>
        protected internal virtual FieldType TypeNameMetaFieldType { get; } = new TypeNameMetaFieldType();
    }
}
