using GraphQL.Tests.Subscription;
using GraphQL.Types;
using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class UniqueOperationNamesTests : ValidationTestBase<UniqueOperationNames, ValidationSchema>
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
    public void multiple_operations_of_different_types()
    {
        ShouldPassRule("""
                query Foo {
                  field
                }

                mutation Bar {
                  field
                }

                subscription Baz {
                  field
                }
                """);
    }

    [Fact]
    public async Task TestMe()
    {
        var s = new Schema();
        var queryType = new ObjectGraphType();
        queryType.Field<IntGraphType>("abc");
        s.Query = queryType;
        var t = """
            mutation {
              abc
            }
            """;
        var result = await new DocumentExecuter().ExecuteAsync(o =>
        {
            o.Schema = s;
            o.Query = t;
        });
        result.ShouldNotBeSuccessful();
    }

    [Fact]
    public void fragment_and_operation_named_the_same()
    {
        ShouldPassRule("""
                query Foo {
                  ...Foo
                }

                fragment Foo on Type {
                  field
                }
                """);
    }

    [Fact]
    public void multiple_operations_of_same_name()
    {
        const string query = """
                query Foo {
                  fieldA
                }

                query Foo {
                  fieldB
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(UniqueOperationNamesError.DuplicateOperationNameMessage("Foo"), 5, 1);
        });
    }

    [Fact]
    public void multiple_operations_of_same_name_of_different_types_mutation()
    {
        const string query = """
                query Foo {
                  fieldA
                }

                mutation Foo {
                  fieldB
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(UniqueOperationNamesError.DuplicateOperationNameMessage("Foo"), 5, 1);
        });
    }

    [Fact]
    public void multiple_operations_of_same_name_of_different_types_subscription()
    {
        const string query = """
                query Foo {
                  fieldA
                }

                subscription Foo {
                  fieldB
                }
                """;

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(UniqueOperationNamesError.DuplicateOperationNameMessage("Foo"), 5, 1);
        });
    }
}
