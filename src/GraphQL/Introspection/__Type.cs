using System.Linq;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __Type : ObjectGraphType
    {
        public __Type()
        {
            Name = "__Type";
            Field("kind", new NonNullGraphType(new __TypeKind()), null, context =>
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
            Field("name", ScalarGraphType.String);
            Field("description", ScalarGraphType.String);
            Field("fields", new ListGraphType<NonNullGraphType<__Field>>(),
                new QueryArguments(new[]
                {
                    new QueryArgument
                    {
                        Name = "includeDeprecated",
                        Type = ScalarGraphType.Boolean,
                        DefaultValue = false
                    }
                }),
                context =>
                {
                    if (context.Source is ObjectGraphType || context.Source is InterfaceGraphType)
                    {
                        var includeDeprecated = (bool)context.Arguments["includeDeprecated"];
                        var type = context.Source as GraphType;
                        if (!includeDeprecated)
                        {
                            return type.Fields.Where(f => string.IsNullOrWhiteSpace(f.DeprecationReason));
                        }
                        else
                        {
                            return type.Fields;
                        }
                    }

                    return null;
                });
            Field("interfaces", new ListGraphType<NonNullGraphType<__Type>>(), null, context =>
            {
                if (context.Source is ObjectGraphType)
                {
                    return ((ObjectGraphType) context.Source).Interfaces;
                }

                return null;
            });
        }
    }
}