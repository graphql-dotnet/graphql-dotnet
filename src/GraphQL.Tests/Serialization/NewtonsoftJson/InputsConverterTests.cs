using GraphQL.NewtonsoftJson;
using Newtonsoft.Json;
using JsonSerializerSettings = GraphQL.NewtonsoftJson.JsonSerializerSettings;

namespace GraphQL.Tests.Serialization.NewtonsoftJson;

public class InputsConverterTests
{
    private static readonly JsonSerializer _jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateParseHandling = DateParseHandling.None,
        Formatting = Formatting.Indented,
        Converters =
        {
            new InputsJsonConverter()
        },
    });

    private T Deserialize<T>(string json)
    {
        using var stringReader = new System.IO.StringReader(json);
        using var jsonReader = new JsonTextReader(stringReader);
        return _jsonSerializer.Deserialize<T>(jsonReader);
    }

    private string Serialize<T>(T data)
    {
        using var stringWriter = new System.IO.StringWriter();
        using (var jsonWriter = new JsonTextWriter(stringWriter))
        {
            _jsonSerializer.Serialize(jsonWriter, data);
        }
        return stringWriter.ToString();
    }

    [Fact]
    public void Throws_For_Deep_Objects()
    {
        var value = "{\"a\":" + new string('[', 65) + new string(']', 65) + "}";
        Should.Throw<JsonReaderException>(() => Deserialize<Inputs>(value));
    }

    [Fact]
    public void Deserialize_And_Serialize_Introspection()
    {
        string json = "IntrospectionResult".ReadJsonResult();

        var data = Deserialize<Inputs>(json);

        string roundtrip = Serialize(data);

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

        var actual = Deserialize<Inputs>(json);

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

        var actual = Deserialize<Inputs>(json);

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

        var actual = Deserialize<Inputs>(json);

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

        var actual = Deserialize<Inputs>(json);

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

        var actual = Deserialize<Inputs>(json);

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

        var actual = Deserialize<Inputs>(json);

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

        var actual = Deserialize<Nested>(json);

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

        string json = Serialize(source);

        json.ShouldBeCrossPlatJson(
            @"{
  ""Value1"": null,
  ""Dictionary"": null,
  ""Value2"": 123
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

        string json = Serialize(source);

        json.ShouldBeCrossPlatJson(
            @"{
  ""Value1"": ""string"",
  ""Dictionary"": {
    ""int"": 123,
    ""string"": ""string""
  },
  ""Value2"": 123
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

        string json = Serialize(source);

        json.ShouldBeCrossPlatJson(
            @"{
  ""Value1"": ""string"",
  ""Dictionary"": {
    ""string"": null
  },
  ""Value2"": 123
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

        string json = Serialize(source);

        json.ShouldBeCrossPlatJson(
            @"{
  ""Value1"": ""string"",
  ""Dictionary"": {
    ""int"": 123,
    ""string"": ""string"",
    ""complex"": {
      ""double"": 1.123
    }
  },
  ""Value2"": 123
}".Trim());
    }

    [Fact]
    public void Deserializes_Dates()
    {
        var d = new DateTimeOffset(2022, 1, 3, 15, 47, 22, System.TimeSpan.Zero);
        var json = $"{{ \"date\": \"{d:o}\"}}";

        var jsonSerializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            Converters =
            {
                new InputsJsonConverter()
            },
        });

        var dic = jsonSerializer.Deserialize<Inputs>(new JsonTextReader(new StringReader(json)));
        dic.ShouldContainKeyAndValue("date", d);
    }

    private class Nested
    {
        public string Value1 { get; set; }

        public Inputs Dictionary { get; set; }

        public int Value2 { get; set; }
    }
}
