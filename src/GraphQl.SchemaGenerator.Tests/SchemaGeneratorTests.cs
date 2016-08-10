using System.Linq;
using GraphQl.SchemaGenerator.Tests.Mocks;
using GraphQL.StarWars;
using GraphQL.StarWars.IoC;
using GraphQL.StarWars.Types;
using GraphQL.Types;
using Xunit;

namespace GraphQl.SchemaGenerator.Tests
{
    public class SchemaGeneratorTests
    {
        [Fact]
        public void BasicExample_Works()
        {
            var data = new StarWarsData();
            var item = data.GetDroidByIdAsync("3").Result;

            var fields = new MockFieldResolver(item);
            var schemaGenerator = new SchemaGenerator(fields, new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(StarWars));

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
            var data = new StarWarsData();
            var item = data.GetDroidByIdAsync("3").Result;

            var fields = new MockFieldResolver(item);
            var schemaGenerator = new SchemaGenerator(fields, new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(StarWars));

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

        [Fact]
        public void BasicExample_WithFieldGenerator_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockGraphFieldResolver(), new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(StarWars));

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


    }
}
