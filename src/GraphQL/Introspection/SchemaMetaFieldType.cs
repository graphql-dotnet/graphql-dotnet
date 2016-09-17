using GraphQL.Resolvers;
using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class SchemaMetaFieldType : FieldType
    {
        public SchemaMetaFieldType()
        {
            Name = "__schema";
            Type = new __Schema();
            Description = "Access the current type schema of this server.";
            Resolver = new FuncFieldResolver<ISchema>(context => context.Schema);
        }
    }

    public class __Schema : ObjectGraphType<object>
    {
        public __Schema()
        {
            var type = new __Type();

            Name = "__Schema";
            Description =
                "A GraphQL Schema defines the capabilities of a GraphQL server. It " +
                "exposes all available types and directives on the server, as well as " +
                "the entry points for query, mutation, and subscription operations.";

            Field(
                new NonNullGraphType(
                    new ListGraphType(new NonNullGraphType(type))
                ),
                "types",
                "A list of all types supported by this server.",
                resolve: context =>
                {
                    return context.Schema.AllTypes;
                });

            Field(
                new NonNullGraphType(type),
                "queryType",
                "The type that query operations will be rooted at.",
                resolve: context =>
                {
                    return context.Schema.Query;
                });

            Field(
                type,
                "mutationType",
                "If this server supports mutation, the type that mutation operations will be rooted at.",
                resolve: context =>
                {
                    return context.Schema.Mutation;
                });

            Field(
                type,
                "subscriptionType",
                "If this server supports subscription, the type that subscription operations will be rooted at.",
                resolve: context =>
                {
                    return context.Schema.Subscription;
                });

            Field(
                new NonNullGraphType(
                    new ListGraphType(new NonNullGraphType(new __Directive()))
                ),
                "directives",
                "A list of all directives supported by this server.",
                resolve: context =>
                {
                    return context.Schema.Directives;
                });
        }
    }
}
