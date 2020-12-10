using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
                    NameConverter = PascalCaseNameConverter.Instance,
                };
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
                    NameConverter = new TestNameConverter(),
                };
                _.Query = SchemaIntrospection.IntrospectionQuery;
            });

            var json = await documentWriter.WriteToStringAsync(executionResult);

            ShouldBe(json, IntrospectionResult.Data);
        }

        public class TestNameConverter : INameConverter
        {
            public string NameForArgument(string argumentName, IComplexGraphType parentGraphType, FieldType field) => throw new Exception();

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
        public async Task validate_null_field_schema_comparer_gives_original_order_of_fields(IDocumentWriter documentWriter)
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new Schema
                {
                    Query = TestQueryType(),
                    Comparer = new NulledOutTestSchemaComparer()
                };
                _.Query = SchemaIntrospection.GetFieldNamesOfTypesQuery;
            });
            var scalarTypeNames = new[] { "String", "Boolean", "Int" };

            static string GetName(JsonElement el) => el.GetProperty("name").GetString();

            var json = JsonDocument.Parse(await documentWriter.WriteToStringAsync(executionResult));

            var types = json.RootElement
                .GetProperty("data")
                .GetProperty("__schema")
                .GetProperty("types")
                .EnumerateArray()
                .Where(el => !GetName(el).StartsWith("__") && !scalarTypeNames.Contains(GetName(el))) // not interested in introspection or scalar types
                .ToList(); 

            Assert.Equal(3, types.Count);
            ShouldBe(GetName(types[0]), "Query");
            Assert.Equal(new[] { "field2", "field1" }, types[0].GetProperty("fields").EnumerateArray().Select(GetName));
            ShouldBe(GetName(types[1]), "Things");
            Assert.Equal(new[] { "foo", "bar", "baz" }, types[1].GetProperty("fields").EnumerateArray().Select(GetName));
            ShouldBe(GetName(types[2]), "Letters");
            Assert.Equal(new[] { "bravo", "charlie", "alfa", "delta" }, types[2].GetProperty("fields").EnumerateArray().Select(GetName));
        }

        private static IObjectGraphType TestQueryType()
        {
            var type1 = new ObjectGraphType { Name = "Letters" };
            type1.AddField(new FieldType { Name = "bravo",   ResolvedType = new StringGraphType() });
            type1.AddField(new FieldType { Name = "charlie", ResolvedType = new IntGraphType() });
            type1.AddField(new FieldType { Name = "alfa",   ResolvedType = new ListGraphType(new IntGraphType()) });
            type1.AddField(new FieldType { Name = "delta",   ResolvedType = new StringGraphType() });

            var type2 = new ObjectGraphType { Name = "Things" };
            type2.AddField(new FieldType { Name = "foo", ResolvedType = new IntGraphType() });
            type2.AddField(new FieldType { Name = "bar", ResolvedType = new IntGraphType() });
            type2.AddField(new FieldType { Name = "baz", ResolvedType = new NonNullGraphType(new IntGraphType()) });

            var queryType = new ObjectGraphType { Name = "Query" };
            queryType.AddField(new FieldType { Name = "field2", ResolvedType = type2 });
            queryType.AddField(new FieldType { Name = "field1", ResolvedType = type1 });

            return queryType;
        }

        private class NulledOutTestSchemaComparer : ISchemaComparer
        {
            public IComparer<IGraphType> TypeComparer => null;
            public IComparer<IFieldType> FieldComparer(IGraphType parent) => null;
            public IComparer<QueryArgument> ArgumentComparer(IFieldType field) => null;
            public IComparer<EnumValueDefinition> EnumValueComparer(EnumerationGraphType parent) => null;
            public IComparer<DirectiveGraphType> DirectiveComparer => null;
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
