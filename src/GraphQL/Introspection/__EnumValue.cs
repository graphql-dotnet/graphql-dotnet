using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __EnumValue : ObjectGraphType
    {
        public __EnumValue()
        {
            Name = "__EnumValue";
            Field("name", NonNullGraphType.String);
            Field("description", ScalarGraphType.String);
            Field("isDeprecated", NonNullGraphType.Boolean, null, context =>
            {
                var enumValue = context.Source as EnumValue;
                return enumValue != null && !string.IsNullOrWhiteSpace(enumValue.DeprecationReason);
            });
            Field("deprecationReason", ScalarGraphType.String);
        }
    }
}