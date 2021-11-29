using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using GraphQL.SystemTextJson;
using Shouldly;
using Xunit;

namespace GraphQL.Tests.Serialization.SystemTextJson
{
    public class ObjectDictionaryConverterTests
    {
        private readonly JsonSerializerOptions _options;

        public ObjectDictionaryConverterTests()
        {
            _options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Converters =
                {
                    new ObjectDictionaryConverter(),
                    new JsonConverterBigInteger(),
                }
            };
        }

        [Fact]
        public void Deserialize_And_Serialize_Introspection()
        {
            string json = "IntrospectionResult".ReadJsonResult();

            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

            string roundtrip = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            roundtrip.ShouldBeCrossPlatJson(json);
        }

        [Fact]
        public void Deserialize_SimpleValues()
        {
            string json = @"
                {
                    ""int"": 123,
                    ""double"": 123.456,
                    ""string"": ""string"",
                    ""bool"": true
                }
            ";

            var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

            actual["int"].ShouldBe(123);
            actual["double"].ShouldBe(123.456);
            actual["string"].ShouldBe("string");
            actual["bool"].ShouldBe(true);
        }

        [Fact]
        public void Deserialize_Simple_Null()
        {
            string json = @"
                {
                    ""string"": null
                }
            ";

            var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

            actual["string"].ShouldBeNull();
        }

        [Fact]
        public void Deserialize_Array()
        {
            string json = @"
                {
                    ""values"": [1, 2, 3]
                }
            ";

            var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

            actual["values"].ShouldNotBeNull();
        }

        [Fact]
        public void Deserialize_Array_in_Array()
        {
            string json = @"
                {
                    ""values"": [[1,2,3]]
                }
            ";

            var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

            actual["values"].ShouldNotBeNull();
            object values = actual["values"];
            values.ShouldBeAssignableTo<IEnumerable<object>>();
        }

        [Fact]
        public void Deserialize_ComplexValue()
        {
            string json = @"
                {
                    ""complex"": {
                        ""int"": 123,
                        ""double"": 123.456,
                        ""string"": ""string"",
                        ""bool"": true
                    }
                }
            ";

            var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

            actual["complex"].ShouldBeAssignableTo<IDictionary<string, object>>();
            var complex = (IDictionary<string, object>)actual["complex"];
            complex["int"].ShouldBe(123);
            complex["double"].ShouldBe(123.456);
            complex["string"].ShouldBe("string");
            complex["bool"].ShouldBe(true);
        }

        [Fact]
        public void Deserialize_MixedValue()
        {
            string json = @"
                {
                    ""int"": 123,
                    ""complex"": {
                        ""int"": 123,
                        ""double"": 123.456,
                        ""string"": ""string"",
                        ""bool"": true
                    },
                    ""bool"": true
                }
            ";

            var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(json, _options);

            actual["int"].ShouldBe(123);
            actual["bool"].ShouldBe(true);

            actual["complex"].ShouldBeAssignableTo<IDictionary<string, object>>();
            var complex = (IDictionary<string, object>)actual["complex"];
            complex["int"].ShouldBe(123);
            complex["double"].ShouldBe(123.456);
            complex["string"].ShouldBe("string");
            complex["bool"].ShouldBe(true);
        }

        [Fact]
        public void Deserialize_Nested_SimpleValues()
        {
            string json = @"
                {
                    ""value1"": ""string"",
                    ""dictionary"": {
                        ""int"": 123,
                        ""double"": 123.456,
                        ""string"": ""string"",
                        ""bool"": true
                    },
                    ""value2"": 123
                }
            ";

            var actual = JsonSerializer.Deserialize<Nested>(json, _options);

            actual.Value1.ShouldBe("string");
            actual.Value2.ShouldBe(123);
        }

        [Fact]
        public void Serialize_SimpleValues()
        {
            var source = new Nested
            {
                Value2 = 123,
                Value1 = null
            };

            string json = JsonSerializer.Serialize(source, _options);

            json.ShouldBeCrossPlatJson(
                @"{
  ""value1"": null,
  ""dictionary"": null,
  ""value2"": 123
}".Trim());
        }

        [Fact(Skip = "This converter currently is only intended to be used to read a JSON object into a strongly-typed representation.")]
        public void Serialize_Nested_SimpleValues()
        {
            var source = new Nested
            {
                Dictionary = new Dictionary<string, object>
                {
                    ["int"] = 123,
                    ["string"] = "string"
                },
                Value2 = 123,
                Value1 = "string"
            };

            string json = JsonSerializer.Serialize(source, _options);

            json.ShouldBeCrossPlatJson(
                @"{
  ""value1"": ""string"",
  ""dictionary"": {
    ""int"": 123,
    ""string"": ""string""
  },
  ""value2"": 123
}".Trim());
        }

        [Fact(Skip = "This converter currently is only intended to be used to read a JSON object into a strongly-typed representation.")]
        public void Serialize_Nested_Simple_Null()
        {
            var source = new Nested
            {
                Dictionary = new Dictionary<string, object>
                {
                    ["string"] = null
                },
                Value2 = 123,
                Value1 = "string"
            };

            string json = JsonSerializer.Serialize(source, _options);

            json.ShouldBeCrossPlatJson(
                @"{
  ""value1"": ""string"",
  ""dictionary"": {
    ""string"": null
  },
  ""value2"": 123
}".Trim());
        }

        [Fact(Skip = "This converter currently is only intended to be used to read a JSON object into a strongly-typed representation.")]
        public void Serialize_Nested_ComplexValues()
        {
            var source = new Nested
            {
                Dictionary = new Dictionary<string, object>
                {
                    ["int"] = 123,
                    ["string"] = "string",
                    ["complex"] = new Dictionary<string, object>
                    {
                        ["double"] = 1.123d
                    }
                },
                Value2 = 123,
                Value1 = "string"
            };

            string json = JsonSerializer.Serialize(source, _options);

            json.ShouldBeCrossPlatJson(
                @"{
  ""value1"": ""string"",
  ""dictionary"": {
    ""int"": 123,
    ""string"": ""string"",
    ""complex"": {
      ""double"": 1.123
    }
  },
  ""value2"": 123
}".Trim());
        }

        private class Nested
        {
            public string Value1 { get; set; }

            public Dictionary<string, object> Dictionary { get; set; }

            public int Value2 { get; set; }
        }
    }
}
