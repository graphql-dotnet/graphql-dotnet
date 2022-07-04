using GraphQL.Validation.Errors;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation;

[Collection("StaticTests")]
public class ArgumentsOfCorrectTypeTests : ValidationTestBase<ArgumentsOfCorrectType, ValidationSchema>
{
    [Fact]
    public void good_int_value()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                intArgField(intArg: 2)
              }
            }");
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2339
    [Fact]
    public void good_int_null_value()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                intArgField(intArg: null)
              }
            }");
    }

    [Fact]
    public void good_boolean_value()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                booleanArgField(booleanArg: true)
              }
            }");
    }

    [Fact]
    public void good_string_value()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                stringArgField(stringArg: ""foo"")
              }
            }");
    }

    [Fact]
    public void good_float_value()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                floatArgField(floatArg: 1.1)
              }
            }");
    }

    [Fact]
    public void int_into_float()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                floatArgField(floatArg: 1)
              }
            }");
    }

    [Fact]
    public void long_into_float()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                floatArgField(floatArg: 1000000000000000001)
              }
            }");
    }

    [Fact]
    public void int_into_id()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                idArgField(idArg: 1)
              }
            }");
    }

    [Fact]
    public void long_into_id()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                idArgField(idArg: 1000000000000000001)
              }
            }");
    }

    [Fact]
    public void string_into_id()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                idArgField(idArg: ""someIdString"")
              }
            }");
    }

    [Fact]
    public void good_enum_value()
    {
        ShouldPassRule(@"{
              dog {
                doesKnowCommand(dogCommand: SIT)
              }
            }");
    }

    [Fact]
    public void int_into_string()
    {
        var query = @"{
              complicatedArgs {
                stringArgField(stringArg: 1)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "stringArg", "String", "1", 3, 32);
        });
    }

    [Fact]
    public void float_into_string_using_cultures()
    {
        CultureTestHelper.UseCultures(float_into_string);
    }

    [Fact]
    public void float_into_string()
    {
        var query = @"{
              complicatedArgs {
                stringArgField(stringArg: 1.0)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "stringArg", "String", "1.0", 3, 32);
        });
    }

    [Fact]
    public void boolean_into_string()
    {
        var query = @"{
              complicatedArgs {
                stringArgField(stringArg: true)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "stringArg", "String", "true", 3, 32);
        });
    }

    [Fact]
    public void unquotedstring_into_string()
    {
        var query = @"{
              complicatedArgs {
                stringArgField(stringArg: BAR)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "stringArg", "String", "BAR", 3, 32);
        });
    }

    [Fact]
    public void string_into_int()
    {
        var query = @"{
              complicatedArgs {
                intArgField(intArg: ""3"")
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "intArg", "Int", "\"3\"", 3, 29);
        });
    }

    //        [Test]
    //        public void big_int_into_int()
    //        {
    //            var query = @"{
    //              complicatedArgs {
    //                intArgField(intArg: 829384293849283498239482938)
    //              }
    //            }";
    //
    //            ShouldFailRule(_ =>
    //            {
    //                _.Query = query;
    //                Rule.badValue(_, "intArg", "Int", "829384293849283498239482938", 3, 29);
    //            });
    //        }

    [Fact]
    public void unquoted_string_into_int()
    {
        var query = @"{
              complicatedArgs {
                intArgField(intArg: FOO)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "intArg", "Int", "FOO", 3, 29);
        });
    }

    [Fact]
    public void simple_float_into_int()
    {
        var query = @"{
              complicatedArgs {
                intArgField(intArg: 3.0)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "intArg", "Int", "3.0", 3, 29);
        });
    }

    [Fact]
    public void float_into_int()
    {
        var query = @"{
              complicatedArgs {
                intArgField(intArg: 3.333)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "intArg", "Int", "3.333", 3, 29);
        });
    }

    [Fact]
    public void string_into_float()
    {
        var query = @"{
              complicatedArgs {
                floatArgField(floatArg: ""3.333"")
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "floatArg", "Float", "\"3.333\"", 3, 31);
        });
    }

    [Fact]
    public void boolean_into_float()
    {
        var query = @"{
              complicatedArgs {
                floatArgField(floatArg: true)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "floatArg", "Float", "true", 3, 31);
        });
    }

    [Fact]
    public void unquoted_string_into_float()
    {
        var query = @"{
              complicatedArgs {
                floatArgField(floatArg: FOO)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "floatArg", "Float", "FOO", 3, 31);
        });
    }

    [Fact]
    public void int_into_boolean()
    {
        var query = @"{
              complicatedArgs {
                booleanArgField(booleanArg: 2)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "booleanArg", "Boolean", "2", 3, 33);
        });
    }

    [Fact]
    public void float_into_boolean()
    {
        var query = @"{
              complicatedArgs {
                booleanArgField(booleanArg: 1.0)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "booleanArg", "Boolean", "1.0", 3, 33);
        });
    }

    [Fact]
    public void string_into_boolean()
    {
        var query = @"{
              complicatedArgs {
                booleanArgField(booleanArg: ""true"")
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "booleanArg", "Boolean", "\"true\"", 3, 33);
        });
    }

    [Fact]
    public void unquotedstring_into_boolean()
    {
        var query = @"{
              complicatedArgs {
                booleanArgField(booleanArg: TRUE)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "booleanArg", "Boolean", "TRUE", 3, 33);
        });
    }

    [Fact]
    public void float_into_id()
    {
        var query = @"{
              complicatedArgs {
                idArgField(idArg: 1.0)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "idArg", "ID", "1.0", 3, 28);
        });
    }

    [Fact]
    public void boolean_into_id()
    {
        var query = @"{
              complicatedArgs {
                idArgField(idArg: true)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "idArg", "ID", "true", 3, 28);
        });
    }

    [Fact]
    public void unquoted_into_id()
    {
        var query = @"{
              complicatedArgs {
                idArgField(idArg: SOMETHING)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "idArg", "ID", "SOMETHING", 3, 28);
        });
    }

    [Fact]
    public void int_into_enum()
    {
        var query = @"{
              dog {
                doesKnowCommand(dogCommand: 2)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "dogCommand", "DogCommand", "2", 3, 33);
        });
    }

    [Fact]
    public void float_into_enum_with_cultures()
    {
        CultureTestHelper.UseCultures(float_into_enum);
    }

    [Fact]
    public void float_into_enum()
    {
        var query = @"{
              dog {
                doesKnowCommand(dogCommand: 1.0)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "dogCommand", "DogCommand", "1.0", 3, 33);
        });
    }

    [Fact]
    public void string_into_enum()
    {
        var query = @"{
              dog {
                doesKnowCommand(dogCommand: ""SIT"")
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "dogCommand", "DogCommand", "\"SIT\"", 3, 33);
        });
    }

    [Fact]
    public void boolean_into_enum()
    {
        var query = @"{
              dog {
                doesKnowCommand(dogCommand: true)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "dogCommand", "DogCommand", "true", 3, 33);
        });
    }

    [Fact]
    public void unknown_enum_value_into_enum()
    {
        var query = @"{
              dog {
                doesKnowCommand(dogCommand: JUGGLE)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "dogCommand", "DogCommand", "JUGGLE", 3, 33);
        });
    }

    // this is currently allowed
    //        [Fact]
    //        public void different_case_enum_value_into_enum()
    //        {
    //            var query = @"{
    //              dog {
    //                doesKnowCommand(dogCommand: sit)
    //              }
    //            }";
    //
    //            ShouldFailRule(_ =>
    //            {
    //                _.Query = query;
    //                Rule.badValue(_, "dogCommand", "DogCommand", "sit", 3, 33);
    //            });
    //        }

    [Fact]
    public void list_value_incorrect_item_type()
    {
        var query = @"{
              complicatedArgs {
                stringListArgField(stringListArg: [""one"", 2])
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "stringListArg", "[String]", "[\"one\", 2]", 3, 36, "In element #2: [Expected type 'String', found 2.]");
        });
    }

    [Fact]
    public void list_value_single_value_of_incorrect_type()
    {
        var query = @"{
              complicatedArgs {
                stringListArgField(stringListArg: 1)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "stringListArg", "String", "1", 3, 36);
        });
    }
}

public class ArgumentsOfCorrectType_Valid_Non_Nullable : ValidationTestBase<ArgumentsOfCorrectType, ValidationSchema>
{
    [Fact]
    public void arg_on_optional_arg()
    {
        ShouldPassRule(@"{
              dog {
                isHousetrained(atOtherHomes: true)
              }
            }");
    }

    [Fact]
    public void no_arg_on_optional_arg()
    {
        ShouldPassRule(@"{
              dog {
                isHousetrained
              }
            }");
    }

    [Fact]
    public void multiple_args()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleReqs(req1: 1, req2: 2)
              }
            }");
    }

    [Fact]
    public void multiple_args_reverse_order()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleReqs(req2: 2, req1: 1)
              }
            }");
    }

    [Fact]
    public void no_args_on_multiple_optional()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleOpts
              }
            }");
    }

    [Fact]
    public void one_arg_on_multiple_optional()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleOpts(opt1: 1)
              }
            }");
    }

    [Fact]
    public void second_arg_on_multiple_optional()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleOpts(opt2: 2)
              }
            }");
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2339
    [Fact]
    public void one_null_arg_on_multiple_optional()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleOpts(opt1: null)
              }
            }");
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2339
    [Fact]
    public void both_null_arg_on_multiple_optional()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleOpts(opt2: null, opt1: null)
              }
            }");
    }

    [Fact]
    public void multiple_reqs_on_mixed()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleOptAndReq(req1: 3, req2: 4)
              }
            }");
    }

    [Fact]
    public void multiple_reqs_and_one_opt_on_mixed()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleOptAndReq(req1: 3, req2: 4, opt1: 5)
              }
            }");
    }

    [Fact]
    public void all_reqs_and_opts_on_mixed()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                multipleOptAndReq(req1: 3, req2: 4, opt1: 5, opt2: 6)
              }
            }");
    }
}

public class ArgumentsOfCorrectType_Invalid_Non_Nullable : ValidationTestBase<ArgumentsOfCorrectType, ValidationSchema>
{
    [Fact]
    public void incorrect_value_type()
    {
        var query = @"{
              complicatedArgs {
                multipleReqs(req2: ""two"", req1: ""one"")
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "req2", "Int", "\"two\"", 3, 30);
            Rule.badValue(_, "req1", "Int", "\"one\"", 3, 43);
        });
    }

    [Fact]
    public void incorrect_value_and_missing_argument()
    {
        var query = @"{
              complicatedArgs {
                multipleReqs(req1: ""one"")
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "req1", "Int", "\"one\"", 3, 30);
        });
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2339
    [Fact]
    public void multiple_args_with_one_null()
    {
        var query = @"{
              complicatedArgs {
                multipleReqs(req1: null)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "req1", "Int", "null", 3, 30, "Expected 'Int!', found null.");
        });
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2339
    [Fact]
    public void multiple_args_with_second_null()
    {
        var query = @"{
              complicatedArgs {
                multipleReqs(req2: null)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "req2", "Int", "null", 3, 30, "Expected 'Int!', found null.");
        });
    }

    // https://github.com/graphql-dotnet/graphql-dotnet/issues/2339
    [Fact]
    public void multiple_args_with_both_null()
    {
        var query = @"{
              complicatedArgs {
                multipleReqs(req2: null, req1: null)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "req2", "Int", "null", 3, 30, "Expected 'Int!', found null.");
            Rule.badValue(_, "req1", "Int", "null", 3, 42, "Expected 'Int!', found null.");
        });
    }
}

public class ArgumentsOfCorrectType_valid_input_object : ValidationTestBase<ArgumentsOfCorrectType, ValidationSchema>
{
    [Fact]
    public void optional_arg_despite_required_field_in_type()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                complexArgField
              }
            }");
    }

    [Fact]
    public void partial_object_only_required()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                complexArgField(complexArg: { requiredField: true })
              }
            }");
    }

    [Fact]
    public void partial_object_required_field_can_be_false()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                complexArgField(complexArg: { requiredField: false })
              }
            }");
    }

    [Fact]
    public void partial_object_including_required()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                complexArgField(complexArg: { requiredField: true, intField: 4 })
              }
            }");
    }

    [Fact]
    public void full_object()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                complexArgField(complexArg: {
                  requiredField: true,
                  intField: 4,
                  stringField: ""foo"",
                  booleanField: false,
                  stringListField: [""one"", ""two""]
                })
              }
            }");
    }

    [Fact]
    public void full_object_with_fields_in_different_order()
    {
        ShouldPassRule(@"{
              complicatedArgs {
                complexArgField(complexArg: {
                  stringListField: [""one"", ""two""],
                  booleanField: false,
                  requiredField: true,
                  stringField: ""foo""
                  intField: 4,
                })
              }
            }");
    }
}

public class ArgumentsOfCorrectType_invalid_input_object_value : ValidationTestBase<ArgumentsOfCorrectType, ValidationSchema>
{
    [Fact]
    public void partial_object_missing_required()
    {
        var query = @"{
              complicatedArgs {
                complexArgField(complexArg: { intField: 4 })
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "complexArg", "ComplexInput", "{intField: 4}", 3, 33, "Missing required field 'requiredField' of type 'Boolean'.");
        });
    }

    [Fact]
    public void partial_object_invalid_field_type()
    {
        var query = @"{
              complicatedArgs {
                complexArgField(complexArg: {
                  stringListField: [""one"", 2],
                  requiredField: true,
                })
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "complexArg", "ComplexInput", "{stringListField: [\"one\", 2], requiredField: true}", 3, 33, "In field 'stringListField': [In element #2: [Expected type 'String', found 2.]]");
        });
    }

    [Fact]
    public void partial_object_unknown_field_arg()
    {
        var query = @"{
              complicatedArgs {
                complexArgField(complexArg: {
                  requiredField: true,
                  unknownField: ""value""
                })
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "complexArg", "ComplexInput", "{requiredField: true, unknownField: \"value\"}", 3, 33, "In field 'unknownField': Unknown field.");
        });
    }
}

public class ArgumentsOfCorrectType_directive_arguments : ValidationTestBase<ArgumentsOfCorrectType, ValidationSchema>
{
    [Fact]
    public void with_directives_of_valid_types()
    {
        ShouldPassRule(@"{
              dog @include(if: true) {
                name
              }
              human @skip(if: false) {
                name
              }
            }");
    }

    [Fact]
    public void with_directives_with_incorrect_types()
    {
        var query = @"{
              dog @include(if: ""yes"") {
                name @skip(if: ENUM)
              }
            }";

        ShouldFailRule(_ =>
        {
            _.Query = query;
            Rule.badValue(_, "if", "Boolean", "\"yes\"", 2, 28);
            Rule.badValue(_, "if", "Boolean", "ENUM", 3, 28);
        });
    }
}

public static class ValidationExtensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "'rule' parameter works as 'this' anchor")]
    public static void badValue(
        this ArgumentsOfCorrectType rule,
        ValidationTestConfig config,
        string argName,
        string typeName,
        string value,
        int line,
        int column,
        string errors = null)
    {
        errors ??= $"Expected type '{typeName}', found {value}.";

        config.Error(
            ArgumentsOfCorrectTypeError.BadValueMessage(argName, errors),
            line,
            column);
    }
}
