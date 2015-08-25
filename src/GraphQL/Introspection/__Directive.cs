using System.Linq;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __Directive : ObjectGraphType
    {
        public __Directive()
        {
            Name = "__Directive";
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args", resolve: context =>
            {
                var fieldType = (FieldType) context.Source;
                return fieldType.Arguments ?? Enumerable.Empty<QueryArgument>();
            });
            Field<NonNullGraphType<BooleanGraphType>>("onOperation");
            Field<NonNullGraphType<BooleanGraphType>>("onFragment");
            Field<NonNullGraphType<BooleanGraphType>>("onField");
        }
    }
}
