using System;
using System.Linq;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __Type : ObjectGraphType
    {
        public __Type()
        {
            Name = "__Type";
            Field<NonNullGraphType<__TypeKind>>("kind", null, null, context =>
            {
                if (context.Source is ScalarGraphType)
                {
                    return TypeKind.SCALAR;
                } else if (context.Source is ObjectGraphType)
                {
                    return TypeKind.OBJECT;
                } else if (context.Source is InterfaceGraphType)
                {
                    return TypeKind.INTERFACE;
                } else if (context.Source is UnionGraphType)
                {
                    return TypeKind.UNION;
                } else if (context.Source is EnumerationGraphType)
                {
                    return TypeKind.ENUM;
                } else if (context.Source is InputObjectGraphType)
                {
                    return TypeKind.INPUT_OBJECT;
                } else if (context.Source is ListGraphType)
                {
                    return TypeKind.LIST;
                } else if (context.Source is NonNullGraphType)
                {
                    return TypeKind.NON_NULL;
                }

                throw new ExecutionError("Unkown kind of type: {0}".ToFormat(context.Source));
            });
            Field<StringGraphType>("name");
            Field<StringGraphType>("description");
            Field<ListGraphType<NonNullGraphType<__Field>>>("fields", null,
                new QueryArguments(new[]
                {
                    new QueryArgument<BooleanGraphType>
                    {
                        Name = "includeDeprecated",
                        DefaultValue = false
                    }
                }),
                context =>
                {
                    if (context.Source is ObjectGraphType || context.Source is InterfaceGraphType)
                    {
                        var includeDeprecated = (bool)context.Arguments["includeDeprecated"];
                        var type = context.Source as GraphType;
                        return !includeDeprecated
                            ? type.Fields.Where(f => string.IsNullOrWhiteSpace(f.DeprecationReason))
                            : type.Fields;
                    }

                    return Enumerable.Empty<FieldType>();
                });
            Field<ListGraphType<NonNullGraphType<__Type>>>("interfaces", null, null, context =>
            {
                var type = context.Source as IImplementInterfaces;
                return type != null ? type.Interfaces : Enumerable.Empty<Type>();
            });
            Field<ListGraphType<NonNullGraphType<__Type>>>("possibleTypes", null, null, context =>
            {
                if (context.Source is InterfaceGraphType || context.Source is UnionGraphType)
                {
                    var type = (GraphType)context.Source;
                    return context.Schema.FindImplemenationsOf(type.GetType());
                }
                return Enumerable.Empty<GraphType>();
            });
            Field<ListGraphType<NonNullGraphType<__EnumValue>>>("enumValues", null,
                new QueryArguments(new[]
                {
                    new QueryArgument<BooleanGraphType>
                    {
                        Name = "includeDeprecated",
                        DefaultValue = false
                    }
                }),
                context =>
                {
                    var type = context.Source as EnumerationGraphType;
                    if (type != null)
                    {
                        var includeDeprecated = (bool)context.Arguments["includeDeprecated"];
                        return !includeDeprecated
                            ? type.Values.Where(e => !string.IsNullOrWhiteSpace(e.DeprecationReason))
                            : type.Values;
                    }

                    return Enumerable.Empty<EnumValue>();
                });
            Field<ListGraphType<NonNullGraphType<__InputValue>>>("inputFields", null, null, context =>
            {
                var type = context.Source as InputObjectGraphType;
                return type != null ? type.Fields : Enumerable.Empty<FieldType>();
            });
            Field<__Type>("ofType");
        }
    }
}
