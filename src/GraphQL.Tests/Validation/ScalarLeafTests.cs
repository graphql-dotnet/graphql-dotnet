using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class ScalarLeafTests : ValidationTestBase<ScalarLeafs, ValidationSchema>
{
    [Fact]
    public void valid_scalar_selection()
    {
        ShouldPassRule("""
                fragment scalarSelection on Dog {
                  barks
                }
                """);
    }

    [Fact]
    public void object_type_missing_selection()
    {
        var query = """
                query directQueryOnObjectWithoutSubFields{
                  human
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(
                message: RequiredSubselectionMessage("human", "Human"),
                line: 2,
                column: 3);
        });
    }

    [Fact]
    public void interface_type_missing_selection()
    {
        var query = """
                {
                  human {
                    pets
                  }
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(
                message: RequiredSubselectionMessage("pets", "[Pet]"),
                line: 3,
                column: 5);
        });
    }

    [Fact]
    public void valid_scalar_selection_with_args()
    {
        ShouldPassRule("""
                fragment scalarSelectionWithArgs on Dog {
                  doesKnowCommand(dogCommand: SIT)
                }
                """);
    }

    [Fact]
    public void scalar_selection_not_allowed_on_boolean()
    {
        var query = """
                fragment scalarSelectionNotAllowedOnBoolean on Dog {
                  barks { sinceWhen }
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(
                message: NoSubselectionAllowedMessage("barks", "Boolean"),
                line: 2,
                column: 9);
        });
    }

    [Fact]
    public void scalar_selection_not_allowed_on_enum()
    {
        var query = """
                fragment scalarSelectionsNotAllowedOnEnum on Cat {
                  furColor { inHexdec }
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(
                message: NoSubselectionAllowedMessage("furColor", "FurColor"),
                line: 2,
                column: 12);
        });
    }

    [Fact]
    public void scalar_selection_not_allowed_with_args()
    {
        var query = """
                fragment scalarSelectionWithArgs on Dog {
                  doesKnowCommand(dogCommand: SIT) { sinceWhen }
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(
                message: NoSubselectionAllowedMessage("doesKnowCommand", "Boolean"),
                line: 2,
                column: 36);
        });
    }

    [Fact]
    public void scalar_selection_not_allowed_with_directives()
    {
        var query = """
                fragment scalarSelectionsNotAllowedWithDirectives on Dog {
                  name @include(if: true) { isAlsoHumanName }
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(
                message: NoSubselectionAllowedMessage("name", "String"),
                line: 2,
                column: 27);
        });
    }

    [Fact]
    public void scalar_selection_not_allowed_with_directives_and_args()
    {
        var query = """
                fragment scalarSelectionsNotAllowedWithDirectivesAndArgs on Dog {
                  doesKnowCommand(dogCommand: SIT) @include(if: true) { sinceWhen }
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(
                message: NoSubselectionAllowedMessage("doesKnowCommand", "Boolean"),
                line: 2,
                column: 55);
        });
    }

    private static string NoSubselectionAllowedMessage(string field, string type)
        => ScalarLeafsError.NoSubselectionAllowedMessage(field, type);

    private static string RequiredSubselectionMessage(string field, string type)
        => ScalarLeafsError.RequiredSubselectionMessage(field, type);
}
