using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class FieldArgumentsAreValidRuleTests : ValidationTestBase<FieldArgumentsAreValidRule, ValidationSchema>
{
    [Fact]
    public void works_with_str1()
    {
        ShouldPassRule("""
            {
              argValidation (str1: "abc")
            }
            """);
    }

    [Fact]
    public void works_with_str2()
    {
        ShouldPassRule("""
            {
              argValidation (str2: "abc")
            }
            """);
    }

    [Fact]
    public void works_with_str1_and_str2()
    {
        ShouldPassRule("""
            {
              argValidation (str1: "abc", str2: "def")
            }
            """);
    }

    [Fact]
    public void fails_with_neither()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                {
                  argValidation
                }
                """;
            _.Error(err =>
            {
                err.Message = "Must provide str1 or str2";
                err.Loc(2, 3);
            });
        });
    }

    [Fact]
    public void bubbles_unhandled_errors()
    {
        Should.Throw<InvalidOperationException>(() =>
            ShouldPassRule("""
                {
                    argValidation (str1: "error")
                }
                """))
            .Message.ShouldBe("critical failure");
    }
}
