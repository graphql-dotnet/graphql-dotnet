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
                        return !includeDeprecated
                            ? type.Fields.Where(f => string.IsNullOrWhiteSpace(f.DeprecationReason))
                            : type.Fields;
                    }

                    return null;
                });
            Field("interfaces", new ListGraphType<NonNullGraphType<__Type>>(), null, context =>
            {
                var type = context.Source as IImplementInterfaces;
                return type != null ? type.Interfaces : null;
            });
            Field("possibleTypes", new ListGraphType<NonNullGraphType<__Type>>(), null, context =>
            {
                var type = context.Source as IProvidePossibleTypes;
                return type != null ? type.PossibleTypes() : null;
            });
            Field("enumValues", new ListGraphType<NonNullGraphType<__EnumValue>>(),
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
                    var type = context.Source as EnumerationGraphType;
                    if (type != null)
                    {
                        var includeDeprecated = (bool)context.Arguments["includeDeprecated"];
                        return !includeDeprecated
                            ? type.Values.Where(e => !string.IsNullOrWhiteSpace(e.DeprecationReason))
                            : type.Values;
                    }

                    return null;
                });
            Field("inputFields", new ListGraphType<NonNullGraphType<__InputValue>>(), null, context =>
            {
                var type = context.Source as InputObjectGraphType;
                return type != null ? type.Fields : null;
            });
            //Field("ofType", new __Type());
        }
    }
}
