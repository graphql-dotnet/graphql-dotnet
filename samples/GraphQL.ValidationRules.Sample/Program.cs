using GraphQL;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser.AST;

Console.WriteLine("GraphQL.NET custom validation rule sample");
Console.WriteLine();

var schema = new ValidationSampleSchema();
var executer = new DocumentExecuter();

await ExecuteAndPrintAsync("{ publicMessage }");
await ExecuteAndPrintAsync("{ secretMessage }");

return;

async Task ExecuteAndPrintAsync(string query)
{
    var result = await executer.ExecuteAsync(options =>
    {
        options.Schema = schema;
        options.Query = query;
        options.ValidationRules = DocumentValidator.CoreRules.Append(BlockSecretFieldValidationRule.Instance);
        options.ThrowOnUnhandledException = true;
    }).ConfigureAwait(false);

    Console.WriteLine($"Query: {query}");
    if (result.Errors?.Count > 0)
    {
        foreach (var error in result.Errors)
            Console.WriteLine($"Validation error: {error.Message}");
    }
    else
    {
        Console.WriteLine("Validation passed.");
    }
    Console.WriteLine();
}

public sealed class ValidationSampleSchema : Schema
{
    public ValidationSampleSchema()
    {
        Query = new ValidationSampleQuery();
    }
}

public sealed class ValidationSampleQuery : ObjectGraphType
{
    public ValidationSampleQuery()
    {
        Field<StringGraphType>("publicMessage")
            .Resolve(_ => "This field is always allowed.");

        Field<StringGraphType>("secretMessage")
            .Resolve(_ => "This field is blocked by custom validation.");
    }
}

public sealed class BlockSecretFieldValidationRule : ValidationRuleBase
{
    public static readonly BlockSecretFieldValidationRule Instance = new();

    private BlockSecretFieldValidationRule()
    {
    }

    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new((field, context) =>
    {
        if (field.Name.Value == "secretMessage")
        {
            context.ReportError(new ValidationError(
                context.Document.Source,
                "BLOCKED_FIELD",
                "Field 'secretMessage' is blocked by BlockSecretFieldValidationRule.",
                field));
        }
    });

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_visitor);
}
