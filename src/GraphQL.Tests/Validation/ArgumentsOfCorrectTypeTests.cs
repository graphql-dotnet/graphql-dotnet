using System.Collections.Generic;
using GraphQL.Validation.Rules;

namespace GraphQL.Tests.Validation
{
    public class ArgumentsOfCorrectTypeTests : ValidationTestBase<ValidationSchema>
    {
        private readonly ArgumentsOfCorrectType _rule = new ArgumentsOfCorrectType();

        [Test]
        public void good_int_value()
        {
            var query = @"{
              complicatedArgs {
                intArgField(intArg: 2)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void good_boolean_value()
        {
            var query = @"{
              complicatedArgs {
                booleanArgField(booleanArg: true)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void good_string_value()
        {
            var query = @"{
              complicatedArgs {
                stringArgField(stringArg: ""foo"")
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void good_float_value()
        {
            var query = @"{
              complicatedArgs {
                floatArgField(floatArg: 1.1)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void int_into_float()
        {
            var query = @"{
              complicatedArgs {
                floatArgField(floatArg: 1)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void long_into_float()
        {
            var query = @"{
              complicatedArgs {
                floatArgField(floatArg: 1000000000000000001)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void int_into_id()
        {
            var query = @"{
              complicatedArgs {
                idArgField(idArg: 1)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void long_into_id()
        {
            var query = @"{
              complicatedArgs {
                idArgField(idArg: 1000000000000000001)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void string_into_id()
        {
            var query = @"{
              complicatedArgs {
                idArgField(idArg: ""someIdString"")
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void good_enum_value()
        {
            var query = @"{
              dog {
                doesKnowCommand(dogCommand: SIT)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "stringArg", "String", "1", 3, 32);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "stringArg", "String", "1.0", 3, 32);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "stringArg", "String", "true", 3, 32);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "stringArg", "String", "BAR", 3, 32);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "intArg", "Int", "\"3\"", 3, 29);
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
//                _.Rule(_rule);
//                _rule.badValue(_, "intArg", "Int", "829384293849283498239482938", 3, 29);
//            });
//        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "intArg", "Int", "FOO", 3, 29);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "intArg", "Int", "3.0", 3, 29);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "intArg", "Int", "3.333", 3, 29);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "floatArg", "Float", "\"3.333\"", 3, 31);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "floatArg", "Float", "true", 3, 31);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "floatArg", "Float", "FOO", 3, 31);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "booleanArg", "Boolean", "2", 3, 33);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "booleanArg", "Boolean", "1.0", 3, 33);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "booleanArg", "Boolean", "\"true\"", 3, 33);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "booleanArg", "Boolean", "TRUE", 3, 33);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "idArg", "ID", "1.0", 3, 28);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "idArg", "ID", "true", 3, 28);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "idArg", "ID", "SOMETHING", 3, 28);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "dogCommand", "DogCommand", "2", 3, 33);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "dogCommand", "DogCommand", "1.0", 3, 33);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "dogCommand", "DogCommand", "\"SIT\"", 3, 33);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "dogCommand", "DogCommand", "true", 3, 33);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "dogCommand", "DogCommand", "JUGGLE", 3, 33);
            });
        }

        // this is currently allowed
//        [Test]
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
//                _.Rule(_rule);
//                _rule.badValue(_, "dogCommand", "DogCommand", "sit", 3, 33);
//            });
//        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "stringListArg", "[String]", "[\"one\", 2]", 3, 36,
                    new[]
                    {
                        "In element #1: Expected type \"String\", found 2."
                    });
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "stringListArg", "String", "1", 3, 36);
            });
        }
    }

    public class ArgumentsOfCorrectType_Valid_Non_Nullable : ValidationTestBase<ValidationSchema>
    {
        private readonly ArgumentsOfCorrectType _rule = new ArgumentsOfCorrectType();

        [Test]
        public void arg_on_optional_arg()
        {
            var query = @"{
              dog {
                isHousetrained(atOtherHomes: true)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void no_arg_on_optional_arg()
        {
            var query = @"{
              dog {
                isHousetrained
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void multiple_args()
        {
            var query = @"{
              complicatedArgs {
                multipleReqs(req1: 1, req2: 2)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void multiple_args_reverse_order()
        {
            var query = @"{
              complicatedArgs {
                multipleReqs(req2: 2, req1: 1)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void no_args_on_multiple_optional()
        {
            var query = @"{
              complicatedArgs {
                multipleOpts
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void one_arg_on_multiple_optional()
        {
            var query = @"{
              complicatedArgs {
                multipleOpts(opt1: 1)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void second_arg_on_multiple_optional()
        {
            var query = @"{
              complicatedArgs {
                multipleOpts(opt2: 2)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void multiple_reqs_on_mixed()
        {
            var query = @"{
              complicatedArgs {
                multipleOptAndReq(req1: 3, req2: 4)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void multiple_reqs_and_one_opt_on_mixed()
        {
            var query = @"{
              complicatedArgs {
                multipleOptAndReq(req1: 3, req2: 4, opt1: 5)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void all_reqs_and_opts_on_mixed()
        {
            var query = @"{
              complicatedArgs {
                multipleOptAndReq(req1: 3, req2: 4, opt1: 5, opt2: 6)
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }
    }

    public class ArgumentsOfCorrectType_Invalid_Non_Nullable : ValidationTestBase<ValidationSchema>
    {
        private readonly ArgumentsOfCorrectType _rule = new ArgumentsOfCorrectType();

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "req2", "Int", "\"two\"", 3, 30);
                _rule.badValue(_, "req1", "Int", "\"one\"", 3, 43);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "req1", "Int", "\"one\"", 3, 30);
            });
        }
    }

    public class ArgumentsOfCorrectType_valid_input_object : ValidationTestBase<ValidationSchema>
    {
        private readonly ArgumentsOfCorrectType _rule = new ArgumentsOfCorrectType();

        [Test]
        public void optional_arg_despite_required_field_in_type()
        {
            var query = @"{
              complicatedArgs {
                complexArgField
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void partial_object_only_required()
        {
            var query = @"{
              complicatedArgs {
                complexArgField(complexArg: { requiredField: true })
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void partial_object_required_field_can_be_falsey()
        {
            var query = @"{
              complicatedArgs {
                complexArgField(complexArg: { requiredField: false })
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void partial_object_including_required()
        {
            var query = @"{
              complicatedArgs {
                complexArgField(complexArg: { requiredField: true, intField: 4 })
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void full_object()
        {
            var query = @"{
              complicatedArgs {
                complexArgField(complexArg: {
                  requiredField: true,
                  intField: 4,
                  stringField: ""foo"",
                  booleanField: false,
                  stringListField: [""one"", ""two""]
                })
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
        public void full_object_with_fields_in_different_order()
        {
            var query = @"{
              complicatedArgs {
                complexArgField(complexArg: {
                  stringListField: [""one"", ""two""],
                  booleanField: false,
                  requiredField: true,
                  stringField: ""foo""
                  intField: 4,
                })
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }
    }

    public class ArgumentsOfCorrectType_invalid_input_object_value : ValidationTestBase<ValidationSchema>
    {
        private readonly ArgumentsOfCorrectType _rule = new ArgumentsOfCorrectType();

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "complexArg", "ComplexInput", "{intField: 4}", 3, 33,
                    new []
                    {
                        "In field \"requiredField\": Expected \"Boolean!\", found null."
                    });
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "complexArg", "ComplexInput", "{stringListField: [\"one\", 2], requiredField: true}", 3, 33,
                    new []
                    {
                        "In field \"stringListField\": In element #1: Expected type \"String\", found 2."
                    });
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "complexArg", "ComplexInput", "{requiredField: true, unknownField: \"value\"}", 3, 33,
                    new []
                    {
                        "In field \"unknownField\": Unknown field."
                    });
            });
        }
    }


    public class ArgumentsOfCorrectType_directive_arguments : ValidationTestBase<ValidationSchema>
    {
        private readonly ArgumentsOfCorrectType _rule = new ArgumentsOfCorrectType();

        [Test]
        public void with_directives_of_valid_types()
        {
            var query = @"{
              dog @include(if: true) {
                name
              }
              human @skip(if: false) {
                name
              }
            }";

            ShouldPassRule(_ =>
            {
                _.Query = query;
                _.Rule(_rule);
            });
        }

        [Test]
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
                _.Rule(_rule);
                _rule.badValue(_, "if", "Boolean", "\"yes\"", 2, 28);
                _rule.badValue(_, "if", "Boolean", "ENUM", 3, 28);
            });
        }
    }

    public static class ValidationExtensions
    {
        public static void badValue(
            this ArgumentsOfCorrectType rule,
            ValidationTestConfig _,
            string argName,
            string typeName,
            string value,
            int? line = null,
            int? column = null,
            IEnumerable<string> errors = null)
        {
            if (errors == null)
            {
                errors = new [] {$"Expected type \"{typeName}\", found {value}."};
            }

            _.Error(
                rule.BadValueMessage(argName, null, value, errors),
                line,
                column);
        }
    }
}
