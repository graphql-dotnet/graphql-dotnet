using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__DirectiveArgument</c> introspection type represents an argument of a directive applied to a
    /// schema element - type, field, argument, etc.
    /// <br/><br/>
    /// Note that this class describes only explicitly specified arguments. If the argument in the directive
    /// definition has default value and this argument was not specified when applying the directive to schema
    /// element, then such an argument with default value will not be returned.
    /// </summary>
    public class __DirectiveArgument : ObjectGraphType<DirectiveArgument>
    {
        /// <summary>
        /// Initializes a new instance of this graph type.
        /// </summary>
        public __DirectiveArgument()
        {
            Description =
                "Value of an argument provided to directive";

            Field<NonNullGraphType<StringGraphType>>(
                "name",
                "Argument name",
                resolve: context => context.Source!.Name);

            Field<NonNullGraphType<StringGraphType>>(
                "value",
                "A GraphQL-formatted string representing the value for argument.",
                resolve: context =>
                {
                    var argument = context.Source!;
                    if (argument.Value == null)
                        return "null";

                    var grandParent = context.Parent!.Parent!;
                    int index = (int)grandParent.Path.Last();
                    var appliedDirective = ((IList<AppliedDirective>)grandParent.Source!)[index];
                    var directiveDefinition = context.Schema.Directives.Find(appliedDirective.Name);
                    var argumentDefinition = directiveDefinition!.Arguments!.Find(argument.Name);

                    return argumentDefinition!.ResolvedType!.Print(argument.Value);
                });
        }
    }
}
