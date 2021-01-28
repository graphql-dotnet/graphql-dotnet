using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__InputValue</c> introspection type represents field and directive arguments as well as the inputFields of an input object.
    /// </summary>
    public class __InputValue : ObjectGraphType<IProvideMetadata> // context.Source either QueryArgument or FieldType
    {
        /// <summary>
        /// Initializes a new instance of the <c>__InputValue</c> introspection type.
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

            Field<NonNullGraphType<__Type>>("type", resolve: context => ((IProvideResolvedType)context.Source).ResolvedType);

            Field<StringGraphType>(
                "defaultValue",
                "A GraphQL-formatted string representing the default value for this input value.",
                resolve: context =>
                {
                    var hasDefault = context.Source as IHaveDefaultValue;
                    if (hasDefault?.DefaultValue == null)
                        return null;

                    var ast = hasDefault.DefaultValue.AstFromValue(context.Schema, hasDefault.ResolvedType);
                    var result = AstPrinter.Print(ast);
                    return string.IsNullOrWhiteSpace(result) ? null : result;
                });

            if (allowAppliedDirectives)
                this.AddAppliedDirectivesField("input value");
        }
    }
}
