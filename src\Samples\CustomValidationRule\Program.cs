using GraphQL;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

// This sample demonstrates how to create a custom validation rule
// that prohibits introspection queries.

var schema = Schema.For(@"
    type Query {
        hello: String
    }
", builder => {
    builder.Types.For("Query").ResolveField("hello", ctx => "Hello World!");
});

// Execute a normal query - should succeed
Console.WriteLine("=== Normal Query ===");
var result1 = await new DocumentExecuter().ExecuteAsync(options =>
{
    options.Schema = schema;
    options.Query = "{ hello }";
    options.ValidationRules = DocumentValidator.CoreRules.Append(new NoIntrospectionValidationRule());
});
Console.WriteLine(result1.Errors == null
    ? $"Result: {System.Text.Json.JsonSerializer.Serialize(result1.Data)}"
    : $"Errors: {string.Join(", ", result1.Errors.Select(e => e.Message))}");

// Execute an introspection query - should fail
Console.WriteLine();
Console.WriteLine("=== Introspection Query (should fail) ===");
var result2 = await new DocumentExecuter().ExecuteAsync(options =>
{
    options.Schema = schema;
    options.Query = "{ __schema { types { name } } }";
    options.ValidationRules = DocumentValidator.CoreRules.Append(new NoIntrospectionValidationRule());
});
Console.WriteLine(result2.Errors == null
    ? $"Result: {System.Text.Json.JsonSerializer.Serialize(result2.Data)}"
    : $"Errors: {string.Join(", ", result2.Errors.Select(e => e.Message))}");

// Execute a mixed query - should fail
Console.WriteLine();
Console.WriteLine("=== Mixed Query (should fail) ===");
var result3 = await new DocumentExecuter().ExecuteAsync(options =>
{
    options.Schema = schema;
    options.Query = "{ hello __typename }";
    options.ValidationRules = DocumentValidator.CoreRules.Append(new NoIntrospectionValidationRule());
});
Console.WriteLine(result3.Errors == null
    ? $"Result: {System.Text.Json.JsonSerializer.Serialize(result3.Data)}"
    : $"Errors: {string.Join(", ", result3.Errors.Select(e => e.Message))}");
