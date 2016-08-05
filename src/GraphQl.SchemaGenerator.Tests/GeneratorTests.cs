using System;
using System.CodeDom.Compiler;
using GraphQl.StarWars.Api.Controllers;
using GraphQL;
using GraphQL.Execution;
using GraphQL.Http;
using GraphQL.StarWars;
using GraphQL.Validation;
using Xunit;

namespace GraphQl.SchemaGenerator.Tests
{
    public class GeneratorTests
    {
        [Fact]
        public void BasicExample_Works()
        {
            var schemaGenerator = new SchemaGenerator(null);
            var schema = schemaGenerator.CreateSchema(typeof(StarWarsController));

            var query = @"
                query HeroNameQuery {
                  hero {
                    Test
                  }
                }
            ";

            var expected = @"{
              hero: {
                Test: 'test'
              }
            }";

            GraphAssert.QuerySuccess(schema, query, expected);
        }

  
    }
}
