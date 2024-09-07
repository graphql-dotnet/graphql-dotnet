using System.Linq.Expressions;
using GraphQL.Types;
using GraphQL.Validation;

namespace GraphQL.Tests.Attributes;

public class ValidateArgumentsAttributeTests
{
    [Fact]
    public void null_throws()
    {
        Should.Throw<ArgumentNullException>(() => new ValidateArgumentsAttribute((string)null!));
        Should.Throw<ArgumentNullException>(() => new ValidateArgumentsAttribute((Type)null!));
        Should.Throw<ArgumentNullException>(() => new ValidateArgumentsAttribute(null!, "test"));
        Should.Throw<ArgumentNullException>(() => new ValidateArgumentsAttribute(typeof(HelperClass), null!));
    }

    [Theory]
    [InlineData(typeof(Class1), "Could not find method 'InvalidMethod' on CLR type 'Class1' while initializing 'Class1.Hello'. The method must have a single parameter of type FieldArgumentsValidationContext.")]
    [InlineData(typeof(Class2), "Could not find method 'ValidateArguments' on CLR type 'Dummy' while initializing 'Class2.Hello'. The method must have a single parameter of type FieldArgumentsValidationContext.")]
    [InlineData(typeof(Class3), "Could not find method 'InvalidMethod' on CLR type 'Dummy' while initializing 'Class3.Hello'. The method must have a single parameter of type FieldArgumentsValidationContext.")]
    [InlineData(typeof(Class4), "Method 'InvalidMethod' on CLR type 'Class4' must have a return type of ValueTask.")]
    [InlineData(typeof(Class5), "Method 'ValidateArguments' on CLR type 'Dummy2' must have a return type of ValueTask.")]
    [InlineData(typeof(Class6), "Method 'InvalidMethod' on CLR type 'Dummy2' must have a return type of ValueTask.")]
    public void method_not_found_arguments(Type clrType, string expectedMessage)
    {
        // create a delegate to call the constructor of the AutoRegisteringObjectGraphType
        var graphType = typeof(AutoRegisteringObjectGraphType<>).MakeGenericType(clrType);
        var fn = Expression.Lambda<Func<object>>(Expression.New(graphType)).Compile();

        Should.Throw<InvalidOperationException>(() => fn()).Message.ShouldBe(expectedMessage);
    }

    public class Class1
    {
        [ValidateArguments("InvalidMethod")]
        public string Hello(string value) => value;
        private ValueTask InvalidMethod(FieldArgumentsValidationContext context) { _ = context; return default; } // not static
        private static void InvalidMethod() { } // wrong signature
    }

    public class Class2
    {
        [ValidateArguments(typeof(Dummy))]
        public string Hello(string value) => value;
    }

    public class Class3
    {
        [ValidateArguments(typeof(Dummy), "InvalidMethod")]
        public string Hello(string value) => value;
    }

    public class Class4
    {
        [ValidateArguments("InvalidMethod")]
        public string Hello(string value) => value;
        private static string InvalidMethod(object value) => (string)value;
    }

    public class Class5
    {
        [ValidateArguments(typeof(Dummy2))]
        public string Hello(string value) => value;
    }

    public class Class6
    {
        [ValidateArguments(typeof(Dummy2), "InvalidMethod")]
        public string Hello(string value) => value;
    }

    public class Dummy
    {
        public ValueTask ValidateArguments(FieldArgumentsValidationContext context) { _ = context; return default; } // not static
        public static void ValidateArguments() { } // wrong signature
        public ValueTask InvalidMethod(FieldArgumentsValidationContext context) { _ = context; return default; } // not static
        public static void InvalidMethod() { } // wrong signature
    }

    public class Dummy2
    {
        public static string ValidateArguments(object value) => (string)value;
        public static string InvalidMethod(object value) => (string)value;
    }

    [Fact]
    public async Task ValidateArguments_works()
    {
        var queryType = new AutoRegisteringObjectGraphType<ArgTests>();
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        var query = """
            {
              hello1(value: "abc")
              hello2(value: "def")
              hello3(value: "ghi")
              hello4(value: "jkl")
            }
            """;
        var expected = """
            {
              "errors": [
                {
                  "message": "abcpass1",
                  "locations": [
                    {
                      "line": 2,
                      "column": 3
                    }
                  ],
                  "extensions": {
                    "code": "VALIDATION_ERROR",
                    "codes": [
                      "VALIDATION_ERROR"
                    ]
                  }
                },
                {
                  "message": "defpass1",
                  "locations": [
                    {
                      "line": 3,
                      "column": 3
                    }
                  ],
                  "extensions": {
                    "code": "VALIDATION_ERROR",
                    "codes": [
                      "VALIDATION_ERROR"
                    ]
                  }
                },
                {
                  "message": "ghipass1",
                  "locations": [
                    {
                      "line": 4,
                      "column": 3
                    }
                  ],
                  "extensions": {
                    "code": "VALIDATION_ERROR",
                    "codes": [
                      "VALIDATION_ERROR"
                    ]
                  }
                },
                {
                  "message": "jklpass1",
                  "locations": [
                    {
                      "line": 5,
                      "column": 3
                    }
                  ],
                  "extensions": {
                    "code": "VALIDATION_ERROR",
                    "codes": [
                      "VALIDATION_ERROR"
                    ]
                  }
                }
              ]
            }
            """;
        var result = await new DocumentExecuter().ExecuteAsync(o =>
        {
            o.Query = query;
            o.Schema = schema;
        });
        var actual = new SystemTextJson.GraphQLSerializer().Serialize(result);
        actual.ShouldBeCrossPlatJson(expected);
    }

    public class ArgTests
    {
        [ValidateArguments(nameof(ValidateHelloArgument))]
        public static string Hello1(string value) => value;
        [ValidateArguments(typeof(ValidateArgumentsClass))]
        public static string Hello2(string value) => value;
        [ValidateArguments(typeof(HelperClass), nameof(HelperClass.ValidateHelloArgument))]
        public static string Hello3(string value) => value;
        [ValidateArguments(typeof(ArgTests), nameof(ValidateHelloArgument))]
        public static string Hello4(string value) => value;

        private static ValueTask ValidateHelloArgument(FieldArgumentsValidationContext context) => throw new ValidationError(context.GetArgument<string>("value") + "pass1");
    }

    public class ValidateArgumentsClass
    {
        public static ValueTask ValidateArguments(FieldArgumentsValidationContext context) => throw new ValidationError(context.GetArgument<string>("value") + "pass1");
    }

    public class HelperClass
    {
        public static ValueTask ValidateHelloArgument(FieldArgumentsValidationContext context) => throw new ValidationError(context.GetArgument<string>("value") + "pass1");
    }
}
