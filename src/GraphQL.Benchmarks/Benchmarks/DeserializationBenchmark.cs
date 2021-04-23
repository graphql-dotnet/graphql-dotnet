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
        public Inputs NewtonsoftJson() => GraphQL.NewtonsoftJson.StringExtensions.ToInputs(Json);

        [Benchmark]
        public Inputs SystemTextJson() => GraphQL.SystemTextJson.StringExtensions.ToInputs(Json);

        void IBenchmark.RunProfiler()
        {
            Code = "Introspection";
            _ = SystemTextJson();
        }
    }
}
