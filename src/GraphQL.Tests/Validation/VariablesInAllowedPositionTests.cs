using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class VariablesInAllowedPositionTests : ValidationTestBase<VariablesInAllowedPosition, ValidationSchema>
{
    [Fact]
    public void boolean_to_boolean()
    {
        ShouldPassRule(@"
                query Query($booleanArg: Boolean)
                {
                  complicatedArgs {
                    booleanArgField(booleanArg: $booleanArg)
                  }
                }
                ");
    }

    [Fact]
    public void boolean_to_boolean_within_fragment()
    {
        ShouldPassRule(@"
              fragment booleanArgFrag on ComplicatedArgs {
                booleanArgField(booleanArg: $booleanArg)
              }
              query Query($booleanArg: Boolean)
              {
                complicatedArgs {
                  ...booleanArgFrag
                }
              }
            ");

        ShouldPassRule(@"
              query Query($booleanArg: Boolean)
              {
                complicatedArgs {
                  ...booleanArgFrag
                }
              }
              fragment booleanArgFrag on ComplicatedArgs {
                booleanArgField(booleanArg: $booleanArg)
              }
            ");
    }

    [Fact]
    public void nonnull_boolean_to_boolean()
    {
        ShouldPassRule(@"
              query Query($nonNullBooleanArg: Boolean!)
              {
                complicatedArgs {
                  booleanArgField(booleanArg: $nonNullBooleanArg)
                }
              }
            ",
        "{ \"nonNullBooleanArg\": true }");
    }

    [Fact]
    public void nonnull_boolean_to_boolean_within_fragment()
    {
        ShouldPassRule(@"
              fragment booleanArgFrag on ComplicatedArgs {
                booleanArgField(booleanArg: $nonNullBooleanArg)
              }
              query Query($nonNullBooleanArg: Boolean!)
              {
                complicatedArgs {
                  ...booleanArgFrag
                }
              }
            ",
        "{ \"nonNullBooleanArg\": true }");
    }

    [Fact]
    public void int_to_int_with_default()
    {
        ShouldPassRule(@"
              query Query($intArg: Int = 1)
              {
                complicatedArgs {
                  nonNullIntArgField(nonNullIntArg: $intArg)
                }
              }
            ");
    }

    [Fact]
    public void string_list_to_string_list()
    {
        ShouldPassRule(@"
              query Query($stringListVar: [String])
              {
                complicatedArgs {
                  stringListArgField(stringListArg: $stringListVar)
                }
              }
            ");
    }

    [Fact]
    public void nonnull_string_list_to_string_list()
    {
        ShouldPassRule(@"
              query Query($stringListVar: [String!])
              {
                complicatedArgs {
                  stringListArgField(stringListArg: $stringListVar)
                }
              }
            ");
    }

    [Fact]
    public void string_to_string_list_in_item_position()
    {
        ShouldPassRule(@"
              query Query($stringVar: String)
              {
                complicatedArgs {
                  stringListArgField(stringListArg: [$stringVar])
                }
              }
            ");
    }

    [Fact]
    public void nonnull_string_to_string_list_in_item_position()
    {
        ShouldPassRule(@"
              query Query($stringVar: String!)
              {
                complicatedArgs {
                  stringListArgField(stringListArg: [$stringVar])
                }
              }
            ",
        "{ \"stringVar\": \"\" }");
    }

    [Fact]
    public void complexinput_to_complexinput()
    {
        ShouldPassRule(@"
              query Query($complexVar: ComplexInput)
              {
                complicatedArgs {
                  complexArgField(complexArg: $complexVar)
                }
              }
            ");
    }

    [Fact]
    public void complexinput_to_complexinput_in_field_position()
    {
        ShouldPassRule(@"
              query Query($boolVar: Boolean = false)
              {
                complicatedArgs {
                  complexArgField(complexArg: {requiredArg: $boolVar})
                }
              }
            ");
    }

    [Fact]
    public void nullable_boolean_to_nullable_boolean_in_directive()
    {
        ShouldPassRule(@"
              query Query($boolVar: Boolean!)
              {
                dog @include(if: $boolVar)
              }
            ",
        "{ \"boolVar\": true }");
    }

    [Fact]
    public void boolean_to_nullable_boolean_in_directive_with_default()
    {
        ShouldPassRule(@"
              query Query($boolVar: Boolean = false)
              {
                dog @include(if: $boolVar)
              }
            ");
    }

    [Fact]
    public void int_to_nonnull_int()
    {
        var query = @"
              query Query($intArg: Int) {
                complicatedArgs {
                  nonNullIntArgField(nonNullIntArg: $intArg)
                }
              }
            ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(err =>
            {
                err.Message = BadVarPosMessage("intArg", "Int", "Int!");
                err.Loc(2, 27);
                err.Loc(4, 53);
            });
        });
    }

    [Fact]
    public void int_to_nonnull_int_within_fragment()
    {
        var query = @"
              fragment nonNullIntArgFieldFrag on ComplicatedArgs {
                nonNullIntArgField(nonNullIntArg: $intArg)
              }

              query Query($intArg: Int) {
                complicatedArgs {
                  ...nonNullIntArgFieldFrag
                }
              }
            ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(err =>
            {
                err.Message = BadVarPosMessage("intArg", "Int", "Int!");
                err.Loc(6, 27);
                err.Loc(3, 51);
            });
        });
    }

    [Fact]
    public void int_to_nonnull_int_within_nested_fragment()
    {
        var query = @"
              fragment outerFrag on ComplicatedArgs {
                ...nonNullIntArgFieldFrag
              }

              fragment nonNullIntArgFieldFrag on ComplicatedArgs {
                nonNullIntArgField(nonNullIntArg: $intArg)
              }

              query Query($intArg: Int) {
                complicatedArgs {
                  ...outerFrag
                }
              }
            ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(err =>
            {
                err.Message = BadVarPosMessage("intArg", "Int", "Int!");
                err.Loc(10, 27);
                err.Loc(7, 51);
            });
        });
    }

    [Fact]
    public void string_over_boolean()
    {
        var query = @"
              query Query($stringVar: String) {
                complicatedArgs {
                  booleanArgField(booleanArg: $stringVar)
                }
              }
            ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(err =>
            {
                err.Message = BadVarPosMessage("stringVar", "String", "Boolean");
                err.Loc(2, 27);
                err.Loc(4, 47);
            });
        });
    }

    [Fact]
    public void string_to_string_list()
    {
        var query = @"
              query Query($stringVar: String) {
                complicatedArgs {
                  stringListArgField(stringListArg: $stringVar)
                }
              }
            ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(err =>
            {
                err.Message = BadVarPosMessage("stringVar", "String", "[String]");
                err.Loc(2, 27);
                err.Loc(4, 53);
            });
        });
    }

    [Fact]
    public void boolean_to_nonnull_boolean_in_directive()
    {
        var query = @"
              query Query($boolVar: Boolean) {
                dog @include(if: $boolVar)
              }
            ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(err =>
            {
                err.Message = BadVarPosMessage("boolVar", "Boolean", "Boolean!");
                err.Loc(2, 27);
                err.Loc(3, 34);
            });
        });
    }

    [Fact]
    public void string_to_nonnull_boolean_in_directive()
    {
        var query = @"
              query Query($stringVar: String) {
                dog @include(if: $stringVar)
              }
            ";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            _.Error(err =>
            {
                err.Message = BadVarPosMessage("stringVar", "String", "Boolean!");
                err.Loc(2, 27);
                err.Loc(3, 34);
            });
        });
    }

    private static string BadVarPosMessage(string varName, string varType, string expectedType)
        => VariablesInAllowedPositionError.BadVarPosMessage(varName, varType, expectedType);
}
