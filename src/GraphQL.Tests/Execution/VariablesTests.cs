using System;
using GraphQL.Types;
using Newtonsoft.Json;
using Should;

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

            return value;
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

    public class UsingVariablesTests : QueryTestBase<VariablesSchema>
    {
        private string _query = @"
            query q($input: TestInputObject) {
              fieldWithObjectInput(input: $input)
            }
        ";

        [Test]
        public void executes_with_complex_input()
        {
            var expected = "{ \"fieldWithObjectInput\": \"{\\\"a\\\":\\\"foo\\\",\\\"b\\\":[\\\"bar\\\"],\\\"c\\\":\\\"baz\\\"}\" }";

            var inputs = "{'input': {'a':'foo', 'b':['bar'], 'c': 'baz'} }".ToInputs();

            AssertQuerySuccess(_query, expected, inputs);
        }

        [Test]
        public void properly_parses_single_value_to_list()
        {
            var expected = "{ \"fieldWithObjectInput\": \"{\\\"a\\\":\\\"foo\\\",\\\"b\\\":[\\\"bar\\\"],\\\"c\\\":\\\"baz\\\"}\" }";

            var inputs = "{'input': {'a':'foo', 'b':'bar', 'c': 'baz'} }".ToInputs();

            AssertQuerySuccess(_query, expected, inputs);
        }

        [Test]
        public void uses_default_value_when_not_provided()
        {
            var queryWithDefaults = @"
                query q($input: TestInputObject = {a: ""foo"", b: [""bar""] c: ""baz""}) {
                  fieldWithObjectInput(input: $input)
                }
            ";

            var expected = "{ \"fieldWithObjectInput\": \"{\\\"a\\\":\\\"foo\\\",\\\"b\\\":[\\\"bar\\\"],\\\"c\\\":\\\"baz\\\"}\" }";

            AssertQuerySuccess(queryWithDefaults, expected);
        }

        [Test]
        public void executes_with_complex_scalar_input()
        {
            var expected = "{ \"fieldWithObjectInput\": \"{\\\"c\\\":\\\"foo\\\",\\\"d\\\":\\\"DeserializedValue\\\"}\" }";

            var inputs = "{'input': {'c': 'foo', 'd': 'SerializedValue'} }".ToInputs();

            AssertQuerySuccess(_query, expected, inputs);
        }

        [Test]
        public void errors_on_null_for_nested_non_null()
        {
            var expected = "{}";

            var inputs = "{'input': {'a': 'foo', 'b': 'bar', 'c': null} }".ToInputs();

            Exception caughtError = null;

            try
            {
                AssertQueryWithErrors(_query, expected, inputs);
            }
            catch (Exception exc)
            {
                caughtError = exc;
            }

            caughtError.ShouldNotBeNull();
            caughtError.InnerException.Message.ShouldEqual("Variable '$input' expected value of type 'TestInputObject'.");
        }

        [Test]
        public void errors_on_incorrect_type()
        {
            var expected = "{}";

            var inputs = "{'input': 'foo bar'}".ToInputs();

            Exception caughtError = null;

            try
            {
                AssertQueryWithErrors(_query, expected, inputs);
            }
            catch (Exception exc)
            {
                caughtError = exc;
            }

            caughtError.ShouldNotBeNull();
            caughtError.InnerException.Message.ShouldEqual("Variable '$input' expected value of type 'TestInputObject'.");
        }

        [Test]
        public void errors_on_omission_of_nested_non_null()
        {
            var expected = "{}";

            var inputs = "{'input': {'a': 'foo', 'b': 'bar'} }".ToInputs();

            Exception caughtError = null;

            try
            {
                AssertQueryWithErrors(_query, expected, inputs);
            }
            catch (Exception exc)
            {
                caughtError = exc;
            }

            caughtError.ShouldNotBeNull();
            caughtError.InnerException.Message.ShouldEqual("Variable '$input' expected value of type 'TestInputObject'.");
        }

        [Test]
        public void errors_on_addition_of_unknown_input_field()
        {
            var expected = "{}";

            var inputs = "{'input': {'a': 'foo', 'b': 'bar', 'c': 'baz', 'e': 'dog'} }".ToInputs();

            Exception caughtError = null;

            try
            {
                AssertQueryWithErrors(_query, expected, inputs);
            }
            catch (Exception exc)
            {
                caughtError = exc;
            }

            caughtError.ShouldNotBeNull();
            caughtError.InnerException.Message.ShouldEqual("Variable '$input' expected value of type 'TestInputObject'.");
        }
    }
}
