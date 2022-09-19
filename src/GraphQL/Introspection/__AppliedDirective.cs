using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <see cref="__AppliedDirective"/> introspection type represents
    /// a directive applied to a schema element - type, field, argument, etc.
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

            Field<NonNullGraphType<StringGraphType>>("name")
                .Description("Directive name")
                .Resolve(context => context.Source!.Name);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__DirectiveArgument>>>>("args")
                .Description("Values of explicitly specified directive arguments")
                .Resolve(context => context.Source!.List ?? Enumerable.Empty<DirectiveArgument>());
        }
    }
}
