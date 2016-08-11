using System.Linq;
using GraphQL.SchemaGenerator.Tests.Mocks;
using GraphQL.StarWars;
using GraphQL.StarWars.IoC;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using Xunit;

namespace GraphQL.SchemaGenerator.Tests
{
    public class SchemaGeneratorTests
    {
        [Fact]
        public void BasicExample_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider(), new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(StarWarsAttributeSchema));

            var query = @"
                query HeroNameQuery {
                  hero {
                    name
                  }
                }
            ";

            var expected = @"{
              hero: {
                name: 'R2-D2'
              }
            }";

           GraphAssert.QuerySuccess(schema, query, expected);
        }

        /// <summary>
        ///     The generate schema aligns with the self generated one.
        /// </summary>
        [Fact]
        public void Schemas_Align()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider(), new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(StarWarsAttributeSchema));

            var container = new SimpleContainer();
            container.Singleton(new StarWarsData());
            container.Register<StarWarsQuery>();
            container.Register<HumanType>();
            container.Register<DroidType>();
            container.Register<CharacterInterface>();
            var manualSchema = new StarWarsSchema(type => (GraphType)container.Get(type));

            Assert.Equal(manualSchema.Query.Fields.Count(), schema.Query.Fields.Count());
            Assert.Equal(manualSchema.AllTypes.Count(), schema.AllTypes.Count()+2); //todo work on interface and enum
        }

        //[Fact] //skipped enums array works but not the new hashset.
        public void BasicExample_WithEnums_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider(), new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(StarWarsAttributeSchema));

            var query = @"
                query HeroNameQuery {
                  hero {
                    appearsIn
                    friends
                  }
                }
            ";

            var expected = @"{
              hero: {
                appearsIn: [4,5,6],
                friends: [1,4]
              }
            }";

            GraphAssert.QuerySuccess(schema, query, expected);
        }

        [Fact]
        public void CreateSchema_WithClassArgument_HasExpectedSchema()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider(), new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(SchemaEcho));

            var sut = schema.AllTypes;
            Assert.True(sut.Any(t=>t.Name == "Input_Schema1Request"));
        }

        [Fact]
        public void BasicParameterExample_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider(), new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(SchemaEcho));

            var query = @"{
                  testRequest {value}
                }";

            var expected = @"{
              testRequest: {value:5}
                }";

            GraphAssert.QuerySuccess(schema, query, expected);
        }

        [Fact]
        public void WithParameterExample_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider(), new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(SchemaEcho));

            var query = @"{
                  testRequest(request:{echo:1}) {value}
                }";

            var expected = @"{
              testRequest: {value:1}
                }";

            GraphAssert.QuerySuccess(schema, query, expected);
        }

        [Fact]
        public void WithEnumerableExample_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider(), new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(SchemaEcho));

            var query = @"{
                  testEnumerable{value}
                }";

            var expected = @"{
                  testEnumerable: [{value: 1},{value: 5}]
                }";

            GraphAssert.QuerySuccess(schema, query, expected);
        }

        [Fact]
        public void WithEnum_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider(), new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(SchemaEcho));

            var query = @"{
                  testRequest {enum}
                }";

            var expected = @"{
              testRequest: {enum:""NEWHOPE""}
                }";

            GraphAssert.QuerySuccess(schema, query, expected);
        }
    }
}
