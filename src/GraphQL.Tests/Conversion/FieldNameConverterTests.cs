using System.Linq;
using GraphQL;
using GraphQL.Conversion;
using GraphQL.Tests.Execution;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Conversion
{
    public class FieldNameConverterTests : BasicQueryTestBase
    {
        public ISchema build_schema(IFieldNameConverter converter = null)
        {
            var schema = new Schema();
            schema.FieldNameConverter = converter ?? new CamelCaseFieldNameConverter();

            var person = new ObjectGraphType();
            person.Name = "Person";
            person.Field("Name", new StringGraphType());

            var query = new ObjectGraphType();
            query.Name = "Query";
            query.Field("PeRsoN", person, resolve: ctx =>
            {
                return new Person{ Name = "Quinn" };
            });

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
            "{ peRsoN: { name: \"Quinn\" } }");
        }

        [Fact]
        public void camel_case_ignores_aliases()
        {
            AssertQuerySuccess(_ =>
            {
                _.Schema = build_schema();
                _.Query = "{ peRsoN { Na: name } }";
            },
            "{ peRsoN: { Na: \"Quinn\" } }");
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
            "{ PeRsoN: { naME: \"Quinn\" } }");
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
            "{ PeRsoN: { naME: \"Quinn\" } }");
        }
    }
}
