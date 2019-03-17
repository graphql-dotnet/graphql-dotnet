using System;
using System.Linq;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __Type : ObjectGraphType
    {
        public __Type()
        {
            Name = nameof(__Type);
            Description =
                "The fundamental unit of any GraphQL Schema is the type. There are " +
                "many kinds of types in GraphQL as represented by the `__TypeKind` enum." +
                $"{Environment.NewLine}{Environment.NewLine}Depending on the kind of a type, certain fields describe " +
                "information about that type. Scalar types provide no information " +
                "beyond a name and description, while Enum types provide their values. " +
                "Object and Interface types provide the fields they describe. Abstract " +
                "types, Union and Interface, provide the Object types possible " +
                "at runtime. List and NonNull types compose other types.";
            Field<NonNullGraphType<__TypeKind>>("kind", null, null, context =>
            {
                if (context.Source is GraphType type)
                {
                    return KindForInstance(type);
                }

                throw new ExecutionError("Unknown kind of type: {0}".ToFormat(context.Source));
            });
            Field<StringGraphType>("name", resolve: context =>
            {
                return ((IGraphType)context.Source).Name;
            });
            Field<StringGraphType>("description");
            Field<ListGraphType<NonNullGraphType<__Field>>>("fields", null,
                new QueryArguments(
                    new QueryArgument<BooleanGraphType>
                    {
                        Name = "includeDeprecated",
                        DefaultValue = false
                    }),
                context =>
                {
                    if (context.Source is IObjectGraphType || context.Source is IInterfaceGraphType)
                    {
                        var includeDeprecated = context.GetArgument<bool>("includeDeprecated");
                        var type = context.Source as IComplexGraphType;
                        var fields = !includeDeprecated
                            ? type?.Fields.Where(f => string.IsNullOrWhiteSpace(f.DeprecationReason))
                            : type?.Fields;

                        return fields.OrderBy(f => f.Name);
                    }
                    return null;
                });
            Field<ListGraphType<NonNullGraphType<__Type>>>("interfaces", resolve: context =>
            {
                var type = context.Source as IImplementInterfaces;
                return type?.ResolvedInterfaces;
            });
            Field<ListGraphType<NonNullGraphType<__Type>>>("possibleTypes", resolve: context =>
            {
                if (context.Source is IAbstractGraphType type)
                {
                    return type.PossibleTypes;
                }

                return null;
            });
            Field<ListGraphType<NonNullGraphType<__EnumValue>>>("enumValues", null,
                new QueryArguments(new QueryArgument<BooleanGraphType>
                {
                    Name = "includeDeprecated",
                    DefaultValue = false
                }),
                context =>
                {
                    if (context.Source is EnumerationGraphType type)
                    {
                        var includeDeprecated = context.GetArgument<bool>("includeDeprecated");
                        var values = !includeDeprecated
                            ? type.Values.Where(e => string.IsNullOrWhiteSpace(e.DeprecationReason)).ToList()
                            : type.Values.ToList();
                        return values;
                    }

                    return null;
                });
            Field<ListGraphType<NonNullGraphType<__InputValue>>>("inputFields", resolve: context =>
            {
                var type = context.Source as IInputObjectGraphType;
                return type?.Fields;
            });
            Field<__Type>("ofType", resolve: context =>
            {
                if (context.Source == null) return null;

                if (context.Source is NonNullGraphType type)
                {
                    return type.ResolvedType;
                }

                if (context.Source is ListGraphType graphType)
                {
                    return graphType.ResolvedType;
                }

                return null;
            });
        }

        public TypeKind KindForInstance(GraphType type)
        {
            if (type is EnumerationGraphType)
            {
                return TypeKind.ENUM;
            }
            if (type is ScalarGraphType)
            {
                return TypeKind.SCALAR;
            }
            if (type is IObjectGraphType)
            {
                return TypeKind.OBJECT;
            }
            if (type is IInterfaceGraphType)
            {
                return TypeKind.INTERFACE;
            }
            if (type is UnionGraphType)
            {
                return TypeKind.UNION;
            }
            if (type is IInputObjectGraphType)
            {
                return TypeKind.INPUT_OBJECT;
            }
            if (type is ListGraphType)
            {
                return TypeKind.LIST;
            }
            if (type is NonNullGraphType)
            {
                return TypeKind.NON_NULL;
            }

            throw new ExecutionError("Unknown kind of type: {0}".ToFormat(type));
        }

        public TypeKind KindForType(Type type)
        {
            if (typeof(EnumerationGraphType).IsAssignableFrom(type))
            {
                return TypeKind.ENUM;
            }
            if (typeof(ScalarGraphType).IsAssignableFrom(type))
            {
                return TypeKind.SCALAR;
            }
            if (typeof(IObjectGraphType).IsAssignableFrom(type))
            {
                return TypeKind.OBJECT;
            }
            if (typeof(IInterfaceGraphType).IsAssignableFrom(type))
            {
                return TypeKind.INTERFACE;
            }
            if (typeof(UnionGraphType).IsAssignableFrom(type))
            {
                return TypeKind.UNION;
            }
            if (typeof (IInputObjectGraphType).IsAssignableFrom(type))
            {
                return TypeKind.INPUT_OBJECT;
            }
            if (typeof (ListGraphType).IsAssignableFrom(type))
            {
                return TypeKind.LIST;
            }
            if (typeof(NonNullGraphType).IsAssignableFrom(type))
            {
                return TypeKind.NON_NULL;
            }

            throw new ExecutionError("Unknown kind of type: {0}".ToFormat(type));
        }
    }
}
