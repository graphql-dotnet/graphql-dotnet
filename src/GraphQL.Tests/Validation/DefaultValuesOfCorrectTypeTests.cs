using GraphQL.Validation.Rules;
using Xunit;

namespace GraphQL.Tests.Validation
{
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
            ");
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
        public void no_required_variables_with_default_values()
        {
            ShouldFailRule(_ =>
            {
                _.Query = @"
                    query UnreachableDefaultValues($a: Int! = 3, $b: String! = ""default"") {
                      dog { name }
                    }";

                _.Error(Rule.BadValueForNonNullArgMessage("a", "Int!", "Int"), 2, 63);
                _.Error(Rule.BadValueForNonNullArgMessage("b", "String!", "String"), 2, 80);
            });
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

                _.Error(Rule.BadValueForDefaultArgMessage("a", "Int", "\"one\"", new []{"Expected type \"Int\", found \"one\"."}), 3, 35);
                _.Error(Rule.BadValueForDefaultArgMessage("b", "String", "4", new []{"Expected type \"String\", found 4."}), 4, 38);
                _.Error(Rule.BadValueForDefaultArgMessage("c", "ComplexInput", "\"notverycomplex\"", new []{"Expected \"ComplexInput\", found not an object."}), 5, 44);
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

                _.Error(Rule.BadValueForDefaultArgMessage("a", "ComplexInput", "{intField: 3}", new []{"In field \"requiredField\": Expected \"Boolean!\", found null."}), 2, 67);
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
                _.Error(Rule.BadValueForDefaultArgMessage("a", "[String]", "[\"one\", 2]", new[] { "In element #1: Expected type \"String\", found 2." }), 2, 54);
            });
        }
    }
}
