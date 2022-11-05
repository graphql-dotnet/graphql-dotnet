using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <see cref="__InputValue"/> introspection type represents field and directive arguments as well as the inputFields of an input object.
    /// </summary>
    public class __InputValue : ObjectGraphType<IProvideMetadata> // context.Source either QueryArgument or FieldType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="__InputValue"/> introspection type.
        /// </summary>
        /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
        public __InputValue(bool allowAppliedDirectives = false)
        {
            Name = nameof(__InputValue);

            Description =
                "Arguments provided to Fields or Directives and the input fields of an " +
                "InputObject are represented as Input Values which describe their type " +
                "and optionally a default value.";

            Field<NonNullGraphType<StringGraphType>>("name");

            Field<StringGraphType>("description");

            Field<NonNullGraphType<__Type>>("type").Resolve(context => ((IProvideResolvedType)context.Source!).ResolvedType);

            Field<StringGraphType>("defaultValue")
                .Description("A GraphQL-formatted string representing the default value for this input value.")
                .Resolve(context =>
                {
                    return context.Source is IHaveDefaultValue hasDefault && hasDefault.DefaultValue != null
                        ? hasDefault.ResolvedType!.Print(hasDefault.DefaultValue)
                        : null;
                });

            if (allowAppliedDirectives)
                this.AddAppliedDirectivesField("input value");
        }
    }
}
