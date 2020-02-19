using System.Collections.Generic;
using System.IO;
using BenchmarkDotNet.Attributes;

namespace GraphQL.Benchmarks
{
    [MemoryDiagnoser]
    [RPlotExporter, CsvMeasurementsExporter]
    public class DeserializationBenchmark
    {
        private string _json;

        private static readonly System.Text.Json.JsonSerializerOptions _jsonOptionsOld = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters =
            {
                new SystemTextJson.ObjectDictionaryConverterOld()
            }
        };

        private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            Converters =
            {
                new SystemTextJson.ObjectDictionaryConverter()
            }
        };

        [GlobalSetup]
        public void GlobalSetup()
        {
            _json = File.ReadAllText("data.json");

            //var a = NewtonsoftJson();
            //var b = SystemTextJsonOld();
            //var c = SystemTextJson();
        }

        [Benchmark(Baseline = true)]
        public Dictionary<string, object> NewtonsoftJson() => GraphQL.NewtonsoftJson.StringExtensions.ToDictionary(_json);

        [Benchmark]
        public Dictionary<string, object> SystemTextJsonOld() => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(_json, _jsonOptionsOld);

        [Benchmark]
        public Dictionary<string, object> SystemTextJson() => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(_json, _jsonOptions);
    }
}
