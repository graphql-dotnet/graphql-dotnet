using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;

namespace GraphQL.Tests.Attributes;

public class DefaultAstValueAttributeTests
{
    [Fact]
    public void TestDefaultArgumentsAndDefaultFields()
    {
        var services = new ServiceCollection();
        services.AddGraphQL(b => b
            .AddAutoSchema<QueryType>());
        var provider = services.BuildServiceProvider();
        var schema = provider.GetRequiredService<ISchema>();
        schema.Initialize();
        var sdl = schema.Print();
        sdl.ShouldBe("""
            type Query {
              testString(value: String! = "str"): String!
              testStringArray(value: [String]! = ["str1", "str2", null]): String!
              testNumber(value: Int! = 123): String!
              testNumberArray(value: [Int]! = [123, 456, null]): String!
              testObject(value: TestObject!): String!
              testObject2(value: TestObject2! = {field1: "str"}): String!
              testObject2Array(value: [TestObject2!]! = [{field1: "str1"}, {field1: "str2"}]): String!
            }

            input TestObject {
              field1: String!
              testString: String! = "str"
              testStringArray: [String!]! = ["str1", "str2"]
              testNumber: Int! = 123
              testNumberArray: [Int!]! = [123, 456]
              testObject2: TestObject2! = {field1: "str"}
              testObject2Array: [TestObject2!]! = [{field1: "str1"}, {field1: "str2"}]
            }

            input TestObject2 {
              field1: String!
            }

            """);
    }

    public class QueryType
    {
        public string TestString([DefaultAstValue("\"str\"")] string value)
        {
            return value;
        }

        public string TestStringArray([DefaultAstValue("[\"str1\", \"str2\", null]")] string?[] value)
        {
            return string.Join(", ", value);
        }

        public string TestNumber([DefaultAstValue("123")] int value)
        {
            return value.ToString();
        }

        public string TestNumberArray([DefaultAstValue("[123, 456, null]")] int?[] value)
        {
            return string.Join(", ", value);
        }

        public string TestObject(TestObject value)
        {
            return value.Field1;
        }

        public string TestObject2([DefaultAstValue("{ field1: \"str\" }")] TestObject2 value)
        {
            return value.Field1;
        }

        public string TestObject2Array([DefaultAstValue("[{ field1: \"str1\" }, { field1: \"str2\" }]")] TestObject2[] value)
        {
            return string.Join(", ", value.Select(x => x.Field1));
        }
    }

    public class TestObject
    {
        public required string Field1 { get; set; }

        [DefaultAstValue("\"str\"")]
        public required string TestString { get; set; }

        [DefaultAstValue("[\"str1\", \"str2\"]")]
        public required string[] TestStringArray { get; set; }

        [DefaultAstValue("123")]
        public required int TestNumber { get; set; }

        [DefaultAstValue("[123, 456]")]
        public required int[] TestNumberArray { get; set; }

        [DefaultAstValue("{ field1: \"str\" }")]
        public required TestObject2 TestObject2 { get; set; }

        [DefaultAstValue("[{ field1: \"str1\" }, { field1: \"str2\" }]")]
        public required TestObject2[] TestObject2Array { get; set; }
    }

    public class TestObject2
    {
        public required string Field1 { get; set; }
    }
}
