using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__EnumValue</c> introspection type represents one of possible values of an enum.
    /// </summary>
    public class __EnumValue : ObjectGraphType<EnumValueDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <c>__EnumValue</c> introspection type.
        /// </summary>
        public __EnumValue()
        {
            Name = nameof(__EnumValue);
            Description =
                "One possible value for a given Enum. Enum values are unique values, not " +
                "a placeholder for a string or numeric value. However an Enum value is " +
                "returned in a JSON response as a string.";

            Field(f => f.Name).Description(null);
            Field(f => f.Description, nullable: true).Description(null);

            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated", resolve: context => (!string.IsNullOrWhiteSpace(context.Source?.DeprecationReason)).Boxed());
            Field(f => f.DeprecationReason, nullable: true).Description(null);

            this.AddAppliedDirectivesField("enum value");
        }
    }
}
