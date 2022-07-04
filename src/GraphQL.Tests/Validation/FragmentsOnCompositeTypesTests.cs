using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class FragmentsOnCompositeTypesTests : ValidationTestBase<FragmentsOnCompositeTypes, ValidationSchema>
{
    [Fact]
    public void object_is_valid_fragment_type()
    {
        ShouldPassRule(@"
              fragment validFragment on Dog {
                barks
              }
            ");
    }

    [Fact]
    public void interface_is_valid_fragment_type()
    {
        ShouldPassRule(@"
              fragment validFragment on Pet {
                name
              }
            ");
    }

    [Fact]
    public void object_is_valid_inline_fragment_type()
    {
        ShouldPassRule(@"
              fragment validFragment on Pet {
                ... on Dog {
                  barks
                }
              }
            ");
    }

    [Fact]
    public void inline_fragment_without_type_is_valid()
    {
        ShouldPassRule(@"
              fragment validFragment on Pet {
                ... {
                  name
                }
              }
            ");
    }

    [Fact]
    public void union_is_valid_fragment_type()
    {
        ShouldPassRule(@"
              fragment validFragment on CatOrDog {
                __typename
              }
            ");
    }

    [Fact]
    public void scalar_is_invalid_fragment_type()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment scalarFragment on Boolean {
                    bad
                  }
                ";
            error(_, "scalarFragment", "Boolean", 2, 46);
        });
    }

    [Fact]
    public void enum_is_invalid_fragment_type()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment scalarFragment on FurColor {
                    bad
                  }
                ";
            error(_, "scalarFragment", "FurColor", 2, 46);
        });
    }

    [Fact]
    public void input_object_is_invalid_fragment_type()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment inputFragment on ComplexInput {
                    stringField
                  }
                ";
            error(_, "inputFragment", "ComplexInput", 2, 45);
        });
    }

    [Fact]
    public void scalar_is_invalid_inline_fragment_type()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  fragment invalidFragment on Pet {
                    ... on String {
                      barks
                    }
                  }
                ";
            _.Error(FragmentsOnCompositeTypesError.InlineFragmentOnNonCompositeErrorMessage("String"), 3, 28);
        });
    }

    private void error(ValidationTestConfig _, string fragName, string typeName, int line, int column)
    {
        _.Error(FragmentsOnCompositeTypesError.FragmentOnNonCompositeErrorMessage(fragName, typeName), line, column);
    }
}
