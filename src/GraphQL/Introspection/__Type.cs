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
                if (context.Source is IGraphType type)
                {
                    return KindForInstance(type);
                }

                throw new ExecutionError($"Unknown kind of type: {context.Source}");
            });
            Field<StringGraphType>("name", resolve: context =>
            {
                return ((IGraphType)context.Source).Name;
            });
            Field<StringGraphType>("description");
            FieldAsync<ListGraphType<NonNullGraphType<__Field>>>("fields", null,
                new QueryArguments(
                    new QueryArgument<BooleanGraphType>
                    {
                        Name = "includeDeprecated",
                        DefaultValue = false
                    }),
                async context =>
                {
                    if (context.Source is IObjectGraphType || context.Source is IInterfaceGraphType)
                    {
                        var includeDeprecated = context.GetArgument<bool>("includeDeprecated");
                        var type = context.Source as IComplexGraphType;
                        var fields = !includeDeprecated
                            ? type?.Fields.Where(f => string.IsNullOrWhiteSpace(f.DeprecationReason))
                            : type?.Fields;

                        fields = fields ?? Enumerable.Empty<FieldType>();
                        fields = await fields.WhereAsync(f => context.Schema.Filter.AllowField(context.Source as IGraphType, f));

                        return fields.OrderBy(f => f.Name);
                    }
                    return null;
                });
            FieldAsync<ListGraphType<NonNullGraphType<__Type>>>("interfaces", resolve: async context =>
            {
                var type = context.Source as IImplementInterfaces;
                if (type == null) return null;
                return await type.ResolvedInterfaces.WhereAsync(x => context.Schema.Filter.AllowType(x));
            });
            FieldAsync<ListGraphType<NonNullGraphType<__Type>>>("possibleTypes", resolve: async context =>
            {
                if (context.Source is IAbstractGraphType type)
                {
                    return await type.PossibleTypes.WhereAsync(x => context.Schema.Filter.AllowType(x));
                }

                return null;
            });
            FieldAsync<ListGraphType<NonNullGraphType<__EnumValue>>>("enumValues", null,
                new QueryArguments(new QueryArgument<BooleanGraphType>
                {
                    Name = "includeDeprecated",
                    DefaultValue = false
                }),
                async context =>
                {
                    if (context.Source is EnumerationGraphType type)
                    {
                        var includeDeprecated = context.GetArgument<bool>("includeDeprecated");
                        var values = !includeDeprecated
                            ? type.Values.Where(e => string.IsNullOrWhiteSpace(e.DeprecationReason)).ToList()
                            : type.Values.ToList();

                        return await values.WhereAsync(v => context.Schema.Filter.AllowEnumValue(type, v));
                    }

                    return null;
                });
            FieldAsync<ListGraphType<NonNullGraphType<__InputValue>>>("inputFields", resolve: async context =>
            {
                var type = context.Source as IInputObjectGraphType;
                if (type == null) return null;
                return await type.Fields.WhereAsync(f => context.Schema.Filter.AllowField(type, f));
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

        private TypeKind KindForInstance(IGraphType type)
        {
            switch (type)
            {
                case EnumerationGraphType _:
                    return TypeKind.ENUM;
                case ScalarGraphType _:
                    return TypeKind.SCALAR;
                case IObjectGraphType _:
                    return TypeKind.OBJECT;
                case IInterfaceGraphType _:
                    return TypeKind.INTERFACE;
                case UnionGraphType _:
                    return TypeKind.UNION;
                case IInputObjectGraphType _:
                    return TypeKind.INPUT_OBJECT;
                case ListGraphType _:
                    return TypeKind.LIST;
                case NonNullGraphType _:
                    return TypeKind.NON_NULL;
                default:
                    throw new ExecutionError("Unknown kind of type: {0}".ToFormat(type));
            }
        }
    }
}
