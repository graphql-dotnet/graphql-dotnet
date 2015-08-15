using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __Field : ObjectGraphType
    {
        public __Field()
        {
            Name = "__Field";
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<NonNullGraphType<ListGraphType<__InputValue>>>("args", null, null,
                context =>
                {
                    var fieldType = (FieldType) context.Source;
                    // TODO: probably need to format these
                    return fieldType.Arguments;
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
