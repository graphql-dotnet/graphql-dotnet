using GraphQL.Types;
using System;
using System.Linq;

namespace GraphQL.Introspection
{
    public class __Directive : ObjectGraphType<DirectiveGraphType>
    {
        public __Directive()
        {
            Name = nameof(__Directive);
            Description =
                "A Directive provides a way to describe alternate runtime execution and " +
                "type validation behavior in a GraphQL document." +
                $"{Environment.NewLine}{Environment.NewLine}In some cases, you need to provide options to alter GraphQL's " +
                "execution behavior in ways field arguments will not suffice, such as " +
                "conditionally including or skipping a field. Directives provide this by " +
                "describing additional information to the executor.";

            Field(f => f.Name);
            Field(f => f.Description, nullable: true);

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__DirectiveLocation>>>>("locations");
            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__InputValue>>>>("args",
                resolve: context =>
                    context.Source.Arguments ?? Enumerable.Empty<QueryArgument>()
            );
            Field<NonNullGraphType<BooleanGraphType>>("onOperation", deprecationReason: "Use 'locations'.",
                resolve: context => context
                    .Source.Locations.Any(l =>
                        l == DirectiveLocation.Query ||
                        l == DirectiveLocation.Mutation ||
                        l == DirectiveLocation.Subscription));
            Field<NonNullGraphType<BooleanGraphType>>("onFragment", deprecationReason: "Use 'locations'.",
                resolve: context => context
                    .Source.Locations.Any(l =>
                        l == DirectiveLocation.FragmentSpread ||
                        l == DirectiveLocation.InlineFragment ||
                        l == DirectiveLocation.FragmentDefinition));

            Field<NonNullGraphType<BooleanGraphType>>("onField", deprecationReason: "Use 'locations'.",
                resolve: context =>
                    context.Source.Locations.Any(l => l == DirectiveLocation.Field));
        }
    }

    public class __DirectiveLocation : EnumerationGraphType<DirectiveLocation>
    {
        public __DirectiveLocation()
        {
            Name = nameof(__DirectiveLocation);
            Description =
                "A Directive can be adjacent to many parts of the GraphQL language, a " +
                "__DirectiveLocation describes one such possible adjacencies.";
        }
    }
}
