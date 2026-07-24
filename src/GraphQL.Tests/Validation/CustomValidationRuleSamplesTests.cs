using GraphQL.Validation;
using GraphQLParser.AST;

namespace GraphQL.Tests.Validation;

public class CustomValidationRuleSamplesTests : ValidationTestBase<CustomValidationRuleSamplesTests.RequireHumanIdValidationRule, ValidationSchema>
{
    [Fact]
    public void custom_field_rule_allows_unrelated_fields()
    {
        ShouldPassRule("""
            {
              dog {
                name
              }
            }
            """);
    }

    [Fact]
    public void custom_field_rule_allows_human_field_with_id_argument()
    {
        ShouldPassRule("""
            {
              human(id: "1000") {
                name
              }
            }
            """);
    }

    [Fact]
    public void custom_field_rule_rejects_human_field_without_id_argument()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                {
                  human {
                    name
                  }
                }
                """;
            _.Error(
                message: "The 'human' field requires an 'id' argument.",
                line: 2,
                column: 3);
        });
    }

    public class RequireHumanIdValidationRule : ValidationRuleBase
    {
        public override ValueTask<INodeVisitor?> GetPreNodeVisitorAsync(ValidationContext context) => new(_visitor);

        private static readonly MatchingNodeVisitor<GraphQLField> _visitor = new((field, context) =>
        {
            var fieldDef = context.TypeInfo.GetFieldDef();
            if (fieldDef?.Name != "human")
                return;

            var hasIdArgument = field.Arguments?.Any(argument => argument.Name == "id") == true;
            if (!hasIdArgument)
            {
                context.ReportError(new ValidationError(
                    context.Document.Source,
                    number: null,
                    message: "The 'human' field requires an 'id' argument.",
                    field));
            }
        });
    }
}
