using System.Collections.Generic;
using System.ComponentModel;

namespace GraphQL.Types
{
    public enum DirectiveLocation
    {
        // Operations
        [Description("Location adjacent to a query operation.")]
        Query,
        [Description("Location adjacent to a mutation operation.")]
        Mutation,
        [Description("Location adjacent to a subscription operation.")]
        Subscription,
        [Description("Location adjacent to a field.")]
        Field,
        [Description("Location adjacent to a fragment definition.")]
        FragmentDefinition,
        [Description("Location adjacent to a fragment spread.")]
        FragmentSpread,
        [Description("Location adjacent to an inline fragment.")]
        InlineFragment,
        // Schema Definitions
        [Description("Location adjacent to a schema definition.")]
        Schema,
        [Description("Location adjacent to a scalar definition.")]
        Scalar,
        [Description("Location adjacent to an object type definition.")]
        Object,
        [Description("Location adjacent to a field definition.")]
        FieldDefinition,
        [Description("Location adjacent to an argument definition.")]
        ArgumentDefinition,
        [Description("Location adjacent to an interface definition.")]
        Interface,
        [Description("Location adjacent to a union definition.")]
        Union,
        [Description("Location adjacent to an enum definition")]
        Enum,
        [Description("Location adjacent to an enum value definition")]
        EnumValue,
        [Description("Location adjacent to an input object type definition.")]
        InputObject,
        [Description("Location adjacent to an input object field definition.")]
        InputFieldDefinition
    }

    /// <summary>
    /// Directives are used by the GraphQL runtime as a way of modifying execution
    /// behavior.Type system creators will usually not create these directly.
    /// </summary>
    public class DirectiveGraphType : INamedType
    {
        public static IncludeDirective Include = new IncludeDirective();
        public static SkipDirective Skip = new SkipDirective();
        public static GraphQLDeprecatedDirective Deprecated = new GraphQLDeprecatedDirective();

        private readonly List<DirectiveLocation> _directiveLocations = new List<DirectiveLocation>();

        public DirectiveGraphType(string name, IEnumerable<DirectiveLocation> locations)
        {
            Name = name;
            _directiveLocations.AddRange(locations);
        }

        public string Name { get; set; }
        public string Description { get; set; }

        public QueryArguments Arguments { get; set; }

        public IEnumerable<DirectiveLocation> Locations => _directiveLocations;
    }

    /// <summary>
    /// Used to conditionally include fields or fragments.
    /// </summary>
    public class IncludeDirective : DirectiveGraphType
    {
        public IncludeDirective()
            : base("include", new[]
            {
                DirectiveLocation.Field,
                DirectiveLocation.FragmentSpread,
                DirectiveLocation.InlineFragment
            })
        {
            Description = "Directs the executor to include this field or fragment only when the 'if' argument is true.";
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<BooleanGraphType>>
            {
                Name = "if",
                Description = "Included when true."
            });
        }
    }

    /// <summary>
    /// Used to conditionally skip (exclude) fields or fragments.
    /// </summary>
    public class SkipDirective : DirectiveGraphType
    {
        public SkipDirective()
            : base("skip", new[]
            {
                DirectiveLocation.Field,
                DirectiveLocation.FragmentSpread,
                DirectiveLocation.InlineFragment
            })
        {
            Description = "Directs the executor to skip this field or fragment when the 'if' argument is true.";
            Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<BooleanGraphType>>
            {
                Name = "if",
                Description = "Skipped when true."
            });
        }
    }

    /// <summary>
    /// Used to declare element of a GraphQL schema as deprecated.
    /// </summary>
    public class GraphQLDeprecatedDirective : DirectiveGraphType
    {
        public GraphQLDeprecatedDirective()
            : base("deprecated", new[]
            {
                DirectiveLocation.FieldDefinition,
                DirectiveLocation.EnumValue
            })
        {
            Description = "Marks an element of a GraphQL schema as no longer supported.";
            Arguments = new QueryArguments(new QueryArgument<StringGraphType>
            {
                Name = "reason",
                Description =
                    "Explains why this element was deprecated, usually also including a " +
                    "suggestion for how to access supported similar data. Formatted " +
                    "in [Markdown](https://daringfireball.net/projects/markdown/).",
                DefaultValue = "No longer supported"
            });
        }
    }
}
