using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__AppliedDirective</c> introspection type represents a directive applied to a schema element - type, field, argument, etc.
    /// </summary>
    public class __AppliedDirective : ObjectGraphType<AppliedDirective>
    {
        /// <summary>
        /// Initializes a new instance of this graph type.
        /// </summary>
        public __AppliedDirective()
        {
            Description =
                "Directive applied to a schema element";

            Field<NonNullGraphType<StringGraphType>>(
                "name",
                "Directive name",
                resolve: context => context.Source!.Name);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__DirectiveArgument>>>>(
                "args",
                "Values of explicitly specified directive arguments",
                resolve: context => context.Source!.List ?? Enumerable.Empty<DirectiveArgument>());
        }
    }
}
