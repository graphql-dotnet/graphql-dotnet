using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Transport;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Serialization
{
    /// <summary>
    /// Tests for <see cref="IGraphQLTextSerializer"/> implementations and the custom converters
    /// that are used in the process of serializing an <see cref="ExecutionResult"/> to JSON.
    /// </summary>
    public class GraphQLRequestTests : DeserializationTestBase
    {
        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_GraphQLRequest_Correctly_Simple(IGraphQLTextSerializer serializer)
        {
            var request = new GraphQLRequest
            {
                Query = "hello",
            };

            var expected = @"{ ""query"": ""hello"" }";

            var actual = serializer.Serialize(request);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_GraphQLRequest_Correctly_Complex(IGraphQLTextSerializer serializer)
        {
            var request = new GraphQLRequest
            {
                Query = "hello",
                OperationName = "opname",
                Variables = new Inputs(new Dictionary<string, object>
                {
                    { "arg1", 1 },
                    { "arg2", "test" },
                }),
                Extensions = new Inputs(new Dictionary<string, object>
                {
                    { "arg1", 2 },
                    { "arg2", "test2" },
                }),
            };

            var expected = @"{ ""query"": ""hello"", ""operationName"": ""opname"", ""variables"": { ""arg1"": 1, ""arg2"": ""test"" }, ""extensions"": { ""arg1"": 2, ""arg2"": ""test2"" } }";

            var actual = serializer.Serialize(request);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_GraphQLRequest_Correctly_SampleData(IGraphQLTextSerializer serializer)
        {
            var request = new GraphQLRequest
            {
                Query = "hello",
                Variables = new Inputs(new Dictionary<string, object>
                {
                    { "arg", ExampleData },
                }),
            };

            var expected = $"{{ \"query\": \"hello\", \"variables\": {{ \"arg\": {ExampleJson} }} }}";

            var actual = serializer.Serialize(request);

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_GraphQLRequest_List_Correctly(IGraphQLTextSerializer serializer)
        {
            var request = new GraphQLRequest
            {
                Query = "hello",
            };

            var expected = @"[{ ""query"": ""hello"" }]";

            var actual = serializer.Serialize(new List<GraphQLRequest> { request });

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Writes_GraphQLRequest_Array_Correctly(IGraphQLTextSerializer serializer)
        {
            var request = new GraphQLRequest
            {
                Query = "hello",
            };

            var expected = @"[{ ""query"": ""hello"" }]";

            var actual = serializer.Serialize(new GraphQLRequest[] { request });

            actual.ShouldBeCrossPlatJson(expected);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{ExampleJson}}}";
            var actual = serializer.Deserialize<GraphQLRequest>(test);
            actual.Query.ShouldBe("hello");
            Verify(actual.Variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_IsCaseSensitive(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"Query\":\"hello\",\"Variables\":{ExampleJson}}}";
            Should.Throw<Exception>(() => serializer.Deserialize<GraphQLRequest>(test));
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_List(IGraphQLTextSerializer serializer)
            => Reads_GraphQLRequest_Test<List<GraphQLRequest>>(serializer);

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_Enumerable(IGraphQLTextSerializer serializer)
            => Reads_GraphQLRequest_Test<IEnumerable<GraphQLRequest>>(serializer);

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_Array(IGraphQLTextSerializer serializer)
            => Reads_GraphQLRequest_Test<GraphQLRequest[]>(serializer);

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_IList(IGraphQLTextSerializer serializer)
            => Reads_GraphQLRequest_Test<IList<GraphQLRequest>>(serializer);

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_IReadOnlyList(IGraphQLTextSerializer serializer)
            => Reads_GraphQLRequest_Test<IReadOnlyList<GraphQLRequest>>(serializer);

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_ICollection(IGraphQLTextSerializer serializer)
            => Reads_GraphQLRequest_Test<ICollection<GraphQLRequest>>(serializer);

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_IReadOnlyCollection(IGraphQLTextSerializer serializer)
            => Reads_GraphQLRequest_Test<IReadOnlyCollection<GraphQLRequest>>(serializer);

        private void Reads_GraphQLRequest_Test<T>(IGraphQLTextSerializer serializer)
            where T : IEnumerable<GraphQLRequest>
        {
            var test = $"[{{\"query\":\"hello\",\"variables\":{ExampleJson}}}, {{\"query\":\"dummy\"}}]";
            var actual = serializer.Deserialize<T>(test);
            actual.Count().ShouldBe(2);
            var request = actual.First();
            request.Query.ShouldBe("hello");
            Verify(request.Variables);
            var request2 = actual.Last();
            request2.Query.ShouldBe("dummy");
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_List_Multiple_Items(IGraphQLTextSerializer serializer)
        {
            var test = $"[{{\"query\":\"hello\",\"variables\":{ExampleJson}}},{{\"query\":\"hello2\"}}]";
            var actual = serializer.Deserialize<List<GraphQLRequest>>(test);
            actual.Count.ShouldBe(2);
            actual[0].Query.ShouldBe("hello");
            Verify(actual[0].Variables);
            actual[1].Query.ShouldBe("hello2");
            actual[1].Variables.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_List_No_Items(IGraphQLTextSerializer serializer)
        {
            var test = $"[]";
            var actual = serializer.Deserialize<List<GraphQLRequest>>(test);
            actual.Count.ShouldBe(0);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_List_Reads_Single_Item(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{ExampleJson}}}";
            var actual = serializer.Deserialize<List<GraphQLRequest>>(test);
            var request = actual.ShouldHaveSingleItem();
            request.Query.ShouldBe("hello");
            Verify(request.Variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_Nulls(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":null,\"operationName\":null,\"variables\":null,\"extensions\":null}}";
            var actual = serializer.Deserialize<GraphQLRequest>(test);
            actual.Query.ShouldBeNull();
            actual.OperationName.ShouldBeNull();
            actual.Variables.ShouldBeNull();
            actual.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_Empty(IGraphQLTextSerializer serializer)
        {
            var test = $"{{}}";
            var actual = serializer.Deserialize<GraphQLRequest>(test);
            actual.Query.ShouldBeNull();
            actual.OperationName.ShouldBeNull();
            actual.Variables.ShouldBeNull();
            actual.Extensions.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_Other_Properties(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"operationName\":\"hello2\",\"extensions\":{ExampleJson}}}";
            var actual = serializer.Deserialize<GraphQLRequest>(test);
            actual.Query.ShouldBe("hello");
            actual.OperationName.ShouldBe("hello2");
            Verify(actual.Extensions);
        }

    }
}
