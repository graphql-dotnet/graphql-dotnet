using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests
{
    /// <summary>
    /// Tests for <see cref="IDocumentWriter"/> implementations and the custom converters
    /// that are used in the process of serializing an <see cref="ExecutionResult"/> to JSON.
    /// </summary>
    public class GraphQLRequestReaderTests
    {
        [Theory]
        [ClassData(typeof(GraphQLRequestReadersTestData))]
        public void Can_Read_GraphQLRequest(IGraphQLRequestReader reader)
        {
            var sample = @"
{
  ""query"": ""test"",
  ""operationName"": ""hello"",
  ""variables"": { ""int"": 1, ""str"": ""value"" },
  ""extensions"": { ""int"": 2, ""str"": ""VALUE"" }
}";

            var result = reader.Read<GraphQLRequest>(sample);
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBe("hello");
            result.Variables.ShouldBe<IDictionary<string, object>>(new Dictionary<string, object>()
            {
                { "int", 1 },
                { "str", "value" }
            });
            result.Extensions.ShouldBe<IDictionary<string, object>>(new Dictionary<string, object>()
            {
                { "int", 2 },
                { "str", "VALUE" }
            });
        }

        [Theory]
        [ClassData(typeof(GraphQLRequestReadersTestData))]
        public void Can_Read_GraphQLRequest_Simple(IGraphQLRequestReader reader)
        {
            var sample = @"
{
  ""query"": ""test""
}";

            var result = reader.Read<GraphQLRequest>(sample);
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBeNull();
            result.Variables.ShouldBeNull();
            result.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLRequestReadersTestData))]
        public void Can_Read_GraphQLRequest_List_Single(IGraphQLRequestReader reader)
        {
            var sample = @"
{
  ""query"": ""test""
}";

            var result = reader.Read<List<GraphQLRequest>>(sample).ShouldHaveSingleItem();
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBeNull();
            result.Variables.ShouldBeNull();
            result.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLRequestReadersTestData))]
        public void Can_Read_GraphQLRequest_List_Multiple(IGraphQLRequestReader reader)
        {
            var sample = @"
[
  {
    ""query"": ""test""
  },
  {
    ""query"": ""test2""
  }
]";

            var result = reader.Read<List<GraphQLRequest>>(sample);
            result.Count.ShouldBe(2);
            result[0].Query.ShouldBe("test");
            result[0].OperationName.ShouldBeNull();
            result[0].Variables.ShouldBeNull();
            result[0].Extensions.ShouldBeNull();
            result[1].Query.ShouldBe("test2");
            result[1].OperationName.ShouldBeNull();
            result[1].Variables.ShouldBeNull();
            result[1].Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLRequestReadersTestData))]
        public void Can_Read_GraphQLRequest_Array_Single(IGraphQLRequestReader reader)
        {
            var sample = @"
{
  ""query"": ""test""
}";

            var result = reader.Read<GraphQLRequest[]>(sample).ShouldHaveSingleItem();
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBeNull();
            result.Variables.ShouldBeNull();
            result.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLRequestReadersTestData))]
        public void Can_Read_GraphQLRequest_Array_Multiple(IGraphQLRequestReader reader)
        {
            var sample = @"
[
  {
    ""query"": ""test""
  },
  {
    ""query"": ""test2""
  }
]";

            var result = reader.Read<GraphQLRequest[]>(sample);
            result.Length.ShouldBe(2);
            result[0].Query.ShouldBe("test");
            result[0].OperationName.ShouldBeNull();
            result[0].Variables.ShouldBeNull();
            result[0].Extensions.ShouldBeNull();
            result[1].Query.ShouldBe("test2");
            result[1].OperationName.ShouldBeNull();
            result[1].Variables.ShouldBeNull();
            result[1].Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLRequestReadersTestData))]
        public void Can_Read_GraphQLRequest_Enumerable_Single(IGraphQLRequestReader reader)
        {
            var sample = @"
{
  ""query"": ""test""
}";

            var result = reader.Read<IEnumerable<GraphQLRequest>>(sample).ShouldHaveSingleItem();
            result.Query.ShouldBe("test");
            result.OperationName.ShouldBeNull();
            result.Variables.ShouldBeNull();
            result.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLRequestReadersTestData))]
        public void Can_Read_GraphQLRequest_Enumerable_Multiple(IGraphQLRequestReader reader)
        {
            var sample = @"
[
  {
    ""query"": ""test""
  },
  {
    ""query"": ""test2""
  }
]";

            var result = reader.Read<IEnumerable<GraphQLRequest>>(sample).ToList();
            result.Count.ShouldBe(2);
            result[0].Query.ShouldBe("test");
            result[0].OperationName.ShouldBeNull();
            result[0].Variables.ShouldBeNull();
            result[0].Extensions.ShouldBeNull();
            result[1].Query.ShouldBe("test2");
            result[1].OperationName.ShouldBeNull();
            result[1].Variables.ShouldBeNull();
            result[1].Extensions.ShouldBeNull();
        }
    }

    internal static class GraphQLReaderExtensions
    {
        public static T Read<T>(this IGraphQLRequestReader reader, string json)
            => reader.ReadAsync<T>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)), default).Result;
    }
}
