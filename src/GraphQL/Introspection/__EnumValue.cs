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
        /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
        public __EnumValue(bool allowAppliedDirectives = false)
        {
            Name = nameof(__EnumValue);
            Description =
                "One possible value for a given Enum. Enum values are unique values, not " +
                "a placeholder for a string or numeric value. However an Enum value is " +
                "returned in a JSON response as a string.";

            Field<NonNullGraphType<StringGraphType>>("name", resolve: context => context.Source!.Name);
            Field<StringGraphType>("description", resolve: context => context.Source!.Description);

            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated", resolve: context => (!string.IsNullOrWhiteSpace(context.Source?.DeprecationReason)).Boxed());
            Field<StringGraphType>("deprecationReason", resolve: context => context.Source!.DeprecationReason);

            if (allowAppliedDirectives)
                this.AddAppliedDirectivesField("enum value");
        }
    }
}
