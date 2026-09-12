using GraphQL.Types;
using GraphQL.Validation;
using GraphQLParser;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class CustomValidationRuleSampleTests
{
    [Fact]
    public async Task Allows_field_without_required_scope_metadata()
    {
        var result = await ExecuteAsync("{ publicReport }");

        result.Errors.ShouldBeNull();
        result.Executed.ShouldBeTrue();
    }

    [Fact]
    public async Task Blocks_field_when_required_scope_is_missing()
    {
        var result = await ExecuteAsync("{ adminReport }");

        result.Executed.ShouldBeFalse();

        var error = result.Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe("MISSING_SCOPE");
        error.Message.ShouldBe("Field 'adminReport' requires the 'reports:read' scope.");
    }

    [Fact]
    public async Task Allows_field_when_required_scope_is_present()
    {
        var result = await ExecuteAsync("{ adminReport }", "reports:read");

        result.Errors.ShouldBeNull();
        result.Executed.ShouldBeTrue();
    }

    private static Task<ExecutionResult> ExecuteAsync(string query, params string[] scopes)
        => new DocumentExecuter().ExecuteAsync(new ExecutionOptions
        {
            Schema = new ReportsSchema(),
            Query = query,
            UserContext = new Dictionary<string, object?>
            {
                [AdminFieldsRequireScopeRule.ScopeContextKey] = scopes,
            },
            ValidationRules = DocumentValidator.CoreRules.Append(AdminFieldsRequireScopeRule.Instance),
        });

    private sealed class ReportsSchema : Schema
    {
        public ReportsSchema()
        {
            Query = new ReportsQuery();
        }
    }

    private sealed class ReportsQuery : ObjectGraphType
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

    private sealed class AdminFieldsRequireScopeRule : ValidationRuleBase
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

    private sealed class MissingScopeError : ValidationError
    {
        public MissingScopeError(ROM source, GraphQLField field, string requiredScope)
            : base(source, null, $"Field '{field.Name.Value}' requires the '{requiredScope}' scope.", field)
        {
        }
    }
}
