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
    public class DeserializationTests : DeserializationTestBase
    {
        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void StringToInputs(IGraphQLTextSerializer serializer)
        {
            var actual = serializer.Deserialize<Inputs>(ExampleJson);
            Verify(actual);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{ExampleJson}}}";
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
            var test = $"{{\"Query\":\"hello\",\"Variables\":{ExampleJson}}}";
            var actual = serializer.Deserialize<TestClass2>(test);
            actual.Query.ShouldBe("hello");
            var variables = serializer.ReadNode<Inputs>(actual.Variables);
            Verify(variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public void FromJson_IsCaseInsensitive_Inputs(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"Query\":\"hello\",\"Variables\":{ExampleJson}}}";
            var actual = serializer.Deserialize<TestClass1>(test);
            actual.Query.ShouldBe("hello");
            var variables = actual.Variables;
            Verify(variables);
        }

        [Theory]
        [ClassData(typeof(GraphQLSerializersTestData))]
        public async Task FromJsonStream(IGraphQLTextSerializer serializer)
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{ExampleJson}}}";
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
            var test = $"{{\"query\":\"hello\",\"variables\":{ExampleJson}}}";
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
    }
}
