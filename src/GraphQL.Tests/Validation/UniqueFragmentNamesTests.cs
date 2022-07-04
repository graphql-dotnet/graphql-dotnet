using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class UniqueFragmentNamesTests : ValidationTestBase<UniqueFragmentNames, ValidationSchema>
{
    [Fact]
    public void no_fragments()
    {
        ShouldPassRule(@"
        {
          field
        }
      ");
    }

    [Fact]
    public void one_fragment()
    {
        ShouldPassRule(@"
        {
          ...fragA
        }
        fragment fragA on Type {
          field
        }
      ");
    }

    [Fact]
    public void many_fragments()
    {
        ShouldPassRule(@"
        {
          ...fragA
          ...fragB
          ...fragC
        }
        fragment fragA on Type {
          fieldA
        }
        fragment fragB on Type {
          fieldB
        }
        fragment fragC on Type {
          fieldC
        }
      ");
    }

    [Fact]
    public void inline_fragments_are_always_unique()
    {
        ShouldPassRule(@"
        {
          ...on Type {
            fieldA
          }
          ...on Type {
            fieldB
          }
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
    public void fragments_named_the_same()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          {
            ...fragA
          }
          fragment fragA on Type {
            fieldA
          }
          fragment fragA on Type {
            fieldB
          }
        ";
            // Note: this is failing on "fragment"; graphql-js fails on the fragment name.
            duplicateFrag(_, "fragA", 5, 11, 8, 11);
        });
    }

    [Fact]
    public void fragments_named_the_same_without_being_referenced()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
          fragment fragA on Type {
            fieldA
          }
          fragment fragA on Type {
            fieldB
          }
        ";
            // Note: this is failing on "fragment"; graphql-js fails on the fragment name.
            duplicateFrag(_, "fragA", 2, 11, 5, 11);
        });
    }

    private static void duplicateFrag(
      ValidationTestConfig _,
      string fragName,
      int line1,
      int column1,
      int line2,
      int column2)
    {
        _.Error(err =>
        {
            err.Message = UniqueFragmentNamesError.DuplicateFragmentNameMessage(fragName);
            err.Loc(line1, column1);
            err.Loc(line2, column2);
        });
    }
}
