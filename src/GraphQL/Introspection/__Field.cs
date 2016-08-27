using System.Linq;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __Field : ObjectGraphType
    {
        public __Field()
        {
            Name = "__Field";
            Description =
                "Object and Interface types are described by a list of Fields, each of " +
                "which has a name, potentially a list of arguments, and a return type.";
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args", null, null,
                context =>
                {
                    var fieldType = (FieldType) context.Source;
                    return fieldType.Arguments ?? Enumerable.Empty<QueryArgument>();
                });
            Field<NonNullGraphType<__Type>>("type");
            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated", null, null, context =>
            {
                var fieldType = (FieldType) context.Source;
                return !string.IsNullOrWhiteSpace(fieldType.DeprecationReason);
            });
            Field<StringGraphType>("deprecationReason");
        }
    }
}
