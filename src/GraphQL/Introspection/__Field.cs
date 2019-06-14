using GraphQL.Types;
using System.Linq;

namespace GraphQL.Introspection
{
    public class __Field : ObjectGraphType<IFieldType>
    {
        public __Field()
        {
            Name = nameof(__Field);
            Description =
                "Object and Interface types are described by a list of Fields, each of " +
                "which has a name, potentially a list of arguments, and a return type.";

            Field(f => f.Name);
            Field(f => f.Description, nullable: true);

            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args",
                resolve: async context =>
                {
                    var arguments = context.Source.Arguments ?? Enumerable.Empty<QueryArgument>();
                    return await arguments.WhereAsync(x => context.Schema.Filter.AllowArgument(context.Source, x));
                });
            Field<NonNullGraphType<__Type>>("type", resolve: ctx => ctx.Source.ResolvedType);
            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated", resolve: context =>
            {
                return !string.IsNullOrWhiteSpace(context.Source.DeprecationReason);
            });
            Field(f => f.DeprecationReason, nullable: true);
        }
    }
}
