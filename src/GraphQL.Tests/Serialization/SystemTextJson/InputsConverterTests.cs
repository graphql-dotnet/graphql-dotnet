using System.Text.Encodings.Web;
using System.Text.Json;
using GraphQL.SystemTextJson;

namespace GraphQL.Tests.Serialization.SystemTextJson;

public class InputsConverterTests
{
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new InputsJsonConverter(),
            new JsonConverterBigInteger(),
        }
    };

    [Fact]
    public void Throws_For_Deep_Objects()
    {
        var value = "{\"a\":" + new string('[', 65) + new string(']', 65) + "}";
        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<Inputs>(value, _options));
    }

    [Fact]
    public void Deserialize_And_Serialize_Introspection()
    {
        string json = "IntrospectionResult".ReadJsonResult();

        var data = JsonSerializer.Deserialize<Inputs>(json, _options);

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

        var actual = JsonSerializer.Deserialize<Inputs>(json, _options);

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

        var actual = JsonSerializer.Deserialize<Inputs>(json, _options);

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

        var actual = JsonSerializer.Deserialize<Inputs>(json, _options);

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

        var actual = JsonSerializer.Deserialize<Inputs>(json, _options);

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

        var actual = JsonSerializer.Deserialize<Inputs>(json, _options);

        var complex = actual["complex"].ShouldBeAssignableTo<IDictionary<string, object>>();
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

        var actual = JsonSerializer.Deserialize<Inputs>(json, _options);

        actual["int"].ShouldBe(123);
        actual["bool"].ShouldBe(true);

        var complex = actual["complex"].ShouldBeAssignableTo<IDictionary<string, object>>();
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

    [Fact]
    public void Serialize_Nested_SimpleValues()
    {
        var source = new Nested
        {
            Dictionary = new Dictionary<string, object>
            {
                ["int"] = 123,
                ["string"] = "string"
            }.ToInputs(),
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

    [Fact]
    public void Serialize_Nested_Simple_Null()
    {
        var source = new Nested
        {
            Dictionary = new Dictionary<string, object>
            {
                ["string"] = null
            }.ToInputs(),
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

    [Fact]
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
            }.ToInputs(),
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

        public Inputs Dictionary { get; set; }

        public int Value2 { get; set; }
    }
}
