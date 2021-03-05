using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using GraphQL.Conversion;
using GraphQL.Instrumentation;
using GraphQL.Introspection;
using GraphQL.Resolvers;
using GraphQL.Types.Relay;
using GraphQL.Utilities;

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
        internal static readonly Dictionary<Type, Type> BuiltInScalarMappings = new Dictionary<Type, Type>
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
            [typeof(DateTimeOffset)] = typeof(DateTimeOffsetGraphType),
            [typeof(TimeSpan)] = typeof(TimeSpanSecondsGraphType),
            [typeof(Guid)] = typeof(IdGraphType),
            [typeof(short)] = typeof(ShortGraphType),
            [typeof(ushort)] = typeof(UShortGraphType),
            [typeof(ulong)] = typeof(ULongGraphType),
            [typeof(uint)] = typeof(UIntGraphType),
            [typeof(byte)] = typeof(ByteGraphType),
            [typeof(sbyte)] = typeof(SByteGraphType),
        };

        // Introspection types http://spec.graphql.org/June2018/#sec-Schema-Introspection
        private readonly Dictionary<Type, IGraphType> _introspectionTypes;

        // Standard scalars https://graphql.github.io/graphql-spec/June2018/#sec-Scalars
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

        private readonly TypeCollectionContext _context;
        private readonly INameConverter _nameConverter;
        private readonly object _lock = new object();
        private bool _sealed;

        private SchemaTypes(ISchema schema, List<(Type clrType, Type graphType)> typeMappings)
        {
            _introspectionTypes = CreateIntrospectionTypes(schema.Features.AppliedDirectives, schema.Features.RepeatableDirectives);

            _context = new TypeCollectionContext(
               type => BuildNamedType(type, t => _builtInScalars.TryGetValue(t, out var graphType) ? graphType : _introspectionTypes.TryGetValue(t, out graphType) ? graphType : (IGraphType)Activator.CreateInstance(t)),
               (name, type, ctx) =>
               {
                   lock (_lock)
                   {
                       SetGraphType(name, type);
                   }
                   ctx.AddType(name, type, null);
               },
               typeMappings);

            // Add introspection types. Note that introspection types rely on the
            // CamelCaseNameConverter, as some fields are defined in pascal case - e.g. Field(x => x.Name)
            _nameConverter = CamelCaseNameConverter.Instance;

            foreach (var introspectionType in _introspectionTypes.Values)
                AddType(introspectionType, _context);

            // set the name converter properly
            _nameConverter = schema.NameConverter ?? CamelCaseNameConverter.Instance;
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

        internal Dictionary<string, IGraphType> Dictionary { get; } = new Dictionary<string, IGraphType>();

        /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
        public IEnumerator<IGraphType> GetEnumerator() => Dictionary.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets the count of all the graph types utilized by the schema.
        /// </summary>
        public int Count => Dictionary.Count;

        private void CheckSealed()
        {
            if (_sealed)
                throw new InvalidOperationException("GraphTypesLookup is sealed for modifications. You attempt to modify schema after it was initialized.");
        }

        private IGraphType BuildNamedType(Type type, Func<Type, IGraphType> resolver) => type.BuildNamedType(t => this[t] ?? resolver(t));

        /// <summary>
        /// Initializes a new instance for the specified graph types and directives, and with the specified type resolver and name converter.
        /// </summary>
        /// <param name="types">A list of graph type instances to register in the lookup table.</param>
        /// <param name="typeMappings">CLR-GraphType type mappings.</param>
        /// <param name="directives">A list of directives to register.</param>
        /// <param name="resolveType">A delegate which returns an instance of a graph type from its .NET type.</param>
        /// <param name="schema">A schema for which this instance is created.</param>
        public static SchemaTypes Create(
            IEnumerable<IGraphType> types,
            List<(Type clrType, Type graphType)> typeMappings,
            IEnumerable<DirectiveGraphType> directives,
            Func<Type, IGraphType> resolveType,
            ISchema schema)
        {
            var lookup = new SchemaTypes(schema, typeMappings);

            var ctx = new TypeCollectionContext(
                t => lookup._builtInScalars.TryGetValue(t, out var graphType) ? graphType : resolveType(t),
                (name, graphType, context) =>
                {
                    if (lookup[name] == null)
                    {
                        lookup.AddType(graphType, context);
                    }
                },
                typeMappings);

            foreach (var type in types)
            {
                lookup.AddType(type, ctx);
            }

            // these fields must not have their field names translated by INameConverter; see HandleField
            lookup.HandleField(null, lookup.SchemaMetaFieldType, ctx, false);
            lookup.HandleField(null, lookup.TypeMetaFieldType, ctx, false);
            lookup.HandleField(null, lookup.TypeNameMetaFieldType, ctx, false);

            foreach (var directive in directives)
            {
                if (directive.Arguments?.Count > 0)
                {
                    foreach (var arg in directive.Arguments.List)
                    {
                        if (arg.ResolvedType != null)
                        {
                            lookup.AddTypeIfNotRegistered(arg.ResolvedType, ctx);
                            arg.ResolvedType = lookup.ConvertTypeReference(directive, arg.ResolvedType);
                        }
                        else
                        {
                            lookup.AddTypeIfNotRegistered(arg.Type, ctx);
                            arg.ResolvedType = lookup.BuildNamedType(arg.Type, ctx.ResolveType);
                        }
                    }
                }
            }

            lookup.ApplyTypeReferences();

            Debug.Assert(ctx.InFlightRegisteredTypes.Count == 0);
            lookup._sealed = true;

            return lookup;
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
        public IGraphType this[string typeName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    throw new ArgumentOutOfRangeException(nameof(typeName), "A type name is required to lookup.");
                }

                IGraphType type;
                lock (_lock)
                {
                    Dictionary.TryGetValue(typeName, out type);
                }
                return type;
            }
            internal set
            {
                CheckSealed();

                lock (_lock)
                {
                    SetGraphType(typeName, value);
                }
            }
        }

        /// <summary>
        /// Returns a graph type instance from the lookup table by its .NET type.
        /// </summary>
        /// <param name="type">The .NET type of the graph type.</param>
        internal IGraphType this[Type type]
        {
            get
            {
                if (type == null)
                    throw new ArgumentOutOfRangeException(nameof(type), "A type is required to lookup.");

                lock (_lock)
                {
                    foreach (var item in Dictionary)
                    {
                        if (item.Value.GetType() == type)
                            return item.Value;
                    }

                    return null;
                }
            }
        }

        private void AddType(IGraphType type, TypeCollectionContext context)
        {
            CheckSealed();

            if (type == null || type is GraphQLTypeReference)
            {
                return;
            }

            if (type is NonNullGraphType || type is ListGraphType)
            {
                throw new ArgumentOutOfRangeException(nameof(type), "Only add root types.");
            }

            string name = context.CollectTypes(type);
            lock (_lock)
            {
                SetGraphType(name, type);
            }

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
                    AddTypeIfNotRegistered(objectInterface, context);

                    if (this[objectInterface] is IInterfaceGraphType interfaceInstance)
                    {
                        obj.AddResolvedInterface(interfaceInstance);
                        interfaceInstance.AddPossibleType(obj);

                        if (interfaceInstance.ResolveType == null && obj.IsTypeOf == null)
                        {
                            throw new InvalidOperationException(
                               $"Interface type \"{interfaceInstance.Name}\" does not provide a \"resolveType\" function " +
                               $"and possible Type \"{obj.Name}\" does not provide a \"isTypeOf\" function. " +
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
                           $"Union type \"{union.Name}\" does not provide a \"resolveType\" function " +
                           $"and possible Type \"{unionedType.Name}\" does not provide a \"isTypeOf\" function. " +
                            "There is no way to resolve this possible type during execution.");
                    }
                }

                foreach (var unionedType in union.Types)
                {
                    AddTypeIfNotRegistered(unionedType, context);

                    var objType = this[unionedType] as IObjectGraphType;

                    if (union.ResolveType == null && objType != null && objType.IsTypeOf == null)
                    {
                        throw new InvalidOperationException(
                           $"Union type \"{union.Name}\" does not provide a \"resolveType\" function " +
                           $"and possible Type \"{objType.Name}\" does not provide a \"isTypeOf\" function. " +
                            "There is no way to resolve this possible type during execution.");
                    }

                    union.AddPossibleType(objType);
                }
            }
        }

        private void HandleField(IComplexGraphType parentType, FieldType field, TypeCollectionContext context, bool applyNameConverter)
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
                field.Name = _nameConverter.NameForField(field.Name, parentType);
                NameValidator.ValidateNameOnSchemaInitialize(field.Name, NamedElement.Field);
            }

            if (field.ResolvedType == null)
            {
                if (field.Type == null)
                    throw new InvalidOperationException($"Both ResolvedType and Type properties on field '{parentType?.Name}.{field.Name}' are null.");

                object typeOrError = RebuildType(field.Type, parentType is IInputObjectGraphType, context.TypeMappings);
                if (typeOrError is string error)
                    throw new InvalidOperationException($"The GraphQL type for field '{parentType.Name}.{field.Name}' could not be derived implicitly. " + error);
                field.Type = (Type)typeOrError;

                AddTypeIfNotRegistered(field.Type, context);
                field.ResolvedType = BuildNamedType(field.Type, context.ResolveType);
            }
            else
            {
                AddTypeIfNotRegistered(field.ResolvedType, context);
            }

            if (field.Arguments?.Count > 0)
            {
                foreach (var arg in field.Arguments.List)
                {
                    if (applyNameConverter)
                    {
                        arg.Name = _nameConverter.NameForArgument(arg.Name, parentType, field);
                        NameValidator.ValidateNameOnSchemaInitialize(arg.Name, NamedElement.Argument);
                    }

                    if (arg.ResolvedType != null)
                    {
                        AddTypeIfNotRegistered(arg.ResolvedType, context);
                        continue;
                    }

                    AddTypeIfNotRegistered(arg.Type, context);
                    arg.ResolvedType = BuildNamedType(arg.Type, context.ResolveType);
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

        private void AddTypeIfNotRegistered(Type type, TypeCollectionContext context)
        {
            var namedType = type.GetNamedType();
            var foundType = this[namedType];
            if (foundType == null)
            {
                if (namedType == typeof(PageInfoType))
                {
                    AddType(new PageInfoType(), context);
                }
                else if (namedType.IsGenericType && (namedType.ImplementsGenericType(typeof(EdgeType<>)) || namedType.ImplementsGenericType(typeof(ConnectionType<,>))))
                {
                    AddType((IGraphType)Activator.CreateInstance(namedType), context);
                }
                else if (_builtInCustomScalars.TryGetValue(namedType, out var builtInCustomScalar))
                {
                    AddType(builtInCustomScalar, _context);
                }
                else
                {
                    AddTypeWithLoopCheck(context.ResolveType(namedType), context, namedType);
                }
            }
        }

        private void AddTypeIfNotRegistered(IGraphType type, TypeCollectionContext context)
        {
            var (namedType, namedType2) = type.GetNamedTypes();
            namedType ??= context.ResolveType(namedType2);

            var foundType = this[namedType.Name];
            if (foundType == null)
            {
                AddType(namedType, context);
            }
        }

        private object RebuildType(Type type, bool input, List<(Type, Type)> typeMappings)
        {
            if (!type.IsGenericType)
                return type;

            var genericDef = type.GetGenericTypeDefinition();
            if (genericDef == typeof(NonNullGraphType<>) || genericDef == typeof(ListGraphType<>))
            {
                var innerType = type.GenericTypeArguments[0];
                object typeOrError = RebuildType(innerType, input, typeMappings);
                if (typeOrError is string)
                    return typeOrError;
                var changed = (Type)typeOrError;
                return changed == innerType ? type : genericDef.MakeGenericType(changed);
            }
            else if (genericDef == typeof(GraphQLClrOutputTypeReference<>) || genericDef == typeof(GraphQLClrInputTypeReference<>))
            {
                return GetGraphType(type.GetGenericArguments()[0], input, typeMappings);
            }
            else
            {
                return type;
            }
        }

        private object GetGraphType(Type clrType, bool input, List<(Type clr, Type graph)> typeMappings)
        {
            // check custom mappings first
            if (typeMappings != null)
            {
                foreach (var mapping in typeMappings)
                {
                    if (mapping.clr == clrType)
                    {
                        if (input && mapping.graph.IsInputType() || !input && mapping.graph.IsOutputType())
                            return mapping.graph;
                    }
                }
            }

            // then built-in mappings
            if (BuiltInScalarMappings.TryGetValue(clrType, out var graphType))
                return graphType;

            return $"Could not find type mapping from CLR type '{clrType.FullName}' to GraphType. Did you forget to register the type mapping with the '{nameof(ISchema)}.{nameof(ISchema.RegisterTypeMapping)}'?";
        }

        private void ApplyTypeReferences()
        {
            CheckSealed();

            // ToList() is a necessary measure here since otherwise we get System.InvalidOperationException: 'Collection was modified; enumeration operation may not execute.'
            foreach (var type in Dictionary.Values.ToList())
            {
                ApplyTypeReference(type);
            }
        }

        private void ApplyTypeReference(IGraphType type)
        {
            CheckSealed();

            if (type is IComplexGraphType complexType)
            {
                foreach (var field in complexType.Fields)
                {
                    field.ResolvedType = ConvertTypeReference(type, field.ResolvedType);

                    if (field.Arguments?.Count > 0)
                    {
                        foreach (var arg in field.Arguments.List)
                        {
                            arg.ResolvedType = ConvertTypeReference(type, arg.ResolvedType);
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
                               $"Interface type \"{interfaceType.Name}\" does not provide a \"resolveType\" function " +
                               $"and possible Type \"{objectType.Name}\" does not provide a \"isTypeOf\" function.  " +
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
                           $"Union type \"{union.Name}\" does not provide a \"resolveType\" function " +
                           $"and possible Type \"{union.Name}\" does not provide a \"isTypeOf\" function. " +
                            "There is no way to resolve this possible type during execution.");
                    }

                    list[i] = unionType;
                }
            }
        }

        private IGraphType ConvertTypeReference(INamedType parentType, IGraphType type)
        {
            if (type is NonNullGraphType nonNull)
            {
                nonNull.ResolvedType = ConvertTypeReference(parentType, nonNull.ResolvedType);
                return nonNull;
            }

            if (type is ListGraphType list)
            {
                list.ResolvedType = ConvertTypeReference(parentType, list.ResolvedType);
                return list;
            }

            var reference = type as GraphQLTypeReference;
            if (reference != null)
            {
                type = this[reference.TypeName];
                if (type == null)
                {
                    type = _builtInScalars.Values.FirstOrDefault(t => t.Name == reference.TypeName) ?? _builtInCustomScalars.Values.FirstOrDefault(t => t.Name == reference.TypeName);
                    if (type != null)
                        this[type.Name] = type;
                }
            }

            if (reference != null && type == null)
            {
                throw new InvalidOperationException($"Unable to resolve reference to type '{reference.TypeName}' on '{parentType.Name}'");
            }

            return type;
        }

        private void SetGraphType(string typeName, IGraphType type)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentOutOfRangeException(nameof(typeName), "A type name is required to lookup.");
            }

            if (Dictionary.TryGetValue(typeName, out var existingGraphType))
            {
                if (ReferenceEquals(existingGraphType, type) || existingGraphType.GetType() == type.GetType())
                {
                    // Soft schema configuration error.
                    // Intentionally or inadvertently, a situation may arise when the same GraphType is registered more that one time.
                    // This may be due to the simultaneous registration of GraphType instances and the GraphType types. In this case
                    // the duplicate MUST be ignored, otherwise errors will occur.
                }
                else
                {
                    // Fatal schema configuration error.
                    throw new InvalidOperationException($@"Unable to register GraphType '{type.GetType().FullName}' with the name '{typeName}'.
The name '{typeName}' is already registered to '{existingGraphType.GetType().FullName}'. Check your schema configuration.");
                }
            }
            else
            {
                Dictionary.Add(typeName, type);
            }
        }

        /// <summary>
        /// Returns the <see cref="FieldType"/> instance for the <c>__schema</c> meta-field.
        /// </summary>
        internal FieldType SchemaMetaFieldType { get; } = new SchemaMetaFieldType();

        /// <summary>
        /// Returns the <see cref="FieldType"/> instance for the <c>__type</c> meta-field.
        /// </summary>
        internal FieldType TypeMetaFieldType { get; } = new TypeMetaFieldType();

        /// <summary>
        /// Returns the <see cref="FieldType"/> instance for the <c>__typename</c> meta-field.
        /// </summary>
        internal FieldType TypeNameMetaFieldType { get; } = new TypeNameMetaFieldType();
    }
}
