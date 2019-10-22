using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Introspection
{
    public class __DirectiveArgument : ObjectGraphType<ParamValue>
    {
        public __DirectiveArgument()
        {
            Description =
                "Value of an argument provided to Directive";

            Field<NonNullGraphType<StringGraphType>>(
                "name",
                "Argument name",
                resolve: context => context.Source.Name);
            Field<StringGraphType>(
                "value",
                "A GraphQL-formatted string representing the value for argument.",
                resolve: context =>
                {
                    var parameter = context.Source;
                    if (parameter.Value == null) return null;

                    var ast = parameter.Value.AstFromValue(context.Schema, parameter.ResolvedType);
                    var result = AstPrinter.Print(ast);
                    return string.IsNullOrWhiteSpace(result) ? null : result;
                });
        }
    }
}
