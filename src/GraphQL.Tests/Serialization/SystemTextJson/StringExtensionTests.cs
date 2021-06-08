using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using GraphQL.SystemTextJson;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Serialization.SystemTextJson
{
    public class StringExtensionTests
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

        [Fact]
        public async Task SerializeWithDocumentWriter()
        {
            var dw = new DocumentWriter();
            // note: WriteToStringAsync<object>(...) always returns "{}" on .Net Core 2.1 / 3.1, but works fine on 5.0
            // so we need to use a strongly typed object here
            var actual = await dw.WriteToStringAsync(_example);
            actual.ShouldBe(_exampleJson);
        }

        [Fact]
        public void StringToDictionary()
        {
            var actual = _exampleJson.ToDictionary();
            Verify(actual);
        }

        [Fact]
        public void StringToInputs()
        {
            var actual = _exampleJson.ToInputs();
            Verify(actual);
        }

        [Fact]
        public void FromJson_Null()
        {
            var test = $"{{\"query\":\"hello\",\"variables\":null}}";
            var actual = test.FromJson<TestClass3>();
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
        }

        [Fact]
        public void FromJson_Missing()
        {
            var test = $"{{\"query\":\"hello\"}}";
            var actual = test.FromJson<TestClass3>();
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
        }

        [Fact]
        public void FromJson_Inputs()
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var actual = test.FromJson<TestClass3>();
            actual.Query.ShouldBe("hello");
            Verify(actual.Variables);
        }

        [Fact]
        public void FromJson_Inputs_Null()
        {
            var test = $"{{\"query\":\"hello\",\"variables\":null}}";
            var actual = test.FromJson<TestClass3>();
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
        }

        [Fact]
        public void FromJson_Inputs_Missing()
        {
            var test = $"{{\"query\":\"hello\"}}";
            var actual = test.FromJson<TestClass3>();
            actual.Query.ShouldBe("hello");
            actual.Variables.ShouldBeNull();
        }

        [Fact]
        public void FromJson_IsCaseSensitive_Element()
        {
            var test = $"{{\"Query\":\"hello\",\"Variables\":{_exampleJson}}}";
            var actual = test.FromJson<TestClass2>();
            actual.Query.ShouldBeNull();
            actual.Variables.ValueKind.ShouldBe(JsonValueKind.Undefined);
        }

        [Fact]
        public void FromJson_IsCaseSensitive_Inputs()
        {
            var test = $"{{\"Query\":\"hello\",\"Variables\":{_exampleJson}}}";
            var actual = test.FromJson<TestClass3>();
            actual.Query.ShouldBeNull();
            actual.Variables.ShouldBeNull();
        }

        [Fact]
        public async Task FromJsonAsync_Inputs()
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var testData = new MemoryStream(Encoding.UTF8.GetBytes(test));
            var actual = await testData.FromJsonAsync<TestClass3>();
            actual.Query.ShouldBe("hello");
            Verify(actual.Variables);
        }

        [Fact]
        public void ElementToInputs()
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var actual = test.FromJson<TestClass2>();
            actual.Query.ShouldBe("hello");
            var variables = actual.Variables.ToInputs();
            Verify(variables);
        }

        [Fact]
        public void ElementToInputs_ReturnsEmptyForNull()
        {
            var test = $"{{\"query\":\"hello\",\"variables\":null}}";
            var actual = test.FromJson<TestClass2>();
            actual.Query.ShouldBe("hello");
            actual.Variables.ValueKind.ShouldBe(JsonValueKind.Null);
            var variables = actual.Variables.ToInputs();
            variables.ShouldNotBeNull();
            variables.Count.ShouldBe(0);
        }

        [Fact]
        public void ElementToInputs_ReturnsEmptyForMissing()
        {
            var test = $"{{\"query\":\"hello\"}}";
            var actual = test.FromJson<TestClass2>();
            actual.Query.ShouldBe("hello");
            actual.Variables.ValueKind.ShouldBe(JsonValueKind.Undefined);
            var variables = actual.Variables.ToInputs();
            variables.ShouldNotBeNull();
            variables.Count.ShouldBe(0);
        }

        [Fact]
        public void ToInputsReturnsEmptyForNull()
        {
            ((string)null).ToInputs().ShouldNotBeNull().Count.ShouldBe(0);
        }

        private class TestClass2
        {
            public string Query { get; set; }
            public JsonElement Variables { get; set; }
        }

        private class TestClass3
        {
            public string Query { get; set; }
            public Inputs Variables { get; set; }
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
