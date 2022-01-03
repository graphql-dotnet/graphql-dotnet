using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Serialization.NewtonsoftJson
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
            var actual = serializer.Read<Inputs>(_exampleJson);
            Verify(actual);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var actual = serializer.Read<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            Verify(actual.Variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson_Null(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":null}}";
            var actual = serializer.Read<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson_Missing(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\"}}";
            var actual = serializer.Read<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
        }

        [Fact]
        public void FromJson_IsCaseInsensitive_Element_Newtonsoft()
        {
            var serializer = new GraphQL.NewtonsoftJson.GraphQLSerializer();
            var test = $"{{\"Query\":\"hello\",\"Variables\":{_exampleJson}}}";
            var actual = serializer.Read<TestClass2>(test);
            actual.Query.ShouldBe("hello");
            var variables = serializer.Read<Inputs>(actual.Variables);
            Verify(variables);
        }

        /*
#if NET6_0_OR_GREATER
        [Fact]
        public void FromJson_IsCaseInsensitive_Element_SystemTextJson()
        {
            var serializer = new GraphQL.SystemTextJson.GraphQLSerializer();
            var test = $"{{\"Query\":\"hello\",\"Variables\":{_exampleJson}}}";
            var actual = serializer.Read<TestClass2SystemTextJson>(test);
            actual.query.ShouldBe("hello");
            var variables = serializer.Read<Inputs>(actual.variables);
            Verify(variables);
        }
#endif

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson_IsCaseInsensitive_Inputs(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"Query\":\"hello\",\"Variables\":{_exampleJson}}}";
            var actual = serializer.Read<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            var variables = actual.Variables;
            Verify(variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJsonStream(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var testData = new MemoryStream(Encoding.UTF8.GetBytes(test));
            var actual = serializer.Read<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            Verify(actual.Variables);
            // verify that the stream has not been disposed
            testData.ReadByte().ShouldBe(-1);
            testData.Dispose();
            Should.Throw<ObjectDisposedException>(() => testData.ReadByte());
        }
        */

        [Fact]
        public void ElementToInputs_NewtonsoftJson()
        {
            var serializer = new GraphQL.NewtonsoftJson.GraphQLSerializer();
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var actual = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClass2>(test);
            actual.Query.ShouldBe("hello");
            var variables = serializer.Read<Inputs>(actual.Variables);
            Verify(variables);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void ElementToInputs_SystemTextJson()
        {
            var serializer = new GraphQL.SystemTextJson.GraphQLSerializer();
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var actual = System.Text.Json.JsonSerializer.Deserialize<TestClass2SystemTextJson>(test);
            actual.query.ShouldBe("hello");
            var variables = serializer.Read<Inputs>(actual.variables);
            Verify(variables);
        }
#endif

        /*
        [Fact]
        public void ElementToInputs_ReturnsEmptyForNull_NewtonsoftJson()
        {
            var serializer = new GraphQL.NewtonsoftJson.GraphQLSerializer();
            var test = $"{{\"query\":\"hello\",\"variables\":null}}";
            var actual = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClass2>(test);
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
            var variables = serializer.Read<Inputs>(actual.Variables);
            variables.ShouldNotBeNull();
            variables.Count.ShouldBe(0);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void ElementToInputs_ReturnsEmptyForNull_SystemTextJson()
        {
            var serializer = new GraphQL.SystemTextJson.GraphQLSerializer();
            var test = $"{{\"query\":\"hello\",\"variables\":null}}";
            var actual = System.Text.Json.JsonSerializer.Deserialize<TestClass2SystemTextJson>(test);
            actual.Query.ShouldBe("hello");
            var variables = serializer.Read<Inputs>(actual.Variables);
            variables.ShouldNotBeNull();
            variables.Count.ShouldBe(0);
        }
#endif

        [Fact]
        public void ElementToInputs_ReturnsEmptyForMissing_NewtonsoftJson()
        {
            var serializer = new GraphQL.NewtonsoftJson.GraphQLSerializer();
            var test = $"{{\"query\":\"hello\"}}";
            var actual = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClass2>(test);
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
            var variables = serializer.Read<Inputs>(actual.Variables);
            variables.ShouldNotBeNull();
            variables.Count.ShouldBe(0);
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void ElementToInputs_ReturnsEmptyForMissing_SystemTextJson()
        {
            var serializer = new GraphQL.SystemTextJson.GraphQLSerializer();
            var test = $"{{\"query\":\"hello\"}}";
            var actual = System.Text.Json.JsonSerializer.Deserialize<TestClass2SystemTextJson>(test);
            actual.Query.ShouldBe("hello");
            var variables = serializer.Read<Inputs>(actual.Variables);
            variables.ShouldNotBeNull();
            variables.Count.ShouldBe(0);
        }
#endif

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void ToInputsReturnsEmptyForNull(IGraphQLTextSerializer serializer)
        {
            serializer.Read<Inputs>(null).ShouldNotBeNull().Count.ShouldBe(0);
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
            public Newtonsoft.Json.Linq.JObject Variables { get; set; }
        }

        private class TestClass2SystemTextJson
        {
            public string query { get; set; }
            public System.Text.Json.JsonElement variables { get; set; }
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
