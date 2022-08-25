using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

public class InputFieldsAndArgumentsOfCorrectLengthRequiredTests : ValidationTestBase<InputFieldsAndArgumentsOfCorrectLength, ValidationSchema>
{
    // SCALAR INPUT FIELD (LITERAL + VARIABLE TESTS)

    [Fact]
    public void good_literal_input_field_length()
    {
        ShouldPassRule(@"
            {
              complicatedArgs {
                complexArgField2(complexArg: { requiredField: true, stringField: ""aaaa"" })
              }
            }");
    }

    [Fact]
    public void good_variable_input_field_length()
    {
        ShouldPassRule(_ =>
        {
            _.Query = @"query q($value: String)
                {
                  complicatedArgs {
                    complexArgField2(complexArg: { requiredField: true, stringField: $value })
                  }
                }";
            _.Variables = new Dictionary<string, object> { ["value"] = "aaaa" }.ToInputs();
        });
    }

    [Fact]
    public void below_min_literal_input_field_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                {
                  complicatedArgs {
                    complexArgField2(complexArg: { requiredField: true, stringField: ""aa"" })
                  }
                }";
            _.Error(
               message: "ObjectField 'stringField' has invalid length (2). Length must be in range [3, 7].",
               line: 4,
               column: 73);
        });
    }

    [Fact]
    public void below_min_variable_input_field_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"query q($value: String)
                {
                  complicatedArgs {
                    complexArgField2(complexArg: { requiredField: true, stringField: $value })
                  }
                }";
            _.Error(
               message: "ObjectField 'stringField' has invalid length (2). Length must be in range [3, 7].",
               line: 4,
               column: 73);
            _.Variables = new Dictionary<string, object> { ["value"] = "aa" }.ToInputs();
        });
    }

    [Fact]
    public void above_max_literal_input_field_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                {
                  complicatedArgs {
                    complexArgField2(complexArg: { requiredField: true, stringField: ""aaaaaaaa"" })
                  }
                }";
            _.Error(
               message: "ObjectField 'stringField' has invalid length (8). Length must be in range [3, 7].",
               line: 4,
               column: 73);
        });
    }

    [Fact]
    public void above_max_variable_input_field_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"query q($value: String)
                {
                  complicatedArgs {
                    complexArgField2(complexArg: { requiredField: true, stringField: $value })
                  }
                }";
            _.Error(
               message: "ObjectField 'stringField' has invalid length (8). Length must be in range [3, 7].",
               line: 4,
               column: 73);
            _.Variables = new Dictionary<string, object> { ["value"] = "aaaaaaaa" }.ToInputs();
        });
    }

    [Fact]
    public void null_literal_input_field_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                {
                  complicatedArgs {
                    complexArgField2(complexArg: { requiredField: true, stringField: null })
                  }
                }";
            _.Error(
               message: "ObjectField 'stringField' has invalid length (null). Length must be in range [3, 7].",
               line: 4,
               column: 73);
        });
    }

    [Fact]
    public void null_variable_input_field_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"query q($value: String)
                {
                  complicatedArgs {
                    complexArgField2(complexArg: { requiredField: true, stringField: $value })
                  }
                }";
            _.Error(
               message: "ObjectField 'stringField' has invalid length (null). Length must be in range [3, 7].",
               line: 4,
               column: 73);
            _.Variables = new Dictionary<string, object> { ["value"] = null }.ToInputs();
        });
    }

    // COMPLEX INPUT FIELD (VARIABLE TESTS ONLY)

    [Fact]
    public void good_variable_input_complex_field_length()
    {
        ShouldPassRule(_ =>
        {
            _.Query = @"query q($complex: ComplexInput2!)
                {
                  complicatedArgs {
                    complexArgField2(complexArg: $complex)
                  }
                }";
            _.Variables = @"{ ""complex"": { ""requiredField"": true, ""stringField"": ""aaaa"" } }".ToInputs();
        });
    }

    [Fact]
    public void below_min_variable_input_complex_field_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"query q($complex: ComplexInput2!)
                {
                  complicatedArgs {
                    complexArgField2(complexArg: $complex)
                  }
                }";
            _.Error(
               message: "Variable 'complex.stringField' has invalid length (2). Length must be in range [3, 7].",
               line: 1,
               column: 9);
            _.Variables = @"{ ""complex"": { ""requiredField"": true, ""stringField"": ""aa"" } }".ToInputs();
        });
    }

    [Fact]
    public void above_max_variable_input_complex_field_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"query q($complex: ComplexInput2!)
                {
                  complicatedArgs {
                    complexArgField2(complexArg: $complex)
                  }
                }";
            _.Error(
               message: "Variable 'complex.stringField' has invalid length (8). Length must be in range [3, 7].",
               line: 1,
               column: 9);
            _.Variables = @"{ ""complex"": { ""requiredField"": true, ""stringField"": ""aaaaaaaa"" } }".ToInputs();
        });
    }

    [Fact]
    public void null_variable_input_complex_field_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"query q($complex: ComplexInput2!)
                {
                  complicatedArgs {
                    complexArgField2(complexArg: $complex)
                  }
                }";
            _.Error(
               message: "Variable '$complex.stringField' is invalid. Received a null input for a non-null variable.",
               line: 1,
               column: 9);
            _.Variables = @"{ ""complex"": { ""requiredField"": true, ""stringField"": null } }".ToInputs();
        });
    }

    // ARGUMENT (LITERAL + VARIABLE TESTS)

    [Fact]
    public void good_literal_argument_length()
    {
        ShouldPassRule(@"
            {
              human2(id: ""aaa"") {
                id
              }
            }");
    }

    [Fact]
    public void good_variable_argument_length()
    {
        ShouldPassRule(_ =>
        {
            _.Query = @"query q($value: String)
                {
                  human2(id: $value) {
                    id
                  }
                }";
            _.Variables = new Dictionary<string, object> { ["value"] = "aaa" }.ToInputs();
        });
    }

    [Fact]
    public void below_min_literal_argument_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                {
                  human2(id: ""a"") {
                    id
                  }
                }";
            _.Error(
               message: "Argument 'id' has invalid length (1). Length must be in range [2, 5].",
               line: 3,
               column: 26);
        });
    }

    [Fact]
    public void below_min_variable_argument_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"query q($value: String)
                {
                  human2(id: $value) {
                    id
                  }
                }";
            _.Error(
               message: "Argument 'id' has invalid length (1). Length must be in range [2, 5].",
               line: 3,
               column: 26);
            _.Variables = new Dictionary<string, object> { ["value"] = "a" }.ToInputs();
        });
    }

    [Fact]
    public void above_max_literal_argument_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                {
                  human2(id: ""aaaaaa"") {
                    id
                  }
                }";
            _.Error(
               message: "Argument 'id' has invalid length (6). Length must be in range [2, 5].",
               line: 3,
               column: 26);
        });
    }

    [Fact]
    public void above_max_variable_argument_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"query q($value: String)
                {
                  human2(id: $value) {
                    id
                  }
                }";
            _.Error(
               message: "Argument 'id' has invalid length (6). Length must be in range [2, 5].",
               line: 3,
               column: 26);
            _.Variables = new Dictionary<string, object> { ["value"] = "aaaaaa" }.ToInputs();
        });
    }

    [Fact]
    public void null_literal_argument_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"
                {
                  human2(id: null) {
                    id
                  }
                }";
            _.Error(
               message: "Argument 'id' has invalid length (null). Length must be in range [2, 5].",
               line: 3,
               column: 26);
        });
    }

    [Fact]
    public void null_variable_argument_length()
    {
        ShouldFailRule(_ =>
        {
            _.Query = @"query q($value: String)
                {
                  human2(id: $value) {
                    id
                  }
                }";
            _.Error(
               message: "Argument 'id' has invalid length (null). Length must be in range [2, 5].",
               line: 3,
               column: 26);
            _.Variables = new Dictionary<string, object> { ["value"] = null }.ToInputs();
        });
    }
}
