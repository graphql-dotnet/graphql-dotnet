using GraphQL.Types;
using Newtonsoft.Json;

namespace GraphQL.Tests.Execution
{
    public class TestComplexScalarType : ScalarGraphType
    {
        public TestComplexScalarType()
        {
            Name = "ComplexScalar";
        }

        public override object Coerce(object value)
        {
            if (value is string && value.Equals("SerializedValue"))
            {
                return "DeserializedValue";
            }

            if (value is string && value.Equals("DeserializedValue"))
            {
                return "SerializedValue";
            }

            return null;
        }
    }

    public class TestInputObject : InputObjectGraphType
    {
        public TestInputObject()
        {
            Name = "TestInputObject";
            Field<StringGraphType>("a");
            Field<ListGraphType<StringGraphType>>("b");
            Field<NonNullGraphType<StringGraphType>>("c");
            Field<TestComplexScalarType>("d");
        }
    }

    public class TestType : ObjectGraphType
    {
        public TestType()
        {
            Name = "TestType";

            Field<StringGraphType>(
                "fieldWithObjectInput",
                arguments: new QueryArguments(new [] { new QueryArgument<TestInputObject> { Name = "input"} }),
                resolve: context =>
                {
                    var result = JsonConvert.SerializeObject(context.Arguments["input"]);
                    return result;
                });
        }
    }

    public class VariablesSchema : Schema
    {
        public VariablesSchema()
        {
            Query = new TestType();
        }
    }

    public class Variables_With_Inline_Structs_Tests : QueryTestBase<VariablesSchema>
    {
        [Test]
        public void executes_with_complex_input()
        {
            var query = @"
            {
              fieldWithObjectInput(input: {a: ""foo"", b: [""bar""], c: ""baz""})
            }
            ";
            var expected = "{ \"fieldWithObjectInput\": \"{\\\"a\\\":\\\"foo\\\",\\\"b\\\":[\\\"bar\\\"],\\\"c\\\":\\\"baz\\\"}\" }";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void properly_parses_single_value_to_list()
        {
            var query = @"
            {
              fieldWithObjectInput(input: {a: ""foo"", b: ""bar"", c: ""baz""})
            }
            ";
            var expected = "{ \"fieldWithObjectInput\": \"{\\\"a\\\":\\\"foo\\\",\\\"b\\\":[\\\"bar\\\"],\\\"c\\\":\\\"baz\\\"}\" }";

            AssertQuerySuccess(query, expected);
        }

        [Test]
        public void does_not_use_incorrect_value()
        {
            var query = @"
            {
              fieldWithObjectInput(input: [""foo"", ""bar"", ""baz""])
            }
            ";
            var expected = "{ \"fieldWithObjectInput\": \"null\" }";

            AssertQuerySuccess(query, expected);
        }
    }
}
