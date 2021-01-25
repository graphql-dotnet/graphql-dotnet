using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    //[RPlotExporter, CsvMeasurementsExporter]
    public class DeserializationBenchmark : IBenchmark
    {
        private const string SHORT_JSON = @"{
  ""key0"": null,
  ""key1"": true,
  ""key2"": 1.2,
  ""key3"": 10,
  ""dict"": { },
  ""key4"": ""value"",
  ""arr"": [1,2,3],
  ""key5"": {
    ""inner1"": null,
    ""inner2"": 14
  }
}";

        private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters =
            {
                new SystemTextJson.ObjectDictionaryConverter(),
                new SystemTextJson.JsonConverterBigInteger(),
            }
        };

        [GlobalSetup]
        public void GlobalSetup()
        {
            var loadedFromFile = IntrospectionResult.Data;
        }

        public IEnumerable<string> Codes => new[] { "Empty", "Short", "Introspection" };

        [ParamsSource(nameof(Codes))]
        public string Code { get; set; }

        private string Json => Code switch
        {
            "Empty" => "{}",
            "Short" => SHORT_JSON,
            "Introspection" => IntrospectionResult.Data,
            _ => throw new NotSupportedException()
        };

        [Benchmark(Baseline = true)]
        public Dictionary<string, object> NewtonsoftJson() => GraphQL.NewtonsoftJson.StringExtensions.ToDictionary(Json);

        [Benchmark]
        public Dictionary<string, object> SystemTextJson() => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(Json, _jsonOptions);

        void IBenchmark.RunProfiler()
        {
            Code = "Introspection";
            _ = SystemTextJson();
        }
    }
}
