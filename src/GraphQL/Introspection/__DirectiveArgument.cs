using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Introspection
{
    public class __DirectiveArgument : ObjectGraphType<DirectiveArgumentValue>
    {
        public __DirectiveArgument()
        {
            Description =
                "Value of an argument provided to directive";

            Field<NonNullGraphType<StringGraphType>>(
                "name",
                "Argument name",
                resolve: context => context.Source.Name);

            Field<StringGraphType>(
                "value",
                "A GraphQL-formatted string representing the value for argument.",
                resolve: context =>
                {
                    var argumentValue = context.Source;
                    if (argumentValue.Value == null) return null;

                    var ast = argumentValue.Value.AstFromValue(context.Schema, argumentValue.ResolvedType);
                    var result = AstPrinter.Print(ast);
                    return string.IsNullOrWhiteSpace(result) ? null : result;
                });
        }
    }
}
