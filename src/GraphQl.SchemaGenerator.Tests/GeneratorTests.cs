using GraphQl.SchemaGenerator.Tests.Mocks;
using GraphQl.StarWars.Api.Controllers;
using GraphQL.StarWars;
using Xunit;

namespace GraphQl.SchemaGenerator.Tests
{
    public class GeneratorTests
    {
        [Fact]
        public void BasicExample_Works()
        {
            var data = new StarWarsData();
            var item = data.GetDroidByIdAsync("3").Result;

            var fields = new MockFieldResolver(item);
            var schemaGenerator = new SchemaGenerator(fields, new GraphTypeResolver());
            var schema = schemaGenerator.CreateSchema(typeof(StarWarsController));

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
