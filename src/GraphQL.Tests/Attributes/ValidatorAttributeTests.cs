using System.Linq.Expressions;
using GraphQL.Types;

namespace GraphQL.Tests.Attributes;

public class ValidatorAttributeTests
{
    [Fact]
    public void null_throws()
    {
        Should.Throw<ArgumentNullException>(() => new ValidatorAttribute((string)null!));
        Should.Throw<ArgumentNullException>(() => new ValidatorAttribute((Type)null!));
        Should.Throw<ArgumentNullException>(() => new ValidatorAttribute(null!, "test"));
        Should.Throw<ArgumentNullException>(() => new ValidatorAttribute(typeof(HelperClass), null!));
    }

    [Theory]
    [InlineData(typeof(Class1), "Could not find method 'InvalidMethod' on CLR type 'Class1' while initializing argument 'value'. The method must have a single parameter of type object.")]
    [InlineData(typeof(Class2), "Could not find method 'Validator' on CLR type 'Dummy' while initializing argument 'value'. The method must have a single parameter of type object.")]
    [InlineData(typeof(Class3), "Could not find method 'InvalidMethod' on CLR type 'Dummy' while initializing argument 'value'. The method must have a single parameter of type object.")]
    [InlineData(typeof(Class4), "Method 'InvalidMethod' on CLR type 'Class4' must have a void return type.")]
    [InlineData(typeof(Class5), "Method 'Validator' on CLR type 'Dummy2' must have a void return type.")]
    [InlineData(typeof(Class6), "Method 'InvalidMethod' on CLR type 'Dummy2' must have a void return type.")]
    public void method_not_found_arguments(Type clrType, string expectedMessage)
    {
        // create a delegate to call the constructor of the AutoRegisteringObjectGraphType
        var graphType = typeof(AutoRegisteringObjectGraphType<>).MakeGenericType(clrType);
        var fn = Expression.Lambda<Func<object>>(Expression.New(graphType)).Compile();

        Should.Throw<InvalidOperationException>(() => fn()).Message.ShouldBe(expectedMessage);
    }

    public class Class1
    {
        public string Hello([Validator("InvalidMethod")] string value) => value;
        private void InvalidMethod(object value) { _ = value; } // not static
        private static void InvalidMethod() { } // wrong signature
    }

    public class Class2
    {
        public string Hello([Validator(typeof(Dummy))] string value) => value;
    }

    public class Class3
    {
        public string Hello([Validator(typeof(Dummy), "InvalidMethod")] string value) => value;
    }

    public class Class4
    {
        public string Hello([Validator("InvalidMethod")] string value) => value;
        private static string InvalidMethod(object value) => (string)value;
    }

    public class Class5
    {
        public string Hello([Validator(typeof(Dummy2))] string value) => value;
    }

    public class Class6
    {
        public string Hello([Validator(typeof(Dummy2), "InvalidMethod")] string value) => value;
    }

    [Theory]
    [InlineData(typeof(Class1b), "Could not find method 'InvalidMethod' on CLR type 'Class1b' while initializing 'Class1b.Hello'. The method must have a single parameter of type object.")]
    [InlineData(typeof(Class2b), "Could not find method 'Validator' on CLR type 'Dummy' while initializing 'Class2b.Hello'. The method must have a single parameter of type object.")]
    [InlineData(typeof(Class3b), "Could not find method 'InvalidMethod' on CLR type 'Dummy' while initializing 'Class3b.Hello'. The method must have a single parameter of type object.")]
    [InlineData(typeof(Class4b), "Method 'InvalidMethod' on CLR type 'Class4b' must have a void return type.")]
    [InlineData(typeof(Class5b), "Method 'Validator' on CLR type 'Dummy2' must have a void return type.")]
    [InlineData(typeof(Class6b), "Method 'InvalidMethod' on CLR type 'Dummy2' must have a void return type.")]
    public void method_not_found_input_fields(Type clrType, string expectedMessage)
    {
        // create a delegate to call the constructor of the AutoRegisteringInputObjectGraphType
        var graphType = typeof(AutoRegisteringInputObjectGraphType<>).MakeGenericType(clrType);
        var fn = Expression.Lambda<Func<object>>(Expression.New(graphType)).Compile();

        Should.Throw<InvalidOperationException>(() => fn()).Message.ShouldBe(expectedMessage);
    }

    public class Class1b
    {
        [Validator("InvalidMethod")]
        public string Hello { get; set; }
        private void InvalidMethod(object value) { _ = value; } // not static
        private static void InvalidMethod() { } // wrong signature
    }

    public class Class2b
    {
        [Validator(typeof(Dummy))]
        public string Hello { get; set; }
    }

    public class Class3b
    {
        [Validator(typeof(Dummy), "InvalidMethod")]
        public string Hello { get; set; }
    }

    public class Class4b
    {
        [Validator("InvalidMethod")]
        public string Hello { get; set; }
        private static string InvalidMethod(object value) => (string)value;
    }

    public class Class5b
    {
        [Validator(typeof(Dummy2))]
        public string Hello { get; set; }
    }

    public class Class6b
    {
        [Validator(typeof(Dummy2), "InvalidMethod")]
        public string Hello { get; set; }
    }

    public class Dummy
    {
        public void Validator(object value) { _ = value; } // not static
        public static void Validator() { } // wrong signature
        public void InvalidMethod(object value) { _ = value; } // not static
        public static void InvalidMethod() { } // wrong signature
    }

    public class Dummy2
    {
        public static string Validator(object value) => (string)value;
        public static string InvalidMethod(object value) => (string)value;
    }

    [Fact]
    public async Task validator_works_for_arguments()
    {
        var queryType = new AutoRegisteringObjectGraphType<ArgTests>();
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        var query = """
            {
              hello1(value: "abc")
              hello2(value: "def")
              hello3(value: "ghi")
            }
            """;
        var expected = """
            {
              "errors": [
                {
                  "message": "Invalid value for argument 'value' of field 'hello1'. abcpass1",
                  "locations": [
                    {
                      "line": 2,
                      "column": 17
                    }
                  ],
                  "extensions": {
                    "code": "INVALID_VALUE",
                    "codes": [
                      "INVALID_VALUE",
                      "INVALID_OPERATION"
                    ],
                    "number": "5.6"
                  }
                },
                {
                  "message": "Invalid value for argument 'value' of field 'hello2'. defpass2",
                  "locations": [
                    {
                      "line": 3,
                      "column": 17
                    }
                  ],
                  "extensions": {
                    "code": "INVALID_VALUE",
                    "codes": [
                      "INVALID_VALUE",
                      "INVALID_OPERATION"
                    ],
                    "number": "5.6"
                  }
                },
                {
                  "message": "Invalid value for argument 'value' of field 'hello3'. ghipass3",
                  "locations": [
                    {
                      "line": 4,
                      "column": 17
                    }
                  ],
                  "extensions": {
                    "code": "INVALID_VALUE",
                    "codes": [
                      "INVALID_VALUE",
                      "INVALID_OPERATION"
                    ],
                    "number": "5.6"
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
        public static string Hello1([Validator(nameof(ValidateHelloArgument))] string value) => value;
        public static string Hello2([Validator(typeof(ValidatorClass))] string value) => value;
        public static string Hello3([Validator(typeof(HelperClass), nameof(HelperClass.ValidateHelloArgument))] string value) => value;

        private static void ValidateHelloArgument(object value) => throw new InvalidOperationException((string)value + "pass1");
    }

    [Fact]
    public async Task validator_works_for_input_fields()
    {
        var queryType = new ObjectGraphType();
        queryType.Field<StringGraphType>("hello1")
            .Argument<AutoRegisteringInputObjectGraphType<FieldTests>>("value")
            .Resolve(ctx => ctx.GetArgument<FieldTests>("value").Field1);
        queryType.Field<StringGraphType>("hello2")
            .Argument<AutoRegisteringInputObjectGraphType<FieldTests>>("value")
            .Resolve(ctx => ctx.GetArgument<FieldTests>("value").Field2);
        queryType.Field<StringGraphType>("hello3")
            .Argument<AutoRegisteringInputObjectGraphType<FieldTests>>("value")
            .Resolve(ctx => ctx.GetArgument<FieldTests>("value").Field3);
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        var query = """
            {
              hello1(value: { field1: "abc" })
              hello2(value: { field2: "def" })
              hello3(value: { field3: "ghi" })
            }
            """;
        var expected = """
            {
              "errors": [
                {
                  "message": "Invalid value for argument 'value' of field 'hello1'. abcpass1",
                  "locations": [
                    {
                      "line": 2,
                      "column": 27
                    }
                  ],
                  "extensions": {
                    "code": "INVALID_VALUE",
                    "codes": [
                      "INVALID_VALUE",
                      "INVALID_OPERATION"
                    ],
                    "number": "5.6"
                  }
                },
                {
                  "message": "Invalid value for argument 'value' of field 'hello2'. defpass2",
                  "locations": [
                    {
                      "line": 3,
                      "column": 27
                    }
                  ],
                  "extensions": {
                    "code": "INVALID_VALUE",
                    "codes": [
                      "INVALID_VALUE",
                      "INVALID_OPERATION"
                    ],
                    "number": "5.6"
                  }
                },
                {
                  "message": "Invalid value for argument 'value' of field 'hello3'. ghipass3",
                  "locations": [
                    {
                      "line": 4,
                      "column": 27
                    }
                  ],
                  "extensions": {
                    "code": "INVALID_VALUE",
                    "codes": [
                      "INVALID_VALUE",
                      "INVALID_OPERATION"
                    ],
                    "number": "5.6"
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

    public class FieldTests
    {
        [Validator(nameof(ValidateHelloArgument))]
        public string? Field1 { get; set; }
        [Validator(typeof(ValidatorClass))]
        public string? Field2 { get; set; }
        [Validator(typeof(HelperClass), nameof(HelperClass.ValidateHelloArgument))]
        public string? Field3 { get; set; }

        private static void ValidateHelloArgument(object value) => throw new InvalidOperationException((string)value + "pass1");
    }

    public class ValidatorClass
    {
        public static void Validator(object value) => throw new InvalidOperationException((string)value + "pass2");
    }

    public class HelperClass
    {
        public static void ValidateHelloArgument(object value) => throw new InvalidOperationException((string)value + "pass3");
    }
}
