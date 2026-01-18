using System.Linq.Expressions;
using GraphQL.Types;

namespace GraphQL.Tests.Attributes;

public class ParserAttributeTests
{
    [Fact]
    public void null_throws()
    {
        Should.Throw<ArgumentNullException>(() => new ParserAttribute((string)null!));
        Should.Throw<ArgumentNullException>(() => new ParserAttribute((Type)null!));
        Should.Throw<ArgumentNullException>(() => new ParserAttribute(null!, "test"));
        Should.Throw<ArgumentNullException>(() => new ParserAttribute(typeof(HelperClass), null!));
    }

    [Theory]
    [InlineData(typeof(Class1), "Could not find method 'InvalidMethod' on CLR type 'Class1' while initializing argument 'value'. The method must have a single parameter of type object, or two parameters of type object and IValueConverter.")]
    [InlineData(typeof(Class2), "Could not find method 'Parse' on CLR type 'Dummy' while initializing argument 'value'. The method must have a single parameter of type object, or two parameters of type object and IValueConverter.")]
    [InlineData(typeof(Class3), "Could not find method 'InvalidMethod' on CLR type 'Dummy' while initializing argument 'value'. The method must have a single parameter of type object, or two parameters of type object and IValueConverter.")]
    [InlineData(typeof(Class4), "Method 'InvalidMethod' on CLR type 'Class4' must have a return type of object.")]
    [InlineData(typeof(Class5), "Method 'Parse' on CLR type 'Dummy2' must have a return type of object.")]
    [InlineData(typeof(Class6), "Method 'InvalidMethod' on CLR type 'Dummy2' must have a return type of object.")]
    public void method_not_found_arguments(Type clrType, string expectedMessage)
    {
        // create a delegate to call the constructor of the AutoRegisteringObjectGraphType
        var graphType = typeof(AutoRegisteringObjectGraphType<>).MakeGenericType(clrType);
        var fn = Expression.Lambda<Func<object>>(Expression.New(graphType)).Compile();

        Should.Throw<InvalidOperationException>(() => fn()).Message.ShouldBe(expectedMessage);
    }

    public class Class1
    {
        public string Hello([Parser("InvalidMethod")] string value) => value;
        private object InvalidMethod(object value) => value; // not static
        private static void InvalidMethod() { } // wrong signature
    }

    public class Class2
    {
        public string Hello([Parser(typeof(Dummy))] string value) => value;
    }

    public class Class3
    {
        public string Hello([Parser(typeof(Dummy), "InvalidMethod")] string value) => value;
    }

    public class Class4
    {
        public string Hello([Parser("InvalidMethod")] string value) => value;
        private static string InvalidMethod(object value) => (string)value;
    }

    public class Class5
    {
        public string Hello([Parser(typeof(Dummy2))] string value) => value;
    }

    public class Class6
    {
        public string Hello([Parser(typeof(Dummy2), "InvalidMethod")] string value) => value;
    }

    [Theory]
    [InlineData(typeof(Class1b), "Could not find method 'InvalidMethod' on CLR type 'Class1b' while initializing 'Class1b.Hello'. The method must have a single parameter of type object, or two parameters of type object and IValueConverter.")]
    [InlineData(typeof(Class2b), "Could not find method 'Parse' on CLR type 'Dummy' while initializing 'Class2b.Hello'. The method must have a single parameter of type object, or two parameters of type object and IValueConverter.")]
    [InlineData(typeof(Class3b), "Could not find method 'InvalidMethod' on CLR type 'Dummy' while initializing 'Class3b.Hello'. The method must have a single parameter of type object, or two parameters of type object and IValueConverter.")]
    [InlineData(typeof(Class4b), "Method 'InvalidMethod' on CLR type 'Class4b' must have a return type of object.")]
    [InlineData(typeof(Class5b), "Method 'Parse' on CLR type 'Dummy2' must have a return type of object.")]
    [InlineData(typeof(Class6b), "Method 'InvalidMethod' on CLR type 'Dummy2' must have a return type of object.")]
    public void method_not_found_input_fields(Type clrType, string expectedMessage)
    {
        // create a delegate to call the constructor of the AutoRegisteringInputObjectGraphType
        var graphType = typeof(AutoRegisteringInputObjectGraphType<>).MakeGenericType(clrType);
        var fn = Expression.Lambda<Func<object>>(Expression.New(graphType)).Compile();

        Should.Throw<InvalidOperationException>(() => fn()).Message.ShouldBe(expectedMessage);
    }

    public class Class1b
    {
        [Parser("InvalidMethod")]
        public string Hello { get; set; }
        private object InvalidMethod(object value) => value; // not static
        private static void InvalidMethod() { } // wrong signature
    }

    public class Class2b
    {
        [Parser(typeof(Dummy))]
        public string Hello { get; set; }
    }

    public class Class3b
    {
        [Parser(typeof(Dummy), "InvalidMethod")]
        public string Hello { get; set; }
    }

    public class Class4b
    {
        [Parser("InvalidMethod")]
        public string Hello { get; set; }
        private static string InvalidMethod(object value) => (string)value;
    }

    public class Class5b
    {
        [Parser(typeof(Dummy2))]
        public string Hello { get; set; }
    }

    public class Class6b
    {
        [Parser(typeof(Dummy2), "InvalidMethod")]
        public string Hello { get; set; }
    }

    public class Dummy
    {
        public object Parse(object value) => value; // not static
        public static void Parse() { } // wrong signature
        public object InvalidMethod(object value) => value; // not static
        public static void InvalidMethod() { } // wrong signature
    }

    public class Dummy2
    {
        public static string Parse(object value) => (string)value;
        public static string InvalidMethod(object value) => (string)value;
    }

    [Fact]
    public async Task parser_works_for_arguments()
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
              "data": {
                "hello1": "abctest1",
                "hello2": "deftest2",
                "hello3": "ghitest3",
                "hello4": "jkltest1"
              }
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
        public static string Hello1([Parser(nameof(ParseHelloArgument))] string value) => value;
        public static string Hello2([Parser(typeof(ParserClass))] string value) => value;
        public static string Hello3([Parser(typeof(HelperClass), nameof(HelperClass.ParseHelloArgument))] string value) => value;
        public static string Hello4([Parser(typeof(ArgTests), nameof(ParseHelloArgument))] string value) => value;

        private static object ParseHelloArgument(object value) => (string)value + "test1";
    }

    [Fact]
    public async Task parser_works_for_input_fields()
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
        queryType.Field<StringGraphType>("hello4")
            .Argument<AutoRegisteringInputObjectGraphType<FieldTests>>("value")
            .Resolve(ctx => ctx.GetArgument<FieldTests>("value").Field4);
        var schema = new Schema { Query = queryType };
        schema.Initialize();
        var query = """
            {
              hello1(value: { field1: "abc" })
              hello2(value: { field2: "def" })
              hello3(value: { field3: "ghi" })
              hello4(value: { field4: "jkl" })
            }
            """;
        var expected = """
            {
              "data": {
                "hello1": "abctest1",
                "hello2": "deftest2",
                "hello3": "ghitest3",
                "hello4": "jkltest1"
              }
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
        [Parser(nameof(ParseHelloArgument))]
        public string? Field1 { get; set; }
        [Parser(typeof(ParserClass))]
        public string? Field2 { get; set; }
        [Parser(typeof(HelperClass), nameof(HelperClass.ParseHelloArgument))]
        public string? Field3 { get; set; }
        [Parser(typeof(FieldTests), nameof(ParseHelloArgument))]
        public string? Field4 { get; set; }

        private static object ParseHelloArgument(object value) => (string)value + "test1";
    }

    public class ParserClass
    {
        public static object Parse(object value) => (string)value + "test2";
    }

    public class HelperClass
    {
        public static object ParseHelloArgument(object value) => (string)value + "test3";
    }
}
