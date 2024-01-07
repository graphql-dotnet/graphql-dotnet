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
            : this(allowAppliedDirectives, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="__InputValue"/> introspection type.
        /// </summary>
        /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
        /// <param name="deprecationOfInputValues">
        /// Allows deprecation of input values - arguments on a field or input fields on an input type.
        /// This feature is from a working draft of the specification.
        /// </param>
        public __InputValue(bool allowAppliedDirectives = false, bool deprecationOfInputValues = false)
        {
            SetName(nameof(__InputValue), validate: false);

            Description =
                "Arguments provided to Fields or Directives and the input fields of an " +
                "InputObject are represented as Input Values which describe their type " +
                "and optionally a default value.";

            Field<NonNullGraphType<StringGraphType>>("name")
                .Resolve(context => context.Source is QueryArgument arg ? arg.Name : ((FieldType)context.Source).Name); // avoid implicit use of FieldNameResolver here to make the code compatible with AOT compilation

            Field<StringGraphType>("description")
                .Resolve(context => context.Source is QueryArgument arg ? arg.Description : ((FieldType)context.Source).Description);

            Field<NonNullGraphType<__Type>>("type").Resolve(context => ((IProvideResolvedType)context.Source!).ResolvedType);

            Field<StringGraphType>("defaultValue")
                .Description("A GraphQL-formatted string representing the default value for this input value.")
                .Resolve(context =>
                {
                    return context.Source is IHaveDefaultValue hasDefault && hasDefault.DefaultValue != null
                        ? hasDefault.ResolvedType!.Print(hasDefault.DefaultValue)
                        : null;
                });

            if (deprecationOfInputValues)
            {
                // context.Source either QueryArgument or FieldType
                Field<NonNullGraphType<BooleanGraphType>>("isDeprecated").Resolve(context => (!string.IsNullOrWhiteSpace(((IProvideDeprecationReason)context.Source).DeprecationReason)).Boxed());
                Field<StringGraphType>("deprecationReason").Resolve(context => ((IProvideDeprecationReason)context.Source).DeprecationReason);
            }

            if (allowAppliedDirectives)
                this.AddAppliedDirectivesField("input value");
        }
    }
}
