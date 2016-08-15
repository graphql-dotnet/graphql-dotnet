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
                mutation SetData{
                    setData (request:4){
                        data
                    }
                }
            ";

            var expected = @"{
              setData: {
                data: 4
              }
            }";

           GraphAssert.QuerySuccess(schema, query, expected);
        }

        [Fact]
        public void BasicExample_WithEnums_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider());
            var schema = schemaGenerator.CreateSchema(typeof(EchoStateSchema));

            var query = @"
                mutation SetState{
                    setState (request:Open){
                        state
                    }
                }
            ";

            var expected = @"{
              setState: {
                state: ""Open""
              }
            }";

            GraphAssert.QuerySuccess(schema, query, expected);
        }

        [Fact]
        public void AdvancedExample_WithEnums_Works()
        {
            var schemaGenerator = new SchemaGenerator(new MockServiceProvider());
            var schema = schemaGenerator.CreateSchema(typeof(EchoStateSchema));

            var query = @"
                mutation SetState{
                    set (request:{state:Open, data:2}){
                        state
                        data
                    }
                }
            ";

            var expected = @"{
              set: {
                state: ""Open"",
                data: 2
              }
            }";

            GraphAssert.QuerySuccess(schema, query, expected);
        }

    }
}
