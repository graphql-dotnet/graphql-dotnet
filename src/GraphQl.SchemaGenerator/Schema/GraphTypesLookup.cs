using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Introspection;
using GraphQL.SchemaGenerator.Extensions;
using GraphQL.SchemaGenerator.Wrappers;
using GraphQL.Types;

namespace GraphQL.SchemaGenerator.Schema
{
    // Modified from GraphQL to add NonNullGraphTypes
    public class GraphTypesLookup
    {
        private readonly Dictionary<string, GraphType> _types = new Dictionary<string, GraphType>();

        public GraphTypesLookup()
        {
            AddType<StringGraphType>();
            AddType<BooleanGraphType>();
            AddType<FloatGraphType>();
            AddType<IntGraphType>();
            AddType<IdGraphType>();
            AddType<DateGraphType>();
            AddType<DecimalGraphType>();

            AddType<NonNullGraphType<StringGraphType>>();
            AddType<NonNullGraphType<BooleanGraphType>>();
            AddType<NonNullGraphType<FloatGraphType>>();
            AddType<NonNullGraphType<IntGraphType>>();
            AddType<NonNullGraphType<IdGraphType>>();
            AddType<NonNullGraphType<DateGraphType>>();
            AddType<NonNullGraphType<DecimalGraphType>>();

            AddType<__Schema>();
            AddType<__Type>();
            AddType<__Field>();
            AddType<__EnumValue>();
            AddType<__InputValue>();
            AddType<__TypeKind>();
            AddType<__Directive>();
        }

        public static GraphTypesLookup Create(IEnumerable<GraphType> types, Func<Type, GraphType> resolveType)
        {
            var lookup = new GraphTypesLookup();

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
            };

            return lookup;
        }

        public void Clear()
        {
            _types.Clear();
        }

        public IEnumerable<GraphType> All()
        {
            return _types.Values;
        }

        public GraphType this[string typeName]
        {
            get
            {
                GraphType result;
                _types.TryGetValue(typeName, out result);
                return result;
            }
            set { _types[typeName] = value; }
        }

        public GraphType this[Type type]
        {
            get
            {
                var result = _types.FirstOrDefault(x => x.Value.GetType() == type);
                return result.Value;
            }
        }

        public IEnumerable<GraphType> FindImplemenationsOf(Type type)
        {
            return _types
                .Values
                .OfType<ObjectGraphType>()
                .Where(t => Enumerable.Any(t.Interfaces, i => i == type))
                .ToList();
        }

        public void AddType<TType>()
            where TType : GraphType, new()
        {
            var context = new TypeCollectionContext(
                type => (GraphType)Activator.CreateInstance(type),
                (name, type, _) =>
                {
                    _types[name] = type;
                    _?.AddType(name, type, null);
                });

            AddType<TType>(context);
        }

        public void AddType<TType>(TypeCollectionContext context)
            where TType : GraphType
        {
            var instance = context.ResolveType(typeof(TType));
            AddType(instance, context);
        }

        public void AddType(GraphType type, TypeCollectionContext context)
        {
            if (type == null)
            {
                return;
            }

            var name = type.CollectTypes(context);
            _types[name] = type;

            bool isInput = type is InputObjectGraphType;

            foreach (var field in type.Fields)
            {
                field.Type = updateInputFieldType(isInput, field.Type);
                AddTypeIfNotRegistered(field.Type, context);

                if (field.Arguments != null)
                {
                    foreach (var arg in field.Arguments)
                    {
                        AddTypeIfNotRegistered(arg.Type, context);
                    }
                }
            }

            if (type is ObjectGraphType)
            {
                var obj = (ObjectGraphType)type;
                foreach (var objectInterface in obj.Interfaces)
                {
                    AddTypeIfNotRegistered(objectInterface, context);

                    var interfaceInstance = this[objectInterface] as InterfaceGraphType;
                    if (interfaceInstance != null)
                    {
                        interfaceInstance.AddPossibleType(obj);

                        if (interfaceInstance.ResolveType == null && obj.IsTypeOf == null)
                        {
                            throw new ExecutionError((
                                "Interface type {0} does not provide a \"resolveType\" function" +
                                "and possible Type \"{1}\" does not provide a \"isTypeOf\" function.  " +
                                "There is no way to resolve this possible type during execution.")
                                .ToFormat(interfaceInstance, obj));
                        }
                    }
                }
            }

            if (type is UnionGraphType)
            {
                var union = (UnionGraphType)type;

                if (!union.Types.Any())
                {
                    throw new ExecutionError("Must provide types for Union {0}.".ToFormat(union));
                }

                union.Types.Apply(unionedType =>
                {
                    AddTypeIfNotRegistered(unionedType, context);

                    var objType = this[unionedType] as ObjectGraphType;

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

        private Type updateInputFieldType(bool isInput, Type type)
        {
            var newType = type;
            if (isInput)
            {
                if (type.IsAssignableToGenericType(typeof(ObjectGraphTypeWrapper<>)))
                {
                    newType = typeof(InputObjectGraphTypeWrapper<>).MakeGenericType(type.GetGenericArguments()[0]);
                }
                else if (type.IsAssignableToGenericType(typeof(KeyValuePairGraphType<,>)))
                {
                    var genericArgs = type.GetGenericArguments();                    
                    newType = typeof(KeyValuePairInputGraphType<,>).MakeGenericType(
                        genericArgs[0],
                        genericArgs[1]);
                }
                else if (typeof(ObjectGraphType).IsAssignableFrom(type))
                {
                    newType = typeof(InputObjectGraphType);
                }
                else if (type.IsAssignableToGenericType(typeof(InterfaceGraphTypeWrapper<>)))
                {
                    newType = typeof(InputObjectGraphTypeWrapper<>).MakeGenericType(type);
                }
                else if (typeof(InterfaceGraphType).IsAssignableFrom(type))
                {
                    newType = typeof(InputObjectGraphType);
                }
                else if (typeof(ListGraphType).IsAssignableFrom(type))
                {
                    var itemType = type.GetGenericArguments()[0];
                    var newItemType = updateInputFieldType(isInput, itemType);
                    if (itemType != newItemType)
                    {
                        newType = typeof(ListGraphType<>).MakeGenericType(newItemType);
                    }
                }
                else if (typeof(EnumerationGraphType).IsAssignableFrom(type))
                {
                    newType = typeof(IntGraphType);
                }
                else if (type.IsAssignableToGenericType(typeof(EnumerationGraphTypeWrapper<>)))
                {
                    newType = typeof(IntGraphType);
                }
            }

            return newType;
        }

        private void AddTypeIfNotRegistered(Type type, TypeCollectionContext context)
        {
            var foundType = this[type];
            if (foundType == null)
            {
                AddType(context.ResolveType(type), context);
            }
        }
    }
}
