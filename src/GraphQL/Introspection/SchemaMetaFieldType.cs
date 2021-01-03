using System.Linq;
using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__schema</c> meta-field is available on the root of a query operation and returns a <c>__Schema</c> graph type for the schema.
    /// </summary>
    public class SchemaMetaFieldType : FieldType
    {
        /// <summary>
        /// Initializes a new instance of the <c>__schema</c> meta-field.
        /// </summary>
        public SchemaMetaFieldType()
        {
            Name = "__schema";
            Type = typeof(__Schema);
            Description = "Access the current type schema of this server.";
            Resolver = new FuncFieldResolver<ISchema>(context => context.Schema);
        }
    }

    /// <summary>
    /// The <c>__Schema</c> introspection type allows querying the schema for available types and directives.
    /// </summary>
    public class __Schema : ObjectGraphType<object>
    {
        /// <summary>
        /// Initializes a new instance of the <c>__Schema</c> introspection type.
        /// </summary>
        public __Schema()
        {
            Name = "__Schema";
            Description =
                "A GraphQL Schema defines the capabilities of a GraphQL server. It " +
                "exposes all available types and directives on the server, as well as " +
                "the entry points for query, mutation, and subscription operations.";

            Field<StringGraphType>(
                "description",
                resolve: context => context.Schema.Description);

            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<__Type>>>>(
                "types",
                "A list of all types supported by this server.",
                resolve: async context =>
                {
                    var types = await context.Schema.AllTypes.WhereAsync(x => context.Schema.Filter.AllowType(x)).ConfigureAwait(false);
                    if (context.Schema.Comparer.TypeComparer != null)
                        types = types.OrderBy(t => t, context.Schema.Comparer.TypeComparer);
                    return types;
                });


            Field<NonNullGraphType<__Type>>(
                "queryType",
                "The type that query operations will be rooted at.",
                resolve: context => context.Schema.Query);

            FieldAsync<__Type>(
                "mutationType",
                "If this server supports mutation, the type that mutation operations will be rooted at.",
                resolve: async context =>
                {
                    if (await context.Schema.Filter.AllowType(context.Schema.Mutation).ConfigureAwait(false))
                    {
                        return context.Schema.Mutation;
                    }
                    return null;
                });

            FieldAsync<__Type>(
                "subscriptionType",
                "If this server supports subscription, the type that subscription operations will be rooted at.",
                resolve: async context =>
                {
                    if (await context.Schema.Filter.AllowType(context.Schema.Subscription).ConfigureAwait(false))
                    {
                        return context.Schema.Subscription;
                    }
                    return null;
                });

            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<__Directive>>>>(
                "directives",
                "A list of all directives supported by this server.",
                resolve: async context =>
                {
                    var directives = await context.Schema.Directives.WhereAsync(d => context.Schema.Filter.AllowDirective(d)).ConfigureAwait(false);
                    if (context.Schema.Comparer.DirectiveComparer != null)
                        directives = directives.OrderBy(d => d, context.Schema.Comparer.DirectiveComparer);
                    return directives;
                });
        }
    }
}
