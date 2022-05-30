using GraphQL.Types;

namespace GraphQL.Introspection
{
    /// <summary>
    /// The <c>__Schema</c> introspection type allows querying the schema for available types and directives.
    /// </summary>
    public class __Schema : ObjectGraphType<ISchema>
    {
        /// <summary>
        /// Initializes a new instance of the <c>__Schema</c> introspection type.
        /// </summary>
        /// <param name="allowAppliedDirectives">Allows 'appliedDirectives' field for this type. It is an experimental feature.</param>
        public __Schema(bool allowAppliedDirectives = false)
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
                    var types = context.ArrayPool.Rent<IGraphType>(context.Schema.AllTypes.Count);

                    int index = 0;
                    foreach (var item in context.Schema.AllTypes.Dictionary)
                    {
                        if (await context.Schema.Filter.AllowType(item.Value).ConfigureAwait(false))
                            types[index++] = item.Value;
                    }

                    var comparer = context.Schema.Comparer.TypeComparer;
                    if (comparer != null)
                        Array.Sort(types, 0, index, comparer);

                    return types.Constrained(index);
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
                    return context.Schema.Mutation != null && await context.Schema.Filter.AllowType(context.Schema.Mutation).ConfigureAwait(false)
                        ? context.Schema.Mutation
                        : null;
                });

            FieldAsync<__Type>(
                "subscriptionType",
                "If this server supports subscription, the type that subscription operations will be rooted at.",
                resolve: async context =>
                {
                    return context.Schema.Subscription != null && await context.Schema.Filter.AllowType(context.Schema.Subscription).ConfigureAwait(false)
                        ? context.Schema.Subscription
                        : null;
                });

            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<__Directive>>>>(
                "directives",
                "A list of all directives supported by this server.",
                resolve: async context =>
                {
                    var directives = context.ArrayPool.Rent<Directive>(context.Schema.Directives.Count);

                    int index = 0;
                    foreach (var directive in context.Schema.Directives.List)
                    {
                        if (await context.Schema.Filter.AllowDirective(directive).ConfigureAwait(false))
                            directives[index++] = directive;
                    }

                    var comparer = context.Schema.Comparer.DirectiveComparer;
                    if (comparer != null)
                        Array.Sort(directives, 0, index, comparer);

                    return directives.Constrained(index);
                });

            if (allowAppliedDirectives)
                this.AddAppliedDirectivesField("schema");
        }
    }
}
