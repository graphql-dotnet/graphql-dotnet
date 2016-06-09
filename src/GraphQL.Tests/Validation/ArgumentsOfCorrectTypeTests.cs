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
        public void string_into_int_value()
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
                badValue(_, "intArg", "Int", "\"3\"", 3, 29);
            });
        }

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
                badValue(_, "intArg", "Int", "FOO", 3, 29);
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
                badValue(_, "intArg", "Int", "3.0", 3, 29);
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
                badValue(_, "intArg", "Int", "3.333", 3, 29);
            });
        }

        private void badValue(
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
                _rule.BadValueMessage(argName, null, value, errors),
                line,
                column);
        }
    }
}
