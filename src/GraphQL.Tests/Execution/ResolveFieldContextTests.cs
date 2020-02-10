using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class ResolveFieldContextTests
    {
        private readonly ResolveFieldContext _context;

        public ResolveFieldContextTests()
        {
            _context = new ResolveFieldContext
            {
                Arguments = new Dictionary<string, object>(),
                Errors = new ExecutionErrors()
            };
        }

        [Fact]
        public void argument_converts_int_to_long()
        {
            int val = 1;
            _context.Arguments["a"] = val;
            var result = _context.GetArgument<long>("a");
            result.ShouldBe(1);
        }

        [Fact]
        public void argument_converts_long_to_int()
        {
            long val = 1;
            _context.Arguments["a"] = val;
            var result = _context.GetArgument<int>("a");
            result.ShouldBe(1);
        }

        [Fact]
        public void long_to_int_should_throw_for_out_of_range()
        {
            long val = 89429901947254093;
            _context.Arguments["a"] = val;
            Should.Throw<OverflowException>(() => _context.GetArgument<int>("a"));
        }

        [Fact]
        public void argument_returns_boxed_string_uncast()
        {
            _context.Arguments["a"] = "one";
            var result = _context.GetArgument<object>("a");
            result.ShouldBe("one");
        }

        [Fact]
        public void argument_returns_long()
        {
            long val = 1000000000000001;
            _context.Arguments["a"] = val;
            var result = _context.GetArgument<long>("a");
            result.ShouldBe(1000000000000001);
        }

        [Fact]
        public void argument_returns_enum()
        {
            _context.Arguments["a"] = SomeEnum.Two;
            var result = _context.GetArgument<SomeEnum>("a");
            result.ShouldBe(SomeEnum.Two);
        }

        [Fact]
        public void argument_returns_enum_from_string()
        {
            _context.Arguments["a"] = "two";
            var result = _context.GetArgument<SomeEnum>("a");
            result.ShouldBe(SomeEnum.Two);
        }

        [Fact]
        public void argument_returns_enum_from_number()
        {
            _context.Arguments["a"] = 1;
            var result = _context.GetArgument<SomeEnum>("a");
            result.ShouldBe(SomeEnum.Two);
        }

        [Fact]
        public void argument_returns_default_when_missing()
        {
            _context.GetArgument<string>("wat").ShouldBeNull();
        }

        [Fact]
        public void argument_returns_provided_default_when_missing()
        {
            _context.GetArgument("wat", "foo").ShouldBe("foo");
        }

        [Fact]
        public void argument_returns_list_from_array()
        {
            _context.Arguments = @"{ ""a"": [""one"", ""two""]}".ToInputs();
            var result = _context.GetArgument<List<string>>("a");
            result.ShouldNotBeNull();
            result.Count.ShouldBe(2);
            result[0].ShouldBe("one");
            result[1].ShouldBe("two");
        }

        [Fact]
        public async Task try_resolve_async_handles_null()
        {
            var result = await _context.TryAsyncResolve(c => null);
            result.ShouldBe(null);
        }

        [Fact]
        public async Task try_resolve_async_handles_exception()
        {
            var result = await _context.TryAsyncResolve(c => throw new InvalidOperationException("Test Error"));
            result.ShouldBeNull();
            _context.Errors.First().Message.ShouldBe("Test Error");
        }

        [Fact]
        public async Task try_resolve_sets_inner_exception()
        {
            var exception = new Exception("Test");
            var result = await _context.TryAsyncResolve(
                c => throw exception);
            result.ShouldBeNull();
            _context.Errors.First().InnerException.ShouldBe(exception);
        }

        [Fact]
        public async Task try_resolve_async_invokes_error_handler()
        {
            var result = await _context.TryAsyncResolve(
                c => throw new InvalidOperationException(),
                e => {
                    e.Add(new ExecutionError("Test Error"));
                    return null;
                }
            );
            result.ShouldBeNull();
            _context.Errors.First().Message.ShouldBe("Test Error");
        }

        [Fact]
        public async Task try_resolve_async_not_null_invokes_error_handler()
        {
            var obj = new object();
            var result = await _context.TryAsyncResolve(
                c => throw new InvalidOperationException(),
                e => {
                    e.Add(new ExecutionError("Test Error"));
                    return Task.FromResult(obj);
                }
            );
            result.ShouldBe(obj);
            _context.Errors.First().Message.ShouldBe("Test Error");
        }

        [Fact]
        public async Task try_resolve_generic_sets_inner_exception()
        {
            var exception = new Exception("Test");
            var result = await _context.TryAsyncResolve<int>(
                c => throw exception);
            result.ShouldBe(default);
            _context.Errors.First().InnerException.ShouldBe(exception);
        }

        [Theory]
        [InlineData(123)]
        public async Task try_resolve_generic_async_invokes_error_handler(int value)
        {
            var result = await _context.TryAsyncResolve(
                c => throw new InvalidOperationException(),
                e => {
                    e.Add(new ExecutionError("Test Error"));
                    return Task.FromResult(value);
                }
            );
            result.ShouldBe(value);
            _context.Errors.First().Message.ShouldBe("Test Error");
        }

        [Fact]
        public async Task try_resolve_async_properly_resolves_result()
        {
            var result = await _context.TryAsyncResolve(
                c => Task.FromResult<object>("Test Result")
            );
            result.ShouldBe("Test Result");
        }

        [Fact]
        public void resolveFieldContextAdapter_throws_error_when_null()
        {
            Should.Throw<ArgumentNullException>(() =>
            {
                var adapter = new ResolveFieldContextAdapter<object>(null);
            });
        }

        [Fact]
        public void resolveFieldContextAdapter_throws_error_if_invalid_type()
        {
            var context = new ResolveFieldContext() { Source = "test" };
            Should.Throw<ArgumentException>(() =>
            {
                var adapter = new ResolveFieldContextAdapter<int>(context);
            });
        }

        [Fact]
        public void resolveFieldContextAdapter_accepts_null_sources_ref()
        {
            var context = new ResolveFieldContext();
            var adapter = new ResolveFieldContextAdapter<string>(context);
            adapter.Source.ShouldBe(null);
        }

        [Fact]
        public void resolveFieldContextAdapter_accepts_null_sources_nullable()
        {
            var context = new ResolveFieldContext();
            var adapter = new ResolveFieldContextAdapter<int?>(context);
            adapter.Source.ShouldBe(null);
        }

        [Fact]
        public void resolveFieldContextAdapter_throws_error_for_null_values()
        {
            var context = new ResolveFieldContext();
            Should.Throw<ArgumentException>(() =>
            {
                var adapter = new ResolveFieldContextAdapter<int>(context);
            });
        }

        enum SomeEnum
        {
            One,
            Two
        }
    }
}
