using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class LoneAnonymousOperationTests : ValidationTestBase<LoneAnonymousOperation, ValidationSchema>
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
    public void one_anon_with_fragment()
    {
        ShouldPassRule(@"
                {
                  ...Foo
                }

                fragment Foo on Type {
                  field
                }
                ");
    }

    [Fact]
    public void multiple_anon_operations()
    {
        var query = @"
                {
                  fieldA
                }

                {
                  fieldB
                }
                ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(LoneAnonymousOperationError.AnonOperationNotAloneMessage(), 2, 17);
            _.Error(LoneAnonymousOperationError.AnonOperationNotAloneMessage(), 6, 17);
        });
    }

    [Fact]
    public void anon_operation_with_mutation()
    {
        var query = @"
                {
                  fieldA
                }

                mutation Foo {
                  fieldB
                }
                ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(LoneAnonymousOperationError.AnonOperationNotAloneMessage(), 2, 17);
        });
    }

    [Fact]
    public void anon_operation_with_subscription()
    {
        var query = @"
                {
                  fieldA
                }

                subscription Foo {
                  fieldB
                }
                ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(LoneAnonymousOperationError.AnonOperationNotAloneMessage(), 2, 17);
        });
    }
}
