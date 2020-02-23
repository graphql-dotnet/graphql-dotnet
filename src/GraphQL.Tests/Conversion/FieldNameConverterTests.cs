using GraphQL.Conversion;
using GraphQL.Tests.Execution;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Conversion
{
    public class FieldNameConverterTests : BasicQueryTestBase
    {
        public ISchema build_schema(IFieldNameConverter converter = null, string argument = "Id")
        {
            var schema = new Schema
            {
                FieldNameConverter = converter ?? CamelCaseFieldNameConverter.Instance
            };

            var person = new ObjectGraphType { Name = "Person" };
            person.Field("Name", new StringGraphType());

            var query = new ObjectGraphType { Name = "Query" };
            query.Field(
                "PeRsoN",
                person,
                arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = argument }),
                resolve: ctx => new Person { Name = "Quinn" });

            schema.Query = query;
            return schema;
        }

        [Fact]
        public void defaults_to_camel_case()
        {
            AssertQuerySuccess(_ =>
            {
                _.Schema = build_schema();
                _.Query = "{ peRsoN { name } }";
            },
            @"{ ""peRsoN"": { ""name"": ""Quinn"" } }");
        }

        [Fact]
        public void camel_case_ignores_aliases()
        {
            AssertQuerySuccess(_ =>
            {
                _.Schema = build_schema();
                _.Query = "{ peRsoN { Na: name } }";
            },
            @"{ ""peRsoN"": { ""Na"": ""Quinn"" } }");
        }

        [Fact]
        public void pascal_case_ignores_aliases()
        {
            var converter = new PascalCaseFieldNameConverter();

            AssertQuerySuccess(_ =>
            {
                _.Schema = build_schema(converter);
                _.Query = "{ PeRsoN { naME: Name } }";
                _.FieldNameConverter = converter;
            },
            @"{ ""PeRsoN"": { ""naME"": ""Quinn"" } }");
        }

        [Fact]
        public void default_case_ignores_aliases()
        {
            var converter = new DefaultFieldNameConverter();
            AssertQuerySuccess(_ =>
            {
                _.Schema = build_schema(converter);
                _.Query = "{ PeRsoN { naME: Name } }";
                _.FieldNameConverter = converter;
            },
            @"{ ""PeRsoN"": { ""naME"": ""Quinn"" } }");
        }

        [Fact]
        public void arguments_default_to_camel_case()
        {
            var schema = build_schema();
            schema.Initialize();

            var query = schema.FindType("Query") as IObjectGraphType;
            var field = query.GetField("peRsoN");
            field.Arguments.Find("id").ShouldNotBeNull();
        }

        [Fact]
        public void arguments_can_use_pascal_case()
        {
            var schema = build_schema(new PascalCaseFieldNameConverter(), "iD");
            schema.Initialize();

            var query = schema.FindType("Query") as IObjectGraphType;
            var field = query.GetField("PeRsoN");
            field.Arguments.Find("ID").ShouldNotBeNull();
        }
    }
}
