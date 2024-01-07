using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <see cref="__EnumValue"/> introspection type represents one of possible values of an enum.
    /// </summary>
    public class __EnumValue : ObjectGraphType<EnumValueDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="__EnumValue"/> introspection type.
        /// </summary>
        /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
        public __EnumValue(bool allowAppliedDirectives = false)
        {
            SetName(nameof(__EnumValue), validate: false);
            Description =
                "One possible value for a given Enum. Enum values are unique values, not " +
                "a placeholder for a string or numeric value. However an Enum value is " +
                "returned in a JSON response as a string.";

            Field<NonNullGraphType<StringGraphType>>("name").Resolve(context => context.Source!.Name);
            Field<StringGraphType>("description").Resolve(context => context.Source!.Description);

            Field<NonNullGraphType<BooleanGraphType>>("isDeprecated").Resolve(context => (!string.IsNullOrWhiteSpace(context.Source?.DeprecationReason)).Boxed());
            Field<StringGraphType>("deprecationReason").Resolve(context => context.Source!.DeprecationReason);

            if (allowAppliedDirectives)
                this.AddAppliedDirectivesField("enum value");
        }
    }
}
