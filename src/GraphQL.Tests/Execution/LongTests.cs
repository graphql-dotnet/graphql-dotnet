using System.Threading.Tasks;
using GraphQL.Types;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Execution
{
    public class LongTests
    {
        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public async Task LongMaxValueShouldBeSerialized(IGraphQLSerializer serializer)
        {
            var documentExecuter = new DocumentExecuter();
            var executionResult = await documentExecuter.ExecuteAsync(_ =>
            {
                _.Schema = new LongSchema();
                _.Query = "{ testField }";
            });

            var json = await serializer.WriteToStringAsync(executionResult);
            executionResult.Errors.ShouldBeNull();

            json.ShouldBe(@"{
  ""data"": {
    ""testField"": 9223372036854775807
  }
}");
        }

        private class LongSchema : Schema
        {
            public LongSchema()
            {
                Query = new Query();
            }
        }

        private class Query : ObjectGraphType<object>
        {
            public Query()
            {
                Field<NonNullGraphType<LongGraphType>>("TestField", resolve: _ => long.MaxValue);
            }
        }
    }
}
