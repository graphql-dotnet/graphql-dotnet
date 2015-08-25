using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __EnumValue : ObjectGraphType
    {
        public __EnumValue()
        {
            Name = "__EnumValue";
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<NonNullGraphType<StringGraphType>>("isDeprecated", null, null, context =>
            {
                var enumValue = context.Source as EnumValue;
                return enumValue != null && !string.IsNullOrWhiteSpace(enumValue.DeprecationReason);
            });
            Field<StringGraphType>("deprecationReason");
        }
    }
}
