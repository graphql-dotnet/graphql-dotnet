using GraphQL.Language.AST;
using GraphQL.Types;
using GraphQL.Utilities;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__DirectiveArgument</c> introspection type represents an argument of a directive applied to a schema element - type, field, argument, etc.
    /// </summary>
    public class __DirectiveArgument : ObjectGraphType<DirectiveArgument>
    {
        /// <summary>
        /// Initializes a new instance of this graph type
        /// </summary>
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
                    var argument = context.Source;
                    if (argument.Value == null) return null;

                    var ast = argument.ResolvedType.ToAST(argument.Value);
                    if (ast is StringValue value) //TODO: ???
                    {
                        return value.Value;
                    }
                    else
                    {
                        string result = AstPrinter.Print(ast);
                        return string.IsNullOrWhiteSpace(result) ? null : result;
                    }
                });
        }
    }
}
