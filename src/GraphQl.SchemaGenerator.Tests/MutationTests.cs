using GraphQL.SchemaGenerator.Tests.Mocks;
using Xunit;

namespace GraphQL.SchemaGenerator.Tests
{
    public class MutationTests
    {
        [Fact]
        public void BasicExample_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider());
            var schema = schemaGenerator.CreateSchema(typeof(EchoStateSchema));

            var query = @"
                mutation SetState{
                    setState (request:4){
                        value
                    }
                }
            ";

            var expected = @"{
              setState: {
                value: 4
              }
            }";

           GraphAssert.QuerySuccess(schema, query, expected);
        }

    
    }
}
