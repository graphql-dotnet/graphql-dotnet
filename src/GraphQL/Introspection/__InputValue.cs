using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Introspection
{
    public class __InputValue : ObjectGraphType<IHaveDefaultValue>
    {
        public __InputValue()
        {
            Name = "__InputValue";
            Description =
                "Arguments provided to Fields or Directives and the input fields of an " +
                "InputObject are represented as Input Values which describe their type " +
                "and optionally a default value.";
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<NonNullGraphType<__Type>>("type", resolve: ctx => ctx.Schema.FindType(ctx.Source.Type));
            Field<StringGraphType>(
                "defaultValue",
                "A GraphQL-formatted string representing the default value for this input value.",
                resolve: context =>
                {
                    var hasDefault = context.Source;
                    if (context.Source == null) return null;

                    var ast = hasDefault.DefaultValue.AstFromValue(context.Schema, context.Schema.FindType(hasDefault.Type));
                    var result = AstPrinter.Print(ast);
                    return string.IsNullOrWhiteSpace(result) ? null : result;
                });
        }
    }
}
