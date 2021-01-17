using System;
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
            SetName("__schema", validate: false);
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
                    return await context.Schema.Filter.AllowType(context.Schema.Mutation).ConfigureAwait(false)
                        ? context.Schema.Mutation
                        : null;
                });

            FieldAsync<__Type>(
                "subscriptionType",
                "If this server supports subscription, the type that subscription operations will be rooted at.",
                resolve: async context =>
                {
                    return await context.Schema.Filter.AllowType(context.Schema.Subscription).ConfigureAwait(false)
                        ? context.Schema.Subscription
                        : null;
                });

            FieldAsync<NonNullGraphType<ListGraphType<NonNullGraphType<__Directive>>>>(
                "directives",
                "A list of all directives supported by this server.",
                resolve: async context =>
                {
                    var directives = context.ArrayPool.Rent<DirectiveGraphType>(context.Schema.Directives.Count);

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
        }
    }
}
