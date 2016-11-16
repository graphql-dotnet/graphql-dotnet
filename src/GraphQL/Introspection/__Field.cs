using System.Linq;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __Field : ObjectGraphType<IFieldType>
    {
        public __Field()
        {
            Name = "__Field";
            Description =
                "Object and Interface types are described by a list of Fields, each of " +
                "which has a name, potentially a list of arguments, and a return type.";

            Field(f => f.Name);
            Field(f => f.Description, nullable: true);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args", null, null,
                context =>
                {
                    return context.Source.Arguments ?? Enumerable.Empty<QueryArgument>();
                });
            Field<NonNullGraphType<__Type>>("type", resolve: ctx => ctx.Source.ResolvedType);
            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated", null, null, context =>
            {
                return !string.IsNullOrWhiteSpace(context.Source.DeprecationReason);
            });
            Field(f => f.DeprecationReason, nullable: true);
        }
    }
}
