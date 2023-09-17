using GraphQL.Validation.Rules.Custom;

namespace GraphQL.Tests.Validation;

public class NoIntrospectionTests : ValidationTestBase<NoIntrospectionValidationRule, ValidationSchema>
{
    [Fact]
    public void works()
    {
        ShouldPassRule("""
            {
              __typename
              complicatedArgs {
                __typename
                complexArgField(complexArg: { requiredField: true, stringField: "aaaa" })
              }
            }
            """);
    }

    [Fact]
    public void fails_type()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                {
                  __type(name:"Query") {
                    kind
                  }
                }
                """;
            _.Error(
               message: "Introspection queries are not allowed.",
               line: 2,
               column: 3);
        });
    }

    [Fact]
    public void fails_schema()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                {
                  __schema {
                    description
                  }
                }
                """;
            _.Error(
               message: "Introspection queries are not allowed.",
               line: 2,
               column: 3);
        });
    }
}
