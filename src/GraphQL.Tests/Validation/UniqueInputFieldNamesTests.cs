using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class UniqueInputFieldNamesTests : ValidationTestBase<UniqueInputFieldNames, ValidationSchema>
{
    [Fact]
    public void oneOf_input_types_contain_one_field()
    {
        ShouldPassRule("""
            {
              complicatedArgs {
                complexArgField3(complexArg: { intField: 2 })
              }
            }
            """);
    }

    [Fact]
    public void oneOf_input_types_containing_multiple_fields()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                {
                  complicatedArgs {
                    complexArgField3(complexArg: { intField: 2, booleanField: true })
                  }
                }
                """;
            _.Error(x =>
            {
                x.Message = OneOfInputValuesError.MULTIPLE_VALUES;
                x.Loc(3, 34);
            });
        });
    }

    [Fact]
    public void oneOf_input_types_containing_null_values()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                {
                  complicatedArgs {
                    complexArgField3(complexArg: { intField: null })
                  }
                }
                """;
            _.Error(x =>
            {
                x.Message = OneOfInputValuesError.MULTIPLE_VALUES;
                x.Loc(3, 36);
                x.Loc(3, 46);
            });
        });
    }

    [Fact]
    public void input_object_with_fields()
    {
        ShouldPassRule("""
            {
              field(arg: { f: true })
            }
            """);
    }

    [Fact]
    public void same_input_object_within_two_args()
    {
        ShouldPassRule("""
            {
              field(arg1: { f: true }, arg2: { f: true })
            }
            """);
    }

    [Fact]
    public void multiple_input_object_fields()
    {
        ShouldPassRule("""
            {
              field(arg: { f1: "value", f2: "value", f3: "value" })
            }
            """);
    }

    [Fact]
    public void allows_for_nested_input_object_with_similar_fields()
    {
        ShouldPassRule("""
            {
              field(arg: {
                deep: {
                  deep: {
                    id: 1
                  }
                  id: 1
                }
                id: 1
              })
            }
            """);
    }

    [Fact]
    public void duplicate_input_object_fields()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                {
                  field(arg: { f1: "value", f1: "value" })
                }
                """;
            _.Error(x =>
            {
                x.Message = UniqueInputFieldNamesError.DuplicateInputField("f1");
                x.Loc(2, 20);
                x.Loc(2, 33);
            });
        });
    }

    [Fact]
    public void many_duplicate_input_object_fields()
    {
        ShouldFailRule(_ =>
        {
            _.Query = """
                {
                  field(arg: { f1: "value", f1: "value", f1: "value" })
                }
                """;
            _.Error(x =>
            {
                x.Message = UniqueInputFieldNamesError.DuplicateInputField("f1");
                x.Loc(2, 20);
                x.Loc(2, 33);
            });
            _.Error(x =>
            {
                x.Message = UniqueInputFieldNamesError.DuplicateInputField("f1");
                x.Loc(2, 20);
                x.Loc(2, 46);
            });
        });
    }
}
