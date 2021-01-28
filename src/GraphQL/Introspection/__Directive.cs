using System.Linq;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__Directive</c> introspection type represents a directive that a server supports.
    /// </summary>
    public class __Directive : ObjectGraphType<DirectiveGraphType>
    {
        /// <summary>
        /// Initializes a new instance of the <c>__Directive</c> introspection type.
        /// </summary>
        /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
        public __Directive(bool allowAppliedDirectives = false)
        {
            Name = nameof(__Directive);
            Description =
                "A Directive provides a way to describe alternate runtime execution and " +
                "type validation behavior in a GraphQL document." +
               @"

" +
                "In some cases, you need to provide options to alter GraphQL's " +
                "execution behavior in ways field arguments will not suffice, such as " +
                "conditionally including or skipping a field. Directives provide this by " +
                "describing additional information to the executor.";

            Field<NonNullGraphType<StringGraphType>>("name", resolve: context => context.Source.Name);
            Field<StringGraphType>("description", resolve: context => context.Source.Description);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__DirectiveLocation>>>>("locations");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args",
                resolve: context => context.Source.Arguments?.List ?? Enumerable.Empty<QueryArgument>()
            );
            Field<NonNullGraphType<BooleanGraphType>>("onOperation", deprecationReason: "Use 'locations'.",
                resolve: context => context.Source.Locations.Any(l =>
                        l == DirectiveLocation.Query ||
                        l == DirectiveLocation.Mutation ||
                        l == DirectiveLocation.Subscription));
            Field<NonNullGraphType<BooleanGraphType>>("onFragment", deprecationReason: "Use 'locations'.",
                resolve: context => context.Source.Locations.Any(l =>
                        l == DirectiveLocation.FragmentSpread ||
                        l == DirectiveLocation.InlineFragment ||
                        l == DirectiveLocation.FragmentDefinition));

            Field<NonNullGraphType<BooleanGraphType>>("onField", deprecationReason: "Use 'locations'.",
                resolve: context => context.Source.Locations.Any(l => l == DirectiveLocation.Field));

            if (allowAppliedDirectives)
                this.AddAppliedDirectivesField("directive");
        }
    }

    /// <summary>
    /// An enumeration representing a location that a directive may be placed.
    /// </summary>
    public class __DirectiveLocation : EnumerationGraphType<DirectiveLocation>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="__DirectiveLocation"/> graph type.
        /// </summary>
        public __DirectiveLocation()
        {
            SetName(nameof(__DirectiveLocation), validate: false);
            Description =
                "A Directive can be adjacent to many parts of the GraphQL language, a " +
                "__DirectiveLocation describes one such possible adjacencies.";
        }
    }
}
