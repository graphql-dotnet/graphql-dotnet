using GraphQL.Http;
using GraphQL.Introspection;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Introspection
{
    public class SchemaIntrospectionTests
    {
        [Fact]
        public void validate_core_schema()
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = documentExecuter.ExecuteAsync(new Schema(), null, SchemaIntrospection.IntrospectionQuery, null).Result;
            var json = new DocumentWriter(true).Write(executionResult.Data);

            ShouldBe(json, IntrospectionResult.Data);
        }

        [Fact]
        public void validate_non_null_schema()
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = documentExecuter.ExecuteAsync(new TestSchema(), null, InputObjectBugQuery, null).Result;
            var json = new DocumentWriter(true).Write(executionResult.Data);
            executionResult.Errors.ShouldBeNull();

            ShouldBe(json, InputObjectBugResult);
        }

        private static void ShouldBe(string actual, string expected)
        {
            Assert.Equal(
                expected.Replace("\\r", "").Replace("\\n", ""),
                actual.Replace("\\r", "").Replace("\\n", ""),
                ignoreLineEndingDifferences: true,
                ignoreWhiteSpaceDifferences: true);
        }

        public static readonly string InputObjectBugQuery = @"
query test {
    __type(name:""SomeInput"") {
        inputFields {
            type {
                name,
                description
                ofType {
                    kind,
                    name
                }
            }
        }
    }
}";

        public static readonly string InputObjectBugResult = "{\r\n  \"__type\": {\r\n    \"inputFields\": [\r\n      {\r\n        \"type\": {\r\n          \"name\": \"String\",\r\n          \"description\": null,\r\n          \"ofType\": null\r\n        }\r\n      }\r\n    ]\r\n  }\r\n}";

        public class SomeInputType : InputObjectGraphType
        {
            public SomeInputType()
                : base()
            {
                Name = "SomeInput";
                Description = "Input values for a patient's demographic information";

                Field<StringGraphType>("address");
            }
        }

        public class RootMutation : ObjectGraphType
        {
            public RootMutation()
            {
                Field<StringGraphType>(
                    "test",
                    arguments: new QueryArguments(new QueryArgument(typeof(SomeInputType)) { Name = "some" }));
            }
        }

        public class TestSchema : Schema
        {
            public TestSchema()
            {
                Mutation = new RootMutation();
            }
        }
    }
}
