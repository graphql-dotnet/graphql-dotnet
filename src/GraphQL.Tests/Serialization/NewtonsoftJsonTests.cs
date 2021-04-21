using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using GraphQL.NewtonsoftJson;
using Newtonsoft.Json.Linq;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Serialization
{
    public class NewtonsoftJsonTests
    {
        private readonly object _example = new
        {
            array = new object[]
                {
                    null,
                    "test",
                    123,
                    1.2
                },
            obj = new
            {
                itemNull = (string)null,
                itemString = "test",
                itemNum = 123,
                itemFloat = 12.3,
            },
            itemNull = (string)null,
            itemString = "test",
            itemNum = 123,
            itemFloat = 12.3,
            itemBigInt = BigInteger.Parse("1234567890123456789012345678901234567890"),
        };
        private readonly string _exampleJson = "{\"array\":[null,\"test\",123,1.2],\"obj\":{\"itemNull\":null,\"itemString\":\"test\",\"itemNum\":123,\"itemFloat\":12.3},\"itemNull\":null,\"itemString\":\"test\",\"itemNum\":123,\"itemFloat\":12.3,\"itemBigInt\":1234567890123456789012345678901234567890}";

        [Fact]
        public async Task SerializeWithDocumentWriter()
        {
            var dw = new DocumentWriter();
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
        public void ElementToInputs()
        {
            var test = $"{{\"query\":\"hello\",\"variables\":{_exampleJson}}}";
            var actual = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClass2>(test);
            actual.query.ShouldBe("hello");
            var variables = actual.variables.ToInputs();
            Verify(variables);
        }

        private class TestClass1
        {
            public string query { get; set; }
            public Dictionary<string, object> variables { get; set; }
        }

        private class TestClass2
        {
            public string query { get; set; }
            public JObject variables { get; set; }
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
            obj["itemFloat"].ShouldBeOfType<double>().ShouldBe(12.3);
            actual["itemNull"].ShouldBeNull();
            actual["itemString"].ShouldBeOfType<string>().ShouldBe("test");
            actual["itemNum"].ShouldBeOfType<int>().ShouldBe(123);
            actual["itemFloat"].ShouldBeOfType<double>().ShouldBe(12.3);
            actual["itemBigInt"].ShouldBeOfType<BigInteger>().ShouldBe((BigInteger)((dynamic)_example).itemBigInt);
        }
    }
}
