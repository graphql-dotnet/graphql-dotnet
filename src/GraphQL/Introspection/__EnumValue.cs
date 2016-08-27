using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __EnumValue : ObjectGraphType
    {
        public __EnumValue()
        {
            Name = "__EnumValue";
            Description =
                "One possible value for a given Enum. Enum values are unique values, not " +
                "a placeholder for a string or numeric value. However an Enum value is " +
                "returned in a JSON response as a string.";
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<NonNullGraphType<StringGraphType>>("isDeprecated", resolve: context =>
            {
                var enumValue = context.Source as EnumValueDefinition;
                return !string.IsNullOrWhiteSpace(enumValue?.DeprecationReason);
            });
            Field<StringGraphType>("deprecationReason");
        }
    }
}
