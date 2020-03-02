using System;
using System.Threading.Tasks;
using GraphQL.Conversion;
using GraphQL.Introspection;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Introspection
{
    public class SchemaIntrospectionTests
    {
        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async Task validate_core_schema(IDocumentWriter documentWriter)
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new Schema
                {
                    Query = new TestQuery()
                };
                _.Query = SchemaIntrospection.IntrospectionQuery;
            });

            var json = await documentWriter.WriteToStringAsync(executionResult);

            ShouldBe(json, IntrospectionResult.Data);
        }

        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async Task validate_core_schema_pascal_case(IDocumentWriter documentWriter)
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new Schema
                {
                    Query = new TestQuery(),
                };
                _.NameConverter = PascalCaseNameConverter.Instance;
                _.Query = SchemaIntrospection.IntrospectionQuery;
            });

            var json = await documentWriter.WriteToStringAsync(executionResult);

            ShouldBe(json, IntrospectionResult.Data);
        }

        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async Task validate_core_schema_doesnt_use_nameconverter(IDocumentWriter documentWriter)
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new Schema
                {
                    Query = new TestQuery(),
                };
                _.NameConverter = new TestNameConverter();
                _.Query = SchemaIntrospection.IntrospectionQuery;
            });

            var json = await documentWriter.WriteToStringAsync(executionResult);

            ShouldBe(json, IntrospectionResult.Data);
        }

        public class TestNameConverter : INameConverter
        {
            public string NameForArgument(string argumentName, IComplexGraphType parentGraphType, IFieldType field) => throw new Exception();

            public string NameForField(string fieldName, IComplexGraphType parentGraphType) => throw new Exception();
        }

        public class TestQuery : ObjectGraphType
        {
            public TestQuery()
            {
                Name = "TestQuery";
            }
        }

        [Theory]
        [ClassData(typeof(DocumentWritersTestData))]
        public async Task validate_non_null_schema(IDocumentWriter documentWriter)
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new TestSchema();
                _.Query = InputObjectBugQuery;
            });

            var json = await documentWriter.WriteToStringAsync(executionResult);
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

        public static readonly string InputObjectBugResult = "{\r\n \"data\": {\r\n  \"__type\": {\r\n    \"inputFields\": [\r\n      {\r\n        \"type\": {\r\n          \"name\": \"String\",\r\n          \"description\": null,\r\n          \"ofType\": null\r\n        }\r\n      }\r\n    ]\r\n  }\r\n }\r\n}";

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
