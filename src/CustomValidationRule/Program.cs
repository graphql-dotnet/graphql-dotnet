using GraphQL;
using GraphQL.Types;
using GraphQL.Validation;
using CustomValidationRule;

// ── Schema definition ────────────────────────────────────────────────────────

var schema = Schema.For(@"
    type Query {
        hello: String
        greet(name: String!): String
    }
", builder =>
{
    builder.Types.Include<QueryType>();
});

// ── Helper: execute and print a query ────────────────────────────────────────

static async Task RunQuery(ISchema schema, string description, string query, bool enableNoIntrospection)
{
    Console.WriteLine($"=== {description} ===");
    Console.WriteLine($"Query: {query.Trim()}");

    var options = new ExecutionOptions
    {
        Schema = schema,
        Query = query,
    };

    if (enableNoIntrospection)
    {
        // Append the custom rule to the default set of validation rules.
        options.ValidationRules = DocumentValidator.CoreRules.Append(new NoIntrospectionRule());
    }

    var result = await new DocumentExecuter().ExecuteAsync(options);
    var json = System.Text.Json.JsonSerializer.Serialize(
        result,
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

    Console.WriteLine($"Result:\n{json}");
    Console.WriteLine();
}

// ── Run example queries ───────────────────────────────────────────────────────

// 1. Normal query – should succeed
await RunQuery(schema, "Normal query (no rule)", "{ hello }", enableNoIntrospection: false);

// 2. Normal query with the custom rule active – should still succeed
await RunQuery(schema, "Normal query (with NoIntrospection rule)", "{ hello }", enableNoIntrospection: true);

// 3. Introspection query WITHOUT the custom rule – should succeed
await RunQuery(schema, "Introspection query (no rule)", "{ __schema { queryType { name } } }", enableNoIntrospection: false);

// 4. Introspection query WITH the custom rule – should be rejected
await RunQuery(schema, "Introspection query (with NoIntrospection rule)", "{ __schema { queryType { name } } }", enableNoIntrospection: true);

// ── Resolver ────────────────���─────────────────────────────────────────────────

[GraphQL.Attributes.GraphQLMetadata("Query")]
public class QueryType
{
    public string Hello() => "world";

    public string Greet(string name) => $"Hello, {name}!";
}
