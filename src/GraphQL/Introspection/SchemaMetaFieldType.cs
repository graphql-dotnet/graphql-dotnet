using GraphQL.Types;

namespace GraphQL.Introspection
{
    public class SchemaMetaFieldType : FieldType
    {
        public SchemaMetaFieldType()
        {
            Name = "__schema";
            Type = typeof (__Schema);
            Description = "Access the current type schema of this server.";
            Resolve = context => context.Schema;
        }
    }

    public class __Schema : ObjectGraphType
    {
        public __Schema()
        {
            Name = "__Schema";
            Description = "A GraphQL Schema defines the capabilities of a GraphQL server. It exposes all available types and directives on the server, as well as the entry points for query and mutation operations.";

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__Type>>>>(
                "types",
                "A list of all types supported by this server.",
                null,
                context =>
                {
                    return context.Schema.AllTypes;
                });

            Field<NonNullGraphType<__Type>>(
                "queryType",
                "The type that query operations will be rooted at.",
                null,
                context =>
                {
                    return context.Schema.Query;
                });

            Field<__Type>(
                "mutationType",
                "If this server supports mutation, the type that mutation operations will be rooted at.",
                null,
                context =>
                {
                    return context.Schema.Mutation;
                });

            Field<NonNullGraphType<ListGraphType<NonNullGraphType<__Directive>>>>(
                "directives",
                "A list of all directives supported by this server.",
                null,
                context =>
                {
                    return context.Schema.Directives;
                });
        }
    }
}
