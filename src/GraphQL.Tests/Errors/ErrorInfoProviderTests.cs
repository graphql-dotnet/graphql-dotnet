using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Execution;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Errors
{
    public class ErrorInfoProviderTests
    {
        [Fact]
        public void null_executionError_throws()
        {
            var provider = new ErrorInfoProvider();
            Should.Throw<ArgumentNullException>(() => provider.GetInfo(null));
        }

        [Fact]
        public void simple_message()
        {
            var error = new ExecutionError("test message");

            var info = new ErrorInfoProvider().GetInfo(error);
            info.Message.ShouldBe("test message");
            info.Extensions.ShouldBeNull();
        }

        [Fact]
        public void null_message_ok()
        {
            var error = new ExecutionError(null); // create executionerror with a default message
            error.Message.ShouldNotBeNull();

            var info = new ErrorInfoProvider().GetInfo(error);
            info.Message.ShouldBe(error.Message);
        }

        [Fact]
        public void message_and_data()
        {
            var data = new Dictionary<string, object>()
            {
                { "test1", "object1" },
                { "test2", 15 },
                { "test3", new Dictionary<string, object>() { { "test4", "object4" } } },
            };
            var error = new ExecutionError(null, data);
            error.Data.ShouldNotBeNull();
            error.Data.Count.ShouldBe(3);
            error.Data["test1"].ShouldBe("object1");
            error.Data["test2"].ShouldBe(15);
            error.Data["test3"].ShouldBe(new Dictionary<string, object>() { { "test4", "object4" } });

            var info = new ErrorInfoProvider().GetInfo(error);
            info.Message.ShouldBe(error.Message);
            info.Extensions.ShouldNotBeNull();
            info.Extensions.Count.ShouldBe(1);
            info.Extensions.ShouldContainKey("data");
            info.Extensions["data"].ShouldBeAssignableTo<IDictionary>().ShouldBe(error.Data);
        }

        [Fact]
        public void message_and_code()
        {
            var error = new ExecutionError(null) { Code = "test code" };

            var info = new ErrorInfoProvider().GetInfo(error);
            info.Message.ShouldBe(error.Message);
            info.Extensions.ShouldNotBeNull();
            info.Extensions.Count.ShouldBe(2);
            info.Extensions.ShouldContainKeyAndValue("code", "test code");
            info.Extensions.ShouldContainKey("codes");
            info.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<string>>().ShouldBe(new[] { "test code" });
        }

        [Fact]
        public void message_and_inner_exception()
        {
            var innerException = new ArgumentNullException(null, new ArgumentOutOfRangeException());
            var error = new ExecutionError(innerException.Message, innerException);
            error.Code.ShouldBe(ErrorInfoProvider.GetErrorCode<ArgumentNullException>());
            error.Code.ShouldNotBeNull();

            var info = new ErrorInfoProvider().GetInfo(error);
            info.Message.ShouldBe(error.Message);
            info.Extensions.ShouldNotBeNull();
            info.Extensions.Count.ShouldBe(2);
            info.Extensions.ShouldContainKeyAndValue("code", error.Code);
            info.Extensions.ShouldContainKey("codes");
            info.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<string>>().ShouldBe(
                new[] { ErrorInfoProvider.GetErrorCode<ArgumentNullException>(), ErrorInfoProvider.GetErrorCode<ArgumentOutOfRangeException>() });
        }

        [Fact]
        public void drops_extensions_when_no_data()
        {
            var error = new ExecutionError(null);
            error.Code.ShouldBeNull();
            error.Data.ShouldNotBeNull();
            error.Data.Count.ShouldBe(0);

            var info = new ErrorInfoProvider().GetInfo(error);
            info.Extensions.ShouldBeNull();
        }

        [Fact]
        public void multiple_innerExceptions()
        {
            var error = new ExecutionError("Test error message", new ArgumentNullException(null, new ArgumentOutOfRangeException()));
            error.Data.Add("test1", "object1");
            error.Data.Add("test2", 15);
            error.Data.Add("test3", new Dictionary<string, object>() { { "test4", "object4" } });
            error.AddLocation(5, 6);
            error.AddLocation(7, 8);

            var info = new ErrorInfoProvider().GetInfo(error);
            info.Message.ShouldBe(error.Message);
            info.Extensions.ShouldNotBeNull();
            info.Extensions.Count.ShouldBe(3);
            info.Extensions.ShouldContainKeyAndValue("code", error.Code);
            info.Extensions.ShouldContainKey("codes");
            info.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<string>>().ShouldBe(
                new[] { ErrorInfoProvider.GetErrorCode<ArgumentNullException>(), ErrorInfoProvider.GetErrorCode<ArgumentOutOfRangeException>() });
            info.Extensions.ShouldContainKey("data");
            info.Extensions["data"].ShouldBeAssignableTo<IDictionary>().ShouldBe(error.Data);
        }

        [Fact]
        public void exposeExceptions()
        {
            var innerException = new ArgumentNullException(null, new ArgumentOutOfRangeException());
            var error = new ExecutionError(innerException.Message, innerException);

            var info = new ErrorInfoProvider(true).GetInfo(error);
            info.Message.ShouldBe(error.ToString());
        }

        [Fact]
        public void exposeExceptions_with_real_stack_trace()
        {
            // generate a real stack trace to serialize
            ExecutionError error;
            try
            {
                try
                {
                    throw new ArgumentNullException(null, new ArgumentOutOfRangeException());
                }
                catch (Exception innerException)
                {
                    throw new ExecutionError(innerException.Message, innerException);
                }
            }
            catch (ExecutionError e)
            {
                error = e;
            }

            var info = new ErrorInfoProvider(true).GetInfo(error);
            info.Message.ShouldBe(error.ToString());
        }

        [Fact]
        public void blank_codes_do_not_serialize()
        {
            var error = new ExecutionError(null)
            {
                Code = "",
            };
            error.Code.ShouldBe("");

            var info = new ErrorInfoProvider(true).GetInfo(error);
            info.Extensions.ShouldBeNull();
        }

        [Fact]
        public void inner_exception_of_type_exception_does_not_serialize_extensions()
        {
            var error = new ExecutionError("Test execution error", new Exception("Test exception"));
            error.Code.ShouldBe(ErrorInfoProvider.GetErrorCode<Exception>());

            var info = new ErrorInfoProvider().GetInfo(error);
            info.Extensions.ShouldBeNull();
        }

        [Fact]
        public void codes_with_blank_code_has_undefined_behavior()
        {
            var error = new ExecutionError(null, new Exception(null, new ArgumentNullException("param")));
            error.Code.ShouldBe(ErrorInfoProvider.GetErrorCode<Exception>());

            var info = new ErrorInfoProvider().GetInfo(error);
            info.Extensions.ShouldBeNull();

            error.Data.Add("test1", "object1");

            info = new ErrorInfoProvider().GetInfo(error);
            info.Extensions.ShouldNotBeNull();
            info.Extensions.ShouldContainKey("data");
            info.Extensions.ShouldContainKey("codes");
            info.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<object>>().ShouldBe(
                new[] { ErrorInfoProvider.GetErrorCode<Exception>(), ErrorInfoProvider.GetErrorCode<ArgumentNullException>() });
        }
    }
}
