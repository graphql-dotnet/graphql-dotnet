using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Conversion;
using GraphQL.Introspection;

namespace GraphQL.Types
{
    public class GraphTypesLookup
    {
        private readonly Dictionary<string, IGraphType> _types = new Dictionary<string, IGraphType>();

        private readonly object _lock = new object();

        public GraphTypesLookup()
        {
            AddType<StringGraphType>();
            AddType<BooleanGraphType>();
            AddType<FloatGraphType>();
            AddType<IntGraphType>();
            AddType<IdGraphType>();
            AddType<DateGraphType>();
            AddType<DecimalGraphType>();

            AddType<__Schema>();
            AddType<__Type>();
            AddType<__Directive>();
            AddType<__Field>();
            AddType<__EnumValue>();
            AddType<__InputValue>();
            AddType<__TypeKind>();
        }

        public static GraphTypesLookup Create(
            IEnumerable<IGraphType> types,
            IEnumerable<DirectiveGraphType> directives,
            Func<Type, IGraphType> resolveType,
            IFieldNameConverter fieldNameConverter)
        {
            var lookup = new GraphTypesLookup();
            lookup.FieldNameConverter = fieldNameConverter ?? new CamelCaseFieldNameConverter();

            var ctx = new TypeCollectionContext(resolveType, (name, graphType, context) =>
            {
                if (lookup[name] == null)
                {
                    lookup.AddType(graphType, context);
                }
            });

            types.Apply(type =>
            {
                lookup.AddType(type, ctx);
            });

            var introspectionType = typeof(SchemaIntrospection);

            lookup.HandleField(introspectionType, SchemaIntrospection.SchemaMeta, ctx);
            lookup.HandleField(introspectionType, SchemaIntrospection.TypeMeta, ctx);
            lookup.HandleField(introspectionType, SchemaIntrospection.TypeNameMeta, ctx);

            directives.Apply(directive =>
            {
                directive.Arguments?.Apply(arg =>
                {
                    if (arg.ResolvedType != null)
                    {
                        return;
                    }

                    arg.ResolvedType = lookup.BuildNamedType(arg.Type, ctx.ResolveType);
                });
            });

            return lookup;
        }

        public IFieldNameConverter FieldNameConverter { get; set; } = new CamelCaseFieldNameConverter();

        public void Clear()
        {
            lock (_lock)
            {
                _types.Clear();
            }
        }

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
                lock (_lock)
                {
                    _types[typeName.TrimGraphQLTypes()] = value;
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
            var context = new TypeCollectionContext(
                type =>
                {
                    return BuildNamedType(type, t => (IGraphType) Activator.CreateInstance(t));
                },
                (name, type, _) =>
                {
                    var trimmed = name.TrimGraphQLTypes();
                    lock (_lock)
                    {
                        _types[trimmed] = type;
                    }
                    _?.AddType(trimmed, type, null);
                });

            AddType<TType>(context);
        }

        private IGraphType BuildNamedType(Type type, Func<Type, IGraphType> resolver)
        {
            return type.BuildNamedType(t =>
            {
                var exists = this[t];

                if (exists != null)
                {
                    return exists;
                }

                return resolver(t);
            });
        }

        public void AddType<TType>(TypeCollectionContext context)
            where TType : IGraphType
        {
            var type = typeof(TType).GetNamedType();
            var instance = context.ResolveType(type);
            AddType(instance, context);
        }

        public void AddType(IGraphType type, TypeCollectionContext context)
        {
            if (type == null)
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
                _types[name] = type;
            }

            if (type is IComplexGraphType)
            {
                var complexType = type as IComplexGraphType;
                complexType.Fields.Apply(field =>
                {
                    HandleField(type.GetType(), field, context);
                });
            }

            if (type is IObjectGraphType)
            {
                var obj = (IObjectGraphType) type;
                obj.Interfaces.Apply(objectInterface =>
                {
                    AddTypeIfNotRegistered(objectInterface, context);

                    var interfaceInstance = this[objectInterface] as IInterfaceGraphType;
                    if (interfaceInstance != null)
                    {
                        obj.AddResolvedInterface(interfaceInstance);
                        interfaceInstance.AddPossibleType(obj);

                        if (interfaceInstance.ResolveType == null && obj.IsTypeOf == null)
                        {
                            throw new ExecutionError((
                                "Interface type {0} does not provide a \"resolveType\" function " +
                                "and possible Type \"{1}\" does not provide a \"isTypeOf\" function.  " +
                                "There is no way to resolve this possible type during execution.")
                                .ToFormat(interfaceInstance, obj));
                        }
                    }
                });
            }

            if (type is UnionGraphType)
            {
                var union = (UnionGraphType) type;

                if (!union.Types.Any() && !union.PossibleTypes.Any())
                {
                    throw new ExecutionError("Must provide types for Union {0}.".ToFormat(union));
                }

                union.PossibleTypes.Apply(unionedType =>
                {
                    AddTypeIfNotRegistered(unionedType, context);

                    if (union.ResolveType == null && unionedType.IsTypeOf == null)
                    {
                        throw new ExecutionError((
                            "Union type {0} does not provide a \"resolveType\" function" +
                            "and possible Type \"{1}\" does not provide a \"isTypeOf\" function.  " +
                            "There is no way to resolve this possible type during execution.")
                            .ToFormat(union, unionedType));
                    }
                });

                union.Types.Apply(unionedType =>
                {
                    AddTypeIfNotRegistered(unionedType, context);

                    var objType = this[unionedType] as IObjectGraphType;

                    if (union.ResolveType == null && objType != null && objType.IsTypeOf == null)
                    {
                        throw new ExecutionError((
                            "Union type {0} does not provide a \"resolveType\" function" +
                            "and possible Type \"{1}\" does not provide a \"isTypeOf\" function.  " +
                            "There is no way to resolve this possible type during execution.")
                            .ToFormat(union, objType));
                    }

                    union.AddPossibleType(objType);
                });
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

            field.Arguments?.Apply(arg =>
            {
                if (arg.ResolvedType != null)
                {
                    AddTypeIfNotRegistered(arg.ResolvedType, context);
                    return;
                }

                AddTypeIfNotRegistered(arg.Type, context);
                arg.ResolvedType = BuildNamedType(arg.Type, context.ResolveType);
            });
        }

        private void AddTypeIfNotRegistered(Type type, TypeCollectionContext context)
        {
            var namedType = type.GetNamedType();
            var foundType = this[namedType];
            if (foundType == null)
            {
                AddType(context.ResolveType(namedType), context);
            }
        }

        private void AddTypeIfNotRegistered(IGraphType type, TypeCollectionContext context)
        {
            var namedType = type.GetNamedType();
            var foundType = this[namedType.Name];
            if(foundType == null)
            {
                AddType(namedType, context);
            }
        }
    }
}
