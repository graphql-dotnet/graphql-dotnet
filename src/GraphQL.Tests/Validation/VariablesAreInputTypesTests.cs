using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class VariablesAreInputTypesTests : ValidationTestBase<VariablesAreInputTypes, ValidationSchema>
{
    [Fact]
    public void input_types_are_valid()
    {
        ShouldPassRule(@"
              query Foo($a: String, $b: [Boolean!]!, $c: ComplexInput) {
                field(a: $a, b: $b, c: $c)
              }
            ",
        "{ \"b\": [true] }");
    }

    [Fact]
    public void output_types_are_invalid()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                  query Foo($a: Dog, $b: [[CatOrDog!]]!, $c: Pet) {
                    field(a: $a, b: $b, c: $c)
                  }
                ";
            _.Error(
                message: VariablesAreInputTypesError.UndefinedVarMessage("a", "Dog"),
                line: 2,
                column: 29);
            _.Error(
                message: VariablesAreInputTypesError.UndefinedVarMessage("b", "[[CatOrDog!]]!"),
                line: 2,
                column: 38);
            _.Error(
                message: VariablesAreInputTypesError.UndefinedVarMessage("c", "Pet"),
                line: 2,
                column: 58);
            _.Error(
               message: "Variable '$b' is invalid. No value provided for a non-null variable.",
               line: 2,
               column: 38);
        });
    }
}
