using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.CustomValidationRule.Sample;

internal static class Program
{
    private const string RequiredScope = "reports:read";

    private static async Task Main()
    {
        await ExecuteAndPrintAsync("{ publicReport }", []);
        await ExecuteAndPrintAsync("{ adminReport }", []);
        await ExecuteAndPrintAsync("{ adminReport }", [RequiredScope]);
    }

    private static async Task ExecuteAndPrintAsync(string query, IReadOnlyCollection<string> scopes)
    {
        var result = await new DocumentExecuter().ExecuteAsync(new ExecutionOptions
        {
            Schema = new ReportsSchema(),
            Query = query,
            UserContext = new Dictionary<string, object?>
            {
                [AdminFieldsRequireScopeRule.ScopeContextKey] = scopes,
            },
            ValidationRules = DocumentValidator.CoreRules.Append(AdminFieldsRequireScopeRule.Instance),
        }).ConfigureAwait(false);

        Console.WriteLine($"Query: {query}");
        Console.WriteLine($"Scopes: {string.Join(", ", scopes.DefaultIfEmpty("(none)"))}");

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
}

public sealed class ReportsSchema : Schema
{
    public ReportsSchema()
    {
        Query = new ReportsQuery();
    }
}

public sealed class ReportsQuery : ObjectGraphType
{
    public ReportsQuery()
    {
        Field<StringGraphType>("publicReport")
            .Resolve(_ => "Public report");

        Field<StringGraphType>("adminReport")
            .Resolve(_ => "Admin report")
            .WithMetadata(AdminFieldsRequireScopeRule.RequiredScopeMetadataKey, "reports:read");
    }
}

public sealed class AdminFieldsRequireScopeRule : ValidationRuleBase
{
    public const string RequiredScopeMetadataKey = "requiredScope";
    public const string ScopeContextKey = "scopes";

    public static readonly AdminFieldsRequireScopeRule Instance = new();

    private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new((field, context) =>
    {
        var requiredScope = context.TypeInfo.GetFieldDef()?.GetMetadata<string>(RequiredScopeMetadataKey);
        if (string.IsNullOrEmpty(requiredScope))
            return;

        var hasScope =
            context.UserContext.TryGetValue(ScopeContextKey, out var value) &&
            value is IEnumerable<string> scopes &&
            scopes.Contains(requiredScope, StringComparer.Ordinal);

        if (!hasScope)
            context.ReportError(new MissingScopeError(context.Document.Source, field, requiredScope));
    });

    private AdminFieldsRequireScopeRule()
    {
    }

    public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_visitor);
}

public sealed class MissingScopeError : ValidationError
{
    public MissingScopeError(ROM source, GraphQLField field, string requiredScope)
        : base(source, null, $"Field '{field.Name.Value}' requires the '{requiredScope}' scope.", field)
    {
    }
}
