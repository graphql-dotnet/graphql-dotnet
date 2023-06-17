using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class LoneAnonymousOperationTests : ValidationTestBase<LoneAnonymousOperation, ValidationSchema>
{
    [Fact]
    public void no_operations()
    {
        ShouldPassRule("""
            fragment fragA on Type {
              field
            }
            """);
    }

    [Fact]
    public void one_anon_operation()
    {
        ShouldPassRule("""
            {
              field
            }
            """);
    }

    [Fact]
    public void one_named_operation()
    {
        ShouldPassRule("""
                query Foo {
                  field
                }
                """);
    }

    [Fact]
    public void multiple_operations()
    {
        ShouldPassRule("""
                query Foo {
                  field
                }

                query Bar {
                  field
                }
                """);
    }

    [Fact]
    public void one_anon_with_fragment()
    {
        ShouldPassRule("""
                {
                  ...Foo
                }

                fragment Foo on Type {
                  field
                }
                """);
    }

    [Fact]
    public void multiple_anon_operations()
    {
        const string query = """
                {
                  fieldA
                }

                {
                  fieldB
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(LoneAnonymousOperationError.AnonOperationNotAloneMessage(), 1, 1);
            _.Error(LoneAnonymousOperationError.AnonOperationNotAloneMessage(), 5, 1);
        });
    }

    [Fact]
    public void anon_operation_with_mutation()
    {
        const string query = """
                {
                  fieldA
                }

                mutation Foo {
                  fieldB
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(LoneAnonymousOperationError.AnonOperationNotAloneMessage(), 1, 1);
        });
    }

    [Fact]
    public void anon_operation_with_subscription()
    {
        const string query = """
                {
                  fieldA
                }

                subscription Foo {
                  fieldB
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(LoneAnonymousOperationError.AnonOperationNotAloneMessage(), 1, 1);
        });
    }
}
