using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GraphQL.Types
{
    /// <summary>
    /// Directives must only be used in the locations they are declared to belong in.
    /// https://graphql.github.io/graphql-spec/June2018/#sec-Type-System.Directives
    /// </summary>
    public enum DirectiveLocation
    {
        // ExecutableDirectiveLocation

        /// <summary>Location adjacent to a query operation.</summary>
        [Description("Location adjacent to a query operation.")]
        Query,
        /// <summary>Location adjacent to a mutation operation.</summary>
        [Description("Location adjacent to a mutation operation.")]
        Mutation,
        /// <summary>Location adjacent to a subscription operation.</summary>
        [Description("Location adjacent to a subscription operation.")]
        Subscription,
        /// <summary>Location adjacent to a field.</summary>
        [Description("Location adjacent to a field.")]
        Field,
        /// <summary>Location adjacent to a fragment definition.</summary>
        [Description("Location adjacent to a fragment definition.")]
        FragmentDefinition,
        /// <summary>Location adjacent to a fragment spread.</summary>
        [Description("Location adjacent to a fragment spread.")]
        FragmentSpread,
        /// <summary>Location adjacent to an inline fragment.</summary>
        [Description("Location adjacent to an inline fragment.")]
        InlineFragment,

        // TypeSystemDirectiveLocation

        /// <summary>Location adjacent to a schema definition.</summary>
        [Description("Location adjacent to a schema definition.")]
        Schema,
        /// <summary>Location adjacent to a scalar definition.</summary>
        [Description("Location adjacent to a scalar definition.")]
        Scalar,
        /// <summary>Location adjacent to an object type definition.</summary>
        [Description("Location adjacent to an object type definition.")]
        Object,
        /// <summary>Location adjacent to a field definition.</summary>
        [Description("Location adjacent to a field definition.")]
        FieldDefinition,
        /// <summary>Location adjacent to an argument definition.</summary>
        [Description("Location adjacent to an argument definition.")]
        ArgumentDefinition,
        /// <summary>Location adjacent to an interface definition.</summary>
        [Description("Location adjacent to an interface definition.")]
        Interface,
        /// <summary>Location adjacent to a union definition.</summary>
        [Description("Location adjacent to a union definition.")]
        Union,
        /// <summary>Location adjacent to an enum definition.</summary>
        [Description("Location adjacent to an enum definition")]
        Enum,
        /// <summary>Location adjacent to an enum value definition.</summary>
        [Description("Location adjacent to an enum value definition")]
        EnumValue,
        /// <summary>Location adjacent to an input object type definition.</summary>
        [Description("Location adjacent to an input object type definition.")]
        InputObject,
        /// <summary>Location adjacent to an input object field definition.</summary>
        [Description("Location adjacent to an input object field definition.")]
        InputFieldDefinition
    }

    /// <summary>
    /// Directives are used by the GraphQL runtime as a way of modifying execution
    /// behavior.Type system creators will usually not create these directly.
    /// </summary>
    public class DirectiveGraphType : INamedType
    {
        public static readonly IncludeDirective Include = new IncludeDirective();
        public static readonly SkipDirective Skip = new SkipDirective();
        public static readonly GraphQLDeprecatedDirective Deprecated = new GraphQLDeprecatedDirective();

        public DirectiveGraphType(string name, IEnumerable<DirectiveLocation> locations)
        {
            Name = name;
            Locations.AddRange(locations);

            if (Locations.Count == 0)
                throw new ArgumentException("Directive must have locations", nameof(locations));
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public QueryArguments Arguments { get; set; }

        public List<DirectiveLocation> Locations { get; } = new List<DirectiveLocation>();
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
