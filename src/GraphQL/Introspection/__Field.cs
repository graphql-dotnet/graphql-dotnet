using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __Field : ObjectGraphType
    {
        public __Field()
        {
            Name = "__Field";
            Field("name", NonNullGraphType.String);
            Field("description", ScalarGraphType.String);
            Field("args", new NonNullListGraphType<NonNullGraphType<__InputValue>>(), null,
                context =>
                {
                    var fieldType = (FieldType) context.Source;
                    // TODO: probably need to format these
                    return fieldType.Arguments;
                });
            Field("type", new NonNullGraphType(new __Type()));
            Field("isDeprecated", NonNullGraphType.Boolean, null, context =>
            {
                var fieldType = (FieldType) context.Source;
                return !string.IsNullOrWhiteSpace(fieldType.DeprecationReason);
            });
            Field("deprecationReason", ScalarGraphType.String);
        }
    }
}