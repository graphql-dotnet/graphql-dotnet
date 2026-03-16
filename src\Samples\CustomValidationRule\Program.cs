using CustomValidationRuleSample;
using GraphQL;
using GraphQL.Types;
using GraphQL.Validation;

// This sample demonstrates how to create a custom validation rule.
// The NoIntrospectionValidationRule prohibits introspection queries,
// which can be useful in production to hide schema structure.

var schema = Schema.For(@"
    type Query {
        hello: String
        add(a: Int!, b: Int!): Int
    }
", builder =>
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
var serializer = new GraphQL.SystemTextJson.GraphQLSerializer(indent: true);

var validationRules = DocumentValidator.CoreRules.Append(new NoIntrospectionValidationRule());

// --- Example 1: Normal query (should succeed) ---
Console.WriteLine("=== Example 1: Normal query ===");
var result = await executer.ExecuteAsync(opt =>
{
    opt.Schema = schema;
    opt.Query = "{ hello }";
    opt.ValidationRules = validationRules;
});
Console.WriteLine(await serializer.SerializeToStringAsync(result));

// --- Example 2: Query with arguments (should succeed) ---
Console.WriteLine("=== Example 2: Query with arguments ===");
result = await executer.ExecuteAsync(opt =>
{
    opt.Schema = schema;
    opt.Query = "{ add(a: 3, b: 4) }";
    opt.ValidationRules = validationRules;
});
Console.WriteLine(await serializer.SerializeToStringAsync(result));

// --- Example 3: Introspection query (should fail) ---
Console.WriteLine("=== Example 3: Introspection query (should fail) ===");
result = await executer.ExecuteAsync(opt =>
{
    opt.Schema = schema;
    opt.Query = "{ __schema { types { name } } }";
    opt.ValidationRules = validationRules;
});
Console.WriteLine(await serializer.SerializeToStringAsync(result));

// --- Example 4: Mixed query with __typename (should fail) ---
Console.WriteLine("=== Example 4: Mixed query with introspection field (should fail) ===");
result = await executer.ExecuteAsync(opt =>
{
    opt.Schema = schema;
    opt.Query = "{ hello __typename }";
    opt.ValidationRules = validationRules;
});
Console.WriteLine(await serializer.SerializeToStringAsync(result));
