using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using GraphQL.Transports.Json;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Serialization
{
    public class DeserializationTests
    {
        private readonly TestData _example = new TestData
        {
            array = new object[]
                {
                    null,
                    "test",
                    123,
                    1.2
                },
            obj = new TestChildData
            {
                itemNull = null,
                itemString = "test",
                itemNum = 123,
                itemFloat = 12.4,
            },
            itemNull = null,
            itemString = "test",
            itemNum = 123,
            itemFloat = 12.4,
            itemBigInt = BigInteger.Parse("1234567890123456789012345678901234567890"),
        };
        private readonly string _exampleJson = "{\"array\":[null,\"test\",123,1.2],\"obj\":{\"itemNull\":null,\"itemString\":\"test\",\"itemNum\":123,\"itemFloat\":12.4},\"itemNull\":null,\"itemString\":\"test\",\"itemNum\":123,\"itemFloat\":12.4,\"itemBigInt\":1234567890123456789012345678901234567890}";

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void StringToInputs(IGraphQLTextSerializer serializer)
        {
            var actual = serializer.Deserialize<Inputs>(_exampleJson);
            Verify(actual);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var actual = serializer.Deserialize<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            Verify(actual.Variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson_Null(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":null}}";
            var actual = serializer.Deserialize<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson_Missing(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\"}}";
            var actual = serializer.Deserialize<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson_IsCaseInsensitive_Element(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"Query\":\"hello\",\"Variables\":{_exampleJson}}}";
            var actual = serializer.Deserialize<TestClass2>(test);
            actual.Query.ShouldBe("hello");
            var variables = serializer.ReadNode<Inputs>(actual.Variables);
            Verify(variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson_IsCaseInsensitive_Inputs(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"Query\":\"hello\",\"Variables\":{_exampleJson}}}";
            var actual = serializer.Deserialize<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            var variables = actual.Variables;
            Verify(variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public async Task FromJsonStream(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var testData = new MemoryStream(Encoding.UTF8.GetBytes(test));
            var actual = await serializer.ReadAsync<TestClass1>(testData);
            actual.Query.ShouldBe("hello");
            Verify(actual.Variables);
            // verify that the stream has not been disposed
            testData.ReadByte().ShouldBe(-1);
            testData.Dispose();
            Should.Throw<ObjectDisposedException>(() => testData.ReadByte());
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void ElementToInputs(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var actual = serializer.Deserialize<TestClass2>(test);
            actual.Query.ShouldBe("hello");
            var variables = serializer.ReadNode<Inputs>(actual.Variables);
            Verify(variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var actual = serializer.Deserialize<GraphQLRequest>(test);
            actual.Query.ShouldBe("hello");
            Verify(actual.Variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_IsCaseSensitive(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"Query\":\"hello\",\"Variables\":{_exampleJson}}}";
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
            var test = $"[{{\"query\":\"hello\",\"variables\":{_exampleJson}}}]";
            var actual = serializer.Deserialize<T>(test);
            var request = actual.ShouldHaveSingleItem();
            request.Query.ShouldBe("hello");
            Verify(request.Variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_GraphQLRequest_List_Multiple_Items(IGraphQLTextSerializer serializer)
        {
            var test = $"[{{\"query\":\"hello\",\"variables\":{_exampleJson}}},{{\"query\":\"hello2\"}}]";
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
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
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
            var test = $"{{\"query\":\"hello\",\"operationName\":\"hello2\",\"extensions\":{_exampleJson}}}";
            var actual = serializer.Deserialize<GraphQLRequest>(test);
            actual.Query.ShouldBe("hello");
            actual.OperationName.ShouldBe("hello2");
            Verify(actual.Extensions);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_WebSocketMessage_Populated(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"id\":\"hello\",\"type\":\"hello2\",\"payload\":{{\"query\":\"hello3\",\"variables\":{_exampleJson}}}}}";
            var actual = serializer.Deserialize<WebSocketMessage>(test);
            actual.Id.ShouldBe("hello");
            actual.Type.ShouldBe("hello2");
            actual.Payload.ShouldNotBeNull();
            var request = serializer.ReadNode<GraphQLRequest>(actual.Payload);
            request.Query.ShouldBe("hello3");
            Verify(request.Variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_WebSocketMessage_Nulls(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"id\":null,\"type\":null,\"payload\":null}}";
            var actual = serializer.Deserialize<WebSocketMessage>(test);
            actual.Id.ShouldBeNull();
            actual.Type.ShouldBeNull();
            actual.Payload.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void Reads_WebSocketMessage_Empty(IGraphQLTextSerializer serializer)
        {
            var test = $"{{}}";
            var actual = serializer.Deserialize<WebSocketMessage>(test);
            actual.Id.ShouldBeNull();
            actual.Type.ShouldBeNull();
            actual.Payload.ShouldBeNull();
        }
        /*
        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void ElementToInputs_ReturnsEmptyForNull(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":null}}";
            var actual = serializer.Deserialize<TestClass2>(test);
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
            var variables = serializer.ReadNode<Inputs>(actual.Variables);
            variables.ShouldNotBeNull();
            variables.Count.ShouldBe(0);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void ElementToInputs_ReturnsEmptyForMissing(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\"}}";
            var actual = serializer.Deserialize<TestClass2>(test);
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
            var variables = serializer.ReadNode<Inputs>(actual.Variables);
            variables.ShouldNotBeNull();
            variables.Count.ShouldBe(0);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void ToInputsReturnsEmptyForNull(IGraphQLTextSerializer serializer)
        {
            serializer.Deserialize<Inputs>(null).ShouldNotBeNull().Count.ShouldBe(0);
        }
        */

        private class TestClass1
        {
            public string Query { get; set; }
            public Inputs Variables { get; set; }
        }

        private class TestClass2
        {
            public string Query { get; set; }
            public object Variables { get; set; }
        }

        public class TestData
        {
            public object[] array { get; set; }
            public TestChildData obj { get; set; }
            public string itemNull { get; set; }
            public string itemString { get; set; }
            public int itemNum { get; set; }
            public double itemFloat { get; set; }
            public BigInteger itemBigInt { get; set; }
        }

        public class TestChildData
        {
            public string itemNull { get; set; }
            public string itemString { get; set; }
            public int itemNum { get; set; }
            public double itemFloat { get; set; }
        }

        private void Verify(IReadOnlyDictionary<string, object> actual)
        {
            var array = actual["array"].ShouldBeOfType<List<object>>();
            array[0].ShouldBeNull();
            array[1].ShouldBeOfType<string>().ShouldBe("test");
            array[2].ShouldBeOfType<int>().ShouldBe(123);
            array[3].ShouldBeOfType<double>().ShouldBe(1.2);
            var obj = actual["obj"].ShouldBeOfType<Dictionary<string, object>>();
            obj["itemNull"].ShouldBeNull();
            obj["itemString"].ShouldBeOfType<string>().ShouldBe("test");
            obj["itemNum"].ShouldBeOfType<int>().ShouldBe(123);
            obj["itemFloat"].ShouldBeOfType<double>().ShouldBe(12.4);
            actual["itemNull"].ShouldBeNull();
            actual["itemString"].ShouldBeOfType<string>().ShouldBe("test");
            actual["itemNum"].ShouldBeOfType<int>().ShouldBe(123);
            actual["itemFloat"].ShouldBeOfType<double>().ShouldBe(12.4);
            actual["itemBigInt"].ShouldBeOfType<BigInteger>().ShouldBe(_example.itemBigInt);
        }
    }
}
