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
            Field<StringGraphType>("name", resolve: context => ((IGraphType)context.Source).Name);
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

                        fields ??= Enumerable.Empty<FieldType>();
                        fields = await fields.WhereAsync(f => context.Schema.Filter.AllowField(context.Source as IGraphType, f)).ConfigureAwait(false);

                        return fields.OrderBy(f => f.Name);
                    }
                    return null;
                });
            FieldAsync<ListGraphType<NonNullGraphType<__Type>>>("interfaces", resolve: async context =>
            {
                return context.Source is IImplementInterfaces type
                    ? await type.ResolvedInterfaces.WhereAsync(x => context.Schema.Filter.AllowType(x)).ConfigureAwait(false)
                    : null;
            });
            FieldAsync<ListGraphType<NonNullGraphType<__Type>>>("possibleTypes", resolve: async context =>
            {
                if (context.Source is IAbstractGraphType type)
                {
                    return await type.PossibleTypes.WhereAsync(x => context.Schema.Filter.AllowType(x)).ConfigureAwait(false);
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

                        return await values.WhereAsync(v => context.Schema.Filter.AllowEnumValue(type, v)).ConfigureAwait(false);
                    }

                    return null;
                });
            FieldAsync<ListGraphType<NonNullGraphType<__InputValue>>>("inputFields", resolve: async context =>
            {
                return context.Source is IInputObjectGraphType type
                    ? await type.Fields.WhereAsync(f => context.Schema.Filter.AllowField(type, f)).ConfigureAwait(false)
                    : null;
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

        private static object KindForInstance(IGraphType type) => type switch
        {
            EnumerationGraphType _ => TypeKindBoxed.ENUM,
            ScalarGraphType _ => TypeKindBoxed.SCALAR,
            IObjectGraphType _ => TypeKindBoxed.OBJECT,
            IInterfaceGraphType _ => TypeKindBoxed.INTERFACE,
            UnionGraphType _ => TypeKindBoxed.UNION,
            IInputObjectGraphType _ => TypeKindBoxed.INPUT_OBJECT,
            ListGraphType _ => TypeKindBoxed.LIST,
            NonNullGraphType _ => TypeKindBoxed.NON_NULL,
            _ => throw new ExecutionError("Unknown kind of type: {0}".ToFormat(type))
        };
    }
}
