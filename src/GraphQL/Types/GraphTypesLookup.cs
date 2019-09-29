using GraphQL.Conversion;
using GraphQL.Introspection;
using GraphQL.Types.Relay;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphQL.Types
{
    public class GraphTypesLookup
    {
        private readonly IDictionary<string, IGraphType> _types = new Dictionary<string, IGraphType>();

        private readonly object _lock = new object();
        private bool _sealed;

        public GraphTypesLookup()
        {
            // standard scalars https://graphql.github.io/graphql-spec/June2018/#sec-Scalars
            AddType<StringGraphType>();
            AddType<BooleanGraphType>();
            AddType<FloatGraphType>();
            AddType<IntGraphType>();
            AddType<IdGraphType>();

            // .NET custom scalars
            AddType<DateGraphType>();
            AddType<DateTimeGraphType>();
            AddType<DateTimeOffsetGraphType>();
            AddType<TimeSpanSecondsGraphType>();
            AddType<TimeSpanMillisecondsGraphType>();
            AddType<DecimalGraphType>();
            AddType<UriGraphType>();
            AddType<GuidGraphType>();
            AddType<ShortGraphType>();
            AddType<UShortGraphType>();
            AddType<UIntGraphType>();
            AddType<LongGraphType>();
            AddType<BigIntegerGraphType>();
            AddType<ULongGraphType>();
            AddType<ByteGraphType>();
            AddType<SByteGraphType>();

            // introspection types
            AddType<__Schema>();
            AddType<__Type>();
            AddType<__Directive>();
            AddType<__Field>();
            AddType<__EnumValue>();
            AddType<__InputValue>();
            AddType<__TypeKind>();
        }

        private void CheckSealed()
        {
            if (_sealed)
                throw new InvalidOperationException("GraphTypesLookup is sealed for modifications. You attempt to modify schema after it was initialized.");
        }

        public static GraphTypesLookup Create(
            IEnumerable<IGraphType> types,
            IEnumerable<DirectiveGraphType> directives,
            Func<Type, IGraphType> resolveType,
            IFieldNameConverter fieldNameConverter,
            bool seal = false)
        {
            var lookup = new GraphTypesLookup
            {
                FieldNameConverter = fieldNameConverter ?? CamelCaseFieldNameConverter.Instance
            };

            var ctx = new TypeCollectionContext(resolveType, (name, graphType, context) =>
            {
                if (lookup[name] == null)
                {
                    lookup.AddType(graphType, context);
                }
            });

            foreach (var type in types)
            {
                lookup.AddType(type, ctx);
            }

            var introspectionType = typeof(SchemaIntrospection);

            lookup.HandleField(introspectionType, SchemaIntrospection.SchemaMeta, ctx);
            lookup.HandleField(introspectionType, SchemaIntrospection.TypeMeta, ctx);
            lookup.HandleField(introspectionType, SchemaIntrospection.TypeNameMeta, ctx);

            foreach (var directive in directives)
            {
                if (directive.Arguments == null)
                    continue;

                foreach (var arg in directive.Arguments)
                {
                    if (arg.ResolvedType != null)
                    {
                        arg.ResolvedType = lookup.ConvertTypeReference(directive, arg.ResolvedType);
                    }
                    else
                    {
                        arg.ResolvedType = lookup.BuildNamedType(arg.Type, ctx.ResolveType);
                    }
                }
            }

            lookup.ApplyTypeReferences();

            lookup._sealed = seal;

            return lookup;
        }

        public IFieldNameConverter FieldNameConverter { get; set; } = CamelCaseFieldNameConverter.Instance;

        internal void Clear(bool internalCall)
        {
            if (!internalCall)
                CheckSealed();

            lock (_lock)
            {
                _types.Clear();
            }
        }

        public void Clear() => Clear(false);

        public IEnumerable<IGraphType> All()
        {
            lock (_lock)
            {
                return _types.Values.ToList();
            }
        }

        public IGraphType this[string typeName]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    throw new ArgumentOutOfRangeException(nameof(typeName), "A type name is required to lookup.");
                }

                IGraphType type;
                var name = typeName.TrimGraphQLTypes();
                lock (_lock)
                {
                    _types.TryGetValue(name, out type);
                }
                return type;
            }
            set
            {
                CheckSealed();

                lock (_lock)
                {
                    SetGraphType(typeName.TrimGraphQLTypes(), value);
                }
            }
        }

        public IGraphType this[Type type]
        {
            get
            {
                lock (_lock)
                {
                    var result = _types.FirstOrDefault(x => x.Value.GetType() == type);
                    return result.Value;
                }
            }
        }

        public void AddType<TType>()
            where TType : IGraphType, new()
        {
            CheckSealed();

            var context = new TypeCollectionContext(
                type =>
                {
                    return BuildNamedType(type, t => (IGraphType)Activator.CreateInstance(t));
                },
                (name, type, ctx) =>
                {
                    var trimmed = name.TrimGraphQLTypes();
                    lock (_lock)
                    {
                        SetGraphType(trimmed, type);
                    }
                    ctx?.AddType(trimmed, type, null);
                });

            AddType<TType>(context);
        }

        private IGraphType BuildNamedType(Type type, Func<Type, IGraphType> resolver)
        {
            return type.BuildNamedType(t => this[t] ?? resolver(t));
        }

        public void AddType<TType>(TypeCollectionContext context)
            where TType : IGraphType
        {
            CheckSealed();

            var type = typeof(TType).GetNamedType();
            var instance = context.ResolveType(type);
            AddType(instance, context);
        }

        public void AddType(IGraphType type, TypeCollectionContext context)
        {
            CheckSealed();

            if (type == null || type is GraphQLTypeReference)
            {
                return;
            }

            if (type is NonNullGraphType || type is ListGraphType)
            {
                throw new ExecutionError("Only add root types.");
            }

            var name = type.CollectTypes(context).TrimGraphQLTypes();
            lock (_lock)
            {
                SetGraphType(name, type);
            }

            if (type is IComplexGraphType complexType)
            {
                foreach (var field in complexType.Fields)
                {
                    HandleField(type.GetType(), field, context);
                }
            }

            if (type is IObjectGraphType obj)
            {
                foreach (var objectInterface in obj.Interfaces)
                {
                    AddTypeIfNotRegistered(objectInterface, context);

                    if (this[objectInterface] is IInterfaceGraphType interfaceInstance)
                    {
                        obj.AddResolvedInterface(interfaceInstance);
                        interfaceInstance.AddPossibleType(obj);

                        if (interfaceInstance.ResolveType == null && obj.IsTypeOf == null)
                        {
                            throw new ExecutionError((
                                "Interface type {0} does not provide a \"resolveType\" function " +
                                "and possible Type \"{1}\" does not provide a \"isTypeOf\" function.  " +
                                "There is no way to resolve this possible type during execution.")
                                .ToFormat(interfaceInstance.Name, obj.Name));
                        }
                    }
                }
            }

            if (type is UnionGraphType union)
            {
                if (!union.Types.Any() && !union.PossibleTypes.Any())
                {
                    throw new ExecutionError("Must provide types for Union {0}.".ToFormat(union));
                }

                foreach (var unionedType in union.PossibleTypes)
                {
                    AddTypeIfNotRegistered(unionedType, context);

                    if (union.ResolveType == null && unionedType.IsTypeOf == null)
                    {
                        throw new ExecutionError((
                            "Union type {0} does not provide a \"resolveType\" function " +
                            "and possible Type \"{1}\" does not provide a \"isTypeOf\" function. " +
                            "There is no way to resolve this possible type during execution.")
                            .ToFormat(union.Name, unionedType.Name));
                    }
                }

                foreach (var unionedType in union.Types)
                {
                    AddTypeIfNotRegistered(unionedType, context);

                    var objType = this[unionedType] as IObjectGraphType;

                    if (union.ResolveType == null && objType != null && objType.IsTypeOf == null)
                    {
                        throw new ExecutionError((
                            "Union type {0} does not provide a \"resolveType\" function " +
                            "and possible Type \"{1}\" does not provide a \"isTypeOf\" function. " +
                            "There is no way to resolve this possible type during execution.")
                            .ToFormat(union.Name, objType.Name));
                    }

                    union.AddPossibleType(objType);
                }
            }
        }

        private void HandleField(Type parentType, FieldType field, TypeCollectionContext context)
        {
            field.Name = FieldNameConverter.NameFor(field.Name, parentType);

            if (field.ResolvedType == null)
            {
                AddTypeIfNotRegistered(field.Type, context);
                field.ResolvedType = BuildNamedType(field.Type, context.ResolveType);
            }
            else
            {
                AddTypeIfNotRegistered(field.ResolvedType, context);
            }

            if (field.Arguments == null)
                return;

            foreach (var arg in field.Arguments)
            {
                arg.Name = FieldNameConverter.NameFor(arg.Name, null);

                if (arg.ResolvedType != null)
                {
                    AddTypeIfNotRegistered(arg.ResolvedType, context);
                    continue;
                }

                AddTypeIfNotRegistered(arg.Type, context);
                arg.ResolvedType = BuildNamedType(arg.Type, context.ResolveType);
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
                    return;
                }
                if (namedType.IsGenericType)
                {
                    if (namedType.ImplementsGenericType(typeof(EdgeType<>)) ||
                        namedType.ImplementsGenericType(typeof(ConnectionType<,>)))
                    {
                        AddType((IGraphType)Activator.CreateInstance(namedType), context);
                        return;
                    }
                }
                AddType(context.ResolveType(namedType), context);
            }
        }

        private void AddTypeIfNotRegistered(IGraphType type, TypeCollectionContext context)
        {
            var namedType = type.GetNamedType();
            var foundType = this[namedType.Name];
            if (foundType == null)
            {
                AddType(namedType, context);
            }
        }

        public void ApplyTypeReferences()
        {
            CheckSealed();

            foreach (var type in _types.Values.ToList())
            {
                ApplyTypeReference(type);
            }
        }

        public void ApplyTypeReference(IGraphType type)
        {
            CheckSealed();

            if (type is IComplexGraphType complexType)
            {
                foreach (var field in complexType.Fields)
                {
                    field.ResolvedType = ConvertTypeReference(type, field.ResolvedType);

                    if (field.Arguments == null)
                        continue;

                    foreach (var arg in field.Arguments)
                    {
                        arg.ResolvedType = ConvertTypeReference(type, arg.ResolvedType);
                    }
                }
            }

            if (type is IObjectGraphType objectType)
            {
                objectType.ResolvedInterfaces = objectType
                    .ResolvedInterfaces
                    .Select(i =>
                    {
                        var interfaceType = (IInterfaceGraphType)ConvertTypeReference(objectType, i);

                        if (objectType.IsTypeOf == null && interfaceType.ResolveType == null)
                        {
                            throw new ExecutionError((
                                    "Interface type {0} does not provide a \"resolveType\" function " +
                                    "and possible Type \"{1}\" does not provide a \"isTypeOf\" function.  " +
                                    "There is no way to resolve this possible type during execution.")
                                .ToFormat(interfaceType.Name, objectType.Name));
                        }

                        interfaceType.AddPossibleType(objectType);

                        return interfaceType;
                    })
                    .ToList();
            }

            if (type is UnionGraphType union)
            {
                union.PossibleTypes = union
                    .PossibleTypes
                    .Select(t =>
                    {
                        var unionType = ConvertTypeReference(union, t) as IObjectGraphType;

                        if (union.ResolveType == null && unionType != null && unionType.IsTypeOf == null)
                        {
                            throw new ExecutionError((
                                "Union type {0} does not provide a \"resolveType\" function " +
                                "and possible Type \"{1}\" does not provide a \"isTypeOf\" function. " +
                                "There is no way to resolve this possible type during execution.")
                                .ToFormat(union.Name, unionType.Name));
                        }

                        return unionType;
                    })
                    .ToList();
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
            var result = reference == null ? type : this[reference.TypeName];

            if (reference != null && result == null)
            {
                throw new ExecutionError($"Unable to resolve reference to type '{reference.TypeName}' on '{parentType.Name}'");
            }

            return result;
        }

        private void SetGraphType(string typeName, IGraphType type)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new ArgumentOutOfRangeException(nameof(typeName), "A type name is required to lookup.");
            }

            if (_types.TryGetValue(typeName, out var existingGraphType))
            {
                if (ReferenceEquals(existingGraphType, type))
                {
                    // nothing to do
                }
                else if (existingGraphType.GetType() == type.GetType())
                {
                    _types[typeName] = type; // this case worked before overwriting the old value
                }
                else
                {
                    throw new InvalidOperationException($@"Unable to register GraphType '{type.GetType().FullName}' with the name '{typeName}';
the name '{typeName}' is already registered to '{existingGraphType.GetType().FullName}'.");
                }
            }
            else
            {
                _types.Add(typeName, type);
            }
        }
    }
}
