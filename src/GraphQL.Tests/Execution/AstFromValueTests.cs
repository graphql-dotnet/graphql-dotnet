using GraphQL.Execution;
using GraphQL.Language;
using GraphQL.Validation;
using Should;

namespace GraphQL.Tests.Execution
{
    public class AstFromValueTests
    {
        DocumentExecuter _executor = new DocumentExecuter(new AntlrDocumentBuilder(), new DocumentValidator());

        [Test]
        public void converts_string_to_string_value()
        {
            var result = _executor.AstFromValue(null, "test", null);
            result.ShouldNotBeNull();
            result.ShouldBeType<StringValue>();
        }

        [Test]
        public void converts_bool_to_boolean_value()
        {
            var result = _executor.AstFromValue(null, true, null);
            result.ShouldNotBeNull();
            result.ShouldBeType<BooleanValue>();
        }

        [Test]
        public void converts_long_to_long_value()
        {
            long val = 123;
            var result = _executor.AstFromValue(null, val, null);
            result.ShouldNotBeNull();
            result.ShouldBeType<LongValue>();
        }

        [Test]
        public void converts_int_to_int_value()
        {
            int val = 123;
            var result = _executor.AstFromValue(null, val, null);
            result.ShouldNotBeNull();
            result.ShouldBeType<IntValue>();
        }
    }
}
