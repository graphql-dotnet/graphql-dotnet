using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
