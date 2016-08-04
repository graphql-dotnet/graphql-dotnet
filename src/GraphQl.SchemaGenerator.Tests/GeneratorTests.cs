using System;
using System.CodeDom.Compiler;
using GraphQl.SchemaGenerator.Definitions;
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
            var data = new GraphQL.StarWars.ModelData();
            var schema = ModelSchemaGenerator.CreateSchema(data);

            var query = @"
                query HeroNameQuery {
                  ModelData {
                    Test
                  }
                }
            ";

            var expected = @"{
              ModelData: {
                Test: 'test'
              }
            }";

            GraphAssert.QuerySuccess(schema, query, expected);
        }

  
    }
}
