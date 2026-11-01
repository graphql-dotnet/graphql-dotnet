using GraphQL.CustomValidation.Sample;
using GraphQL.Types;

// Example: Registering custom validation rules with a GraphQL schema

// Define a simple schema
public class PersonType : ObjectGraphType
{
    public PersonType()
    {
        Field<StringGraphType>("name");
        Field<StringGraphType>("email");
        Field<StringGraphType>("ssn").Description("Social Security Number - restricted field");
        Field<IntGraphType>("salary").Description("Salary - restricted field");
        Field<ListGraphType<PersonType>>("friends");
    }
}

public class QueryType : ObjectGraphType
{
    public QueryType()
    {
        Field<PersonType>("person")
            .Argument<StringGraphType>("id")
            .Resolve(context => new Dictionary<string, object?>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com",
                ["ssn"] = "123-45-6789",
                ["salary"] = 75000,
                ["friends"] = Array.Empty<object>()
            });
    }
}

// Setup validation rules
var restrictedFields = new[] { "ssn", "salary" };
string? currentUser = null; // Simulate: set to a username to "authenticate"

var depthRule = new QueryDepthValidationRule(maxDepth: 5);
var authRule = new FieldAuthorizationRule(restrictedFields, () => currentUser);

// Usage example with ExecuteAsync:
Console.WriteLine("Custom Validation Rules Sample");
Console.WriteLine("===============================");
Console.WriteLine();
Console.WriteLine("1. QueryDepthValidationRule - limits query nesting depth");
Console.WriteLine("   Usage: new QueryDepthValidationRule(maxDepth: 5)");
Console.WriteLine();
Console.WriteLine("2. FieldAuthorizationRule - restricts fields by auth status");
Console.WriteLine("   Usage: new FieldAuthorizationRule(new[]{"ssn"}, () => userName)");
Console.WriteLine();
Console.WriteLine("Registration with DI:");
Console.WriteLine("  services.AddGraphQL(b => b");
Console.WriteLine("    .AddSchema<MySchema>()");
Console.WriteLine("    .AddValidationRule<QueryDepthValidationRule>()");
Console.WriteLine("    .AddValidationRule<FieldAuthorizationRule>());");
