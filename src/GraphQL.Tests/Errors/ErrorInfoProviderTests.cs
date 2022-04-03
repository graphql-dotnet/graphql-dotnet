using System.Collections;
using GraphQL.Execution;
using GraphQLParser;

namespace GraphQL.Tests.Errors;

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
        error.AddLocation(new Location(5, 6));
        error.AddLocation(new Location(7, 8));

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

        var info = new ErrorInfoProvider(new ErrorInfoProviderOptions { ExposeExceptionStackTrace = true }).GetInfo(error);
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

        var info = new ErrorInfoProvider(new ErrorInfoProviderOptions { ExposeExceptionStackTrace = true }).GetInfo(error);
        info.Message.ShouldBe(error.ToString());
    }

    [Fact]
    public void blank_codes_do_serialize()
    {
        var error = new ExecutionError(null)
        {
            Code = "",
        };
        error.Code.ShouldBe("");

        var info = new ErrorInfoProvider(new ErrorInfoProviderOptions { ExposeExceptionStackTrace = true }).GetInfo(error);
        info.Extensions.ShouldNotBeNull();
        info.Extensions.ShouldContainKey("code");
        info.Extensions["code"].ShouldBe("");
        info.Extensions.ShouldContainKey("codes");
        info.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<object>>().ShouldBe(new object[] { "" });
    }

    [Fact]
    public void inner_exception_of_type_exception_does_serialize_extensions()
    {
        var error = new ExecutionError("Test execution error", new Exception("Test exception"));
        error.Code.ShouldBe(ErrorInfoProvider.GetErrorCode<Exception>());

        var info = new ErrorInfoProvider().GetInfo(error);
        info.Extensions.ShouldNotBeNull();
        info.Extensions.ShouldContainKey("code");
        info.Extensions["code"].ShouldBe(ErrorInfoProvider.GetErrorCode<Exception>());
        info.Extensions.ShouldContainKey("codes");
        info.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<object>>().ShouldBe(new[] { ErrorInfoProvider.GetErrorCode<Exception>() });
    }

    [Fact]
    public void codes_with_blank_code_always_serialize()
    {
        var error = new ExecutionError(null, new Exception(null, new ArgumentNullException("param")));
        error.Code.ShouldBe(ErrorInfoProvider.GetErrorCode<Exception>());

        var info = new ErrorInfoProvider().GetInfo(error);
        info.Extensions.ShouldNotBeNull();
        info.Extensions.ShouldContainKey("code");
        info.Extensions["code"].ShouldBe(ErrorInfoProvider.GetErrorCode<Exception>());
        info.Extensions.ShouldContainKey("codes");
        info.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<object>>().ShouldBe(
            new[] { ErrorInfoProvider.GetErrorCode<Exception>(), ErrorInfoProvider.GetErrorCode<ArgumentNullException>() });

        error.Data.Add("test1", "object1");

        info = new ErrorInfoProvider().GetInfo(error);
        info.Extensions.ShouldNotBeNull();
        info.Extensions.ShouldContainKey("code");
        info.Extensions["code"].ShouldBe("");
        info.Extensions.ShouldContainKey("data");
        info.Extensions.ShouldContainKey("codes");
        info.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<object>>().ShouldBe(
            new[] { ErrorInfoProvider.GetErrorCode<Exception>(), ErrorInfoProvider.GetErrorCode<ArgumentNullException>() });
    }

    [Fact]
    public void verify_exposeextensions_functionality_code()
    {
        var error = new ExecutionError("test")
        {
            Code = "test code"
        };
        var info = new ErrorInfoProvider(opts => opts.ExposeExtensions = false).GetInfo(error);
        info.Extensions.ShouldBeNull();
    }

    [Fact]
    public void verify_exposeextensions_functionality_codes()
    {
        var error = new ExecutionError("test", new ArgumentNullException())
        {
            Code = null
        };
        var info = new ErrorInfoProvider(opts => opts.ExposeExtensions = false).GetInfo(error);
        info.Extensions.ShouldBeNull();
    }

    [Fact]
    public void verify_exposeextensions_functionality_data()
    {
        var error = new ExecutionError("test");
        error.Data["test"] = "abc";
        var info = new ErrorInfoProvider(opts => opts.ExposeExtensions = false).GetInfo(error);
        info.Extensions.ShouldBeNull();
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void verify_exposeextensions_functionality_ignore_other_properties(bool exposeCode, bool exposeCodes, bool exposeData)
    {
        var error = new ExecutionError("test", new ArgumentNullException())
        {
            Code = "code"
        };
        error.Data["test"] = "abc";
        var info = new ErrorInfoProvider(opts =>
        {
            opts.ExposeExtensions = false;
            opts.ExposeCode = exposeCode;
            opts.ExposeCodes = exposeCodes;
            opts.ExposeData = exposeData;
        }).GetInfo(error);
        info.Extensions.ShouldBeNull();
    }

    [Fact]
    public void verify_exposecode_functionality()
    {
        var error = new ExecutionError("message")
        {
            Code = "code"
        };
        var info = new ErrorInfoProvider(opts => opts.ExposeCode = false).GetInfo(error);
        info.Extensions.ShouldNotBeNull();
        info.Extensions.ShouldNotContainKey("code");
        info.Extensions.ShouldContainKey("codes");
        info.Extensions["codes"].ShouldBeAssignableTo<IEnumerable<object>>().ShouldBe(new[] { "code" });
    }

    [Fact]
    public void verify_exposecodes_functionality_no_other_data()
    {
        var error = new ExecutionError("message", new ArgumentNullException())
        {
            Code = null
        };
        var info = new ErrorInfoProvider(opts => opts.ExposeCodes = false).GetInfo(error);
        info.Extensions.ShouldBeNull();
    }

    [Fact]
    public void verify_exposecodes_functionality_with_other_data()
    {
        var error = new ExecutionError("message", new ArgumentNullException())
        {
            Code = "code"
        };
        var info = new ErrorInfoProvider(opts => opts.ExposeCodes = false).GetInfo(error);
        info.Extensions.ShouldNotBeNull();
        info.Extensions.ShouldContainKey("code");
        info.Extensions.ShouldNotContainKey("codes");
    }

    [Fact]
    public void verify_exposedata_functionality_no_other_data()
    {
        var error = new ExecutionError("message");
        error.Data["test"] = "abc";
        var info = new ErrorInfoProvider(opts => opts.ExposeData = false).GetInfo(error);
        info.Extensions.ShouldBeNull();
    }

    [Fact]
    public void verify_exposedata_functionality_with_other_data()
    {
        var error = new ExecutionError("message")
        {
            Code = "code"
        };
        error.Data["test"] = "abc";
        var info = new ErrorInfoProvider(opts => opts.ExposeData = false).GetInfo(error);
        info.Extensions.ShouldNotBeNull();
        info.Extensions.ShouldContainKey("code");
        info.Extensions.ShouldNotContainKey("data");
    }

    [Theory]
    [InlineData(typeof(Exception), "")]
    [InlineData(typeof(ArgumentException), "ARGUMENT")]
    [InlineData(typeof(ArgumentNullException), "ARGUMENT_NULL")]
    [InlineData(typeof(GraphQLParser.Exceptions.GraphQLSyntaxErrorException), "SYNTAX_ERROR")]
    [InlineData(typeof(GraphQLException), "")]
    [InlineData(typeof(GraphQlException), "GRAPH_QL")]
    [InlineData(typeof(ExecutionError), "EXECUTION_ERROR")]
    [InlineData(typeof(GraphQL.Validation.ValidationError), "VALIDATION_ERROR")]
    public void geterrorcode_tests(Type type, string code)
    {
        ErrorInfoProvider.GetErrorCode(type).ShouldBe(code);
    }

    [Fact]
    public void geterrorcode_null_throws()
    {
        Assert.Throws<ArgumentNullException>(() => ErrorInfoProvider.GetErrorCode((Type)null));
    }

    [Fact]
    public void geterrorcode_instance_works()
    {
        ErrorInfoProvider.GetErrorCode(new ArgumentNullException()).ShouldBe("ARGUMENT_NULL");
    }

    [Fact]
    public void geterrorcode_generic_works()
    {
        ErrorInfoProvider.GetErrorCode<ArgumentNullException>().ShouldBe("ARGUMENT_NULL");
    }

    private class GraphQLException : Exception { }
    private class GraphQlException : Exception { }
}
