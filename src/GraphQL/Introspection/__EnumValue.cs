using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class __EnumValue : ObjectGraphType<EnumValueDefinition>
    {
        public __EnumValue()
        {
            Name = nameof(__EnumValue);
            Description =
                "One possible value for a given Enum. Enum values are unique values, not " +
                "a placeholder for a string or numeric value. However an Enum value is " +
                "returned in a JSON response as a string.";

            Field(f => f.Name);
            Field(f => f.Description, nullable: true);

            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated", resolve: context => (!string.IsNullOrWhiteSpace(context.Source?.DeprecationReason)).Boxed());
            Field(f => f.DeprecationReason, nullable: true);
        }
    }
}
