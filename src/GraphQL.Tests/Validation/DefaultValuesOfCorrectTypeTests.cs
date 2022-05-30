using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class DefaultValuesOfCorrectTypeTests : ValidationTestBase<DefaultValuesOfCorrectType, ValidationSchema>
{
    [Fact]
    public void variables_with_no_default_values()
    {
        ShouldPassRule(@"
              query NullableValues($a: Int, $b: String, $c: ComplexInput) {
                dog { name }
              }
            ");
    }

    [Fact]
    public void required_variables_without_default_values()
    {
        ShouldPassRule(@"
              query RequiredValues($a: Int!, $b: String!) {
                dog { name }
              }
            ",
        "{ \"a\": 1, \"b\": \"\" }");
    }

    [Fact]
    public void variables_with_valid_default_values()
    {
        ShouldPassRule(@"
              query WithDefaultValues(
                $a: Int = 1,
                $b: String = ""ok"",
                $c: ComplexInput = { requiredField: true, intField: 3 }
              ) {
                  dog { name }
                }
            ");
    }

    [Fact]
    public void allows_required_variables_with_default_values()
    {
        ShouldPassRule(@"
                query UnreachableDefaultValues($a: Int! = 3, $b: String! = ""default"") {
                    dog { name }
                }");
    }

    [Fact]
    public void variables_with_invalid_default_values()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                    query InvalidDefaultValues(
                        $a: Int = ""one"",
                        $b: String = 4,
                        $c: ComplexInput = ""notverycomplex""
                    ) {
                      dog { name }
                    }";

            _.Error(BadValueForDefaultArgMessage("a", "Int", "\"one\"", "Expected type 'Int', found \"one\"."), 3, 35);
            _.Error(BadValueForDefaultArgMessage("b", "String", "4", "Expected type 'String', found 4."), 4, 38);
            _.Error(BadValueForDefaultArgMessage("c", "ComplexInput", "\"notverycomplex\"", "Expected 'ComplexInput', found not an object."), 5, 44);
            _.Error("Variable '$a' is invalid. Error coercing default value.", 3, 25);
            _.Error("Variable '$b' is invalid. Error coercing default value.", 4, 25);
            _.Error("Variable '$c' is invalid. Error coercing default value.", 5, 25);
        });
    }

    [Fact]
    public void complex_variables_missing_required_field()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                    query MissingRequiredField($a: ComplexInput = {intField: 3}) {
                      dog { name }
                    }";

            _.Error(BadValueForDefaultArgMessage("a", "ComplexInput", "{intField: 3}", "Missing required field 'requiredField' of type 'Boolean'."), 2, 67);
        });
    }

    [Fact]
    public void list_variables_with_invalid_item()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                    query InvalidItem($a: [String] = [""one"", 2]) {
                      dog { name }
                    }";
            _.Error(BadValueForDefaultArgMessage("a", "[String]", "[\"one\", 2]", "In element #2: [Expected type 'String', found 2.]"), 2, 54);
            _.Error("Variable '$a' is invalid. Error coercing default value.", 2, 39);
        });
    }

    private static string BadValueForDefaultArgMessage(string varName, string type, string value, string verboseErrors)
        => DefaultValuesOfCorrectTypeError.BadValueForDefaultArgMessage(varName, type, value, verboseErrors);
}
