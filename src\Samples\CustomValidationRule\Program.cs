using CustomValidationRuleSample;
using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;

// ---------------------------------------------------------------------------
// Custom Validation Rule Sample
//
// This sample shows how to implement IValidationRule to enforce
// application-specific constraints on incoming GraphQL queries.
//
// The NoIntrospectionValidationRule blocks any field whose name starts with
// "__" (e.g. __schema, __type, __typename).  Disabling introspection in
// production is a common security measure.
//
// Run this project with:
//   dotnet run --project src/Samples/CustomValidationRule
// ---------------------------------------------------------------------------

var schema = Schema.For(
    """
    type Query {
        hello: String
        add(a: Int!, b: Int!): Int
    }
    """,
    builder =>
    {
        builder.Types.For("Query").ResolveField("hello", _ => "Hello World!");
        builder.Types.For("Query").ResolveField("add", ctx =>
        {
            var a = ctx.GetArgument<int>("a");
            var b = ctx.GetArgument<int>("b");
            return a + b;
        });
    });

var executer = new DocumentExecuter();
var serializer = new GraphQLSerializer(indent: true);

// Combine the built-in core rules with our custom rule.
var validationRules = DocumentValidator.CoreRules.Append(new NoIntrospectionValidationRule());

await RunAsync("Example 1: Normal query (should succeed)",          "{ hello }");
await RunAsync("Example 2: Query with arguments (should succeed)",  "{ add(a: 3, b: 4) }");
await RunAsync("Example 3: Introspection query (should fail)",      "{ __schema { types { name } } }");
await RunAsync("Example 4: Field with __typename (should fail)",    "{ hello __typename }");

async Task RunAsync(string title, string query)
{
    Console.WriteLine($"=== {title} ===");
    Console.WriteLine($"Query: {query}");

    var result = await executer.ExecuteAsync(opt =>
    {
        opt.Schema = schema;
        opt.Query = query;
        opt.ValidationRules = validationRules;
    });

    Console.WriteLine(await serializer.SerializeToStringAsync(result));
    Console.WriteLine();
}
