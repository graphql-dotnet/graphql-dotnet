using System.Collections.Generic;
using GraphQL.Types;
using Should;

namespace GraphQL.Tests.Execution
{
    public class ResolveFieldContextTests
    {
        private readonly ResolveFieldContext _context;

        public ResolveFieldContextTests()
        {
            _context = new ResolveFieldContext();
            _context.Arguments = new Dictionary<string, object>();
        }

        [Test]
        public void argument_converts_int_to_long()
        {
            int val = 1;
            _context.Arguments["a"] = val;
            var result = _context.Argument<long>("a");
            result.ShouldEqual(1);
        }

        [Test]
        public void argument_converts_long_to_int()
        {
            long val = 1;
            _context.Arguments["a"] = val;
            var result = _context.Argument<int>("a");
            result.ShouldEqual(1);
        }

        [Test]
        public void argument_returns_boxed_string_uncast()
        {
            _context.Arguments["a"] = "one";
            var result = _context.Argument<object>("a");
            result.ShouldEqual("one");
        }

        [Test]
        public void argument_returns_long()
        {
            long val = 1000000000000001;
            _context.Arguments["a"] = val;
            var result = _context.Argument<long>("a");
            result.ShouldEqual(1000000000000001);
        }

        [Test]
        public void argument_returns_enum()
        {
            _context.Arguments["a"] = SomeEnum.Two;
            var result = _context.Argument<SomeEnum>("a");
            result.ShouldEqual(SomeEnum.Two);
        }

        [Test]
        public void argument_returns_enum_from_string()
        {
            _context.Arguments["a"] = "two";
            var result = _context.Argument<SomeEnum>("a");
            result.ShouldEqual(SomeEnum.Two);
        }

        [Test]
        public void argument_returns_enum_from_number()
        {
            _context.Arguments["a"] = 1;
            var result = _context.Argument<SomeEnum>("a");
            result.ShouldEqual(SomeEnum.Two);
        }

        [Test]
        public void throw_error_if_argument_doesnt_exist()
        {
            Expect.Throws<ExecutionError>(() => _context.Argument<string>("wat"));
        }

        enum SomeEnum
        {
            One,
            Two
        }
    }
}
