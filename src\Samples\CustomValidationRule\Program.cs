using CustomValidationRuleSample;
using GraphQL;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using GraphQL.Validation;

// This sample demonstrates how to create a custom validation rule.
//
// The NoIntrospectionValidationRule prohibits introspection queries,
// which can be useful in production to prevent clients from discovering
// your schema structure.
//
// To run this sample:
//   dotnet run --project src/Samples/CustomValidationRule

var schema = Schema.For("""
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

// Add the custom rule alongside the built-in core rules
var validationRules = DocumentValidator.CoreRules.Append(new NoIntrospectionValidationRule());

await RunExampleAsync(
    "Example 1: Normal query (should succeed)",
    "{ hello }");

await RunExampleAsync(
    "Example 2: Query with arguments (should succeed)",
    "{ add(a: 3, b: 4) }");

await RunExampleAsync(
    "Example 3: Introspection query (should fail)",
    "{ __schema { types { name } } }");

await RunExampleAsync(
    "Example 4: Mixed query with introspection field (should fail)",
    "{ hello __typename }");

async Task RunExampleAsync(string title, string query)
{
    Console.WriteLine($"=== {title} ===");

    var result = await executer.ExecuteAsync(opt =>
    {
        opt.Schema = schema;
        opt.Query = query;
        opt.ValidationRules = validationRules;
    });

    Console.WriteLine(await serializer.SerializeToStringAsync(result));
    Console.WriteLine();
}
