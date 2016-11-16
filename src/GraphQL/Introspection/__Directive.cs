using System;
using GraphQL.Types;
using System.Linq;

namespace GraphQL.Introspection
{
    public class __Directive : ObjectGraphType<DirectiveGraphType>
    {
        public __Directive()
        {
            Name = "__Directive";
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

    public class __DirectiveLocation : EnumerationGraphType
    {
        public __DirectiveLocation()
        {
            Name = "__DirectiveLocation";
            Description =
                "A Directive can be adjacent to many parts of the GraphQL language, a " +
                "__DirectiveLocation describes one such possible adjacencies.";

            AddValue("QUERY", "Location adjacent to a query operation.", DirectiveLocation.Query);
            AddValue("MUTATION", "Location adjacent to a mutation operation.", DirectiveLocation.Mutation);
            AddValue("SUBSCRIPTION", "Location adjacent to a subscription operation.", DirectiveLocation.Subscription);
            AddValue("FIELD", "Location ajdacent to a field.", DirectiveLocation.Field);
            AddValue("FRAGMENT_DEFINITION", "Location adjacent to a fragment definition.", DirectiveLocation.FragmentDefinition);
            AddValue("FRAGMENT_SPREAD", "Location adjacent to a fragment spread.", DirectiveLocation.FragmentSpread);
            AddValue("INLINE_FRAGMENT", "Location adjacent to an inline fragment.", DirectiveLocation.InlineFragment);
            AddValue("SCHEMA", "Location adjacent to a schema definition.", DirectiveLocation.Schema);
            AddValue("SCALAR", "Location adjacent to a scalar definition.", DirectiveLocation.Scalar);
            AddValue("OBJECT", "Location adjacent to an object type definition.", DirectiveLocation.Object);
            AddValue("FIELD_DEFINITION", "Location adjacent to a field definition.", DirectiveLocation.FieldDefinition);
            AddValue("ARGUMENT_DEFINITION", "Location adjacent to an argument defintion.", DirectiveLocation.ArgumentDefinition);
            AddValue("INTERFACE", "Location adjacent to an interface definition.", DirectiveLocation.Interface);
            AddValue("UNION", "Location adjacent to a union definition.", DirectiveLocation.Union);
            AddValue("ENUM", "Location adjacent to an enum definition", DirectiveLocation.Enum);
            AddValue("ENUM_VALUE", "Location adjacent to an enum value definition", DirectiveLocation.EnumValue);
            AddValue("INPUT_OBJECT", "Location adjacent to an input object type defintion.", DirectiveLocation.InputObject);
            AddValue("INPUT_FIELD_DEFINITION", "Location adjacent to an input object field definition.", DirectiveLocation.InputFieldDefinition);
        }
    }
}
