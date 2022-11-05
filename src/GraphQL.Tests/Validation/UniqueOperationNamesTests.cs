using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class UniqueOperationNamesTests : ValidationTestBase<UniqueOperationNames, ValidationSchema>
{
    [Fact]
    public void no_operations()
    {
        ShouldPassRule(@"
                fragment fragA on Type {
                  field
                }
                ");
    }

    [Fact]
    public void one_anon_operation()
    {
        ShouldPassRule(@"
                {
                  field
                }
                ");
    }

    [Fact]
    public void one_named_operation()
    {
        ShouldPassRule(@"
                query Foo {
                  field
                }
                ");
    }

    [Fact]
    public void multiple_operations()
    {
        ShouldPassRule(@"
                query Foo {
                  field
                }

                query Bar {
                  field
                }
                ");
    }

    [Fact]
    public void multiple_operations_of_different_types()
    {
        ShouldPassRule(@"
                query Foo {
                  field
                }

                mutation Bar {
                  field
                }

                subscription Baz {
                  field
                }
                ");
    }

    [Fact]
    public void fragment_and_operation_named_the_same()
    {
        ShouldPassRule(@"
                query Foo {
                  ...Foo
                }

                fragment Foo on Type {
                  field
                }
                ");
    }

    [Fact]
    public void multiple_operations_of_same_name()
    {
        var query = @"
                query Foo {
                  fieldA
                }

                query Foo {
                  fieldB
                }
                ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(UniqueOperationNamesError.DuplicateOperationNameMessage("Foo"), 6, 17);
        });
    }

    [Fact]
    public void multiple_operations_of_same_name_of_different_types_mutation()
    {
        var query = @"
                query Foo {
                  fieldA
                }

                mutation Foo {
                  fieldB
                }
                ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(UniqueOperationNamesError.DuplicateOperationNameMessage("Foo"), 6, 17);
        });
    }

    [Fact]
    public void multiple_operations_of_same_name_of_different_types_subscription()
    {
        var query = @"
                query Foo {
                  fieldA
                }

                subscription Foo {
                  fieldB
                }
                ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(UniqueOperationNamesError.DuplicateOperationNameMessage("Foo"), 6, 17);
        });
    }
}
