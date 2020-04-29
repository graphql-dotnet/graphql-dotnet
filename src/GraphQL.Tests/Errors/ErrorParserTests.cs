using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GraphQL.Execution;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Errors
{
    public class ErrorParserTests
    {
        [Fact]
        public void null_executionError_throws()
        {
            var parser = new ErrorParser();
            Should.Throw<ArgumentNullException>(() => parser.Parse(null));
        }

        [Fact]
        public void simple_message()
        {
            var error = new ExecutionError("test message");

            var parsed = new ErrorParser().Parse(error);
            parsed.Message.ShouldBe("test message");
            parsed.Locations.ShouldBeNull();
            parsed.Path.ShouldBeNull();
            parsed.Extensions.ShouldBeNull();
        }

        [Fact]
        public void null_message_ok()
        {
            var error = new ExecutionError(null); // create executionerror with a default message
            error.Message.ShouldNotBeNull();

            var parsed = new ErrorParser().Parse(error);
            parsed.Message.ShouldBe(error.Message);
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

            var parsed = new ErrorParser().Parse(error);
            parsed.Message.ShouldBe(error.Message);
            parsed.Locations.ShouldBeNull();
            parsed.Path.ShouldBeNull();
            parsed.Extensions.ShouldNotBeNull();
            parsed.Extensions.Count.ShouldBe(1);
            parsed.Extensions.ShouldContainKey("data");
            parsed.Extensions["data"].ShouldBeAssignableTo<IDictionary>().ShouldBe(error.Data);
        }

        [Fact]
        public void message_and_code()
        {
            var error = new ExecutionError(null) { Code = "test code" };

            var parsed = new ErrorParser().Parse(error);
            parsed.Message.ShouldBe(error.Message);
            parsed.Locations.ShouldBeNull();
            parsed.Path.ShouldBeNull();
            parsed.Extensions.ShouldNotBeNull();
            parsed.Extensions.Count.ShouldBe(2);
            parsed.Extensions.ShouldContainKeyAndValue("code", "test code");
            parsed.Extensions.ShouldContainKey("codes");
            parsed.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<string>>().ShouldBe(new[] { "test code" });
        }

        [Fact]
        public void message_and_inner_exception()
        {
            var innerException = new ArgumentNullException(null, new ArgumentOutOfRangeException());
            var error = new ExecutionError(innerException.Message, innerException);
            //error.Code.ShouldBe("ARGUMENT_NULL");
            error.Code.ShouldNotBeNull();
            //error.Codes.ShouldBe(new[] { "ARGUMENT_NULL", "ARGUMENT_OUT_OF_RANGE" });
            error.Codes.Count().ShouldBe(2);

            var parsed = new ErrorParser().Parse(error);
            parsed.Message.ShouldBe(error.Message);
            parsed.Locations.ShouldBeNull();
            parsed.Path.ShouldBeNull();
            parsed.Extensions.ShouldNotBeNull();
            parsed.Extensions.Count.ShouldBe(2);
            parsed.Extensions.ShouldContainKeyAndValue("code", error.Code);
            parsed.Extensions.ShouldContainKey("codes");
            parsed.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<string>>().ShouldBe(error.Codes);
        }

        [Fact]
        public void message_and_single_location()
        {
            var error = new ExecutionError(null);
            error.AddLocation(1, 2);
            error.Locations.Count().ShouldBe(1);

            var parsed = new ErrorParser().Parse(error);
            parsed.Message.ShouldBe(error.Message);
            parsed.Locations.ShouldBe(error.Locations);
            parsed.Path.ShouldBeNull();
            parsed.Extensions.ShouldBeNull();
        }

        [Fact]
        public void message_and_multiple_locations()
        {
            var error = new ExecutionError(null);
            error.AddLocation(1, 2);
            error.AddLocation(3, 4);
            error.Locations.Count().ShouldBe(2);

            var parsed = new ErrorParser().Parse(error);
            parsed.Message.ShouldBe(error.Message);
            parsed.Locations.ShouldBe(error.Locations);
            parsed.Path.ShouldBeNull();
            parsed.Extensions.ShouldBeNull();
        }

        [Fact]
        public void message_and_path()
        {
            var error = new ExecutionError(null)
            {
                //Path = new object[] { "root", 23, "child" },
                Path = new string[] { "root", "23", "child" },
            };

            var parsed = new ErrorParser().Parse(error);
            parsed.Message.ShouldBe(error.Message);
            parsed.Locations.ShouldBeNull();
            //parsed.Path.ShouldBe(new object[] { "root", 23, "child" });
            parsed.Path.ShouldBe(new string[] { "root", "23", "child" });
            parsed.Extensions.ShouldBeNull();
        }

        [Fact]
        public void drops_extensions_when_no_data()
        {
            var error = new ExecutionError(null);
            error.Code.ShouldBeNull();
            error.Codes.ShouldNotBeNull();
            error.Codes.Count().ShouldBe(0);
            error.Data.ShouldNotBeNull();
            error.Data.Count.ShouldBe(0);

            var parsed = new ErrorParser().Parse(error);
            parsed.Extensions.ShouldBeNull();
        }

        [Fact]
        public void path_does_not_drop_when_empty()
        {
            var error = new ExecutionError(null)
            {
                Path = new string[] { },
            };
            error.Path.ShouldNotBeNull();

            var parsed = new ErrorParser().Parse(error);
            parsed.Path.ShouldNotBeNull();
            parsed.Path.ShouldBe(error.Path);
        }

        [Fact]
        public void innerException_with_path()
        {
            var error = new ExecutionError("Test error message", new ArgumentNullException(null, new ArgumentOutOfRangeException()))
            {
                Path = new string[] { "path1", "path2" },
            };
            error.Data.Add("test1", "object1");
            error.Data.Add("test2", 15);
            error.Data.Add("test3", new Dictionary<string, object>() { { "test4", "object4" } });
            error.AddLocation(5, 6);
            error.AddLocation(7, 8);

            var parsed = new ErrorParser().Parse(error);
            parsed.Message.ShouldBe(error.Message);
            parsed.Locations.ShouldBe(error.Locations);
            parsed.Path.ShouldNotBeNull();
            parsed.Path.ShouldBe(error.Path);
            parsed.Extensions.ShouldNotBeNull();
            parsed.Extensions.Count.ShouldBe(3);
            parsed.Extensions.ShouldContainKeyAndValue("code", error.Code);
            parsed.Extensions.ShouldContainKey("codes");
            parsed.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<string>>().ShouldBe(error.Codes);
            parsed.Extensions.ShouldContainKey("data");
            parsed.Extensions["data"].ShouldBeAssignableTo<IDictionary>().ShouldBe(error.Data);
        }

        [Fact]
        public void exposeExceptions()
        {
            var innerException = new ArgumentNullException(null, new ArgumentOutOfRangeException());
            var error = new ExecutionError(innerException.Message, innerException);

            var parsed = new ErrorParser(true).Parse(error);
            parsed.Message.ShouldBe(error.ToString());
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

            var parsed = new ErrorParser(true).Parse(error);
            parsed.Message.ShouldBe(error.ToString());
        }

        [Fact]
        public void blank_codes_do_not_serialize()
        {
            var error = new ExecutionError(null)
            {
                Code = "",
            };
            error.Code.ShouldBe("");
            error.HasCodes.ShouldBeFalse();

            var parsed = new ErrorParser(true).Parse(error);
            parsed.Extensions.ShouldBeNull();
        }

        [Fact]
        public void inner_exception_of_type_exception_does_not_serialize_extensions()
        {
            var error = new ExecutionError("Test execution error", new Exception("Test exception"));
            error.Code.ShouldBe("");
            error.HasCodes.ShouldBeTrue();
            error.Codes.ShouldBe(new[] { "" });

            var parsed = new ErrorParser().Parse(error);
            parsed.Extensions.ShouldBeNull();
        }

        [Fact]
        public void codes_with_blank_code_has_undefined_behavior()
        {
            var error = new ExecutionError(null, new Exception(null, new ArgumentNullException("param")));
            error.Code.ShouldBe("");
            error.HasCodes.ShouldBeTrue();
            //error.Codes.ShouldBe(new[] { "", "ARGUMENT_NULL" });
            error.Codes.Count().ShouldBe(2);

            var parsed = new ErrorParser().Parse(error);
            parsed.Extensions.ShouldBeNull();

            error.Data.Add("test1", "object1");

            parsed = new ErrorParser().Parse(error);
            parsed.Extensions.ShouldNotBeNull();
        }
    }

}
